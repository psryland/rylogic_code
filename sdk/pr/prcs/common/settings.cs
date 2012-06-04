using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;
using NUnit.Framework;
using pr.util;

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
	[Serializable] public abstract class SettingsBase
	{
		protected Dictionary<string,object> Data = new Dictionary<string, object>();
		private const string version_key = "__SettingsVersion";
		
		/// <summary>Options for loading settings</summary>
		public enum ELoadOptions { Normal, Defaults };
		
		/// <summary>Return default values for these settings</summary>
		protected abstract SettingsBase DefaultData { get; }
		
		/// <summary>The settings version, used to detect when 'Upgrade' is needed</summary>
		protected virtual string Version { get { return "v1.0"; } }
		
		/// <summary>Returns the directory in which to store app settings</summary>
		protected virtual string AppDataDirectory
		{
			get
			{
				string company = Util.GetAssemblyAttribute<AssemblyCompanyAttribute>().Company;
				string app_name = Util.GetAssemblyAttribute<AssemblyProductAttribute>().Product;
				string app_data = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.Create);
				return Path.Combine(app_data, company, app_name);
			}
		}
		
		/// <summary>Returns the filepath for the persisted settings file</summary>
		protected virtual string Filepath
		{
			get { return Path.Combine(AppDataDirectory, "settings.xml"); }
		}
		
		/// <summary>Read a settings value</summary>
		protected T get<T>(string key)
		{
			return (T)Data[key];
		}

		/// <summary>Write a settings value</summary>
		protected void set<T>(string key, T value)
		{
			// Key not in the data yet, must be initial value from startup
			if (!Data.ContainsKey(key))
			{
				Data.Add(key, value);
				return;
			}
			
			// If the values are the same, don't raise 'changing' events
			if (value.Equals(Data[key])) return; // same value
			
			var args = new SettingsChangingEventArgs(key, value, false);
			if (SettingChanging != null && key != version_key) SettingChanging(this, args);
			if (!args.Cancel) Data[key] = value;
		}
		
		/// <summary>An event raised when a settings is about to change value</summary>
		public event EventHandler<SettingsChangingEventArgs> SettingChanging;
		public class SettingsChangingEventArgs :CancelEventArgs
		{
			public string Key      { get; private set; }
			public object NewValue { get; private set; }
			public SettingsChangingEventArgs(string key, object new_value, bool cancel = false) :base(cancel) { Key = key; NewValue = new_value; }
		}
		
		/// <summary>An event raised whenever the settings are loaded from persistent storage</summary>
		public event EventHandler<SettingsLoadedEventArgs> SettingsLoaded;
		public class SettingsLoadedEventArgs :EventArgs
		{
			/// <summary>The location of where the settings were loaded from</summary>
			public string Filepath { get; private set; }
			
			public SettingsLoadedEventArgs(string filepath) { Filepath = filepath; }
		}

		/// <summary>An event raised whenver the settings are about to be saved to persistent storage</summary>
		public event SettingsSavingEventHandler SettingsSaving;
		
		/// <summary>Refreshes the settings from persistent storage</summary>
		public virtual void Reload()
		{
			string filepath = Filepath;

			XmlSerializer ser = new XmlSerializer(typeof(Dictionary<string,object>));
			XmlReaderSettings rs = new XmlReaderSettings();
			using (XmlReader r = XmlReader.Create(filepath, rs))
				Data = (Dictionary<string,object>)ser.Deserialize(r);
			
			// Check the version of the settings
			string version = get<string>(version_key);
			if (version != Version)
				 Upgrade();
			
			// Notify of settings loaded
			if (SettingsLoaded != null)
				SettingsLoaded(this, new SettingsLoadedEventArgs(filepath));
		}

		/// <summary>Persist current settings to storage</summary>
		public virtual void Save()
		{
			// Notify of a save about to happen
			CancelEventArgs args = new CancelEventArgs(false);
			if (SettingsSaving != null) SettingsSaving(this, args);
			if (args.Cancel) return;

			// Ensure the save directory exists
			string filepath = Filepath;
			string path = Path.GetDirectoryName(filepath);
			if (path != null && !Directory.Exists(path)) Directory.CreateDirectory(path);

			// Perform the save
			set(version_key, Version);
			XmlSerializer ser = new XmlSerializer(typeof(Dictionary<string,object>));
			XmlWriterSettings ws = new XmlWriterSettings{NewLineHandling = NewLineHandling.Entitize, Indent = true};
			using (XmlWriter wr = XmlWriter.Create(Filepath, ws))
				ser.Serialize(wr, Data);
		}

		/// <summary>Resets the persistent settings to their defaults</summary>
		public virtual void Reset()
		{
			DefaultData.Save();
		}

		/// <summary>Called when loading settings from an earlier version</summary>
		public virtual void Upgrade() {}
	}
	
	/// <summary>String extension unit tests</summary>
	[TestFixture] internal static class UnitTests
	{
		/// <summary>Example use of settings</summary>
		private sealed class Settings :SettingsBase
		{
			public static readonly Settings Default = new Settings(0);
			protected override SettingsBase DefaultData { get { return Default; } }
			
			public string Str { get { return get<string>("Str"); } set { set("Str", value); } }
			public int    Int { get { return get<int   >("Int"); } set { set("Int", value); } }

			public Settings(ELoadOptions opts = ELoadOptions.Normal)
			{
				if (opts == ELoadOptions.Normal)
				{
					// Try to load from file, if that fails, fall through an load defaults
					try { Reload(); return; } catch {}
				}
				Str = "default";
				Int = 4;
			}
		}
		
		[Test] public static void TestEventExtensions()
		{
			Settings s = new Settings();
			Assert.AreEqual(Settings.Default.Str, s.Str);
			Assert.AreEqual(Settings.Default.Int, s.Int);
			s.Save();

		}
	}
}
