using System.Collections.Generic;

namespace ripple.New
{
    public interface IFloatingFeed : INugetFeed
    {
        IEnumerable<IRemoteNuget> GetLatest();
    }
}