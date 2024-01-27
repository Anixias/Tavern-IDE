using System.Collections.Generic;

namespace Tavern;

public struct TextSpan
{
	public TextSpan(int lineStart, int lineEnd, int columnStart, int columnEnd)
	{
		LineStart = lineStart;
		LineEnd = lineEnd;
		ColumnStart = columnStart;
		ColumnEnd = columnEnd;
	}

	public int LineStart { get; }
	public int LineEnd { get; }
	public int ColumnStart { get; }
	public int ColumnEnd { get; }
}

public struct Error
{
	public TextSpan Span { get; }
	public string Message { get; }
	
	public Error(TextSpan span, string message)
	{
		Span = span;
		Message = message;
	}
}

public interface IRichSyntaxHighlighter
{
	IEnumerable<Error> GetErrors();
}