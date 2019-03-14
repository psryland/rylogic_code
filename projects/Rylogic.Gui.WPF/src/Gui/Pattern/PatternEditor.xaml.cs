using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Microsoft.Toolkit.Wpf.UI.Controls;
using Rylogic.Common;

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
		event EventHandler Commit;

		/// <summary>Access to the test text field</summary>
		string TestText { get; set; }

		/// <summary>Set focus to the primary input field</summary>
		void FocusInput();
	}

	/// <summary>A control for editing patterns</summary>
	public partial class PatternEditor : UserControl ,INotifyPropertyChanged
	{
		public PatternEditor()
		{
			InitializeComponent();
			CaptureGroups = new ObservableCollection<CaptureGroup>();
			TestText =
				"Enter text here to test your pattern.\n" +
				"The pattern to applied to each line separately\n" +
				"so that you can test multiple cases simultaneously.\n";

			// Bind
			m_root.DataContext = this;
			ApplyPattern();
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
			get { return m_pattern; }
			private set
			{
				if (m_pattern == value) return;
				m_pattern = value;
				IgnoreCase = m_pattern.IgnoreCase;
				WholeLine = m_pattern.WholeLine;
				Invert = m_pattern.Invert;
			}
		}
		private IPattern m_pattern;

		/// <summary>The pattern before any changes</summary>
		public IPattern Original
		{
			get { return m_original; }
			private set
			{
				if (m_original == value) return;
				m_original = value;
			}
		}
		private IPattern m_original;

		/// <summary>Pattern test area text</summary>
		public string PatternExpr
		{
			get { return (string)GetValue(PatternExprProperty); }
			set { SetValue(PatternExprProperty, value); }
		}
		private void PatternExpr_Changed(string value)
		{
			if (Pattern == null) return;
			Pattern.Expr = value;
			Touched = true;
			ApplyPattern();
		}
		public static readonly DependencyProperty PatternExprProperty = Gui_.DPRegister<PatternEditor>(nameof(PatternExpr));

		/// <summary>The current pattern type</summary>
		public EPattern PatnType
		{
			get { return (EPattern)GetValue(PatnTypeProperty); }
			set { SetValue(PatnTypeProperty, value); }
		}
		private void PatnType_Changed(EPattern patn_type)
		{
			if (Pattern == null) return;
			Pattern.PatnType = patn_type;
			Touched = true;
			ApplyPattern();
		}
		public static readonly DependencyProperty PatnTypeProperty = Gui_.DPRegister<PatternEditor>(nameof(PatnType));

		/// <summary>The ignore case flag</summary>
		public bool IgnoreCase
		{
			get { return (bool)GetValue(IgnoreCaseProperty); }
			set { SetValue(IgnoreCaseProperty, value); }
		}
		private void IgnoreCase_Changed(bool value)
		{
			if (Pattern == null) return;
			Pattern.IgnoreCase = value;
			Touched = true;
			ApplyPattern();
		}
		public static readonly DependencyProperty IgnoreCaseProperty = Gui_.DPRegister<PatternEditor>(nameof(IgnoreCase));

		/// <summary>The whole line flag</summary>
		public bool WholeLine
		{
			get { return (bool)GetValue(WholeLineProperty); }
			set { SetValue(WholeLineProperty, value); }
		}
		private void WholeLine_Changed(bool value)
		{
			if (Pattern == null) return;
			Pattern.WholeLine = value;
			Touched = true;
			ApplyPattern();
		}
		public static readonly DependencyProperty WholeLineProperty = Gui_.DPRegister<PatternEditor>(nameof(WholeLine));

		/// <summary>The invert flag</summary>
		public bool Invert
		{
			get { return (bool)GetValue(InvertProperty); }
			set { SetValue(InvertProperty, value); }
		}
		private void Invert_Changed(bool value)
		{
			if (Pattern == null) return;
			Pattern.Invert = value;
			Touched = true;
			ApplyPattern();
		}
		public static readonly DependencyProperty InvertProperty = Gui_.DPRegister<PatternEditor>(nameof(Invert));

		/// <summary>Test text for trying output patterns</summary>
		public string TestText
		{
			get { return (string)GetValue(TestTextProperty); }
			set { SetValue(TestTextProperty, value); }
		}
		private void TestText_Changed(string value)
		{
			if (Pattern == null) return;
			ApplyPattern();
		}
		public static readonly DependencyProperty TestTextProperty = Gui_.DPRegister<PatternEditor>(nameof(TestText));

		/// <summary>The capture groups</summary>
		public ObservableCollection<CaptureGroup> CaptureGroups { get; private set; }

		/// <summary>True when user activity has changed something in the UI</summary>
		public bool Touched { get; set; }

		/// <summary>True if the pattern currently contained is a new instance, vs editing an existing pattern</summary>
		public bool IsNew
		{
			get { return Original is null; }
		}

		/// <summary>True if the pattern contains unsaved changes</summary>
		public bool HasUnsavedChanges
		{
			get
			{
				return
					Pattern != null &&
					(( IsNew && Pattern.Expr.Length != 0 && Touched) ||
					 (!IsNew && !Equals(Original, Pattern)));
			}
		}

		/// <summary>True if the contained pattern is valid and therefore can be saved</summary>
		public bool CommitEnabled
		{
			get { return Pattern?.IsValid ?? false; }
		}

		/// <summary>Raised when the 'Commit' button is hit and the pattern field contains a valid pattern</summary>
		public event EventHandler Commit;
		private void RaiseCommitEvent()
		{
			Commit?.Invoke(this, EventArgs.Empty);
		}

		/// <summary>Property changed notification</summary>
		public event PropertyChangedEventHandler PropertyChanged;
		private bool SetProp<T>(ref T prop, T value, string prop_name)
		{
			if (Equals(prop, value)) return false;
			prop = value;
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
			return true;
		}

		/// <summary>Apply highlighting to the test text</summary>
		private void ApplyPattern()
		{
			// Notify when 'CommentEnabled' changes
			if (CommitEnabled != _last_commit_enabled)
			{
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CommitEnabled)));
				_last_commit_enabled = CommitEnabled;
			}
			if (HasUnsavedChanges != _last_has_unsaved_changes)
			{
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HasUnsavedChanges)));
				_last_has_unsaved_changes = HasUnsavedChanges;
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
		private bool _last_commit_enabled;
		private bool _last_has_unsaved_changes;

		/// <summary>Update the collection of capture groups</summary>
		private void UpdateCaptureGroups()
		{
			CaptureGroups.Clear();
			if (Pattern != null && Pattern.IsValid)
			{
				// Get the line that the caret is position is on
				var this_line = m_rtb.CaretPosition.GetLineStartPosition(0) ?? m_rtb.CaretPosition.DocumentStart;
				var next_line = m_rtb.CaretPosition.GetLineStartPosition(1) ?? m_rtb.CaretPosition.DocumentEnd;
				var line = new TextRange(this_line, next_line).Text.Trim('\n','\r');

				// Update the capture groups data
				var groups = new Dictionary<string, string>();
				foreach (var name in Pattern.CaptureGroupNames) groups[name] = string.Empty;
				foreach (var cap in Pattern.CaptureGroups(line)) groups[cap.Key] = cap.Value;
				foreach (var group in groups)
					CaptureGroups.Add(new CaptureGroup(group.Key, group.Value));
			}
		}

		/// <summary>Show a help dialog</summary>
		private void ShowHelp()
		{
			if (_help_ui == null)
			{
				var browser = new WebView();
				var panel = new DockPanel { LastChildFill = true };
				panel.Children.Add(browser);

				_help_ui = new Window
				{
					Title = "Regular Expressions Quick Reference",
					Icon = Window.GetWindow(this)?.Icon,
					Content = panel,
				};
				_help_ui.Loaded += (s, a) =>
				{
					browser.NavigateToString(WPF.Resources.regex_quick_ref);
				};
				_help_ui.Closed += (s, a) =>
				{
					_help_ui = null;
				};
			}
			_help_ui.Show();
		}
		private Window _help_ui;

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
