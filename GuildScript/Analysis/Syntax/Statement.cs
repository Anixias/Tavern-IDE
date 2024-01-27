using System.Collections.Immutable;
using GuildScript.Analysis.Text;

namespace GuildScript.Analysis.Syntax;

public abstract class Statement : SyntaxNode
{
	public interface IVisitor<out T>
	{
		T Visit(Program program);
		T Visit(Class @class);
		T Visit(Struct @struct);
		T Visit(Interface @interface);
		T Visit(Enum @enum);
	}

	public interface IVisitor
	{
		void Visit(Program program);
		void Visit(Class @class);
		void Visit(Struct @struct);
		void Visit(Interface @interface);
		void Visit(Enum @enum);
	}

	public abstract void Accept(IVisitor visitor);
	public abstract T Accept<T>(IVisitor<T> visitor);

	public sealed class Program : Statement
	{
		public ImmutableArray<ModuleName> ImportedModules { get; }
		public ModuleName Module { get; }
		public ImmutableArray<Statement> Statements { get; }

		public Program(IEnumerable<ModuleName> importedModules, ModuleName module, IEnumerable<Statement> statements)
		{
			ImportedModules = importedModules.ToImmutableArray();
			Module = module;
			Statements = statements.ToImmutableArray();
		}
		
		public override void Accept(IVisitor visitor)
		{
			visitor.Visit(this);
		}

		public override T Accept<T>(IVisitor<T> visitor)
		{
			return visitor.Visit(this);
		}
	}

	public sealed class Class : Statement
	{
		public Token? AccessModifier { get; }
		public Token? ClassModifier { get; }
		public Token Identifier { get; }
		//public ImmutableArray<Token> TypeParameters { get; }
		//public TypeSyntax? BaseClass { get; }
		public ImmutableArray<Statement> Members { get; }
		
		public Class(Token? accessModifier, Token? classModifier, Token identifier, IEnumerable<Statement> members)
		{
			AccessModifier = accessModifier;
			ClassModifier = classModifier;
			Identifier = identifier;
			Members = members.ToImmutableArray();
		}
		
		public override void Accept(IVisitor visitor)
		{
			visitor.Visit(this);
		}

		public override T Accept<T>(IVisitor<T> visitor)
		{
			return visitor.Visit(this);
		}
	}
	
	public sealed class Struct : Statement
	{
		public Token? AccessModifier { get; }
		public Token? StructModifier { get; }
		public Token Identifier { get; }
		public ImmutableArray<Statement> Members { get; }
		//public ImmutableArray<Token> TypeParameters { get; }

		public Struct(Token? accessModifier, Token? structModifier, Token identifier,
					  /*IEnumerable<Token> typeParameters,*/ IEnumerable<Statement> members)
		{
			AccessModifier = accessModifier;
			StructModifier = structModifier;
			Identifier = identifier;
			Members = members.ToImmutableArray();
			//TypeParameters = typeParameters.ToImmutableArray();
		}
		
		public override void Accept(IVisitor visitor)
		{
			visitor.Visit(this);
		}

		public override T Accept<T>(IVisitor<T> visitor)
		{
			return visitor.Visit(this);
		}
	}
	
	public sealed class Interface : Statement
	{
		public Token? AccessModifier { get; }
		public Token Identifier { get; }
		public ImmutableArray<Statement> Members { get; }
		//public ImmutableArray<SyntaxToken> TypeParameters { get; }

		public Interface(Token? accessModifier, Token identifier, /*IEnumerable<Token> typeParameters,*/
						 IEnumerable<Statement> members)
		{
			AccessModifier = accessModifier;
			Identifier = identifier;
			Members = members.ToImmutableArray();
			//TypeParameters = typeParameters.ToImmutableArray();
		}
		
		public override void Accept(IVisitor visitor)
		{
			visitor.Visit(this);
		}

		public override T Accept<T>(IVisitor<T> visitor)
		{
			return visitor.Visit(this);
		}
	}
	
	public sealed class Enum : Statement
	{
		public class Member
		{
			public Token Identifier { get; }
			//public Expression? Expression { get; }

			public Member(Token identifier/*, Expression? expression*/)
			{
				Identifier = identifier;
				//Expression = expression;
			}
		}
		
		public Token? AccessModifier { get; }
		public Token Identifier { get; }
		public ImmutableArray<Member> Members { get; }
		//public TypeSyntax Type { get; }

		public Enum(Token? accessModifier, Token identifier, IEnumerable<Member> members/*, TypeSyntax type*/)
		{
			AccessModifier = accessModifier;
			Identifier = identifier;
			//Type = type;
			Members = members.ToImmutableArray();
		}
		
		public override void Accept(IVisitor visitor)
		{
			visitor.Visit(this);
		}

		public override T Accept<T>(IVisitor<T> visitor)
		{
			return visitor.Visit(this);
		}
	}
}