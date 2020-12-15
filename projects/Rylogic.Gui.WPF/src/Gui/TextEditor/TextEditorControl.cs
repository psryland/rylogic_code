using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using Rylogic.Common;
using Rylogic.Gui.WPF.TextEditor;
using Rylogic.Maths;
using Rylogic.Utility;

namespace Rylogic.Gui.WPF
{
	[Localizability(LocalizationCategory.Text), ContentProperty("Text")]
	public partial class TextEditorControl :Control, INotifyPropertyChanged
	{
		// Notes:
		//  - This is a Scintilla-inspired pure WPF text editor control
		//  - Architecturally, it modelled on AvalonEdit.
		//      'TextEditorControl' is basically a container.
		//      'TextDocument' contains all line/text management (think fancy string builder)
		//      'TextArea' handles user input and caret.
		//      'TextView' handles all rendering.
		static TextEditorControl()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(TextEditorControl), new FrameworkPropertyMetadata(typeof(TextEditorControl)));
			FocusableProperty.OverrideMetadata(typeof(TextEditorControl), new FrameworkPropertyMetadata(Boxed.True));
		}
		public TextEditorControl()
		{
			Options = new OptionsData();
			TextArea = new TextArea(Options);
			Document = new TextDocument(Options);
		}

		/// <inheritdoc/>
		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
			ScrollViewer = (ScrollViewer)Template.FindName("PART_ScrollViewer", this);
		}
		
		/// <inheritdoc/>
		protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
		{
			/// Forward focus to TextArea.
			base.OnGotKeyboardFocus(e);
			if (e.NewFocus == this)
			{
				Keyboard.Focus(TextArea);
				e.Handled = true;
			}
		}

		/// <summary>Text editor options</summary>
		public OptionsData Options
		{
			get => m_options;
			private set
			{
				if (m_options == value) return;
				if (m_options != null)
				{
					m_options.SettingChange -= HandleSettingChange;
				}
				m_options = value;
				if (m_options != null)
				{
					m_options.SettingChange += HandleSettingChange;
				}

				// Handlers
				void HandleSettingChange(object? sender, SettingChangeEventArgs e)
				{
				}
			}
		}
		private OptionsData m_options = null!;

		/// <summary>Renderering and user input</summary>
		public TextArea TextArea
		{
			get => m_text_area;
			private set
			{
				if (m_text_area == value) return;
				if (m_text_area != null)
				{}
				m_text_area = value;
				if (m_text_area != null)
				{}
			}
		}
		private TextArea m_text_area = null!;

		/// <summary>Gets/Sets the document displayed by the text editor.</summary>
		public TextDocument Document
		{
			get => (TextDocument)GetValue(DocumentProperty);
			set => SetValue(DocumentProperty, value);
		}
		void Document_Changed(TextDocument new_value, TextDocument old_value)
		{
			if (old_value != null)
			{
				//TextDocumentWeakEventManager.TextChanged.RemoveListener(oldValue, this);
				//PropertyChangedEventManager.RemoveListener(oldValue.UndoStack, this, "IsOriginalFile");
			}
			TextArea.Document = new_value;
			if (new_value != null)
			{
				//TextDocumentWeakEventManager.TextChanged.AddListener(newValue, this);
				//PropertyChangedEventManager.AddListener(newValue.UndoStack, this, "IsOriginalFile");
			}
			DocumentChanged?.Invoke(this, EventArgs.Empty);
			//OnTextChanged(EventArgs.Empty);
		}
		public static readonly DependencyProperty DocumentProperty = TextView.DocumentProperty.AddOwner<TextEditorControl>(nameof(Document));

		/// <summary>Occurs when the document property has changed.</summary>
		public event EventHandler? DocumentChanged;

		/// <summary>Specifies whether the text editor uses word wrapping.</summary>
		public bool WordWrap
		{
			get => (bool)GetValue(WordWrapProperty);
			set => SetValue(WordWrapProperty, Boxed.Box(value));
		}
		public static readonly DependencyProperty WordWrapProperty = Gui_.DPRegister<TextEditorControl>(nameof(WordWrap), Boxed.False);

		/// <inheritdoc/>
		public event PropertyChangedEventHandler? PropertyChanged;
		private void NotifyPropertyChanged(string prop_name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));

		/// <summary>Forward a command to the TextArea</summary>
		private bool CanExecute(RoutedUICommand command)
		{
			return TextArea != null ? command.CanExecute(null, TextArea) : false;
		}

		/// <summary>Forward a command to the TextArea</summary>
		private void Execute(RoutedUICommand command)
		{
			if (TextArea != null)
				command.Execute(null, TextArea);
		}

		#region Scrolling

		/// <summary>Gets the scroll viewer used by the text editor. This property can be null if the template has not been applied or does not contain a scroll viewer.</summary>
		internal ScrollViewer? ScrollViewer
		{
			get => ((IScrollInfo)TextArea).ScrollOwner;
			set => ((IScrollInfo)TextArea).ScrollOwner = value;
		}

		/// <summary>Gets/Sets the horizontal scroll bar visibility.</summary>
		public ScrollBarVisibility HorizontalScrollBarVisibility
		{
			get => (ScrollBarVisibility)GetValue(HorizontalScrollBarVisibilityProperty);
			set => SetValue(HorizontalScrollBarVisibilityProperty, value);
		}
		public static readonly DependencyProperty HorizontalScrollBarVisibilityProperty = ScrollViewer.HorizontalScrollBarVisibilityProperty.AddOwner<TextEditorControl>(nameof(HorizontalScrollBarVisibility), ScrollBarVisibility.Visible);

		/// <summary>Gets/Sets the vertical scroll bar visibility.</summary>
		public ScrollBarVisibility VerticalScrollBarVisibility
		{
			get => (ScrollBarVisibility)GetValue(VerticalScrollBarVisibilityProperty);
			set => SetValue(VerticalScrollBarVisibilityProperty, value);
		}
		public static readonly DependencyProperty VerticalScrollBarVisibilityProperty = ScrollViewer.VerticalScrollBarVisibilityProperty.AddOwner<TextEditorControl>(nameof(VerticalScrollBarVisibility), ScrollBarVisibility.Visible);

		/// <summary>Scrolls to the specified line.</summary>
		public void ScrollToLine(int line) => ScrollTo(line, -1);

		/// <summary>Scrolls to the specified line/column.</summary>
		public void ScrollTo(int line, int column)
		{
			// This method requires that the TextEditor was already assigned a size (WPF layout must have run prior).

			var text_view = TextArea.TextView;
			var document = text_view.Document;
			if (ScrollViewer != null && document != null)
			{
				line = Math_.Clamp(line, 0, document.LineCount - 1);

				if (text_view is IScrollInfo scrollInfo && !scrollInfo.CanHorizontallyScroll)
				{
					//// Word wrap is enabled. Ensure that we have up-to-date info about line height so that we scroll
					//// to the correct position.
					//// This avoids that the user has to repeat the ScrollTo() call several times when there are very long lines.
					//VisualLine vl = text_view.GetOrConstructVisualLine(document.GetLineByNumber(line));
					//double remainingHeight = ScrollViewer.ViewportHeight / 2;
					//while (remainingHeight > 0)
					//{
					//	DocumentLine prevLine = vl.FirstDocumentLine.PreviousLine;
					//	if (prevLine == null)
					//		break;
					//	vl = text_view.GetOrConstructVisualLine(prevLine);
					//	remainingHeight -= vl.Height;
					//}
				}

				// Get the visual position of the line
				var line_pos = text_view.VisualPosition(new TextLocation(line, Math.Max(0, column)), VisualYPosition.LineMiddle);

				// Scroll so that the line is visible
				const double MinimumScrollPercentage = 0.3;
				var ypos = line_pos.Y - ScrollViewer.ViewportHeight / 2;
				if (Math.Abs(ypos - ScrollViewer.VerticalOffset) > MinimumScrollPercentage * ScrollViewer.ViewportHeight)
				{
					ScrollViewer.ScrollToVerticalOffset(Math.Max(0, ypos));
				}

				if (column > 0)
				{
					if (line_pos.X > ScrollViewer.ViewportWidth - Caret.MinimumDistanceToViewBorder * 2)
					{
						double horizontalPos = Math.Max(0, line_pos.X - ScrollViewer.ViewportWidth / 2);
						if (Math.Abs(horizontalPos - ScrollViewer.HorizontalOffset) > MinimumScrollPercentage * ScrollViewer.ViewportWidth)
						{
							ScrollViewer.ScrollToHorizontalOffset(horizontalPos);
						}
					}
					else
					{
						ScrollViewer.ScrollToHorizontalOffset(0);
					}
				}
			}
		}

		#endregion
	}
}