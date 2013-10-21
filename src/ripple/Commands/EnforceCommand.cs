using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using FubuCore.CommandLine;
using NuGet;
using ripple.Local;
using ripple.Nuget;

namespace ripple.Commands
{
    public class EnforceInput: SolutionInput
    {
        [Description("Id of a package you want want to change version of")]
        public String PackageId { get; set; }
        [Description("Version to change to")]
        public String DesiredVersion { get; set; }
        [Description("use this one if you want diagnostics messages to show up")]
        public Boolean VerboseFlag { get; set; }
    }

    [CommandDescription("changes all versions of a package across whole solution to the one provided")]
    public class EnforceCommand: FubuCommand<EnforceInput>
    {
        const string hintPathAttributeName = "HintPath";
        const string referenceNodeName = "Reference";
        const string packagesNodeName = "packages";
        const string packageAttributeName = "package";
        const string versionAttributeName = "version";
        const string includeAttributeName = "Include";

        public override bool Execute(EnforceInput input)
        {
            SemanticVersion version;
            var projectToOldPackageVersion = new Dictionary<Project, string>();
            var textWriter = input.VerboseFlag? Console.Out : new StringWriter();
            using(textWriter)
            {
                if (SemanticVersion.TryParse(input.DesiredVersion, out version))
                {
                    input.FindSolutions().Each(solution =>
                    {
                        int counter = 0;
                        solution.Projects.Where(pr => pr.NugetDependencies.Any(nd => nd.Name == input.PackageId)).Each(proj =>
                        {
                            if (File.Exists(proj.PackagesFile()))
                            {
                                //0. edit the packages.config file
                                var configDocument = XDocument.Load(proj.PackagesFile());
                                var packageNode = configDocument.Descendants(packagesNodeName).SingleOrDefault()
                                    .Descendants(packageAttributeName).SingleOrDefault(el => el.Attribute("id").Value == input.PackageId);
                                var oldVersionNumber = packageNode.Attribute(versionAttributeName).Value;
                                projectToOldPackageVersion.Add(proj, oldVersionNumber);
                                packageNode.Attribute(versionAttributeName).Value = input.DesiredVersion;

                                configDocument.Save(proj.PackagesFile());
                                counter++;
                                textWriter.WriteLine("Project {0} packages.config - successfully changed {1} from version {2} to {3}", proj.ProjectName, input.PackageId, oldVersionNumber, input.DesiredVersion);
                            }
                        });

                        //1. remove old version binaries
                        var binaryDirectories = Directory.EnumerateDirectories(solution.PackagesFolder(), input.PackageId + ".?.?.*");
                        if (binaryDirectories.Any())
                        {
                            foreach (var binaryDirectory in binaryDirectories)
                            {
                                Directory.Delete(binaryDirectory, true);
                            }
                        }

                        new RestoreCommand().Execute(new RestoreInput());

                        var repoBuilder = new PackageRepositoryBuilder();
                        var aggregateRepository = repoBuilder.BuildRemote(new [] { "http://packages.nuget.org/v1/FeedService.svc/", "http://nuget.org/api/v2" });
                        var requiredPackageVersion = aggregateRepository.FindPackagesById(input.PackageId).FirstOrDefault(x => x.Version == version);

                        foreach (var assemblyReference in requiredPackageVersion.AssemblyReferences)
                        {
                            solution.Projects.Where(pr => pr.NugetDependencies.Any(x => x.Name == input.PackageId)).Each(proj =>
                            {
                                var referencePathForProjectFile = assemblyReference.Path;

                                var projectFile = XDocument.Load(proj.ProjectFile);
                                var packageReferenceNode = projectFile.Root
                                    .DescendantsWithLocalName("ItemGroup").FirstOrDefault(itemGroupNode => itemGroupNode.DescendantsWithLocalName(referenceNodeName).Any())
                                    .DescendantsWithLocalName(referenceNodeName).SingleOrDefault(x => x.Attribute(includeAttributeName).Value.Contains(input.PackageId + ", ")
                                        || x.Attribute(includeAttributeName).Value == input.PackageId);

                                if (packageReferenceNode != null)
                                {
                                    //var oldIncludeAttributeValue = packageReferenceNode.Attribute(includeAttributeName).Value;

                                    //var versionTokens = oldIncludeAttributeValue.Split(new[] { ',' }).Skip(1).Select(x => x.Trim()).Where(s => s.StartsWith("Version")).ToList();
                                    //var oldVersionNumberAccordingToProjectFile = "SomethingMore";
                                    //if (versionTokens.Any())
                                    //{
                                    //    oldVersionNumberAccordingToProjectFile = versionTokens.First().Split(new[] { '=' }).Last();
                                    //}

                                    //packageReferenceNode.Attribute(includeAttributeName).SetValue(oldIncludeAttributeValue.Replace(oldVersionNumberAccordingToProjectFile, input.DesiredVersion));
                                    var hintPathElement = packageReferenceNode.DescendantsWithLocalName(hintPathAttributeName).SingleOrDefault();
                                    hintPathElement.SetValue(@"..\packages\" + string.Format("{0}.{1}\\", input.PackageId, input.DesiredVersion) + referencePathForProjectFile);
                                }
                                else
                                {
                                    var referencesRoot = projectFile.Root.DescendantsWithLocalName("ItemGroup").FirstOrDefault(itemGroupNode => itemGroupNode.DescendantsWithLocalName(referenceNodeName).Any());

                                    var newReferenceNode = new XElement(referenceNodeName);
                                    var assembly = Assembly.LoadFile(Path.Combine(solution.PackagesFolder(), assemblyReference.Path));
                                    var cultureInfo = string.IsNullOrEmpty(assembly.GetName().CultureInfo.Name) ? "neutral" : assembly.GetName().CultureInfo.ToString();
                                    var assemblyVersion = FileVersionInfo.GetVersionInfo(assembly.Location).FileVersion;
                                    var keyToken = Convert.ToString(assembly.GetName().GetPublicKeyToken());
                                    var processorArchitecture = assembly.GetName().ProcessorArchitecture.ToString();
                                    newReferenceNode.SetAttributeValue(includeAttributeName, string.Format("{0}, Version={1}, Culture={2}, PublicKeyToken={3}, processorArchitecture={4}", 
                                        assemblyReference.Name, assemblyVersion, cultureInfo, keyToken, processorArchitecture));
                                    newReferenceNode.Add(new XElement(hintPathAttributeName, @"..\packages" + string.Format("{0}.{1}\\", input.PackageId, input.DesiredVersion) + referencePathForProjectFile));
                                    referencesRoot.Add(newReferenceNode);
                                }

                                projectFile.Save(proj.ProjectFile);
                                textWriter.WriteLine("Successfully updated project reference in {0} project to {1} version of {2} package", proj.ProjectName, input.DesiredVersion, input.PackageId);
                            });
                        }

                        if (counter == 0)
                        {
                            textWriter.WriteLine("Couldn't find package {0} referenced in any project of {1} solution", input.PackageId, solution.Name);
                        }
                    });
                    return true;
                }
                textWriter.WriteLine("The version number you provided ({0}) is invalid", input.DesiredVersion);
                return false;
            }
        }
    }
}
