using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Xml;
using pr.common;
using pr.util;
using pr.extn;

// /// <summary>Example use of settings</summary>
// public sealed class Settings :SettingsBase
// {
//     public static readonly Settings Default = new Settings(0);
//     protected override SettingsBase DefaultData { get { return Default; } }
//
//     public string Str { get { return get<string>("Str"); } set { set("Str", value); } }
//     public int    Int { get { return get<int   >("Int"); } set { set("Int", value); } }
//
//     public Settings(ELoadOptions opts = ELoadOptions.Normal)
//     {
//         // Try to load from file, if that fails, fall through an load defaults
//         if (opts == ELoadOptions.Normal) try { Reload(); return; } catch {}
//         Str = "default";
//         Int = 4;
//     }
// }

namespace pr.common
{
	/// <summary>A base class for simple settings</summary>
	[KnownType("KnownTypes")]
	public abstract class SettingsBase<T> where T:SettingsBase<T>, new()
	{
		// Using a List<> instead of a Dictionary as Dictionary isn't serialiseable
		[KnownType(typeof(Point))] // Add more of these as needed
		[KnownType(typeof(Color))]
		[KnownType(typeof(Size))]
		[KnownType(typeof(DateTime))]
		[KnownType(typeof(Font))]
		[KnownType(typeof(FontStyle))]
		[KnownType(typeof(GraphicsUnit))]
		[DataContract(Name="setting")]
		protected class Pair
		{
			[DataMember(Name="key"  )] public string Key   {get;set;}
			[DataMember(Name="value")] public object Value {get;set;}
			public override string ToString() { return Key + "  " + Value; }
		}

		protected const string VersionKey = "__SettingsVersion";
		protected List<Pair> Data = new List<Pair>();
		private string m_filepath = "";
		private bool m_block_saving;
		private bool m_auto_save;

		/// <summary>The default values for the settings</summary>
		public static T Default { get { return m_default ?? (m_default = new T()); } }
		private static T m_default;

		/// <summary>Returns the directory in which to store app settings</summary>
		public static string DefaultAppDataDirectory
		{
			get
			{
				string company = Util.GetAssemblyAttribute<AssemblyCompanyAttribute>().Company;
				string app_name = Util.GetAssemblyAttribute<AssemblyTitleAttribute>().Title;
				string app_data = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
				return Path.Combine(Path.Combine(app_data, company), app_name);
			}
		}

		/// <summary>Returns the directory in which to store app settings</summary>
		public static string DefaultFilepath
		{
			get { return Path.Combine(DefaultAppDataDirectory, "settings.xml"); }
		}

		/// <summary>The settings version, used to detect when 'Upgrade' is needed</summary>
		protected virtual string Version { get { return "v1.0"; } }

		/// <summary>Override this method to return addition known types</summary>
		protected virtual IEnumerable<Type> KnownTypes { get { return Enumerable.Empty<Type>(); } }

		/// <summary>Returns the filepath for the persisted settings file. Settings cannot be saved until this property has a valid filepath</summary>
		public string Filepath
		{
			get { return m_filepath; }
			set { m_filepath = value ?? string.Empty; }
		}

		/// <summary>True to block all writes to the settings</summary>
		public bool ReadOnly { get; set; }

		/// <summary>Read a settings value</summary>
		protected Value get<Value>(string key)
		{
			int idx = index(key);
			if (idx >= 0) return (Value)Data[idx].Value;
			idx = Default.index(key);
			if (idx >= 0) return (Value)Default.Data[idx].Value;
			throw new KeyNotFoundException("Unknown setting '"+key+"'.\r\n"+
				"This is probably because there is no default value set "+
				"in the constructor of the derived settings class");
		}

