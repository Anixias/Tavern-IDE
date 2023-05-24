using Godot;
using System;

public partial class Welcome : Control
{
	private Window newProjectWindow;

	public override void _Ready()
	{
		newProjectWindow = GetNode<Window>("%NewProjectWindow");
	}

	private void OnNewProjectButtonPressed()
	{
		newProjectWindow.PopupCentered();
	}
}
