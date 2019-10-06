using System;
using System.ComponentModel;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Input;
using Rylogic.Utility;

namespace Rylogic.Gui.WPF
{
	public partial class ComboBoxAutoComplete : ComboBox
	{
		// Notes:
		//  - A combo box that has a drop down list of items based on the 
		//    current editable text. The drop down list can be updated dynamically
		//    with affecting the user input text
		//  - Use the 'IsTextSearchCaseSensitive' property to control case sensitivity.
		//    Is it re-purposing the property but the meaning is near enough.
		public ComboBoxAutoComplete()
		{
			InitializeComponent();
		}
		protected override void OnPreviewKeyDown(KeyEventArgs e)
		{
			if (e.Key == Key.Down || (e.Key == Key.Space && Keyboard.Modifiers.HasFlag(ModifierKeys.Control)))
			{
				// On down arrow, or Ctrl+Space, show the drop down (if not already visible)
				if (!IsDropDownOpen)
				{
					using (PreserveTextState())
						IsDropDownOpen = true;

					e.Handled = true;
					return;
				}
			}
			if (e.Key == Key.Delete || e.Key == Key.Back)
			{
				// Trigger an update of the completion list on characters deleted
				NotifyUpdateAutoCompleteList();
			}
			if (ItemsSource is ICollectionView view && !view.IsEmpty)
			{
				// On Down/Up arrow, selected the first/last item in the auto complete list
				if (e.Key == Key.Down && view.CurrentItem == null && IsDropDownOpen)
				{
					view.MoveCurrentToFirst();
					e.Handled = true;
					return;
				}
				if (e.Key == Key.Up && view.CurrentItem == null && IsDropDownOpen)
				{
					view.MoveCurrentToLast();
					e.Handled = true;
					return;
				}
			}
			base.OnPreviewKeyDown(e);
		}
		protected override void OnPreviewTextInput(TextCompositionEventArgs e)
		{
			// Trigger update of the auto complete list only after text input,
			// not TextChanged, because that changes with selection changes as well.
			NotifyUpdateAutoCompleteList();
			base.OnPreviewTextInput(e);
		}
		protected override void OnSelectionChanged(SelectionChangedEventArgs e)
		{
			base.OnSelectionChanged(e);
			if (ItemsSource is ICollectionView view)
				view.MoveCurrentTo(SelectedItem);
		}
		protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e)
		{
			base.OnLostKeyboardFocus(e);

			// On focus lost, try to set the selected item to match the current text
			if (SelectedItem == null && ItemsSource is ICollectionView view)
			{
				var ty = (Type?)null;
				var prop = (PropertyInfo?)null;
				foreach (var item in view.SourceCollection)
				{
					prop = prop != null && ty == item.GetType() ? prop : (ty = item.GetType()).GetProperty(DisplayMemberPath);
					var text = prop.GetValue(item).ToString();
					if (string.Compare(text, Text, !IsTextSearchCaseSensitive) != 0) continue;
					view.MoveCurrentTo(item);
					SelectedItem = item;
					break;
				}
			}
		}

		/// <summary>Raised to update the collection bound to 'ItemsSource'</summary>
		public event EventHandler? UpdateAutoCompleteList;
		private void NotifyUpdateAutoCompleteList()
		{
			// When the user types, set CurrentItem to null
			if (ItemsSource is ICollectionView view)
				using (PreserveTextState())
					view.MoveCurrentTo(null);

			// Invoke the event after the completion of the current event
			Dispatcher.BeginInvoke(new Action(() =>
			{
				UpdateAutoCompleteList?.Invoke(this, EventArgs.Empty);
			}));
		}

		/// <summary>Save the state of the text box</summary>
		private Scope PreserveTextState()
		{
			var tb = this.EditableTextBox();
			return Scope.Create(
				() => new TextState(tb.Text, tb.SelectionStart, tb.SelectionLength),
				s =>
				{
					tb.Text = s.Text;
					tb.SelectionStart = s.SelectionStart;
					tb.SelectionLength = s.SelectionLength;
				});
		}
		private struct TextState
		{
			public TextState(string text, int selection_start, int selection_length)
			{
				Text = text;
				SelectionStart = selection_start;
				SelectionLength = selection_length;
			}
			public string Text;
			public int SelectionStart;
			public int SelectionLength;
		}
	}
}