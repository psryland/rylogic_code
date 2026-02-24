#! "net9.0"
#r "System.Diagnostics.Process"
#r "System.Net.Http"
#r "System.IO.Compression"
#r "System.IO.Compression.ZipFile"
#nullable enable

using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Runtime.CompilerServices;

// Vulkan SDK version to target
static string VulkanVersion = "1.4.309";
static string VulkanHeadersTag = $"v{VulkanVersion}";
static string VulkanLoaderTag = $"v{VulkanVersion}";

// Where the SDK should go
static string ThisDir = Path.GetDirectoryName(ThisFile())!;
static string HeadersDir = Path.Join(ThisDir, "Vulkan-Headers");
static string LoaderDir = Path.Join(ThisDir, "Vulkan-Loader");

// Output directory structure (what consumers reference)
static string IncludeDir = Path.Join(ThisDir, "include");
static string LibDir = Path.Join(ThisDir, "lib");

// ===========================================================
// Step 1: Clone and CMake-install Vulkan-Headers
// ===========================================================
var headersInstallDir = Path.Join(ThisDir, "headers-install");
if (!Directory.Exists(HeadersDir))
{
	Console.WriteLine($"Cloning Vulkan-Headers ({VulkanHeadersTag})...");
	Run("git.exe", $"clone --depth 1 --branch {VulkanHeadersTag} https://github.com/KhronosGroup/Vulkan-Headers.git {HeadersDir}", ThisDir);
}
if (!Directory.Exists(headersInstallDir))
{
	Console.WriteLine("Installing Vulkan-Headers via CMake...");
	var headersBuild = Path.Join(HeadersDir, "build");
	Directory.CreateDirectory(headersBuild);
	Run("cmake.exe", $"-S \"{HeadersDir}\" -B \"{headersBuild}\" -DCMAKE_INSTALL_PREFIX=\"{headersInstallDir}\"", HeadersDir);
	Run("cmake.exe", $"--install \"{headersBuild}\"", HeadersDir);
}

// ===========================================================
// Step 2: Clone and build Vulkan-Loader (produces vulkan-1.lib)
// ===========================================================
var loaderLib = Path.Join(LibDir, "vulkan-1.lib");
if (!File.Exists(loaderLib))
{
	if (!Directory.Exists(LoaderDir))
	{
		Console.WriteLine($"Cloning Vulkan-Loader ({VulkanLoaderTag})...");
		Run("git.exe", $"clone --depth 1 --branch {VulkanLoaderTag} https://github.com/KhronosGroup/Vulkan-Loader.git {LoaderDir}", ThisDir);
	}

	Console.WriteLine("Building Vulkan-Loader...");
	var buildDir = Path.Join(LoaderDir, "build");
	Directory.CreateDirectory(buildDir);

	// Point the loader build at our CMake-installed headers
	Run("cmake.exe",
		$"-S \"{LoaderDir}\" -B \"{buildDir}\" " +
		$"-DVULKAN_HEADERS_INSTALL_DIR=\"{headersInstallDir}\" " +
		$"-DCMAKE_PREFIX_PATH=\"{headersInstallDir}\" " +
		$"-DUPDATE_DEPS=OFF " +
		$"-DBUILD_TESTS=OFF",
		LoaderDir);

	Run("cmake.exe", $"--build \"{buildDir}\" --config Release -j 8", LoaderDir);

	// Copy the built library to our lib/ directory
	Directory.CreateDirectory(LibDir);
	var builtLib = Path.Join(buildDir, "loader", "Release", "vulkan-1.lib");
	if (!File.Exists(builtLib))
	{
		// Try alternate path
		builtLib = Path.Join(buildDir, "Release", "vulkan-1.lib");
	}
	if (File.Exists(builtLib))
	{
		File.Copy(builtLib, loaderLib, overwrite: true);
		Console.WriteLine($"  vulkan-1.lib -> {loaderLib}");
	}
	else
	{
		Console.WriteLine($"WARNING: Could not find built vulkan-1.lib. Searched:");
		Console.WriteLine($"  {Path.Join(buildDir, "loader", "Release", "vulkan-1.lib")}");
		Console.WriteLine($"  {Path.Join(buildDir, "Release", "vulkan-1.lib")}");
	}
}

