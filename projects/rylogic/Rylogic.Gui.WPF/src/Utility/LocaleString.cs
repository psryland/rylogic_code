using System;
using System.ComponentModel;
using Rylogic.Common;

namespace Rylogic.Gui.WPF
{
	public class LocaleString : INotifyPropertyChanged
	{
		// Notes:
		//  - This is a helper object for handling localisation of strings.
		//  - Localised string resources are assumed to be global so the 'Provider' in this object is a singleton.
		/*
			Situation:
				- UI components can be shared via assembles (e.g. Rylogic.Gui.WPF) or specific to applications. Shared UI components
				  need to support localisation without knowing what locales are available of even *if* they are available.
				- The 'LocaleString' type represents a string that can be localised. It is a resource that contains a key
				  used to look up strings in the current application. i.e., An application will have 'system:String' resources,
				  the shared UI component library will have 'LocaleString' resources containing 'Key' values that map to the 
				  string resources that are expected to exist in the application.
				- Xaml code, that references LocaleString objects, will update localised strings when the culture changes.
				  Strings within source code will need to be updated on the LocaleChanged event.
				  If convenient, LocaleString can be used instead of 'string' and it will update automatically.

			Usage:
			- Shared UI components can declare localisable strings as resources:
				e.g. <gui:LocaleString x:Key="ConnTo" Key="Connect To" FallBack="Connect To:"/>
					"x:Key" is the key for the LocaleString object in the ResourceDictionary,
					"Key" is the key for the string resource to lookup,
					"FallBack" is what to display if the resource string cannot be found (defaults to the value of 'Key'),
				Usage:
					For static use, i.e. culture set on start up, not changed without app restart, use
						<TextBlock Text="{StaticResource ConnTo}" ... />
					For dynamic use, i.e. culture can change at runtime,
						<TextBlock Text="{Binding Source={StaticResource ConnTo}, Path=Value}" ... />
				Note:
					'x:Key' is for the 'LocaleString' resource, 'Key' is for the system:String' resource. Don't use the same
					string for both or one will overwrite the other.
			
			- The application can then assign a 'Provider' implementation to this class that maps the 'Key'
				value to a string from the resources:
					public class MyApp : LocaleString.IProvider
					{
						public MyApp() { LocaleString.Provider = this; }
						string? LocaleString.IProvider.this[string key] => (string?)TryFindResource(key);
					}

			- The 'App.xaml' of the application should add resource dictionaries for each supported language:
					<ResourceDictionary x:Key="en-US" Source="/MyApp;component/res/Resources.en-US.xaml" />
					<ResourceDictionary x:Key="zh-CN" Source="/MyApp;component/res/Resources.zh-CN.xaml" />
			
			- The application can also declare LocaleString instances that are used from code:
				Duplicates don't matter because they all reference to embedded resources by "Key"
					public static class MyStrings
					{
						public static readonly LocaleString Hello = new LocaleString("Hello", "Yo");
						public static readonly LocaleString World = new LocaleString("World", "Homies");
					}
				Then, throughout the application use 'MyStrings.Hello' anywhere a translated string is needed.

			- Switching locale dynamically can be done by changing the 'CurrentCulture':
				class App
				{
					public string CurrentCulture
					{
						get => m_current_culture ?? "en-US";
						set
						{
							if (m_current_culture == value) return;
							if (m_current_culture != null)
							{
								if (TryFindResource(m_current_culture) is ResourceDictionary dic)
									Current.Resources.MergedDictionaries.Remove(dic);
							}
							m_current_culture = value;
							if (m_current_culture != null)
							{
								if (TryFindResource(m_current_culture) is ResourceDictionary dic)
									Current.Resources.MergedDictionaries.Add(dic);
							}
							LocaleString.NotifyLocaleChanged();
						}
					}
					private string? m_current_culture;
				}

				// Use this to detect changes if necessary:
				LocaleString.LocaleChanged += WeakRef.MakeWeak(HandleCultureChanged, x => LocaleString.LocaleChanged -= x);
		*/

		public LocaleString()
			:this(string.Empty)
		{}
		public LocaleString(string key, string? fallback = null)
		{
			Key = key;
			Fallback = fallback;
			LocaleChanged += WeakRef.MakeWeak(HandleLocaleChanged, h => LocaleChanged -= h);
			void HandleLocaleChanged(object? sender, EventArgs e) => NotifyPropertyChanged(string.Empty); // invalidate all properties of this object
		}

		/// <summary>The key used to lookup the translated string</summary>
		public string Key { get; set; } = string.Empty;

		/// <summary>The value to return if not found by the provider</summary>
		public string? Fallback { get; set;  }

		/// <summary>The localised string value</summary>
		public string Value => GetOrDefault(Key, Fallback);

		/// <summary>Conversion to translated string</summary>
		public static implicit operator string(LocaleString s) => s.Value;
		public override string ToString() => Value;

		/// <summary>Lookup the localised string by key</summary>
		public static string Get(string key, params object?[] args) => GetOrDefault(key, null, args);
		public static string GetOrDefault(string key, string? fallback, params object?[] args)
		{
			if (Provider is not IProvider provider) return fallback ?? key;
			if (provider[key] is not string str) return fallback ?? key;
			return args.Length != 0 ? string.Format(str, args) : str;
		}

		/// <summary>Localised string look up</summary>
		public static IProvider? Provider { get; set; }

		/// <summary>Raised when the local changes (typically raised by the 'Provider')</summary>
		public static event EventHandler? LocaleChanged;
		public static void NotifyLocaleChanged() => LocaleChanged?.Invoke(null, EventArgs.Empty);

		/// <inheritdoc/>
		public event PropertyChangedEventHandler? PropertyChanged;
		private void NotifyPropertyChanged(string prop_name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));

		/// <summary>Interface of a type that converts 'EStringId' to a Localised string</summary>
		public interface IProvider
		{
			string? this[string key] { get; }
		}
	}
}
