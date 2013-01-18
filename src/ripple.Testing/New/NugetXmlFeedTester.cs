using NUnit.Framework;
using ripple.New;
using System.Linq;
using FubuTestingSupport;

namespace ripple.Testing.New
{
    [TestFixture]
    public class NugetXmlFeedTester
    {
        private NugetXmlFeed theFeed;

        [SetUp]
        public void SetUp()
        {
            theFeed = NugetXmlFeed.LoadFrom("feed.xml");
        }

        [Test]
        public void does_not_blow_up()
        {
            theFeed.ReadAll().Any().ShouldBeTrue();
        }

        [Test]
        public void spot_check_a_nuget()
        {
            var nuget = theFeed.ReadAll().Single(x => x.Name == "FubuMVC.Core").ShouldBeOfType<RemoteNuget>();

            nuget.Name.ShouldEqual("FubuMVC.Core");
            nuget.Version.Version.ToString().ShouldEqual("1.0.0.1402");
            nuget.Url.ShouldEqual("http://build.fubu-project.org/guestAuth/app/nuget/v1/FeedService.svc/Packages(Id='FubuMVC.Core',Version='1.0.0.1402')");
            nuget.Filename.ShouldEqual("FubuMVC.Core.1.0.0.1402.nupkg");
        }
    }
}