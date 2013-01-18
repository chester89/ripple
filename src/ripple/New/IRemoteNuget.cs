using NuGet;

namespace ripple.New
{
    public interface IRemoteNuget
    {
        string Name { get; }
        SemanticVersion Version { get; }
        INugetFile DownloadTo(string directory);
        string Filename { get; }
    }
}