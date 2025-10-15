#! "net9.0"
#load "UserVars.csx"
#load "Tools.csx"
#nullable enable

// Post Build Event for exporting library files to a directory
// Use:
//   dotnet-script DeployLib.csx $(TargetPath)
// 
// Note: 
//  pdb files are associated with the file name at the time they are build so it is
//  not possible to rename the lib and pdb. 

using System;
using System.IO;
using System.Text.RegularExpressions;
using Console = System.Console;

try
{
	List<string> args =
		//["E:\\Rylogic\\Code\\obj\\v143\\view3d-12\\x64\\Debug\\view3d-12-static.lib"]
		Environment.GetCommandLineArgs().Skip(2).ToList()
	;

	// 'Target' is the build target (i.e. in the obj/ directory)
	var target_path = args.Count > 0 ? args[0] : throw new Exception("Target Path?");
	var m = Regex.Match(target_path, @"^(?<objdir>.*)[\\/](?<platform>.*?)[\\/](?<config>.*?)[\\/](?<target_name>.*?)$");
	if (!m.Success) throw new Exception($"Invalid $(TargetPath): {target_path}");

	Tools.DeployLib(
		m.Groups["target_name"].Value,
		m.Groups["objdir"].Value,
		[m.Groups["platform"].Value],
		[m.Groups["config"].Value]);
}
catch (Exception ex)
{
	Console.WriteLine(ex.Message);
}
