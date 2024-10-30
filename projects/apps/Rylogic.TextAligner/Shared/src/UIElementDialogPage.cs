using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms.Integration;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Microsoft.VisualStudio.Shell;
using Rylogic.Interop.Win32;

namespace Rylogic.TextAligner
{
	[ComVisible(true)]
	public abstract class UIElementDialogPage :DialogPage
	{
		// Credit to: https://github.com/dwmkerr/switch/
		// Notes:
		//  - Host WPF content inside a native dialog running an IsDialogMessage-style message loop.
		//    UIElementDialogPage enables tabbing into and out of the WPF child HWND, and enables
		//    keyboard navigation within the WPF child HWND.

		static UIElementDialogPage()
		{
			// Common controls that require centralized handling should have handlers here.
			EventManager.RegisterClassHandler(typeof(ComboBox), DialogKeyPendingEvent, (EventHandler<DialogKeyEventArgs>)HandleComboBoxDialogKey);
			EventManager.RegisterClassHandler(typeof(DatePicker), DialogKeyPendingEvent, (EventHandler<DialogKeyEventArgs>)HandleDatePickerDialogKey);
			
			// Handlers
			void HandleComboBoxDialogKey(object sender, DialogKeyEventArgs e)
			{
				// If the ComboBox is dropped down and Enter or Escape are pressed, we should
				// cancel or commit the selection change rather than allowing the default button
				// or cancel button to be invoked.
				ComboBox comboBox = (ComboBox)sender;
				if ((e.Key == Key.Enter || e.Key == Key.Escape) && comboBox.IsDropDownOpen)
				{
					e.Handled = true;
				}
			}
			void HandleDatePickerDialogKey(object sender, DialogKeyEventArgs e)
			{
				// If the DatePicker is dropped down and Enter or Escape are pressed, we should
				// cancel or commit the selection change rather than allowing the default button
				// or cancel button to be invoked.
				DatePicker datePicker = (DatePicker)sender;
				if ((e.Key == Key.Enter || e.Key == Key.Escape) && datePicker.IsDropDownOpen)
				{
					e.Handled = true;
				}
			}
		}

		/// <summary>
		/// Routed event used to determine whether or not key input in the dialog should be handled by the dialog or by
		/// the content of this page.  If this event is marked as handled, the key press should be handled by the content,
		/// and DLGC_WANTALLKEYS will be returned from WM_GETDLGCODE.  If the event is not handled, then only arrow keys,
		/// tabbing, and character input will be handled within this dialog page.</summary>
		public static readonly RoutedEvent DialogKeyPendingEvent = EventManager.RegisterRoutedEvent("DialogKeyPending", RoutingStrategy.Bubble, typeof(EventHandler<DialogKeyEventArgs>), typeof(UIElementDialogPage));

		/// <inheritdoc/>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		protected override System.Windows.Forms.IWin32Window Window
		{
			get
			{
				if (m_element_host == null)
				{
					m_element_host = new DialogPageElementHost();
					m_element_host.Dock = System.Windows.Forms.DockStyle.Fill;

					var child = CreateChild();
					if (child != null)
					{
						// The child is the root of a visual tree, so it has no parent from whom to 
						// inherit its TextFormattingMode; set it appropriately.
						// NOTE: We're setting this value on an element we didn't create; we should consider
						// creating a wrapping ContentPresenter to nest the external Visual in.
						TextOptions.SetTextFormattingMode(child, TextFormattingMode.Display);

						HookChildHwndSource(child);
						m_element_host.Child = child;
					}
				}

				return m_element_host;
			}
		}
		private ElementHost? m_element_host;

		/// <summary>Gets the WPF child element to be hosted inside the dialog page.</summary>
		protected abstract UIElement CreateChild();

		/// <summary>
		/// Observes for HwndSource changes on the given UIElement, and adds and removes an HwndSource hook when the HwndSource changes.</summary>
		private void HookChildHwndSource(UIElement child)
		{
			// The delegate reference is stored on the UIElement, and the lifetime
			// of the child is equal to the lifetime of this UIElementDialogPage,
			// so we are not leaking memory by not calling RemoveSourceChangedHandler.
			PresentationSource.AddSourceChangedHandler(child, OnSourceChanged);
			void OnSourceChanged(object sender, SourceChangedEventArgs e)
			{
				if (e.OldSource is HwndSource old_source)
					old_source.RemoveHook(SourceHook);
				if (e.NewSource is HwndSource new_source)
					new_source.AddHook(SourceHook);
			}
			IntPtr SourceHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
			{
				// Handle WM_GETDLGCODE in order to allow for arrow and tab navigation inside the dialog page.
				// By returning this code, Windows will pass arrow and tab keys to our HWND instead of handling
				// them for its own default tab and directional navigation.
				switch (msg)
				{
				case Win32.WM_GETDLGCODE:
					{
						int dlg_code = Win32.DLGC_WANTARROWS | Win32.DLGC_WANTTAB | Win32.DLGC_WANTCHARS;

						// Ask the currently-focused element if it wants to handle all keys or not.  The DialogKeyPendingEvent
						// is a routed event starting with the focused control.  If any control in the route handles
						// this message, then we'll add DLGC_WANTALLKEYS to request that this pending message
						// be delivered to our content instead of the default dialog procedure.
						if (Keyboard.FocusedElement is IInputElement current_element)
						{
							var args = new DialogKeyEventArgs(DialogKeyPendingEvent, KeyInterop.KeyFromVirtualKey(wParam.ToInt32()));
							current_element.RaiseEvent(args);
							if (args.Handled)
							{
								dlg_code |= Win32.DLGC_WANTALLKEYS;
							}
						}

						handled = true;
						return new IntPtr(dlg_code);
					}
				}

				return IntPtr.Zero;
			}
		}

