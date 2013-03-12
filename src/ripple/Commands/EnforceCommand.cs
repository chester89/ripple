using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using FubuCore.CommandLine;
using NuGet;
using ripple.Local;

namespace ripple.Commands
{
    public class EnforceInput: SolutionInput
    {
        public string PackageId { get; set; }
        public string DesiredVersion { get; set; }
    }

    [CommandDescription("changes all versions of a package across whole solution to the one provided")]
    public class EnforceCommand: FubuCommand<EnforceInput>
    {
        const string packagesNodeName = "packages";
        const string packageAttributeName = "package";
        const string versionAttributeName = "version";
        const string includeAttributeName = "Include";

        public override bool Execute(EnforceInput input)
        {
            SemanticVersion version;
            var projectToOldPackageVersion = new Dictionary<Project, string>();

            if (SemanticVersion.TryParse(input.DesiredVersion, out version))
            {
                input.FindSolutions().Each(solution =>
                {
                    int counter = 0;
                    solution.Projects.Each(proj =>
                    {
                        if (File.Exists(proj.PackagesFile()))
                        {
                            var nugets = NugetDependency.ReadFrom(proj.PackagesFile());
                            var oldVersionNumber = string.Empty;
                            
                            if (nugets.Any(x => x.Name == input.PackageId))
                            {
                                //0. edit the packages.config file
                                var configDocument = XDocument.Load(proj.PackagesFile());
                                var packageNode = configDocument.Descendants(packagesNodeName).SingleOrDefault()
                                    .Descendants(packageAttributeName).SingleOrDefault(el => el.Attribute("id").Value == input.PackageId);
                                Console.WriteLine("Editing package version");
                                oldVersionNumber = packageNode.Attribute(versionAttributeName).Value;
                                projectToOldPackageVersion.Add(proj, oldVersionNumber);
                                packageNode.Attribute(versionAttributeName).Value = input.DesiredVersion;
                                
                                configDocument.Save(proj.PackagesFile());
                                counter++;
                                Console.WriteLine("Project {0} packages.config - successfully changed {1} from version {2} to {3}", proj.ProjectName, input.PackageId, oldVersionNumber, input.DesiredVersion);
                            }
                        }
                    });

                    //1. remove old version binaries
                    var binaryDirectories = Directory.EnumerateDirectories(solution.PackagesFolder(), input.PackageId + ".?.?.*");
                    if (binaryDirectories.Any())
                    {
                        Console.WriteLine("Found these package directories: {0}", string.Join("; ", binaryDirectories));
                        foreach (var binaryDirectory in binaryDirectories)
                        {
                            Directory.Delete(binaryDirectory, true);
                            Console.WriteLine("Deleted {0}", binaryDirectory);
                        }
                    }

                    new RestoreCommand().Execute(new RestoreInput());

                    solution.Projects.Each(proj =>
                    {
                        //2. update .csproj file
                        var relativePackagePathFormat = string.Format(@"{0}.{1}\lib\{2}{0}.dll", input.PackageId, input.DesiredVersion, "{0}");
                        var referencePathForProjectFile = string.Empty;
                        var targetFrameworkFormat = "{0}" + proj.TargetFrameworkVersion +
                            (string.IsNullOrEmpty(proj.TargetFrameworkProfile) ? string.Empty : proj.TargetFrameworkProfile.ToLower()) + "\\";

                        const string shortFrameworkName = "net";
                        var frameworkSpecificPaths = new[] { shortFrameworkName, shortFrameworkName.ToLower() }.Select(x => string.Format(targetFrameworkFormat, x)).Concat(new[] { string.Empty });

                        //at first - try all paths that include concrete target frameworks; then, as a fallback, go for lib folder
                        foreach (var frameworkPathTail in frameworkSpecificPaths)
                        {
                            if (File.Exists(Path.Combine(solution.PackagesFolder(), string.Format(relativePackagePathFormat, frameworkPathTail))))
                            {
                                referencePathForProjectFile = string.Format(relativePackagePathFormat, frameworkPathTail);
                            }
                        }

                        //later refactor to CsProjFile.UpdatePackageReference(string packageId, string oldVersion, string newVersion) or smth like that
                        var projectFile = XDocument.Load(proj.ProjectFile);
                        var packageReferenceNode = projectFile.Root
                            .DescendantsWithLocalName("ItemGroup").FirstOrDefault(itemGroupNode => itemGroupNode.DescendantsWithLocalName("Reference").Any())
                            .DescendantsWithLocalName("Reference").SingleOrDefault(x => x.Attribute(includeAttributeName).Value.Contains(input.PackageId + ", ")
                                || x.Attribute(includeAttributeName).Value == input.PackageId);
                        //change package version in the include attribute
                        var oldIncludeAttributeValue = packageReferenceNode.Attribute(includeAttributeName).Value;
                        var oldVersionNumber = projectToOldPackageVersion.ContainsKey(proj) ? projectToOldPackageVersion[proj] : "some random string";

                        packageReferenceNode.Attribute(includeAttributeName).SetValue(oldIncludeAttributeValue.Replace(oldVersionNumber, input.DesiredVersion));
                        //change package version in the HintPath element
                        var hintPathElement = packageReferenceNode.DescendantsWithLocalName("HintPath").SingleOrDefault();
                        hintPathElement.SetValue(@"..\packages\" + referencePathForProjectFile);
                        projectFile.Save(proj.ProjectFile);
                        Console.WriteLine("Successfully updated project reference in {0} project to {1} version of {2} package", proj.ProjectName, input.DesiredVersion, input.PackageId);
                    });


                    if (counter == 0)
                    {
                        Console.WriteLine("Couldn't find package {0} referenced in any project of {1} solution", input.PackageId, solution.Name);
                    }
                });
                return true;
            }
            Console.WriteLine("The version number you provided ({0}) is invalid", input.DesiredVersion);
            return false;
        }
    }
}