		/// <summary>Write a settings value</summary>
		protected void set<Value>(string key, Value value)
		{
			if (ReadOnly)
				return;

			// Key not in the data yet? Must be initial value from startup
			int idx = index(key);
			if (idx < 0) { Data.Insert(~idx, new Pair{Key = key, Value = value}); return; }

			object old_value = Data[idx].Value;
			if (Equals(old_value, value)) return; // If the values are the same, don't raise 'changing' events

			var args = new SettingsChangingEventArgs(key, old_value, value, false);
			if (SettingChanging != null && key != VersionKey) SettingChanging(this, args);
			if (!args.Cancel) Data[idx].Value = value;
			if (SettingChanged != null && key != VersionKey) SettingChanged(this, new SettingChangedEventArgs(key, old_value, value));
		}

		/// <summary>Return the index of 'key' in the data. Returned value is negative if not found</summary>
		protected int index(string key)
		{
			return Data.BinarySearch(x => string.CompareOrdinal(x.Key, key));
		}

		/// <summary>Returns true if 'key' is present within the data</summary>
		protected bool has(string key)
		{
			return index(key) >= 0;
		}

		/// <summary>An event raised when a settings is about to change value</summary>
		public event EventHandler<SettingsChangingEventArgs> SettingChanging;
		public class SettingsChangingEventArgs :CancelEventArgs
		{
			public string Key      { get; private set; }
			public object OldValue { get; private set; }
			public object NewValue { get; private set; }
			public SettingsChangingEventArgs(string key, object old_value, object new_value, bool cancel)
			:base(cancel)
			{
				Key = key;
				OldValue = old_value;
				NewValue = new_value;
			}
			public SettingsChangingEventArgs(string key, object old_value, object new_value)
			:this(key, old_value, new_value, false)
			{}
		}

		/// <summary>An event raised after a setting has been changed</summary>
		public event EventHandler<SettingChangedEventArgs> SettingChanged;
		public class SettingChangedEventArgs :EventArgs
		{
			public string Key      { get; private set; }
			public object OldValue { get; private set; }
			public object NewValue { get; private set; }
			public SettingChangedEventArgs(string key, object old_value, object new_value)
			{
				Key = key;
				OldValue = old_value;
				NewValue = new_value;
			}
		}

		/// <summary>An event raised whenever the settings are loaded from persistent storage</summary>
		public event EventHandler<SettingsLoadedEventArgs> SettingsLoaded;
		public class SettingsLoadedEventArgs :EventArgs
		{
			/// <summary>The location of where the settings were loaded from</summary>
			public string Filepath { get; private set; }
			public SettingsLoadedEventArgs(string filepath) { Filepath = filepath; }
		}

		/// <summary>An event raised whenever the settings are about to be saved to persistent storage</summary>
		public event EventHandler<SettingsSavingEventArgs> SettingsSaving;
		public class SettingsSavingEventArgs :CancelEventArgs
		{
			public SettingsSavingEventArgs() :this(false) {}
			public SettingsSavingEventArgs(bool cancel) :base(cancel) {}
		}

		/// <summary>Default settings instance</summary>
		protected SettingsBase()
		{}

		/// <summary>Initialise the settings object</summary>
		protected SettingsBase(string filepath, bool read_only = false)
		{
			Debug.Assert(!string.IsNullOrEmpty(filepath));

			Filepath = filepath;
			Data = new List<Pair>(Default.Data);

			try
			{
				Load(Filepath, read_only);
			}
			catch (Exception ex)
			{
				// If anything goes wrong, use the defaults
				Log.Exception(this, ex, "Failed to load settings from {0}".Fmt(filepath));
				Data = new List<Pair>(Default.Data);
			}
			AutoSaveOnChanges = true;
		}

		/// <summary>Get/Set whether to automatically save whenever a setting is changed</summary>
		public bool AutoSaveOnChanges
		{
			get { return m_auto_save; }
			set
			{
				if (m_auto_save == value) return;
				m_auto_save = value;
				EventHandler<SettingChangedEventArgs> save = (s,a)=> Save();
				if (m_auto_save) SettingChanged += save;
				else             SettingChanged -= save;
			}
		}

		/// <summary>Resets the persistent settings to their defaults</summary>
		public void Reset()
		{
			Default.Save(Filepath);
			Reload();
		}

		/// <summary>Reload the current settings file</summary>
		public void Reload()
		{
			Load(Filepath);
		}

