using System.Collections.Immutable;
using GuildScript.Analysis.Text;

namespace GuildScript.Analysis.Syntax;

public abstract class TypeSyntax
{
	public abstract bool IsNullable { get; }

	public static readonly BaseTypeSyntax Int8 = new(TokenType.Int8);
	public static readonly BaseTypeSyntax UInt8 = new(TokenType.UInt8);
	public static readonly BaseTypeSyntax Int16 = new(TokenType.Int16);
	public static readonly BaseTypeSyntax UInt16 = new(TokenType.UInt16);
	public static readonly BaseTypeSyntax Int32 = new(TokenType.Int32);
	public static readonly BaseTypeSyntax UInt32 = new(TokenType.UInt32);
	public static readonly BaseTypeSyntax Int64 = new(TokenType.Int64);
	public static readonly BaseTypeSyntax UInt64 = new(TokenType.UInt64);
	public static readonly BaseTypeSyntax Single = new(TokenType.Single);
	public static readonly BaseTypeSyntax Double = new(TokenType.Double);
	public static readonly BaseTypeSyntax Char = new(TokenType.Char);
	public static readonly BaseTypeSyntax Bool = new(TokenType.Bool);
	public static readonly BaseTypeSyntax Object = new(TokenType.Object);
	public static readonly BaseTypeSyntax String = new(TokenType.String);
	public static readonly BaseTypeSyntax Inferred = new(TokenType.Var);
	
	public override string ToString()
	{
		return "(??)";
	}

	public override bool Equals(object? obj)
	{
		if (obj is TypeSyntax other)
			return Equals(other);

		return false;
	}

	private bool Equals(TypeSyntax other)
	{
		if (IsNullable != other.IsNullable)
			return false;

		return GetType() == other.GetType() && GetHashCode() == other.GetHashCode();
	}

	public override int GetHashCode()
	{
		return 0;
	}
}

public class NullableTypeSyntax : TypeSyntax
{
	public override bool IsNullable => true;

	public TypeSyntax BaseType { get; }

	public NullableTypeSyntax(TypeSyntax baseType)
	{
		BaseType = baseType;
	}
	
	public override string ToString()
	{
		return $"{BaseType}?";
	}
	
	public override int GetHashCode()
	{
		var hash = 0;
		hash ^= IsNullable.GetHashCode();
		hash ^= BaseType.GetHashCode() << 2;
		return hash;
	}
}

public class ArrayTypeSyntax : TypeSyntax
{
	public override bool IsNullable => false;

	public TypeSyntax BaseType { get; }

	public ArrayTypeSyntax(TypeSyntax baseType)
	{
		BaseType = baseType;
	}
	
	public override string ToString()
	{
		return $"{BaseType}[]";
	}
	
	public override int GetHashCode()
	{
		var hash = 0;
		hash ^= IsNullable.GetHashCode();
		hash ^= BaseType.GetHashCode() << 1;
		return hash;
	}
}

public class TemplatedTypeSyntax : TypeSyntax
{
	public override bool IsNullable => false;

	public NamedTypeSyntax BaseType { get; }
	public IReadOnlyList<TypeSyntax> TypeArguments { get; }

	public TemplatedTypeSyntax(NamedTypeSyntax baseType, IEnumerable<TypeSyntax> typeArguments)
	{
		BaseType = baseType;
		TypeArguments = typeArguments.ToImmutableArray();
	}
	
	public override string ToString()
	{
		var typeArgs = TypeArguments.Count > 0 ? $"<{string.Join(", ", TypeArguments)}>" : "";
		return $"{BaseType}{typeArgs}";
	}
	
	public override int GetHashCode()
	{
		var hash = 0;
		hash ^= IsNullable.GetHashCode();
		hash ^= BaseType.GetHashCode() << 1;
		hash ^= TypeArguments.GetHashCode() << 2;
		return hash;
	}
}

public class BaseTypeSyntax : TypeSyntax
{
	public override bool IsNullable => false;

	public TokenType TokenType { get; }

	public BaseTypeSyntax(TokenType tokenType)
	{
		TokenType = tokenType;
	}
	
	public override string ToString()
	{
		return TokenType.ToString();
	}
	
	public override int GetHashCode()
	{
		var hash = 0;
		hash ^= IsNullable.GetHashCode();
		hash ^= TokenType.GetHashCode() << 1;
		return hash;
	}
}

public class NamedTypeSyntax : TypeSyntax
{
	public override bool IsNullable => false;

	public string Name { get; }

	public NamedTypeSyntax(string name)
	{
		Name = name;
	}
	
	public override string ToString()
	{
		return Name;
	}
	
	public override int GetHashCode()
	{
		var hash = 0;
		hash ^= IsNullable.GetHashCode();
		hash ^= Name.GetHashCode() << 1;
		return hash;
	}
}

/*public class ExpressionTypeSyntax : TypeSyntax
{
	public override bool IsNullable => false;

	public Expression Expression { get; }

	public ExpressionTypeSyntax(Expression expression)
	{
		Expression = expression;
	}
	
	public override string ToString()
	{
		return Expression.ToString() ?? "(Expression)";
	}
	
	public override int GetHashCode()
	{
		var hash = 0;
		hash ^= IsNullable.GetHashCode();
		hash ^= Expression.GetHashCode() << 1;
		return hash;
	}
}

public class LambdaTypeSyntax : TypeSyntax
{
	public override bool IsNullable => false;

	public TextSpan Span { get; }
	public IReadOnlyList<TypeSyntax> InputTypes { get; }
	public TypeSyntax? OutputType { get; }

	public LambdaTypeSyntax(IEnumerable<TypeSyntax> inputTypes, TextSpan span)
	{
		Span = span;
		InputTypes = inputTypes.ToImmutableArray();
		OutputType = null;
	}

	public LambdaTypeSyntax(IEnumerable<TypeSyntax> inputTypes, TypeSyntax? outputType, TextSpan span)
	{
		InputTypes = inputTypes.ToImmutableArray();
		OutputType = outputType;
		Span = span;
	}
	
	public override string ToString()
	{
		var inputTypesStr = string.Join(", ", InputTypes);
		return OutputType is null ? $"[{inputTypesStr}] |> {{}}" : $"[{inputTypesStr}] <| [{OutputType}] {{}}";
	}
	
	public override int GetHashCode()
	{
		var hash = 0;
		hash ^= IsNullable.GetHashCode();
		hash ^= InputTypes.GetHashCode() << 1;
		hash ^= OutputType?.GetHashCode() << 2 ?? 0;
		return hash;
	}
}*/