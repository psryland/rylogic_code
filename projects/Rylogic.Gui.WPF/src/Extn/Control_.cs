using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace Rylogic.Gui.WPF
{
	public static class Control_
	{
		// Notes:
		//  - If you're trying to fix the binding errors for 'HorizontalContentAlignment' and 'VerticalContentAlignment'
		//    add this to your App.xaml:
		//    <Style TargetType="ComboBoxItem"|"MenuItem"|"TreeViewItem">
		//        <Setter Property = "HorizontalContentAlignment" Value="Left" />
		//        <Setter Property = "VerticalContentAlignment" Value="Center" />
		//    </Style>

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
			//  - The DataContext is null during construction, so we need to defer
			//    finding the command until the event is invoked.

			var evt2cmd = GetEventToCommand(obj) ?? string.Empty;
			var parts = evt2cmd.Split(':');
			if (parts.Length != 2)
			{
				Debug.WriteLine($"EventToCommand required syntax is 'EventName:CommandName'. '{evt2cmd}' is invalid");
				return; // Silent because of hot editing
			}

			// Locate the event on 'obj'
			var ty = obj.GetType();
			var evt = ty.GetEvent(parts[0]);
			if (evt == null)
			{
				Debug.WriteLine($"No event named '{parts[0]}' found on type {ty.Name}");
				return; // Silent because of hot editing
			}
			if (evt.EventHandlerType == null)
				return;

			// Attach an event handler.
			// Get the 'invoke' method on the event handler type to extract the parameter types.
			// Then, create an instance of the generic helper type so that we can supply an event
			// handler with the correct signature type.
			var invoke_mi = evt.EventHandlerType.GetMethod(nameof(EventHandler.Invoke));
			if (invoke_mi == null)
				return;

			var parms = invoke_mi.GetParameters().Select(x => x.ParameterType).ToArray();
			var fwd = Activator.CreateInstance(typeof(EventToCommandData<,>).MakeGenericType(parms), parts[1]);
			if (fwd == null)
				return;

			evt.AddEventHandler(obj, Delegate.CreateDelegate(evt.EventHandlerType, fwd, "Handler"));
		}
		private class EventToCommandData<TSender, TArgs>
		{
			private readonly string m_command_name;
			private ReflectedCommand? m_reflected_command;
			private object? m_data_context;
			public EventToCommandData(string command_name)
			{
				m_command_name = command_name;
				m_reflected_command = null;
				m_data_context = null;
			}
			public void Handler(TSender sender, TArgs args)
			{
				// The reflected command can only be created once the data context has been set
				if (m_reflected_command == null && sender is FrameworkElement elem && elem.DataContext != m_data_context)
				{
					m_data_context = elem.DataContext;
					m_reflected_command = new ReflectedCommand(m_data_context, m_command_name);
				}
				if (m_reflected_command != null && m_reflected_command.CanExecute(args))
				{
					m_reflected_command.Execute(args);
				}
			}
		}

		/// <summary>VerticalOffset property</summary>
		public const int VerticalOffset = 0;
		public static readonly DependencyProperty VerticalOffsetProperty = Gui_.DPRegisterAttached(typeof(Control_), nameof(VerticalOffset));
		public static double GetVerticalOffset(DependencyObject obj) => (double)obj.GetValue(VerticalOffsetProperty);
		public static void SetVerticalOffset(DependencyObject obj, double value) => obj.SetValue(VerticalOffsetProperty, value);
		private static void VerticalOffset_Changed(DependencyObject obj)
		{
			if (obj is ScrollViewer sv)
				sv.ScrollToVerticalOffset(GetVerticalOffset(obj));
			if (obj is TextBoxBase tb)
				tb.ScrollToVerticalOffset(GetVerticalOffset(obj));
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
