using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Xml;
using NuGet;
using System.Linq;
using FubuCore;

namespace ripple.New
{

    public interface IFloatingFeed : INugetFeed
    {
        IEnumerable<IRemoteNuget> GetLatest();
    }

    public class FloatingFeed : IFloatingFeed
    {
        public const string FindAllLatestCommand =
            "/Packages()?$filter=IsAbsoluteLatestVersion&$orderby=DownloadCount%20desc,Id&$skip=0&$top=1000";

        private readonly string _url;

        public FloatingFeed(string url)
        {
            _url = url.TrimEnd('/');
        }

        private XmlDocument loadLatestFeed()
        {
            var url = _url + FindAllLatestCommand;
            var client = new WebClient();
            var text = client.DownloadString(url);

            var document = new XmlDocument();
            document.LoadXml(text);

            return document;
        }

        public IRemoteNuget Find(NugetQuery query)
        {
            throw new System.NotImplementedException();
        }

        public IRemoteNuget FindLatest(NugetQuery query)
        {
            throw new System.NotImplementedException();
        }

        public IEnumerable<IRemoteNuget> GetLatest()
        {
            var feed = new NugetXmlFeed(loadLatestFeed());
            return feed.ReadAll();
        }
    }

    public interface INugetFeed
    {
        IRemoteNuget Find(NugetQuery query);
        IRemoteNuget FindLatest(NugetQuery query);
    }

    public enum NugetStability
    {
        ReleasedOnly,
        Anything
    }

    public class NugetQuery
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public NugetStability Stability { get; set; }
    }

    public interface INugetCache
    {
        void UpdateAll(IEnumerable<IRemoteNuget> nugets);
        INugetFile Latest(NugetQuery query);

        void Flush();

        INugetFile Find(NugetQuery query);
    }

    public class NugetFolderCache : INugetCache
    {
        private readonly string _folder;

        public NugetFolderCache(string folder)
        {
            _folder = folder;
        }

        public void UpdateAll(IEnumerable<IRemoteNuget> nugets)
        {
            throw new NotImplementedException();
        }

        public INugetFile Latest(NugetQuery query)
        {
            throw new NotImplementedException();
        }

        public void Flush()
        {
            throw new NotImplementedException();
        }

        public INugetFile Find(NugetQuery query)
        {
            throw new NotImplementedException();
        }
    }

    public interface INugetFile
    {
        string Name { get; }
        SemanticVersion Version { get; }
        IPackage ExplodeTo(string directory);
    }

    public class NugetFile : INugetFile
    {
        private readonly string _path;

        public NugetFile(string path)
        {
            _path = path;

            var file = Path.GetFileNameWithoutExtension(path);
            var parts = file.Split('.');
            Name = parts.First();
            Version = SemanticVersion.Parse(parts.Skip(1).Join("."));

            IsPreRelease = Version.SpecialVersion.IsNotEmpty();
        }

        public string Name { get; private set; }
        public SemanticVersion Version { get; private set; }
        public bool IsPreRelease { get; private set; }

        public IPackage ExplodeTo(string directory)
        {
            throw new NotImplementedException();
        }
    }

}