		/// <summary>Specialised ElementHost for dialogs</summary>
		private class DialogPageElementHost :ElementHost
		{
			// Note:
			//  - Subclass of ElementHost designed to work around focus problems with ElementHost.

			protected override void WndProc(ref System.Windows.Forms.Message m)
			{
				base.WndProc(ref m);

				if (m.Msg == Win32.WM_SETFOCUS)
				{
					var old_handle = m.WParam;

					// Get the handle to the child WPF element that we are hosting
					// After that get the next and previous items that would fall before 
					// and after the WPF control in the tools->options page tabbing order
					if (PresentationSource.FromVisual(Child) is HwndSource source && old_handle != IntPtr.Zero)
					{
						var next_tab_element = GetNextFocusElement(source.Handle, forward: true);
						var prev_tab_element = GetNextFocusElement(source.Handle, forward: false);

						var root_element = source.RootVisual as UIElement;

						// If we tabbed back from the next element then set focus to the last item
						if (root_element != null && next_tab_element == old_handle)
						{
							root_element.MoveFocus(new TraversalRequest(FocusNavigationDirection.Last));
						}

						// If we tabbed in from the previous element then set focus to the first item
						else if (root_element != null && prev_tab_element == old_handle)
						{
							root_element.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
						}
					}
				}
			}
			protected override void OnHandleCreated(EventArgs e)
			{
				base.OnHandleCreated(e);

				// Set up an IKeyboardInputSite that understands how to tab outside the WPF content.
				// (see the notes on DialogKeyboardInputSite for more detail).
				// NOTE: This should be done after calling base.OnHandleCreated, which is where
				// ElementHost sets up its own IKeyboardInputSite.
				if (PresentationSource.FromVisual(Child) is HwndSource source)
					((IKeyboardInputSink)source).KeyboardInputSite = new DialogKeyboardInputSite(source);
			}

			// From a given handle get the next focus element either forward or backward
			internal static IntPtr GetNextFocusElement(IntPtr handle, bool forward)
			{
				var hDlg = User32.GetAncestor(handle, Win32.GA_ROOT);
				if (hDlg != IntPtr.Zero)
				{
					// Find the next dialog item in the parent dialog (searching in the correct direction)
					// This can return IntPtr.Zero if there are no more items in that direction
					return User32.GetNextDlgTabItem(hDlg, handle, !forward);
				}
				return IntPtr.Zero;
			}
		}
		private class DialogKeyboardInputSite :IKeyboardInputSite
		{
			// Notes:
			//  - The default IKeyboardInputSite that ElementHost uses relies on being hosted
			//    in a pure Windows Forms window for tabbing outside the ElementHost's WPF content.
			//    However, this DialogPageElementHost is hosted inside a Win32 dialog, and should
			//    rely on the Win32 navigation logic directly. This replaces the default IKeyboardInputSite
			//    with one that has specialized handling for OnNoMoreTabStops.

			private readonly HwndSource m_source;
			public DialogKeyboardInputSite(HwndSource source)
			{
				m_source = source;
			}

			/// <summary>Gets the IKeyboardInputSink associated with this site.</summary>
			public IKeyboardInputSink Sink => m_source;

			/// <summary></summary>
			public void Unregister()
			{
				// We have nothing to unregister, so do nothing.
			}

			/// <summary></summary>
			public bool OnNoMoreTabStops(TraversalRequest request)
			{
				// First, determine if we are tabbing forward or backwards outside of our content.
				bool forward = true;
				if (request != null)
				{
					switch (request.FocusNavigationDirection)
					{
					case FocusNavigationDirection.Next:
					case FocusNavigationDirection.Right:
					case FocusNavigationDirection.Down:
						forward = true;
						break;

					case FocusNavigationDirection.Previous:
					case FocusNavigationDirection.Left:
					case FocusNavigationDirection.Up:
						forward = false;
						break;
					}
				}

				// Based on the direction, tab forward or backwards in our parent dialog.
				var next_handle = DialogPageElementHost.GetNextFocusElement(m_source.Handle, forward);
				if (next_handle != IntPtr.Zero)
				{
					// If we were able to find another control, send focus to it and inform
					// WPF that we moved focus outside the HwndSource.
					User32.SetFocus(next_handle);
					return true;
				}

				// If we couldn't find a dialog item to focus, inform WPF that it should
				// continue cycling inside its own tab order.
				return false;
			}
		}
		public class DialogKeyEventArgs :RoutedEventArgs
		{
			// Notes:
			// - Event args used by UIElementDialogPage.DialogKeyPendingEvent

			public DialogKeyEventArgs(RoutedEvent evt, Key key)
				:base(evt)
			{
				Key = key;
			}

			/// <summary>Gets the key being pressed within the UIElementDialogPage.</summary>
			public Key Key { get; }
		}
	}
}