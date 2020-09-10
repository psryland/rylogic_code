using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Rylogic.Gui.WPF.TextEditor;
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
			TextArea = new TextArea();
			Document = new TextDocument();
		}

		/// <inheritdoc/>
		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
			((IScrollInfo)TextArea).ScrollOwner = (ScrollViewer)Template.FindName("PART_ScrollViewer", this);
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
	}
}