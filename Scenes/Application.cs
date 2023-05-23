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
	}
}
