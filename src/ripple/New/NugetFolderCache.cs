using System;
using System.Collections.Generic;
using System.Linq;
using FubuCore;

namespace ripple.New
{
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
            new FileSystem().CleanDirectory(_folder);
        }

        public IEnumerable<INugetFile> AllFiles()
        {
            return
                new FileSystem().FindFiles(_folder, new FileSet {Include = "*.nupkg"})
                                .Select(file => new NugetFile(file));
        } 

        public INugetFile Find(NugetQuery query)
        {
            throw new NotImplementedException();
        }
    }
}