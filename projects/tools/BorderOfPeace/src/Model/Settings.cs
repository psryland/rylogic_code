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
			new("Red",    0xFF, 0x00, 0x00),
			new("Green",  0x00, 0xC0, 0x00),
			new("Blue",   0x00, 0x00, 0xFF),
			new("Orange", 0xFF, 0x80, 0x00),
			new("Purple", 0x80, 0x00, 0xFF),
			new("Cyan",   0x00, 0xC0, 0xC0),
			new("Yellow", 0xFF, 0xD0, 0x00),
		];
	}
}
