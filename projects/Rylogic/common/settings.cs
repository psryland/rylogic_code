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
using pr.gui;
using System.Windows.Forms;

// Example use of settings
//  private sealed class Settings :SettingsBase<Settings>
//  {
//  	public string Name  { get { return get(x => x.Name ); } set { set(x => x.Name , value); } }
//  	public int    Value { get { return get(x => x.Value); } set { set(x => x.Value, value); } }
//  	public Settings()
//  	{
//  		Name  = "default";
//  		Value = 4;
//  	}
//		public Settings(string filepath) :base(filepath) {}
//  }

namespace pr.common
{
	public enum ESettingsEvent
	{
		LoadFailed,
		NoVersion,
		FileNotFound,
		LoadingSettings,
		SavingSettings,
		SaveFailed,
	}

	/// <summary>A single setting</summary>
	[Serializable]
	[DebuggerDisplay("{Key}={Value}")]
	public class Setting
	{
		public Setting() {}
		public Setting(string key, object value)
		{
			Key   = key;
			Value = value;
		}
		public override string ToString()
		{
			return Key + "  " + Value;
		}

		/// <summary>The name of the setting variable</summary>
		public string Key
		{
			get;
			set;
		}

		/// <summary>The value of the setting</summary>
		public object Value
		{
			get;
			set;
		}
	}

	/// <summary>Common interface for 'SettingsSet' and 'SettingsBase'</summary>
	public interface ISettingsSet
	{
		/// <summary>Parent settings for this settings object</summary>
		ISettingsSet Parent { get; set; }

		/// <summary>An event raised when a setting is about to change value</summary>
		event EventHandler<SettingChangingEventArgs> SettingChanging;

		/// <summary>An event raised after a setting has been changed</summary>
		event EventHandler<SettingChangedEventArgs> SettingChanged;

		/// <summary>Called just before a setting changes</summary>
		void OnSettingChanging(SettingChangingEventArgs args);

		/// <summary>Called just after a setting changes</summary>
		void OnSettingChanged(SettingChangedEventArgs args);
	}

	/// <summary>A base class for settings structures</summary>
	[Serializable]
	public abstract class SettingsSet<T> :ISettingsSet where T:SettingsSet<T>, new()
	{
		protected SettingsSet()
		{
			Data = new List<Setting>();
		}

		/// <summary>The collection of settings</summary>
		protected readonly List<Setting> Data;

		/// <summary>The default values for the settings</summary>
		public static T Default
		{
			get { return m_default ?? (m_default = new T()); }
		}
		private static T m_default;

		/// <summary>True to block all writes to the settings</summary>
		[Browsable(false)]
		public bool ReadOnly { get; set; }

		/// <summary>Parent settings for this settings object</summary>
		[Browsable(false)]
		public ISettingsSet Parent
		{
			get { return m_parent; }
			set { m_parent = value; } // could notify of parent changed...
		}
		private ISettingsSet m_parent;

		/// <summary>Returns the top-most settings set</summary>
		public ISettingsSet Root
		{
			get
			{
				ISettingsSet root;
				for (root = this; root.Parent != null; root = root.Parent) {}
				return root as T;
			}
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
			return get<Value>(R<T>.Name(expression));
		}

		/// <summary>Write a settings value</summary>
		protected virtual void set<Value>(string key, Value value)
		{
			if (ReadOnly)
				return;

			// If 'value' is a nested setting, set this object as the parent
			SetParentIfNesting(value);

			// Key not in the data yet? Must be initial value from startup
			int idx = index(key);
			if (idx < 0) { Data.Insert(~idx, new Setting{Key = key, Value = value}); return; }

			object old_value = Data[idx].Value;
			if (Equals(old_value, value)) return; // If the values are the same, don't raise 'changing' events

			var args = new SettingChangingEventArgs(key, old_value, value, false);
			OnSettingChanging(args);
			if (args.Cancel) return;
			Data[idx].Value = value;
			OnSettingChanged(new SettingChangedEventArgs(key, old_value, value));
		}

		/// <summary>Write a settings value</summary>
		protected void set<Value>(Expression<Func<T,Value>> expression, Value value)
		{
			set(R<T>.Name(expression), value);
		}

		/// <summary>Return the settings as an XML node tree</summary>
		public virtual XElement ToXml(XElement node)
		{
			foreach (var d in Data)
			{
				var elem = node.Add2(new XElement("setting"));
				elem.SetAttributeValue("key", d.Key);
				d.Value.ToXml(elem, true);
			}
			return node;
		}

