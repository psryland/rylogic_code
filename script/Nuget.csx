#! "net9.0"
#r "System.Text.Json"
#r "nuget: Rylogic.Core, 1.0.4"
#load "UserVars.csx"
#load "Tools.csx"
#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Rylogic.Extn;
using IOPath = System.IO.Path;

public class Nuget
{
	// Notes:
	//  - Assembly signing, packaging, and publishing are all separate steps.
	public record class File(string Filepath, string Target, bool Sign)
	{
	}

	// Create a Nuget package from the given project file
	// Expects a file called 'package.nuspec' in the same directory as 'proj'
	// The resulting package will be in UserVars.root\lib\packages
	// The full package path is returned
	public static string Package(string proj, IEnumerable<File> files)
	{
		var proj_name = IOPath.GetFileNameWithoutExtension(proj) ?? throw new Exception("Could not read project filename");
		var proj_dir = IOPath.GetDirectoryName(proj) ?? throw new Exception("Could not read project directory");
		var obj_dir = Tools.Path([proj_dir, "obj"]);

		Tools.SetupVcEnvironment();
		var (_, version) = Tools.Run([UserVars.MSBuild, proj, "-getProperty:Version", "-nologo", "-verbosity:quiet"]);
		version = version.Trim();

		// Create a nuspec file based on the template
		var ns = "http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd";
		var xml_nuspec = XDocument.Load(Tools.Path([UserVars.Root, "build\\nuget\\rylogic.template.nuspec"])).Root ?? throw new Exception("Root element not found in nuspec template");;

		// Update metadata
		var xml_metadata = xml_nuspec.Element(XName.Get("metadata", ns)) ?? throw new Exception("metadata element not found in nuspec template");

		// Now you can update metadata elements, for example:
		xml_metadata.Element(XName.Get("id", ns))?.SetValue(proj_name);
		xml_metadata.Element(XName.Get("version", ns))?.SetValue(version); // or get from project file

		// Add each file to the spec
		var xml_files = xml_nuspec.Element(XName.Get("files", ns)) ?? throw new Exception("files element not found in nuspec template");
		foreach (var file in files)
		{
			// Check the file exists
			var filepath = Tools.Path([proj_dir, file.Filepath]);

			// Sign if required
			if (file.Sign)
				Tools.SignAssembly(filepath);

			// Add the file to the spec
			xml_files.Add2(new XElement(XName.Get("file", ns),
				new XAttribute("src", filepath),
				new XAttribute("target", file.Target)
				));
		}

		// Write the nuspec to 'objdir'
		var nuspec = Tools.Path([obj_dir, $"{proj_name}.nuspec"], check_exists: false);
		xml_nuspec.Save(nuspec);

		// Build the Nuget package directly in the lib\packages folder
		Tools.Run([UserVars.Nuget, "pack", nuspec, "-OutputDirectory", Tools.Path([UserVars.Root, "lib\\packages"], check_exists: false)]);

		// Sign the package
		var package_path = Tools.Path([UserVars.Root, $"lib\\packages\\{proj_name}.{version}.nupkg"]);
		Tools.Run([UserVars.Nuget, "sign", package_path, "-CertificatePath", UserVars.CodeSignCert_Pfx, "-CertificatePassword", UserVars.CodeSignCert_Pw, "-Timestamper", "http://timestamp.digicert.com"]);

		return package_path;
	}

	// Push a nugget package to 'nuget.org'
	public static void Publish(string package_path)
	{
		Tools.Run([UserVars.Nuget, "push", package_path, UserVars.NugetApiKey, "-source", "https://api.nuget.org/v3/index.json"]);
	}
}
