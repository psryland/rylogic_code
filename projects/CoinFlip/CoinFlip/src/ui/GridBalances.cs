using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using pr.container;
using pr.extn;
using pr.gui;
using pr.maths;
using pr.util;

namespace CoinFlip
{
	public class GridBalances :GridBase
	{
		public GridBalances(Model model, string title, string name)
			:base(model, title, name)
		{
			ReadOnly = true;
			DoubleBuffered = true;
			AllowUserToOrderColumns = false;
			AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
			RowTemplate.Height = DefaultCellStyle.Font.Height * 2 + 4;
			BalanceDisplayMode = new Dictionary<string, EDisplay>();

			// Maintain a sorted view of the balances
			Balances = new BindingListEx<Balances>();

			// Add columns for each fund
			UpdateColumns();
		}
		protected override void OnCellFormatting(DataGridViewCellFormattingEventArgs e)
		{
			if (this.Within(e.ColumnIndex, e.RowIndex, out DataGridViewColumn col, out DataGridViewCell cell))
			{
				if (e.ColumnIndex == (int)ECol.CurrencyColumn)
				{
					// If the cross exchange is selected, display the full name of the currency
					e.Value = Model.Exchanges.Current is CrossExchange
						? Balances[e.RowIndex].Coin.SymbolWithExchange
						: Balances[e.RowIndex].Coin.Symbol;
					e.FormattingApplied = true;
				}
			}
			DataGridView_.HalfBrightSelection(this, e);
			base.OnCellFormatting(e);
		}
		protected override void OnCellPainting(DataGridViewCellPaintingEventArgs e)
		{
			if (this.Within(e.ColumnIndex, e.RowIndex, out DataGridViewColumn col, out DataGridViewCell cell))
			{
				// Paint 'Available' over 'Total' for each currency in each fund
				var parts = e.PaintParts;
				e.Graphics.SmoothingMode = SmoothingMode.HighQuality;

				// Paint the background
				e.PaintBackground(e.ClipBounds, e.State.HasFlag(DataGridViewElementStates.Selected));
				parts ^= DataGridViewPaintParts.Background | DataGridViewPaintParts.ContentBackground;

				// Paint the currency column as normal
				if (col.Name == nameof(ECol.CurrencyColumn))
				{
					e.PaintContent(e.ClipBounds);
					{
						var str = "Total:";
						var sz = e.Graphics.MeasureString(str, e.CellStyle.Font);
						e.Graphics.DrawString(str, e.CellStyle.Font, Brushes.DarkGreen, e.CellBounds.Right - sz.Width - 4, e.CellBounds.Top + (e.CellBounds.Height/2 - sz.Height)*0.5f);
					}
					{
						var str = "Avail:";
						var sz = e.Graphics.MeasureString(str, e.CellStyle.Font);
						e.Graphics.DrawString(str, e.CellStyle.Font, Brushes.DarkBlue, e.CellBounds.Right - sz.Width - 4, e.CellBounds.Top + e.CellBounds.Height/2 - (e.CellBounds.Height/2 - sz.Height)*0.5f);
					}
				}

				// Otherwise, find the fund balance for the cell
				else
				{
					var bal = Balances[e.RowIndex][col.Name];
					var mode = BalanceDisplayMode.GetOrAdd(col.Name);

					// Paint the available and total balances
					{
						var str = 
							mode == EDisplay.Tokens ? bal.Total.ToString("F8", false) :
							mode == EDisplay.Values ? $"{bal.Coin.ValueOf(bal.Total).ToString("C", false)}" :
							throw new Exception($"Unknown balance display mode: {mode}");

						var sz = e.Graphics.MeasureString(str, e.CellStyle.Font);
						if (bal.Total < 0) e.Graphics.FillRectangle(Brushes.Red, e.CellBounds);
						e.Graphics.DrawString(str, e.CellStyle.Font, Brushes.DarkGreen, e.CellBounds.Left + 4, e.CellBounds.Top + (e.CellBounds.Height/2 - sz.Height)*0.5f);
					}
					{
						var str =
							mode == EDisplay.Tokens ? bal.Available.ToString("F8", false) :
							mode == EDisplay.Values ? $"{bal.Coin.ValueOf(bal.Available).ToString("C", false)}" :
							throw new Exception($"Unknown balance display mode: {mode}");

						var sz = e.Graphics.MeasureString(str, e.CellStyle.Font);
						if (bal.Available < 0) e.Graphics.FillRectangle(Brushes.Red, e.CellBounds);
						e.Graphics.DrawString(str, e.CellStyle.Font, Brushes.DarkBlue, e.CellBounds.Left + 4, e.CellBounds.Top + e.CellBounds.Height/2 - (e.CellBounds.Height/2 - sz.Height)*0.5f);
					}
				}
				parts ^= DataGridViewPaintParts.ContentForeground;

				// Paint the focus mark
				e.Paint(e.ClipBounds, parts);
				e.Handled = true;
			}
			base.OnCellPainting(e);
		}
		protected override void OnColumnHeaderMouseClick(DataGridViewCellMouseEventArgs e)
		{
			// Click on header to toggle from tokens to live value
			if (e.Button == MouseButtons.Left && e.ColumnIndex.Within(1, ColumnCount))
			{
				var col = Columns[e.ColumnIndex];
				BalanceDisplayMode[col.Name] = Enum<EDisplay>.Cycle(BalanceDisplayMode.GetOrAdd(col.Name));
			}

			base.OnColumnHeaderMouseClick(e);
		}
		protected override void OnMouseClick(MouseEventArgs e)
		{
			base.OnMouseClick(e);
			
			if (e.Button == MouseButtons.Right)
			{
				var hit = this.HitTestEx(e.Location);
				switch (hit.Type)
				{
				case DataGridView_.HitTestInfo.EType.ColumnHeader:
					{
						var cmenu = new ContextMenuStrip();
						{// Add a new user created fund
							var opt = cmenu.Items.Add2(new ToolStripMenuItem("Add Fund"));
							opt.Click += (s,a) =>
							{
								Model.ShowAddFundUI();
							};
						}
						{// Delete a fund
							var opt = cmenu.Items.Add2(new ToolStripMenuItem("Remove Fund"));
							opt.Enabled = hit.ColumnIndex > (int)ECol.MainFund;
							opt.Click += (s,a) =>
							{
								var id = hit.Column.Name;
								Model.Funds.RemoveIf(x => x.Id == id);
							};
						}
						cmenu.Show(this, e.Location);
						break;
					}
				case DataGridView_.HitTestInfo.EType.Cell:
					{
						break;
					}
				}
			}
		}
		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);

