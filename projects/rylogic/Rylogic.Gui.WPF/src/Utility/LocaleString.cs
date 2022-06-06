using System;
using System.ComponentModel;
using System.Windows.Markup;
using Rylogic.Common;

namespace Rylogic.Gui.WPF
{
	public class LocaleString : MarkupExtension, INotifyPropertyChanged
	{
		// Notes:
		//  - This is a helper object for handling localisation of strings.
		//  - Localised string resources are assumed to be global so the 'Provider' in this object is a singleton.
		//
		/* Usage:
			- Shared UI components can declare localisable strings as resources:
					"x:Key" is the local ResourceDictionary key,
					"Key" is the resource key in the resource file containing the strings
					"FallBack" is what to display if the resource string cannot be found (defaults to 'Key')
					<gui:LocaleString x:Key="ConnTo" Key="Connect To" FallBack="Connect To:"/>
				and use them:
					<TextBlock Text="{StaticResource ConnTo}"... 
				Be careful if 'x:Key' and 'Key' are the same value. One will overwrite the other in the resource dictionary.
			
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
			void HandleLocaleChanged(object? sender, EventArgs e) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
		}
		public event PropertyChangedEventHandler? PropertyChanged;

		/// <summary>The key used to lookup the translated string</summary>
		public string Key { get; set; } = string.Empty;

		/// <summary>The value to return if not found by the provider</summary>
		public string? Fallback { get; set;  }

		/// <summary>The localised string value</summary>
		public string Value => Get(Key, Fallback);

		/// <summary>Conversion to translated string</summary>
		public static implicit operator string(LocaleString s) => s.Value;
		public override string ToString() => Value;

		/// <summary>Lookup the localised string by key</summary>
		public static string Get(string key, string? fallback = null) => Provider is IProvider provider && provider[key] is string str ? str : (fallback ?? key);

		/// <summary>Localised string look up</summary>
		public static IProvider? Provider { get; set; }

		/// <summary>Raised when the local changes (typically raised by the 'Provider')</summary>
		public static event EventHandler? LocaleChanged;
		public static void NotifyLocaleChanged() => LocaleChanged?.Invoke(null, EventArgs.Empty);

		/// <summary>Interface of a type that converts 'EStringId' to a Localised string</summary>
		public interface IProvider
		{
			string? this[string key] { get; }
		}

		/// <inheritdoc/>
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			// This has to return a string because it is used in XAML on string properties (e.g. Title)
			return ToString();
		}
	}
}
