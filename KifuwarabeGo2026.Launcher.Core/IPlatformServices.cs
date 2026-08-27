namespace KifuwarabeGo2026.Launcher;

public interface ILauncherPathProvider
{
    string LocalApplicationData { get; }
    string MyPictures { get; }
}

public interface IRunningProcessCatalog
{
    bool IsProcessRunningFrom(string directory);
}

public interface IPlatformProcessService
{
    bool Start(string executable, string workingDirectory);
}

public interface ILauncherEnginePlatform : ILauncherPathProvider, IRunningProcessCatalog, IPlatformProcessService
{
}

public interface ILauncherGuiPlatform
{
    string? SelectFolder(string title, string initialDirectory);
    bool OpenFolder(string directory);
    bool OpenFile(string filePath);
}

public interface IFileSystem
{
    bool FileExists(string path);
    string ReadAllText(string path);
    void WriteAllText(string path, string contents);
    void CreateDirectory(string path);
    void MoveFile(string source, string destination);
    void ReplaceFile(string source, string destination);
}

public sealed class SystemFileSystem : IFileSystem
{
    public bool FileExists(string path) => File.Exists(path);
    public string ReadAllText(string path) => File.ReadAllText(path);
    public void WriteAllText(string path, string contents) => File.WriteAllText(path, contents);
    public void CreateDirectory(string path) => Directory.CreateDirectory(path);
    public void MoveFile(string source, string destination) => File.Move(source, destination);
    public void ReplaceFile(string source, string destination) => File.Replace(source, destination, null);
}