			var hit = this.HitTestEx(e.Location);
			if (hit.Type == DataGridView_.HitTestInfo.EType.Cell &&
				hit.RowIndex.Within(0, Balances.Count) &&
				hit.ColumnIndex > (int)ECol.MainFund)
			{
				m_drag_funds = new DragFunds(hit);
				Capture = true;
			}
		}
		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);
			if (m_drag_funds != null)
				m_drag_funds.Do(e.Location);
		}
		protected override void OnMouseUp(MouseEventArgs e)
		{
			base.OnMouseUp(e);
			if (m_drag_funds != null)
			{
				Model.Settings.Save();
				m_drag_funds = null;
				Capture = false;
			}
		}
		protected override void OnCellDoubleClick(DataGridViewCellEventArgs e)
		{
			base.OnCellDoubleClick(e);
			if (this.Within(e.ColumnIndex, e.RowIndex, out DataGridViewColumn col))
			{
				// If the user double clicks on a fund cell, allow them to set the total
				// balance for that currency in the selected fund.
				if (e.ColumnIndex > (int)ECol.MainFund)
				{
					var bal = Balances[e.RowIndex];
					var main = bal[Fund.Main];
					var fund = bal[col.Name];
					using (var dlg = new PromptUI { Title = "Assign Funds", PromptText = $"Set the amount to allocate to {fund.ToString()}" })
					{
						// The amount is moved from the main fund, so the limit is the sum of 'main.Total + fund.Total'
						var limit = main.Total + fund.Total;
						dlg.ValueType = typeof(decimal);
						dlg.Value = (decimal)fund.Total;
						dlg.ValidateValue = t => decimal.TryParse(t, out var v) && v.WithinInclusive(0m, limit);
						if (dlg.ShowDialog(Model.UI) != DialogResult.OK)
							return;

						// Transfer funds between 'main' and 'fund'
						var amount = ((decimal)dlg.Value)._(bal.Coin);
						fund.Update(Model.UtcNow, total: amount);
						main.Update(Model.UtcNow, total: limit - amount);
					}
				}
			}
		}
		protected override void OnPaint(PaintEventArgs e)
		{
			// Sort before every paint so that most valuable balances are at the top
			Balances?.Sort((l,r) => l.Coin.Order.CompareTo(r.Coin.Order));
			base.OnPaint(e);
		}
		protected override void SetModelCore(Model model)
		{
			if (Model != null)
			{
				Model.Balances.ListChanging -= HandleBalancesListChanging;
				Model.Funds.ListChanging    -= HandleFundsListChanging;
			}
			base.SetModelCore(model);
			if (Model != null)
			{
				Model.Funds.ListChanging    += HandleFundsListChanging;
				Model.Balances.ListChanging += HandleBalancesListChanging;
			}

			// Handlers
			void HandleFundsListChanging(object sender, ListChgEventArgs<Fund> e)
			{
				// Update the funds as the collection changes
				switch (e.ChangeType)
				{
				case ListChg.Reset:
				case ListChg.ItemAdded:
				case ListChg.ItemRemoved:
					{
						UpdateColumns();
						Invalidate();
						break;
					}
				}
			}
			void HandleBalancesListChanging(object sender, ListChgEventArgs<Balances> e)
			{
				// Keep 'Balances' in sync with the Model.Balances
				switch (e.ChangeType)
				{
				case ListChg.Reset:
					{
						Balances.Clear();
						Balances.AddRange(Model.Balances);
						Invalidate();
						break;
					}
				case ListChg.ItemAdded:
					{
						Balances.Add(e.Item);
						Invalidate();
						break;
					}
				case ListChg.ItemPreRemove:
					{
						Balances.Remove(e.Item);
						Invalidate();
						break;
					}
				}
			}
		}

		/// <summary>An ordered copy of the model's balances</summary>
		private BindingListEx<Balances> Balances
		{
			get { return DataSource as BindingListEx<Balances>; }
			set { DataSource = value; }
		}

		/// <summary>How to display the values in a column</summary>
		private Dictionary<string, EDisplay> BalanceDisplayMode { get; set; }

		/// <summary>Update the columns in the grid to match the number balance contexts</summary>
		private void UpdateColumns()
		{
			// Remove all columns
			Columns.Clear();

			// Add a column for the currency
			Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Currency",
				Name = nameof(ECol.CurrencyColumn),
				FillWeight = 1.0f,
			});

			// Add a column for each fund
			foreach (var fund in Model.Funds)
			{
				Columns.Add(new DataGridViewTextBoxColumn
				{
					HeaderText = fund.Id,
					Name = fund.Id,
					FillWeight = 1.0f,
				});
			}
		}

		/// <summary></summary>
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
					var bal = SelectedRows.Cast<DataGridViewRow>().Select(x => (Balances)x.DataBoundItem).First();
					using (var dlg = new PromptUI { Title = "Add Fake Cash!", PromptText = "Add additional funds for testing", InputType = PromptUI.EInputType.Number, ValueType = typeof(decimal) })
					{
						if (dlg.ShowDialog(this) != DialogResult.OK) return;
						throw new NotImplementedException();
						//bal.FakeCash.Add((decimal)dlg.Value);
					}
				};
			}
			return cmenu;
		}

		/// <summary>Helper for dragging funds between main and another fund</summary>
		private class DragFunds
		{
			private DataGridView_.HitTestInfo m_hit;
			private FundBalance m_main;
			private FundBalance m_fund;
			private Unit<decimal> m_initial;
			private Unit<decimal> m_limit;
			private Point m_mouse_down;

			public DragFunds(DataGridView_.HitTestInfo hit)
			{
				m_hit = hit;
				m_main = Grid.Balances[hit.RowIndex][Fund.Main];
				m_fund = Grid.Balances[hit.RowIndex][hit.Column.Name];
				m_limit = Maths.Min(m_fund.Total + m_main.Total, m_fund.Total + m_main.Available);
				m_initial = m_fund.Total;
				m_mouse_down = hit.GridPoint;
			}
			public void Do(Point mouse_location)
			{
				if (m_limit == 0) return;
				var dx = mouse_location.X - m_mouse_down.X;
				var sign = Math.Sign(dx);

				// Get the division point for allocating funds
				const decimal MaxRangeX = 100m;
				var t_init = (decimal)(m_initial / m_limit);
				var t = Maths.Clamp(t_init + sign * Maths.Frac(0, Math.Abs(dx), +MaxRangeX), 0, 1);

				var now = m_main.Model.UtcNow;
				m_fund.Update(now, total: (    t) * m_limit);
				m_main.Update(now, total: (1 - t) * m_limit);

				Grid.InvalidateCell(m_hit.ColumnIndex, m_hit.RowIndex);
				Grid.InvalidateCell((int)ECol.MainFund, m_hit.RowIndex);
				Grid.Update();
			}
			private GridBalances Grid
			{
				get { return  (GridBalances)m_hit.Grid; }
			}
		}
		private DragFunds m_drag_funds;

		/// <summary>Modes for displaying the balances</summary>
		private enum EDisplay
		{
			Tokens,
			Values,
		}

		/// <summary>Column names</summary>
		private enum ECol
		{
			CurrencyColumn,
			MainFund,
		}
	}
}
