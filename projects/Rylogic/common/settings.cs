using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Xml.Linq;
using pr.common;
using pr.util;
using pr.extn;

// Example use of settings
//  private sealed class Settings :SettingsBase<Settings>
//  {
//  	public string Str { get { return get(x => x.Str); } set { set(x => x.Str, value); } }
//  	public int    Int { get { return get(x => x.Int); } set { set(x => x.Int, value); } }
//  	public Settings()
//  	{
//  		Str = "default";
//  		Int = 4;
//  	}
//		public Settings(string filepath) :base(filepath) {}
//  }

namespace pr.common
{
	public class SettingsPair
	{
		public string Key   { get; set; }
		public object Value { get; set; }

		public SettingsPair() {}
		public SettingsPair(XElement node)
		{
			Key   = node.Element("key").As<string>();
			Value = node.Element("value").ToObject();
		}
		public XElement ToXml(XElement node)
		{
			node.Add(Key.ToXml("key", false), Value.ToXml("value", true));
			return node;
		}
		public override string ToString() { return Key + "  " + Value; }
	}

	public interface ISettingsSet
	{
		/// <summary>Parent settings for this settings object</summary>
		ISettingsSet Parent { get; set; }

		/// <summary>Called just before a setting changes</summary>
		void OnSettingChanging(SettingChangingEventArgs args);

		/// <summary>Called just after a setting changes</summary>
		void OnSettingChanged(SettingChangedEventArgs args);
	}

	/// <summary>A base class for settings structures</summary>
	public abstract class SettingsSet<T> :ISettingsSet where T:SettingsSet<T>, new()
	{
		protected readonly List<SettingsPair> Data;

		/// <summary>An event raised when a settings is about to change value</summary>
		public event EventHandler<SettingChangingEventArgs> SettingChanging;

		/// <summary>An event raised after a setting has been changed</summary>
		public event EventHandler<SettingChangedEventArgs> SettingChanged;

		protected SettingsSet()
		{
			Data = new List<SettingsPair>();
		}

		/// <summary>The default values for the settings</summary>
		public static T Default { get { return m_default ?? (m_default = new T()); } }
		private static T m_default;

		/// <summary>True to block all writes to the settings</summary>
		public bool ReadOnly { get; set; }

		/// <summary>Parent settings for this settings object</summary>
		public ISettingsSet Parent
		{
			get { return m_parent; }
			set { m_parent = value; } // could notify of parent changed...
		}
		private ISettingsSet m_parent;

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

		/// <summary>Read a settings value</summary>
		protected virtual Value get<Value>(string key)
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
		protected virtual void set<Value>(string key, Value value)
		{
			if (ReadOnly)
				return;

			// If 'value' is a nested setting, set this object as the parent
			SetParentIfNesting(value as ISettingsSet);

			// Key not in the data yet? Must be initial value from startup
			int idx = index(key);
			if (idx < 0) { Data.Insert(~idx, new SettingsPair{Key = key, Value = value}); return; }

			object old_value = Data[idx].Value;
			if (Equals(old_value, value)) return; // If the values are the same, don't raise 'changing' events

			var args = new SettingChangingEventArgs(key, old_value, value, false);
			OnSettingChanging(args);
			if (!args.Cancel) Data[idx].Value = value;
			OnSettingChanged(new SettingChangedEventArgs(key, old_value, value));
		}

		/// <summary>Write a settings value</summary>
		protected void set<Value>(Expression<Func<T,Value>> expression, Value value)
		{
			set(Reflect<T>.MemberName(expression), value);
		}

		/// <summary>Return the settings as an xml node tree</summary>
		public virtual XElement ToXml(XElement node)
		{
			foreach (var d in Data)
				node.Add2("setting", d, false);
			return node;
		}

		/// <summary>Populate this object from xml</summary>
		public virtual void FromXml(XElement node)
		{
			// Load data from settings
			Data.Clear();
			foreach (var setting in node.Elements())
			{
				var pair = setting.As<SettingsPair>();
				Debug.Assert(pair != null, "Failed to read setting " + setting.Name);
				SetParentIfNesting(pair.Value as ISettingsSet);
				Data.Add(pair);
			}

			// Sort for binary search
			Data.Sort((l,r) => string.CompareOrdinal(l.Key, r.Key));

			// Add any default options that aren't in the settings file
			// Use 'new T().Data' so that reference types can be used, otherwise we'll change the defaults
			foreach (var i in new T().Data)
			{
				if (has(i.Key)) continue;

				int idx = index(i.Key);
				Debug.Assert(idx < 0, "has() is supposed to check the key is not already present");

				SetParentIfNesting(i.Value as ISettingsSet);

				// Key not in the data yet? Must be initial value from startup
				Data.Insert(~idx, new SettingsPair{Key = i.Key, Value = i.Value});
			}
		}

