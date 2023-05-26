using System.Text;

namespace GuildScript.Analysis.Text;

internal enum EditOperationKind : byte
{
    None,    // Nothing to do
    Add,     // Add new character
    Edit,    // Edit character into character (including char into itself)
    Remove,  // Delete existing character
};

internal readonly struct EditSequence
{
    public EditOperation[] Operations { get; }
    public EditOperationKind Operation { get; }
    public int Start { get; }
    public int Length { get; }
    public int End => Start + Length;
    public string SourceText { get; }
    public string TargetText { get; }

    public EditSequence(EditOperation[] operations, EditOperationKind operation, int start, int length)
    {
        Operations = operations;
        Operation = operation;
        Start = start;
        Length = length;

        var sourceText = new StringBuilder();
        var targetText = new StringBuilder();

        foreach (var op in operations)
        {
            if (op.ValueFrom != '\0')
                sourceText.Append(op.ValueFrom);

            if (op.ValueTo != '\0')
                targetText.Append(op.ValueTo);
        }

        SourceText = sourceText.Length > 0 ? sourceText.ToString() : "";
        TargetText = targetText.Length > 0 ? targetText.ToString() : "";
    }
}

internal readonly struct EditOperation
{
    public EditOperation(int position, char valueFrom, char valueTo, EditOperationKind operation)
    {
        Position = position;
        ValueFrom = valueFrom;
        ValueTo = valueTo;

        Operation = valueFrom == valueTo ? EditOperationKind.None : operation;
    }

    public char ValueFrom { get; }
    public char ValueTo { get; }
    public int Position { get; }
    public EditOperationKind Operation { get; }

    public override string ToString()
    {
        return Operation switch
        {
            EditOperationKind.None   => $"'{ValueTo}' Equal",
            EditOperationKind.Add    => $"'{ValueTo}' Add",
            EditOperationKind.Remove => $"'{ValueFrom}' Remove",
            EditOperationKind.Edit   => $"'{ValueFrom}' to '{ValueTo}' Edit",
            _                        => "(???)"
        };
    }
}

internal static class StringComparer
{
    public static IEnumerable<EditOperation> GetEditOperations(string source, string target,
                                                               int insertCost = 1, int removeCost = 1, int editCost = 2)
    {
        if (source is null)
            throw new ArgumentNullException(nameof(source));

        if (target is null)
            throw new ArgumentNullException(nameof(target));

        var bestOperation = Enumerable
            .Range(0, source.Length + 1)
            .Select(_ => new EditOperationKind[target.Length + 1])
            .ToArray();

        var minimumCost = Enumerable
            .Range(0, source.Length + 1)
            .Select(_ => new int[target.Length + 1])
            .ToArray();

        for (var i = 1; i <= source.Length; ++i)
        {
            bestOperation[i][0] = EditOperationKind.Remove;
            minimumCost[i][0] = removeCost * i;
        }

        for (var i = 1; i <= target.Length; ++i)
        {
            bestOperation[0][i] = EditOperationKind.Add;
            minimumCost[0][i] = insertCost * i;
        }

        for (var i = 1; i <= source.Length; ++i)
        {
            for (var j = 1; j <= target.Length; ++j)
            {
                var insert = minimumCost[i][j - 1] + insertCost;
                var delete = minimumCost[i - 1][j] + removeCost;
                var edit = minimumCost[i - 1][j - 1] + (source[i - 1] == target[j - 1] ? 0 : editCost);

                var min = Math.Min(Math.Min(insert, delete), edit);

                if (min == insert)
                    bestOperation[i][j] = EditOperationKind.Add;
                else if (min == delete)
                    bestOperation[i][j] = EditOperationKind.Remove;
                else if (min == edit)
                    bestOperation[i][j] = EditOperationKind.Edit;

                minimumCost[i][j] = min;
            }
        }

        var result = new List<EditOperation>(source.Length + target.Length);

        for (int x = target.Length, y = source.Length; (x > 0) || (y > 0);)
        {
            var op = bestOperation[y][x];

            if (op == EditOperationKind.Add)
            {
                x -= 1;
                result.Add(new EditOperation(x, '\0', target[x], op));
            }
            else if (op == EditOperationKind.Remove)
            {
                y -= 1;
                result.Add(new EditOperation(y, source[y], '\0', op));
            }
            else if (op == EditOperationKind.Edit)
            {
                x -= 1;
                y -= 1;
                result.Add(new EditOperation(y, source[y], target[x], op));
            }
            else break;
        }

        result.Reverse();
        return result.ToArray();
    }

    public static IEnumerable<EditSequence> GetEditSequences(string source, string target,
                                                  int insertCost = 1, int removeCost = 1, int editCost = 2)
    {
        var operations = GetEditOperations(source, target, insertCost, removeCost, editCost);

        EditOperationKind? currentKind = null;
        var currentOperations = new List<EditOperation>();
        var sequences = new List<EditSequence>();
        foreach (var operation in operations)
        {
            if (currentKind is not null && currentKind != operation.Operation)
            {
                if (currentKind != EditOperationKind.None)
                {
                    var start = currentOperations[0].Position;
                    sequences.Add(new EditSequence(currentOperations.ToArray(), currentKind.Value, start,
                        currentOperations.Count));
                }

                currentOperations.Clear();
            }

            currentKind = operation.Operation;
            currentOperations.Add(operation);
        }

        if (currentOperations.Count > 0 && currentKind is not null)
        {
            if (currentKind == EditOperationKind.None)
                return sequences;
            
            var start = currentOperations[0].Position;
            sequences.Add(new EditSequence(currentOperations.ToArray(), currentKind.Value, start,
                currentOperations.Count));
        }

        return sequences;
    }
}