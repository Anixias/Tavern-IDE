using System.Collections.Generic;
using System.IO;
using Godot;
using FileAccess = Godot.FileAccess;

namespace Tavern;

public partial class Welcome : Control
{
	private NewProjectWindow newProjectWindow;
	private readonly List<Project> recentProjects = new();
	private Container recentProjectsContainer;
	private const string RecentProjectsPath = "user://data/recent.dat";

	public override void _Ready()
	{
		recentProjectsContainer = GetNode<Container>("%RecentProjectsContainer");
		newProjectWindow = GetNode<NewProjectWindow>("%NewProjectWindow");
		newProjectWindow.NewProjectCreated += OnNewProjectCreated;

		LoadRecentProjects();
	}

	private void SaveRecentProjects()
	{
		DirAccess.MakeDirRecursiveAbsolute(RecentProjectsPath.GetBaseDir());
		using var file = FileAccess.Open(RecentProjectsPath, FileAccess.ModeFlags.Write);
		if (file is null)
		{
			GD.PushError(FileAccess.GetOpenError());
			return;
		}
		
		file.Store64((ulong)recentProjects.Count);
		foreach (var project in recentProjects)
		{
			file.StorePascalString(project.Name);
			file.StorePascalString(project.Directory);
			file.Flush();
		}
	}

	private void LoadRecentProjects()
	{
		if (!FileAccess.FileExists(RecentProjectsPath))
			return;
		
		recentProjects.Clear();
		foreach (var projectEntry in recentProjectsContainer.GetChildren())
		{
			projectEntry.QueueFree();
		}
		
		using var file = FileAccess.Open(RecentProjectsPath, FileAccess.ModeFlags.Read);
		var count = file.Get64();
		var projects = new List<Project>();
		while (count-- > 0)
		{
			var name = file.GetPascalString();
			var directory = file.GetPascalString();
			projects.Add(new Project(name, directory));
		}

		projects.Reverse();

		foreach (var project in projects)
		{
			AppendRecentProject(project);
		}
	}

	private void AppendRecentProject(Project project)
	{
		if (!recentProjects.Contains(project))
		{
			var entryScene = ResourceLoader.Load<PackedScene>("res://Scenes/ListEntry.tscn");
			var entry = entryScene.Instantiate<ListEntry>();
			entry.SetProject(project);
			entry.Pressed += () => LoadProject(project);

			recentProjects.Insert(0, project);
			recentProjectsContainer.AddChild(entry);
			recentProjectsContainer.MoveChild(entry, 0);
		}
		else
		{
			var index = recentProjects.FindIndex(p => p == project);
			recentProjects.RemoveAt(index);
			recentProjects.Insert(0, project);
			var entry = recentProjectsContainer.GetChild<ListEntry>(index);
			recentProjectsContainer.MoveChild(entry, 0);
		}

		SaveRecentProjects();
	}

	private void OnNewProjectCreated(string name, string directory)
	{
		var targetDirectory = $"{directory}/{name}";
		Directory.CreateDirectory(targetDirectory);
		var project = new Project(name, targetDirectory);

		LoadProject(project);
	}

	private void LoadProject(Project project)
	{
		var applicationScene = ResourceLoader.Load<PackedScene>("res://Scenes/Application.tscn");
		var application = applicationScene.Instantiate<Application>();
		application.LoadProjectAsync(project);
		AppendRecentProject(project);

		Hide();
		GetParent().AddChild(application);
	}

	private void OnNewProjectButtonPressed()
	{
		newProjectWindow.PopupCentered();
	}
}
