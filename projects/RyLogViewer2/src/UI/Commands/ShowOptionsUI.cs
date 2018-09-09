using System;
using System.Windows.Input;
using RyLogViewer.Options;

namespace RyLogViewer
{
	public class ShowOptionsUI : ICommand
	{
		private readonly OptionsUI m_ui;
		public ShowOptionsUI(MainWindow ui, Main main, Settings settings, IReport report)
		{
			m_ui = new OptionsUI(main, settings, report) { Owner = ui};
		}
		public void Execute(object show_page)
		{
			m_ui.SelectedPage = show_page != null ? (EOptionsPage)show_page : EOptionsPage.General;
			m_ui.Show();
			m_ui.Focus();
		}
		public bool CanExecute(object parameter) => true;
		public event EventHandler CanExecuteChanged { add { } remove { } }
	}
}
