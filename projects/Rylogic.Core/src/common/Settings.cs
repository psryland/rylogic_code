﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml.Linq;
using Rylogic.Common;
using Rylogic.Extn;
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
	/// <summary>Common interface for 'SettingsSet' and 'SettingsBase'</summary>
	public interface ISettingsSet
	{
		/// <summary>Parent settings for this settings object</summary>
		ISettingsSet Parent { get; set; }

		/// <summary>Find the key that corresponds to 'value'</summary>
		IReadOnlyDictionary<string, object> Data { get; }

		/// <summary>An event raised when a setting is about to change value</summary>
		event EventHandler<SettingChangeEventArgs> SettingChange;

		/// <summary>Called before and after a setting changes</summary>
		void OnSettingChange(SettingChangeEventArgs args);
	}

	/// <summary>A base class for settings structures</summary>
	public abstract class SettingsSet<T> :ISettingsSet, INotifyPropertyChanged, INotifyPropertyChanging
		where T:SettingsSet<T>, new()
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
			set
			{
				if (m_parent == value) return;
				m_parent = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Parent)));
			}
		}
		private ISettingsSet m_parent;

		/// <summary>Find the key that corresponds to 'value'</summary>
		public IReadOnlyDictionary<string, object> Data => m_data;

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
			var found = m_data.TryGetValue(key, out var value) || Default.m_data.TryGetValue(key, out value);
			if (!found)
				throw new KeyNotFoundException($"Unknown setting '{key}'.\r\n"+
					"This is probably because there is no default value set "+
					"in the constructor of the derived settings class");

			return
				value is Value v ? v :
				value != null ? Util.ConvertTo<Value>(value) :
				default(Value);
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
			var args0 = new SettingChangeEventArgs(this, key, old, true);
			OnSettingChange(args0); if (args0.Cancel) return;
			PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(key));

			// Update the value
			m_data[key] = value;

			// Notify changed
			var args1 = new SettingChangeEventArgs(this, key, value, false);
			OnSettingChange(args1);
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(key));
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
		public virtual void FromXml(XElement node, ESettingsLoadFlags flags)
		{
			m_data.Clear();
			foreach (var setting in node.Elements())
			{
				var key = setting.Attribute("key")?.Value;
				try
				{
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
				catch (TypeLoadException)
				{
					if (flags.HasFlag(ESettingsLoadFlags.IgnoreUnknownTypes)) continue;
					throw;
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
		public void FromXml(XElement node)
		{
			FromXml(node, ESettingsLoadFlags.None);
		}

		/// <summary>An event raised before and after a setting is changes value</summary>
		public event EventHandler<SettingChangeEventArgs> SettingChange;

		/// <summary>Property changed</summary>
		public event PropertyChangedEventHandler PropertyChanged;
		public event PropertyChangingEventHandler PropertyChanging;

		/// <summary>Manually notify of a setting change</summary>
		public void NotifySettingChanged(string key)
		{
			var val = get<object>(key);
			OnSettingChange(new SettingChangeEventArgs(this, key, val, false));
		}
		public void NotifyAllSettingsChanged()
		{
			foreach (var key in Data.Keys)
				NotifySettingChanged(key);
		}

		/// <summary>Called before and after a setting changes</summary>
		protected virtual void OnSettingChange(SettingChangeEventArgs args)
		{
			SettingChange?.Invoke(this, args);
			Parent?.OnSettingChange(args);
		}
		void ISettingsSet.OnSettingChange(SettingChangeEventArgs args)
		{
			OnSettingChange(args);
		}

		/// <summary>If 'nested' is not null, sets its Parent to this settings object</summary>
		private void SetParentIfNesting(object nested)
		{
			if (nested == null)
				return;

			// If 'nested' is a settings set
			if (nested is ISettingsSet set)
				set.Parent = this;

			// Or if 'nested' is a collection of settings sets
			if (nested is IEnumerable<ISettingsSet> sets)
				sets.ForEach(x => x.Parent = this);
		}
	}

	/// <summary>A base class for simple settings</summary>
	public abstract class SettingsBase<T> :SettingsSet<T>
		where T:SettingsBase<T>, new()
	{
		public const string VersionKey = "__SettingsVersion";

		protected SettingsBase()
		{
			m_filepath = "";
			SettingsEvent = null;
			BackupOldSettings = true;

			// Default to off so users can enable after startup completes
			AutoSaveOnChanges = false;
		}
		protected SettingsBase(XElement node, ESettingsLoadFlags flags = ESettingsLoadFlags.None)
			:this()
		{
			if (node != null)
			{
				try
				{
					FromXml(node, flags);
					return;
				}
				catch (Exception ex)
				{
					SettingsEvent(ESettingsEvent.LoadFailed, ex, "Failed to load settings from XML data");
					if (flags.HasFlag(ESettingsLoadFlags.ThrowOnError)) throw;
					ResetToDefaults(); // Fall back to default values
				}
			}
		}
		protected SettingsBase(Stream stream, ESettingsLoadFlags flags = ESettingsLoadFlags.None)
			:this()
		{
			if (stream != null)
			{
				try
				{
					Load(stream, flags);
					return;
				}
				catch (Exception ex)
				{
					SettingsEvent(ESettingsEvent.LoadFailed, ex, "Failed to load settings from stream");
					if (flags.HasFlag(ESettingsLoadFlags.ThrowOnError)) throw;
					ResetToDefaults(); // Fall back to default values
				}
			}
		}
		protected SettingsBase(string filepath, ESettingsLoadFlags flags = ESettingsLoadFlags.None)
			: this()
		{
			if (!string.IsNullOrEmpty(filepath))
			{
				try
				{
					Load(filepath, flags);
					return;
				}
				catch (Exception ex)
				{
					SettingsEvent(ESettingsEvent.LoadFailed, ex, $"Failed to load settings from {filepath}");
					if (flags.HasFlag(ESettingsLoadFlags.ThrowOnError)) throw;
					ResetToDefaults(); // Fall back to default values
				}
			}
		}
		protected SettingsBase(SettingsBase<T> rhs, ESettingsLoadFlags flags = ESettingsLoadFlags.None)
			: this(rhs.ToXml(new XElement("root")), flags)
		{ }

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
		public virtual string Version
		{
			get { return "v1.0"; }
		}

		/// <summary>The version number that was loaded (and possibly upgraded from)</summary>
		public string LoadedVersion { get; private set; }

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
					SettingChange -= HandleSettingChange;
				}
				m_auto_save = value;
				if (m_auto_save)
				{
					SettingChange += HandleSettingChange;
				}

				// Handlers
				async void HandleSettingChange(object sender, EventArgs args)
				{
					if (m_block_saving)
						return;

					// Save already pending?
					if (m_auto_save_pending) return;
					using (Scope.Create(() => m_auto_save_pending = true, () => m_auto_save_pending = false))
					{
						await Task.Delay(TimeSpan.FromMilliseconds(500));
						Save();
					}
				}
			}
		}
		private bool m_auto_save_pending;
		private bool m_auto_save;

		/// <summary>An event raised whenever the settings are loaded from persistent storage</summary>
		public event EventHandler<SettingsLoadedEventArgs> SettingsLoaded;

		/// <summary>An event raised whenever the settings are about to be saved to persistent storage</summary>
		public event EventHandler<SettingsSavingEventArgs> SettingsSaving;

		/// <summary>Called whenever an error or warning condition occurs. By default, this function calls 'OnSettingsError'</summary>
		public Action<ESettingsEvent, Exception, string> SettingsEvent
		{
			get { return m_impl_settings_error ?? ((err, ex, msg) => OnSettingsEvent(err, ex, msg)); }
			set { m_impl_settings_error = value; }
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
			Load(Filepath);
		}

		/// <summary>Load the settings from XML</summary>
		public override void FromXml(XElement node, ESettingsLoadFlags flags = ESettingsLoadFlags.None)
		{
			// Read the settings version
			var vers = node.Element(VersionKey);
			if (vers == null)
			{
				SettingsEvent(ESettingsEvent.NoVersion, null, "Settings data does not contain a version, using defaults");
				Reset();
				return; // Reset will recursively call Load again
			}

			// Save the loaded version number
			LoadedVersion = vers.Value;

			// Upgrade old settings
			if (vers.Value != Version)
			{
				// Backup old settings
				if (BackupOldSettings && Filepath.HasValue())
				{
					var backup_filepath = Path.ChangeExtension(Filepath, $"backup_({vers.Value}){Path_.Extn(Filepath)}");
					node.Save(backup_filepath);
				}

				// Upgrade the settings in memory
				Upgrade(node, vers.Value);
			}

			// Remove the version number to support old-style settings
			// where the element name was interpreted as the setting name.
			vers.Remove();

			// Load the settings from XML
			base.FromXml(node, flags);
			Validate();

			// Set readonly after loading is complete
			ReadOnly = flags.HasFlag(ESettingsLoadFlags.ReadOnly);

			// Notify of settings loaded
			SettingsLoaded?.Invoke(this,new SettingsLoadedEventArgs(Filepath));
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

		/// <summary>
		/// Refreshes the settings from a file storage.
		/// Starts with a copy of the Default.Data, then overwrites settings with those described in 'filepath'
		/// After load, the settings is the union of Default.Data and those in the given file.</summary>
		public void Load(string filepath, ESettingsLoadFlags flags = ESettingsLoadFlags.None)
		{
			if (!Path_.FileExists(filepath))
			{
				SettingsEvent(ESettingsEvent.FileNotFound, null, $"Settings file {filepath} not found, using defaults");
				Filepath = filepath;
				Reset();
				return; // Reset will recursively call Load again
			}

			SettingsEvent(ESettingsEvent.LoadingSettings, null, $"Loading settings file {filepath}");

			var settings = XDocument.Load(filepath).Root;
			if (settings == null)
				throw new Exception($"Invalidate settings file ({filepath})");

			// Set the filepath before loading so that it's valid for the SettingsLoaded event
			Filepath = filepath;

			// Load the settings from XML. Block saving during load/upgrade
			using (Scope.Create(() => m_block_saving = true, () => m_block_saving = false))
				FromXml(settings, flags);
		}
		public void Load(Stream stream, ESettingsLoadFlags flags = ESettingsLoadFlags.None)
		{
			SettingsEvent(ESettingsEvent.LoadingSettings, null, "Loading settings from stream");

			var settings = XDocument.Load(stream).Root;
			if (settings == null) throw new Exception("Invalidate settings source");

			// Set the filepath before loading so that it's valid for the SettingsLoaded event
			Filepath = string.Empty;

			// Load the settings from XML. Block saving during load/upgrade
			using (Scope.Create(() => m_block_saving = true, () => m_block_saving = false))
				FromXml(settings, flags);
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
				SettingsSaving?.Invoke(this, args);
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
				SettingsSaving?.Invoke(this, args);
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

		/// <summary>Save if 'AutoSaveOnChanges' is true</summary>
		public void AutoSave()
		{
			if (!AutoSaveOnChanges) return;
			Save();
		}

		/// <summary>Remove the settings file from persistent storage</summary>
		public void Delete()
		{
			if (Path_.FileExists(Filepath))
				File.Delete(Filepath);
		}

		/// <summary>Backup old settings before upgrades</summary>
		public bool BackupOldSettings { get; set; }

		/// <summary>Called when loading settings from an earlier version</summary>
		public virtual void Upgrade(XElement old_settings, string from_version)
		{
			// Boiler-plate:
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
				//MsgBox.Show(null, $"An error occurred that prevented settings being saved.\r\n\r\n{msg}\r\n{ex.Message}", "Save Settings", MessageBoxButtons.OK, MessageBoxIcon.Error);
				break;
			}
		}
	}

	/// <summary>A base class for a class that gets saved to/loaded from XML only</summary>
	public abstract class SettingsXml<T> :ISettingsSet, INotifyPropertyChanged, INotifyPropertyChanging
		where T:SettingsXml<T>, new()
	{
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
		public static T Default => m_default ?? (m_default = new T());
		private static T m_default;

		/// <summary>True to block all writes to the settings</summary>
		[Browsable(false)]
		public bool ReadOnly { get; set; }

		/// <summary>Parent settings for this settings object</summary>
		[Browsable(false)]
		public ISettingsSet Parent
		{
			get { return m_parent; }
			set
			{
				if (m_parent == value) return;
				if (m_parent != null)
				{
					ParentKey = null;
				}
				m_parent = value;
				if (m_parent != null)
				{
					// Find the key that corresponds to 'this' in the parent
					ParentKey = m_parent.Data.FirstOrDefault(x => Equals(x.Value, value)).Key;
				}
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Parent)));
			}
		}
		private ISettingsSet m_parent;

		/// <summary>The key for 'this' in 'Parent'</summary>
		protected string ParentKey { get; set; }

		/// <summary>Find the key that corresponds to 'value'</summary>
		public IReadOnlyDictionary<string, object> Data => m_data;
		private readonly Dictionary<string, object> m_data;

		/// <summary>Raised before and after a setting changes</summary>
		public event EventHandler<SettingChangeEventArgs> SettingChange;

		/// <summary>Property changed</summary>
		public event PropertyChangedEventHandler PropertyChanged;
		public event PropertyChangingEventHandler PropertyChanging;

		/// <summary>Called before and after a setting changes</summary>
		protected virtual void OnSettingChange(SettingChangeEventArgs args)
		{
			SettingChange?.Invoke(this, args);
			if (Parent == null) return;
			Parent.OnSettingChange(args);
		}
		void ISettingsSet.OnSettingChange(SettingChangeEventArgs args)
		{
			OnSettingChange(args);
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
			var args0 = new SettingChangeEventArgs(this, key, old_value, true);
			OnSettingChange(args0); if (args0.Cancel) return;
			PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(key));

			// Change the property
			m_data[key] = value;

			// Notify changed
			var arg1 = new SettingChangeEventArgs(this, key, value, false);
			OnSettingChange(arg1);
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(key));
		}

		/// <summary>Allow XML settings to up converted to the latest version before being loaded</summary>
		protected virtual void Upgrade(XElement node)
		{
			// User code will need to add versions to deal with this
		}
	}

	/// <summary>Extension methods for settings</summary>
	public static class Settings_
	{
		/// <summary>Return the single child setting based on the given key names</summary>
		public static XElement Child(XElement element, params string[] key_names)
		{
			var elem = element;
			foreach (var key in key_names)
				elem = elem?.Elements("setting").SingleOrDefault(x => x.Attribute("key").Value == key);

			return elem;
		}

		/// <summary>Return all child settings based on the given key names</summary>
		public static IEnumerable<XElement> Children(XElement element, params string[] key_names)
		{
			IEnumerable<XElement> ChildrenInternal(XElement e, int i)
			{
				if (i == key_names.Length)
					return Enumerable.Empty<XElement>();

				var children = e.Elements("setting").Where(x => x.Attribute("key").Value == key_names[i]);
				return i + 1 != key_names.Length
					? children.SelectMany(x => ChildrenInternal(x, i + 1))
					: children;
			}
			return ChildrenInternal(element, 0);
		}
	}

	/// <summary></summary>
	public enum ESettingsEvent
	{
		LoadFailed,
		NoVersion,
		FileNotFound,
		LoadingSettings,
		SavingSettings,
		SaveFailed,
	}

	/// <summary></summary>
	[Flags]
	public enum ESettingsLoadFlags
	{
		None = 0,
		ReadOnly = 1 << 0,
		IgnoreUnknownTypes = 1 << 1,
		ThrowOnError = 1 << 2,
	}

	#region Event Args
	public class SettingChangeEventArgs : EventArgs
	{
		private readonly ISettingsSet m_ss;
		public SettingChangeEventArgs(ISettingsSet ss, string key, object value, bool before)
		{
			m_ss = ss;
			Key = key;
			Value = value;
			Before = before;
			Cancel = false;
		}

		/// <summary>The full address of the key that was changed</summary>
		public string FullKey
		{
			get
			{
				var full = Key;
				var ss = m_ss;
				for (; ss.Parent != null; ss = ss.Parent)
				{
					var pkey = ss.Parent.Data.FirstOrDefault(x => Equals(x.Value, ss)).Key;
					full = $"{pkey}.{full}";
				}
				return full;
			}
		}

		/// <summary>The setting key</summary>
		public string Key { get; private set; }

		/// <summary>The current value of the setting</summary>
		public object Value { get; private set; }

		/// <summary>True if this event is just before the setting changes, false if just after</summary>
		public bool Before { get; private set; }
		public bool After => !Before;

		/// <summary>Set to 'true' to cancel a setting change (when 'Before' is true only)</summary>
		public bool Cancel { get; set; }
	}
	public class SettingsLoadedEventArgs :EventArgs
	{
		public SettingsLoadedEventArgs(string filepath)
		{
			Filepath = filepath;
		}

		/// <summary>The location of where the settings were loaded from</summary>
		public string Filepath { get; private set; }
	}
	public class SettingsSavingEventArgs :CancelEventArgs
	{
		public SettingsSavingEventArgs()
			:this(false)
		{ }
		public SettingsSavingEventArgs(bool cancel)
			:base(cancel)
		{ }
	}
	#endregion
}