		/// <summary>Called just before a setting changes</summary>
		public virtual void OnSettingChanging(SettingChangingEventArgs args)
		{
			SettingChanging.Raise(this, args);
			if (Parent == null) return;
			Parent.OnSettingChanging(args);
		}

		/// <summary>Called just after a setting changes</summary>
		public virtual void OnSettingChanged(SettingChangedEventArgs args)
		{
			SettingChanged.Raise(this, args);
			var me = (ISettingsSet)this;
			if (me.Parent == null) return;
			me.Parent.OnSettingChanged(args);
		}

		/// <summary>If 'nested' is not null, sets its Parent to this settings object</summary>
		private void SetParentIfNesting(ISettingsSet nested)
		{
			if (nested == null) return;
			nested.Parent = this;
		}
	}

	/// <summary>A base class for simple settings</summary>
	public abstract class SettingsBase<T> :SettingsSet<T> where T:SettingsBase<T>, new()
	{
		protected const string VersionKey = "__SettingsVersion";
		private string m_filepath;
		private bool m_block_saving;
		private bool m_auto_save;

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

		/// <summary>An event raised whenever the settings are loaded from persistent storage</summary>
		public event EventHandler<SettingsLoadedEventArgs> SettingsLoaded;

		/// <summary>An event raised whenever the settings are about to be saved to persistent storage</summary>
		public event EventHandler<SettingsSavingEventArgs> SettingsSaving;

		protected SettingsBase()
		{
			m_filepath = "";

			// Default to off so users can enable after startup completes
			AutoSaveOnChanges = false;
		}
		protected SettingsBase(SettingsBase<T> rhs, bool read_only = false) :this(rhs.ToXml(new XElement("root")), read_only)
		{}
		protected SettingsBase(XElement node, bool read_only = false) :this()
		{
			try
			{
				FromXml(node, read_only);
			}
			catch (Exception ex)
			{
				// If anything goes wrong, use the defaults
				// Use 'new T().Data' so that reference types can be used, otherwise we'll change the defaults
				Log.Exception(this, ex, "Failed to load settings from xml data");
				Data.AddRange(new T().Data);
			}
		}
		protected SettingsBase(Stream stream, bool read_only = false) :this()
		{
			Debug.Assert(stream != null);
			try
			{
				Load(stream, read_only);
			}
			catch (Exception ex)
			{
				// If anything goes wrong, use the defaults
				// Use 'new T().Data' so that reference types can be used, otherwise we'll change the defaults
				Log.Exception(this, ex, "Failed to load settings from stream");
				Data.AddRange(new T().Data);
			}
		}
		protected SettingsBase(string filepath, bool read_only = false) :this()
		{
			Debug.Assert(!string.IsNullOrEmpty(filepath));
			Filepath = filepath;
			Reload(read_only);
		}

		/// <summary>Get/Set whether to automatically save whenever a setting is changed</summary>
		public bool AutoSaveOnChanges
		{
			get { return m_auto_save; }
			set
			{
				if (m_auto_save == value) return;
				m_auto_save = value;
				
				EventHandler<SettingChangedEventArgs> save = (s,a) => Save();
				
				SettingChanged -= save;
				if (m_auto_save)
					SettingChanged += save;
			}
		}

		/// <summary>Resets the persistent settings to their defaults</summary>
		public void Reset()
		{
			Default.Save(Filepath);
			Reload();
		}

		/// <summary>Reload the current settings file</summary>
		public void Reload(bool read_only = false)
		{
			try
			{
				Load(Filepath, read_only);
			}
			catch (Exception ex)
			{
				// If anything goes wrong, use the defaults
				// Use 'new T().Data' so that reference types can be used, otherwise we'll change the defaults
				Log.Exception(this, ex, "Failed to load settings from {0}".Fmt(Filepath));
				Data.AddRange(new T().Data);
			}
		}

		/// <summary>Load the settings from xml</summary>
		public override void FromXml(XElement node)
		{
			// Upgrade old settings
			var vers = node.Element(VersionKey);
			if (vers == null)
			{
				Log.Info(this,"Settings data does not contain a version, using defaults");
				Reset();
				return; // Reset will recursively call Load again
			}

			vers.Remove();
			if (vers.Value != Version)
				Upgrade(node, vers.Value);

			// Load the settings from xml
			base.FromXml(node);
			Validate();

			// Notify of settings loaded
			if (SettingsLoaded != null)
				SettingsLoaded(this,new SettingsLoadedEventArgs(Filepath));
		}
		public void FromXml(XElement node, bool read_only)
		{
			FromXml(node);
			ReadOnly = read_only;
		}

