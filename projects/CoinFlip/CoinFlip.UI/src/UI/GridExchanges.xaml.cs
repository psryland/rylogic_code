using System.Windows.Controls;
using Rylogic.Gui.WPF;
using Rylogic.Utility;

namespace CoinFlip.UI
{
	public partial class GridExchanges : DataGrid, IDockable
	{
		public GridExchanges(Model model)
		{
			InitializeComponent();
			DockControl = new DockControl(this, "Exchanges");
			Model = model;
		}

		/// <summary>Logic</summary>
		public Model Model
		{
			get { return m_model; }
			set
			{
				if (m_model == value) return;
				if (m_model != null)
				{
					ItemsSource = null;
				}
				m_model = value;
				if (m_model != null)
				{
					ItemsSource = m_model.Exchanges;
				}
			}
		}
		private Model m_model;

		/// <summary>Provides support for the DockContainer</summary>
		public DockControl DockControl
		{
			get { return m_dock_control; }
			private set
			{
				if (m_dock_control == value) return;
				Util.Dispose(ref m_dock_control);
				m_dock_control = value;
			}
		}
		private DockControl m_dock_control;

	}
}