#if PR_UNITTESTS
namespace Rylogic.UnitTests
{
	using Extn;

	[TestFixture]
	public class TestSettings
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
			public int x, y, z;
		}
		private sealed class SubSettings : SettingsSet<SubSettings>
		{
			public SubSettings() { Field = 2; Buffer = new byte[] { 1, 2, 3 }; }
			public int Field { get { return get<int>(nameof(Field)); } set { set(nameof(Field), value); } }
			public byte[] Buffer { get { return get<byte[]>(nameof(Buffer)); } set { set(nameof(Buffer), value); } }
		}
		private sealed class Settings : SettingsBase<Settings>
		{
			public Settings()
			{
				Str = "default";
				Int = 4;
				DTO = DateTimeOffset.Parse("2013-01-02 12:34:56");
				Floats = new[] { 1f, 2f, 3f };
				Sub = new SubSettings();
				Sub2 = new XElement("external");
				Things = new object[] { 1, 2.3f, "hello" };
			}
			public Settings(string filepath) : base(filepath) { }
			public Settings(XElement node) : base(node) { }

			public string Str { get { return get<string>(nameof(Str)); } set { set(nameof(Str), value); } }
			public int Int { get { return get<int>(nameof(Int)); } set { set(nameof(Int), value); } }
			public DateTimeOffset DTO { get { return get<DateTimeOffset>(nameof(DTO)); } set { set(nameof(DTO), value); } }
			public float[] Floats { get { return get<float[]>(nameof(Floats)); } set { set(nameof(Floats), value); } }
			public SubSettings Sub { get { return get<SubSettings>(nameof(Sub)); } set { set(nameof(Sub), value); } }
			public XElement Sub2 { get { return get<XElement>(nameof(Sub2)); } set { set(nameof(Sub2), value); } }
			public object[] Things { get { return get<object[]>(nameof(Things)); } set { set(nameof(Things), value); } }
		}

		[TestFixtureSetUp]
		public void Setup()
		{
			//Xml.SupportWinFormsTypes();
		}
		[Test]
		public void TestSettings1()
		{
			var s = new Settings();
			Assert.Equal(Settings.Default.Str, s.Str);
			Assert.Equal(Settings.Default.Int, s.Int);
			Assert.Equal(Settings.Default.DTO, s.DTO);
			Assert.True(Settings.Default.Floats.SequenceEqual(s.Floats));
			Assert.Equal(Settings.Default.Sub.Field, s.Sub.Field);
			Assert.True(Settings.Default.Sub.Buffer.SequenceEqual(s.Sub.Buffer));
			Assert.True(Settings.Default.Sub2.Name == s.Sub2.Name);
			Assert.True((int)Settings.Default.Things[0] == 1);
			Assert.True((float)Settings.Default.Things[1] == 2.3f);
			Assert.True((string)Settings.Default.Things[2] == "hello");
			Assert.Throws(typeof(ArgumentNullException), s.Save); // no filepath set
		}
		[Test]
		public void TestSettings2()
		{
			var file = Path.GetTempFileName();
			var st = new SettingsThing();
			var s = new Settings
			{
				Str = "Changed",
				Int = 42,
				DTO = DateTimeOffset.UtcNow,
				Floats = new[] { 4f, 5f, 6f },
				Sub = new SubSettings
				{
					Field = 12,
					Buffer = new byte[] { 4, 5, 6 }
				},
				Sub2 = st.ToXml(new XElement("external")),
				Things = new object[] { "Hello", 6.28 },
			};
			var xml = s.ToXml();

			var S = new Settings(xml);

			Assert.Equal(s.Str, S.Str);
			Assert.Equal(s.Int, S.Int);
			Assert.Equal(s.DTO, S.DTO);
			Assert.True(s.Floats.SequenceEqual(S.Floats));
			Assert.Equal(s.Sub.Field, S.Sub.Field);
			Assert.True(s.Sub.Buffer.SequenceEqual(S.Sub.Buffer));
			Assert.True((string)s.Things[0] == "Hello");
			Assert.True((double)s.Things[1] == 6.28);

			var st2 = new SettingsThing(s.Sub2);
			Assert.Equal(st.x, st2.x);
			Assert.Equal(st.y, st2.y);
			Assert.Equal(st.z, st2.z);
		}
		[Test]
		public void TestEvents()
		{
			var i = 0;
			int changing = 0, changed = 0, saving = 0, loading = 0;

			var file = Path.GetTempFileName();
			var settings = new Settings();
			settings.SettingChange += (s, a) => { if (a.Before) changing = ++i; else changed = ++i; };
			settings.SettingsSaving += (s, a) => saving = ++i;
			settings.SettingsLoaded += (s, a) => loading = ++i;

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
		[Test]
		public void TestExtensions()
		{
			var settings = @"
				<settings>
					<setting key='One'>
						<setting key='Two'/>
						<setting key='Two'/>
						<setting key='Two'/>
					</setting>
					<setting key='One'>
						<setting key='Two'/>
						<setting key='Two'/>
					</setting>
					<setting key='One'>
						<setting key='Three'/>
					</setting>
				</settings>
				";

			var xml = XElement.Parse(settings);
			Assert.Equal(3, Settings_.Children(xml, "One").Count());
			Assert.Equal(5, Settings_.Children(xml, "One", "Two").Count());
			Assert.Equal(1, Settings_.Children(xml, "One", "Three").Count());
		}
	}
}
#endif