using GuildScript.Analysis.Syntax;
using GuildScript.Analysis.Text;

namespace GuildScript;

public sealed class LanguageServer
{
	private class CodeFile
	{
		private readonly Lexer lexer;
		private readonly Parser parser;

		public CodeFile(string source)
		{
			lexer = new Lexer(source);
			parser = new Parser(lexer);
		}

		public void UpdateSource(string content)
		{
			lexer.Lex(content);
		}

		public Token[] GetTokens()
		{
			return lexer.GetTokens();
		}

		public SyntaxNode? Parse()
		{
			return parser.Parse();
		}
	}
	
	private readonly Dictionary<string, CodeFile> codeFiles = new();

	public void AddFile(string path)
	{
		if (!File.Exists(path))
			return;

		if (codeFiles.ContainsKey(path))
			return;
		
		var content = File.ReadAllText(path).Replace("\r\n", "\n").Replace("\r", "\n");
		var codeFile = new CodeFile(content);
		codeFiles.Add(path, codeFile);
	}

	public void UpdateFile(string path, string content)
	{
		if (!codeFiles.TryGetValue(path, out var codeFile))
			return;

		codeFile.UpdateSource(content);
	}

	public void RemoveFile(string path)
	{
		codeFiles.Remove(path);
	}

	public IEnumerable<Token> GetTokens(string path)
	{
		return !codeFiles.TryGetValue(path, out var codeFile) ? Array.Empty<Token>() : codeFile.GetTokens();
	}

	public SyntaxNode? Parse(string path)
	{
		return !codeFiles.TryGetValue(path, out var codeFile) ? null : codeFile.Parse();
	}
}