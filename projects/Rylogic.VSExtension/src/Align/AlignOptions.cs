using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Xml.Linq;
using Microsoft.VisualStudio.Shell;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Utility;

namespace Rylogic.VSExtension
{
	[ClassInterface(ClassInterfaceType.AutoDual)]
	[Guid("C0392BF9-56C7-4D4E-9669-5C4B2B38366C")]
	internal sealed class AlignOptions :DialogPage
	{
		public AlignOptions()
		{
			ResetSettings();
		}

		/// <summary>The location on disk of where the settings are saved</summary>
		public string SettingsFilepath
		{
			get { return Util.ResolveAppDataPath("Rylogic", "VSExtension", "align_patterns.xml"); }
		}

		/// <summary>The groups of alignment patterns</summary>
		public BindingList<AlignGroup> Groups { get; private set; }

		/// <summary>Should be overridden to reset settings to their default values.</summary>
		public override void ResetSettings()
		{
			Groups = Groups ?? new BindingList<AlignGroup>();
			Groups.Clear();
			Groups.AllowNew               = true;
			Groups.AllowRemove            = true;
			Groups.AllowEdit              = true;
			Groups.RaiseListChangedEvents = true;

			Groups.Add(new AlignGroup("Assignments", 1,
				new AlignPattern(EPattern.RegularExpression, @"(?<![+\-*/%^~&|!=<>])=(?![=<>])",  0, 1, "Assignment not preceded by: +,-,*,/,%,^,~,&,|,!,=,<,> and not followed by: =,<,>"),
				new AlignPattern(EPattern.RegularExpression, @"(?<![&|])[+\-*/%^~&|]={1}"      , -1, 2, "Assignment preceded by: +,-,*,/,%,^,~,&,| but not preceded by &&,||"),
				new AlignPattern(EPattern.RegularExpression, @"&&=|\|\|="                      , -2, 3, "Assignments &&= and ||=")));

			Groups.Add(new AlignGroup("Lambda", 1,
				new AlignPattern(EPattern.Substring, @"=>")));

			Groups.Add(new AlignGroup("Comparisons", 1,
				new AlignPattern(EPattern.Substring, @"==", 0, 2),
				new AlignPattern(EPattern.Substring, @"!=", 0, 2),
				new AlignPattern(EPattern.Substring, @"<=", 0, 2),
				new AlignPattern(EPattern.Substring, @">=", 0, 2),
				new AlignPattern(EPattern.RegularExpression, @"(?<!=)>(?!=)" , 0, 1, "> not preceded or followed by ="),
				new AlignPattern(EPattern.RegularExpression, @"(?<!=)<(?!=)" , 0, 1, "< not preceded or followed by =")));

			Groups.Add(new AlignGroup("Boolean operators", 1,
				new AlignPattern(EPattern.Substring, @"&&"),
				new AlignPattern(EPattern.Substring, @"||")));

			Groups.Add(new AlignGroup("Line comments", 1,
				new AlignPattern(EPattern.RegularExpression, @"/{2,}", 0, 0, "Two or more '/' characters")));

			Groups.Add(new AlignGroup("Open brackets", 0,
				new AlignPattern(EPattern.Substring, @"(")));

			Groups.Add(new AlignGroup("Close brackets", 0,
				new AlignPattern(EPattern.Substring, @")")));

			Groups.Add(new AlignGroup("Scope start", 0,
				new AlignPattern(EPattern.Substring, @"{")));

			Groups.Add(new AlignGroup("Scope end", 1,
				new AlignPattern(EPattern.Substring, @"}")));

			Groups.Add(new AlignGroup("Increment / Decrement", 1,
				new AlignPattern(EPattern.Substring, @"++"),
				new AlignPattern(EPattern.Substring, @"--")));

			Groups.Add(new AlignGroup("Plus / Minus", 1,
				new AlignPattern(EPattern.RegularExpression, @"(?<!\+)\+(?!\+)", 0, 1, "Matches '+' but not '++'"),
				new AlignPattern(EPattern.RegularExpression, @"(?<!\-)\-(?!\-)", 0, 1, "Matches '-' but not '--'")));

			Groups.Add(new AlignGroup("Comma delimiter", 1,
				new AlignPattern(EPattern.Substring, @",")));

			Groups.Add(new AlignGroup("Members", 0,
				new AlignPattern(EPattern.RegularExpression, @"(?<![~^])(?<=\s)m_[0-9a-zA-Z_]*", 0, 1, "Matches class members that being with 'm_'"),
				new AlignPattern(EPattern.RegularExpression, @"(?<![~^])(?<=\s)_[0-9a-zA-Z_]*", 0, 1, "Matches class members that being with '_'")));
		}

		/// <summary>Load settings from AppData</summary>
		public override void LoadSettingsFromStorage()
		{
			// Note: the 'LoadSettingsFromXml' and 'SaveSettingsToXml' methods are
			// only used when the user Exports or Imports their settings.
			// To/From storage are used for normal saving.
			try
			{
				var filepath = SettingsFilepath;
				if (!Path_.FileExists(filepath))
					return;

				// Load the settings XML file
				var root = XDocument.Load(filepath).Root;
				Groups.Assign(root.Elements(nameof(Groups), nameof(AlignGroup)).Select(x => x.As<AlignGroup>()));
			}
			catch { } // Don't allow anything to throw from here, otherwise VS locks up... :-/
		}

