using System;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Rylogic.Utility;

namespace Rylogic.Gui.WPF
{
	public static class Control_
	{
		/// <summary>Foreground Attached property</summary>
		public const int Foreground = 0;
		public static readonly DependencyProperty ForegroundProperty = Gui_.DPRegisterAttached(typeof(Control_), nameof(Foreground));
		public static SolidColorBrush GetForeground(DependencyObject obj) => (SolidColorBrush)obj.GetValue(ForegroundProperty);
		public static void SetForeground(DependencyObject obj, SolidColorBrush value) => obj.SetValue(ForegroundProperty, value);
		private static void Foreground_Changed(DependencyObject obj)
		{
			foreach (var child in Gui_.AllVisualChildren(obj).OfType<Control>())
				child.Foreground = GetForeground(obj);
		}

		/// <summary>HorizontalContentAlignment Attached Property</summary>
		public const int HorizontalContentAlignment = 0;
		public static readonly DependencyProperty HorizontalContentAlignmentProperty = Gui_.DPRegisterAttached(typeof(Control_), nameof(HorizontalContentAlignment));
		public static HorizontalAlignment GetHorizontalContentAlignment(DependencyObject obj) => (HorizontalAlignment)obj.GetValue(HorizontalContentAlignmentProperty);
		public static void SetHorizontalContentAlignment(DependencyObject obj, SolidColorBrush value) => obj.SetValue(HorizontalContentAlignmentProperty, value);
		private static void HorizontalContentAlignment_Changed(DependencyObject obj)
		{
			foreach (var child in Gui_.AllVisualChildren(obj).OfType<Control>())
				child.HorizontalContentAlignment = GetHorizontalContentAlignment(obj);
		}

		/// <summary>VerticalContentAlignment Attached Property</summary>
		public const int VerticalContentAlignment = 0;
		public static readonly DependencyProperty VerticalContentAlignmentProperty = Gui_.DPRegisterAttached(typeof(Control_), nameof(VerticalContentAlignment));
		public static VerticalAlignment GetVerticalContentAlignment(DependencyObject obj) => (VerticalAlignment)obj.GetValue(VerticalContentAlignmentProperty);
		public static void SetVerticalContentAlignment(DependencyObject obj, SolidColorBrush value) => obj.SetValue(VerticalContentAlignmentProperty, value);
		private static void VerticalContentAlignment_Changed(DependencyObject obj)
		{
			foreach (var child in Gui_.AllVisualChildren(obj).OfType<Control>())
				child.VerticalContentAlignment = GetVerticalContentAlignment(obj);
		}

		/// <summary>EventToCommand Attached Property</summary>
		public const int EventToCommand = 0;
		public static readonly DependencyProperty EventToCommandProperty = Gui_.DPRegisterAttached(typeof(Control_), nameof(EventToCommand));
		public static string GetEventToCommand(DependencyObject obj) => (string)obj.GetValue(EventToCommandProperty);
		public static void SetEventToCommand(DependencyObject obj, string value) => obj.SetValue(EventToCommandProperty, value);
		private static void EventToCommand_Changed(DependencyObject obj)
		{
			// Use:
			//   <Button
			//      gui:Control_.EventToCommand="PreviewMouseDoubleClick:MyCommand"
			//      />
			// Notes:
			//  - The DataContext is null during construction, so we need to defer finding the command
			//    until the event is invoked.

			var evt2cmd = GetEventToCommand(obj) ?? string.Empty;
			var parts = evt2cmd.Split(':');
			if (parts.Length != 2)
				throw new FormatException("EventToCommand required syntax is 'EventName:CommandName'");

			// Locate the event on 'obj'
			var ty = obj.GetType();
			var evt = ty.GetEvent(parts[0]);
			if (evt == null)
				throw new Exception($"No event named '{parts[0]}' found on type {ty.Name}");

			ReflectedCommand? reflected_command = null;
			
			// Attach an event handler.
			// I think the only alternative to this is a dynamic assembly, feel free to try...
			var handler =
				evt.EventHandlerType == typeof(EventHandler)            ? (Delegate)new EventHandler(ForwardEventToCommand) :
				evt.EventHandlerType == typeof(MouseButtonEventHandler) ? (Delegate)new MouseButtonEventHandler(ForwardEventToCommand) :
				throw new Exception("Unsupported event handler type. Please extend");

			evt.AddEventHandler(obj, handler);
			void ForwardEventToCommand<TArgs>(object sender, TArgs args) where TArgs:EventArgs
			{
				if (reflected_command == null && sender is FrameworkElement elem && elem.DataContext != null)
					reflected_command = new ReflectedCommand(elem.DataContext, parts[1]);
				if (reflected_command != null && reflected_command.CanExecute(args))
					reflected_command.Execute(args);
			}
		}

		/// <summary>Find a context menu resource and set it's data context to the given object</summary>
		public static ContextMenu FindCMenu(this FrameworkElement fe, string resource_key, object? data_context = null)
		{
			var cmenu = (ContextMenu)fe.FindResource(resource_key);
			if (data_context != null) cmenu.DataContext = data_context;
			return cmenu;
		}
	}
}
