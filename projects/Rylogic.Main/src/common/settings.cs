﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using System.Windows.Threading;
using System.Xml.Linq;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Gui;
using Rylogic.Utility;

// Example use of settings
#if false
public class Settings :SettingsBase<Settings>
{
	public Settings()
	{
		Name  = "default";
		Value = 4;
	}
	public Settings(string filepath)
		: base(filepath)
	{}

	public string Name
	{
		get { return get<string>(nameof(Name)); }
		set { set(nameof(Name), value); }
	}

	public int Value
	{
		get { return get<int>(nameof(Value)); }
		set { set(nameof(Value), value); }
	}
}
#endif

namespace Rylogic.Common
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
	public abstract class SettingsSet<T> :ISettingsSet ,INotifyPropertyChanged ,INotifyPropertyChanging where T:SettingsSet<T>, new()
	{
		/// <summary>The collection of settings</summary>
		protected readonly Dictionary<string, object> m_data;

		protected SettingsSet()
		{
			m_data = new Dictionary<string, object>();
		}

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

		/// <summary>Returns true if 'key' is present within the data</summary>
		protected bool has(string key)
		{
			return m_data.ContainsKey(key);
		}

		/// <summary>Read a settings value</summary>
		protected virtual Value get<Value>(string key)
		{
			if (m_data.TryGetValue(key, out var value) ||
				Default.m_data.TryGetValue(key, out value))
				return (Value)value;

			throw new KeyNotFoundException($"Unknown setting '{key}'.\r\n"+
				"This is probably because there is no default value set "+
				"in the constructor of the derived settings class");
		}

		/// <summary>Write a settings value</summary>
		protected virtual void set<Value>(string key, Value value)
		{
			// If 'value' is a nested setting, set this object as the parent
			SetParentIfNesting(value);

			// Key not in the data yet? Must be initial value from startup
			if (!m_data.TryGetValue(key, out var old))
			{
				m_data[key] = value;
				return;
			}

			if (ReadOnly)
				return;

			// If the values are the same, don't raise 'changing' events
			if (Equals(old, value))
				return;

			// Notify about to change
			var args = new SettingChangingEventArgs(key, old, value, false);
			OnSettingChanging(args); if (args.Cancel) return;
			PropertyChanging.Raise(this, new PropertyChangingEventArgs(key));

			// Update the value
			m_data[key] = value;

			// Notify changed
			OnSettingChanged(new SettingChangedEventArgs(key, old, value));
			PropertyChanged.Raise(this, new PropertyChangedEventArgs(key));
		}

		/// <summary>Return the settings as an XML node tree</summary>
		public virtual XElement ToXml(XElement node)
		{
			foreach (var d in m_data.OrderBy(x => x.Key))
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
			m_data.Clear();
			foreach (var setting in node.Elements())
			{
				var key = setting.Attribute("key")?.Value;
				if (key == null)
				{
					// Support old style settings
					var key_elem = setting.Element("key");
					var val_elem = setting.Element("value");
					if (key_elem == null || val_elem == null)
						throw new Exception($"Invalid setting element found: {setting}");

					key = key_elem.As<string>();
					var val = val_elem.ToObject();
					SetParentIfNesting(val);
					m_data[key] = val;
				}
				else
				{
					var val = setting.ToObject();
					SetParentIfNesting(val);
					m_data[key] = val;
				}
			}

			// Add any default options that aren't in the settings file
			// Use 'new T().Data' so that reference types can be used, otherwise we'll change the defaults
			foreach (var i in new T().m_data.Where(x => !has(x.Key)))
			{
				SetParentIfNesting(i.Value);

				// Key not in the data yet? Must be initial value from startup
				m_data[i.Key] = i.Value;
			}
		}

		/// <summary>An event raised when a setting is about to change value</summary>
		public event EventHandler<SettingChangingEventArgs> SettingChanging;

		/// <summary>An event raised after a setting has been changed</summary>
		public event EventHandler<SettingChangedEventArgs> SettingChanged;

		/// <summary>Property changed</summary>
		public event PropertyChangedEventHandler PropertyChanged;
		public event PropertyChangingEventHandler PropertyChanging;

		/// <summary>Manually notify of a setting change</summary>
		public void RaiseSettingChanged(string key)
		{
			var val = get<object>(key);
			OnSettingChanged(new SettingChangedEventArgs(key, val, val));
		}

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
			if (nested is ISettingsSet set)
				set.Parent = this;

			// Of if 'nested' is a collection of settings sets
			if (nested is IEnumerable<ISettingsSet> sets)
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
		protected SettingsBase(XElement node, bool throw_on_error, bool read_only = false)
			:this()
		{
			if (node != null)
			{
				try
				{
					FromXml(node, read_only);
					return;
				}
				catch (Exception ex)
				{
					SettingsEvent(ESettingsEvent.LoadFailed, ex, "Failed to load settings from XML data");
					if (throw_on_error) throw;
					ResetToDefaults(); // Fall back to default values
				}
			}
		}
		protected SettingsBase(Stream stream, bool throw_on_error, bool read_only = false)
			:this()
		{
			if (stream != null)
			{
				try
				{
					Load(stream, read_only);
					return;
				}
				catch (Exception ex)
				{
					SettingsEvent(ESettingsEvent.LoadFailed, ex, "Failed to load settings from stream");
					if (throw_on_error) throw;
					ResetToDefaults(); // Fall back to default values
				}
			}
		}
		protected SettingsBase(string filepath, bool throw_on_error, bool read_only = false) :this()
		{
			Debug.Assert(!string.IsNullOrEmpty(filepath));
			Filepath = filepath;
			try
			{
				Reload(read_only);
				return;
			}
			catch (Exception ex)
			{
				SettingsEvent(ESettingsEvent.LoadFailed, ex, $"Failed to load settings from {filepath}");
				if (throw_on_error) throw;
				ResetToDefaults(); // Fall back to default values
			}
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
			get { return Util2.ResolveAppPath("settings.xml"); }
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
					SettingChanged -= HandleSettingChanged;
				}
				m_auto_save = value;
				if (m_auto_save)
				{
					SettingChanged += HandleSettingChanged;
				}

				// Handlers
				void HandleSettingChanged(object sender, EventArgs args)
				{
					if (m_block_saving)
						return;

					// Save already pending?
					if (m_auto_save_trigger.Pending) return;
					m_auto_save_trigger.Signal();

					// Save settings soon, to batch up multiple changes
					Dispatcher.CurrentDispatcher.BeginInvokeDelayed(AutoSave, TimeSpan.FromMilliseconds(500));
				}
				void AutoSave()
				{
					m_auto_save_trigger.Actioned();
					Save();
				}
			}
		}
		private Trigger m_auto_save_trigger;
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

		/// <summary>Populate these settings from the default instance values</summary>
		private void ResetToDefaults()
		{
			// Use 'new T()' so that reference types can be used, otherwise we'll change the defaults
			// Use 'set' so that ISettingsSets are parented correctly
			foreach (var d in new T().m_data)
				set(d.Key, d.Value); 
		}

		/// <summary>Resets the persisted settings to their defaults</summary>
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
				ResetToDefaults();

				// Notify of load failure
				SettingsEvent(ESettingsEvent.LoadFailed, ex, $"Failed to load settings from {Filepath}");

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
				SettingsEvent(ESettingsEvent.FileNotFound, null, $"Settings file {filepath} not found, using defaults");
				Reset();
				return; // Reset will recursively call Load again
			}

			SettingsEvent(ESettingsEvent.LoadingSettings, null, $"Loading settings file {filepath}");

			var settings = XDocument.Load(filepath).Root;
			if (settings == null) throw new Exception($"Invalidate settings file ({filepath})");

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

				SettingsEvent(ESettingsEvent.SavingSettings, null, $"Saving settings to file {filepath}");

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
					SettingsEvent(ESettingsEvent.SaveFailed, ex, $"Failed to save settings to file {filepath}");
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

		/// <summary>Remove the settings file from persistent storage</summary>
		public void Delete()
		{
			if (Path_.FileExists(Filepath))
				File.Delete(Filepath);
		}

		/// <summary>Called when loading settings from an earlier version</summary>
		public virtual void Upgrade(XElement old_settings, string from_version)
		{
			// Boiler-plate:
			//// Preserve old settings
			//if (from_version != Version && Filepath.HasValue())
			//{
			//	// Note: Do not save over 'Filepath' just leave the settings upgraded in memory.
			//	// It's up to the caller whether settings should be saved.
			//	var extn = Path_.Extn(Filepath);
			//	var backup_filepath = Path.ChangeExtension(Filepath, $"backup_({from_version}){extn}");
			//	old_settings.Save(backup_filepath);
			//}
			//
			//for (; from_version != Version; )
			//{
			//	switch (from_version)
			//	{
			//	default:
			//		{
			//			base.Upgrade(old_settings, from_version);
			//			return;
			//		}
			//	case "v1.1":
			//		{
			//			#region Change description
			//			{
			//				// Modify the XML document using hard-coded element names.
			//				// Note: don't use constants for the element names because they
			//				// may get changed in the future. This is one case where magic
			//				// strings is actually the correct thing to do!
			//			}
			//			#endregion
			//			from_version = "v1.2";
			//			break;
			//		}
			//	}
			//}

			throw new NotSupportedException($"Settings file version is {from_version}. Latest version is {Version}. Upgrading from this version is not supported");
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
				MsgBox.Show(null, $"An error occurred that prevented settings being saved.\r\n\r\n{msg}\r\n{ex.Message}", "Save Settings", MessageBoxButtons.OK, MessageBoxIcon.Error);
				break;
			}
		}
	}

	/// <summary>A base class for a class that gets saved to/loaded from XML only</summary>
	public abstract class SettingsXml<T> :ISettingsSet ,INotifyPropertyChanged ,INotifyPropertyChanging where T:SettingsXml<T>, new()
	{
		private readonly Dictionary<string, object> m_data;

		protected SettingsXml()
		{
			m_data = new Dictionary<string, object>();
		}
		protected SettingsXml(XElement node)
			:this()
		{
			if (node != null)
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

				// Exceptions will bubble up to the root SettingsBase object
				return;
			}

			// Fall back to default values
			// Use 'new T().Data' so that reference types can be used, otherwise we'll change the defaults
			m_data = new T().m_data;
		}
		public virtual XElement ToXml(XElement node)
		{
			var ty = typeof(T);
			foreach (var pair in m_data.OrderBy(x => x.Key))
			{
				// Set the type attribute if the type of value is not the save as the type of the property
				var pi = ty.GetProperty(pair.Key, BindingFlags.Instance|BindingFlags.Public);
				var type_attr = pi != null && pair.Value != null && pi.PropertyType != pair.Value.GetType();

				node.Add2(pair.Key, pair.Value, type_attr);
			}

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

		/// <summary>Property changed</summary>
		public event PropertyChangedEventHandler PropertyChanged;
		public event PropertyChangingEventHandler PropertyChanging;

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
			if (m_data.TryGetValue(key, out var value))
				return (Value)value;

			if (Default.m_data.TryGetValue(key, out value))
				return (Value)value;

			throw new KeyNotFoundException(
				$"Unknown setting '{key}'.\r\n"+
				"This is probably because there is no default value set "+
				"in the constructor of the derived settings class");
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
			OnSettingChanging(args); if (args.Cancel) return;
			PropertyChanging.Raise(this, new PropertyChangingEventArgs(key));

			// Change the property
			m_data[key] = value;

			// Notify changed
			OnSettingChanged(new SettingChangedEventArgs(key, old_value, value));
			PropertyChanged.Raise(this, new PropertyChangedEventArgs(key));
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
namespace Rylogic.UnitTests
{
	using Extn;

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
			public int Field     { get { return get<int>(nameof(Field)); } set { set(nameof(Field), value); } }
			public byte[] Buffer { get { return get<byte[]>(nameof(Buffer)); } set { set(nameof(Buffer), value); } }
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
			public Settings(string filepath) :base(filepath, throw_on_error:false) {}
			public Settings(XElement node) : base(node, throw_on_error: false) {}

			public string         Str    { get { return get<string        >(nameof(Str   )); } set { set(nameof(Str   ) , value); } }
			public int            Int    { get { return get<int           >(nameof(Int   )); } set { set(nameof(Int   ) , value); } }
			public DateTimeOffset DTO    { get { return get<DateTimeOffset>(nameof(DTO   )); } set { set(nameof(DTO   ) , value); } }
			public Font           Font   { get { return get<Font          >(nameof(Font  )); } set { set(nameof(Font  ) , value); } }
			public float[]        Floats { get { return get<float[]       >(nameof(Floats)); } set { set(nameof(Floats) , value); } }
			public SubSettings    Sub    { get { return get<SubSettings   >(nameof(Sub   )); } set { set(nameof(Sub   ) , value); } }
			public XElement       Sub2   { get { return get<XElement      >(nameof(Sub2  )); } set { set(nameof(Sub2  ) , value); } }
			public object[]       Things { get { return get<object[]      >(nameof(Things)); } set { set(nameof(Things) , value); } }
		}

		[TestFixtureSetUp] public void Setup()
		{
			Xml.SupportWinFormsTypes();
		}
		[Test] public void TestSettings1()
		{
			var s = new Settings();
			Assert.Equal(Settings.Default.Str, s.Str);
			Assert.Equal(Settings.Default.Int, s.Int);
			Assert.Equal(Settings.Default.DTO, s.DTO);
			Assert.Equal(Settings.Default.Font, s.Font);
			Assert.True(Settings.Default.Floats.SequenceEqual(s.Floats));
			Assert.Equal(Settings.Default.Sub.Field, s.Sub.Field);
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

			Assert.Equal(s.Str       , S.Str);
			Assert.Equal(s.Int       , S.Int);
			Assert.Equal(s.DTO       , S.DTO);
			Assert.Equal(s.Font      , S.Font);
			Assert.True(s.Floats.SequenceEqual(S.Floats));
			Assert.Equal(s.Sub.Field , S.Sub.Field);
			Assert.True(s.Sub.Buffer.SequenceEqual(S.Sub.Buffer));
			Assert.True((string)s.Things[0] == "Hello");
			Assert.True((double)s.Things[1] == 6.28);

			var st2 = new SettingsThing(s.Sub2);
			Assert.Equal(st.x, st2.x);
			Assert.Equal(st.y, st2.y);
			Assert.Equal(st.z, st2.z);
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
			Assert.Equal(1, changing);
			Assert.Equal(2, changed);
			Assert.Equal(0, saving);
			Assert.Equal(0, loading);

			settings.Sub.Field = 23;
			Assert.Equal(3, changing);
			Assert.Equal(4, changed);
			Assert.Equal(0, saving);
			Assert.Equal(0, loading);

			settings.Save(file);
			Assert.Equal(3, changing);
			Assert.Equal(4, changed);
			Assert.Equal(5, saving);
			Assert.Equal(0, loading);

			settings.Load(file);
			Assert.Equal(3, changing);
			Assert.Equal(4, changed);
			Assert.Equal(5, saving);
			Assert.Equal(6, loading);
		}
	}
}
#endif