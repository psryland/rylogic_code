using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using pr.container;
using pr.extn;
using pr.util;

namespace CoinFlip
{
	public class GridBots :GridBase
	{
		private DragDrop m_dd;

		public GridBots(Model model, string title, string name)
			:base(model, title, name)
		{
			m_dd = new DragDrop(this);
			m_dd.DoDrop += DataGridView_.DragDrop_DoDropMoveRow;
			Bots = Model.Bots;

			AllowDrop = true;
			RowHeadersVisible = true;
			RowHeadersWidth = 20;
			MultiSelect = false;
			Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Name",
				Name = nameof(IBot.Name),
				DataPropertyName = nameof(IBot.Name),
				FillWeight = 1.0f,
			});
			Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Funding (%)",
				Name = nameof(IBot.FundAllocationPC),
				DataPropertyName = nameof(IBot.FundAllocationPC),
				FillWeight = 1.0f,
				ToolTipText = "The fraction of the current balances allocated to this bot",
			});
			Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Update Rate (s)",
				Name = nameof(IBot.StepRate),
				DataPropertyName = nameof(IBot.StepRate),
				FillWeight = 1.0f,
				ToolTipText = "The step rate (in steps/second) that the main loop of this bot is run at",
			});
			Columns.Add(new DataGridViewImageColumn
			{
				HeaderText = "Active",
				Name = nameof(IBot.Active),
				DataPropertyName = nameof(IBot.Active),
				FillWeight = 0.5f,
				ToolTipText = "Enable/Disable the main loop of this bot",
			});
			ContextMenuStrip = CreateCMenu();
		}
		protected override void Dispose(bool disposing)
		{
			Bots = null;
			base.Dispose(disposing);
		}
		protected override void OnDataSourceChanged(EventArgs e)
		{
			base.OnDataSourceChanged(e);
		}
		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);
			DataGridView_.DragDrop_DragRow(this, e);
		}
		protected override void OnCellClick(DataGridViewCellEventArgs a)
		{
			base.OnCellClick(a);
			if (!this.Within(a.ColumnIndex, a.RowIndex, out DataGridViewColumn col))
				return;

			var bot = Model.Bots[a.RowIndex];
			switch (col.DataPropertyName) {
			case nameof(IBot.Active):
				{
					bot.Active = !bot.Active;
					break;
				}
			}
		}
		protected override void OnCellFormatting(DataGridViewCellFormattingEventArgs e)
		{
			base.OnCellFormatting(e);
			if (!this.Within(e.ColumnIndex, e.RowIndex, out DataGridViewColumn col))
				return;

			var bot = Model.Bots[e.RowIndex];
			switch (col.DataPropertyName) {
			case nameof(IBot.Active):
				{
					e.Value = bot.Active ? Res.Active : Res.Inactive;
					break;
				}
			}

			e.CellStyle.ForeColor = Color.Black;
			e.CellStyle.BackColor = bot.Settings.Valid ? Color.White : Color.LightSalmon;
			e.CellStyle.SelectionForeColor = e.CellStyle.ForeColor;
			e.CellStyle.SelectionBackColor = e.CellStyle.BackColor.Lerp(Color.Gray, 0.5f);
		}
		protected override void OnCellToolTipTextNeeded(DataGridViewCellToolTipTextNeededEventArgs e)
		{
			base.OnCellToolTipTextNeeded(e);
			if (!this.Within(e.ColumnIndex, e.RowIndex, out DataGridViewColumn col))
				return;

			var bot = Model.Bots[e.RowIndex];
			if (bot.Settings.Valid)
				return;

			e.ToolTipText = bot.Settings.ErrorDescription;
		}

		/// <summary>The data source for this grid</summary>
		private BindingSource<IBot> Bots
		{
			get { return m_bots; }
			set
			{
				if (m_bots == value) return;
				if (m_bots != null)
				{
					m_bots.ListChanging -= HandleBotsListChanging;
					DataSource = null;
				}
				m_bots = value;
				if (m_bots != null)
				{
					DataSource = m_bots;
					m_bots.ListChanging += HandleBotsListChanging;
				}
			}
		}
		private BindingSource<IBot> m_bots;
		private void HandleBotsListChanging(object sender, ListChgEventArgs<IBot> e)
		{}

		/// <summary>Create the context menu for the grid</summary>
		private ContextMenuStrip CreateCMenu()
		{
			var cmenu = new ContextMenuStrip();
			{
				// Add a bot.
				var opt = cmenu.Items.Add2(new ToolStripMenuItem("Add Bot"));
				opt.Click += (s,a) =>
				{
					Model.ShowAddBotUI();
				};
			}
			{
				// Delete a bot
				var opt = cmenu.Items.Add2(new ToolStripMenuItem("Remove Bot"));
				cmenu.Opening += (s,a) =>
				{
					opt.Enabled = SelectedRows.Count == 1;
				};
				opt.Click += async (s,a) =>
				{
					var bot = (IBot)SelectedRows[0].DataBoundItem;
					Model.Bots.Remove(bot);
					await bot.ShutdownAsync();
					Util.Dispose(ref bot);
				};
			}
			cmenu.Opening += (s,a) =>
			{
				// Repopulate from the selected bot
				for (int i = cmenu.Items.Count; i-- > 2;)
					cmenu.Items.RemoveAt(i);

				if (SelectedRows.Count == 1)
				{
					var bot = (IBot)SelectedRows[0].DataBoundItem;
					bot.CMenuItems(cmenu);
				}
			};
			return cmenu;
		}
	}
}
