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
		
		public OpenEditor(CodeEditor codeEditor, int tabIndex)
		{
			CodeEditor = codeEditor;
			TabIndex = tabIndex;
		}
	}

	private int activeFileTab = -1;
	private TabBar fileTabBar;
	private TabContainer bottomTabContainer;
	private Container codeEditorContainer;
	private PackedScene codeEditorScene;
	private readonly Dictionary<string, OpenEditor> openFiles = new();
	private readonly List<TreeItem> treeItems = new();
	private CodeEditor currentCodeEditor;
	private Tree fileSystemTree;
	private Project activeProject;
	private Container fileInfoContainer;

	private Texture2D iconFile;
	private Texture2D iconFolderOpen;
	private Texture2D iconFolderClosed;
	private Button createFileButton;
	private Button createFolderButton;
	private CreateFileWindow createFileWindow;
	private CreateFolderWindow createFolderWindow;
	private ConfirmationDialog deleteConfirmationDialog;
	private bool deleteFile;
	private string deletePath;

	public override void _Ready()
	{
		codeEditorContainer = GetNode<Container>("%CodeEditorContainer");
		fileInfoContainer = GetNode<Container>("%FileInfoContainer");
		fileTabBar = GetNode<TabBar>("%FileTabBar");
		bottomTabContainer = GetNode<TabContainer>("%BottomTabContainer");
		fileSystemTree = GetNode<Tree>("%FileSystemTree");
		createFileButton = GetNode<Button>("%CreateFileButton");
		createFolderButton = GetNode<Button>("%CreateFolderButton");
		createFileWindow = GetNode<CreateFileWindow>("%CreateFileWindow");
		createFolderWindow = GetNode<CreateFolderWindow>("%CreateFolderWindow");
		deleteConfirmationDialog = GetNode<ConfirmationDialog>("%DeleteConfirmationDialog");
		GetWindow().FilesDropped += OnFilesDropped;
		bottomTabContainer.SetTabIcon(0, ResourceLoader.Load<Texture2D>("res://Assets/Icons/terminal.svg"));

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
		
		openFiles.Add(path, new OpenEditor(codeEditor, fileTabBar.TabCount - 1));

		if (!codeEditor.IsNodeReady())
			await ToSignal(codeEditor, Node.SignalName.Ready);
		
		using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
		codeEditor.Text = file.GetAsText();
		
		// @TODO Detect file info
		fileInfoContainer.Visible = true;
	}

	private void ActivateEditor(CodeEditor codeEditor)
	{
		if (currentCodeEditor is not null && IsInstanceValid(currentCodeEditor))
			currentCodeEditor.Hide();
		
		// @TODO Detect file info
		currentCodeEditor = codeEditor;
		codeEditor.Show();
	}

	public async void LoadProject(Project project)
	{
		if (!IsNodeReady())
		{
			await ToSignal(this, Node.SignalName.Ready);
		}

		fileSystemTree.Clear();
		treeItems.Clear();
		
		activeProject = project;
		if (project is null)
			return;
		
		LoadDirectory(project.Directory);
	}

	private void LoadDirectory(string directory, TreeItem parent = null)
	{
		var treeItem = fileSystemTree.CreateItem(parent);
		treeItems.Add(treeItem);
		treeItem.SetText(0, Path.GetFileName(directory));
		treeItem.SetIcon(0, iconFolderOpen);
		treeItem.SetMetadata(0, directory);

		foreach (var childDirectory in Directory.GetDirectories(directory).OrderBy(Path.GetFileName))
		{
			LoadDirectory(childDirectory, treeItem);
		}

		foreach (var childFile in Directory.GetFiles(directory).OrderBy(Path.GetFileName))
		{
			AddFileToTree(childFile, treeItem);
		}
		
		treeItem.SetCollapsedRecursive(true);
	}

	private TreeItem AddFileToTree(string path, TreeItem parent)
	{
		var fileTreeItem = fileSystemTree.CreateItem(parent);
		treeItems.Add(fileTreeItem);
		fileTreeItem.SetText(0, Path.GetFileName(path));
		fileTreeItem.SetIcon(0, iconFile);
		fileTreeItem.SetMetadata(0, path);
		fileTreeItem.DisableFolding = true;

		return fileTreeItem;
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
		if (Directory.Exists(path))
		{
			createFileButton.Disabled = false;
			createFolderButton.Disabled = false;
		}
		else
		{
			createFileButton.Disabled = true;
			createFolderButton.Disabled = true;
		}
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

		string deleteFilePath = null;
		string openPath = null;
		foreach (var (path, openEditor) in openFiles)
		{
			if (openEditor.TabIndex < index)
				continue;

			if (openEditor.TabIndex == index)
			{
				deleteFilePath = path;
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
		
		if (deleteFilePath is null)
			return;
		
		openFiles.Remove(deleteFilePath);
		if (openFiles.Count == 0)
			fileInfoContainer.Visible = false;
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

	private void OnCreateFileButtonPressed()
	{
		var item = fileSystemTree.GetSelected();
		if (item is null)
			return;

		var path = item.GetMetadata(0).AsString();
		if (!Directory.Exists(path))
			return;

		createFileWindow.CurrentDirectory = path;
		createFileWindow.PopupCentered();
	}

	private void OnCreateFileWindowSubmitted(string path)
	{
		using var file = FileAccess.Open(path, FileAccess.ModeFlags.Write);
		
		// @TODO Load file template
		
		ReloadProject();
		OpenFile(path);
		
		foreach (var item in treeItems)
		{
			var itemPath = item.GetMetadata(0).AsString();
			if (itemPath != path)
				continue;
			
			fileSystemTree.SetSelected(item, 0);
			fileSystemTree.ScrollToItem(item);
			break;
		}
	}

	private void OnCreateFolderButtonPressed()
	{
		var item = fileSystemTree.GetSelected();
		if (item is null)
			return;

		var path = item.GetMetadata(0).AsString();
		if (!Directory.Exists(path))
			return;

		createFolderWindow.CurrentDirectory = path;
		createFolderWindow.PopupCentered();
	}

	private void OnCreateFolderWindowSubmitted(string path)
	{
		Directory.CreateDirectory(path);
		ReloadProject();
	}

	private void ReloadProject()
	{
		var collapsedStates = new Dictionary<string, bool>();
		string selectedPath = null;

		foreach (var item in treeItems)
		{
			var path = item.GetMetadata(0).AsString();
			collapsedStates[path] = item.Collapsed;
			
			if (fileSystemTree.GetSelected() == item)
				selectedPath = path;
		}
		
		LoadProject(activeProject);

		TreeItem selectedItem = null;
		foreach (var item in treeItems)
		{
			var path = item.GetMetadata(0).AsString();
			item.Collapsed = !collapsedStates.TryGetValue(path, out var collapsed) || collapsed;

			if (path != selectedPath)
				continue;

			selectedItem = item;
		}

		if (selectedItem is null)
			return;
		
		fileSystemTree.SetSelected(selectedItem, 0);
		fileSystemTree.ScrollToItem(selectedItem);
	}

	private void OnFileSystemTreeGuiInput(InputEvent @event)
	{
		if (@event is InputEventKey eventKey)
		{
			if (eventKey.GetKeycodeWithModifiers() == Key.Delete && eventKey.Pressed)
			{
				var item = fileSystemTree.GetSelected();
				if (item is null)
					return;

				var path = item.GetMetadata(0).AsString();
				PromptDelete(path);
			}
		}
	}

	private void PromptDelete(string path)
	{
		if (path == activeProject.Directory)
			return;
		
		deletePath = path;
		if (File.Exists(path))
		{
			deleteFile = true;
			deleteConfirmationDialog.Title = "Delete File";
			deleteConfirmationDialog.DialogText = $"Are you sure you want to delete file '{Path.GetFileName(path)}'?";
			deleteConfirmationDialog.PopupCentered();
		}
		else if (Directory.Exists(path))
		{
			deleteFile = false;
			deleteConfirmationDialog.Title = "Delete Folder";
			deleteConfirmationDialog.DialogText = $"Are you sure you want to delete folder '{Path.GetFileName(path)}'?";
			deleteConfirmationDialog.PopupCentered();
		}
	}

	private void OnDeleteConfirmationDialogCanceled()
	{
		deletePath = null;
		deleteFile = false;
	}

	private void OnDeleteConfirmationDialogConfirmed()
	{
		if (deleteFile)
			File.Delete(deletePath);
		else
			Directory.Delete(deletePath);
		
		deletePath = null;
		deleteFile = false;
		
		ReloadProject();
	}
}
