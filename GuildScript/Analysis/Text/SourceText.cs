using System;
using System.Collections.Generic;

namespace GuildScript.Analysis.Text;

public sealed class SourceText
{
	public string Text { get; }
	public string FilePath { get; }
	public IReadOnlyList<string> Lines { get; }
	public IReadOnlyList<int> LineStartPositions { get; }
	
	public SourceText(string text, string filePath)
	{
		Text = text;
		FilePath = filePath;
		Lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

		var lineStartPositions = new List<int>();
		var position = 0;

		foreach (var line in Lines)
		{
			lineStartPositions.Add(position);
			position += line.Length + 1;
		}

		LineStartPositions = lineStartPositions;
	}

	public (int line, int column) GetLineAndColumn(int position)
	{
		if (position < 0 || position > Text.Length)
			throw new ArgumentOutOfRangeException(nameof(position));

		var line = GetLineFromPosition(position);
		var column = position - LineStartPositions[line];

		return (line + 1, column + 1);
	}

	public int GetLineFromPosition(int position)
	{
		var left = 0;
		var right = LineStartPositions.Count - 1;

		while (left <= right)
		{
			var mid = left + (right - left) / 2;
			var lineStart = LineStartPositions[mid];

			if (position < lineStart)
			{
				right = mid - 1;
			}
			else if (position >= lineStart &&
					 (mid == LineStartPositions.Count - 1 || position < LineStartPositions[mid + 1]))
			{
				return mid;
			}
			else
			{
				left = mid + 1;
			}
		}

		return -1;
	}
}