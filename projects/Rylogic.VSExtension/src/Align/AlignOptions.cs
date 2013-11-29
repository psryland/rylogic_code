using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Xml.Linq;
using Microsoft.VisualStudio.Shell;
using Microsoft.Win32;
using pr.common;
using pr.extn;
using pr.util;

namespace Rylogic.VSExtension
{
	[ClassInterface(ClassInterfaceType.AutoDual)]
	[Guid("C0392BF9-56C7-4D4E-9669-5C4B2B38366C")]
	internal sealed class AlignOptions :DialogPage
	{
		/// <summary>The groups of alignment patterns</summary>
		public BindingList<AlignGroup> Groups { get; private set; }

		public AlignOptions()
		{
			ResetSettings();
		}

		/// <summary>Should be overridden to reset settings to their default values.</summary>
		public override void ResetSettings()
		{
			Groups = Groups ?? new BindingList<AlignGroup>();
			Groups.Clear();
			Groups.AllowNew               = true;
			Groups.AllowRemove            = true;
			Groups.AllowEdit              = true;
			Groups.RaiseListChangedEvents = true;

			Groups.Add(new AlignGroup("Assignments", true,
				new AlignPattern(EPattern.RegularExpression, @"(?<![+\-*/%^~&|!=<>])=(?![=<>])",  0, 1, "Assignment not preceded by: +,-,*,/,%,^,~,&,|,!,=,<,> and not followed by: =,<,>"),
				new AlignPattern(EPattern.RegularExpression, @"(?<![&|])[+\-*/%^~&|]={1}"      , -1, 2, "Assignment preceded by: +,-,*,/,%,^,~,&,| but not preceded by &&,||"),
				new AlignPattern(EPattern.RegularExpression, @"&&=|\|\|="                      , -2, 3, "Assignments &&= and ||=")));

			Groups.Add(new AlignGroup("Comparisons", true,
				new AlignPattern(EPattern.Substring, @"==", 0, 2),
				new AlignPattern(EPattern.Substring, @"!=", 0, 2),
				new AlignPattern(EPattern.Substring, @"<=", 0, 2),
				new AlignPattern(EPattern.Substring, @">=", 0, 2),
				new AlignPattern(EPattern.Substring, @">" , 0, 1),
				new AlignPattern(EPattern.Substring, @"<" , 0, 1)));

			Groups.Add(new AlignGroup("Comma delimiter", true,
				new AlignPattern(EPattern.Substring, @",")));

			Groups.Add(new AlignGroup("Scope start", false,
				new AlignPattern(EPattern.Substring, @"{")));

			Groups.Add(new AlignGroup("Scope end", true,
				new AlignPattern(EPattern.Substring, @"}")));

			Groups.Add(new AlignGroup("Line comments", false,
				new AlignPattern(EPattern.RegularExpression, @"/{2,}", 0, 0, "Two or more '/' characters")));

			Groups.Add(new AlignGroup("Member variables", false,
				new AlignPattern(EPattern.RegularExpression, @"\bm_\w*", 0, 0, "Variable names prefixed with 'm_'"),
				new AlignPattern(EPattern.RegularExpression, @"\b_\w*" , 0, 0, "Variable names prefixed with '_'")));

			Groups.Add(new AlignGroup("Open brackets", false,
				new AlignPattern(EPattern.Substring, @"(")));

			Groups.Add(new AlignGroup("Close brackets", false,
				new AlignPattern(EPattern.Substring, @")")));
		}

		/// <summary>Save the patterns to the registry</summary>
		public override void SaveSettingsToStorage()
		{
			var package = (Package)GetService(typeof(Package));
			if (package == null)
				return;

			using (var registry_root = package.UserRegistryRoot)
			{
				var root_path = SettingsRegistryPath;
				registry_root.DeleteSubKeyTree(root_path);
				using (var root_regkey = registry_root.CreateSubKey(root_path))
				{
					if (root_regkey == null) return;
					foreach (var grp in Groups)
					{
						var path = root_path + "\\" + grp.Name;
						using (var regkey = registry_root.CreateSubKey(path))
						{
							if (regkey == null) continue;
							regkey.SetValue("leading_space", grp.LeadingSpace ? 1 : 0, RegistryValueKind.DWord);
							try
							{
								int i = -1;
								foreach (var patn in grp.Patterns)
								{
									var elem = patn.ToXml("align_pattern", false);
									regkey.SetValue((++i).ToString(CultureInfo.InvariantCulture), elem.ToString(), RegistryValueKind.String);
								}
							}
							catch (Exception)
							{
								// Ignore patterns that fail to write
							}
							regkey.Flush();
						}
					}
					root_regkey.Flush();
				}
			}
		}

		/// <summary>Restore the patterns from the registry</summary>
		public override void LoadSettingsFromStorage()
		{
			var package = (Package)GetService(typeof(Package));
			if (package == null)
				return;

			using (Scope.Create(() => Groups.RaiseListChangedEvents = false, () => Groups.RaiseListChangedEvents = true))
			using (var registry_root = package.UserRegistryRoot)
			{
				var root_path   = SettingsRegistryPath;
				var root_regkey = registry_root.OpenSubKey(root_path, false);
				if (root_regkey == null) return;

				Groups.Clear();
				foreach (var key in root_regkey.GetSubKeyNames())
				{
					var path   = root_path + "\\" + key;
					var regkey = registry_root.OpenSubKey(path, false);
					if (regkey == null) continue;

					try
					{
						var ls = (int)regkey.GetValue("leading_space", 0) == 1;
						var grp = new AlignGroup(key, ls);
						foreach (var value_name in regkey.GetValueNames())
						{
							var xml = XElement.Parse((string)regkey.GetValue(value_name));
							var patn = new AlignPattern(xml);
							grp.Patterns.Add(patn);
						}
						Groups.Add(grp);
					}
					catch (Exception)
					{
						// Ignore invalid groups/patterns
					}
				}
			}

			if (Groups.Count == 0)
				ResetSettings();
		}

		protected override void OnDeactivate(CancelEventArgs e)
		{
			base.OnDeactivate(e);
			SaveSettingsToStorage();
		}

		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		protected override IWin32Window Window
		{
			get { return new AlignOptionsControl(this); }
		}
	}
}
