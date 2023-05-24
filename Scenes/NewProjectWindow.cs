using Godot;
using System;

public partial class NewProjectWindow : Window
{
	private void OnCloseRequested()
	{
		Hide();
	}

	private void OnCancelButtonPressed()
	{
		Hide();
	}
}
