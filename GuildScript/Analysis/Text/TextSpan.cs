using System.Collections;

namespace GuildScript.Analysis.Text;

public sealed class TextSpan : IEnumerable<int>
{
	public int LineStart { get; }
	public int LineEnd { get; }
	public int ColumnStart { get; }
	public int ColumnEnd { get; }
	public int Start { get; set; }
	public int Length { get; }
	public int End => Start + Length;
	internal SourceText Source { get; }

	public bool Contains(int position)
	{
		return Start <= position && position <= End;
	}

	internal TextSpan(int start, int length, SourceText source)
	{
		Start = start;
		Length = length;
		Source = source;

		(LineStart, ColumnStart) = source.GetLineAndColumn(Start);
		(LineEnd, ColumnEnd) = source.GetLineAndColumn(Start + Length - 1);
	}

	public IEnumerator<int> GetEnumerator()
	{
		for (var i = Start; i <= End; i++)
		{
			yield return i;
		}
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	public bool ContainsLine(int line)
	{
		return LineStart <= line && line <= LineEnd;
	}

	public int StartColumnForLine(int line)
	{
		if (!ContainsLine(line))
			return -1;

		return line == LineStart ? ColumnStart : 1;
	}
	
	public int EndColumnForLine(int line)
	{
		if (!ContainsLine(line))
			return -1;

		return line == LineEnd ? ColumnEnd : Source.LineLength(line);
	}
}