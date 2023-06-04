using GuildScript.Analysis.Text;

namespace GuildScript.Analysis.Syntax;

public sealed class Parser
{
	private readonly Lexer lexer;
	private readonly List<Token> tokens = new();
	private int position;

	public Parser(Lexer lexer)
	{
		this.lexer = lexer;
	}

	public void Parse()
	{
		tokens.Clear();
		tokens.AddRange(lexer.GetTokens());
	}
}