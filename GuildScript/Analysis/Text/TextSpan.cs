using System.Collections;

namespace GuildScript.Analysis.Text;

internal sealed class TextSpan : IEnumerable<int>
{
	public int Start { get; }
	public int Length { get; }
	public int End => Start + Length;
	public SourceText Source { get; }

	public bool Contains(int position)
	{
		return Start <= position && position <= End;
	}

	public TextSpan(int start, int length, SourceText source)
	{
		Start = start;
		Length = length;
		Source = source;
	}

	public IEnumerator<int> GetEnumerator()
	{
		for (var i = Start; i <= End; i++)
		{
			yield return i;
		}
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}