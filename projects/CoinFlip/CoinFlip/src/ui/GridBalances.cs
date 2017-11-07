using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using pr.extn;
using pr.gui;

namespace CoinFlip
{
	public class GridBalances :GridBase
	{
		public GridBalances(Model model, string title, string name)
			:base(model, title, name)
		{
			Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Currency",
				Name = nameof(Balance.Coin),
				DataPropertyName = nameof(Balance.Coin),
				SortMode = DataGridViewColumnSortMode.Automatic,
				FillWeight = 0.6383387f,
			});
			Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Total",
				Name = nameof(Balance.Total),
				DataPropertyName = nameof(Balance.Total),
				SortMode = DataGridViewColumnSortMode.Automatic,
				FillWeight = 0.6337074f,
			});
			Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Available",
				Name = nameof(Balance.Available),
				DataPropertyName = nameof(Balance.Available),
				SortMode = DataGridViewColumnSortMode.Automatic,
				FillWeight = 0.5407636f,
			});
			Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Value",
				Name = nameof(Balance.Value),
				DataPropertyName = nameof(Balance.Value),
				DefaultCellStyle = new DataGridViewCellStyle{ Format = "C" },
				SortMode = DataGridViewColumnSortMode.Automatic,
				FillWeight = 0.4985165f,
			});
			Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Auto Trade Limit",
				Name = nameof(Balance.AutoTradeLimit),
				DataPropertyName = nameof(Balance.AutoTradeLimit),
				SortMode = DataGridViewColumnSortMode.Automatic,
				FillWeight = 0.3886738f,
			});
			ContextMenuStrip = CreateCMenu();
			DataSource = Model.Balances;
		}
		protected override void OnCellFormatting(DataGridViewCellFormattingEventArgs a)
		{
			base.OnCellFormatting(a);
			var col = Columns[a.ColumnIndex];
			if (Model.Exchanges.Current is CrossExchange && col.Name == nameof(Balance.Coin))
			{
				a.Value = Model.Balances[a.RowIndex].Coin.SymbolWithExchange;
				a.FormattingApplied = true;
			}
			a.CellStyle.SelectionBackColor = a.CellStyle.BackColor.Lerp(Color.Gray, 0.5f);
			a.CellStyle.SelectionForeColor = a.CellStyle.ForeColor;
		}
		private ContextMenuStrip CreateCMenu()
		{
			var cmenu = new ContextMenuStrip();
			{
				var opt = cmenu.Items.Add2(new ToolStripMenuItem("Add \"Fake\" Cash"));
				cmenu.Opening += (s,a) =>
				{
					opt.Visible = Model.AllowTrades == false;
					opt.Enabled = SelectedRows.Count == 1;
				};
				opt.Click += (s,a) =>
				{
					var bal = SelectedRows.Cast<DataGridViewRow>().Select(x => (Balance)x.DataBoundItem).First();
					using (var dlg = new PromptUI { Title = "Add Fake Cash!", PromptText = "Add additional funds for testing", InputType = PromptUI.EInputType.Number, ValueType = typeof(decimal) })
					{
						if (dlg.ShowDialog(this) != DialogResult.OK) return;
						bal.FakeCash.Add((decimal)dlg.Value);
					}
				};
			}
			return cmenu;
		}
	}
}
