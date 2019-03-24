using System;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Data;
using Rylogic.Gui.WPF;
using Rylogic.Utility;

namespace CoinFlip.UI
{
	public partial class GridTradeOrders : DataGrid, IDockable, IDisposable, INotifyPropertyChanged
	{
		public GridTradeOrders(Model model)
		{
			InitializeComponent();
			MouseRightButtonUp += DataGrid_.ColumnVisibility;
			DockControl = new DockControl(this, "Orders");
			Model = model;

			DataContext = this;
		}
		public void Dispose()
		{
			Model = null;
			DockControl = null;
		}

		/// <summary></summary>
		public Model Model
		{
			get { return m_model; }
			set
			{
				if (m_model == value) return;
				m_model = value;
				Exchanges = CollectionViewSource.GetDefaultView(m_model?.Exchanges);
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

		/// <summary>The global exchanges view. Provides the source of the "Current" exchange </summary>
		private ICollectionView Exchanges
		{
			get { return m_exchanges; }
			set
			{
				if (m_exchanges == value) return;
				if (m_exchanges != null)
				{
					m_exchanges.CurrentChanged -= HandleCurrentChanged;
				}
				m_exchanges = value;
				if (m_exchanges != null)
				{
					m_exchanges.CurrentChanged += HandleCurrentChanged;
				}
				HandleCurrentChanged(null, null);

				// Handler
				void HandleCurrentChanged(object sender, EventArgs e)
				{
					var orders = ((Exchange)Exchanges?.CurrentItem)?.Orders;
					Orders = orders != null ? new ListCollectionView(orders) : null;
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Orders)));
				}
			}
		}
		private ICollectionView m_exchanges;

		/// <summary>The view of the trade history</summary>
		public ICollectionView Orders { get; private set; }

		/// <summary>The currently selected exchange</summary>
		public OrderCompleted Current
		{
			get => (OrderCompleted)Orders?.CurrentItem;
			set => Orders.MoveCurrentTo(value);
		}

		/// <summary></summary>
		public event PropertyChangedEventHandler PropertyChanged;

	}
}
