using FubuCore;
using NUnit.Framework;
using NuGet;
using ripple.New;
using FubuTestingSupport;
using System.Linq;

namespace ripple.Testing.New
{
    [TestFixture]
    public class NugetFileTester
    {
        [Test]
        public void build_a_nuget_file_by_file_name()
        {
            var file = new NugetFile(@"c:\nugets\Bottles.1.0.0.441.nupkg");
            file.Name.ShouldEqual("Bottles");
            file.Version.ShouldEqual(SemanticVersion.Parse("1.0.0.441"));
            file.IsPreRelease.ShouldBeFalse();
        }

        [Test]
        public void build_a_nuget_file_by_file_name_for_alpha()
        {
            var file = new NugetFile(@"c:\nugets\Bottles.1.0.0.441-alpha.nupkg");
            file.Name.ShouldEqual("Bottles");
            file.Version.ShouldEqual(SemanticVersion.Parse("1.0.0.441-alpha"));
            file.IsPreRelease.ShouldBeTrue();
        }

        [Test]
        public void explode_smoke_test()
        {
            var file = new NugetFile("Bottles.1.0.0.441.nupkg");
            var system = new FileSystem();
            system.CreateDirectory("bottles");
            system.CleanDirectory("bottles");

            var package = file.ExplodeTo("bottles");

            package.ShouldNotBeNull();
            package.AssemblyReferences.Single().Name.ShouldEqual("Bottles.dll");
        }
    }
}