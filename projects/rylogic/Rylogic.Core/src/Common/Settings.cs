using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
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
		get => get<string>(nameof(Name));
		set => set(nameof(Name), value);
	}

	public int Value
	{
		get => return get<int>(nameof(Value));
		set => set(nameof(Value), value);
	}
}
#endif

namespace Rylogic.Common
{
	/// <summary>Common interface for "SettingsSet'T" and "SettingsBase'T"</summary>
	public interface ISettingsSet
	{
		/// <summary>Parent settings for this settings object</summary>
		ISettingsSet? Parent { get; set; }

		/// <summary>Find the key that corresponds to 'value'</summary>
		IReadOnlyDictionary<string, object?> Data { get; }

		/// <summary>Save the current settings</summary>
		void Save();

		/// <summary>An event raised when a setting is about to change value</summary>
		event EventHandler<SettingChangeEventArgs>? SettingChange;

		/// <summary>Called before and after a setting changes</summary>
		void OnSettingChange(SettingChangeEventArgs args);

		/// <summary>
		/// A callback method called when a settings related event occurs.
		/// Note: in any tree-like structure of settings there is only one 'SettingsEvent'
		/// which is the one provided by the top-level settings set.</summary>
		Action<ESettingsEvent, Exception?, string>? SettingsEvent { get; set; }
	}

