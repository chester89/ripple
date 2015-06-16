using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FubuCore;
using FubuCore.Descriptions;
using FubuCore.Logging;
using NuGet;
using ripple.Model;

namespace ripple.Nuget
{
    public class FileSystemNugetFeed : NugetFeedBase
    {
        private readonly string _directory;
        private readonly FubuCore.IFileSystem _fileSystem;
        private readonly NugetStability _stability;
        private readonly Lazy<IEnumerable<INugetFile>> _files; 
        private IPackageRepository _repository;
        private bool _online = true;

        public FileSystemNugetFeed(string directory, NugetStability stability)
        {
            _directory = directory.ToCanonicalPath();

            Path.GetInvalidPathChars().Each(x =>
            {
                if (_directory.Contains(x))
                {
                    throw new InvalidOperationException("Invalid character in path: {0} ({1})".ToFormat(x, (int)x));
                }
            });

            _stability = stability;
            _fileSystem = new FileSystem();

            _files = new Lazy<IEnumerable<INugetFile>>(findFiles);
        }

        public string Directory { get { return _directory; } }

        protected IEnumerable<INugetFile> files
        {
            get { return _files.Value; }
        }

        private IEnumerable<INugetFile> findFiles()
        {
            var nupkgSet = new FileSet
            {
                Include = "*.nupkg",
                DeepSearch = false
            };

            return _fileSystem.FindFiles(_directory, nupkgSet).Select(x => new NugetFile(x, SolutionMode.Ripple));
        }

        private IRemoteNuget findMatching(Func<INugetFile, bool> predicate)
        {
            var file = files.FirstOrDefault(predicate);
            if (file == null)
            {
                return null;
            }

            return new FileSystemNuget(file); 
        }

        public override bool IsOnline()
        {
            return _online;
        }

        public override void MarkOffline()
        {
            _online = false;
        }

        protected override IRemoteNuget find(Dependency query)
        {
            RippleLog.Debug("Searching for {0} in {1}".ToFormat(query, _directory));

            SemanticVersion version;
			if (!SemanticVersion.TryParse(query.Version, out version))
			{
				RippleLog.Debug("Could not find exact for " + query);
				return null;
			}

            return findMatching(nuget => query.MatchesName(nuget.Name) && nuget.Version == version);
        }

        public override IRemoteNuget FindLatestByName(string name)
        {
            return findLatest(new Dependency(name));
        }

        public override IEnumerable<IRemoteNuget> FindAllLatestByName(string idPart)
        {
            // TODO: reconsider whether querying over file system should be enabled
            return Enumerable.Empty<IRemoteNuget>();
        }

        protected override IRemoteNuget findLatest(Dependency query)
        {
            RippleLog.Debug("Searching for latest of {0} in {1}".ToFormat(query, _directory));

            var nugets = files
                .Where(x => query.MatchesName(x.Name) && (!x.IsPreRelease || (x.IsPreRelease && query.DetermineStability(_stability) == NugetStability.Anything)))
                .ToList();
                
            var nuget = nugets
                .OrderByDescending(x => x.Version)
                .FirstOrDefault();

            if (nuget == null)
            {
                return null;
            }

            return new FileSystemNuget(nuget);
        }

        public override IPackageRepository Repository
        {
            get { return _repository; }
        }

        public override string ToString()
        {
            return _directory;
        }
    }

    public class FloatingFileSystemNugetFeed : FileSystemNugetFeed, IFloatingFeed
    {
        private readonly Lazy<IEnumerable<IRemoteNuget>> _nugets;
        private bool _dumped = false;

        public FloatingFileSystemNugetFeed(string directory, NugetStability stability) 
            : base(directory, stability)
        {
            _nugets = new Lazy<IEnumerable<IRemoteNuget>>(findLatest);
        }

        private IEnumerable<IRemoteNuget> findLatest()
        {
            var nugets = new List<INugetFile>();

            RippleLog.Debug("Retrieving all latest from " + Directory);

			var distinct = from nuget in files
						   let name = nuget.Name.ToLower()
						   group nuget by name;

            distinct
                .Each(x =>
                {
                    var latest = x.OrderByDescending(n => n.Version).First();
                    nugets.Add(latest);
                });

            return nugets
                .Select(x => new FileSystemNuget(x))
                .OrderBy(x => x.Name);
        }

        public IEnumerable<IRemoteNuget> GetLatest()
        {
            return _nugets.Value;
        }

        public IRemoteNuget LatestFor(Dependency dependency)
        {
            throw new NotImplementedException();
        }

        public void DumpLatest()
        {
            lock (this)
            {
                if (_dumped) return;

                var latest = GetLatest();
                var topic = new LatestNugets(latest, Directory);

                RippleLog.DebugMessage(topic);
                _dumped = true;
            }
        }

        
    }

    public class LatestNugets : LogTopic, DescribesItself
    {
        private readonly IEnumerable<IRemoteNuget> _nugets;
        private readonly string _source;

        public LatestNugets(IEnumerable<IRemoteNuget> nugets, string source)
        {
            _nugets = nugets;
            _source = source;
        }

        public void Describe(Description description)
        {
            description.ShortDescription = "Nugets found from " + _source;
            var list = description.AddList("Nugets", _nugets);
            list.Label = "Nugets";
        }
    }
}