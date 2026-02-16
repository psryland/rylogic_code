#! "net9.0"
#r "System.Net.Http"
#nullable enable

using System;
using System.IO.Compression;
using System.Net.Http;
using System.Runtime.CompilerServices;
using Console = System.Console;

static string fluidsynth_zip = "fluidsynth-2.4.7-android24.zip";
static string fluidsynth_url = $"https://github.com/FluidSynth/fluidsynth/releases/download/v2.4.7/{fluidsynth_zip}";

// Where the SDK should go
static string ThisDir = Path.GetDirectoryName(ThisFile())!;
static string SDKDir = Path.Join(ThisDir, "fluidsynth");
static string ZipPath = Path.Join(ThisDir, fluidsynth_zip);

// If the sdk directory doesn't exist, download and extract
if (!Directory.Exists(SDKDir))
{
	if (!File.Exists(ZipPath))
	{
		Console.WriteLine($"Downloading {fluidsynth_zip}...");

		using var http = new HttpClient();
		http.DefaultRequestHeaders.UserAgent.ParseAdd("dotnet-script");
		using var response = await http.GetAsync(fluidsynth_url, HttpCompletionOption.ResponseHeadersRead);
		response.EnsureSuccessStatusCode();

		using var fs = File.Create(ZipPath);
		await response.Content.CopyToAsync(fs);

		Console.WriteLine("Download complete.");
	}

	Console.WriteLine($"Extracting to {SDKDir}...");
	ZipFile.ExtractToDirectory(ZipPath, SDKDir);

	Console.WriteLine("Done.");
}

// Returns the directory of *this* file
static string ThisFile([CallerFilePath] string sourceFilePath = "") => sourceFilePath;
