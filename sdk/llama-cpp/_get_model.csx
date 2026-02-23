#! "net9.0"
#r "System.Net.Http"
#nullable enable

// Dynamically browses Hugging Face for GGUF models, shows available
// quantisations, and downloads the selected file into sdk/llama-cpp/models.
// Usage:
//   dotnet-script _get_model.csx              # browse popular models
//   dotnet-script _get_model.csx -- mistral   # search for "mistral"

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

static string ScriptDir = Path.GetDirectoryName(ScriptFile())!;
static string ModelsDir = Path.Join(ScriptDir, "models");
Directory.CreateDirectory(ModelsDir);

// Optional search filter from command line (e.g. "mistral", "llama 8b")
var search_filter = Args.Count > 0 ? string.Join(" ", Args) : "";

// ─── Curated annotations for well-known model families ───────────────────────
var annotations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
{
	["mistral"]     = "Strong instruction following, good for NPC dialogue",
	["mixtral"]     = "Mixture-of-experts, very capable but large",
	["llama"]       = "Meta, strong general purpose, wide community",
	["phi"]         = "Microsoft, fast for its size, decent quality",
	["qwen"]        = "Alibaba, multilingual, strong reasoning",
	["gemma"]       = "Google, compact and well-trained",
	["smollm"]      = "HuggingFace, ultra-lightweight, good for testing",
	["camel"]       = "Role-play and character dialogue specialist",
	["yi"]          = "01.AI, bilingual (EN/ZH), long context",
	["deepseek"]    = "Strong reasoning and coding",
	["tinyllama"]   = "Extremely compact, for background NPCs",
	["solar"]       = "Upstage, good instruction adherence",
	["openchat"]    = "Optimised for natural conversation",
	["zephyr"]      = "Alignment-focused chat model",
	["starling"]    = "Berkeley, RLHF-optimised dialogue",
	["neural-chat"] = "Intel, chat-optimised",
	["vicuna"]      = "LMSYS, conversational fine-tune of LLaMA",
	["command"]     = "Cohere, good for RAG and agentic tasks",
	["orca"]        = "Microsoft, trained on synthetic reasoning data",
	["nous"]        = "Nous Research, creative/RP fine-tunes",
	["internlm"]    = "Shanghai AI Lab, multilingual",
	["glm"]         = "Tsinghua/Zhipu, bilingual chat",
};

// Quantisation descriptions (ordered smallest → largest)
var quant_info = new (string Id, string Quality, int Order)[]
{
	("IQ1_S",  "Extreme compression, very low quality",    1),
	("IQ1_M",  "Extreme compression, low quality",         2),
	("IQ2_XXS","Tiny, experimental",                       3),
	("IQ2_XS", "Tiny, experimental",                       4),
	("IQ2_S",  "Very small, experimental",                 5),
	("IQ2_M",  "Very small, experimental",                 6),
	("Q2_K",   "Smallest practical, lower quality",        7),
	("Q2_K_L", "Small, slightly better than Q2_K",         8),
	("IQ3_XXS","Small, experimental",                      9),
	("IQ3_XS", "Small, experimental",                     10),
	("IQ3_S",  "Small, experimental",                     11),
	("IQ3_M",  "Small, experimental",                     12),
	("Q3_K_S", "Small, lower quality",                    13),
	("Q3_K_M", "Small, moderate quality",                 14),
	("Q3_K_L", "Small, reasonable quality",               15),
	("IQ4_NL", "Medium, experimental",                    16),
	("IQ4_XS", "Medium, experimental",                    17),
	("Q4_0",   "Medium, basic quantisation",              18),
	("Q4_K_S", "Medium, good quality",                    19),
	("Q4_K_M", "Medium, good balance (recommended)",      20),
	("Q4_1",   "Medium, slightly better Q4",              21),
	("Q5_0",   "Medium-large, higher quality",            22),
	("Q5_K_S", "Medium-large, good quality",              23),
	("Q5_K_M", "Medium-large, high quality",              24),
	("Q6_K",   "Large, very high quality",                25),
	("Q8_0",   "Largest practical, near-original",        26),
	("F16",    "Full half-precision, very large",         27),
	("fp16",   "Full half-precision, very large",         27),
	("BF16",   "Full bfloat16-precision, very large",     28),
	("bf16",   "Full bfloat16-precision, very large",     28),
	("F32",    "Full precision, enormous",                29),
};

