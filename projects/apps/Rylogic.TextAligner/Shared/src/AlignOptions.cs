using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Xml.Linq;
using Microsoft.VisualStudio.Shell;
using Rylogic.Common;
using Rylogic.Extn;
using Rylogic.Utility;

namespace Rylogic.TextAligner
{
	[ClassInterface(ClassInterfaceType.AutoDual)]
	[Guid("C0392BF9-56C7-4D4E-9669-5C4B2B38366C")]
	internal sealed class AlignOptions :UIElementDialogPage, INotifyPropertyChanged
	{
		public AlignOptions()
			:base()
		{
			try
			{
				Groups = new ObservableCollection<AlignGroup>();
				ResetSettings();

				// Record the file modified timestamp
				SettingsWatcher = new FileSystemWatcher(Path_.Directory(SettingsFilepath), Path_.FileName(SettingsFilepath)) { EnableRaisingEvents = true };
				SettingsWatcher.Changed += HandleChanged;
				async void HandleChanged(object? sender, FileSystemEventArgs e)
				{
					await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(CancellationToken.None);
					LoadSettingsFromStorage();
				};
			}
			catch (Exception ex)
			{
				Log.Write(ELogLevel.Error, ex, "AlignOptions constructor failed");
				throw;
			}
		}

		/// <summary>The location on disk of where the settings are saved</summary>
		public string SettingsFilepath => Util.ResolveAppDataPath("Rylogic", "VSExtension", "align_patterns.xml"); // Don't change this, it will cause users patterns to reset
		private FileSystemWatcher? SettingsWatcher;

		/// <summary>The groups of alignment patterns</summary>
		public ObservableCollection<AlignGroup> Groups { get; }

		/// <summary>The method to use for aligning</summary>
		public EAlignCharacters AlignStyle
		{
			get => m_align_style;
			set
			{
				if (AlignStyle == value) return;
				m_align_style = value;
				NotifyPropertyChanged(nameof(AlignStyle));
			}
		}
		private EAlignCharacters m_align_style = EAlignCharacters.Spaces;

		/// <summary>Pattern for lines that should be ignored when aligning</summary>
		public AlignPattern LineIgnorePattern
		{
			get => m_line_ignore_pattern;
			set
			{
				if (LineIgnorePattern == value) return;
				m_line_ignore_pattern = value;
				NotifyPropertyChanged(nameof(LineIgnorePattern));
			}
		}
		private AlignPattern m_line_ignore_pattern = new AlignPattern();

		/// <summary>Should be overridden to reset settings to their default values.</summary>
		public override void ResetSettings()
		{
			try
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

				Groups.Add(new AlignGroup("Comma delimiter", 0,
					new AlignPattern(EPattern.Substring, @",")));

				Groups.Add(new AlignGroup("Single colon delimiter", 0,
					new AlignPattern(EPattern.RegularExpression, @"(?<!:):(?!:)", 0, 1, "Matches a single ':' character (not '::')")));

				Groups.Add(new AlignGroup("Members", 1,
					new AlignPattern(EPattern.RegularExpression, @"(?<![~^])(?<=\s)m_[0-9a-zA-Z_]*", 0, 1, "Matches class members that begin with 'm_'"),
					new AlignPattern(EPattern.RegularExpression, @"(?<![~^])(?<=\s)_[0-9a-zA-Z_]*", 0, 1, "Matches class members that begin with '_'"),
					new AlignPattern(EPattern.RegularExpression, @"(?<=\s)[_a-zA-z][_a-zA-Z0-9]*_(?![_a-zA-Z0-9])", 0, 1, "Matches class members that end with '_'")));

				AlignStyle = EAlignCharacters.Spaces;
				LineIgnorePattern = new AlignPattern();
			}
			catch (Exception ex)
			{
				Log.Write(ELogLevel.Error, ex, "Error in ResetSettings");
				throw;
			}
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
				AlignStyle = root.Element(nameof(AlignStyle)).As<EAlignCharacters>(EAlignCharacters.Spaces);
				LineIgnorePattern = new AlignPattern(EPattern.RegularExpression, root.Element(nameof(LineIgnorePattern)).As<string>(string.Empty));
			}
			catch (Exception ex)
			{
				// Don't allow anything to throw from here, otherwise VS locks up... :-/
				Log.Write(ELogLevel.Error, ex, "Error in LoadSettingsFromStorage");
			}
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
				root.Add2(nameof(LineIgnorePattern), LineIgnorePattern.Expr, false);
				root.Save(SettingsFilepath);
			}
			catch (Exception ex)
			{
				// Don't allow anything to throw from here, otherwise VS locks up... :-/
				Log.Write(ELogLevel.Error, ex, "Error in SaveSettingsToStorage");
			}
		}

		/// <summary>Called just before settings are saved</summary>
		internal event EventHandler? Saving;

		/// <summary></summary>
		internal event EventHandler<CancelEventArgs>? Deactivating;

		/// <inheritdoc/>
		protected override void OnDeactivate(CancelEventArgs e)
		{
			try
			{
				Deactivating?.Invoke(this, e);
				if (e.Cancel) return;

				SaveSettingsToStorage();
				base.OnDeactivate(e);
			}
			catch (Exception ex)
			{
				Log.Write(ELogLevel.Error, ex, "AlignOptions.OnDeactivate failed");
				throw;
			}
		}

		/// <inheritdoc/>
		protected override UIElement CreateChild()
		{
			try
			{
				return new AlignOptionsUI(this);
			}
			catch (Exception ex)
			{
				Log.Write(ELogLevel.Error, ex, "AlignOptions.CreateChild failed");
				throw;
			}
		}

		/// <inheritdoc/>
		public event PropertyChangedEventHandler? PropertyChanged;
		private void NotifyPropertyChanged(string prop_name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
	}
}