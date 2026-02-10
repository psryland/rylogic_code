using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BorderOfPeace.Model
{
	/// <summary>Application settings, persisted as JSON</summary>
	public class Settings
	{
		/// <summary>The predefined color presets shown in menus</summary>
		public List<ColorPreset> Presets { get; set; } = DefaultPresets();

		/// <summary>Border thickness in pixels for colored windows (0 = system default)</summary>
		public uint BorderThickness { get; set; } = 0;

		/// <summary>Default file path for settings</summary>
		[JsonIgnore]
		public static string FilePath => Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
			"Rylogic", "BorderOfPeace", "settings.json");

		/// <summary>Load settings from disk, or return defaults if no file exists</summary>
		public static Settings Load()
		{
			try
			{
				var path = FilePath;
				if (File.Exists(path))
				{
					var json = File.ReadAllText(path);
					return JsonSerializer.Deserialize<Settings>(json) ?? new Settings();
				}
			}
			catch
			{
				// If the file is corrupted, just use defaults
			}
			return new Settings();
		}

		/// <summary>Save settings to disk</summary>
		public void Save()
		{
			try
			{
				var path = FilePath;
				var dir = Path.GetDirectoryName(path)!;
				Directory.CreateDirectory(dir);

				var options = new JsonSerializerOptions { WriteIndented = true };
				var json = JsonSerializer.Serialize(this, options);
				File.WriteAllText(path, json);
			}
			catch
			{
				// Best effort - settings are not critical
			}
		}

		/// <summary>Create the default set of color presets</summary>
		public static List<ColorPreset> DefaultPresets() =>
		[
			new("Navy",       0x1B, 0x2A, 0x4A),
			new("Burgundy",   0x6B, 0x21, 0x3C),
			new("Forest",     0x2D, 0x5A, 0x3D),
			new("Slate",      0x4A, 0x55, 0x68),
			new("Amber",      0xB4, 0x6A, 0x1F),
			new("Teal",       0x1A, 0x6B, 0x6A),
			new("Plum",       0x5B, 0x3A, 0x6E),
			new("Charcoal",   0x36, 0x36, 0x3B),
			new("Terracotta", 0xA0, 0x4A, 0x35),
			new("Steel",      0x5A, 0x7D, 0x9A),
			new("Powder",     0x9B, 0xBC, 0xD5),
			new("Rose",       0xC9, 0x8B, 0x9A),
			new("Sage",       0x8F, 0xB0, 0x8F),
			new("Lavender",   0xA0, 0x90, 0xC0),
			new("Sand",       0xC8, 0xB0, 0x88),
			new("Sky",        0x7A, 0xAE, 0xCC),
		];
	}
}
