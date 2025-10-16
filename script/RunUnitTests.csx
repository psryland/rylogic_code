#! "net9.0"
#load "Tools.csx"
#nullable enable

// Run unit tests in a binary file (managed or native)
// Use: <Exec Command="dotnet-script $(ScriptPath)RunUnitTests.csx $(TargetPath) <is_managed> <dependency> <dependency> ..." />

using System;
using Console = System.Console;

try
{
	List<string> args =
		//["E:/Rylogic/Code/projects/rylogic/Rylogic.Core/bin/Debug/net9.0-windows/Rylogic.Core.dll", "true"]
		//["E:/Rylogic/Code/projects/rylogic/Rylogic.Windows/bin/Debug/net9.0-windows/Rylogic.Windows.dll", "true", "Rylogic.Core", "WindowsBase"]
		Environment.GetCommandLineArgs().Skip(2).ToList()
	;

	var target_path = args.Count > 0 ? args[0] : throw new Exception("TargetPath not provided");
	var is_managed = args.Count > 1 ? bool.Parse(args[1]) : throw new Exception("IsManaged not provided");
	Tools.UnitTest(target_path, is_managed, args[2..]);
}
catch (Exception ex)
{
	Console.Error.WriteLine(ex.Message);
}
