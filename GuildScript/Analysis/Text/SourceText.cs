namespace GuildScript.Analysis.Text;

internal sealed class SourceText
{
    public TextSpan All => new(0, Text.Length, this);
    public string Text { get; }

    private readonly List<int> lineNumbers = new();

    public SourceText(string source)
    {
        Text = source;
        ProcessLineNumbers();
    }

    public string ToString(TextSpan span)
    {
        return Text.Substring(span.Start, span.Length);
    }

    private void ProcessLineNumbers()
    {
        lineNumbers.Add(0);
        for (var i = 0; i < Text.Length; i++)
        {
            switch (Text[i])
            {
                case '\r':
                {
                    if (i + 1 < Text.Length && Text[i + 1] == '\n')
                    {
                        i++;
                    }

                    break;
                }
                case '\n':
                    break;
                default:
                    continue;
            }

            lineNumbers.Add(i + 1);
        }
    }

    public int GetPosition(int line, int column)
    {
        if (line <= 0 || line > lineNumbers.Count)
            throw new ArgumentOutOfRangeException(nameof(line));

        var lineStart = lineNumbers[line - 1];
        var lineLength = (line < lineNumbers.Count ? Text.Length : lineNumbers[line]) - lineStart;
        if (column <= 0 || column > lineLength)
            throw new ArgumentOutOfRangeException(nameof(column));

        return lineStart + column;
    }

    public TextSpan GetLineSpan(int line)
    {
        if (line <= 0 || line > lineNumbers.Count)
            throw new ArgumentOutOfRangeException(nameof(line));

        var lineStart = lineNumbers[line - 1];
        var lineLength = (line < lineNumbers.Count ? Text.Length : lineNumbers[line]) - lineStart;

        return new TextSpan(lineStart, lineLength, this);
    }

    public (int line, int column) GetLineAndColumn(int position)
    {
        if (position < 0 || position >= Text.Length)
            return (0, 0);

        var left = 0;
        var right = lineNumbers.Count - 1;
        var line = 0;

        while (left <= right)
        {
            var mid = left + (right - left) / 2;
            line = mid + 1;

            if (lineNumbers[mid] == position)
                break;

            if (lineNumbers[mid] < position)
            {
                if (mid >= lineNumbers.Count - 1 || lineNumbers[mid + 1] > position)
                {
                    line = mid + 1;
                    break;
                }

                left = mid + 1;
            }
            else right = mid - 1;
        }

        var column = position - lineNumbers[line - 1] + 1;
        return (line, column);
    }

    public IEnumerable<EditSequence> GetDifference(SourceText target)
    {
        return StringComparer.GetEditSequences(Text, target.Text);
    }
}