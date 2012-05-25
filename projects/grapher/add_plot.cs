using System.Windows.Forms;

namespace pr
{
	// Dialog for creating a new plot window
	public partial class AddPlot :Form
	{
		public AddPlot(string title, string xlabel, string ylabel)
		{
			InitializeComponent();
			m_edit_plot_title.Text = title;
			m_edit_xlabel.Text = xlabel;
			m_edit_ylabel.Text = ylabel;
			m_edit_plot_title.SelectAll();
		}

		public Plot NewPlot()
		{
			Plot p = new Plot();
			p.Graph.SetLabels(m_edit_plot_title.Text, m_edit_xlabel.Text, m_edit_ylabel.Text);
			return p;
		}
	}
}
