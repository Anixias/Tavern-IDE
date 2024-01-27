using System.Text;

namespace GuildScript.Analysis.Text;

public sealed class Lexer
{
	private bool EndOfFile => currentPosition >= sourceText.Text.Length;
	private SourceText sourceText;
	private readonly List<Token> tokens = new();
	private int currentPosition;
	private int start;

	private char Current => Peek();
	private char Next => Peek(1);

	public Lexer(string sourceText)
	{
		this.sourceText = new SourceText(sourceText);
		LexAll();
	}

	private void LexAll()
	{
		LexSpan(0, sourceText.Text.Length);
	}

	private void LexSpan(int startIndex, int length)
	{
		var tokenIndex = GetTokenIndexAtPosition(startIndex);
		var endTokenIndex = GetTokenIndexAtPosition(startIndex + length);
		
		if (tokenIndex >= 0 && endTokenIndex >= 0)
			tokens.RemoveRange(tokenIndex, endTokenIndex - tokenIndex + 1);
		else
		{
			tokenIndex = 0;
			tokens.Clear();
		}
		
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

			if (ScanComment() is { } commentToken)
			{
				newTokens.Add(commentToken);
				continue;
			}

			switch (character)
			{
				case '+':
	                switch (Next)
	                {
	                    case '+':
	                        newTokens.Add(ScanOperator(TokenType.PlusPlus, 2));
	                        break;
						case '=':
							newTokens.Add(ScanOperator(TokenType.PlusEqual, 2));
							break;
	                    default:
							newTokens.Add(ScanOperator(TokenType.Plus));
	                        break;
	                }
	                break;
	            case '-':
	                switch (Next)
	                {
	                    case '-':
							newTokens.Add(ScanOperator(TokenType.MinusMinus, 2));
	                        break;
	                    case '=':
							newTokens.Add(ScanOperator(TokenType.MinusEqual, 2));
	                        break;
	                    case '>':
	                        switch (Peek(2))
	                        {
	                            case '>':
									newTokens.Add(ScanOperator(TokenType.RightArrowArrow, 3));
	                                break;
	                            default:
									newTokens.Add(ScanOperator(TokenType.RightArrow, 2));
	                                break;
	                        }
	                        break;
	                    default:
							newTokens.Add(ScanOperator(TokenType.Minus));
	                        break;
	                }
	                break;
	            case '*':
	                switch (Next)
	                {
	                    case '=':
							newTokens.Add(ScanOperator(TokenType.StarEqual, 2));
	                        break;
						case '*':
							switch (Peek(2))
							{
								case '=':
									newTokens.Add(ScanOperator(TokenType.StarStarEqual, 3));
									break;
								default:
									newTokens.Add(ScanOperator(TokenType.StarStar, 2));
									break;
							}
							break;
	                    default:
							newTokens.Add(ScanOperator(TokenType.Star));
	                        break;
	                }
	                break;
	            case '/':
	                switch (Next)
	                {
	                    case '=':
							newTokens.Add(ScanOperator(TokenType.SlashEqual, 2));
	                        break;
	                    default:
							newTokens.Add(ScanOperator(TokenType.Slash));
	                        break;
	                }
	                break;
	            case '^':
	                switch (Next)
	                {
	                    case '=':
							newTokens.Add(ScanOperator(TokenType.CaretEqual, 2));
	                        break;
						case '^':
							switch (Peek(2))
							{
								case '=':
									newTokens.Add(ScanOperator(TokenType.CaretCaretEqual, 3));
									break;
								default:
									newTokens.Add(ScanOperator(TokenType.CaretCaret, 2));
									break;
							}
							break;
	                    default:
							newTokens.Add(ScanOperator(TokenType.Caret));
	                        break;
	                }
	                break;
	            case '&':
	                switch (Next)
	                {
	                    case '=':
	                        newTokens.Add(ScanOperator(TokenType.AmpEqual, 2));
	                        break;
						case '&':
							switch (Peek(2))
							{
								case '=':
									newTokens.Add(ScanOperator(TokenType.AmpAmpEqual, 3));
									break;
								default:
									newTokens.Add(ScanOperator(TokenType.AmpAmp, 2));
									break;
							}
							break;
	                    default:
	                        newTokens.Add(ScanOperator(TokenType.Amp));
	                        break;
	                }
	                break;
	            case '%':
	                switch (Next)
	                {
	                    case '=':
							newTokens.Add(ScanOperator(TokenType.PercentEqual, 2));
	                        break;
	                    default:
	                        newTokens.Add(ScanOperator(TokenType.Percent));
	                        break;
	                }
	                break;
	            case '(':
	                newTokens.Add(ScanOperator(TokenType.OpenParen));
	                break;
	            case ')':
	                newTokens.Add(ScanOperator(TokenType.CloseParen));
	                break;
	            case '{':
	                newTokens.Add(ScanOperator(TokenType.OpenBrace));
	                break;
	            case '}':
	                newTokens.Add(ScanOperator(TokenType.CloseBrace));
	                break;
	            case '[':
	                newTokens.Add(ScanOperator(TokenType.OpenSquare));
	                break;
	            case ']':
	                newTokens.Add(ScanOperator(TokenType.CloseSquare));
	                break;
	            case ',':
	                newTokens.Add(ScanOperator(TokenType.Comma));
	                break;
	            case '.':
	                newTokens.Add(ScanOperator(TokenType.Dot));
	                break;
	            case ';':
	                newTokens.Add(ScanOperator(TokenType.Semicolon));
	                break;
				case ':':
					newTokens.Add(ScanOperator(TokenType.Colon));
					break;
				case '~':
					newTokens.Add(ScanOperator(TokenType.Tilde));
					break;
	            case '<':
	                switch (Next)
	                {
	                    case '|':
	                        newTokens.Add(ScanOperator(TokenType.LeftTriangle, 2));
	                        break;
	                    case '=':
	                        newTokens.Add(ScanOperator(TokenType.LeftAngledEqual, 2));
	                        break;
	                    case '-':
	                        newTokens.Add(ScanOperator(TokenType.LeftArrow, 2));
	                        break;
	                    case '<':
	                        switch (Peek(2))
	                        {
	                            case '-':
	                                newTokens.Add(ScanOperator(TokenType.LeftArrowArrow, 3));
	                                break;
								case '=':
									newTokens.Add(ScanOperator(TokenType.LeftLeftEqual, 3));
									break;
								case '<':
									switch (Peek(3))
									{
										case '=':
											newTokens.Add(ScanOperator(TokenType.LeftLeftLeftEqual, 4));
											break;
										default:
											newTokens.Add(ScanOperator(TokenType.LeftAngled));
											break;
									}
									break;
	                            default:
	                                newTokens.Add(ScanOperator(TokenType.LeftAngled));
	                                break;
	                        }
	                        break;
	                    default:
	                        newTokens.Add(ScanOperator(TokenType.LeftAngled));
	                        break;
	                }
	                break;
	            case '>':
	                switch (Next)
	                {
						case '>':
							switch (Peek(2))
							{
								case '=':
									newTokens.Add(ScanOperator(TokenType.RightRightEqual, 3));
									break;
								case '>':
									switch (Peek(3))
									{
										case '=':
											newTokens.Add(ScanOperator(TokenType.RightRightRightEqual, 4));
											break;
										default:
											newTokens.Add(ScanOperator(TokenType.RightAngled));
											break;
									}
									break;
								default:
									newTokens.Add(ScanOperator(TokenType.RightAngled));
									break;
							}
							break;
	                    case '=':
	                        newTokens.Add(ScanOperator(TokenType.RightAngledEqual, 2));
	                        break;
	                    default:
	                        newTokens.Add(ScanOperator(TokenType.RightAngled));
	                        break;
	                }
	                break;
	            case '!':
	                switch (Next)
	                {
	                    case '=':
	                        newTokens.Add(ScanOperator(TokenType.BangEqual, 2));
	                        break;
						case '.':
							newTokens.Add(ScanOperator(TokenType.BangDot, 2));
							break;
	                    default:
	                        newTokens.Add(ScanOperator(TokenType.Bang));
	                        break;
	                }
	                break;
	            case '=':
	                switch (Next)
	                {
	                    case '=':
	                        newTokens.Add(ScanOperator(TokenType.EqualEqual, 2));
	                        break;
	                    default:
	                        newTokens.Add(ScanOperator(TokenType.Equal));
	                        break;
	                }
	                break;
	            case '|':
	                switch (Next)
	                {
	                    case '>':
	                        newTokens.Add(ScanOperator(TokenType.RightTriangle, 2));
	                        break;
						case '=':
							newTokens.Add(ScanOperator(TokenType.PipeEqual, 2));
							break;
						case '|':
							switch (Peek(2))
							{
								case '=':
									newTokens.Add(ScanOperator(TokenType.PipePipeEqual, 3));
									break;
								default:
									newTokens.Add(ScanOperator(TokenType.PipePipe, 2));
									break;
							}
							break;
						default:
							newTokens.Add(ScanOperator(TokenType.Pipe));
							break;
	                }
	                break;
				case '?':
					switch (Next)
					{
						case '?':
							switch (Peek(2))
							{
								case '=':
									newTokens.Add(ScanOperator(TokenType.QuestionQuestionEqual, 3));
									break;
								default:
									newTokens.Add(ScanOperator(TokenType.QuestionQuestion, 2));
									break;
							}
							break;
						case '!':
							switch (Peek(2))
							{
								case '=':
									newTokens.Add(ScanOperator(TokenType.QuestionBangEqual, 3));
									break;
								default:
									newTokens.Add(ScanOperator(TokenType.Question));
									break;
							}
							break;
						case '.':
							newTokens.Add(ScanOperator(TokenType.QuestionDot, 2));
							break;
						case '=':
							newTokens.Add(ScanOperator(TokenType.QuestionEqual, 2));
							break;
						case ':':
							newTokens.Add(ScanOperator(TokenType.QuestionColon, 2));
							break;
						case '[':
							newTokens.Add(ScanOperator(TokenType.QuestionOpenSquare, 2));
							break;
						default:
							newTokens.Add(ScanOperator(TokenType.Question));
							break;
					}
					break;
	            case '"':
					newTokens.Add(ScanString());
	                break;
	            case '\'':
					newTokens.Add(ScanCharacter());
	                break;
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
		
		tokens.InsertRange(tokenIndex, newTokens);
	}

	private Token? ScanComment()
	{
		if (Current != '/')
			return null;

		switch (Next)
		{
			// Match double slash as line comment
			case '/':
			{
				Advance();
				Advance();

				while (!EndOfFile)
				{
					if (Current is '\r' or '\n')
						break;

					Advance();
				}

				break;
			}
			// Match /* as a block comment */
			case '*':
			{
				Advance();
				Advance();
			
				var nestLevel = 1;
				while (!EndOfFile)
				{
					if (Current == '*' && Next == '/')
					{
						nestLevel--;
						Advance();
						Advance();

						if (nestLevel <= 0)
							break;
					}
					else if (Current == '/' && Next == '*')
					{
						nestLevel++;
						Advance();
						Advance();
						continue;
					}

					Advance();
				}

				break;
			}
			default:
				return null;
		}
		
		var length = currentPosition - start;
		return new Token(TokenType.Comment, new TextSpan(start, length, sourceText),
			sourceText.Text.Substring(start, length), null);
	}

	private Token ScanCharacter()
	{
		var escaped = false;
		var stringBuilder = new StringBuilder();

		while (!EndOfFile)
		{
			Advance();
			
			if (Current == '\n')
			{
				// @TODO Report an unclosed character error
				break;
			}

			if (escaped)
			{
				escaped = false;
				switch (Current)
				{
					case '\\':
						stringBuilder.Append('\\');
						break;
					case 'n':
						stringBuilder.Append('\n');
						break;
					case 'r':
						stringBuilder.Append('\r');
						break;
					case 't':
						stringBuilder.Append('\t');
						break;
					case '0':
						stringBuilder.Append('\0');
						break;
					case 'v':
						stringBuilder.Append('\v');
						break;
					case '"':
						stringBuilder.Append('"');
						break;
					case '\'':
						stringBuilder.Append('\'');
						break;
				}
			}
			else
			{
				if (Current == '\'')
				{
					Advance();
					break;
				}

				if (Current == '\\')
					escaped = !escaped;
				else
					stringBuilder.Append(Current);
			}
		}

		var character = stringBuilder.ToString();
		var value = character.Length > 0 ? character[0] : '\0';
		var length = currentPosition - start;
		var text = sourceText.Text.Substring(start, length);
		var span = new TextSpan(start, length, sourceText);

		if (text.Length != 1)
		{
			// Diagnostics.ReportLexerInvalidCharacterConstant(new TextSpan(start, Length, source), text);
		}

		return new Token(TokenType.CharacterConstant, span, text, value);
	}

	private Token ScanString()
	{
		var escaped = false;
		var stringBuilder = new StringBuilder();

		while (!EndOfFile)
		{
			Advance();

			if (Current == '\n')
			{
				// @TODO Report an unclosed string error
				break;
			}

			if (escaped)
			{
				escaped = false;
				switch (Current)
				{
					case '\\':
						stringBuilder.Append('\\');
						break;
					case 'n':
						stringBuilder.Append('\n');
						break;
					case 'r':
						stringBuilder.Append('\r');
						break;
					case 't':
						stringBuilder.Append('\t');
						break;
					case '0':
						stringBuilder.Append('\0');
						break;
					case 'v':
						stringBuilder.Append('\v');
						break;
					case '"':
						stringBuilder.Append('"');
						break;
					case '\'':
						stringBuilder.Append('\'');
						break;
				}
			}
			else
			{
				if (Current == '"')
				{
					Advance();
					break;
				}

				if (Current == '\\')
					escaped = !escaped;
				else
					stringBuilder.Append(Current);
			}
		}

		var value = stringBuilder.ToString();
		var length = currentPosition - start;
		var text = sourceText.Text.Substring(start, length);
		var span = new TextSpan(start, length, sourceText);
		return new Token(TokenType.StringConstant, span, text, value);
	}

	private char Advance()
	{
		var current = Current;
		
		if (!EndOfFile)
			currentPosition++;

		return current;
	}

	private char Peek(int offset = 0)
	{
		var index = currentPosition + offset;
		return index >= sourceText.Text.Length ? '\0' : sourceText.Text[index];
	}

	private Token ScanNumber()
	{
		var type = TokenType.Int64Constant;
		while (char.IsDigit(Current))
		{
			Advance();
		}
		
		var length = currentPosition - start;
		var text = sourceText.Text.Substring(start, length);
		object? value = null;

		var literal = text;
		switch (Current)
		{
			case 'i' or 'I':
			{
				Advance();
				switch (Current)
				{
					case '8':
						Advance();
						type = TokenType.Int8Constant;
						break;
					case '1' when Peek(1) == '6':
						Advance();
						Advance();
						type = TokenType.Int16Constant;
						break;
					case '3' when Peek(1) == '2':
						Advance();
						Advance();
						type = TokenType.Int32Constant;
						break;
					case '6' when Peek(1) == '4':
						Advance();
						Advance();
						type = TokenType.Int64Constant;
						break;
				}
				
				if (int.TryParse(literal, out var intValue))
					value = intValue;
				
				break;
			}
			case 'u' or 'U':
			{
				Advance();
				switch (Current)
				{
					case '8':
						Advance();
						type = TokenType.UInt8Constant;
						break;
					case '1' when Peek(1) == '6':
						Advance();
						Advance();
						type = TokenType.UInt16Constant;
						break;
					case '3' when Peek(1) == '2':
						Advance();
						Advance();
						type = TokenType.UInt32Constant;
						break;
					case '6' when Peek(1) == '4':
						Advance();
						Advance();
						type = TokenType.UInt64Constant;
						break;
				}
				
				if (int.TryParse(literal, out var intValue))
					value = intValue;
				
				break;
			}
			case '.':
			{
				type = TokenType.DoubleConstant;
				Advance();
			
				while (char.IsDigit(Current))
				{
					Advance();
				}

				length = currentPosition - start;
				text = sourceText.Text.Substring(start, length);
				literal = text;

				switch (Current)
				{
					case 's' or 'S':
						Advance();
						type = TokenType.SingleConstant;
			
						if (float.TryParse(literal, out var floatValue))
							value = floatValue;
						
						break;
					case 'd' or 'D':
						Advance();
						type = TokenType.DoubleConstant;
			
						if (double.TryParse(literal, out var doubleValue))
							value = doubleValue;
						
						break;
					default:
						if (double.TryParse(literal, out var defaultDoubleValue))
							value = defaultDoubleValue;
						
						break;
				}
				break;
			}
			default:
			{
				type = TokenType.Int64Constant;
				if (int.TryParse(text, out var intValue))
					value = intValue;
				break;
			}
		}
		
		return new Token(type, new TextSpan(start, length, sourceText), text, value);
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
		var text = sourceText.Text.Substring(start, length);
		var type = Token.LookupIdentifier(text);
		return new Token(type, new TextSpan(start, length, sourceText), text, null);
	}

	private Token ScanOperator(TokenType type, int length = 1)
	{
		currentPosition += length;
		return new Token(type, new TextSpan(start, length, sourceText), sourceText.Text.Substring(start, length), null);
	}

	public void Lex(string text)
	{
		foreach (var token in tokens)
		{
			token.Category = "";
			token.IsError = false;
			token.ErrorMessage = "";
		}
		
		var newSource = new SourceText(text);
		//var editSequences = sourceText.GetDifference(newSource);
		sourceText = newSource;
		LexAll();

		/*foreach (var editSequence in editSequences)
		{
			switch (editSequence.Operation)
			{
				case EditOperationKind.None:
					continue;
				case EditOperationKind.Edit:
				{
					// Re-lex from preceding token to succeeding token
					var startIndex = GetTokenIndexBeforePosition(editSequence.Start);
					var endIndex = GetTokenIndexAfterPosition(editSequence.End);

					if (startIndex < 0 || endIndex < 0)
						throw new Exception("Failed to find token indices for edit operation.");

					var startToken = tokens[startIndex];
					var endToken = tokens[endIndex];
					
					LexSpan(startToken.Span.Start, endToken.Span.End - startToken.Span.Start);
					break;
				}
				case EditOperationKind.Add:
				{
					// Re-lex from preceding token to succeeding token, increment indices and spans of succeeding tokens
					var startIndex = GetTokenIndexBeforePosition(editSequence.Start);
					var endIndex = GetTokenIndexAfterPosition(editSequence.Start);

					if (startIndex < 0 || endIndex < 0)
						throw new Exception("Failed to find token indices for add operation.");

					var startToken = tokens[startIndex];
					var endToken = tokens[endIndex];
					
					Console.WriteLine("Start Index: " + $"({startIndex})");
					Console.WriteLine("End Index: " + $"({endIndex})");

					for (var i = endIndex; i < tokens.Count; i++)
					{
						var token = tokens[i];
						token.Span.Start += editSequence.Length;
						Console.WriteLine($"Incrementing ({token}) by {editSequence.Length}");
					}
					
					LexSpan(startToken.Span.Start, endToken.Span.End - startToken.Span.Start + editSequence.Length);
					break;
				}
				case EditOperationKind.Remove:
				{
					// Re-lex from preceding token to succeeding token, decrement indices and spans of succeeding tokens
					break;
				}
			}
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

			if (token.Span.Start <= position && position < token.Span.End)
				return mid;

			if (token.Span.End <= position)
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