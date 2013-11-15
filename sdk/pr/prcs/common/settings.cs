using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using System.Xml.Linq;
using pr.common;
using pr.util;
using pr.extn;

// /// <summary>Example use of settings</summary>
// public sealed class Settings :SettingsBase
// {
//     public static readonly Settings Default = new Settings(0);
//     protected override SettingsBase DefaultData { get { return Default; } }
//
//     public string Str { get { return get(x => x.Str); } set { set(x => x.Str, value); } }
//     public int    Int { get { return get(x => x.Int); } set { set(x => x.Int, value); } }
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
	[DataContract(Name="setting")] public class SettingsPair
	{
		[DataMember(Name="key"  )] public string Key   { get; set; }
		[DataMember(Name="value")] public object Value { get; set; }
		public override string ToString() { return Key + "  " + Value; }
	}

	/// <summary>A base class for simple settings</summary>
	public abstract class SettingsBase<T> where T:SettingsBase<T>, new()
	{
		protected const string VersionKey = "__SettingsVersion";
		protected readonly List<SettingsPair> Data = new List<SettingsPair>();
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

		/// <summary>Read a settings value</summary>
		protected Value get<Value>(Expression<Func<T,Value>> expression)
		{
			return get<Value>(Reflect<T>.MemberName(expression));
		}

		/// <summary>Write a settings value</summary>
		protected void set<Value>(string key, Value value)
		{
			if (ReadOnly)
				return;

			// Key not in the data yet? Must be initial value from startup
			int idx = index(key);
			if (idx < 0) { Data.Insert(~idx, new SettingsPair{Key = key, Value = value}); return; }

			object old_value = Data[idx].Value;
			if (Equals(old_value, value)) return; // If the values are the same, don't raise 'changing' events

			var args = new SettingsChangingEventArgs(key, old_value, value, false);
			if (SettingChanging != null && key != VersionKey) SettingChanging(this, args);
			if (!args.Cancel) Data[idx].Value = value;
			if (SettingChanged != null && key != VersionKey) SettingChanged(this, new SettingChangedEventArgs(key, old_value, value));
		}

		/// <summary>Write a settings value</summary>
		protected void set<Value>(Expression<Func<T,Value>> expression, Value value)
		{
			set(Reflect<T>.MemberName(expression), value);
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
			try
			{
				Data = new List<SettingsPair>();
				Load(Filepath, read_only);
			}
			catch (Exception ex)
			{
				// If anything goes wrong, use the defaults
				Log.Exception(this, ex, "Failed to load settings from {0}".Fmt(filepath));
				Data = new List<SettingsPair>(Default.Data);
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

		/// <summary>
		/// Refreshes the settings from persistent storage.
		/// Starts with a copy of the Default.Data, then overwrites settings with those described in 'filepath'
		/// After load, the settings is the union of Default.Data and those in the given file.</summary>
		public void Load(string filepath, bool read_only = false)
		{
			// Block saving during load/upgrade
			using (Scope.Create(() => m_block_saving = true, () => m_block_saving = false))
			{
				if (!PathEx.FileExists(filepath))
				{
					Log.Info(this,"Settings file {0} not found, using defaults".Fmt(filepath));
					Reset();
					return; // Reset will recursively call Load again
				}

				Log.Debug(this,"Loading settings file {0}".Fmt(filepath));

				var settings = XDocument.Load(filepath).Root;
				if (settings == null) throw new Exception("Invalidate settings file ({0})".Fmt(filepath));

				// Upgrade old settings
				XElement vers = settings.Element(VersionKey) ?? new XElement(VersionKey);
				vers.Remove();
				if (vers.Value != Version)
					Upgrade(settings, vers.Value);

				// Load the settings from xml
				FromXml(settings);
				Filepath = filepath;
				ReadOnly = read_only;
			}

			Validate();

			// Notify of settings loaded
			if (SettingsLoaded != null)
				SettingsLoaded(this,new SettingsLoadedEventArgs(filepath));
		}

		/// <summary>Persist current settings to storage</summary>
		public void Save(string filepath)
		{
			if (m_block_saving) return;
			using (Scope.Create(() => m_block_saving = true, () => m_block_saving = false))
			{
				if (string.IsNullOrEmpty(filepath))
					throw new ArgumentNullException("filepath", "No settings filepath set");

				// Notify of a save about to happen
				var args = new SettingsSavingEventArgs(false);
				if (SettingsSaving != null) SettingsSaving(this, args);
				if (args.Cancel) return;

				// Ensure the save directory exists
				var path = Path.GetDirectoryName(filepath);
				if (path != null && !Directory.Exists(path))
					Directory.CreateDirectory(path);

				Log.Debug(this, "Saving settings to file {0}".Fmt(filepath));

				// Perform the save
				var settings = ToXml();
				settings.Save(filepath, SaveOptions.None);
			}
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
		public virtual void Upgrade(XElement old_settings, string from_version)
		{
			throw new NotSupportedException("Settings file version is {0}. Latest version is {1}. Upgrading from this version is not supported".Fmt(from_version, Version));
		}

		/// <summary>Perform validation on the loaded settings</summary>
		public virtual void Validate() {}

		/// <summary>Return the settings as an xml node tree</summary>
		private XElement ToXml()
		{
			var settings = new XElement("settings");
			settings.Add2(VersionKey, Version);
			foreach (var d in Data) settings.Add2("setting", d);
			return settings;
		}

		/// <summary>Populate the settings from an xml node</summary>
		private void FromXml(XElement root)
		{
			// Load data from settings
			Data.Clear();
			foreach (var setting in root.Elements())
			{
				var pair = setting.As<SettingsPair>();
				Debug.Assert(pair != null, "Failed to read setting " + setting.Name);
				Data.Add(pair);
			}

			// Sort for binary search
			Data.Sort((l,r) => string.CompareOrdinal(l.Key, r.Key));

			// Add any default options that aren't in the settings file
			foreach (var i in Default.Data)
			{
				if (has(i.Key)) continue;
				set(i.Key, i.Value);
			}
		}
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
			public string         Str  { get { return get(x => x.Str ); } set { set(x => x.Str,  value); } }
			public int            Int  { get { return get(x => x.Int ); } set { set(x => x.Int,  value); } }
			public DateTimeOffset DTO  { get { return get(x => x.DTO ); } set { set(x => x.DTO,  value); } }
			public Font           Font { get { return get(x => x.Font); } set { set(x => x.Font, value); } }
			public Settings()
			{
				Str = "default";
				Int = 4;
				DTO = DateTimeOffset.Parse("2013-01-02 12:34:56");
				Font = SystemFonts.StatusFont;
			}
			public Settings(string filepath) :base(filepath) {}
		}

		[Test] public static void TestSettings1()
		{
			var s = new Settings();
			Assert.AreEqual(Settings.Default.Str, s.Str);
			Assert.AreEqual(Settings.Default.Int, s.Int);
			Assert.AreEqual(Settings.Default.DTO, s.DTO);
			Assert.AreEqual(Settings.Default.Font, s.Font);
			Assert.Throws(typeof(ArgumentNullException), s.Save); // no filepath set
		}
		[Test] public static void TestSettings2()
		{
			var file = Path.GetTempFileName();

			var s = new Settings
				{
					Str = "Changed",
					Int = 42,
					DTO = DateTimeOffset.UtcNow,
					Font = SystemFonts.DialogFont
				};
			s.Save(file);

			var S = new Settings(file);

			Assert.AreEqual(s.Str  , S.Str);
			Assert.AreEqual(s.Int  , S.Int);
			Assert.AreEqual(s.DTO  , S.DTO);
			Assert.AreEqual(s.Font , S.Font);
		}
	}
}

#endif
