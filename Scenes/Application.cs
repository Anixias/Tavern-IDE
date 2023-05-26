using System.Collections.Generic;
using System.IO;
using System.Linq;
using Godot;
using FileAccess = Godot.FileAccess;

namespace Tavern;

public partial class Application : Control
{
	private class OpenEditor
	{
		public CodeEditor CodeEditor { get; }
		public int TabIndex { get; set; }
		public string Path { get; }
		
		public OpenEditor(CodeEditor codeEditor, int tabIndex, string path)
		{
			CodeEditor = codeEditor;
			TabIndex = tabIndex;
			Path = path;
		}
	}

	private int activeFileTab = -1;
	private TabBar fileTabBar;
	private Container codeEditorContainer;
	private PackedScene codeEditorScene;
	private readonly Dictionary<string, OpenEditor> openFiles = new();
	private CodeEditor? currentCodeEditor;
	private Tree fileSystemTree;
	private Project activeProject;

	private Texture2D iconFile;
	private Texture2D iconFolderOpen;
	private Texture2D iconFolderClosed;

	public override void _Ready()
	{
		codeEditorContainer = GetNode<Container>("%CodeEditorContainer");
		fileTabBar = GetNode<TabBar>("%FileTabBar");
		fileSystemTree = GetNode<Tree>("%FileSystemTree");
		GetWindow().FilesDropped += OnFilesDropped;

		codeEditorScene = ResourceLoader.Load<PackedScene>("res://Scenes/CodeEditor.tscn");
		iconFile = ResourceLoader.Load<Texture2D>("res://Assets/Icons/file.svg");
		iconFolderOpen = ResourceLoader.Load<Texture2D>("res://Assets/Icons/folder-open.svg");
		iconFolderClosed = ResourceLoader.Load<Texture2D>("res://Assets/Icons/folder.svg");
	}

	private void OnFilesDropped(string[] files)
	{
		foreach (var file in files)
		{
			OpenFile(file);
		}
	}

	private async void OpenFile(string path)
	{
		if (!File.Exists(path))
			return;

		if (openFiles.TryGetValue(path, out var openFile))
		{
			fileTabBar.CurrentTab = openFile.TabIndex;
			ActivateEditor(openFile.CodeEditor);
			return;
		}

		fileTabBar.AddTab(Path.GetFileName(path));
		fileTabBar.CurrentTab = fileTabBar.TabCount - 1;
		
		var codeEditor = codeEditorScene.Instantiate<CodeEditor>();
		codeEditorContainer.CallDeferred(Node.MethodName.AddChild, codeEditor);
		
		if (currentCodeEditor is not null && IsInstanceValid(currentCodeEditor))
			currentCodeEditor.Hide();
		
		currentCodeEditor = codeEditor;
		
		openFiles.Add(path, new OpenEditor(codeEditor, fileTabBar.TabCount - 1, path));

		if (!codeEditor.IsNodeReady())
			await ToSignal(codeEditor, Node.SignalName.Ready);
		
		using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
		codeEditor.Text = file.GetAsText();
	}

	private void ActivateEditor(CodeEditor codeEditor)
	{
		if (currentCodeEditor is not null && IsInstanceValid(currentCodeEditor))
			currentCodeEditor.Hide();
		
		currentCodeEditor = codeEditor;
		codeEditor.Show();
	}

	public async void LoadProject(Project project)
	{
		if (!IsNodeReady())
		{
			await ToSignal(this, Node.SignalName.Ready);
		}
		
		activeProject = project;
		if (project is null)
			return;
		
		LoadDirectory(project.Directory);
	}

	private void LoadDirectory(string directory, TreeItem parent = null)
	{
		var treeItem = fileSystemTree.CreateItem(parent);
		treeItem.SetText(0, Path.GetFileName(directory));
		treeItem.SetIcon(0, iconFolderOpen);

		foreach (var childDirectory in Directory.GetDirectories(directory).OrderBy(Path.GetFileName))
		{
			LoadDirectory(childDirectory, treeItem);
		}

		foreach (var childFile in Directory.GetFiles(directory).OrderBy(Path.GetFileName))
		{
			var fileTreeItem = fileSystemTree.CreateItem(treeItem);
			fileTreeItem.SetText(0, Path.GetFileName(childFile));
			fileTreeItem.SetIcon(0, iconFile);
			fileTreeItem.SetMetadata(0, childFile);
			fileTreeItem.DisableFolding = true;
		}
		
		treeItem.SetCollapsedRecursive(true);
	}

	private void OnFileSystemTreeItemActivated()
	{
		var item = fileSystemTree.GetSelected();
		if (item is null)
			return;

		var path = item.GetMetadata(0).AsString();
		OpenFile(path);
	}

	private void OnFileSystemTreeItemSelected()
	{
		var item = fileSystemTree.GetSelected();
		if (item is null)
			return;

		var path = item.GetMetadata(0).AsString();
		OpenFile(path);
	}

	private void OnFileSystemTreeItemCollapsed(TreeItem item)
	{
		if (item.DisableFolding)
			return;
		
		item.SetIcon(0, item.Collapsed ? iconFolderClosed : iconFolderOpen);
	}

	private void OnFileTabBarTabChanged(int index)
	{
		activeFileTab = index;
		
		foreach (var openEditor in openFiles.Values)
		{
			if (openEditor.TabIndex != index)
				continue;

			ActivateEditor(openEditor.CodeEditor);
			return;
		}
	}

	private void OnFileTabBarTabClosePressed(int index)
	{
		fileTabBar.RemoveTab(index);

		string? deletePath = null;
		string? openPath = null;
		foreach (var (path, openEditor) in openFiles)
		{
			if (openEditor.TabIndex < index)
				continue;

			if (openEditor.TabIndex == index)
			{
				deletePath = path;
				openEditor.CodeEditor.QueueFree();
			}
			else
			{
				if (openEditor.TabIndex == index + 1)
				{
					openPath = path;
				}
				
				openEditor.TabIndex--;
			}
		}

		if (openPath is not null)
			OpenFile(openPath);
		
		if (deletePath is null)
			return;
		
		openFiles.Remove(deletePath);
	}

	private void OnFileTabBarActiveTabRearranged(int newIndex)
	{
		if (newIndex > activeFileTab)
		{
			foreach (var openEditor in openFiles.Values)
			{
				if (openEditor.TabIndex > activeFileTab && openEditor.TabIndex <= newIndex)
				{
					openEditor.TabIndex--;
				}
				else if (openEditor.TabIndex == activeFileTab)
				{
					openEditor.TabIndex = newIndex;
				}
			}
		}
		else if (newIndex < activeFileTab)
		{
			foreach (var openEditor in openFiles.Values)
			{
				if (openEditor.TabIndex >= newIndex && openEditor.TabIndex < activeFileTab)
				{
					openEditor.TabIndex++;
				}
				else if (openEditor.TabIndex == activeFileTab)
				{
					openEditor.TabIndex = newIndex;
				}
			}
		}

		activeFileTab = newIndex;
	}
}
