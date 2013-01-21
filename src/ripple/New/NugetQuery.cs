using System.Diagnostics;
using System.Reflection;

namespace ripple.New
{
    public class NugetQuery
    {
        public NugetQuery()
        {
            Stability = NugetStability.Anything;
        }

        public string Name { get; set; }
        public string Version { get; set; }
        public NugetStability Stability { get; set; }

        public override string ToString()
        {
            return string.Format("Name: {0}, Version: {1}, Stability: {2}", Name, Version, Stability);
        }
    }
}