namespace Tavern;

public sealed class Project
{
	public string Name { get; }
	public string Directory { get; }
	
	public Project(string name, string directory)
	{
		Name = name;
		Directory = directory;
	}
}