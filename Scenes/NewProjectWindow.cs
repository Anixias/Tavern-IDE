using Godot;
using System;

namespace Tavern;

public partial class NewProjectWindow : Window
{
	[Signal]
	public delegate void NewProjectCreatedEventHandler(string name, string directory); 
	
	private Container templateContainer;
	private LineEdit nameLineEdit;
	private LineEdit directoryLineEdit;
	private Label templateNameLabel;
	private FileDialog fileDialog;

	public override void _Ready()
	{
		templateContainer = GetNode<Container>("%TemplateContainer");
		nameLineEdit = GetNode<LineEdit>("%NameLineEdit");
		directoryLineEdit = GetNode<LineEdit>("%DirectoryLineEdit");
		templateNameLabel = GetNode<Label>("%TemplateNameLabel");
		fileDialog = GetNode<FileDialog>("%FileDialog");
	}

	private void OnCloseRequested()
	{
		Hide();
	}

	private void OnCancelButtonPressed()
	{
		Hide();
	}

	private void OnCreateButtonPressed()
	{
		Hide();
		EmitSignal(SignalName.NewProjectCreated, nameLineEdit.Text, directoryLineEdit.Text);
	}

	private void OnAboutToPopup()
	{
		ResetForm();
	}

	private void OnBrowseButtonPressed()
	{
		fileDialog.CurrentDir = directoryLineEdit.Text;
		fileDialog.PopupCentered();
	}

	private void ResetForm()
	{
		if (templateContainer.GetChildCount() > 0)
		{
			var button = templateContainer.GetChild<Button>(0);
			button.ButtonPressed = true;
			templateNameLabel.Text = button.Text;
		}

		nameLineEdit.Text = "Untitled";
		directoryLineEdit.Text = OS.GetSystemDir(OS.SystemDir.Documents);
	}

	private void OnFileDialogDirSelected(string directory)
	{
		directoryLineEdit.Text = directory;
	}
}