		/// <summary>Save settings to AppData</summary>
		public override void SaveSettingsToStorage()
		{
			try
			{
				// Notify of saving
				Saving?.Invoke(this, EventArgs.Empty);

				// Ensure the directory in AppData exists
				Directory.CreateDirectory(Path_.Directory(SettingsFilepath));

				// Write the settings to XML
				var root = new XElement("root");
				root.Add2(nameof(Groups), nameof(AlignGroup), Groups, false);
				root.Save(SettingsFilepath);
			}
			catch { } // Don't allow anything to throw from here, otherwise VS locks up... :-/
		}

		/// <summary>Called just before settings are saved</summary>
		internal event EventHandler Saving;

		/// <summary></summary>
		internal event EventHandler<CancelEventArgs> Deactivating;

		/// <summary>Save changes when the dialog page loses focus</summary>
		protected override void OnDeactivate(CancelEventArgs e)
		{
			Deactivating.Raise(this, e);
			if (e.Cancel) return;

			SaveSettingsToStorage();
			base.OnDeactivate(e);
		}

		/// <summary>Provides the user control for the options page</summary>
		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		protected override IWin32Window Window
		{
			get { return new AlignOptionsControl(this); }
		}
	}
}
#if false
		/// <summary>Restore the patterns from the registry</summary>
		public override void LoadSettingsFromStorage()
		{
			try
			{
				var package = (Package)GetService(typeof(Package));
				if (package == null)
					return;

				// Block events on the group collection while loading
				using (Scope.Create(() => Groups.RaiseListChangedEvents = false, () => Groups.RaiseListChangedEvents = true))
				using (var registry_root = package.UserRegistryRoot)
				{
					// Open the sub key for the extension
					var root_path   = SettingsRegistryPath;
					var root_regkey = registry_root.OpenSubKey(root_path, false);
					if (root_regkey == null) return;

					using (root_regkey)
					{
						Groups.Clear();
						foreach (var grp_key in root_regkey.GetSubKeyNames())
						{
							// Open each group key
							if (!grp_key.StartsWith("AlignGroup")) continue;
							var path   = root_path + "\\" + grp_key;
							var regkey = registry_root.OpenSubKey(path, false);
							if (regkey == null) continue;

							using (regkey)
							{
								try
								{
									// Read the group
									var name = (string)regkey.GetValue("name", "AlignGroup");
									var ls   = (int)regkey.GetValue("leading_space", 0);
									var grp  = new AlignGroup(name, ls);

									// Read each pattern within the group
									foreach (var patn_key in regkey.GetValueNames())
									{
										// Pattern use indices at the value name, anything else is a group property
										if (!patn_key.StartsWith("AlignPattern")) continue;
										var xml = XElement.Parse((string)regkey.GetValue(patn_key));
										var patn = new AlignPattern(xml);
										grp.Patterns.Add(patn);
									}
									Groups.Add(grp);
								}
								catch (Exception) {} // Ignore invalid groups/patterns
							}
						}
					}
				}

				if (Groups.Count == 0)
					ResetSettings();
			}
			catch {} // Throw allow anything to throw from here, otherwise VS locks up... :-/
		}
#endif
#if false
		/// <summary>Save the patterns to the registry</summary>
		public override void SaveSettingsToStorage()
		{
			try
			{
				var package = (Package)GetService(typeof(Package));
				if (package == null)
					return;

				Saving.Raise(this, EventArgs.Empty);

				// Get the root reg key
				using (var registry_root = package.UserRegistryRoot)
				{
					var root_path = SettingsRegistryPath;

					// Delete all groups and recreate the sub key for the extension
					registry_root.DeleteSubKeyTree(root_path, false);
					var root_regkey = registry_root.CreateSubKey(root_path);
					if (root_regkey == null) return;

					using (root_regkey)
					{
						var grp_index = -1;
						foreach (var grp in Groups)
						{
							// Create a (uniquely named) sub key for the group
							var path = root_path + "\\AlignGroup " + (++grp_index).ToString("000");
							var regkey = registry_root.CreateSubKey(path);
							if (regkey == null) continue;

							using (regkey)
							{
								regkey.SetValue("name"          , grp.Name         , RegistryValueKind.String);
								regkey.SetValue("leading_space" , grp.LeadingSpace , RegistryValueKind.DWord);
								try
								{
									// Add each pattern a values containing xml
									var patn_index = -1; foreach (var patn in grp.Patterns)
									{
										var elem = patn.ToXml("align_pattern", false);
										var key = "AlignPattern " + (++patn_index).ToString("000");
										regkey.SetValue(key, elem.ToString(), RegistryValueKind.String);
									}
								}
								catch (Exception) {} // Ignore patterns that fail to write
							}
						}
					}
				}
			}
			catch {} // Don't allow anything to throw from here, otherwise VS locks up... :-/
		}
#endif