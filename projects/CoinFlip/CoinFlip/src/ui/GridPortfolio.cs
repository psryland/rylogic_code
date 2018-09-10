using Rylogic.Extn;
using Rylogic.Gui.WinForms;
using Rylogic.Utility;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CoinFlip
{
	public class GridPortfolio :GridBase
	{
		public GridPortfolio(Model model, string title, string name)
			:base(model, title, name)
		{
			Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Currency Pair",
				Name = nameof(ColumnNames.Pair),
				ToolTipText = "The pair traded",
			});
			Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Date",
				Name = nameof(ColumnNames.Date),
				ToolTipText = "When the trade was made",
			});
			Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Price",
				Name = nameof(ColumnNames.Price),
				ToolTipText = "The price at the time of the trade",
			});
			Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Volume",
				Name = nameof(ColumnNames.Price),
				ToolTipText = "The volume traded",
			});
			Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Value Change",
			});
			DataSource = model.Portfolio;
		}
		protected override void OnMouseClick(MouseEventArgs e)
		{
			base.OnMouseClick(e);
			if (e.Button == MouseButtons.Right)
			{
				var hit = this.HitTestEx(e.Location);
				if (hit.Type == DataGridView_.HitTestInfo.EType.ColumnHeader || hit.Type == DataGridView_.HitTestInfo.EType.ColumnDivider)
				{
					this.ColumnVisibilityContextMenu(hit.GridPoint);
				}
				else
				{
					var pos = SelectedRows.Cast<DataGridViewRow>().Select(x => (PortfolioData)x.DataBoundItem).SingleOrDefault();
					var cmenu = new ContextMenuStrip();
					{
						var opt = cmenu.Items.Add2(new ToolStripMenuItem("Edit Details"));
						opt.Enabled = pos != null;
						opt.Click += (s, a) =>
						{

						};
					}
					cmenu.Items.AddSeparator();
					{
						var opt = cmenu.Items.Add2(new ToolStripMenuItem("Add Position"));
						opt.Click += (s, a) =>
						{
							var pd = new PortfolioData();
							using (var dlg = new EditPortfolioPositionUI(Model, pd))
							{
								if (dlg.ShowDialog(Model.UI) != DialogResult.OK) return;
								Model.Portfolio.Add(dlg.Position);
							}
						};
					}
					cmenu.Show(this, hit.GridPoint);
				}
			}
		}
		public static class ColumnNames
		{
			public const int Pair = 0;
			public const int Date = 0;
			public const int Price = 0;
			public const int Volume = 0;
		}
	}
}