		/// <summary>Populate this object from XML</summary>
		public virtual void FromXml(XElement node)
		{
			// Load data from settings
			Data.Clear();
			foreach (var setting in node.Elements())
			{
				var key = setting.Attribute("key")?.Value;
				if (key == null)
				{
					// Support old style settings
					var key_elem = setting.Element("key");
					var val_elem = setting.Element("value");
					if (key_elem == null || val_elem == null)
						throw new Exception("Invalid setting element found: {0}".Fmt(setting));

					key = key_elem.As<string>();
					var val = val_elem.ToObject();
					SetParentIfNesting(val);
					Data.Add(new Setting(key, val));
				}
				else
				{
					var val = setting.ToObject();
					SetParentIfNesting(val);
					Data.Add(new Setting(key, val));
				}
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

				SetParentIfNesting(i.Value);

				// Key not in the data yet? Must be initial value from startup
				Data.Insert(~idx, new Setting(i.Key, i.Value));
			}
		}

		/// <summary>An event raised when a setting is about to change value</summary>
		public event EventHandler<SettingChangingEventArgs> SettingChanging;

		/// <summary>An event raised after a setting has been changed</summary>
		public event EventHandler<SettingChangedEventArgs> SettingChanged;

		/// <summary>Called just before a setting changes</summary>
		protected virtual void OnSettingChanging(SettingChangingEventArgs args)
		{
			SettingChanging.Raise(this, args);
			if (Parent == null) return;
			Parent.OnSettingChanging(args);
		}
		void ISettingsSet.OnSettingChanging(SettingChangingEventArgs args)
		{
			OnSettingChanging(args);
		}

		/// <summary>Called just after a setting changes</summary>
		protected virtual void OnSettingChanged(SettingChangedEventArgs args)
		{
			SettingChanged.Raise(this, args);
			var me = (ISettingsSet)this;
			if (me.Parent == null) return;
			me.Parent.OnSettingChanged(args);
		}
		void ISettingsSet.OnSettingChanged(SettingChangedEventArgs args)
		{
			OnSettingChanged(args);
		}

		/// <summary>If 'nested' is not null, sets its Parent to this settings object</summary>
		private void SetParentIfNesting(object nested)
		{
			if (nested == null)
				return;

			// If 'nested' is a settings set
			var set = nested as ISettingsSet;
			if (set != null)
				set.Parent = this;

			// Of if 'nested' is a collection of settings sets
			var sets = nested as IEnumerable<ISettingsSet>;
			if (sets != null)
				sets.ForEach(x => x.Parent = this);
		}
	}

