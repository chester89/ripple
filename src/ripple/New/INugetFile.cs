using NuGet;

namespace ripple.New
{
    public interface INugetFile
    {
        string Name { get; }
        SemanticVersion Version { get; }
        IPackage ExplodeTo(string directory);
    }
}