using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CoinFlip.UI
{
	public partial class FundAllocationsUI : Window, IDisposable
	{
		public FundAllocationsUI(Window owner, Model model)
		{
			InitializeComponent();
			Icon = owner?.Icon;
			Owner = owner;
			Model = model;

			Funds = new ListCollectionView(model.Funds);
			Closed += delegate { Dispose(); };

			DataContext = this;
		}
		public void Dispose()
		{
			Model = null;
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
					//m_model.Exchanges.CollectionChanged -= HandleExchangesChanged;
					//m_model.Coins.CollectionChanged -= HandleCoinsChanged;
					//m_model.Funds.CollectionChanged -= HandleFundsChanged;
				}
				m_model = value;
				if (m_model != null)
				{
					//m_model.Funds.CollectionChanged += HandleFundsChanged;
					//m_model.Coins.CollectionChanged += HandleCoinsChanged;
					//m_model.Exchanges.CollectionChanged += HandleExchangesChanged;
				}

				//// Handle funds being created or destroyed
				//void HandleFundsChanged(object sender, NotifyCollectionChangedEventArgs e)
				//{
				//	CreateFundColumns();
				//}
				//void HandleCoinsChanged(object sender, NotifyCollectionChangedEventArgs e)
				//{
				//	Coins.Refresh();
				//}
				//void HandleExchangesChanged(object sender, NotifyCollectionChangedEventArgs e)
				//{
				//	var list = (List<Exchange>)Exchanges.SourceCollection;
				//	using (Scope.Create(() => Exchanges.CurrentItem, ci => Exchanges.MoveCurrentTo(ci)))
				//		list.Assign(Model.TradingExchanges);

				//	Exchanges.Refresh();
				//	Coins.Refresh();
				//}
			}
		}
		private Model m_model;

		/// <summary>The available funds</summary>
		public ICollectionView Funds { get; }
	}
}
