using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Xml;
using FubuCore;
using FubuCore.CommandLine;
using ripple.New;
using System.Collections.Generic;

namespace ripple.Extract
{
    [CommandDescription("Extracts all the latest nugets from a remote feed ")]
    public class ExtractCommand : FubuCommand<ExtractInput>
    {
        public const string Command =
            "/Packages()?$filter=IsAbsoluteLatestVersion&$orderby=DownloadCount%20desc,Id&$skip=0&$top=";

        public override bool Execute(ExtractInput input)
        {
            var system = new FileSystem();
            system.DeleteDirectory(input.Directory);
            system.CreateDirectory(input.Directory);

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var feed = new FloatingFeed(input.FeedFlag);


            var remoteNugets = feed.GetLatest().ToList();
            var i = 0;

            remoteNugets.Each(nuget => {
                nuget.DownloadTo(input.Directory);
                Console.WriteLine("Finished {0} of {1}", i++, remoteNugets.Count);
            });

            return true;
        }
    }
}