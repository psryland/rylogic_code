using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Toolkit.Wpf.UI.Controls;
using Rylogic.Common;
using Rylogic.Utility;

namespace Rylogic.Gui.WPF
{
	/// <summary>A pattern editor control</summary>
	public interface IPatternUI
	{
		/// <summary>The original pattern before any edits were made</summary>
		IPattern Original { get; }

		/// <summary>The pattern currently being edited</summary>
		IPattern Pattern { get; }

		/// <summary>True if the pattern currently contained is a new instance, vs editing an existing pattern</summary>
		bool IsNew { get; }

		/// <summary>Set a new pattern for the UI</summary>
		void NewPattern(IPattern pat);

		/// <summary>Select a pattern into the UI for editing</summary>
		void EditPattern(IPattern pat);

		/// <summary>True if the contained pattern is different to the original</summary>
		bool HasUnsavedChanges { get; }

		/// <summary>True if the contained pattern is valid and therefore can be saved</summary>
		bool CommitEnabled { get; }

		/// <summary>Raised when the 'Commit' button is hit and the pattern field contains a valid pattern</summary>
		event EventHandler? Commit;

		/// <summary>Access to the test text field</summary>
		string TestText { get; set; }

		/// <summary>Set focus to the primary input field</summary>
		void FocusInput();
	}

	/// <summary>A control for editing patterns</summary>
	public partial class PatternEditor :UserControl, INotifyPropertyChanged
	{
		public PatternEditor()
		{
			InitializeComponent();
			CaptureGroups = new ObservableCollection<CaptureGroup>();
			Original = null;
			Pattern = new Pattern();

			CommitChanges = Command.Create(this, CommitChangesInternal);
			ShowHelp = Command.Create(this, ShowHelpInternal);
			DataContext = this;
			ApplyPattern();

			Loaded += delegate
			{
				_ = MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
			};
		}

		/// <summary>Raised when the 'Commit' button is hit and the pattern field contains a valid pattern</summary>
		public event EventHandler? Commit;
		public void NotifyCommit()
		{
			Commit?.Invoke(this, EventArgs.Empty);
		}

		/// <summary>Set a new pattern for the UI</summary>
		public void NewPattern(IPattern pattern)
		{
			Original = null;
			Pattern = pattern;
		}

		/// <summary>Select a pattern into the UI for editing</summary>
		public void EditPattern(IPattern pattern)
		{
			Original = pattern;
			Pattern = (IPattern)Original.Clone();
		}

		/// <summary>The pattern being edited</summary>
		public IPattern Pattern
		{
			get => m_pattern;
			private set
			{
				if (m_pattern == value) return;
				m_pattern = value ?? new Pattern();
				m_pattern.PropertyChanged += WeakRef.MakeWeak(HandlePatternPropertyChanged, h => m_pattern.PropertyChanged -= h);
				NotifyPropertyChanged(string.Empty);
				
				// Handler
				void HandlePatternPropertyChanged(object sender, PropertyChangedEventArgs e)
				{
					ApplyPattern();
					Dirty = true;
				}
			}
		}
		private IPattern m_pattern = null!;

		/// <summary>The pattern before any changes</summary>
		public IPattern? Original
		{
			get => m_original;
			private set
			{
				if (m_original == value) return;
				m_original = value;
			}
		}
		private IPattern? m_original;

		/// <summary>Test text for trying output patterns</summary>
		public string TestText
		{
			get => (string)GetValue(TestTextProperty);
			set => SetValue(TestTextProperty, value);
		}
		private void TestText_Changed(string value)
		{
			if (Pattern == null) return;
			ApplyPattern();
		}
		public static readonly DependencyProperty TestTextProperty = Gui_.DPRegister<PatternEditor>(nameof(TestText), DefaultTestText);
		private const string DefaultTestText =
			"Enter text here to test your pattern.\n" +
			"The pattern is applied to each line separately\n" +
			"so that you can test multiple cases simultaneously.\n";

		/// <summary>The capture groups</summary>
		public ObservableCollection<CaptureGroup> CaptureGroups { get; }

		/// <summary>True when user activity has changed something in the UI</summary>
		public bool Dirty
		{
			get => m_dirty;
			set
			{
				if (m_dirty == value) return;
				m_dirty = value;
				NotifyPropertyChanged(nameof(Dirty));
			}
		}
		private bool m_dirty;

		/// <summary>True if the pattern currently contained is a new instance, vs editing an existing pattern</summary>
		public bool IsNew => Original is null;

		/// <summary>True if the pattern contains unsaved changes</summary>
		public bool HasUnsavedChanges =>
			Pattern != null &&
			((IsNew && Pattern.Expr.Length != 0 && Dirty) ||
			(!IsNew && !Equals(Original, Pattern)));

		/// <summary>True if the contained pattern is valid and therefore can be saved</summary>
		public bool PatternValid => Pattern?.IsValid ?? false;

