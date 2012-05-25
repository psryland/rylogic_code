using System.Windows.Forms;
using pr.gui;

namespace pr
{
	// A graph window
	public partial class Plot :Form
	{
		public Plot()
		{
			InitializeComponent();
			FormClosing += delegate(object sender, FormClosingEventArgs e) { if (e.CloseReason == CloseReason.UserClosing) {Hide(); e.Cancel = true;} };
		}
		public GraphControl Graph
		{
			get { return m_plot; }
		}
	}
}
