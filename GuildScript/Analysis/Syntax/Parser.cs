using System.Diagnostics.CodeAnalysis;
using GuildScript.Analysis.Text;

namespace GuildScript.Analysis.Syntax;

public sealed class Parser
{
	private Token? Next => Peek();
	private bool EndOfFile => Next is null;
	
	private readonly Lexer lexer;
	private readonly List<Token> tokens = new();
	private int position;
	private bool suppressErrors = false;

	public Parser(Lexer lexer)
	{
		this.lexer = lexer;
	}
	
	public class ParseException : Exception
	{
		public ParseException(string message) : base(message)
		{
		}
	}
	
	public ParseException Error(string message)
	{
		var errorToken = Peek();
		if (errorToken is not null)
		{
			errorToken.IsError = true;
			errorToken.ErrorMessage = message;
		}
		else if (tokens.Count > 0)
		{
			var lastToken = tokens[^1];
			lastToken.IsError = true;
			lastToken.ErrorMessage = message;
		}

		var tokenDescription = errorToken is null ? "end" : $"'{errorToken.Text}'";
		var errorMessage = $"Error at {tokenDescription}: {message}";

		if (suppressErrors)
			return new ParseException(errorMessage);
		
		//Diagnostics.ReportParserError(errorToken.Span, errorMessage);
		//Console.ForegroundColor = ConsoleColor.DarkRed;
		//Console.WriteLine(errorMessage);
		//Console.ResetColor();

		return new ParseException(errorMessage);
	}

	public SyntaxNode? Parse()
	{
		tokens.Clear();
		tokens.AddRange(lexer.GetTokens());
		tokens.RemoveAll(token => token.Type is TokenType.WhiteSpace or TokenType.Comment);
		
		position = 0;

		try
		{
			return ParseProgram();
		}
		catch (ParseException)
		{
			return null;
		}
	}
	
	private Token? Advance()
	{
		var token = Next;

		if (position < tokens.Count)
			position++;
		
		return token;
	}

	private Token Consume(params TokenType?[] types)
	{
		foreach (var type in types)
		{
			if (Match(out var token, type))
				return token;
		}
		
		if (types.Length == 1)
			throw Error($"Expected '{types[0]}'.");
		
		throw Error($"Expected one of '{string.Join("', '", types)}'.");
	}
	
	private bool Check(params TokenType?[] types)
	{
		foreach (var type in types)
		{
			if (Next?.Type != type)
				continue;
			
			return true;
		}

		return false;
	}

	private bool Match(params TokenType?[] types)
	{
		if (!Check(types))
			return false;
		
		Advance();
		return true;
	}

	private bool Match([NotNullWhen(true)] out Token? token, params TokenType?[] types)
	{
		token = null;
		
		foreach (var type in types)
		{
			if (Next?.Type != type)
				continue;
			
			token = Advance();
			return token is not null;
		}

		return false;
	}

	private Token? Peek(int offset = 0)
	{
		var index = position + offset;
		if (index < 0 || index >= tokens.Count)
			return null;

		return tokens[index];
	}

	private void Recover()
	{
		while (!EndOfFile && !Match(TokenType.CloseBrace, TokenType.Semicolon))
		{
			Advance();
		}
	}

	private Statement.Program ParseProgram()
	{
		// <import-statement>*
		var importStatements = new List<ModuleName>();
		while (Match(TokenType.Import))
		{
			var importModuleName = ParseModule();
			importStatements.Add(importModuleName);

			Consume(TokenType.Semicolon);
		}

		// <module-statement>
		Consume(TokenType.Module);
		var module = ParseModule();
		Consume(TokenType.Semicolon);

		// <top-level-statement>*
		var statements = new List<Statement>();
		while (!EndOfFile)
		{
			try
			{
				statements.Add(ParseTopLevelStatement());
			}
			catch (ParseException)
			{
				Recover();
			}
		}

		if (Next is not null)
			throw Error("Expected end of file.");

		return new Statement.Program(importStatements.ToArray(), module, statements.ToArray());
	}
	