// ===========================================================
// Step 3: Set up the include directory from installed headers
// ===========================================================
var vulkanInclude = Path.Join(IncludeDir, "vulkan");
if (!Directory.Exists(vulkanInclude))
{
	Console.WriteLine("Setting up include directory...");
	Directory.CreateDirectory(IncludeDir);

	var srcVulkan = Path.Join(headersInstallDir, "include", "vulkan");
	var srcVkVideo = Path.Join(headersInstallDir, "include", "vk_video");

	CopyDirectory(srcVulkan, vulkanInclude);
	if (Directory.Exists(srcVkVideo))
		CopyDirectory(srcVkVideo, Path.Join(IncludeDir, "vk_video"));

	Console.WriteLine($"  Headers -> {IncludeDir}");
}

Console.WriteLine();
Console.WriteLine("Vulkan SDK ready.");
Console.WriteLine($"  Include: {IncludeDir}");
Console.WriteLine($"  Lib:     {LibDir}");

// ===========================================================
// Step 4: Download glslc (shader compiler, needed by llama.cpp Vulkan backend)
// ===========================================================
var glslcDir = Path.Join(ThisDir, "glslc");
var glslcExe = Path.Join(glslcDir, "glslc.exe");
if (!File.Exists(glslcExe))
{
	Console.WriteLine("Downloading glslc (shaderc) for Vulkan shader compilation...");
	Directory.CreateDirectory(glslcDir);

	// Google's prebuilt shaderc for Windows VS2022 x64 Release
	var shadercUrl = "https://storage.googleapis.com/shaderc/artifacts/prod/graphics_shader_compiler/shaderc/windows/vs2022_amd64_release_continuous/35/20260126-070305/install.zip";
	var zipPath = Path.Join(glslcDir, "shaderc.zip");

	using (var client = new HttpClient())
	{
		client.Timeout = TimeSpan.FromMinutes(10);
		Console.WriteLine("  Downloading...");
		var data = client.GetByteArrayAsync(shadercUrl).Result;
		File.WriteAllBytes(zipPath, data);
	}

	Console.WriteLine("  Extracting...");
	ZipFile.ExtractToDirectory(zipPath, glslcDir, overwriteFiles: true);
	File.Delete(zipPath);

	// The zip extracts to install/bin/glslc.exe — move it up
	var extractedGlslc = Path.Join(glslcDir, "install", "bin", "glslc.exe");
	if (File.Exists(extractedGlslc))
	{
		File.Copy(extractedGlslc, glslcExe, overwrite: true);
		Console.WriteLine($"  glslc.exe -> {glslcExe}");
	}
	else
	{
		// Search for it
		foreach (var f in Directory.GetFiles(glslcDir, "glslc.exe", SearchOption.AllDirectories))
		{
			File.Copy(f, glslcExe, overwrite: true);
			Console.WriteLine($"  glslc.exe -> {glslcExe}");
			break;
		}
	}

	// Clean up extracted directories (keep only glslc.exe)
	var installDir = Path.Join(glslcDir, "install");
	if (Directory.Exists(installDir))
		Directory.Delete(installDir, recursive: true);
}

Console.WriteLine($"  glslc:   {glslcExe}");

// ===========================================================
// Helpers
// ===========================================================

static void Run(string exe, string args, string workDir)
{
	var proc = new Process();
	proc.StartInfo.FileName = exe;
	proc.StartInfo.Arguments = args;
	proc.StartInfo.WorkingDirectory = workDir;
	proc.StartInfo.UseShellExecute = true;
	proc.Start();
	proc.WaitForExit();
	if (proc.ExitCode != 0)
		throw new Exception($"{exe} exited with code {proc.ExitCode}");
}

static void CopyDirectory(string src, string dst)
{
	Directory.CreateDirectory(dst);
	foreach (var file in Directory.GetFiles(src))
		File.Copy(file, Path.Join(dst, Path.GetFileName(file)), overwrite: true);
	foreach (var dir in Directory.GetDirectories(src))
		CopyDirectory(dir, Path.Join(dst, Path.GetFileName(dir)));
}

static string ThisFile([CallerFilePath] string sourceFilePath = "") => sourceFilePath;
