using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;
using GuildScript;
using GuildScript.Analysis.Text;

namespace Tavern;

public partial class GuildScriptSyntaxHighlighter : SyntaxHighlighter, IRichSyntaxHighlighter
{
	public static readonly System.Collections.Generic.Dictionary<string, Color> ColorDefinitions = new()
	{
		{ "comment", Color.Color8(98, 114, 164) },
		{ "keyword", Color.Color8(255, 121, 198) },
		{ "number", Color.Color8(189, 147, 249) },
		{ "string", Color.Color8(241, 250, 140) },
		{ "parameter", Color.Color8(255, 184, 108) },
		{ "method", Color.Color8(80, 250, 123) },
		{ "class", Color.Color8(139, 233, 253) },
		{ "struct", Color.Color8(241, 250, 140) },
		{ "interface", Color.Color8(189, 147, 249) },
		{ "enum", Color.Color8(255, 85, 85) },
		{ "local", Color.Color8(255, 255, 255) }, // @TODO
	};

	public static readonly Color[] BraceColors =
	{
		Color.Color8(255, 121, 198),
		Color.Color8(241, 250, 140),
		Color.Color8(80, 250, 123),
		Color.Color8(139, 233, 253),
		Color.Color8(189, 147, 249)
	};

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
			
			Color color;
			switch (token.Type)
			{
				case TokenType.Invalid:
					color = Colors.Red;
					break;
				case TokenType.Comment:
					color = ColorDefinitions["comment"];
					break;
				case TokenType.True:
				case TokenType.False:
				case TokenType.Null:
				case TokenType.Define:
				case TokenType.As:
				case TokenType.Var:
				case TokenType.Async:
				case TokenType.Await:
				case TokenType.For:
				case TokenType.Foreach:
				case TokenType.In:
				case TokenType.Public:
				case TokenType.Private:
				case TokenType.Protected:
				case TokenType.Internal:
				case TokenType.External:
				case TokenType.Class:
				case TokenType.Struct:
				case TokenType.Interface:
				case TokenType.Enum:
				case TokenType.This:
				case TokenType.Base:
				case TokenType.Global:
				case TokenType.Template:
				case TokenType.Lock:
				case TokenType.Import:
				case TokenType.Export:
				case TokenType.Implicit:
				case TokenType.Explicit:
				case TokenType.Module:
				case TokenType.Entry:
				case TokenType.Final:
				case TokenType.Shared:
				case TokenType.Required:
				case TokenType.Prototype:
				case TokenType.Return:
				case TokenType.Void:
				case TokenType.Constructor:
				case TokenType.Destructor:
				case TokenType.Seal:
				case TokenType.Constant:
				case TokenType.Fixed:
				case TokenType.Immutable:
				case TokenType.Throw:
				case TokenType.Try:
				case TokenType.Catch:
				case TokenType.Finally:
				case TokenType.If:
				case TokenType.Else:
				case TokenType.While:
				case TokenType.Do:
				case TokenType.Repeat:
				case TokenType.New:
				case TokenType.Switch:
				case TokenType.Case:
				case TokenType.Default:
				case TokenType.Continue:
				case TokenType.Break:
				case TokenType.Get:
				case TokenType.Set:
				case TokenType.Event:
				case TokenType.Ref:
				case TokenType.Int8:
				case TokenType.Int16:
				case TokenType.Int32:
				case TokenType.Int64:
				case TokenType.UInt8:
				case TokenType.UInt16:
				case TokenType.UInt32:
				case TokenType.UInt64:
				case TokenType.Single:
				case TokenType.Double:
				case TokenType.Bool:
				case TokenType.String:
				case TokenType.Char:
				case TokenType.Object:
					color = ColorDefinitions["keyword"];
					break;
				case TokenType.Int8Constant:
				case TokenType.Int16Constant:
				case TokenType.Int32Constant:
				case TokenType.Int64Constant:
				case TokenType.UInt8Constant:
				case TokenType.UInt16Constant:
				case TokenType.UInt32Constant:
				case TokenType.UInt64Constant:
				case TokenType.SingleConstant:
				case TokenType.DoubleConstant:
					color = ColorDefinitions["number"];
					break;
				case TokenType.StringConstant:
				case TokenType.CharacterConstant:
					color = ColorDefinitions["string"];
					break;
				case TokenType.Identifier:
				default:
					color = Colors.White;
					break;
			}

			var lineData = new Dictionary();
			lineData.Add("color", color);
			
			highlighting[token.Span.StartColumnForLine(line + 1) - 1] = lineData;
		}

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

	public IEnumerable<TextSpan> GetErrors()
	{
		yield break;
	}
}