		/// <summary>
		/// Refreshes the settings from a file storage.
		/// Starts with a copy of the Default.Data, then overwrites settings with those described in 'filepath'
		/// After load, the settings is the union of Default.Data and those in the given file.</summary>
		public void Load(string filepath, bool read_only = false)
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

			// Set the filepath before loading so that it's valid for the SettingsLoaded event
			Filepath = filepath;

			// Load the settings from xml. Block saving during load/upgrade
			using (Scope.Create(() => m_block_saving = true, () => m_block_saving = false))
				FromXml(settings);

			// Set readonly after loading is complete
			ReadOnly = read_only;
		}
		public void Load(Stream stream, bool read_only = false)
		{
			Log.Debug(this,"Loading settings stream");

			var settings = XDocument.Load(stream).Root;
			if (settings == null) throw new Exception("Invalidate settings source");

			// Set the filepath before loading so that it's valid for the SettingsLoaded event
			Filepath = string.Empty;

			// Load the settings from xml. Block saving during load/upgrade
			using (Scope.Create(() => m_block_saving = true, () => m_block_saving = false))
				FromXml(settings);

			// Set readonly after loading is complete
			ReadOnly = read_only;
		}

		/// <summary>Return the settings as xml</summary>
		public XElement ToXml()
		{
			return ToXml(new XElement("settings"));
		}
		public override XElement ToXml(XElement node)
		{
			node.Add2(VersionKey, Version, false);
			base.ToXml(node);
			return node;
		}

		/// <summary>Persist current settings to storage</summary>
		public void Save(string filepath)
		{
			if (m_block_saving)
				return;

			if (string.IsNullOrEmpty(filepath))
				throw new ArgumentNullException("filepath", "No settings filepath set");

			using (Scope.Create(() => m_block_saving = true, () => m_block_saving = false))
			{
				// Notify of a save about to happen
				var args = new SettingsSavingEventArgs(false);
				if (SettingsSaving != null) SettingsSaving(this, args);
				if (args.Cancel) return;

				// Ensure the save directory exists
				filepath = Path.GetFullPath(filepath);
				var path = Path.GetDirectoryName(filepath);
				if (!PathEx.DirExists(path))
					Directory.CreateDirectory(path);

				// Perform the save
				var settings = ToXml();
				settings.Save(filepath, SaveOptions.None);
			}
		}
		public void Save(Stream stream)
		{
			if (m_block_saving)
				return;

			using (Scope.Create(() => m_block_saving = true, () => m_block_saving = false))
			{
				// Notify of a save about to happen
				var args = new SettingsSavingEventArgs(false);
				if (SettingsSaving != null) SettingsSaving(this, args);
				if (args.Cancel) return;

				Log.Debug(this, "Saving settings to stream");

				// Perform the save
				var settings = ToXml();
				settings.Save(stream, SaveOptions.None);
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
	}

	#region Settings Event args
	public class SettingChangingEventArgs :CancelEventArgs
	{
		public string Key      { get; private set; }
		public object OldValue { get; private set; }
		public object NewValue { get; private set; }
		public SettingChangingEventArgs(string key, object old_value, object new_value, bool cancel)
		:base(cancel)
		{
			Key = key;
			OldValue = old_value;
			NewValue = new_value;
		}
		public SettingChangingEventArgs(string key, object old_value, object new_value)
		:this(key, old_value, new_value, false)
		{}
	}

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

	public class SettingsLoadedEventArgs :EventArgs
	{
		/// <summary>The location of where the settings were loaded from</summary>
		public string Filepath { get; private set; }
		public SettingsLoadedEventArgs(string filepath) { Filepath = filepath; }
	}

	public class SettingsSavingEventArgs :CancelEventArgs
	{
		public SettingsSavingEventArgs() :this(false) {}
		public SettingsSavingEventArgs(bool cancel) :base(cancel) {}
	}
	#endregion
}

#if PR_UNITTESTS
namespace pr.unittests
{
	using extn;

