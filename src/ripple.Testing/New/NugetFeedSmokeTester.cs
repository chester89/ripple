using NUnit.Framework;
using ripple.New;
using System.Linq;
using FubuTestingSupport;

namespace ripple.Testing.New
{
    [TestFixture, Explicit]
    public class NugetFeedSmokeTester
    {
        [Test]
        public void find_nuget_by_name()
        {
            var feed = new NugetFeed(RippleConstants.NugetOrgFeed.First());
            feed.Find(new NugetQuery
            {
                Name = "FubuMVC.Core",
                Version = "1.0.0.1402"
            }).ShouldNotBeNull();
        }

        [Test]
        public void find_latest_by_name()
        {
            var feed = new NugetFeed(RippleConstants.NugetOrgFeed.First());
            feed.FindLatest(new NugetQuery { Name = "FubuMVC.Core" })
                .Version.Version.ToString().ShouldEqual("1.0.0.1402");
        }
    }
}