using System;
using System.Diagnostics;
using System.Text;
using System.Windows;

namespace Rylogic.Gui.WPF
{
	// Notes:
	//  - Use this in the root xaml node:
	//    xmlns:diag="clr-namespace:System.Diagnostics;assembly=WindowsBase"
	//    diag:PresentationTraceSources.TraceLevel="High"
	//
	// Handy Tips:
	//  - Add a TextBlock like this to see what the binding source is:
	//    <TextBlock
	//        Text = "{Binding DataContext, Converter={conv:ToString}}"
	//        />
	//  - How to Fix:
	//     "System.Windows.Data Error: 4 : Cannot find source for binding...HorizontalContentAlignment/VerticalContentAlignment"
	//   Set the HorizontalContentAlignment and VerticalContentAlignment properties in your App.xaml
	//  e.g.
	//    // App.xaml
	//    <Application>
	//        <Application.Resources>
	//            <ResourceDictionary>
	//                <!-- Fix for Library binding bugs -->
	//                <Style TargetType="ComboBoxItem">
	//                    <Setter Property="HorizontalContentAlignment" Value="Left"/>
	//                    <Setter Property="VerticalContentAlignment" Value="Center"/>
	//                </Style>
	//                <Style TargetType="MenuItem">
	//                    <Setter Property="HorizontalContentAlignment" Value="Left"/>
	//                    <Setter Property="VerticalContentAlignment" Value="Center"/>
	//                </Style>
	//                <Style TargetType="TreeViewItem">
	//                    <Setter Property="HorizontalContentAlignment" Value="Left"/>
	//                    <Setter Property="VerticalContentAlignment" Value="Center"/>
	//                </Style>
	//                <Style TargetType = "ListViewItem">
	//                    <Setter Property="HorizontalContentAlignment" Value="Left"/>
	//                    <Setter Property="VerticalContentAlignment" Value="Center"/>
	//                </Style>
	//            </ResourceDictionary>
	//        </Application.Resources>
	//    </Application>

	public class BindingErrorTraceListener : DefaultTraceListener
	{
		// Notes:
		//   Credit: http://www.switchonthecode.com/tutorials/wpf-snippet-detecting-binding-errors
		//   Lifted from StackOverflow
		// 
		// Use:
		//	public partial class Window1 : Window
		//	{
		//		public Window1()
		//		{
		//			BindingErrorTraceListener.SetTrace();
		//			InitializeComponent();
		//		}
		//	}

		private BindingErrorTraceListener()
		{}

		/// <summary></summary>
		public static void SetTrace()
		{
			SetTrace(SourceLevels.Error, TraceOptions.None);
		}
		public static void SetTrace(SourceLevels level, TraceOptions options)
		{
			if (m_listener == null)
			{
				m_listener = new BindingErrorTraceListener();
				PresentationTraceSources.DataBindingSource.Listeners.Add(m_listener);
			}
			m_listener.TraceOutputOptions = options;
			PresentationTraceSources.DataBindingSource.Switch.Level = level;
		}
		public static void CloseTrace()
		{
			if (m_listener == null)
				return;

			m_listener.Flush();
			m_listener.Close();
			PresentationTraceSources.DataBindingSource.Listeners.Remove(m_listener);
			m_listener = null;
		}
		private static BindingErrorTraceListener? m_listener;
		private StringBuilder m_message = new StringBuilder();

		/// <summary></summary>
		public override void Write(string message)
		{
			m_message.Append(message);
		}
		public override void WriteLine(string message)
		{
			var msg = m_message.Append(message).ToString();
			m_message.Length = 0;
			MessageBox.Show(msg, "Binding Error", MessageBoxButton.OK, MessageBoxImage.Error);
		}
	}

	/// <summary></summary>
	public class OnBindingError :TraceListener
	{
		// Notes:
		//   Credit: https://thecolorofcode.com/2018/01/30/how-to-never-miss-a-wpf-binding-error-again/
		// 
		// Use:
		//	public partial class Window1 : Window
		//	{
		//		public Window1()
		//		{
		//			new OnBindingError(msg => Debugger.Break());
		//			InitializeComponent();
		//		}
		//	}

		private readonly Action<string> m_error_handler;

		public OnBindingError(Action<string> error_handler)
		{
			m_error_handler = error_handler;

			var binding_trace = PresentationTraceSources.DataBindingSource;
			binding_trace.Listeners.Add(this);
			binding_trace.Switch.Level = SourceLevels.Information;
		}
		public override void WriteLine(string message)
		{
			m_error_handler?.Invoke(message);
		}
		public override void Write(string message)
		{
		}
	}
}
