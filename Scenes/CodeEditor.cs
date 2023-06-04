using System.IO;
using System.Linq;
using System.Text;
using Godot;

namespace Tavern;

public partial class CodeEditor : CodeEdit
{
	public bool UnsavedChanges => GetVersion() != GetSavedVersion();
	public string Path { get; set; }
	public Encoding Encoding { get; private set; }

	public EncodingManager.EndOfLine EndOfLine { get; set; }

	private EncodingManager.Tab tab;
	public EncodingManager.Tab Tab
	{
		get => tab;
		set
		{
			tab = value;
			
			switch (tab)
			{
				case EncodingManager.Tab.Tab:
					Text = Text.Replace("    ", "\t");
					break;
				case EncodingManager.Tab.Space:
					Text = Text.Replace("\t", string.Concat(Enumerable.Repeat(' ', IndentSize)));
					break;
			}
		}
	}

	public override void _Ready()
	{
		if (Path is null)
			return;

		Encoding = EncodingManager.DetectTextEncoding(Path, out var text);
		EndOfLine = EncodingManager.DetectEol(text);
		Tab = EncodingManager.DetectTab(text);

		IndentUseSpaces = Tab == EncodingManager.Tab.Space;
		
		// Godot uses LF
		Text = EncodingManager.ConvertEol(text, EncodingManager.EndOfLine.LF);

		ClearUndoHistory();
		TagSavedVersion();
	}

	public void SaveChanges()
	{
		TagSavedVersion();

		var text = EncodingManager.ConvertEol(Text, EndOfLine);
		File.WriteAllBytes(Path, Encoding.GetBytes(text));
	}

	private void OnGuiInput(InputEvent @event)
	{
		if (@event is not InputEventMouseButton eventMouseButton)
			return;
		
		if (!eventMouseButton.Pressed)
			return;

		if (!eventMouseButton.CtrlPressed)
			return;

		var fontSize = GetThemeFontSize("font_size");
		switch (eventMouseButton.ButtonIndex)
		{
			case MouseButton.WheelUp:
				AddThemeFontSizeOverride("font_size", fontSize + 1);
				break;
			case MouseButton.WheelDown:
				AddThemeFontSizeOverride("font_size", Mathf.Max(8, fontSize - 1));
				break;
			default:
				return;
		}
	}

	private void OnTextChanged()
	{
		SyntaxHighlighter?.CallDeferred(SyntaxHighlighter.MethodName.UpdateCache);
		CallDeferred(CanvasItem.MethodName.QueueRedraw);
	}

	private void OnLinesEditedFrom(int lineFrom, int lineTo)
	{
		//SyntaxHighlighter?.CallDeferred(SyntaxHighlighter.MethodName.UpdateCache);
		//CallDeferred(CanvasItem.MethodName.QueueRedraw);
	}

	public override void _Draw()
	{
		if (SyntaxHighlighter is not IRichSyntaxHighlighter richSyntaxHighlighter)
			return;

		var errors = richSyntaxHighlighter.GetErrors();
		foreach (var error in errors)
		{
			for (var i = error.LineStart; i <= error.LineEnd; i++)
			{
				var lineLength = GetLine(i - 1).Length;
				var columnStart = i == error.LineStart ? error.ColumnStart : 0;
				var columnEnd = i == error.LineEnd ? error.ColumnEnd : lineLength;
				SetLineBackgroundColor(i - 1, new Color(1.0f, 0.0f, 0.0f, 0.25f));

				for (var j = columnStart; j <= columnEnd; j++)
				{
					var rect = GetRectAtLineColumn(i - 1, j - 1);
					DrawDashedLine(new Vector2(rect.Position.X, rect.End.Y), new Vector2(rect.End.X, rect.End.Y), Colors.Red, 2.0f);
				}
			}
		}
	}
}
