using System.IO;
using Godot;
using System.Text.RegularExpressions;

public partial class CreateFolderWindow : Window
{
	[Signal]
	public delegate void SubmittedEventHandler(string path);
	
	private Button createButton;
	private LineEdit nameLineEdit;
	
	public string CurrentDirectory { get; set; }

	public override void _Ready()
	{
		createButton = GetNode<Button>("%CreateButton");
		nameLineEdit = GetNode<LineEdit>("%NameLineEdit");
	}

	private void OnAboutToPopup()
	{
		nameLineEdit.Text = "";
		createButton.Disabled = true;
		nameLineEdit.GrabFocus();
	}

	private void OnCancelButtonPressed()
	{
		Hide();
	}

	private void OnCloseRequested()
	{
		Hide();
	}

	private void OnNameLineEditTextSubmitted(string text)
	{
		if (IsValidFilename(text))
			Submit(text);
	}

	private void OnCreateButtonPressed()
	{
		var text = nameLineEdit.Text;
		
		if (IsValidFilename(text))
			Submit(text);
	}

	private void Submit(string text)
	{
		Hide();
		EmitSignal(SignalName.Submitted, $"{CurrentDirectory}/{text}");
	}

	private void OnNameLineEditTextChanged(string text)
	{
		createButton.Disabled = !IsValidFilename(text);
	}
	
	private bool IsValidFilename(string filename)
	{
		if (string.IsNullOrWhiteSpace(filename))
		{
			return false;
		}

		if (filename.EndsWith("."))
		{
			return false;
		}

		if (filename.StartsWith("-"))
		{
			return false;
		}

		if (File.Exists($"{CurrentDirectory.Replace("/", "\\")}\\{filename}"))
			return false;

		if (Directory.Exists($"{CurrentDirectory.Replace("/", "\\")}\\{filename}"))
			return false;

		var containsInvalidCharacter = ValidFilenameRegex();
		return !containsInvalidCharacter.IsMatch(filename);
	}

    [GeneratedRegex("[\\\\/:*?\"<>|]")]
    private static partial Regex ValidFilenameRegex();
}