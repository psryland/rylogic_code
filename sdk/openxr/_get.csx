#! "net9.0"
#r "System.Diagnostics.Process"
#r "System.Net.Http"
#nullable enable

using System;
using System.Diagnostics;
using System.IO.Compression;
using System.Net.Http;
using System.Runtime.CompilerServices;
using Console = System.Console;

// Repo
static string url = "https://github.com/KhronosGroup/OpenXR-SDK-Source/releases/download/release-1.1.53/openxr_loader_windows-1.1.53.zip";

// Where the SDK should go
static string ThisDir = Path.GetDirectoryName(ThisFile())!;
static string SDKDir = Path.Join(ThisDir, "openxr");

// Clone the repo
if (!Directory.Exists(SDKDir))
{
	// Download the file
	using var client = new HttpClient();
	var zipPath = Path.Join(ThisDir, "openxr.zip");

	Console.WriteLine($"Downloading {url}...");
	var response = await client.GetAsync(url);
	response.EnsureSuccessStatusCode();
	
	await using var fs = new FileStream(zipPath, FileMode.Create);
	await response.Content.CopyToAsync(fs);
	fs.Close();

	Console.WriteLine("Download complete.");

	// Unzip the file
	Console.WriteLine($"Extracting to {SDKDir}...");
	ZipFile.ExtractToDirectory(zipPath, SDKDir);
	Console.WriteLine("Extraction complete.");

	// Clean up
	File.Delete(zipPath);
}

// Returns the directory of *this* file
static string ThisFile([CallerFilePath] string sourceFilePath = "") => sourceFilePath;