		/// <summary>Apply highlighting to the test text</summary>
		private void ApplyPattern()
		{
			if (m_in_apply_pattern != 0) return;
			using var in_apply_pattern = Scope.Create(() => ++m_in_apply_pattern, () => --m_in_apply_pattern);

			// Notify when 'CommentEnabled' changes
			if (PatternValid != m_last_pattern_valid)
			{
				NotifyPropertyChanged(nameof(PatternValid));
				m_last_pattern_valid = PatternValid;
			}
			if (HasUnsavedChanges != m_last_has_unsaved_changes)
			{
				NotifyPropertyChanged(nameof(HasUnsavedChanges));
				m_last_has_unsaved_changes = HasUnsavedChanges;
			}

			// Apply the pattern to the test text
			var doc = new FlowDocument();
			if (Pattern != null && Pattern.IsValid && Pattern.CaptureGroupNames.Length >= 1)
			{
				// Process each line of the test text
				foreach (var line in TestText.Split('\n'))
				{
					var par = new Paragraph() { Margin = new Thickness(0) };
					var grp = Pattern.Match(line).GetEnumerator();
					if (grp.MoveNext())
					{
						var no_match_colour = Brushes.Transparent;
						var global_match_colour = BkColors[0];

						// Add text from 0 to start of global match
						var r0 = grp.Current;
						if (r0.Begi != 0)
							par.Inlines.Add(new Run(line.Substring(0, r0.Begi)) { Background = no_match_colour });

						// Add matches within global match
						var c = 1;
						for (; grp.MoveNext();)
						{
							var r = grp.Current;
							if (r0.Begi != r.Begi) par.Inlines.Add(new Run(line.Substring(r0.Begi, r.Begi - r0.Begi)) { Background = global_match_colour });
							if (r.Begi != r.Endi) par.Inlines.Add(new Run(line.Substring(r.Begi, r.Endi - r.Begi)) { Background = BkColors[c++ % BkColors.Length] });
							r0.Beg = r.End;
							r0.End = Math.Max(r0.Beg, r0.End);
						}
						if (r0.Begi != r0.Endi) par.Inlines.Add(new Run(line.Substring(r0.Begi, r0.Endi - r0.Begi)) { Background = global_match_colour });

						// Add text after end of global match
						if (r0.Endi != line.Length)
							par.Inlines.Add(new Run(line.Substring(r0.Endi)) { Background = no_match_colour });
					}
					else
					{
						par.Inlines.Add(new Run(line));
					}

					// Add the line to the doc
					doc.Blocks.Add(par);
				}
			}
			else
			{
				foreach (var line in TestText.Split('\n'))
					doc.Blocks.Add(new Paragraph(new Run(line)) { Margin = new Thickness(0) });
			}
			m_rtb.Document = doc;

			// Update the capture groups
			UpdateCaptureGroups();
		}
		private bool m_last_pattern_valid;
		private bool m_last_has_unsaved_changes;
		private int m_in_apply_pattern;

		/// <summary>Update the collection of capture groups</summary>
		private void UpdateCaptureGroups()
		{
			CaptureGroups.Clear();
			if (Pattern != null && Pattern.IsValid)
			{
				// Get the line that the caret is position is on
				var this_line = m_rtb.CaretPosition.GetLineStartPosition(0) ?? m_rtb.CaretPosition.DocumentStart;
				var next_line = m_rtb.CaretPosition.GetLineStartPosition(1) ?? m_rtb.CaretPosition.DocumentEnd;
				var line = new TextRange(this_line, next_line).Text.Trim('\n', '\r');

				// Update the capture groups data
				var groups = new Dictionary<string, string>();
				foreach (var name in Pattern.CaptureGroupNames) groups[name] = string.Empty;
				foreach (var cap in Pattern.CaptureGroups(line)) groups[cap.Key] = cap.Value;
				foreach (var group in groups)
					CaptureGroups.Add(new CaptureGroup(group.Key, group.Value));
			}
		}

		/// <summary>Commit changes to the pattern</summary>
		public Command CommitChanges { get; }
		private void CommitChangesInternal()
		{
			NotifyCommit();
		}

		/// <summary>Show a help dialog</summary>
		public Command ShowHelp { get; }
		private void ShowHelpInternal()
		{
			if (m_help_ui == null)
			{
				var browser = new WebView();
				var panel = new DockPanel { LastChildFill = true };
				panel.Children.Add(browser);

				m_help_ui = new Window
				{
					Title = "Regular Expressions Quick Reference",
					Icon = Window.GetWindow(this)?.Icon,
					Content = panel,
				};
				m_help_ui.Loaded += (s, a) => { browser.NavigateToString(WPF.Resources.regex_quick_ref); };
				m_help_ui.Closed += (s, a) => { m_help_ui = null; };
				m_help_ui.Show();
			}
			m_help_ui.Focus();
		}
		private Window? m_help_ui;

		/// <summary>Property changed notification</summary>
		public event PropertyChangedEventHandler? PropertyChanged;
		private void NotifyPropertyChanged(string prop_name)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
		}

		/// <summary>Capture group key/value pair</summary>
		public class CaptureGroup
		{
			public CaptureGroup(string tag, string value)
			{
				Tag = tag;
				Value = value;
			}
			public string Tag { get; private set; }
			public string Value { get; private set; }
		}

		/// <summary>Colours used for match highlighting</summary>
		private static readonly Brush[] BkColors = new[]
		{
			Brushes.LightGreen, Brushes.LightBlue, Brushes.LightCoral, Brushes.LightSalmon, Brushes.Violet, Brushes.LightSkyBlue,
			Brushes.Aquamarine, Brushes.Yellow, Brushes.Orchid, Brushes.GreenYellow, Brushes.PaleGreen, Brushes.Goldenrod, Brushes.MediumTurquoise,
		};
	}
}
