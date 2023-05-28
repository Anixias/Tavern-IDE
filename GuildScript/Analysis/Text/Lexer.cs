namespace GuildScript.Analysis.Text;

public sealed class Lexer
{
	private SourceText sourceText;

	public Lexer(string sourceText)
	{
		this.sourceText = new SourceText(sourceText);
		LexAll();
	}

	private void LexAll()
	{
		LexSpan(0, sourceText.Text.Length);
	}

	private void LexSpan(int start, int length)
	{
		
	}
}