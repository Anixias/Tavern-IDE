using Godot;

namespace Tavern;

public partial class ListEntry : Control
{
	[Signal]
	public delegate void PressedEventHandler();
	
	private Label nameLabel;
	private Label directoryLabel;
	
	public override void _Ready()
	{
		nameLabel = GetNode<Label>("%NameLabel");
		directoryLabel = GetNode<Label>("%DirectoryLabel");
	}

	public async void SetProject(Project project)
	{
		if (!IsNodeReady())
			await ToSignal(this, Node.SignalName.Ready);
		
		nameLabel.Text = project.Name;
		directoryLabel.Text = project.Directory;
	}

	private void OnButtonPressed()
	{
		EmitSignal(SignalName.Pressed);
	}
}