	[TestFixture] public class TestSettings
	{
		private sealed class SettingsThing
		{
			public int x,y,z;
				
			public SettingsThing()
			{
				x = 1;
				y = 2;
				z = 3;
			}
			public SettingsThing(XElement node)
			{
				x = node.Element("x").As<int>();
				y = node.Element("y").As<int>();
				z = node.Element("z").As<int>();
			}
			public XElement ToXml(XElement node)
			{
				node.Add2("x", x, false);
				node.Add2("y", y, false);
				node.Add2("z", z, false);
				return node;
			}
		}
		private sealed class SubSettings :SettingsSet<SubSettings>
		{
			public int Field     { get { return get(x => x.Field); } set { set(x => x.Field, value); } }
			public byte[] Buffer { get { return get(x => x.Buffer); } set { set(x => x.Buffer, value); } }
			public SubSettings() { Field = 2; Buffer = new byte[]{1,2,3}; }
		}
		private sealed class Settings :SettingsBase<Settings>
		{
			public string         Str    { get { return get(x => x.Str   ); } set { set(x => x.Str    , value); } }
			public int            Int    { get { return get(x => x.Int   ); } set { set(x => x.Int    , value); } }
			public DateTimeOffset DTO    { get { return get(x => x.DTO   ); } set { set(x => x.DTO    , value); } }
			public Font           Font   { get { return get(x => x.Font  ); } set { set(x => x.Font   , value); } }
			public float[]        Floats { get { return get(x => x.Floats); } set { set(x => x.Floats , value); } }
			public SubSettings    Sub    { get { return get(x => x.Sub   ); } set { set(x => x.Sub    , value); } }
			public XElement       Sub2   { get { return get(x => x.Sub2  ); } set { set(x => x.Sub2   , value); } }

			public Settings()
			{
				Str    = "default";
				Int    = 4;
				DTO    = DateTimeOffset.Parse("2013-01-02 12:34:56");
				Font   = SystemFonts.StatusFont;
				Floats = new[]{1f,2f,3f};
				Sub    = new SubSettings();
				Sub2   = new XElement("external");
			}
			public Settings(string filepath) :base(filepath) {}
			public Settings(XElement node) : base(node) {}
		}

		[Test] public void TestSettings1()
		{
			var s = new Settings();
			Assert.AreEqual(Settings.Default.Str, s.Str);
			Assert.AreEqual(Settings.Default.Int, s.Int);
			Assert.AreEqual(Settings.Default.DTO, s.DTO);
			Assert.AreEqual(Settings.Default.Font, s.Font);
			Assert.True(Settings.Default.Floats.SequenceEqual(s.Floats));
			Assert.AreEqual(Settings.Default.Sub.Field, s.Sub.Field);
			Assert.True(Settings.Default.Sub.Buffer.SequenceEqual(s.Sub.Buffer));
			Assert.True(Settings.Default.Sub2.Name == s.Sub2.Name);
			Assert.Throws(typeof(ArgumentNullException), s.Save); // no filepath set
		}
		[Test] public void TestSettings2()
		{
			var file = Path.GetTempFileName();
			var st = new SettingsThing();
			var s = new Settings
				{
					Str = "Changed",
					Int = 42,
					DTO = DateTimeOffset.UtcNow,
					Font = SystemFonts.DialogFont,
					Floats = new[]{4f,5f,6f},
					Sub = new SubSettings
					{
						Field = 12,
						Buffer = new byte[]{4,5,6}
					},
					Sub2 = st.ToXml(new XElement("external")),
				};
			var xml = s.ToXml();

			var S = new Settings(xml);

			Assert.AreEqual(s.Str       , S.Str);
			Assert.AreEqual(s.Int       , S.Int);
			Assert.AreEqual(s.DTO       , S.DTO);
			Assert.AreEqual(s.Font      , S.Font);
			Assert.True(s.Floats.SequenceEqual(S.Floats));
			Assert.AreEqual(s.Sub.Field , S.Sub.Field);
			Assert.True(s.Sub.Buffer.SequenceEqual(S.Sub.Buffer));

			var st2 = new SettingsThing(s.Sub2);
			Assert.AreEqual(st.x, st2.x);
			Assert.AreEqual(st.y, st2.y);
			Assert.AreEqual(st.z, st2.z);
		}
		[Test] public void TestEvents()
		{
			var i = 0;
			int changing = 0, changed = 0, saving = 0, loading = 0;

			var file = Path.GetTempFileName();
			var settings = new Settings();
			settings.SettingChanging += (s,a) => changing = ++i;
			settings.SettingChanged  += (s,a) => changed  = ++i;
			settings.SettingsSaving  += (s,a) => saving   = ++i;
			settings.SettingsLoaded  += (s,a) => loading  = ++i;

			settings.Str = "Modified";
			Assert.AreEqual(1, changing);
			Assert.AreEqual(2, changed);
			Assert.AreEqual(0, saving);
			Assert.AreEqual(0, loading);

			settings.Sub.Field = 23;
			Assert.AreEqual(3, changing);
			Assert.AreEqual(4, changed);
			Assert.AreEqual(0, saving);
			Assert.AreEqual(0, loading);

			settings.Save(file);
			Assert.AreEqual(3, changing);
			Assert.AreEqual(4, changed);
			Assert.AreEqual(5, saving);
			Assert.AreEqual(0, loading);

			settings.Load(file);
			Assert.AreEqual(3, changing);
			Assert.AreEqual(4, changed);
			Assert.AreEqual(5, saving);
			Assert.AreEqual(6, loading);
		}
	}
}

#endif
