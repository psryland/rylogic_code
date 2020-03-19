using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Xml.Linq;
using Microsoft.VisualStudio.Shell;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Utility;

namespace Rylogic.TextAligner
{
    [ClassInterface(ClassInterfaceType.AutoDual)]
	[Guid("C0392BF9-56C7-4D4E-9669-5C4B2B38366C")]
	internal sealed class AlignOptions :UIElementDialogPage
	{
		public AlignOptions()
			:base()
		{
			Groups = new ObservableCollection<AlignGroup>();
			ResetSettings();
		}

		/// <summary>The location on disk of where the settings are saved</summary>
		public string SettingsFilepath => Util.ResolveAppDataPath("Rylogic", "VSExtension", "align_patterns.xml"); // Don't change this, it will cause users patterns to reset

		/// <summary>The groups of alignment patterns</summary>
		public ObservableCollection<AlignGroup> Groups { get; }

		/// <summary>The method to use for aligning</summary>
		public EAlignStyle AlignStyle { get; set; }

		/// <summary>Should be overridden to reset settings to their default values.</summary>
		public override void ResetSettings()
		{
			Groups.Clear();

			Groups.Add(new AlignGroup("Assignments", 1,
				new AlignPattern(EPattern.RegularExpression, @"(?<![+\-*/%^~&|!=<>])=(?![=<>])", 0, 1, "Assignment not preceded by: +,-,*,/,%,^,~,&,|,!,=,<,> and not followed by: =,<,>"),
				new AlignPattern(EPattern.RegularExpression, @"(?<![&|])[+\-*/%^~&|]={1}", -1, 2, "Assignment preceded by: +,-,*,/,%,^,~,&,| but not preceded by &&,||"),
				new AlignPattern(EPattern.RegularExpression, @"&&=|\|\|=", -2, 3, "Assignments &&= and ||=")));

			Groups.Add(new AlignGroup("Lambda", 1,
				new AlignPattern(EPattern.Substring, @"=>")));

			Groups.Add(new AlignGroup("Comparisons", 1,
				new AlignPattern(EPattern.Substring, @"==", 0, 2),
				new AlignPattern(EPattern.Substring, @"!=", 0, 2),
				new AlignPattern(EPattern.Substring, @"<=", 0, 2),
				new AlignPattern(EPattern.Substring, @">=", 0, 2),
				new AlignPattern(EPattern.RegularExpression, @"(?<!=)>(?!=)", 0, 1, "> not preceded or followed by ="),
				new AlignPattern(EPattern.RegularExpression, @"(?<!=)<(?!=)", 0, 1, "< not preceded or followed by =")));

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

			AlignStyle = EAlignStyle.Spaces;
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
				var grps = root.Elements(nameof(Groups), nameof(AlignGroup)).Select(x => x.As<AlignGroup>());
				Groups.Assign(grps);

				// Load other settings
				AlignStyle = root.Element(nameof(AlignStyle)).As<EAlignStyle>(EAlignStyle.Spaces);
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
				root.Add2(nameof(AlignStyle), AlignStyle, false);
				root.Save(SettingsFilepath);
			}
			catch { } // Don't allow anything to throw from here, otherwise VS locks up... :-/
		}

		/// <summary>Called just before settings are saved</summary>
		internal event EventHandler? Saving;

		/// <summary></summary>
		internal event EventHandler<CancelEventArgs>? Deactivating;

		/// <summary>Save changes when the dialog page loses focus</summary>
		protected override void OnDeactivate(CancelEventArgs e)
		{
			Deactivating?.Invoke(this, e);
			if (e.Cancel) return;

			SaveSettingsToStorage();
			base.OnDeactivate(e);
		}

		/// <summary></summary>
		protected override UIElement CreateChild()
		{
			return new AlignOptionsUI(this);
		}
	}
}