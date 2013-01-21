﻿using System.Security.Policy;
using FubuCore;
using NuGet;

namespace ripple.New
{
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