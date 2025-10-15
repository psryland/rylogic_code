#! "net9.0"
#load "UserVars.csx"
#load "Tools.csx"
#nullable enable

// Execute unit tests
// Use:
//   dotnet-script RunTests.csx $(TargetPath)

using System;
using Console = System.Console;

// Set this to false to disable running tests on compiling
var RunTests = true;
//RunTests = False;

try
{
	List<string> args =
		//["E:\\Rylogic\\Code\\obj\\v143\\unittests\\x64\\Debug\\unittests.dll"]
		Environment.GetCommandLineArgs().Skip(2).ToList()
	;

	var target_path = args.Count > 0 ? args[0] : throw new Exception("TargetPath not provided");
	var target_extn = Path.GetExtension(target_path);

	// If the target is an exe, just run it
	if (!RunTests)
	{
		Console.WriteLine("   **** Unit tests not run ****   ");
	}
	else if (target_extn == ".exe")
	{
		Tools.Run([target_path], return_output: false);
	}
	else if (target_extn == ".dll")
	{
		var vstest = Tools.Path([UserVars.VSDir, "Common7\\IDE\\Extensions\\TestPlatform\\vstest.console.exe"]);
		Tools.Run([vstest, target_path, "--logger:console;verbosity=minimal", "--nologo"], return_output: false);
	}
}
catch (Exception ex)
{
	Console.Error.WriteLine($" TESTS FAILED : {ex.Message}");
}