	/// <summary>A base class for settings structures</summary>
	public abstract class SettingsSet<T> :ISettingsSet, INotifyPropertyChanged, INotifyPropertyChanging
		where T:SettingsSet<T>, new()
	{
		// Notes:
		//  - SettingsSet is intended to be a recursive data structure.
		//  - Functionality should be mainly in SettingsSet, SettingsBase is
		//    just for supporting loading from files/streams/etc.
		//  - Top level SettingsSets are instances of SettingsBase.

		public const string VersionKey = "__SettingsVersion";

		protected SettingsSet()
		{
			m_data = new Dictionary<string, object?>();
			LoadedVersion = string.Empty;
		}
		protected SettingsSet(XElement node, ESettingsLoadFlags flags = ESettingsLoadFlags.None)
			: this()
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
					SettingsEvent?.Invoke(ESettingsEvent.LoadFailed, ex, "Failed to load settings from XML data");
					if (flags.HasFlag(ESettingsLoadFlags.ThrowOnError)) throw;
					ResetToDefaults(); // Fall back to default values
				}
			}
		}
		protected SettingsSet(SettingsSet<T> rhs, ESettingsLoadFlags flags = ESettingsLoadFlags.None)
			: this(rhs.ToXml(new XElement("root")), flags)
		{}

		/// <summary>The default values for the settings</summary>
		public static T Default { get; } = new T();

		/// <summary>The settings version, used to detect when 'Upgrade' is needed</summary>
		public virtual string Version => "v1.0";

		/// <summary>The version number that was loaded (and possibly upgraded from)</summary>
		public string LoadedVersion { get; private set; }

		/// <summary>True to block all writes to the settings</summary>
		[Browsable(false)]
		public bool ReadOnly { get; set; }

		/// <summary>Parent settings for this settings object</summary>
		[Browsable(false)]
		public ISettingsSet? Parent
		{
			get => m_parent;
			set
			{
				if (m_parent == value) return;
				m_parent = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Parent)));
			}
		}
		private ISettingsSet? m_parent;

		/// <summary>Find the key that corresponds to 'value'</summary>
		public IReadOnlyDictionary<string, object?> Data => m_data;
		protected readonly Dictionary<string, object?> m_data;

		/// <summary>Returns the top-most settings set</summary>
		public ISettingsSet Root
		{
			get
			{
				ISettingsSet root;
				for (root = this; root.Parent != null; root = root.Parent) {}
				return root;
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
				default!;
		}

		/// <summary>Write a settings value</summary>
		protected virtual void set<Value>(string key, Value value)
		{
			// If 'value' is a nested setting, set this object as the parent
			SetParentIfNesting(value);

			// Key not in the data yet? Must be initial value from startup
			if (!m_data.TryGetValue(key, out var old))
			{
				m_data[key] = value!;
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
			NotifyPropertyChanging(key);

			// Update the value
			m_data[key] = value!;

			// Notify changed
			var args1 = new SettingChangeEventArgs(this, key, value!, false);
			OnSettingChange(args1);
			NotifyPropertyChanged(key);
		}

		/// <summary>Return the settings as an XML node tree</summary>
		public XElement ToXml(XElement? node = null)
		{
			node ??= new XElement("settings");
			node.Add2(VersionKey, Version, false);
			return ToXmlCore(node);
		}
		protected virtual XElement ToXmlCore(XElement node)
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
		public void FromXml(XElement node)
		{
			// Don't merge this and make flags a default parameter. Reflection is used to find
			// this specific overload of 'FromXml'
			FromXmlCore(node, ESettingsLoadFlags.None);
		}
		public void FromXml(XElement node, ESettingsLoadFlags flags)
		{
			FromXmlCore(node, flags);
		}
		protected virtual void FromXmlCore(XElement node, ESettingsLoadFlags flags)
		{
			m_data.Clear();

			// Read and save the settings version. If no version available, assume the 'latest' version.
			var vers = node.Element(VersionKey);
			LoadedVersion = vers?.Value ?? Version;

			// Upgrade old settings
			if (LoadedVersion != Version)
			{
				Upgrade(node, LoadedVersion);

				// This is an old version, so optionally ignore types
				if (flags.HasFlag(ESettingsLoadFlags.IgnoreUnknownTypesInOldVersions))
					flags |= ESettingsLoadFlags.IgnoreUnknownTypes;
			}

			// Remove the version number to support old-style settings
			// where the element name was interpreted as the setting name.
			vers?.Remove();

			// Load the settings values into 'm_data'
			foreach (var setting in node.Elements())
			{
				var key = setting.Attribute("key")?.Value;
				try
				{
					if (key != null)
					{
						var val = setting.ToObject();
						SetParentIfNesting(val);
						m_data[key] = val!;
					}
					// Support old SettingXml style settings where the element name matches the property name
					else if (setting.Name.LocalName != "setting")
					{
						// Use the type to determine the type of the XML element
						var prop_name = setting.Name.LocalName;
						var pi = typeof(T).GetProperty(prop_name, BindingFlags.Instance | BindingFlags.Public);

						// Ignore XML values that are no longer properties of 'T'
						if (pi == null)
							continue;

						var val = setting.As(pi.PropertyType);
						SetParentIfNesting(val);
						m_data[prop_name] = val!;
					}
					else
					{
						// Support old style settings that used to have key/value child elements
						var key_elem = setting.Element("key");
						var val_elem = setting.Element("value");
						if (key_elem == null || val_elem == null)
							throw new Exception($"Invalid setting element found: {setting}");

						key = key_elem.As<string>();
						var val = val_elem.ToObject();
						SetParentIfNesting(val);
						m_data[key] = val!;
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

			// Allow invalid settings to be rejected
			if (Validate() is Exception ex)
				throw ex;

			// Set readonly after loading is complete
			ReadOnly = flags.HasFlag(ESettingsLoadFlags.ReadOnly);
		}

		/// <summary>Upgrade settings to the latest version from 'from_version'</summary>
		public void Upgrade(XElement old_settings, string from_version)
		{
			BackupCore(old_settings, from_version);
			UpgradeCore(old_settings, from_version);
		}
		protected virtual void BackupCore(XElement old_settings, string from_version)
		{
			// Perform a backup of 'old_settings' before they get upgraded
		}
		protected virtual void UpgradeCore(XElement old_settings, string from_version)
		{
			// Boiler-plate:
			// for (; from_version != Version;)
			// {
			// 	switch (from_version)
			// 	{
			// 		case "v1.1":
			// 		{
			// 			#region Change description
			// 			{
			// 				// Modify the XML document using hard-coded element names.
			// 				// Note: don't use constants for the element names because they
			// 				// may get changed in the future. This is one case where magic
			// 				// strings is actually the correct thing to do!
			//
			//				// Use the child helper:
			//				Settings_.Child(old_settings, "thing")?.SetAttributeValue("ty", "NewType");
			//				Settings_.Child(old_settings, "redundant")?.Remove();
			//				foreach (var elem in Settings_.Children(old_settings, "MyList", "ListItemField"))
			//					elem.Value = "42";
			//
			//				// Or the Xml Extensions:
			//				var thing = old_settings.Elements("setting","key").Where(x => x.Value == "Thing").First();
			// 			}
			// 			#endregion
			// 			from_version = "v1.2";
			// 			break;
			// 		}
			// 		default:
			// 		{
			// 			base.UpgradeCore(old_settings, from_version);
			// 			return;
			// 		}
			// 	}
			// }
			//
			// Notes:
			//  - Old settings may contain references to types that don't exist in newer applications.
			//    Typically the Upgrade step would remove these from the XML so that no TypeLoadException's
			//    occur. A more lazy option is to use the 'ESettingsLoadFlags.IgnoreUnknownTypes' flag, or
			//    possibly the 'ESettingsLoadFlags.IgnoreUnknownTypesInOldVersions' flag.

			throw new NotSupportedException($"Settings file version is {from_version}. Latest version is {Version}. Upgrading from this version is not supported");
		}

		/// <summary>Perform validation on the loaded settings</summary>
		public virtual Exception? Validate()
		{
			return null;
		}

		/// <summary>Save the current settings</summary>
		public virtual void Save()
		{
			if (Parent == null) throw new Exception("Settings set is not a child of SettingsBase. Save not possible");
			Parent.Save();
		}

		/// <summary>Reset this settings set to the default construction values</summary>
		public void Reset()
		{
			ResetCore();
		}
		protected virtual void ResetCore()
		{
			ResetToDefaults();
		}

		/// <summary>An event raised before and after a setting is changes value</summary>
		public event EventHandler<SettingChangeEventArgs>? SettingChange;

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

		/// <summary>Called whenever an error or warning condition occurs. By default, this function calls 'OnSettingsError'</summary>
		public Action<ESettingsEvent, Exception?, string>? SettingsEvent
		{
			get => Parent?.SettingsEvent ?? m_action;
			set
			{
				if (Parent != null)
					Parent.SettingsEvent = value;
				else
					m_action = value;
			}
		}
		private Action<ESettingsEvent, Exception?, string>? m_action;

		/// <summary>Property changing</summary>
		public event PropertyChangingEventHandler? PropertyChanging;
		private void NotifyPropertyChanging(string prop_name)
		{
			PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(prop_name));
		}

		/// <summary>Property changed</summary>
		public event PropertyChangedEventHandler? PropertyChanged;
		public void NotifyPropertyChanged(string prop_name)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
		}

		/// <summary>If 'nested' is not null, sets its Parent to this settings object</summary>
		private void SetParentIfNesting(object? nested)
		{
			if (nested == null)
				return;

			// If 'nested' is a settings set
			if (nested is ISettingsSet set)
				set.Parent = this;

			// Or if 'nested' is a collection of settings sets
			if (nested is IEnumerable<ISettingsSet> sets)
			{
				sets.ForEach(x => x.Parent = this);

				// If the collection is observable, watch for changes
				if (nested is INotifyCollectionChanged notif)
				{
					notif.CollectionChanged += WeakRef.MakeWeak(HandleSettingCollectionChanged, h => notif.CollectionChanged -= h);
					void HandleSettingCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
					{
						foreach (var set in e.OldItems<ISettingsSet>())
							set.Parent = null;
						foreach (var set in e.NewItems<ISettingsSet>())
							set.Parent = this;
					}
				}
			}

			// Or if 'nested' is an associative collection of settings sets
			if (nested is IDictionary<string, ISettingsSet> map)
			{
				map.ForEach(x => x.Value.Parent = this);
			}
		}

		/// <summary>Populate these settings from the default instance values</summary>
		protected void ResetToDefaults()
		{
			// Use 'new T()' so that reference types can be used, otherwise we'll change the defaults
			// Use 'set' so that ISettingsSets are parented correctly
			foreach (var d in new T().m_data)
				set(d.Key, d.Value);
		}
	}

	/// <summary>A base class for simple settings</summary>
	public abstract class SettingsBase<T> :SettingsSet<T>
		where T:SettingsBase<T>, new()
	{
		protected SettingsBase()
			: base()
		{
			BackupOldSettings = true;

			// Default to off so users can enable after startup completes
			AutoSaveOnChanges = false;
		}
		protected SettingsBase(XElement node, ESettingsLoadFlags flags = ESettingsLoadFlags.None)
			: base(node, flags)
		{ }
		protected SettingsBase(Stream stream, ESettingsLoadFlags flags = ESettingsLoadFlags.None)
			: this()
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
					SettingsEvent?.Invoke(ESettingsEvent.LoadFailed, ex, "Failed to load settings from stream");
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
					SettingsEvent?.Invoke(ESettingsEvent.LoadFailed, ex, $"Failed to load settings from {filepath}");
					if (flags.HasFlag(ESettingsLoadFlags.ThrowOnError)) throw;
					ResetToDefaults(); // Fall back to default values
				}
			}
		}
		protected SettingsBase(SettingsBase<T> rhs, ESettingsLoadFlags flags = ESettingsLoadFlags.None)
			: base(rhs, flags)
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

		/// <summary>Returns the filepath for the persisted settings file. Settings cannot be saved until this property has a valid filepath</summary>
		public string Filepath
		{
			get => m_filepath ?? string.Empty;
			set => m_filepath = value;
		}
		private string? m_filepath;

		/// <summary>Get/Set whether to automatically save whenever a setting is changed</summary>
		public bool AutoSaveOnChanges
		{
			get => m_auto_save;
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
				void HandleSettingChange(object? sender, SettingChangeEventArgs args)
				{
					if (args.Before) return;
					AutoSave();
				}
			}
		}
		private bool m_auto_save;

		/// <summary>Backup old settings before upgrades</summary>
		public bool BackupOldSettings { get; set; }

		/// <summary>An event raised whenever the settings are loaded from persistent storage</summary>
		public event EventHandler<SettingsLoadedEventArgs>? SettingsLoaded;

		/// <summary>An event raised whenever the settings are about to be saved to persistent storage</summary>
		public event EventHandler<SettingsSavingEventArgs>? SettingsSaving;

		/// <summary>Resets the persisted settings to their defaults</summary>
		protected override void ResetCore()
		{
			// If the settings were loaded from disk, overwrite them with the defaults
			if (Path_.IsValidFilepath(Filepath, require_rooted: true))
			{
				Default.Save(Filepath);
				Load(Filepath);
			}
			else
			{
				base.ResetCore();
			}
		}

		/// <summary>Populate this object from XML</summary>
		protected override void FromXmlCore(XElement node, ESettingsLoadFlags flags)
		{
			base.FromXmlCore(node, flags);

			// Notify of settings loaded
			SettingsLoaded?.Invoke(this, new SettingsLoadedEventArgs(Filepath));
		}

		/// <summary>Perform a backup of 'old_settings' before they get upgraded</summary>
		protected override void BackupCore(XElement old_settings, string from_version)
		{
			if (BackupOldSettings && Filepath.HasValue())
			{
				var backup_filepath = Path.ChangeExtension(Filepath, $"backup_({from_version}){Path_.Extn(Filepath)}");
				old_settings.Save(backup_filepath);
			}
		}

		/// <summary>
		/// Refreshes the settings from a file storage.
		/// Starts with a copy of the Default.Data, then overwrites settings with those described in 'filepath'
		/// After load, the settings is the union of Default.Data and those in the given file.</summary>
		public void Load(string filepath, ESettingsLoadFlags flags = ESettingsLoadFlags.None)
		{
			if (!Path_.FileExists(filepath))
			{
				SettingsEvent?.Invoke(ESettingsEvent.FileNotFound, null, $"Settings file {filepath} not found, using defaults");
				Filepath = filepath;
				Reset();
				return; // Reset will recursively call Load again
			}

			SettingsEvent?.Invoke(ESettingsEvent.LoadingSettings, null, $"Loading settings file {filepath}");

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
			SettingsEvent?.Invoke(ESettingsEvent.LoadingSettings, null, "Loading settings from stream");

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
			using (Scope.Create(null, () => m_auto_save_pending = false))
			{
				// Notify of a save about to happen
				var args = new SettingsSavingEventArgs(false);
				SettingsSaving?.Invoke(this, args);
				if (args.Cancel) return;

				SettingsEvent?.Invoke(ESettingsEvent.SavingSettings, null, $"Saving settings to file {filepath}");

				// Ensure the save directory exists
				filepath = Path.GetFullPath(filepath);
				var path = Path.GetDirectoryName(filepath);
				if (path != null && !Path_.DirExists(path))
					Directory.CreateDirectory(path);

				try
				{
					// Perform the save
					var settings = ToXml();
					settings.Save(filepath, SaveOptions.None);
				}
				catch (Exception ex)
				{
					SettingsEvent?.Invoke(ESettingsEvent.SaveFailed, ex, $"Failed to save settings to file {filepath}");
				}
			}
		}
		public void Save(Stream stream)
		{
			if (m_block_saving)
				return;

			using (Scope.Create(() => m_block_saving = true, () => m_block_saving = false))
			using (Scope.Create(null, () => m_auto_save_pending = false))
			{
				// Notify of a save about to happen
				var args = new SettingsSavingEventArgs(false);
				SettingsSaving?.Invoke(this, args);
				if (args.Cancel) return;

				SettingsEvent?.Invoke(ESettingsEvent.SavingSettings, null, "Saving settings to stream");

				try
				{
					// Perform the save
					var settings = ToXml();
					settings.Save(stream, SaveOptions.None);
				}
				catch (Exception ex)
				{
					SettingsEvent?.Invoke(ESettingsEvent.SaveFailed, ex, "Failed to save settings to the given stream");
				}
			}
		}
		private bool m_block_saving;

		/// <summary>Save using the last filepath</summary>
		public override void Save()
		{
			Save(Filepath);
		}

		/// <summary>Save if 'AutoSaveOnChanges' is true</summary>
		public void AutoSave()
		{
			if (!AutoSaveOnChanges || m_auto_save_pending || string.IsNullOrEmpty(Filepath))
				return;

			// If there is a sync context, then we can defer saving for a bit to catch batches of settings changes
			// If not, then just save immediately. I don't know of any other .net core thread dispatcher system to use.
			var ctx = SynchronizationContext.Current;
			if (ctx != null)
			{
				void DoSave(object? _) => Save();
				Task.Delay(500).ContinueWith(x => ctx.Post(DoSave, null));
				m_auto_save_pending = true;
			}
			else
			{
				Save();
			}
		}
		private bool m_auto_save_pending;

		/// <summary>Remove the settings file from persistent storage</summary>
		public void Delete()
		{
			if (Path_.FileExists(Filepath))
				File.Delete(Filepath);
		}
	}

	/// <summary>Extension methods for settings</summary>
	public static class Settings_
	{
		/// <summary>Return the single child setting based on the given key names</summary>
		public static XElement? Child(XElement element, params string[] key_names)
		{
			XElement? elem = element;
			foreach (var key in key_names)
			{
				elem = elem.Elements("setting").SingleOrDefault(x => x.Attribute("key")?.Value == key);
				if (elem == null) break;
			}
			return elem;
		}

		/// <summary>Return all child settings based on the given key names</summary>
		public static IEnumerable<XElement> Children(XElement element, params string[] key_names)
		{
			// Note:
			//  - List elements are handled automatically, calling 'Children(e, "list", "child_property")' will
			//    return the 'child_property' of each element in 'list'
			IEnumerable<XElement> ChildrenInternal(XElement e, int i)
			{
				if (i == key_names.Length)
					return Enumerable.Empty<XElement>();

				// If the children of 'e' have the special name '_' then 'e' is a list.
				// Search the children of each list element.
				var children = e.Elements("_").Any()
					? e.Elements("_", "setting").Where(x => x.Attribute("key")?.Value == key_names[i])
					: e.Elements("setting").Where(x => x.Attribute("key")?.Value == key_names[i]);

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
		None                            = 0,
		ReadOnly                        = 1 << 0,
		ThrowOnError                    = 1 << 1,
		IgnoreUnknownTypes              = 1 << 2,
		IgnoreUnknownTypesInOldVersions = 1 << 3,
	}

	#region Event Args
	public class SettingChangeEventArgs : EventArgs
	{
		public SettingChangeEventArgs(ISettingsSet ss, string key, object? value, bool before)
		{
			SettingSet = ss;
			Key = key;
			Value = value;
			Before = before;
			Cancel = false;
		}

		/// <summary>The settings set that contains 'key'</summary>
		public ISettingsSet SettingSet { get; }

		/// <summary>The full address of the key that was changed</summary>
		public string FullKey
		{
			get
			{
				var full = Key;
				var ss = SettingSet;
				for (; ss.Parent != null; ss = ss.Parent)
				{
					// Find the key for 'ss' in parent
					var pkey = ss.Parent.Data.FirstOrDefault(x => Equals(x.Value, ss)).Key;
					full = $"{pkey}.{full}";
				}
				return full;
			}
		}

		/// <summary>The setting key</summary>
		public string Key { get; private set; }

		/// <summary>The current value of the setting</summary>
		public object? Value { get; private set; }

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
	using System.Collections.ObjectModel;
	using Extn;
	using Rylogic.Container;

	[TestFixture]
	public class TestSettings
	{
		private sealed class Settings :SettingsBase<Settings>
		{
			public Settings()
			{
				Str = "default";
				Int = 4;
				DTO = DateTimeOffset.Parse("2013-01-02 12:34:56");
				Floats = new[] { 1f, 2f, 3f };
				Things = new object[] { 1, 2.3f, "hello" };
				Sub = new SubSettings();
				Sub2 = new XElement("external");
				Sub3 = new ObservableCollection<SubSettings>();
			}
			public Settings(string filepath)
				:base(filepath, ESettingsLoadFlags.ThrowOnError)
			{
			}
			public Settings(XElement node)
				:base(node, ESettingsLoadFlags.ThrowOnError)
			{
			}

			public string Str
			{
				get { return get<string>(nameof(Str)); }
				set { set(nameof(Str), value); }
			}
			public int Int
			{
				get { return get<int>(nameof(Int)); }
				set { set(nameof(Int), value); }
			}
			public DateTimeOffset DTO
			{
				get { return get<DateTimeOffset>(nameof(DTO)); }
				set { set(nameof(DTO), value); }
			}
			public float[] Floats
			{
				get { return get<float[]>(nameof(Floats)); }
				set { set(nameof(Floats), value); }
			}
			public object[] Things
			{
				get { return get<object[]>(nameof(Things)); }
				set { set(nameof(Things), value); }
			}
			public SubSettings Sub
			{
				get { return get<SubSettings>(nameof(Sub)); }
				set { set(nameof(Sub), value); }
			}
			public XElement Sub2
			{
				get { return get<XElement>(nameof(Sub2)); }
				set { set(nameof(Sub2), value); }
			}
			public ObservableCollection<SubSettings> Sub3
			{
				get => get<ObservableCollection<SubSettings>>(nameof(Sub3));
				set => set(nameof(Sub3), value);
			}
		}
		private sealed class SubSettings :SettingsSet<SubSettings>
		{
			public SubSettings()
				:this(2, new byte[] { 1, 2, 3 })
			{}
			public SubSettings(int field, byte[] buf)
			{
				Field = field;
				Buffer = buf;
			}
			public int Field
			{
				get => get<int>(nameof(Field));
				set => set(nameof(Field), value);
			}
			public byte[] Buffer
			{
				get => get<byte[]>(nameof(Buffer));
				set => set(nameof(Buffer), value);
			}
		}
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
				Things = new object[] { "Hello", 6.28 },
				Sub = new SubSettings(12, new byte[]{ 4, 5, 6 }),
				Sub2 = st.ToXml(new XElement("external")),
				Sub3 = new ObservableCollection<SubSettings>
				{
					new SubSettings(1, new byte[]{ 1, 1, 1 }),
					new SubSettings(2, new byte[]{ 2, 2, 2 }),
					new SubSettings(3, new byte[]{ 3, 3, 3 }),
				},
			};
			var xml = s.ToXml();
			var S = new Settings(xml);

			Assert.Equal(s.Str, S.Str);
			Assert.Equal(s.Int, S.Int);
			Assert.Equal(s.DTO, S.DTO);
			Assert.True(s.Floats.SequenceEqual(S.Floats));
			Assert.True((string)s.Things[0] == "Hello");
			Assert.True((double)s.Things[1] == 6.28);
			Assert.Equal(s.Sub.Field, S.Sub.Field);
			Assert.True(s.Sub.Buffer.SequenceEqual(S.Sub.Buffer));
			Assert.True(s.Sub3[0].Field == 1);
			Assert.True(s.Sub3[1].Field == 2);
			Assert.True(s.Sub3[2].Field == 3);

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
			settings.SettingsSaving += (s, a) => saving = ++i;
			settings.SettingsLoaded += (s, a) => loading = ++i;
			settings.SettingChange += (s, a) =>
			{
				if (a.Before) changing = ++i;
				else changed = ++i;
			};

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

			var s0 = settings.Sub3.Add2(new SubSettings());
			s0.Field = 4;
			Assert.Equal(5, changing);
			Assert.Equal(6, changed);
			Assert.Equal(0, saving);
			Assert.Equal(0, loading);

			settings.Save(file);
			Assert.Equal(5, changing);
			Assert.Equal(6, changed);
			Assert.Equal(7, saving);
			Assert.Equal(0, loading);

			settings.Load(file);
			Assert.Equal(5, changing);
			Assert.Equal(6, changed);
			Assert.Equal(7, saving);
			Assert.Equal(8, loading);
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
