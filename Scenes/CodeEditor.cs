using System.IO;
using System.Linq;
using System.Text;
using Godot;
using GuildScript.Analysis.Text;

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
			
			// Replace 4 spaces at start of lines with tabs, account for selection
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

	private Lexer lexer;

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
		lexer = new Lexer(Text);
	}

	public void SaveChanges()
	{
		TagSavedVersion();

		var text = EncodingManager.ConvertEol(Text, EndOfLine);
		
		using var file = File.OpenWrite(Path);
		file.Write(Encoding.GetBytes(text));
		file.Flush();
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
}
