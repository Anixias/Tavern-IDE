using System.Collections.Generic;
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
	private Window hoverWindow;
	private Label hoverMessage;

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

	private readonly Dictionary<Rect2, string> errorRects = new();

	public override void _Ready()
	{
		hoverWindow = GetNode<Window>("%HoverWindow");
		hoverMessage = GetNode<Label>("%HoverMessage");
		
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

		var fontSize = GetThemeFontSize("font_size");
		switch (eventMouseButton.ButtonIndex)
		{
			case MouseButton.WheelUp:
				if (eventMouseButton.CtrlPressed)
				{
					AddThemeFontSizeOverride("font_size", fontSize + 1);
					AcceptEvent();
				}

				break;
			case MouseButton.WheelDown:
				if (eventMouseButton.CtrlPressed)
				{
					AddThemeFontSizeOverride("font_size", Mathf.Max(8, fontSize - 1));
					AcceptEvent();
				}

				break;
			default:
				return;
		}
	}

	public override void _Process(double delta)
	{
		var hovered = false;
		UpdateErrors();

		if (hoverWindow.GetVisibleRect().HasPoint(DisplayServer.MouseGetPosition()))
			return;
		
		foreach (var (rect, message) in errorRects)
		{
			if (rect.HasPoint(GetLocalMousePosition()))
			{
				hovered = true;
				hoverMessage.Text = message;
				break;
			}
		}

		if (hovered)
		{
			hoverWindow.Position = DisplayServer.MouseGetPosition() + Vector2I.Right * 8 + Vector2I.Down * 8;
			hoverWindow.Popup();
		}
		else
		{
			hoverWindow.Hide();
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

	private void UpdateErrors()
	{
		errorRects.Clear();
		
		for (var i = 0; i < GetLineCount(); i++)
		{
			SetLineBackgroundColor(i, Colors.Transparent);
		}
		
		if (SyntaxHighlighter is not IRichSyntaxHighlighter richSyntaxHighlighter)
			return;

		var errors = richSyntaxHighlighter.GetErrors();
		foreach (var error in errors)
		{
			for (var i = error.Span.LineStart; i <= error.Span.LineEnd; i++)
			{
				if (i - 1 >= GetLineCount())
					break;
				
				var lineLength = GetLine(i - 1).Length;
				var columnStart = i == error.Span.LineStart ? error.Span.ColumnStart : 0;
				var columnEnd = i == error.Span.LineEnd ? error.Span.ColumnEnd : lineLength;
				SetLineBackgroundColor(i - 1, new Color(1.0f, 0.0f, 0.0f, 0.25f));

				if (lineLength == 1)
				{
					columnStart = 0;
					columnEnd = 0;
				}

				Rect2? errorRect = null;
				for (var j = columnStart; j <= columnEnd; j++)
				{
					if (j > lineLength)
						break;
					
					var rect = GetRectAtLineColumn(i - 1, j);
					errorRect = errorRect?.Merge(rect) ?? rect;
				}
				
				if (errorRect is not null)
					errorRects[errorRect.Value] = error.Message;
			}
		}
	}

	public override void _Draw()
	{
		foreach (var rect in errorRects.Keys)
			DrawDashedLine(new Vector2(rect.Position.X, rect.End.Y), new Vector2(rect.End.X, rect.End.Y), Colors.Red, 2.0f);
	}
}
