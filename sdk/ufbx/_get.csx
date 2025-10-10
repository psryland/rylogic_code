#! "net9.0"
#r "System.Diagnostics.Process"
#r "System.Net.Http"
#nullable enable

using System;
using System.Diagnostics;
using System.Net.Http;
using System.Runtime.CompilerServices;

// Repo
static string url = "https://github.com/ufbx/ufbx.git";

// Where the SDK should go
static string ThisDir = Path.GetDirectoryName(ThisFile())!;
static string SDKDir = Path.Join(ThisDir, "ufbx");

// Clone the repo
if (!Directory.Exists(SDKDir))
{
	var proc = new Process();
	proc.StartInfo.FileName = "git.exe";
	proc.StartInfo.Arguments = $"clone {url} {SDKDir}";
	proc.StartInfo.WorkingDirectory = ThisDir;
	proc.StartInfo.UseShellExecute = true;
	try
	{
		proc.Start();
		proc.WaitForExit();
	}
	catch (Exception ex)
	{
		Console.WriteLine($"Error: {ex.Message}");
	}

	// Switch to the 3.4 branch
	proc.StartInfo.FileName = "git.exe";
	proc.StartInfo.Arguments = $"checkout v0.20.1";
	proc.StartInfo.WorkingDirectory = SDKDir;
	try
	{
		proc.Start();
		proc.WaitForExit();
	}
	catch (Exception ex)
	{
		Console.WriteLine($"Error: {ex.Message}");
	}
}

// Returns the directory of *this* file
static string ThisFile([CallerFilePath] string sourceFilePath = "") => sourceFilePath;
