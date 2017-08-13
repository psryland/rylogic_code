using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using pr.container;
using pr.extn;
using pr.gui;

namespace CoinFlip
{
	public class GridArbitrage :GridBase
	{
		public GridArbitrage(Model model, string title, string name)
			:base(model, title, name)
		{
			ReadOnly = true;
			Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Pair",
				Name = nameof(ColumnNames.Pair),
			});
			ContextMenuStrip = CreateCMenu();

			Exchanges = Model.Exchanges;
		}
		protected override void Dispose(bool disposing)
		{
			Exchanges = null;
			base.Dispose(disposing);
		}
		protected override void OnCellPainting(DataGridViewCellPaintingEventArgs e)
		{
			base.OnCellPainting(e);
		}

		/// <summary>The exchanges to display</summary>
		private BindingSource<Exchange> Exchanges
		{
			get { return m_exchanges; }
			set
			{
				if (m_exchanges == value) return;
				if (m_exchanges != null)
				{
					m_exchanges.ListChanging -= HandleExchangesChanging;
				}
				m_exchanges = value;
				if (m_exchanges != null)
				{
					m_exchanges.ListChanging += HandleExchangesChanging;
				}
			}
		}
		private BindingSource<Exchange> m_exchanges;
		private void HandleExchangesChanging(object sender, ListChgEventArgs<Exchange> e)
		{
			// Update the columns in the grid, one for each exchange
			if (!e.IsDataChanged) return;

			// Remove old columns
			for (;ColumnCount > 1;)
				Columns.RemoveAt(1);

			// Add new columns
			foreach (var exch in Exchanges.Except(Model.CrossExchange))
			{
				Columns.Add(new DataGridViewTextBoxColumn
				{
					HeaderText = exch.Name,
					Name = exch.Name,
				});
			}
		}

		/// <summary>Create a context menu for the grid</summary>
		private ContextMenuStrip CreateCMenu()
		{
			var cmenu = new ContextMenuStrip();
			{
				var opt = cmenu.Items.Add2(new ToolStripMenuItem("Add Pair"));
				opt.Click += (s,a) =>
				{
					AddPair();
				};
			}
			return cmenu;
		}

		/// <summary>Add a pair to the grid</summary>
		private void AddPair()
		{
			using (var dlg = new PromptUI{ Title = "Currency Pair", PromptText = "Select the currency pair to add" })
			{

			}
		}

		/// <summary>Column names</summary>
		private static class ColumnNames
		{
			public const int Pair = 0;
		}
	}
}
