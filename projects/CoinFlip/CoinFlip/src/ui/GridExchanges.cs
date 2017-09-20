using System;
using System.Drawing;
using System.Windows.Forms;
using pr.extn;
using pr.gui;
using pr.util;

namespace CoinFlip
{
	public class GridExchanges :GridBase
	{
		public GridExchanges(Model model, string title, string name)
			:base(model, title, name)
		{
			ReadOnly = true;
			Columns.Add(new DataGridViewImageColumn
			{
				Name = nameof(Exchange.Active),
				HeaderText = "Active",
				DataPropertyName = nameof(Exchange.Active),
				AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader,
				ImageLayout = DataGridViewImageCellLayout.Normal,
				FillWeight = 0.1f,
			});
			Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = nameof(Exchange.Colour),
				HeaderText = "Colour",
				DataPropertyName = nameof(Exchange.Colour),
				FillWeight = 0.1f,
			});
			Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = nameof(Exchange.Name),
				HeaderText = "Exchange",
				DataPropertyName = nameof(Exchange.Name),
				FillWeight = 1.0f,
			});
			Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = nameof(Exchange.Status),
				HeaderText = "Status",
				DataPropertyName = nameof(Exchange.Status),
				FillWeight = 1.0f,
			});
			Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = nameof(Exchange.NettWorth),
				HeaderText = "Nett Worth",
				DataPropertyName = nameof(Exchange.NettWorth),
				DefaultCellStyle = new DataGridViewCellStyle{ Format = "C" },
				FillWeight = 1.0f,
			});
			Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = nameof(Exchange.CoinsAvailable),
				HeaderText = "Coins",
				DataPropertyName = nameof(Exchange.CoinsAvailable),
				FillWeight = 0.5f,
			});
			Columns.Add(new DataGridViewTextBoxColumn
			{
				Name = nameof(Exchange.PairsAvailable),
				HeaderText = "Pairs",
				DataPropertyName = nameof(Exchange.PairsAvailable),
				FillWeight = 0.5f,
			});
			ContextMenuStrip = CreateCMenu();
			DataSource = model.Exchanges;
		}
		protected override void OnCellClick(DataGridViewCellEventArgs a)
		{
			base.OnCellClick(a);

			if (!this.Within(a.ColumnIndex, a.RowIndex, out DataGridViewColumn col)) return;
			var exch = Model?.Exchanges[a.RowIndex];
			if (exch == null) return;

			switch (col.Name) {
			case nameof(Exchange.Active):
				{
					exch.Active = !exch.Active;
					break;
				}
			}
		}
		protected override void OnCellFormatting(DataGridViewCellFormattingEventArgs a)
		{
			base.OnCellFormatting(a);

			if (!this.Within(a.ColumnIndex, a.RowIndex, out DataGridViewColumn col)) return;
			var exch = Model?.Exchanges[a.RowIndex];

			a.CellStyle.BackColor =
				exch == null ? Color.White :
				exch.Status.HasFlag(EStatus.Error     ) ? Color.Red :
				exch.Status.HasFlag(EStatus.Stopped   ) ? Color.LightYellow :
				exch.Status.HasFlag(EStatus.Connected ) ? Color.LightGreen :
				exch.Status.HasFlag(EStatus.Connecting) ? Color.PaleGoldenrod :
				exch.Status.HasFlag(EStatus.Offline   ) ? Color.LightGray :
				Color.White;
			a.CellStyle.SelectionForeColor = a.CellStyle.ForeColor;
			a.CellStyle.SelectionBackColor = a.CellStyle.BackColor.Lerp(Color.Gray, 0.5f);

			switch (col.Name) {
			case nameof(Exchange.Active):
				{
					a.Value = (exch?.Active ?? false) ? Res.Active : Res.Inactive;
					break;
				}
			case nameof(Exchange.Colour):
				{
					a.Value = string.Empty;
					a.FormattingApplied = true;
					a.CellStyle.BackColor = exch.Colour;
					a.CellStyle.SelectionBackColor = a.CellStyle.BackColor;
					break;
				}
			}
		}

		/// <summary>Create the context menu for the grid</summary>
		private ContextMenuStrip CreateCMenu()
		{
			var cmenu = new ContextMenuStrip();
			{
				var opt = cmenu.Items.Add2(new ToolStripMenuItem("API Keys"));
				cmenu.Opening += (s,a) =>
				{
					opt.Enabled = SelectedRows.Count == 1;
				};
				opt.Click += (s,a) =>
				{
					var exch = (Exchange)SelectedRows[0].DataBoundItem;
					Model.ChangeAPIKeys(exch);
				};
			}
			return cmenu;
		}
	}
}
