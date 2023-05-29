using GuildScript.Analysis.Text;

namespace GuildScript;

public sealed class LanguageServer
{
	private class CodeFile
	{
		private readonly Lexer lexer;

		public CodeFile(string source)
		{
			lexer = new Lexer(source);
		}

		public void UpdateSource(string content)
		{
			lexer.Lex(content);
		}

		public Token[] GetTokens()
		{
			return lexer.GetTokens();
		}
	}
	
	private readonly Dictionary<string, CodeFile> codeFiles = new();

	public void AddFile(string path)
	{
		if (!File.Exists(path))
			return;

		if (codeFiles.ContainsKey(path))
			return;
		
		var content = File.ReadAllText(path);
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

	public Token[] GetTokens(string path)
	{
		if (!codeFiles.TryGetValue(path, out var codeFile))
			return Array.Empty<Token>();

		return codeFile.GetTokens();
	}
}