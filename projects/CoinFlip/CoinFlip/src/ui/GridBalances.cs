using System.Drawing;
using System.Windows.Forms;
using pr.extn;

namespace CoinFlip
{
	public class GridBalances :GridBase
	{
		public GridBalances(Model model, string title, string name)
			:base(model, title, name)
		{
			ReadOnly = true;
			Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Currency",
				Name = nameof(Balance.Coin),
				DataPropertyName = nameof(Balance.Coin),
				SortMode = DataGridViewColumnSortMode.Automatic,
				FillWeight = 0.3f,
			});
			Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Available",
				Name = nameof(Balance.Available),
				DataPropertyName = nameof(Balance.Available),
				SortMode = DataGridViewColumnSortMode.Automatic,
				FillWeight = 0.6f,
			});
			Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Total",
				Name = nameof(Balance.Total),
				DataPropertyName = nameof(Balance.Total),
				SortMode = DataGridViewColumnSortMode.Automatic,
				FillWeight = 0.6f,
			});
			Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Value",
				Name = nameof(Balance.Value),
				DataPropertyName = nameof(Balance.Value),
				DefaultCellStyle = new DataGridViewCellStyle{ Format = "C" },
				SortMode = DataGridViewColumnSortMode.Automatic,
				FillWeight = 0.6f,
			});
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
	}
}