// ─── Helper functions ────────────────────────────────────────────────────────

// Extract parameter count from model name (e.g. "7B", "3.8B", "0.5B")
string ParseParams(string name)
{
	var m = Regex.Match(name, @"[\-_](\d+\.?\d*)[Bb][\-_\.]");
	if (m.Success) return $"{m.Groups[1].Value}B";

	// Try without delimiters (e.g. "Phi3" where "3.8B" might not be in the name)
	m = Regex.Match(name, @"(\d+\.?\d*)[Bb]\b");
	return m.Success ? $"{m.Groups[1].Value}B" : "?";
}

// Classify model type from its name
string ClassifyType(string name)
{
	var lower = name.ToLowerInvariant();
	if (lower.Contains("role") || lower.Contains("camel")) return "RP";
	if (lower.Contains("chat"))    return "Chat";
	if (lower.Contains("instruct")) return "Instruct";
	if (lower.Contains("coder") || lower.Contains("code")) return "Code";
	return "Base";
}

// Find annotation for a model
string GetAnnotation(string model_id)
{
	var lower = model_id.ToLowerInvariant();
	foreach (var kvp in annotations)
		if (lower.Contains(kvp.Key))
			return kvp.Value;
	return "General purpose";
}

// Format byte size as human-readable string
string FormatSize(long bytes)
{
	if (bytes >= 1024L * 1024 * 1024) return $"{bytes / (1024.0 * 1024 * 1024):F1} GB";
	if (bytes >= 1024L * 1024)        return $"{bytes / (1024.0 * 1024):F0} MB";
	return $"{bytes / 1024.0:F0} KB";
}

// Format download count as compact string
string FormatDownloads(long downloads)
{
	if (downloads >= 1_000_000) return $"{downloads / 1_000_000.0:F1}M";
	if (downloads >= 1_000)    return $"{downloads / 1_000.0:F0}K";
	return downloads.ToString();
}

// Find the best matching quantisation info for a filename
(string Id, string Quality, int Order) MatchQuant(string filename)
{
	// Check from most specific to least (longer names first to avoid partial matches)
	foreach (var qi in quant_info.OrderByDescending(q => q.Id.Length))
	{
		if (filename.Contains(qi.Id, StringComparison.OrdinalIgnoreCase))
			return qi;
	}
	return ("?", "Unknown quantisation", 99);
}

// ─── Main ────────────────────────────────────────────────────────────────────

Console.WriteLine();
Console.WriteLine("=== GGUF Model Browser ===");
Console.WriteLine();

var client = new HttpClient();
client.DefaultRequestHeaders.Add("User-Agent", "rylogic-model-browser/1.0");
client.Timeout = TimeSpan.FromSeconds(30);

// Already-downloaded local files
var local_files = Directory.GetFiles(ModelsDir, "*.gguf")
	.Select(Path.GetFileName)
	.ToHashSet(StringComparer.OrdinalIgnoreCase);

if (local_files.Count > 0)
{
	Console.WriteLine($"Models already downloaded ({ModelsDir}):");
	foreach (var lf in local_files.OrderBy(f => f))
	{
		var fi = new FileInfo(Path.Join(ModelsDir, lf));
		Console.WriteLine($"  [x] {lf} ({FormatSize(fi.Length)})");
	}
	Console.WriteLine();
}

// ── Step 1: Fetch model list from Hugging Face ──────────────────────────────

Console.Write("Fetching models from Hugging Face...");

var search_param = string.IsNullOrEmpty(search_filter) ? "" : $"&search={Uri.EscapeDataString(search_filter)}";
var api_url = $"https://huggingface.co/api/models?filter=gguf&sort=downloads&pipeline_tag=text-generation&limit=200{search_param}";

