using NUnit.Framework;
using ripple.New;
using FubuTestingSupport;

namespace ripple.Testing.New
{
    [TestFixture]
    public class RemoteNugetTester
    {
        [Test]
        public void get_file_name_with_special_version()
        {
            var nuget = new RemoteNuget("FubuMVC.Core", "1.0.0.1442-alpha", "http://something");

            nuget.Filename.ShouldEqual("FubuMVC.Core.1.0.0.1442-alpha.nupkg");
        }
    }
}