namespace GuildScript.Analysis.Text;

public enum TokenType
{
	EndOfFile,
	Invalid,
	WhiteSpace,
	
	Equals,
	Semicolon,
	
	Identifier,
	
	Int32Literal,
}

public sealed class Token
{
	public TokenType Type { get; }
	public TextSpan Span { get; }
	public string Text { get; }
	public object? Value { get; }
	
	internal Token(TokenType type, TextSpan span, string text, object? value)
	{
		Type = type;
		Span = span;
		Text = text;
		Value = value;
	}
}