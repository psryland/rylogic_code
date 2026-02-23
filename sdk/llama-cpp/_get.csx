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
static string RylogicRoot = Path.GetFullPath(Path.Join(ThisDir, "..", ".."));

// Check if our Vulkan SDK is available (built by sdk/vulkan/_get.csx)
static string VulkanSDKDir = Path.Join(RylogicRoot, "sdk", "vulkan");
static bool HasVulkan =
	File.Exists(Path.Join(VulkanSDKDir, "lib", "vulkan-1.lib")) &&
	Directory.Exists(Path.Join(VulkanSDKDir, "include", "vulkan")) &&
	File.Exists(Path.Join(VulkanSDKDir, "glslc", "glslc.exe"));

// Build the Vulkan CMake flags if available
static string VulkanFlags()
{
	if (!HasVulkan) return "";

	var includeDir = Path.Join(VulkanSDKDir, "include");
	var libDir = Path.Join(VulkanSDKDir, "lib");
	var glslcExe = Path.Join(VulkanSDKDir, "glslc", "glslc.exe");

	return $"-DGGML_VULKAN=ON " +
		$"-DVulkan_INCLUDE_DIR=\"{includeDir}\" " +
		$"-DVulkan_LIBRARY=\"{Path.Join(libDir, "vulkan-1.lib")}\" " +
		$"-DVulkan_GLSLC_EXECUTABLE=\"{glslcExe}\" ";
}

// Clone the repo
if (!Directory.Exists(SDKDir))
{
	Run("git.exe", $"clone {url} {SDKDir}", ThisDir);
	Run("git.exe", $"checkout b8061", SDKDir);
}

if (HasVulkan)
	Console.WriteLine("Vulkan SDK detected — building with GPU support.");
else
	Console.WriteLine("No Vulkan SDK found — building CPU-only. Run sdk/vulkan/_get.csx first for GPU support.");

// Build static libraries (Release)
var buildDir = Path.Join(SDKDir, "build");
var libFile = Path.Join(buildDir, "src", "Release", "llama.lib");
if (!File.Exists(libFile))
{
	Console.WriteLine("Building llama.cpp static libraries (Release)...");
	Directory.CreateDirectory(buildDir);

	var vulkan = VulkanFlags();
	Run("cmake.exe",
		$"-S \"{SDKDir}\" -B \"{buildDir}\" " +
		$"-DBUILD_SHARED_LIBS=OFF -DLLAMA_BUILD_EXAMPLES=OFF -DLLAMA_BUILD_TESTS=OFF -DLLAMA_BUILD_SERVER=OFF " +
		$"-DCMAKE_MSVC_RUNTIME_LIBRARY=MultiThreaded -DCMAKE_POLICY_DEFAULT_CMP0091=NEW " +
		vulkan,
		SDKDir);

	Run("cmake.exe", $"--build \"{buildDir}\" --config Release -j 8", SDKDir);
}

// Build static libraries (Debug)
var debugBuildDir = Path.Join(SDKDir, "build-debug");
var debugLibFile = Path.Join(debugBuildDir, "src", "Debug", "llama.lib");
if (!File.Exists(debugLibFile))
{
	Console.WriteLine("Building llama.cpp static libraries (Debug)...");
	Directory.CreateDirectory(debugBuildDir);

	var vulkan = VulkanFlags();
	Run("cmake.exe",
		$"-S \"{SDKDir}\" -B \"{debugBuildDir}\" " +
		$"-DBUILD_SHARED_LIBS=OFF -DLLAMA_BUILD_EXAMPLES=OFF -DLLAMA_BUILD_TESTS=OFF -DLLAMA_BUILD_SERVER=OFF " +
		$"-DCMAKE_MSVC_RUNTIME_LIBRARY=MultiThreadedDebug -DCMAKE_POLICY_DEFAULT_CMP0091=NEW " +
		$"-DCMAKE_C_FLAGS_DEBUG=\"/MTd /Zi /Ob0 /Od /RTC1 /D_ITERATOR_DEBUG_LEVEL=1\" " +
		$"-DCMAKE_CXX_FLAGS_DEBUG=\"/MTd /Zi /Ob0 /Od /RTC1 /D_ITERATOR_DEBUG_LEVEL=1\" " +
		vulkan,
		SDKDir);

	Run("cmake.exe", $"--build \"{debugBuildDir}\" --config Debug -j 8", SDKDir);
}

Console.WriteLine("llama.cpp SDK ready.");

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

static string ThisFile([CallerFilePath] string sourceFilePath = "") => sourceFilePath;
