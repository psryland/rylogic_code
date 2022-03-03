using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Rylogic.Utility;

namespace Rylogic.Gui.WPF.TextEditor
{
	public class TextArea :Control, IScrollInfo, INotifyPropertyChanged
	{
		// Notes:
		//  - 'TextArea' handles user input and caret.
		//  - 'TextArea' is a wrapper around a 'TextView' that adds user input support

		static TextArea()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(TextArea), new FrameworkPropertyMetadata(typeof(TextArea)));
			KeyboardNavigation.IsTabStopProperty.OverrideMetadata(typeof(TextArea), new FrameworkPropertyMetadata(Boxed.True));
			KeyboardNavigation.TabNavigationProperty.OverrideMetadata(typeof(TextArea), new FrameworkPropertyMetadata(KeyboardNavigationMode.None));
			FocusableProperty.OverrideMetadata(typeof(TextArea), new FrameworkPropertyMetadata(Boxed.True));
		}
		public TextArea()
			: this(new OptionsData())
		{
		}
		public TextArea(OptionsData options)
			:this(options, new TextView(options))
		{
		}
		protected TextArea(OptionsData options, TextView text_view)
		{
			Options = options;
			TextView = text_view;
			EmptySelection = new EmptySelection(this);
			m_selection = EmptySelection;
		}

		/// <summary>Text editor options</summary>
		public OptionsData Options { get; }

		/// <summary>The view implementation</summary>
		public TextView TextView
		{
			get => m_textview;
			private set
			{
				if (m_textview == value) return;
				if (m_textview != null)
				{ }
				m_textview = value;
				if (m_textview != null)
				{
				}
				NotifyPropertyChanged(nameof(TextView));
			}
		}
		private TextView m_textview = null!;

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
				//TextDocumentWeakEventManager.Changing.RemoveListener(old_value, this);
				//TextDocumentWeakEventManager.Changed.RemoveListener(old_value, this);
				//TextDocumentWeakEventManager.UpdateStarted.RemoveListener(old_value, this);
				//TextDocumentWeakEventManager.UpdateFinished.RemoveListener(old_value, this);
			}
			TextView.Document = new_value;
			if (new_value != null)
			{
				//TextDocumentWeakEventManager.Changing.AddListener(new_value, this);
				//TextDocumentWeakEventManager.Changed.AddListener(new_value, this);
				//TextDocumentWeakEventManager.UpdateStarted.AddListener(new_value, this);
				//TextDocumentWeakEventManager.UpdateFinished.AddListener(new_value, this);
			}
			// Reset caret location and selection: this is necessary because the caret/selection might be invalid
			// in the new document (e.g. if new document is shorter than the old document).
			//caret.Location = new TextLocation(1, 1);
			//this.ClearSelection();

			NotifyPropertyChanged(nameof(Document));
			DocumentChanged?.Invoke(this, EventArgs.Empty);
			//CommandManager.InvalidateRequerySuggested();
		}
		public static readonly DependencyProperty DocumentProperty = TextView.DocumentProperty.AddOwner<TextArea>(nameof(Document));

		/// <inheritdoc/>
		public event EventHandler? DocumentChanged;

		#region Selection property

		/// <summary>Empty selection</summary>
		internal Selection EmptySelection { get; }

		/// <summary>Gets/Sets the selection in this text area.</summary>
		public Selection Selection
		{
			get => m_selection;
			set
			{
				// Check the TextArea's are the same
				if (value.TextArea != this)
					throw new ArgumentException("Cannot use a Selection instance that belongs to another text area.");

				if (m_selection == value) return;
				var nue = value.SurroundingSegment;
				var old = m_selection.SurroundingSegment;

				// 
				//if (!Options.EnableVirtualSpace && (m_selection is SimpleSelection && value is SimpleSelection && old != null && nue != null))
				//{
				//	// perf optimization:
				//	// When a simple selection changes, don't redraw the whole selection, but only the changed parts.
				//	int oldSegmentOffset = old.Offset;
				//	int newSegmentOffset = nue.Offset;
				//	if (oldSegmentOffset != newSegmentOffset)
				//	{
				//		text_view.Redraw(Math.Min(oldSegmentOffset, newSegmentOffset),
				//						Math.Abs(oldSegmentOffset - newSegmentOffset),
				//						DispatcherPriority.Background);
				//	}
				//	int oldSegmentEndOffset = old.EndOffset;
				//	int newSegmentEndOffset = nue.EndOffset;
				//	if (oldSegmentEndOffset != newSegmentEndOffset)
				//	{
				//		text_view.Redraw(Math.Min(oldSegmentEndOffset, newSegmentEndOffset),
				//						Math.Abs(oldSegmentEndOffset - newSegmentEndOffset),
				//						DispatcherPriority.Background);
				//	}
				//}
				//else
				{
					TextView.Redraw(old, DispatcherPriority.Background);
					TextView.Redraw(nue, DispatcherPriority.Background);
				}

				m_selection = value;
				SelectionChanged?.Invoke(this, EventArgs.Empty);

				// a selection change causes commands like copy/paste/etc. to change status
				CommandManager.InvalidateRequerySuggested();
			}
		}
		private Selection m_selection = null!;

		/// <summary>Occurs when the selection has changed.</summary>
		public event EventHandler? SelectionChanged;

		/// <summary>Clears the current selection.</summary>
		public void ClearSelection()
		{
			Selection = EmptySelection;
		}

		/// <summary>Gets/Sets the background brush used for the selection.</summary>
		public Brush SelectionBrush
		{
			get => (Brush)GetValue(SelectionBrushProperty);
			set => SetValue(SelectionBrushProperty, value);
		}
		public static readonly DependencyProperty SelectionBrushProperty = Gui_.DPRegister<TextArea>(nameof(SelectionBrush), Brushes.LightSteelBlue, Gui_.EDPFlags.None);

		/// <summary>Gets/Sets the foreground brush used selected text.</summary>
		public Brush SelectionForeground
		{
			get => (Brush)GetValue(SelectionForegroundProperty);
			set => SetValue(SelectionForegroundProperty, value);
		}
		public static readonly DependencyProperty SelectionForegroundProperty = Gui_.DPRegister<TextArea>(nameof(SelectionForeground), Brushes.White, Gui_.EDPFlags.None);

		/// <summary>Gets/Sets the background brush used for the selection.</summary>
		public Pen SelectionBorder
		{
			get => (Pen)GetValue(SelectionBorderProperty);
			set => SetValue(SelectionBorderProperty, value);
		}
		public static readonly DependencyProperty SelectionBorderProperty = Gui_.DPRegister<TextArea>(nameof(SelectionBorder), new Pen(Brushes.SteelBlue,1), Gui_.EDPFlags.None);

		/// <summary>Gets/Sets the corner radius of the selection.</summary>
		public double SelectionCornerRadius
		{
			get => (double)GetValue(SelectionCornerRadiusProperty);
			set => SetValue(SelectionCornerRadiusProperty, value);
		}
		public static readonly DependencyProperty SelectionCornerRadiusProperty = Gui_.DPRegister<TextArea>(nameof(SelectionCornerRadius), 3.0, Gui_.EDPFlags.None);

		#endregion

		#region IScrollInfo implementation
		ScrollViewer IScrollInfo.ScrollOwner
		{
			get => ((IScrollInfo)TextView).ScrollOwner;
			set => ((IScrollInfo)TextView).ScrollOwner = value;
		}
		bool IScrollInfo.CanVerticallyScroll
		{
			get => ((IScrollInfo)TextView).CanVerticallyScroll;
			set => ((IScrollInfo)TextView).CanVerticallyScroll = value;
		}
		bool IScrollInfo.CanHorizontallyScroll
		{
			get => ((IScrollInfo)TextView).CanHorizontallyScroll;
			set => ((IScrollInfo)TextView).CanHorizontallyScroll = value;
		}
		double IScrollInfo.ExtentWidth => ((IScrollInfo)TextView).ExtentWidth;
		double IScrollInfo.ExtentHeight => ((IScrollInfo)TextView).ExtentHeight;
		double IScrollInfo.ViewportWidth => ((IScrollInfo)TextView).ViewportWidth;
		double IScrollInfo.ViewportHeight => ((IScrollInfo)TextView).ViewportHeight;
		double IScrollInfo.HorizontalOffset => ((IScrollInfo)TextView).HorizontalOffset;
		double IScrollInfo.VerticalOffset => ((IScrollInfo)TextView).VerticalOffset;

		void IScrollInfo.LineUp() => ((IScrollInfo)TextView).LineUp();
		void IScrollInfo.LineDown() => ((IScrollInfo)TextView).LineDown();
		void IScrollInfo.LineLeft() => ((IScrollInfo)TextView).LineLeft();
		void IScrollInfo.LineRight() => ((IScrollInfo)TextView).LineRight();
		void IScrollInfo.PageUp() => ((IScrollInfo)TextView).PageUp();
		void IScrollInfo.PageDown() => ((IScrollInfo)TextView).PageDown();
		void IScrollInfo.PageLeft() => ((IScrollInfo)TextView).PageLeft();
		void IScrollInfo.PageRight() => ((IScrollInfo)TextView).PageRight();
		void IScrollInfo.MouseWheelUp() => ((IScrollInfo)TextView).MouseWheelUp();
		void IScrollInfo.MouseWheelDown() => ((IScrollInfo)TextView).MouseWheelDown();
		void IScrollInfo.MouseWheelLeft() => ((IScrollInfo)TextView).MouseWheelLeft();
		void IScrollInfo.MouseWheelRight() => ((IScrollInfo)TextView).MouseWheelRight();
		void IScrollInfo.SetHorizontalOffset(double offset) => ((IScrollInfo)TextView).SetHorizontalOffset(offset);
		void IScrollInfo.SetVerticalOffset(double offset) => ((IScrollInfo)TextView).SetVerticalOffset(offset);
		Rect IScrollInfo.MakeVisible(Visual visual, Rect rectangle) => ((IScrollInfo)TextView).MakeVisible(visual, rectangle);
		#endregion

		/// <summary></summary>
		public event PropertyChangedEventHandler? PropertyChanged;
		private void NotifyPropertyChanged(string prop_name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop_name));
	}
}
