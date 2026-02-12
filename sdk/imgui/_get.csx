#! "net9.0"
#r "System.Diagnostics.Process"
#r "System.Net.Http"
#nullable enable

using System;
using System.Diagnostics;
using System.Net.Http;
using System.Runtime.CompilerServices;

// Repo
static string url = "https://github.com/ocornut/imgui.git";

// Where the SDK should go
static string ThisDir = Path.GetDirectoryName(ThisFile())!;
static string SDKDir = Path.Join(ThisDir, "imgui");

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

	// Switch to latest stable release
	proc.StartInfo.FileName = "git.exe";
	proc.StartInfo.Arguments = $"checkout v1.92.5";
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
