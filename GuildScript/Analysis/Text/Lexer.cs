namespace GuildScript.Analysis.Text;

public sealed class Lexer
{
	private bool EndOfFile => currentPosition >= sourceText.Text.Length;
	private SourceText sourceText;
	private readonly List<Token> tokens = new();
	private int currentPosition;
	private int start;

	public Lexer(string sourceText)
	{
		this.sourceText = new SourceText(sourceText);
		LexAll();
	}

	private void LexAll()
	{
		LexSpan(0, sourceText.Text.Length + 1);
	}

	private void LexSpan(int startIndex, int length)
	{
		var tokenIndex = GetTokenIndexAtPosition(startIndex);
		var endTokenIndex = GetTokenIndexAtPosition(startIndex + length);
		tokenIndex = 0;
		endTokenIndex = tokens.Count - 1;
		if (tokenIndex >= 0 && endTokenIndex >= 0)
			tokens.RemoveRange(tokenIndex, endTokenIndex - tokenIndex + 1);
		
		currentPosition = startIndex;
		var newTokens = new List<Token>();

		while (currentPosition < startIndex + length)
		{
			if (EndOfFile)
			{
				newTokens.Add(new Token(TokenType.EndOfFile, new TextSpan(currentPosition, 1, sourceText), "\0", null));
				break;
			}
			
			var character = sourceText.Text[currentPosition];
			start = currentPosition;

			switch (character)
			{
				case ';':
					newTokens.Add(ScanOperator(TokenType.Semicolon));
					continue;
				case '=':
					newTokens.Add(ScanOperator(TokenType.Equals));
					continue;
				default:
					if (char.IsWhiteSpace(character))
					{
						newTokens.Add(ScanWhiteSpace());
						continue;
					}

					if (char.IsLetter(character) || character == '_')
					{
						newTokens.Add(ScanIdentifier());
						continue;
					}

					if (char.IsDigit(character))
					{
						newTokens.Add(ScanNumber());
						continue;
					}

					newTokens.Add(ScanOperator(TokenType.Invalid));
					continue;
			}
		}
		
		if (tokenIndex >= 0)
			tokens.InsertRange(tokenIndex, newTokens);
		else
			tokens.AddRange(newTokens);

		Console.WriteLine(tokenIndex);
	}

	private Token ScanNumber()
	{
		while (!EndOfFile && char.IsDigit(sourceText.Text[currentPosition]))
		{
			currentPosition++;
		}

		var length = currentPosition - start;
		var text = sourceText.Text.Substring(start, length);
		return new Token(TokenType.Int32Literal, new TextSpan(start, length, sourceText),
			text, int.Parse(text));
	}

	private Token ScanWhiteSpace()
	{
		while (!EndOfFile && char.IsWhiteSpace(sourceText.Text[currentPosition]))
		{
			currentPosition++;
		}

		var length = currentPosition - start;
		return new Token(TokenType.WhiteSpace, new TextSpan(start, length, sourceText),
			sourceText.Text.Substring(start, length), null);
	}

	private Token ScanIdentifier()
	{
		while (!EndOfFile && (char.IsLetterOrDigit(sourceText.Text[currentPosition]) || sourceText.Text[currentPosition] == '_'))
		{
			currentPosition++;
		}

		var length = currentPosition - start;
		return new Token(TokenType.Identifier, new TextSpan(start, length, sourceText),
			sourceText.Text.Substring(start, length), null);
	}

	private Token ScanOperator(TokenType type, int length = 1)
	{
		currentPosition += length;
		return new Token(type, new TextSpan(start, length, sourceText), sourceText.Text.Substring(start, length), null);
	}

	public void Lex(string text)
	{
		var newSource = new SourceText(text);
		//var editSequences = sourceText.GetDifference(newSource);
		sourceText = newSource;
		LexAll();
		/*
		Console.WriteLine("--------------------------");
		
		foreach (var editSequence in editSequences)
		{
			var editStart = editSequence.Start;
			var editLength = editSequence.Operation == EditOperationKind.Add ? 0 : editSequence.Length;

			var tokenStartIndex = GetTokenIndexBeforePosition(editStart);
			var tokenEndIndex = GetTokenIndexAfterPosition(editStart + editLength);

			if (tokenStartIndex >= 0 && tokenEndIndex >= 0)
			{
				var tokenStart = tokens[Math.Max(0, tokenStartIndex - 1)];
				var tokenEnd = tokens[Math.Min(tokens.Count - 1, tokenEndIndex + 1)];

				editStart = Math.Min(editStart, tokenStart.Span.Start);
				editLength = Math.Max(editSequence.Start + editSequence.Length, tokenEnd.Span.End) - editStart;
			}
			
			for (var i = tokenEndIndex; i < tokens.Count; i++)
			{
				switch (editSequence.Operation)
				{
					case EditOperationKind.Add:
						tokens[i].Span.Start += editSequence.Length;
						break;
					case EditOperationKind.Remove:
						tokens[i].Span.Start -= editSequence.Length;
						break;
				}
			}
			
			LexSpan(editStart, editLength);
		}
		
		foreach (var token in tokens)
		{
			Console.WriteLine(token.Type + ": " + token.Text + $" ({token.Span.Start}-{token.Span.End})");
		}*/
	}

	private Token? GetTokenAtPosition(int position)
	{
		var index = GetTokenIndexAtPosition(position);
		return index < 0 ? null : tokens[index];
	}

	public int GetTokenIndexAtPosition(int position)
	{
		var left = 0;
		var right = tokens.Count - 1;

		while (left <= right)
		{
			var mid = left + (right - left) / 2;
			var token = tokens[mid];

			if (token.Span.Start <= position && position <= token.Span.End)
				return mid;

			if (token.Span.End < position)
				left = mid + 1;
			else
				right = mid - 1;
		}

		return -1;
	}
	
	private int GetTokenIndexBeforePosition(int position)
	{
		var left = 0;
		var right = tokens.Count - 1;

		while (left <= right)
		{
			var mid = left + (right - left) / 2;
			var token = tokens[mid];

			if (token.Span.End <= position)
			{
				if (mid + 1 >= tokens.Count || tokens[mid + 1].Span.End > position)
					return mid;

				left = mid + 1;
			}
			else
				right = mid - 1;
		}

		return 0;
	}
	
	private int GetTokenIndexAfterPosition(int position)
	{
		var left = 0;
		var right = tokens.Count - 1;

		while (left <= right)
		{
			var mid = left + (right - left) / 2;
			var token = tokens[mid];

			if (token.Span.Start >= position)
			{
				if (mid - 1 < 0 || tokens[mid - 1].Span.Start < position)
					return mid;

				right = mid - 1;
			}
			else
				left = mid + 1;
		}

		return tokens.Count - 1;
	}

	public Token[] GetTokens()
	{
		return tokens.ToArray();
	}
}