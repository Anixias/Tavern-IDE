using System.Collections.Immutable;

namespace GuildScript.Analysis.Text;

public sealed class TokenGroup
{
	public ImmutableArray<Token> Tokens { get; }
	
	public TokenGroup(IEnumerable<Token> tokens)
	{
		Tokens = tokens.ToImmutableArray();
	}
}