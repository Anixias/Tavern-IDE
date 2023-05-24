using System.IO;
using Godot;

namespace Tavern;

public partial class Application : Control
{
	private TabBar fileTabBar;
	private CodeEditor codeEditor;
	
	public override void _Ready()
	{
		fileTabBar = GetNode<TabBar>("%FileTabBar");
		codeEditor = GetNode<CodeEditor>("%CodeEditor");
		GetWindow().FilesDropped += OnFilesDropped;
	}

	private void OnFilesDropped(string[] files)
	{
		foreach (var file in files)
		{
			fileTabBar.AddTab(Path.GetFileName(file));
			fileTabBar.CurrentTab = fileTabBar.TabCount - 1;
		}

		codeEditor.Visible = true;
	}
}