		/// <summary>Refreshes the settings from persistent storage</summary>
		public void Load(string filepath, bool read_only = false)
		{
			try
			{
				// Block saving during load/upgrade
				m_block_saving = true;

				if (!PathEx.FileExists(filepath))
				{
					Log.Info(this, "Settings file {0} not found, using defaults".Fmt(filepath));
					Reset();
					return; // Reset will recursively call Load again
				}

				Log.Debug(this, "Loading settings file {0}".Fmt(filepath));

				var ser = new DataContractSerializer(typeof(List<Pair>), KnownTypes);
				using (var fs = new FileStream(Filepath, FileMode.Open, FileAccess.Read, FileShare.Read))
				{
					Data = (List<Pair>)ser.ReadObject(fs);
					Data.Sort((lhs,rhs) => string.CompareOrdinal(lhs.Key, rhs.Key));
				}

				// Add any default options that aren't in the settings file
				foreach (var i in Default.Data)
				{
					if (has(i.Key)) continue;
					set(i.Key, i.Value);
				}

				// Migrate old settings
				for (string version; (version = get<string>(VersionKey)) != Version;)
					Upgrade(version);
			}
			finally
			{
				Filepath = filepath;
				ReadOnly = read_only;
				m_block_saving = false;
			}

			Validate();

			// Notify of settings loaded
			if (SettingsLoaded != null)
				SettingsLoaded(this, new SettingsLoadedEventArgs(filepath));
		}

		/// <summary>Persist current settings to storage</summary>
		public void Save(string filepath)
		{
			if (m_block_saving)
				return;
			try
			{
				m_block_saving = true;
				if (string.IsNullOrEmpty(filepath))
					throw new ArgumentNullException("filepath", "No settings filepath set");

				// Notify of a save about to happen
				var args = new SettingsSavingEventArgs(false);
				if (SettingsSaving != null) SettingsSaving(this, args);
				if (args.Cancel) return;

				// Ensure the save directory exists
				string path = Path.GetDirectoryName(filepath);
				if (path != null && !Directory.Exists(path)) Directory.CreateDirectory(path);

				// Save the settings version
				set(VersionKey, Version);

				Log.Debug(this, "Saving settings to file {0}".Fmt(filepath));

				// Perform the save
				var ser = new DataContractSerializer(typeof(List<Pair>), KnownTypes);
				using (var fs = XmlWriter.Create(filepath, new XmlWriterSettings{Indent = true, ConformanceLevel = ConformanceLevel.Fragment}))
					ser.WriteObject(fs, Data);
			}
			finally { m_block_saving = false; }
		}

		/// <summary>Save using the last filepath</summary>
		public void Save()
		{
			Save(Filepath);
		}

		/// <summary>Remove the settings file from persistent storage</summary>
		public void Delete()
		{
			if (PathEx.FileExists(Filepath))
				File.Delete(Filepath);
		}

		/// <summary>Called when loading settings from an earlier version</summary>
		public virtual void Upgrade(string from_version)
		{
			throw new NotSupportedException("Settings file version is {0}. Latest version is {1}. Upgrading from this version is not supported".Fmt(from_version, Version));
		}

		/// <summary>Perform validation on the loaded settings</summary>
		public virtual void Validate() {}
	}
}

#if PR_UNITTESTS

namespace pr
{
	using NUnit.Framework;

	[TestFixture] internal static partial class UnitTests
	{
		private sealed class Settings :SettingsBase<Settings>
		{
			public string   Str { get { return get<string  >("Str"); } private set { set("Str", value); } }
			public int      Int { get { return get<int     >("Int"); } private set { set("Int", value); } }

			public Settings()
			{
				Str = "default";
				Int = 4;
			}
		}

		[Test] public static void TestSettings()
		{
			Settings s = new Settings();
			Assert.AreEqual(Settings.Default.Str, s.Str);
			Assert.AreEqual(Settings.Default.Int, s.Int);
			Assert.Throws(typeof(ArgumentNullException), s.Save); // no filepath set
		}
	}
}

#endif
