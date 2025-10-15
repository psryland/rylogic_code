#! "net9.0"
#load "Tools.csx"
#nullable enable

// Use: <Exec Command="dotnet-script $(ScriptPath)RunUnitTests.csx $(TargetPath) <dependency> <dependency> ..." />
using System;
using Console = System.Console;

try
{
	var args =
		//["E:/Rylogic/Code/projects/rylogic/Rylogic.Core/bin/Debug/net9.0-windows/Rylogic.Core.dll"]
		Environment.GetCommandLineArgs().Skip(2)
	;

	Tools.UnitTest(args[0], args[1..])
}
catch (Exception ex)
{
	Console.Error.WriteLine(ex.Message);
}
