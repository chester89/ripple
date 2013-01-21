using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Policy;
using FubuCore;
using NuGet;

namespace ripple.New
{
    public interface INugetDownloader
    {
        INugetFile DownloadTo(string filename);
    }

    public class UrlNugetDownloader : INugetDownloader
    {
        private readonly string _url;

        public UrlNugetDownloader(string url)
        {
            _url = url;
        }

        public string Url
        {
            get { return _url; }
        }

        public INugetFile DownloadTo(string filename)
        {
            var client = new WebClient();

            Console.WriteLine("Downloading {0} to {1}", Url, filename);
            client.DownloadFile(Url, filename);

            return new NugetFile(filename);
        }

        private sealed class UrlEqualityComparer : IEqualityComparer<UrlNugetDownloader>
        {
            public bool Equals(UrlNugetDownloader x, UrlNugetDownloader y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.GetType() != y.GetType()) return false;
                return string.Equals(x._url, y._url);
            }

            public int GetHashCode(UrlNugetDownloader obj)
            {
                return (obj._url != null ? obj._url.GetHashCode() : 0);
            }
        }

        private static readonly IEqualityComparer<UrlNugetDownloader> UrlComparerInstance = new UrlEqualityComparer();

        public static IEqualityComparer<UrlNugetDownloader> UrlComparer
        {
            get { return UrlComparerInstance; }
        }
    }

    public class RemotePackageDownloader : INugetDownloader
    {
        private readonly IPackage _package;

        public RemotePackageDownloader(IPackage package)
        {
            _package = package;
        }

        public IPackage Package
        {
            get { return _package; }
        }

        public INugetFile DownloadTo(string filename)
        {
            using (var stream = new FileStream(filename, FileMode.Create, FileAccess.Write))
            {
                _package.GetStream().CopyTo(stream);
            }

            return new NugetFile(filename);
        }
    }

    public class RemoteNuget : IRemoteNuget
    {
        private readonly INugetDownloader _downloader;

        public RemoteNuget(string name, string version, string url)
        {
            Name = name;
            Version = SemanticVersion.Parse(version);
            _downloader = new UrlNugetDownloader(url);
        }

        public RemoteNuget(IPackage package)
        {
            Name = package.Id;
            Version = package.Version;
            _downloader = new RemotePackageDownloader(package);
        }

        public INugetDownloader Downloader
        {
            get { return _downloader; }
        }

        public string Name { get; private set; }
        public SemanticVersion Version { get; private set; }
        public INugetFile DownloadTo(string directory)
        {
            var file = directory.AppendPath(Filename);
            return _downloader.DownloadTo(file);
        }

        public string Filename
        {
            get
            {
                if (Version.SpecialVersion.IsEmpty())
                {
                    return "{0}.{1}.nupkg".ToFormat(Name, Version.Version.ToString());
                }

                return "{0}.{1}-{2}.nupkg".ToFormat(Name, Version.Version.ToString(), Version.SpecialVersion);
            }    
        }
    }
}