string json;
try
{
	json = await client.GetStringAsync(api_url);
}
catch (Exception ex)
{
	Console.WriteLine($"\nError: {ex.Message}");
	return;
}

var models = new List<(string Id, string ShortName, string Params, string Type, long Downloads, string Annotation)>();
using (var doc = JsonDocument.Parse(json))
{
	foreach (var model in doc.RootElement.EnumerateArray())
	{
		var id = model.GetProperty("id").GetString() ?? "";
		var downloads = model.TryGetProperty("downloads", out var dl) ? dl.GetInt64() : 0;

		// Must have GGUF in the name (some repos are tagged gguf but aren't GGUF repos)
		if (!id.Contains("gguf", StringComparison.OrdinalIgnoreCase) &&
			!id.Contains("GGUF", StringComparison.OrdinalIgnoreCase))
			continue;

		// Extract the short name (after the owner/ prefix, without -GGUF suffix)
		var short_name = id.Contains('/') ? id[(id.IndexOf('/') + 1)..] : id;
		short_name = Regex.Replace(short_name, @"[\-_]?[Gg][Gg][Uu][Ff]$", "");

		var parms = ParseParams(id);
		var type = ClassifyType(id);
		var annotation = GetAnnotation(id);

		models.Add((id, short_name, parms, type, downloads, annotation));
	}
}

Console.WriteLine($" found {models.Count} models.");

if (models.Count == 0)
{
	Console.WriteLine("No models found.");
	if (!string.IsNullOrEmpty(search_filter))
		Console.WriteLine($"  Try a different search: dotnet-script _get_model.csx -- <keyword>");
	return;
}

// ── Display model list ──────────────────────────────────────────────────────

