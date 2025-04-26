#! "net9.0"
#r "System.Diagnostics.Process"
#r "System.Net.Http"
#nullable enable

using System;
using System.Diagnostics;
using System.Net.Http;
using System.Runtime.CompilerServices;

// Download the FBX SDK from Autodesk
static string url = "https://damassets.autodesk.net/content/dam/autodesk/www/files/fbx202037_fbxsdk_vs2022_win.exe";

// Where the SDK should go
static string ThisDir = Path.GetDirectoryName(ThisFile())!;
static string SDKFilePath = Path.Join(ThisDir, "fbx202037_fbxsdk_vs2022_win.exe");
static string SDKDir = Path.Join(ThisDir, "fbx");

// Download the zip
if (!File.Exists(SDKFilePath))
{
	// Download the file
	using var client = new HttpClient();
	using var response = client.GetAsync(url).Result;
	using var stream = response.Content.ReadAsStreamAsync().Result;
	using var fileStream = File.Create(SDKFilePath);
	stream.CopyTo(fileStream);
}

// Expand the zip
if (!Directory.Exists(SDKDir))
{
	Directory.CreateDirectory(SDKDir);
	var proc = new Process();
	proc.StartInfo.FileName = SDKFilePath;
	proc.StartInfo.Arguments = $"/S /D={SDKDir}";
	proc.StartInfo.WorkingDirectory = ThisDir;
	proc.StartInfo.UseShellExecute = true;
	proc.StartInfo.Verb = "runas";
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
