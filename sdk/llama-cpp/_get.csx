#! "net9.0"
#r "System.Diagnostics.Process"
#r "System.Net.Http"
#nullable enable

using System;
using System.Diagnostics;
using System.Net.Http;
using System.Runtime.CompilerServices;

// Repo
static string url = "https://github.com/ggml-org/llama.cpp.git";

// Where the SDK should go
static string ThisDir = Path.GetDirectoryName(ThisFile())!;
static string SDKDir = Path.Join(ThisDir, "llama.cpp");

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
	proc.StartInfo.Arguments = $"checkout b8061";
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

// Build static libraries via CMake if not already built
var buildDir = Path.Join(SDKDir, "build");
var libFile = Path.Join(buildDir, "bin", "Release", "llama.lib");
if (!File.Exists(libFile))
{
	Console.WriteLine("Building llama.cpp static libraries...");
	Directory.CreateDirectory(buildDir);

	var proc = new Process();
	proc.StartInfo.UseShellExecute = true;

	// Configure with static CRT (/MT) to match the Rylogic build configuration
	proc.StartInfo.FileName = "cmake.exe";
	proc.StartInfo.Arguments = $"-S \"{SDKDir}\" -B \"{buildDir}\" -DBUILD_SHARED_LIBS=OFF -DLLAMA_BUILD_EXAMPLES=OFF -DLLAMA_BUILD_TESTS=OFF -DLLAMA_BUILD_SERVER=OFF -DCMAKE_MSVC_RUNTIME_LIBRARY=MultiThreaded -DCMAKE_POLICY_DEFAULT_CMP0091=NEW";
	proc.StartInfo.WorkingDirectory = SDKDir;
	try
	{
		proc.Start();
		proc.WaitForExit();
		if (proc.ExitCode != 0) throw new Exception("CMake configure failed");
	}
	catch (Exception ex)
	{
		Console.WriteLine($"Error: {ex.Message}");
	}

	// Build
	proc.StartInfo.FileName = "cmake.exe";
	proc.StartInfo.Arguments = $"--build \"{buildDir}\" --config Release -j 8";
	proc.StartInfo.WorkingDirectory = SDKDir;
	try
	{
		proc.Start();
		proc.WaitForExit();
		if (proc.ExitCode != 0) throw new Exception("CMake build failed");
	}
	catch (Exception ex)
	{
		Console.WriteLine($"Error: {ex.Message}");
	}
}

Console.WriteLine("llama.cpp SDK ready.");

// Returns the directory of *this* file
static string ThisFile([CallerFilePath] string sourceFilePath = "") => sourceFilePath;