Console.WriteLine();
Console.WriteLine($"  {"#",-4} {"Model",-45} {"Params",-8} {"Type",-10} {"DLs",-8} Description");
Console.WriteLine($"  {new string('\u2500', 4)} {new string('\u2500', 45)} {new string('\u2500', 8)} {new string('\u2500', 10)} {new string('\u2500', 8)} {new string('\u2500', 20)}");for (int i = 0; i < models.Count; i++)
{
	var m = models[i];
	Console.WriteLine($"  {i + 1,-4} {Truncate(m.ShortName, 44),-45} {m.Params,-8} {m.Type,-10} {FormatDownloads(m.Downloads),-8} {m.Annotation}");
}
Console.WriteLine();
if (string.IsNullOrEmpty(search_filter))
	Console.WriteLine("  Tip: narrow results with: dotnet-script _get_model.csx -- <keyword>");
Console.Write("\nEnter model # for quantisation options (or 'q' to quit): ");
var input = Console.ReadLine()?.Trim() ?? "";
if (input.Equals("q", StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(input))
	return;

if (!int.TryParse(input, out var choice) || choice < 1 || choice > models.Count)
{
	Console.WriteLine("Invalid selection.");
	return;
}

var selected = models[choice - 1];

// ── Step 2: Fetch quantisation files ────────────────────────────────────────

Console.Write($"\nFetching files for {selected.Id}...");

string tree_json;
try
{
	tree_json = await client.GetStringAsync($"https://huggingface.co/api/models/{selected.Id}/tree/main");
}
catch (Exception ex)
{
	Console.WriteLine($"\nError: {ex.Message}");
	return;
}

var files = new List<(string Name, long Size, string QuantId, string Quality, int Order)>();
using (var doc = JsonDocument.Parse(tree_json))
{
	foreach (var file in doc.RootElement.EnumerateArray())
	{
		var ftype = file.GetProperty("type").GetString() ?? "";
		if (ftype != "file") continue;

		var fname = file.GetProperty("path").GetString() ?? "";
		if (!fname.EndsWith(".gguf", StringComparison.OrdinalIgnoreCase)) continue;

		// Skip split files (e.g. model-00001-of-00003.gguf)
		if (Regex.IsMatch(fname, @"-\d{5}-of-\d{5}")) continue;

		var size = file.TryGetProperty("size", out var sz) ? sz.GetInt64() : -1;
		var qi = MatchQuant(fname);
		files.Add((fname, size, qi.Id, qi.Quality, qi.Order));
	}
}

files.Sort((a, b) => a.Order.CompareTo(b.Order));
Console.WriteLine($" found {files.Count} quantisations.");

if (files.Count == 0)
{
	Console.WriteLine("No .gguf files found in this repository.");
	return;
}

Console.WriteLine($"\nQuantisations for {selected.ShortName} ({selected.Params} params, {selected.Type}):");
Console.WriteLine($"  {selected.Annotation}");
Console.WriteLine();
for (int i = 0; i < files.Count; i++)
{
	var f = files[i];
	var size_str = f.Size > 0 ? FormatSize(f.Size) : "? GB";
	var is_local = local_files.Contains(f.Name!);
	var status = is_local ? "  [x] downloaded" : "";
	var recommended = f.QuantId == "Q4_K_M" ? "  * recommended" : "";

	Console.WriteLine($"  {i + 1,3}. {f.Name}");
	Console.WriteLine($"       {size_str,-12} {f.Quality}{recommended}{status}");
}

Console.Write($"\nEnter # to download (or 'q' to quit): ");
input = Console.ReadLine()?.Trim() ?? "";
if (input.Equals("q", StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(input))
	return;

if (!int.TryParse(input, out var file_choice) || file_choice < 1 || file_choice > files.Count)
{
	Console.WriteLine("Invalid selection.");
	return;
}

var selected_file = files[file_choice - 1];
var dest = Path.Join(ModelsDir, selected_file.Name);

if (File.Exists(dest))
{
	var existing = new FileInfo(dest);
	Console.Write($"'{selected_file.Name}' already exists ({FormatSize(existing.Length)}). Re-download? (y/N): ");
	var confirm = Console.ReadLine()?.Trim() ?? "";
	if (!confirm.Equals("y", StringComparison.OrdinalIgnoreCase))
		return;
}

// ── Step 3: Download ────────────────────────────────────────────────────────

var download_url = $"https://huggingface.co/{selected.Id}/resolve/main/{Uri.EscapeDataString(selected_file.Name)}";
var size_label = selected_file.Size > 0 ? FormatSize(selected_file.Size) : "unknown size";

Console.WriteLine($"\nDownloading '{selected_file.Name}' ({size_label})...");
Console.WriteLine($"  From: {download_url}");
Console.WriteLine($"  To:   {dest}");
Console.WriteLine();

try
{
	var dl_client = new HttpClient();
	dl_client.Timeout = TimeSpan.FromHours(2);

	var response = await dl_client.GetAsync(download_url, HttpCompletionOption.ResponseHeadersRead);
	response.EnsureSuccessStatusCode();

	var total_bytes = response.Content.Headers.ContentLength ?? selected_file.Size;
	var stream = await response.Content.ReadAsStreamAsync();
	var file_stream = File.Create(dest);

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
				Console.Write($"\r  {FormatSize(bytes_downloaded)} / {FormatSize(total_bytes)} ({pct:F1}%)   ");
			}
			else
			{
				Console.Write($"\r  {FormatSize(bytes_downloaded)} downloaded...   ");
			}
		}
	}

	Console.WriteLine($"\r  Download complete: {FormatSize(bytes_downloaded)}                    ");

	file_stream.Dispose();
	stream.Dispose();
	response.Dispose();
	dl_client.Dispose();

	Console.WriteLine($"\nModel saved to: {dest}");
	Console.WriteLine($"\nTo use with ai-test:");
	Console.WriteLine($"  ai-test.exe --local \"{dest}\"");
}
catch (Exception ex)
{
	Console.WriteLine($"\nDownload failed: {ex.Message}");
	if (File.Exists(dest))
	{
		try { File.Delete(dest); }
		catch { }
	}
}

// ─── Utilities ───────────────────────────────────────────────────────────────

static string Truncate(string s, int max) => s.Length <= max ? s : s[..(max - 2)] + "..";
static string ScriptFile([CallerFilePath] string path = "") => path;
