using System;
using System.Windows;
using System.Windows.Input;

namespace RyLogViewer
{
	public class ShutdownCommand :ICommand
	{
		public void Execute(object _)
		{
			Application.Current.Shutdown(0);
		}
		public bool CanExecute(object _) => true;
		public event EventHandler CanExecuteChanged { add { } remove { } }
	}
}
