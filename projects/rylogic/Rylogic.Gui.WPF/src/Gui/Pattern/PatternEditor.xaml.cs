using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Rylogic.Common;
using Rylogic.Extn;
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
		// Notes:
		//  - 'Pattern' is updated as changes are made. The 'Commit' event is only needed to
		//    allow callers to close the containing window or update something.
		//  - 'Pattern' is usually a new instance; either new, or cloned from the given pattern.
		//  - Using 'clone = false' in EditPattern allows the given instance to be modified.
		//  - Once the editor is closed, the caller decides whether to use 'Pattern' or 'Original'.

		private const string DefaultTestText =
			"Enter text here to test your pattern.\n" +
			"The pattern is applied to each line separately\n" +
			"so that you can test multiple cases simultaneously.\n" +
			"Select a line to see the capture groups for that line.\n";

		public PatternEditor()
		{
			InitializeComponent();
			Original = null;
			CaptureGroups = new ObservableCollection<CaptureGroup>();
			Pattern = new Pattern();

			CommitChanges = Command.Create(this, CommitChangesInternal);
			ShowHelp = Command.Create(this, ShowHelpInternal);
			
			DataContext = this;
			TestText = DefaultTestText;

			Loaded += delegate
			{
				m_tb_expr.Focus();
				ApplyHighlighting();
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

		/// <summary>Select a pattern into the UI for editing. If 'clone' is false, the given pattern is the one that's modified</summary>
		public void EditPattern(IPattern pattern, bool clone = true)
		{
			Original = pattern;
			Pattern = clone ? (IPattern)Original.Clone() : pattern;
		}

		/// <summary>The pattern being edited</summary>
		public IPattern Pattern
		{
			get;
			private set
			{
				if (field == value) return;
				field = value ?? new Pattern();
				field.PropertyChanged += WeakRef.MakeWeak(HandlePatternPropertyChanged, h => field.PropertyChanged -= h);
				NotifyPropertyChanged(string.Empty);
				
				// Handler
				void HandlePatternPropertyChanged(object? sender, PropertyChangedEventArgs e)
				{
					ApplyHighlighting();
					Dirty = true;
				}
			}
		} = null!;

		/// <summary>The pattern before any changes</summary>
		public IPattern? Original
		{
			get;
			private set
			{
				if (field == value) return;
				field = value;
			}
		}

		/// <summary>Test text for trying output patterns</summary>
		public string TestText
		{
			get;
			set
			{
				if (TestText == value) return;
				field = value;

				// Update the highlighting
				if (Pattern != null)
					ApplyHighlighting();

				NotifyPropertyChanged(nameof(TestText));
			}
		} = string.Empty;

		/// <summary>The capture groups</summary>
		public ObservableCollection<CaptureGroup> CaptureGroups { get; }

		/// <summary>True when user activity has changed something in the UI</summary>
		public bool Dirty
		{
			get;
			set
			{
				if (field == value) return;
				field = value;
				NotifyPropertyChanged(nameof(Dirty));
				NotifyPropertyChanged(nameof(HasUnsavedChanges));
			}
		}

		/// <summary>True if the pattern currently contained is a new instance, vs editing an existing pattern</summary>
		public bool IsNew => Original is null;

		/// <summary>True if the pattern contains unsaved changes</summary>
		public bool HasUnsavedChanges =>
			Pattern != null && (
			(IsNew && Pattern.Expr.Length != 0 && Dirty) ||
			(!IsNew && (!Equals(Original, Pattern) || ReferenceEquals(Original, Pattern)))
			);

		/// <summary>True if the contained pattern is valid and therefore can be saved</summary>
		public bool PatternValid => Pattern?.IsValid ?? false;

		/// <summary>Apply highlighting to the test text</summary>
		private void ApplyHighlighting()
		{
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

			// Remove all highlighting
			m_highlight.Children.Clear();

			// Remove all highlighting if the pattern is invalid
			if (PatternValid)
			{
				// If the pattern is a single line pattern, merge all paragraphs into one
				int c = 0, s = 0, e = Pattern.SingleLine ? TestText.Length : TestText.Find(x => x == '\r' || x == '\n');
				for (;s != TestText.Length;)
				{
					// Apply the pattern to each line
					var text = TestText.Substring(s, e - s);

					// Add highlights for matches
					foreach (var match in Pattern.Match(text).Where(x => !x.Empty))
						DoHighlight(match.Shift(s), BkColors[c++ % BkColors.Length]);

					s = TestText.Find(x => x != '\r' && x != '\n', e);
					e = TestText.Find(x => x == '\r' || x == '\n', s);
					c = 0;

					// Add a rectangle to highlight a region of text
					void DoHighlight(RangeI r, Brush fill)
					{
						var l0 = m_tb_testtext.GetLineIndexFromCharacterIndex(r.Begi);
						var l1 = m_tb_testtext.GetLineIndexFromCharacterIndex(r.Endi);

						// Same line
						if (l0 == l1)
						{
							var x0 = m_tb_testtext.GetRectFromCharacterIndex(r.Begi, false);
							var x1 = m_tb_testtext.GetRectFromCharacterIndex(r.Endi, false);
							var rect = new Rectangle { Width = x1.X - x0.X, Height = x0.Height, Fill = fill };
							Canvas.SetLeft(rect, x0.X + m_tb_testtext.HorizontalOffset);
							Canvas.SetTop(rect, x0.Y + m_tb_testtext.VerticalOffset);
							m_highlight.Children.Add(rect);
						}
						// Multiple lines
						else
						{
							for (var l = l0; l <= l1; ++l)
							{
								var x0 = l == l0
									? m_tb_testtext.GetRectFromCharacterIndex(r.Begi, false)
									: m_tb_testtext.GetRectFromCharacterIndex(m_tb_testtext.GetCharacterIndexFromLineIndex(l), false);
								var x1 = l == l1
									? m_tb_testtext.GetRectFromCharacterIndex(r.Endi, false)
									: m_tb_testtext.GetRectFromCharacterIndex(m_tb_testtext.GetCharacterIndexFromLineIndex(l) + Math.Max(0, m_tb_testtext.GetLineLength(l) - 1), false);

								var rect = new Rectangle { Width = x1.X - x0.X, Height = x0.Height, Fill = fill };
								Canvas.SetLeft(rect, x0.X + m_tb_testtext.HorizontalOffset);
								Canvas.SetTop(rect, x0.Y + m_tb_testtext.VerticalOffset);
								m_highlight.Children.Add(rect);
							}
						}
					}
				}
			}

			// Update the capture groups
			UpdateCaptureGroups();
		}
		private bool m_last_pattern_valid;
		private bool m_last_has_unsaved_changes;

		/// <summary>Update the collection of capture groups</summary>
		private void UpdateCaptureGroups()
		{
			CaptureGroups.Clear();
			if (Pattern != null && Pattern.IsValid)
			{
				string text;
				if (Pattern.SingleLine)
				{
					text = TestText;
				}
				else
				{
					// Get the line that the caret is positioned on
					var line_index = m_tb_testtext.GetLineIndexFromCharacterIndex(m_tb_testtext.CaretIndex);
					text = line_index != -1 ? m_tb_testtext.GetLineText(line_index).Trim('\r','\n') : string.Empty;
				}

				// Update the capture groups data
				var groups = new Dictionary<string, string>();
				foreach (var name in Pattern.CaptureGroupNames) groups[name] = string.Empty;
				foreach (var cap in Pattern.CaptureGroups(text)) groups[cap.Key] = cap.Value;
				foreach (var group in groups)
					CaptureGroups.Add(new CaptureGroup(group.Key, group.Value));
			}
		}

		/// <summary>Commit changes to the pattern</summary>
		public Command CommitChanges { get; }
		private void CommitChangesInternal()
		{
			if (!PatternValid) return;
			NotifyCommit();
		}

		/// <summary>Show a help dialog</summary>
		public Command ShowHelp { get; }
		private void ShowHelpInternal()
		{
			var help_filepath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "RegularExpressionQuickReference.html");
			File.WriteAllText(help_filepath, WPF.Resources.regex_quick_ref);
			var psi = new ProcessStartInfo(help_filepath) { UseShellExecute = true };
			Process.Start(psi);
		}

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

		/// <summary>Links the highlighting canvas can test text textbox together</summary>
		private void TestTextScrolled(object sender, ScrollChangedEventArgs e)
		{
			Canvas.SetLeft(m_highlight, -m_tb_testtext.HorizontalOffset);
			Canvas.SetTop(m_highlight, -m_tb_testtext.VerticalOffset);
		}
	}
}
