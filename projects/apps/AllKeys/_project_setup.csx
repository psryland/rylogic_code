#! "net9.0"
#r "System.Diagnostics.Process"
#nullable enable

// Project Setup Script for 'AllKeys'

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

static string ThisDir = Path.GetDirectoryName(ThisFile())!;
static string SdkDir = Path.GetFullPath(Path.Join(ThisDir, "..", "..", "..", "sdk"));
static string FluidSynthDir = Path.Join(SdkDir, "fluidsynth", "fluidsynth");
static string JniLibsDir = Path.Join(ThisDir, "app", "src", "main", "jniLibs");
static string CppDir = Path.Join(ThisDir, "app", "src", "main", "cpp");

// Run the '_get.csx' script to make sure the FluidSynth binaries are downloaded
var get_script = Path.Join(SdkDir, "fluidsynth", "_get.csx");
if (!Directory.Exists(FluidSynthDir))
{
	Console.WriteLine("Running FluidSynth SDK download...");
	var proc = Process.Start(new ProcessStartInfo
	{
		FileName = "dotnet-script",
		Arguments = $"\"{get_script}\"",
		WorkingDirectory = Path.GetDirectoryName(get_script)!,
		UseShellExecute = false,
	})!;
	proc.WaitForExit();
	if (proc.ExitCode != 0)
		throw new Exception("Failed to download FluidSynth SDK");
}

// Copy the folders in the 'lib' directory to the 'jniLibs' directory
Console.WriteLine("Copying FluidSynth libs to jniLibs...");
CopyDirectory(Path.Join(FluidSynthDir, "lib"), JniLibsDir);

// Copy the headers in the 'include' directory to the 'cpp/include' directory
Console.WriteLine("Copying FluidSynth headers to cpp...");
CopyDirectory(Path.Join(FluidSynthDir, "include"), Path.Join(CppDir, "include"));

// Locate the Android NDK
var ndk_version = "27";
var clang_version = "18";
var android_sdk = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Android", "Sdk");
var ndk_root = Path.Join(android_sdk, "ndk");

var ndk_path = Directory.Exists(ndk_root)
	? Directory.GetDirectories(ndk_root)
		.Select(d => new DirectoryInfo(d))
		.Where(d => string.Compare(d.Name, ndk_version, StringComparison.Ordinal) >= 0)
		.OrderBy(d => d.Name)
		.FirstOrDefault()?.FullName
	: null;

if (ndk_path == null)
{
	Console.WriteLine($"Error: Could not find NDK >= {ndk_version} in {ndk_root}. Make sure the NDK is installed.");
	return;
}

var prebuilt_dir = Path.Join(ndk_path, "toolchains", "llvm", "prebuilt", "windows-x86_64", "lib", "clang", clang_version, "lib", "linux");
if (!Directory.Exists(prebuilt_dir))
{
	Console.WriteLine($"Error: Could not find prebuilt dir: {prebuilt_dir}. Update versions in this script if necessary.");
	return;
}

// Copy required .so files from the NDK to the 'jniLibs' directory
Console.WriteLine("Copying NDK libs to jniLibs...");
var ndk_libs = new[] { "libomp.so" };
var abi_map = new Dictionary<string, string>
{
	["aarch64"] = "arm64-v8a",
	["arm"]     = "armeabi-v7a",
	["i386"]    = "x86",
	["x86_64"]  = "x86_64",
};
foreach (var lib in ndk_libs)
{
	foreach (var (arch, abi) in abi_map)
	{
		var src = Path.Join(prebuilt_dir, arch, lib);
		var dst = Path.Join(JniLibsDir, abi, lib);
		Directory.CreateDirectory(Path.GetDirectoryName(dst)!);
		File.Copy(src, dst, overwrite: true);
	}
}

Console.WriteLine("Project setup complete.");

// --- Helpers ---

static void CopyDirectory(string src, string dst)
{
	// Remove conflicting file if a directory is needed
	if (File.Exists(dst))
		File.Delete(dst);

	Directory.CreateDirectory(dst);
	foreach (var file in Directory.GetFiles(src))
	{
		File.Copy(file, Path.Join(dst, Path.GetFileName(file)), overwrite: true);
	}
	foreach (var dir in Directory.GetDirectories(src))
	{
		CopyDirectory(dir, Path.Join(dst, Path.GetFileName(dir)));
	}
}

static string ThisFile([CallerFilePath] string sourceFilePath = "") => sourceFilePath;
