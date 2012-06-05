using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Xml;
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
	public abstract class SettingsBase
	{
		// Add more of these as needed
		[KnownType(typeof(Point))]
		[KnownType(typeof(Color))]
		[KnownType(typeof(Size))]
		[KnownType(typeof(DateTime))]
		[KnownType(typeof(Font))]
		[KnownType(typeof(FontStyle))]
		[KnownType(typeof(GraphicsUnit))]

		// Using a List<> instead of a Dictionary as Dictionary isn't serialisable
		[DataContract(Name="setting")] public class Pair
		{
			[DataMember(Name="key"  )] public string Key   {get;set;}
			[DataMember(Name="value")] public object Value {get;set;}
			public override string ToString() { return Key + "  " + Value; }
		}
		
		protected List<Pair> Data = new List<Pair>();
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
				string app_name = Util.GetAssemblyAttribute<AssemblyTitleAttribute>().Title;
				string app_data = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
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
			Pair pair = Data.Find(x => x.Key == key);
			if (pair != null) return (T)pair.Value;
			pair = DefaultData.Data.Find(x => x.Key == key);
			if (pair != null) return (T)pair.Value;
			throw new KeyNotFoundException("Unknown setting '"+key+"'.\r\n"+
				"This is probably because there is no default value set "+
				"in the constructor of the derived settings class");
		}

		/// <summary>Write a settings value</summary>
		protected void set<T>(string key, T new_value)
		{
			// Key not in the data yet, must be initial value from startup
			Pair pair = Data.Find(x => x.Key == key);
			if (pair == null)
			{
				Data.Add(new Pair{Key = key, Value = new_value});
				return;
			}
			
			// If the values are the same, don't raise 'changing' events
			if (Equals(pair.Value, new_value)) return; // same value
			
			T old_value = (T)pair.Value;
			var args = new SettingsChangingEventArgs(key, old_value, new_value, false);
			if (SettingChanging != null && key != version_key) SettingChanging(this, args);
			if (!args.Cancel) pair.Value = new_value;
			if (SettingChanged != null && key != version_key) SettingChanged(this, new SettingChangedEventArgs(key, old_value, new_value));
		}
		
		/// <summary>An event raised when a settings is about to change value</summary>
		public event EventHandler<SettingsChangingEventArgs> SettingChanging;
		public class SettingsChangingEventArgs :CancelEventArgs
		{
			public string Key      { get; private set; }
			public object OldValue { get; private set; }
			public object NewValue { get; private set; }
			public SettingsChangingEventArgs(string key, object old_value, object new_value, bool cancel = false) :base(cancel)
			{
				Key = key;
				OldValue = old_value;
				NewValue = new_value;
			}
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

		/// <summary>An event raised whenver the settings are about to be saved to persistent storage</summary>
		public event EventHandler<SettingsSavingEventArgs> SettingsSaving;
		public class SettingsSavingEventArgs :CancelEventArgs
		{
			public SettingsSavingEventArgs(bool cancel = false) :base(cancel)
			{}
		}
		
		/// <summary>Refreshes the settings from persistent storage</summary>
		public virtual void Reload()
		{
			string filepath = Filepath;
			if (!File.Exists(filepath))
				Reset();
			
			DataContractSerializer ser = new DataContractSerializer(typeof(List<Pair>));
			using (FileStream fs = new FileStream(Filepath, FileMode.Open, FileAccess.Read))
				Data = (List<Pair>)ser.ReadObject(fs);
			
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
			SettingsSavingEventArgs args = new SettingsSavingEventArgs(false);
			if (SettingsSaving != null) SettingsSaving(this, args);
			if (args.Cancel) return;

			// Ensure the save directory exists
			string filepath = Filepath;
			string path = Path.GetDirectoryName(filepath);
			if (path != null && !Directory.Exists(path)) Directory.CreateDirectory(path);

			// Save the settings version
			set(version_key, Version);

			// Perform the save
			DataContractSerializer ser = new DataContractSerializer(typeof(List<Pair>));
			using (XmlWriter fs = XmlWriter.Create(Filepath, new XmlWriterSettings{Indent = true, ConformanceLevel = ConformanceLevel.Fragment, NamespaceHandling = NamespaceHandling.OmitDuplicates}))
				ser.WriteObject(fs, Data);
		}

		/// <summary>Resets the persistent settings to their defaults</summary>
		public virtual void Reset()
		{
			DefaultData.Save();
		}

		/// <summary>Remove the settings file from persistent storage</summary>
		public virtual void Delete()
		{
			if (File.Exists(Filepath))
				File.Delete(Filepath);
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
			public static readonly Settings Default = new Settings(ELoadOptions.Defaults);
			protected override SettingsBase DefaultData { get { return Default; } }
			
			public string   Str { get { return get<string  >("Str"); } set { set("Str", value); } }
			public int      Int { get { return get<int     >("Int"); } set { set("Int", value); } }
			
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
