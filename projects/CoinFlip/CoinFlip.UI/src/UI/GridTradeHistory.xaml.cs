using System;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Data;
using Rylogic.Gui.WPF;
using Rylogic.Utility;

namespace CoinFlip.UI
{
	public partial class GridTradeHistory : DataGrid, IDockable, IDisposable, INotifyPropertyChanged
	{
		// Notes:
		//   - Historic trades are instances of 'OrderCompleted' classes. They contain
		//     one or more 'TradeCompleted' classes.

		public GridTradeHistory(Model model)
		{
			InitializeComponent();
			MouseRightButtonUp += DataGrid_.ColumnVisibility;
			DockControl = new DockControl(this, "History");
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
					var history = ((Exchange)Exchanges?.CurrentItem)?.History;
					History = history != null ? new ListCollectionView(history) : null;
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(History)));
				}
			}
		}
		private ICollectionView m_exchanges;

		/// <summary>The view of the trade history</summary>
		public ICollectionView History { get; private set; }

		/// <summary>The currently selected exchange</summary>
		public OrderCompleted Current
		{
			get => (OrderCompleted)History?.CurrentItem;
			set => History.MoveCurrentTo(value);
		}

		/// <summary></summary>
		public event PropertyChangedEventHandler PropertyChanged;
	}
}