	/// <summary>A base class for simple settings</summary>
	[Serializable]
	public abstract class SettingsBase<T> :SettingsSet<T> where T:SettingsBase<T>, new()
	{
		protected SettingsBase()
		{
			m_filepath = "";
			SettingsEvent = null;

			// Default to off so users can enable after startup completes
			AutoSaveOnChanges = false;
		}
		protected SettingsBase(SettingsBase<T> rhs, bool read_only = false)
			:this(rhs.ToXml(new XElement("root")), read_only)
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
				SettingsEvent(ESettingsEvent.LoadFailed, ex, "Failed to load settings from XML data");
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
				SettingsEvent(ESettingsEvent.LoadFailed, ex, "Failed to load settings from stream");
				Data.AddRange(new T().Data);
			}
		}
		protected SettingsBase(string filepath, bool read_only = false) :this()
		{
			Debug.Assert(!string.IsNullOrEmpty(filepath));
			Filepath = filepath;
			Reload(read_only);
		}

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

		/// <summary>Returns a filepath for storing app settings in the app data directory</summary>
		public static string DefaultFilepath
		{
			get { return Path.Combine(DefaultAppDataDirectory, "settings.xml"); }
		}

		/// <summary>Returns a filepath for storing settings in the same directory as the application executable</summary>
		public static string DefaultLocalFilepath
		{
			get { return Util.ResolveAppPath("settings.xml"); }
		}

		/// <summary>The settings version, used to detect when 'Upgrade' is needed</summary>
		protected virtual string Version
		{
			get { return "v1.0"; }
		}
		public const string VersionKey = "__SettingsVersion";

		/// <summary>Returns the filepath for the persisted settings file. Settings cannot be saved until this property has a valid filepath</summary>
		public string Filepath
		{
			get { return m_filepath; }
			set { m_filepath = value ?? string.Empty; }
		}
		private string m_filepath;

		/// <summary>Get/Set whether to automatically save whenever a setting is changed</summary>
		public bool AutoSaveOnChanges
		{
			get { return m_auto_save; }
			set
			{
				if (m_auto_save == value) return;
				if (m_auto_save)
				{
					SettingChanged -= Save;
				}
				m_auto_save = value;
				if (m_auto_save)
				{
					SettingChanged += Save;
				}
			}
		}
		private bool m_auto_save;

		/// <summary>An event raised whenever the settings are loaded from persistent storage</summary>
		public event EventHandler<SettingsLoadedEventArgs> SettingsLoaded;

		/// <summary>An event raised whenever the settings are about to be saved to persistent storage</summary>
		public event EventHandler<SettingsSavingEventArgs> SettingsSaving;

		/// <summary>Called whenever an error or warning condition occurs. By default, this function calls 'OnSettingsError'</summary>
		public Action<ESettingsEvent, Exception, string> SettingsEvent
		{
			get { return m_impl_settings_error; }
			set { m_impl_settings_error = value ?? ((err,ex,msg) => OnSettingsEvent(err,ex,msg)); }
		}
		private Action<ESettingsEvent, Exception, string> m_impl_settings_error;

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
				foreach (var d in new T().Data) // use 'set' so that ISettingsSets are parented correctly
					set(d.Key, d.Value);

				// Notify of load failure
				SettingsEvent(ESettingsEvent.LoadFailed, ex, "Failed to load settings from {0}".Fmt(Filepath));

				// Parse the exception up
				throw;
			}
		}

		/// <summary>Load the settings from XML</summary>
		public override void FromXml(XElement node)
		{
			// Upgrade old settings
			var vers = node.Element(VersionKey);
			if (vers == null)
			{
				SettingsEvent(ESettingsEvent.NoVersion, null, "Settings data does not contain a version, using defaults");
				Reset();
				return; // Reset will recursively call Load again
			}

			vers.Remove();
			if (vers.Value != Version)
				Upgrade(node, vers.Value);

			// Load the settings from XML
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
			if (!Path_.FileExists(filepath))
			{
				SettingsEvent(ESettingsEvent.FileNotFound, null, "Settings file {0} not found, using defaults".Fmt(filepath));
				Reset();
				return; // Reset will recursively call Load again
			}

			SettingsEvent(ESettingsEvent.LoadingSettings, null, "Loading settings file {0}".Fmt(filepath));

			var settings = XDocument.Load(filepath).Root;
			if (settings == null) throw new Exception("Invalidate settings file ({0})".Fmt(filepath));

			// Set the filepath before loading so that it's valid for the SettingsLoaded event
			Filepath = filepath;

			// Load the settings from XML. Block saving during load/upgrade
			using (Scope.Create(() => m_block_saving = true, () => m_block_saving = false))
				FromXml(settings);

			// Set readonly after loading is complete
			ReadOnly = read_only;
		}
		public void Load(Stream stream, bool read_only = false)
		{
			SettingsEvent(ESettingsEvent.LoadingSettings, null, "Loading settings from stream");

			var settings = XDocument.Load(stream).Root;
			if (settings == null) throw new Exception("Invalidate settings source");

			// Set the filepath before loading so that it's valid for the SettingsLoaded event
			Filepath = string.Empty;

			// Load the settings from XML. Block saving during load/upgrade
			using (Scope.Create(() => m_block_saving = true, () => m_block_saving = false))
				FromXml(settings);

			// Set readonly after loading is complete
			ReadOnly = read_only;
		}

		/// <summary>Return the settings as XML</summary>
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

				SettingsEvent(ESettingsEvent.SavingSettings, null, "Saving settings to file {0}".Fmt(filepath));

				// Ensure the save directory exists
				filepath = Path.GetFullPath(filepath);
				var path = Path.GetDirectoryName(filepath);
				if (!Path_.DirExists(path))
					Directory.CreateDirectory(path);

				try
				{
					// Perform the save
					var settings = ToXml();
					settings.Save(filepath, SaveOptions.None);
				}
				catch (Exception ex)
				{
					SettingsEvent(ESettingsEvent.SaveFailed, ex, "Failed to save settings to file {0}".Fmt(filepath));
				}
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

				SettingsEvent(ESettingsEvent.SavingSettings, null, "Saving settings to stream");

				try
				{
					// Perform the save
					var settings = ToXml();
					settings.Save(stream, SaveOptions.None);
				}
				catch (Exception ex)
				{
					SettingsEvent(ESettingsEvent.SaveFailed, ex, "Failed to save settings to the given stream");
				}
			}
		}
		private bool m_block_saving;

		/// <summary>Save using the last filepath</summary>
		public void Save()
		{
			Save(Filepath);
		}
		public void Save(object sender, EventArgs args)
		{
			Save();
		}

		/// <summary>Remove the settings file from persistent storage</summary>
		public void Delete()
		{
			if (Path_.FileExists(Filepath))
				File.Delete(Filepath);
		}

		/// <summary>Called when loading settings from an earlier version</summary>
		public virtual void Upgrade(XElement old_settings, string from_version)
		{
			throw new NotSupportedException("Settings file version is {0}. Latest version is {1}. Upgrading from this version is not supported".Fmt(from_version, Version));
		}

		/// <summary>Perform validation on the loaded settings</summary>
		public virtual void Validate() {}

		/// <summary>Default handling of settings errors/warnings</summary>
		public virtual void OnSettingsEvent(ESettingsEvent err, Exception ex, string msg)
		{
			switch (err)
			{
			default:
				Debug.Assert(false, "Unknown settings event type");
				Log.Exception(this, ex, msg);
				break;
			case ESettingsEvent.LoadingSettings:
			case ESettingsEvent.SavingSettings:
				//Log.Debug(this, msg);
				break;
			case ESettingsEvent.NoVersion:
				Log.Info(this, msg);
				break;
			case ESettingsEvent.FileNotFound:
				Log.Warn(this, msg);
				break;
			case ESettingsEvent.LoadFailed:
				Log.Exception(this, ex, msg);
				break;
			case ESettingsEvent.SaveFailed:
				// By default, show an error message box. User code can prevent this by replacing
				// the SettingsEvent action, or overriding OnSettingsEvent
				MsgBox.Show(null, "An error occurred that prevented settings being saved.\r\n\r\n{0}\r\n{1}".Fmt(msg, ex.Message), "Save Settings", MessageBoxButtons.OK, MessageBoxIcon.Error);
				break;
			}
		}
	}

	/// <summary>A base class for a class that gets saved to/loaded from XML only</summary>
	public abstract class SettingsXml<T> :ISettingsSet where T:SettingsXml<T>, new()
	{
		private readonly Dictionary<string, object> m_data;

		protected SettingsXml()
		{
			m_data = new Dictionary<string, object>();
		}
		protected SettingsXml(XElement node) :this()
		{
			Upgrade(node);

			var ty = typeof(T);
			foreach (var n in node.Elements())
			{
				// Use the type to determine the type of the XML element
				var prop_name = n.Name.LocalName;
				var pi = ty.GetProperty(prop_name, BindingFlags.Instance|BindingFlags.Public);

				// Ignore XML values that are no longer properties of 'T'
				if (pi == null) continue;
				m_data[prop_name] = n.As(pi.PropertyType);
			}
		}
		public virtual XElement ToXml(XElement node)
		{
			foreach (var pair in m_data.OrderBy(x => x.Key))
				node.Add2(pair.Key, pair.Value);

			return node;
		}

		/// <summary>The default values for the settings</summary>
		public static T Default { get { return m_default ?? (m_default = new T()); } }
		private static T m_default;

		/// <summary>True to block all writes to the settings</summary>
		[Browsable(false)]
		public bool ReadOnly { get; set; }

		/// <summary>Parent settings for this settings object</summary>
		[Browsable(false)]
		public ISettingsSet Parent
		{
			get { return m_parent; }
			set { m_parent = value; } // could notify of parent changed...
		}
		private ISettingsSet m_parent;

		/// <summary>An event raised when a setting is about to change value</summary>
		public event EventHandler<SettingChangingEventArgs> SettingChanging;

		/// <summary>An event raised after a setting has been changed</summary>
		public event EventHandler<SettingChangedEventArgs> SettingChanged;

		/// <summary>Called just before a setting changes</summary>
		protected virtual void OnSettingChanging(SettingChangingEventArgs args)
		{
			SettingChanging.Raise(this, args);
			if (Parent == null) return;
			Parent.OnSettingChanging(args);
		}
		void ISettingsSet.OnSettingChanging(SettingChangingEventArgs args)
		{
			OnSettingChanging(args);
		}

		/// <summary>Called just after a setting changes</summary>
		protected virtual void OnSettingChanged(SettingChangedEventArgs args)
		{
			SettingChanged.Raise(this, args);
			var me = (ISettingsSet)this;
			if (me.Parent == null) return;
			me.Parent.OnSettingChanged(args);
		}
		void ISettingsSet.OnSettingChanged(SettingChangedEventArgs args)
		{
			OnSettingChanged(args);
		}

		/// <summary>Read a settings value</summary>
		protected virtual Value get<Value>(string key)
		{
			object value;
			if (m_data.TryGetValue(key, out value))
				return (Value)value;

			var ty = typeof(T);
			var pi = ty.GetProperty(key, BindingFlags.Instance|BindingFlags.Public);
			if (pi != null)
				return (Value)pi.GetValue(Default);

			throw new KeyNotFoundException("Unknown setting '"+key+"'.\r\n"+
				"This is probably because there is no default value set "+
				"in the constructor of the derived settings class");
		}

		/// <summary>Read a settings value</summary>
		protected Value get<Value>(Expression<Func<T,Value>> expression)
		{
			return get<Value>(R<T>.Name(expression));
		}

		/// <summary>Write a settings value</summary>
		protected virtual void set<Value>(string key, Value value)
		{
			// If the property doesn't exist, this is construction, just set the value
			if (!m_data.ContainsKey(key))
			{
				m_data[key] = value;
				return;
			}

			if (ReadOnly)
				return;

			// Otherwise, test for changes and notify
			var old_value = (Value)m_data[key];
			if (Equals(old_value, value)) return;

			// Notify about to change
			var args = new SettingChangingEventArgs(key, old_value, value, false);
			OnSettingChanging(args);
			if (args.Cancel) return;

			// Change the property
			m_data[key] = value;

			// Notify changed
			OnSettingChanged(new SettingChangedEventArgs(key, old_value, value));
		}

		/// <summary>Write a settings value</summary>
		protected void set<Value>(Expression<Func<T,Value>> expression, Value value)
		{
			set(R<T>.Name(expression), value);
		}

		/// <summary>Allow XML settings to up converted to the latest version before being loaded</summary>
		protected virtual void Upgrade(XElement node)
		{
			// User code will need to add versions to deal with this
		}
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
			public int x,y,z;
		}
		private sealed class SubSettings :SettingsSet<SubSettings>
		{
			public SubSettings() { Field = 2; Buffer = new byte[]{1,2,3}; }
			public int Field     { get { return get(x => x.Field); } set { set(x => x.Field, value); } }
			public byte[] Buffer { get { return get(x => x.Buffer); } set { set(x => x.Buffer, value); } }
		}
		private sealed class Settings :SettingsBase<Settings>
		{
			public Settings()
			{
				Str    = "default";
				Int    = 4;
				DTO    = DateTimeOffset.Parse("2013-01-02 12:34:56");
				Font   = SystemFonts.StatusFont;
				Floats = new[]{1f,2f,3f};
				Sub    = new SubSettings();
				Sub2   = new XElement("external");
				Things = new object[] { 1, 2.3f, "hello" };
			}
			public Settings(string filepath) :base(filepath) {}
			public Settings(XElement node) : base(node) {}

			public string         Str    { get { return get(x => x.Str   ); } set { set(x => x.Str    , value); } }
			public int            Int    { get { return get(x => x.Int   ); } set { set(x => x.Int    , value); } }
			public DateTimeOffset DTO    { get { return get(x => x.DTO   ); } set { set(x => x.DTO    , value); } }
			public Font           Font   { get { return get(x => x.Font  ); } set { set(x => x.Font   , value); } }
			public float[]        Floats { get { return get(x => x.Floats); } set { set(x => x.Floats , value); } }
			public SubSettings    Sub    { get { return get(x => x.Sub   ); } set { set(x => x.Sub    , value); } }
			public XElement       Sub2   { get { return get(x => x.Sub2  ); } set { set(x => x.Sub2   , value); } }
			public object[]       Things { get { return get(x => x.Things); } set { set(x => x.Things , value); } }
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
			Assert.True((int)Settings.Default.Things[0] == 1);
			Assert.True((float)Settings.Default.Things[1] == 2.3f);
			Assert.True((string)Settings.Default.Things[2] == "hello");
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
				Things = new object[] { "Hello", 6.28 },
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
			Assert.True((string)s.Things[0] == "Hello");
			Assert.True((double)s.Things[1] == 6.28);

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
