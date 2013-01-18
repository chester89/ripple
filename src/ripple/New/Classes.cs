using System;
using System.Collections.Generic;
using System.Xml;
using FubuCore;
using NuGet;

namespace ripple.New
{
    public interface INugetFile
    {
        string Name { get; }
        SemanticVersion Version { get; }
        IPackage ExplodeTo(string directory);

    }

    public interface IRemoteNuget
    {
        string Name { get; }
        SemanticVersion Version { get; }
        INugetFile DownloadTo(string directory);
        string Filename { get; }
    }

    public class NugetXmlFeed
    {
        private readonly XmlDocument _document;
        private static readonly XmlNamespaceManager _manager;

        static NugetXmlFeed()
        {
            _manager = new XmlNamespaceManager(new NameTable());
            _manager.AddNamespace("d", "http://schemas.microsoft.com/ado/2007/08/dataservices");
            _manager.AddNamespace("m", "http://schemas.microsoft.com/ado/2007/08/dataservices/metadata");
            _manager.AddNamespace("atom", "http://www.w3.org/2005/Atom");
        }

        public static NugetXmlFeed LoadFrom(string file)
        {
            var document = new XmlDocument();
            document.Load(file);

            return new NugetXmlFeed(document);
        }

        public static NugetXmlFeed FromXml(string xml)
        {
            var document = new XmlDocument();
            document.LoadXml(xml);

            return new NugetXmlFeed(document);
        }

        public NugetXmlFeed(XmlDocument document)
        {
            _document = document;
        }

        public IEnumerable<IRemoteNuget> ReadAll()
        {
            var nodes = _document.DocumentElement.SelectNodes("atom:entry", _manager);
            foreach (XmlElement element in nodes)
            {
                var url = element.SelectSingleNode("atom:id", _manager).InnerText;
                var name = element.SelectSingleNode("atom:title", _manager).InnerText;

                var properties = element.SelectSingleNode("m:properties", _manager);

                var version = properties.SelectSingleNode("d:Version", _manager).InnerText;


                yield return new RemoteNuget(name, version, url);
            }
        }
    }

    public class RemoteNuget : IRemoteNuget
    {
        private string _url;

        public RemoteNuget(string name, string version, string url)
        {
            Name = name;
            Version = SemanticVersion.Parse(version);
            _url = url;
        }

        public string Url
        {
            get { return _url; }
        }

        public string Name { get; private set; }
        public SemanticVersion Version { get; private set; }
        public INugetFile DownloadTo(string directory)
        {
            throw new System.NotImplementedException();
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

    public interface IFloatingFeed : INugetFeed
    {
        IEnumerable<IRemoteNuget> GetLatest();
    }

    public class FloatingFeed : IFloatingFeed
    {
        private readonly string _url;

        public FloatingFeed(string url)
        {
            _url = url;
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
            throw new System.NotImplementedException();
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
}