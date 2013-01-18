using System;
using System.Net;
using FubuCore;
using NuGet;

namespace ripple.New
{
    public class RemoteNuget : IRemoteNuget
    {
        private readonly string _url;

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
            var file = directory.AppendPath(Filename);
            var client = new WebClient();

            Console.WriteLine("Downloading {0} to {1}", Url, file);
            client.DownloadFile(Url, file);

            return new NugetFile(file);
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