	private ModuleName ParseModule()
	{
		var identifiers = new List<string>();
		var moduleTokens = new List<Token>();

		var identifierToken = Consume(TokenType.Identifier);
		identifierToken.Category = "module";
		moduleTokens.Add(identifierToken);
		identifiers.Add(identifierToken.Text);

		while (Match(out var dotToken, TokenType.Dot))
		{
			identifierToken = Consume(TokenType.Identifier);
			identifierToken.Category = "module";
			moduleTokens.Add(dotToken);
			moduleTokens.Add(identifierToken);
			identifiers.Add(identifierToken.Text);
		}

		return new ModuleName(identifiers, new TokenGroup(moduleTokens));
	}
	
	private Statement ParseTopLevelStatement()
	{
		return Next?.Type switch
		{
			//TokenType.Entry => ParseEntryPoint(),
			//TokenType.Module => ParseModuleDeclaration(),
			//TokenType.Define => ParseDefineStatement(),
			_                => ParseTypeDeclaration()
		};
	}
	
	private Statement ParseTypeDeclaration(Token? accessModifier)
	{
		if (Match(TokenType.Interface))
		{
			return ParseInterface(accessModifier);
		}
		
		if (Match(TokenType.Enum))
		{
			return ParseEnum(accessModifier);
		}

		if (Check(TokenType.Struct) || Peek(1)?.Type == TokenType.Struct)
		{
			return ParseStruct(accessModifier);
		}

		if (Check(TokenType.Class) || Peek(1)?.Type == TokenType.Class)
		{
			return ParseClass(accessModifier);
		}

		throw Error("Expected type declaration.");
	}

	private Statement ParseTypeDeclaration()
	{
		var accessModifier = ParseAccessModifier();
		return ParseTypeDeclaration(accessModifier);
	}

	private Token? ParseAccessModifier()
	{
		return Match(out var token, TokenType.Public, TokenType.Private, TokenType.Protected, TokenType.Internal)
			? token
			: null;
	}
	
	private Statement.Class ParseClass(Token? accessModifier)
	{
		var classModifier =
			Match(out var token, TokenType.Global, TokenType.Template, TokenType.Final)
				? token
				: null;
		
		Consume(TokenType.Class);
		var identifier = Consume(TokenType.Identifier);
		identifier.Category = "class";

		/*var typeParameters = ParseTypeParameters();

		TypeSyntax? baseType = null;
		if (Match(SyntaxTokenType.Colon))
		{
			baseType = ParseType();
		}*/
		
		Consume(TokenType.OpenBrace);

		var members = new List<Statement>();
		while (!EndOfFile && !Check(TokenType.CloseBrace))
		{
			members.Add(ParseMember());
		}
		
		Consume(TokenType.CloseBrace);

		return new Statement.Class(accessModifier, classModifier, identifier, /*typeParameters, baseType,*/ members);
	}
	
	private Statement.Struct ParseStruct(Token? accessModifier)
	{
		var structModifier = Match(out var token, TokenType.Immutable) ? token : null;
		Consume(TokenType.Struct);
		var identifier = Consume(TokenType.Identifier);
		identifier.Category = "struct";
		//var typeParameters = ParseTypeParameters();
		
		Consume(TokenType.OpenBrace);

		var members = new List<Statement>();
		while (!EndOfFile && !Check(TokenType.CloseBrace))
		{
			members.Add(ParseMember());
		}
		
		Consume(TokenType.CloseBrace);

		return new Statement.Struct(accessModifier, structModifier, identifier, /*typeParameters,*/ members);
	}
	
	private Statement.Interface ParseInterface(Token? accessModifier)
	{
		var identifier = Consume(TokenType.Identifier);
		identifier.Category = "interface";
		//var typeParameters = ParseTypeParameters();
		
		Consume(TokenType.OpenBrace);

		var members = new List<Statement>();
		while (!EndOfFile && !Check(TokenType.CloseBrace))
		{
			//members.Add(ParseInterfaceMember());
			Advance();
		}
		
		Consume(TokenType.CloseBrace);

		return new Statement.Interface(accessModifier, identifier, /*typeParameters, */members);
	}
	
	private Statement.Enum ParseEnum(Token? accessModifier)
	{
		//var type = ParseNamedType();
		var identifier = Consume(TokenType.Identifier);
		identifier.Category = "enum";
		Consume(TokenType.OpenBrace);

		var members = new List<Statement.Enum.Member>();
		
		while (!EndOfFile && !Check(TokenType.CloseBrace))
		{
			var memberIdentifier = Consume(TokenType.Identifier);
			/*Expression? memberExpression = null;

			if (Match(SyntaxTokenType.Equal))
			{
				memberExpression = ParseExpression();
			}*/
			
			members.Add(new Statement.Enum.Member(memberIdentifier/*, memberExpression*/));

			if (!Match(TokenType.Comma))
				break;
		}
		
		Consume(TokenType.CloseBrace);

		return new Statement.Enum(accessModifier, identifier, members/*, type*/);
	}
	
