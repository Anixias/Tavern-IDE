using Godot;
using System;

public partial class CodeEditor : CodeEdit
{
	private void OnGuiInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton eventMouseButton)
		{
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
}
