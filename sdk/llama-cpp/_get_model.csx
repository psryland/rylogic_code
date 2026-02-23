#! "net9.0"
#r "System.Net.Http"
#nullable enable

using System;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

// Where this script lives and where models are stored
static string ThisDir = Path.GetDirectoryName(ThisFile())!;
static string ModelsDir = Path.Join(ThisDir, "models");

// Curated list of popular small GGUF chat models suitable for game NPCs
// Format: { Name, HuggingFace URL, Approximate size, Description }
var models = new (string Name, string Url, string Size, string Description)[]
{
	(
		"Phi-3-mini-4k-instruct-q4.gguf",
		"https://huggingface.co/microsoft/Phi-3-mini-4k-instruct-gguf/resolve/main/Phi-3-mini-4k-instruct-q4.gguf",
		"~2.2 GB",
		"Microsoft Phi-3 Mini (3.8B params, Q4). Fast, good quality for its size."
	),
	(
		"Phi-3.5-mini-instruct-Q4_K_M.gguf",
		"https://huggingface.co/bartowski/Phi-3.5-mini-instruct-GGUF/resolve/main/Phi-3.5-mini-instruct-Q4_K_M.gguf",
		"~2.3 GB",
		"Microsoft Phi-3.5 Mini (3.8B params, Q4_K_M). Improved over Phi-3."
	),
	(
		"Llama-3.2-3B-Instruct-Q4_K_M.gguf",
		"https://huggingface.co/bartowski/Llama-3.2-3B-Instruct-GGUF/resolve/main/Llama-3.2-3B-Instruct-Q4_K_M.gguf",
		"~2.0 GB",
		"Meta Llama 3.2 (3B params, Q4_K_M). Small and capable."
	),
	(
		"Qwen2.5-3B-Instruct-Q4_K_M.gguf",
		"https://huggingface.co/Qwen/Qwen2.5-3B-Instruct-GGUF/resolve/main/qwen2.5-3b-instruct-q4_k_m.gguf",
		"~2.1 GB",
		"Alibaba Qwen 2.5 (3B params, Q4_K_M). Strong multilingual support."
	),
	(
		"Mistral-7B-Instruct-v0.3-Q4_K_M.gguf",
		"https://huggingface.co/bartowski/Mistral-7B-Instruct-v0.3-GGUF/resolve/main/Mistral-7B-Instruct-v0.3-Q4_K_M.gguf",
		"~4.4 GB",
		"Mistral 7B Instruct v0.3 (7B params, Q4_K_M). High quality, needs more RAM."
	),
	(
		"gemma-2-2b-it-Q4_K_M.gguf",
		"https://huggingface.co/bartowski/gemma-2-2b-it-GGUF/resolve/main/gemma-2-2b-it-Q4_K_M.gguf",
		"~1.6 GB",
		"Google Gemma 2 (2B params, Q4_K_M). Tiny and fast, good for NPCs."
	),
	(
		"SmolLM2-1.7B-Instruct-Q4_K_M.gguf",
		"https://huggingface.co/bartowski/SmolLM2-1.7B-Instruct-GGUF/resolve/main/SmolLM2-1.7B-Instruct-Q4_K_M.gguf",
		"~1.1 GB",
		"HuggingFace SmolLM2 (1.7B params, Q4_K_M). Very small, good for testing."
	),
};

// Ensure models directory exists
Directory.CreateDirectory(ModelsDir);

// Display available models
Console.WriteLine();
Console.WriteLine("=== Available GGUF Models ===");
Console.WriteLine();

for (int i = 0; i < models.Length; i++)
{
	var model = models[i];
	var local_path = Path.Join(ModelsDir, model.Name);
	var downloaded = File.Exists(local_path);
	var status = downloaded ? "[DOWNLOADED]" : "[not downloaded]";

	Console.WriteLine($"  {i + 1}. {model.Name}");
	Console.WriteLine($"     {model.Description}");
	Console.WriteLine($"     Size: {model.Size}  {status}");
	if (downloaded)
	{
		var file_info = new FileInfo(local_path);
		Console.WriteLine($"     Local: {file_info.Length / (1024.0 * 1024.0):F1} MB");
	}
	Console.WriteLine();
}

Console.Write("Enter model number to download (or 'q' to quit): ");
var input = Console.ReadLine()?.Trim() ?? "";

if (input.Equals("q", StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(input))
{
	Console.WriteLine("No model selected.");
	return;
}

if (!int.TryParse(input, out var choice) || choice < 1 || choice > models.Length)
{
	Console.WriteLine($"Invalid selection. Enter a number between 1 and {models.Length}.");
	return;
}

var selected = models[choice - 1];
var dest_path = Path.Join(ModelsDir, selected.Name);

if (File.Exists(dest_path))
{
	Console.Write($"'{selected.Name}' already exists. Re-download? (y/N): ");
	var confirm = Console.ReadLine()?.Trim() ?? "";
	if (!confirm.Equals("y", StringComparison.OrdinalIgnoreCase))
	{
		Console.WriteLine("Skipped.");
		return;
	}
}

Console.WriteLine($"\nDownloading '{selected.Name}' ({selected.Size})...");
Console.WriteLine($"  From: {selected.Url}");
Console.WriteLine($"  To:   {dest_path}");
Console.WriteLine();

try
{
	using var client = new HttpClient();
	client.Timeout = TimeSpan.FromHours(2);

	using var response = await client.GetAsync(selected.Url, HttpCompletionOption.ResponseHeadersRead);
	response.EnsureSuccessStatusCode();

	var total_bytes = response.Content.Headers.ContentLength ?? -1;
	using var stream = await response.Content.ReadAsStreamAsync();
	using var file_stream = File.Create(dest_path);

	var buffer = new byte[1024 * 1024]; // 1 MB buffer
	long bytes_downloaded = 0;
	int bytes_read;
	var last_report = DateTime.MinValue;

	while ((bytes_read = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
	{
		await file_stream.WriteAsync(buffer, 0, bytes_read);
		bytes_downloaded += bytes_read;

		// Report progress every 2 seconds
		if ((DateTime.UtcNow - last_report).TotalSeconds >= 2)
		{
			last_report = DateTime.UtcNow;
			if (total_bytes > 0)
			{
				var pct = (double)bytes_downloaded / total_bytes * 100.0;
				Console.Write($"\r  {bytes_downloaded / (1024.0 * 1024.0):F1} / {total_bytes / (1024.0 * 1024.0):F1} MB ({pct:F1}%)   ");
			}
			else
			{
				Console.Write($"\r  {bytes_downloaded / (1024.0 * 1024.0):F1} MB downloaded...   ");
			}
		}
	}

	Console.WriteLine($"\r  Download complete: {bytes_downloaded / (1024.0 * 1024.0):F1} MB                 ");
	Console.WriteLine($"\nModel saved to: {dest_path}");
	Console.WriteLine($"\nTo use with ai-test:");
	Console.WriteLine($"  ai-test.exe --local \"{dest_path}\"");
}
catch (Exception ex)
{
	Console.WriteLine($"\nDownload failed: {ex.Message}");

	// Clean up partial download
	if (File.Exists(dest_path))
	{
		try { File.Delete(dest_path); }
		catch { }
	}
}

static string ThisFile([CallerFilePath] string sourceFilePath = "") => sourceFilePath;