	private TypeSyntax? ParseType()
	{
		if (Match(TokenType.Void))
			return null;
		
		/*if (Check(TokenType.OpenSquare))
		{
			return ParseLambdaType();
		}*/

		var type = ParseNamedType();
		while (!EndOfFile)
		{
			if (Match(TokenType.Question))
			{
				type = new NullableTypeSyntax(type);
			}
			else if (Match(TokenType.OpenSquare))
			{
				type = new ArrayTypeSyntax(type);
				Consume(TokenType.CloseSquare);
			}
			else if (Match(TokenType.QuestionOpenSquare))
			{
				type = new ArrayTypeSyntax(new NullableTypeSyntax(type));
				Consume(TokenType.CloseSquare);
			}
			else if (type is NamedTypeSyntax namedTypeSyntax && Match(TokenType.LeftAngled))
			{
				var templateTypes = new List<TypeSyntax>();

				do
				{
					var newType = ParseType();
					if (newType is null)
						throw Error("Cannot template void.");
					
					templateTypes.Add(newType);
				} while (Match(TokenType.Comma));
				
				type = new TemplatedTypeSyntax(namedTypeSyntax, templateTypes);
				Consume(TokenType.RightAngled);
			}
			else
			{
				break;
			}
		}

		return type;
	}
	
	private TypeSyntax ParseNamedType()
	{
		TypeSyntax? type = Next?.Type switch
		{
			TokenType.Int8   => TypeSyntax.Int8,
			TokenType.UInt8  => TypeSyntax.UInt8,
			TokenType.Int16  => TypeSyntax.Int16,
			TokenType.UInt16 => TypeSyntax.UInt16,
			TokenType.Int32  => TypeSyntax.Int32,
			TokenType.UInt32 => TypeSyntax.UInt32,
			TokenType.Int64  => TypeSyntax.Int64,
			TokenType.UInt64 => TypeSyntax.UInt64,
			TokenType.String => TypeSyntax.String,
			TokenType.Single => TypeSyntax.Single,
			TokenType.Double => TypeSyntax.Double,
			TokenType.Bool   => TypeSyntax.Bool,
			TokenType.Char   => TypeSyntax.Char,
			TokenType.Object => TypeSyntax.Object,
			_                => null
		};

		//if (type is null)
		//	return new ExpressionTypeSyntax(ParseTypeExpression());
		
		Advance();
		return type;

	}
	
	private Statement ParseMember()
	{
		/*if (Match(out var castTypeToken, TokenType.Implicit, TokenType.Explicit))
		{
			return ParseCastOverload(castTypeToken);
		}*/
		
		/*if (Match(out var destructorToken, TokenType.Destructor))
		{
			return ParseDestructor(destructorToken);
		}*/

		/*if (Match(TokenType.External))
		{
			return ParseExternalMethod();
		}

		if (IsOperatorOverload())
		{
			return ParseOperatorOverload();
		}*/
		
		var accessModifier = ParseAccessModifier();

		/*if (Match(out var constructorToken, TokenType.Constructor))
		{
			return ParseConstructor(constructorToken, accessModifier);
		}*/

		var peek = 0;
		var continuePeek = true;
		
		while (continuePeek)
		{
			var token = Peek(peek++);
			switch (token?.Type)
			{
				/*case TokenType.This:
					return ParseIndexer(accessModifier);
				case TokenType.Event:
					return ParseEvent(accessModifier);
				case TokenType.OpenBrace:
					return ParseProperty(accessModifier);
				case TokenType.OpenParen:
					return ParseMethod(accessModifier);
				case TokenType.Semicolon:
					return ParseField(accessModifier);*/
				case TokenType.Class:
				case TokenType.Struct:
				case TokenType.Interface:
				case TokenType.Enum:
					return ParseTypeDeclaration(accessModifier);
				case TokenType.EndOfFile:
					continuePeek = false;
					break;
				default:
					continue;
			}
		}

		throw Error("Expected member.");
	}
}