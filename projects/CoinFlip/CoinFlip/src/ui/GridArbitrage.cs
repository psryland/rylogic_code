using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using Rylogic.Container;
using Rylogic.Extn;
using Rylogic.Utility;

namespace CoinFlip
{
	public class GridArbitrage :GridBase
	{
		public GridArbitrage(Model model, string title, string name)
			:base(model, title, name)
		{
			ReadOnly = true;
			DoubleBuffered = true;
			AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
			RowTemplate.Height = DefaultCellStyle.Font.Height * 2 + 4;
			Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Pair",
				Name = nameof(ColumnNames.Pair),
				DataPropertyName = nameof(ColumnNames.Pair),
			});
			ContextMenuStrip = CreateCMenu();
		}
		protected override void OnCellFormatting(DataGridViewCellFormattingEventArgs e)
		{
			DataGridView_.HalfBrightSelection(this, e);
			base.OnCellFormatting(e);
		}
		protected override void OnCellPainting(DataGridViewCellPaintingEventArgs e)
		{
			if (this.Within(e.ColumnIndex, e.RowIndex, out DataGridViewColumn col, out DataGridViewCell cell))
			{
				var parts = e.PaintParts;
				e.Graphics.SmoothingMode = SmoothingMode.HighQuality;

				// Paint the background
				e.PaintBackground(e.ClipBounds, e.State.HasFlag(DataGridViewElementStates.Selected));
				parts ^= DataGridViewPaintParts.Background | DataGridViewPaintParts.ContentBackground;

				var pair_name = ((IList<string>)DataSource)[e.RowIndex];

				// Paint the content foreground
				if (col.Name == nameof(ColumnNames.Pair))
				{
					// Pair name
					var sz = e.Graphics.MeasureString(pair_name, e.CellStyle.Font);
					e.Graphics.DrawString(pair_name, e.CellStyle.Font, Brushes.Black, e.CellBounds.Left, e.CellBounds.Top + (e.CellBounds.Height - sz.Height)*0.5f);
				}
				else
				{
					// Price on 'exch'
					var exch = Model.Exchanges.First(x => x.Name == col.Name);
					var pair = exch.Pairs.Values.FirstOrDefault(x => x.Name == pair_name);
					if (pair != null)
					{
						{// Q2B
							var q2b_spot = pair.SpotPrice(ETradeType.Q2B).ToString("G6");
							var sz = e.Graphics.MeasureString(q2b_spot, e.CellStyle.Font);
							e.Graphics.DrawString(q2b_spot, e.CellStyle.Font, Brushes.DarkGreen, e.CellBounds.Left + (e.CellBounds.Width - sz.Width)*0.5f, e.CellBounds.Top + (e.CellBounds.Height/2 - sz.Height)*0.5f);
						}
						{// B2Q
							var b2q_spot = pair.SpotPrice(ETradeType.B2Q).ToString("G6");
							var sz = e.Graphics.MeasureString(b2q_spot, e.CellStyle.Font);
							e.Graphics.DrawString(b2q_spot, e.CellStyle.Font, Brushes.DarkRed, e.CellBounds.Left + (e.CellBounds.Width - sz.Width)*0.5f, e.CellBounds.Top + e.CellBounds.Height/2 - (e.CellBounds.Height/2 - sz.Height)*0.5f);
						}
					}
				}
				parts ^= DataGridViewPaintParts.ContentForeground;

				// Paint the focus mark
				e.Paint(e.ClipBounds, parts);
				e.Handled = true;
			}
			base.OnCellPainting(e);
		}
		protected override void SetModelCore(Model model)
		{
			if (Model != null)
			{
				Model.Pairs.ListChanging -= HandlePairsListChanging;
				Model.Exchanges.ListChanging -= HandleExchangesChanging;
			}
			base.SetModelCore(model);
			if (Model != null)
			{
				Model.Pairs.ListChanging += HandlePairsListChanging;
				Model.Exchanges.ListChanging += HandleExchangesChanging;
			}

			// Update the pairs to display prices for
			void HandlePairsListChanging(object sender = null, ListChgEventArgs<TradePair> e = null)
			{
				DataSource = Model.Pairs
					.Where(x => !(x.Exchange is CrossExchange))
					.Select(x => x.Name)
					.Distinct()
					.OrderBy(x => x)
					.ToList();
			}

			// Update the exchanges to display
			void HandleExchangesChanging(object sender, ListChgEventArgs<Exchange> e)
			{
				// Update the columns in the grid, one for each exchange
				if (!e.IsDataChanged) return;

				// Remove old columns
				for (; ColumnCount > 1;)
					Columns.RemoveAt(1);

				// Add new columns
				foreach (var exch in Model.TradingExchanges)
				{
					Columns.Add(new DataGridViewTextBoxColumn
					{
						HeaderText = exch.Name,
						Name = exch.Name,
						DataPropertyName = exch.Name,
					});
				}
			}
		}

		/// <summary>Create a context menu for the grid</summary>
		private ContextMenuStrip CreateCMenu()
		{
			var cmenu = new ContextMenuStrip();
			{}
			return cmenu;
		}

		/// <summary>Column names</summary>
		private static class ColumnNames
		{
			public const int Pair = 0;
		}
	}
}
