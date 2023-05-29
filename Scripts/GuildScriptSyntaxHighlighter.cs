using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;
using GuildScript;
using GuildScript.Analysis.Text;

namespace Tavern;

public partial class GuildScriptSyntaxHighlighter : SyntaxHighlighter
{
	public string Path { get; }
	private readonly LanguageServer languageServer;
	private readonly List<Token> tokens = new();
	
	public GuildScriptSyntaxHighlighter(string path, LanguageServer languageServer)
	{
		Path = path;
		this.languageServer = languageServer;
	}

	public override Dictionary _GetLineSyntaxHighlighting(int line)
	{
		var highlighting = new Dictionary();
		var startToken = tokens.Count;
		var endToken = -1;
		for (var i = 0; i < tokens.Count; i++)
		{
			var token = tokens[i];
			if (!token.Span.ContainsLine(line + 1))
				continue;
			
			startToken = Math.Min(startToken, i);
			endToken = Math.Max(endToken, i);
		}

		if (startToken > endToken)
			return highlighting;

		for (var i = startToken; i <= endToken; i++)
		{
			var token = tokens[i];
			if (token.Type == TokenType.WhiteSpace)
				continue;
			
			var color = token.Type switch
			{
				TokenType.Int32Literal => Colors.Purple,
				TokenType.Invalid => Colors.Red,
				TokenType.Identifier => Colors.PaleGreen,
				_ => Colors.White
			};

			var lineData = new Dictionary();
			lineData.Add("color", color);
			
			highlighting[token.Span.ColumnStart - 1] = lineData;
		}

		GD.Print(highlighting);
		return highlighting;
	}

	public override void _ClearHighlightingCache()
	{
		tokens.Clear();
	}

	public override void _UpdateCache()
	{
		tokens.AddRange(languageServer.GetTokens(Path));
	}
}