using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using CoinFlip.Properties;
using pr.container;
using pr.extn;
using pr.gui;
using pr.util;
using ToolStripContainer = pr.gui.ToolStripContainer;

namespace CoinFlip
{
	public class MainUI :Form
	{
		#region UI Elements
		private CheckBox m_chk_run;
		private ToolStripContainer m_tsc;
		private StatusStrip m_ss;
		private ToolStripStatusLabel m_status;
		private SplitContainer m_split0;
		private pr.gui.DataGridView m_grid_exchanges;
		private TableLayoutPanel m_table0;
		private pr.gui.DataGridView m_grid_coins;
		private SplitContainer m_split1;
		private pr.gui.DataGridView m_grid_balances;
		private CheckBox m_chk_allow_trades;
		private pr.gui.DataGridView m_grid_positions;
		private MenuStrip m_menu;
		private ToolStripMenuItem m_menu_file;
		private ToolStripMenuItem m_menu_file_show_loops;
		private ToolStripSeparator m_menu_file_sep0;
		private ToolStripMenuItem m_menu_file_exit;
		private ToolStripMenuItem m_menu_file_test;
		private ToolStripMenuItem m_menu_file_show_pairs;
		private ToolStripSeparator toolStripSeparator1;
		private SplitContainer m_split2;
		private SplitContainer m_split3;
		private LogUI m_log;
		#endregion

		public MainUI()
		{
			InitializeComponent();
			Model = new Model(this);
			LoopsUI = new LoopsUI(Model, this);
			PairsUI = new PairsUI(Model, this);

			SetupUI();
		}
		protected override void Dispose(bool disposing)
		{
			LoopsUI = null;
			PairsUI = null;
			Model = null;
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}
		protected override void OnClosed(EventArgs e)
		{
			m_chk_run.Checked = false;
			base.OnClosed(e);
		}

		/// <summary>App logic</summary>
		public Model Model
		{
			[DebuggerStepThrough] get { return m_model; }
			private set
			{
				if (m_model == value) return;
				if (m_model != null)
				{
					m_model.Pairs.ListChanging           -= HandlePairsChanging;
					m_model.CoinsOfInterest.ListChanging -= HandleCoinsOfInterestChanging;
					m_model.Exchanges.ListChanging       -= HandleExchangesChanging;
					m_model.HeartBeat                    -= HandleHeatBeat;
					Util.Dispose(ref m_model);
				}
				m_model = value;
				if (m_model != null)
				{
					m_model.HeartBeat                    += HandleHeatBeat;
					m_model.Exchanges.ListChanging       += HandleExchangesChanging;
					m_model.CoinsOfInterest.ListChanging += HandleCoinsOfInterestChanging;
					m_model.Pairs.ListChanging           += HandlePairsChanging;
				}
			}
		}
		private Model m_model;

		/// <summary>The UI for displaying the loops</summary>
		public LoopsUI LoopsUI
		{
			get { return m_loops_ui; }
			private set
			{
				if (m_loops_ui == value) return;
				Util.Dispose(ref m_loops_ui);
				m_loops_ui = value;
			}
		}
		private LoopsUI m_loops_ui;

		/// <summary>The UI for displaying info about a particular pair</summary>
		public PairsUI PairsUI
		{
			get { return m_pairs_ui; }
			private set
			{
				if (m_pairs_ui == value) return;
				Util.Dispose(ref m_pairs_ui);
				m_pairs_ui = value;
			}
		}
		private PairsUI m_pairs_ui;

		/// <summary>Wire up the UI</summary>
		private void SetupUI()
		{
			#region Menu
			{
				m_menu_file_test.Click += (s,a) =>
				{
					Model.Test();
				};
				m_menu_file_show_pairs.Click += (s,a) =>
				{
					PairsUI.Show();
				};
				m_menu_file_show_loops.Click += (s,a) =>
				{
					LoopsUI.Show();
				};
				m_menu_file_exit.Click += (s,a) =>
				{
					Close();
				};
			}
			#endregion

			#region Coins grid
			{
				m_grid_coins.AutoGenerateColumns = false;
				m_grid_coins.Columns.Add(new DataGridViewTextBoxColumn
				{
					HeaderText = "Currency",
					SortMode = DataGridViewColumnSortMode.Automatic,
				});
				m_grid_coins.DataSource = Model.CoinsOfInterest;
				m_grid_coins.CellFormatting += (s,a) =>
				{
					if (!a.RowIndex.Within(0, Model.CoinsOfInterest.Count)) return;
					a.Value = Model.CoinsOfInterest[a.RowIndex];
					a.CellStyle.BackColor = Color.White;
					a.CellStyle.SelectionForeColor = a.CellStyle.ForeColor;
					a.CellStyle.SelectionBackColor = a.CellStyle.BackColor.Lerp(Color.Gray, 0.5f);
				};
				m_grid_coins.ContextMenuStrip = new ContextMenuStrip();
				{
					var opt = m_grid_coins.ContextMenuStrip.Items.Add2(new ToolStripMenuItem("Add Coin"));
					opt.Click += (s,a) =>
					{
						using (var prompt = new PromptForm { Title = "Currency Symbol", PromptText = string.Empty })
						{
							if (prompt.ShowDialog(this) != DialogResult.OK) return;
							Model.CoinsOfInterest.Add(prompt.Value.ToUpperInvariant());
						}
					};
				}
				{
					var opt = m_grid_coins.ContextMenuStrip.Items.Add2(new ToolStripMenuItem("Delete"));
					m_grid_coins.ContextMenuStrip.Opening += (s,a) =>
					{
						opt.Enabled = m_grid_coins.SelectedRows.Count != 0;
					};
					opt.Click += (s,a) =>
					{
						var doomed = m_grid_coins.SelectedRows.Cast<DataGridViewRow>().Select(x => (string)x.DataBoundItem).ToHashSet();
						Model.CoinsOfInterest.RemoveAll(doomed);
					};
				}
			}
			#endregion

			#region Exchanges grid
			{
				m_grid_exchanges.AutoGenerateColumns = false;
				m_grid_exchanges.Columns.Add(new DataGridViewImageColumn
				{
					Name = nameof(Exchange.Active),
					HeaderText = "Active",
					DataPropertyName = nameof(Exchange.Active),
					AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader,
					ImageLayout = DataGridViewImageCellLayout.Normal,
					DefaultCellStyle = new DataGridViewCellStyle{ Padding = new Padding(5) },
					FillWeight = 0.1f,
				});
				m_grid_exchanges.Columns.Add(new DataGridViewTextBoxColumn
				{
					Name = nameof(Exchange.Name),
					HeaderText = "Exchange",
					DataPropertyName = nameof(Exchange.Name),
					FillWeight = 1.0f,
				});
				m_grid_exchanges.Columns.Add(new DataGridViewTextBoxColumn
				{
					Name = nameof(Exchange.Status),
					HeaderText = "Status",
					DataPropertyName = nameof(Exchange.Status),
					FillWeight = 1.0f,
				});
				m_grid_exchanges.Columns.Add(new DataGridViewTextBoxColumn
				{
					Name = nameof(Exchange.CoinsAvailable),
					HeaderText = "Coins",
					DataPropertyName = nameof(Exchange.CoinsAvailable),
					FillWeight = 0.5f,
				});
				m_grid_exchanges.Columns.Add(new DataGridViewTextBoxColumn
				{
					Name = nameof(Exchange.PairsAvailable),
					HeaderText = "Pairs",
					DataPropertyName = nameof(Exchange.PairsAvailable),
					FillWeight = 0.5f,
				});
				m_grid_exchanges.CellClick += (s,a) =>
				{
					if (!m_grid_exchanges.Within(a.ColumnIndex, a.RowIndex, out DataGridViewColumn col)) return;
					var exch = Model.Exchanges[a.RowIndex];

					switch (col.Name) {
					case nameof(Exchange.Active):
						{
							exch.Active = !exch.Active;
							break;
						}
					}
				};
				m_grid_exchanges.CellFormatting += (s,a) =>
				{
					if (!m_grid_exchanges.Within(a.ColumnIndex, a.RowIndex, out DataGridViewColumn col)) return;
					var exch = Model.Exchanges[a.RowIndex];

					switch (col.Name) {
					case nameof(Exchange.Active):
						{
							a.Value = exch.Active ? Res.Active : Res.Inactive;
							break;
						}
					}

					a.CellStyle.BackColor =
						exch.Status.HasFlag(EStatus.Error     ) ? Color.Red :
						exch.Status.HasFlag(EStatus.Stopped   ) ? Color.LightYellow :
						exch.Status.HasFlag(EStatus.Connected ) ? Color.LightGreen :
						exch.Status.HasFlag(EStatus.Connecting) ? Color.PaleGoldenrod :
						exch.Status.HasFlag(EStatus.Offline   ) ? Color.LightGray :
						Color.White;
					a.CellStyle.SelectionForeColor = a.CellStyle.ForeColor;
					a.CellStyle.SelectionBackColor = a.CellStyle.BackColor.Lerp(Color.Gray, 0.5f);
				};
				m_grid_exchanges.DataSource = Model.Exchanges;
			}
			#endregion

			#region Balances Grid
			{
				m_grid_balances.AutoGenerateColumns = false;
				m_grid_balances.Columns.Add(new DataGridViewTextBoxColumn
				{
					Name = nameof(Balance.Coin),
					HeaderText = "Currency",
					DataPropertyName = nameof(Balance.Coin),
					SortMode = DataGridViewColumnSortMode.Automatic,
					FillWeight = 0.3f,
				});
				m_grid_balances.Columns.Add(new DataGridViewTextBoxColumn
				{
					Name = nameof(Balance.Available),
					HeaderText = "Available",
					DataPropertyName = nameof(Balance.Available),
					SortMode = DataGridViewColumnSortMode.Automatic,
					FillWeight = 0.6f,
				});
				m_grid_balances.CellFormatting += (s,a) =>
				{
					var col = m_grid_balances.Columns[a.ColumnIndex];
					if (Model.Exchanges.Current is CrossExchange && col.Name == nameof(Balance.Coin))
					{
						a.Value = Model.Balances[a.RowIndex].Coin.SymbolWithExchange;
						a.FormattingApplied = true;
					}
					a.CellStyle.SelectionBackColor = a.CellStyle.BackColor.Lerp(Color.Gray, 0.5f);
					a.CellStyle.SelectionForeColor = a.CellStyle.ForeColor;
				};
				m_grid_balances.DataSource = Model.Balances;
			}
			#endregion

			#region Positions Grid
			{
				m_grid_positions.AutoGenerateColumns = false;
				m_grid_positions.Columns.Add(new DataGridViewTextBoxColumn
				{
					Name = nameof(Position.OrderId),
					HeaderText = "Order Id",
					DataPropertyName = nameof(Position.OrderId),
					SortMode = DataGridViewColumnSortMode.Automatic,
					FillWeight = 1.0f,
					Visible = false,
				});
				m_grid_positions.Columns.Add(new DataGridViewTextBoxColumn
				{
					Name = nameof(Position.TradeType),
					HeaderText = "Type",
					DataPropertyName = nameof(Position.TradeType),
					SortMode = DataGridViewColumnSortMode.Automatic,
					FillWeight = 1.0f,
				});
				m_grid_positions.Columns.Add(new DataGridViewTextBoxColumn
				{
					Name = nameof(Position.Pair),
					HeaderText = "Pair",
					DataPropertyName = nameof(Position.Pair),
					SortMode = DataGridViewColumnSortMode.Automatic,
					FillWeight = 0.5f,
				});
				m_grid_positions.Columns.Add(new DataGridViewTextBoxColumn
				{
					Name = nameof(Position.Rate),
					HeaderText = "Trade at Rate",
					DataPropertyName = nameof(Position.Rate),
					SortMode = DataGridViewColumnSortMode.NotSortable,
					FillWeight = 1.0f,
				});
				m_grid_positions.Columns.Add(new DataGridViewTextBoxColumn
				{
					Name = nameof(ColumnNames.LivePrice),
					HeaderText = "Live Price",
					DataPropertyName = nameof(ColumnNames.LivePrice),
					SortMode = DataGridViewColumnSortMode.NotSortable,
					FillWeight = 1.0f,
				});
				m_grid_positions.Columns.Add(new DataGridViewTextBoxColumn
				{
					Name = nameof(ColumnNames.PriceDist),
					HeaderText = "Distance",
					DataPropertyName = nameof(ColumnNames.PriceDist),
					SortMode = DataGridViewColumnSortMode.NotSortable,
					FillWeight = 1.0f,
				});
				m_grid_positions.Columns.Add(new DataGridViewTextBoxColumn
				{
					Name = nameof(Position.VolumeBase),
					HeaderText = "Volume (Base)",
					DataPropertyName = nameof(Position.VolumeBase),
					SortMode = DataGridViewColumnSortMode.NotSortable,
					FillWeight = 1.0f,
				});
				m_grid_positions.Columns.Add(new DataGridViewTextBoxColumn
				{
					Name = nameof(Position.VolumeQuote),
					HeaderText = "Volume (Quote)",
					DataPropertyName = nameof(Position.VolumeQuote),
					SortMode = DataGridViewColumnSortMode.NotSortable,
					FillWeight = 1.0f,
				});
				m_grid_positions.Columns.Add(new DataGridViewTextBoxColumn
				{
					Name = nameof(Position.Remaining),
					HeaderText = "Remaining",
					DataPropertyName = nameof(Position.Remaining),
					SortMode = DataGridViewColumnSortMode.NotSortable,
					FillWeight = 1.0f,
				});
				m_grid_positions.CellFormatting += (s,a) =>
				{
					if (!m_grid_positions.Within(a.ColumnIndex, a.RowIndex, out DataGridViewColumn col)) return;
					var pos = Model.Positions[a.RowIndex];

					switch (col.Name)
					{
					case nameof(Position.TradeType):
						{
							a.Value = "{0}→{1} ({2})".Fmt(
								pos.TradeType == ETradeType.Buy ? pos.Pair.Base : pos.Pair.Quote,
								pos.TradeType == ETradeType.Buy ? pos.Pair.Quote : pos.Pair.Base,
								pos.TradeType);
							a.FormattingApplied = true;
							break;
						}
					case nameof(Position.Rate):
						{
							a.Value = "{0:G8}".Fmt((decimal)pos.Rate);
							a.FormattingApplied = true;
							break;
						}
					case nameof(ColumnNames.LivePrice):
						{
							a.Value = "{0:G8}".Fmt((decimal)pos.Pair.CurrentPrice(pos.TradeType));
							a.FormattingApplied = true;
							break;
						}
					case nameof(ColumnNames.PriceDist):
						{
							a.Value = "{0:G8}".Fmt(
								pos.TradeType == ETradeType.Sell ? (decimal)(pos.Rate - pos.Pair.CurrentPrice(ETradeType.Sell)) :
								pos.TradeType == ETradeType.Buy  ? (decimal)(pos.Pair.CurrentPrice(ETradeType.Buy) - pos.Rate) :
								0m);
							a.FormattingApplied = true;
							break;
						}
					case nameof(Position.VolumeBase):
						{
							a.Value = "{0:G8} {1}".Fmt((decimal)pos.VolumeBase, pos.Pair.Base);
							a.FormattingApplied = true;
							break;
						}
					case nameof(Position.VolumeQuote):
						{
							a.Value = "{0:G8} {1}".Fmt((decimal)pos.VolumeQuote, pos.Pair.Quote);
							a.FormattingApplied = true;
							break;
						}
					case nameof(Position.Remaining):
						{
							a.Value = "{0:G8} {1}".Fmt((decimal)pos.Remaining, pos.Pair.Base);
							a.FormattingApplied = true;
							break;
						}
					}
					a.CellStyle.SelectionBackColor = a.CellStyle.BackColor.Lerp(Color.Gray, 0.5f);
					a.CellStyle.SelectionForeColor = a.CellStyle.ForeColor;
				};
				m_grid_positions.MouseDown += DataGridViewEx.ColumnVisibility;
				m_grid_positions.DataSource = Model.Positions;
			}
			#endregion

			#region Log control
			{
				m_log.LogFilepath = ((LogToFile)Model.Log.LogCB).Filepath;
				m_log.LogEntryPattern = new Regex(@"^.*?\|(?<Level>.*?)\|(?<Timestamp>.*?)\|(?<Message>.*)\n"
					,RegexOptions.Singleline|RegexOptions.Multiline|RegexOptions.CultureInvariant|RegexOptions.Compiled);
				m_log.Freeze = true;
			}
			#endregion

			#region Start/Stop button
			{
				m_chk_run.CheckedChanged += (s,a) =>
				{
					Model.Run = m_chk_run.Checked;
					m_chk_run.Text = m_chk_run.Checked ? "Stop" : "Start";
					m_chk_run.BackColor = m_chk_run.Checked ? Color.LightGreen : SystemColors.Control;
				};
			}
			#endregion

			#region Enable Trading
			{
				m_chk_allow_trades.CheckedChanged += (s,a) =>
				{
					Model.AllowTrades = m_chk_allow_trades.Checked;
					m_chk_allow_trades.Text = m_chk_allow_trades.Checked ? "Disable Trading" : "Enable Trading";
					m_chk_allow_trades.BackColor = m_chk_allow_trades.Checked ? Color.LightGreen : SystemColors.Control;
				};
			}
			#endregion
		}

		/// <summary>Model heart beat event handler</summary>
		private void HandleHeatBeat(object sender, EventArgs e)
		{
			// Invalidate the live price data 
			m_grid_balances.InvalidateColumn(m_grid_balances.Columns[nameof(Balance.Available)].Index);
			m_grid_positions.InvalidateColumn(m_grid_positions.Columns[nameof(ColumnNames.LivePrice)].Index);
			m_grid_positions.InvalidateColumn(m_grid_positions.Columns[nameof(ColumnNames.PriceDist)].Index);
		}

		/// <summary>Coins of interest changed</summary>
		private void HandleCoinsOfInterestChanging(object sender, ListChgEventArgs<string> e)
		{
			m_grid_coins.Invalidate();
			m_grid_balances.Invalidate();
		}

		/// <summary>Exchanges changed</summary>
		private void HandleExchangesChanging(object sender, ListChgEventArgs<Exchange> e)
		{
			m_grid_exchanges.Invalidate();
			m_grid_balances.Invalidate();
			m_grid_positions.Invalidate();
		}

		/// <summary>Available pairs changed</summary>
		private void HandlePairsChanging(object sender, EventArgs e)
		{
			m_grid_exchanges.InvalidateColumn(m_grid_exchanges.Columns[nameof(Exchange.CoinsAvailable)].Index);
			m_grid_exchanges.InvalidateColumn(m_grid_exchanges.Columns[nameof(Exchange.PairsAvailable)].Index);
		}

		private static class ColumnNames
		{
			public const string LivePrice = "LivePrice";
			public const string PriceDist = "PriceDist";
		}
		private static class Res
		{
			public static readonly Image Active   = new Bitmap(Resources.active, new Size(28,28));
			public static readonly Image Inactive = new Bitmap(Resources.inactive, new Size(28,28));
		}

		#region Windows Form Designer generated code
		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainUI));
			this.m_chk_run = new System.Windows.Forms.CheckBox();
			this.m_tsc = new pr.gui.ToolStripContainer();
			this.m_ss = new System.Windows.Forms.StatusStrip();
			this.m_status = new System.Windows.Forms.ToolStripStatusLabel();
			this.m_split0 = new System.Windows.Forms.SplitContainer();
			this.m_table0 = new System.Windows.Forms.TableLayoutPanel();
			this.m_chk_allow_trades = new System.Windows.Forms.CheckBox();
			this.m_grid_exchanges = new pr.gui.DataGridView();
			this.m_grid_coins = new pr.gui.DataGridView();
			this.m_split1 = new System.Windows.Forms.SplitContainer();
			this.m_split2 = new System.Windows.Forms.SplitContainer();
			this.m_grid_balances = new pr.gui.DataGridView();
			this.m_grid_positions = new pr.gui.DataGridView();
			this.m_log = new pr.gui.LogUI();
			this.m_menu = new System.Windows.Forms.MenuStrip();
			this.m_menu_file = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_file_show_pairs = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_file_show_loops = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_file_test = new System.Windows.Forms.ToolStripMenuItem();
			this.m_menu_file_sep0 = new System.Windows.Forms.ToolStripSeparator();
			this.m_menu_file_exit = new System.Windows.Forms.ToolStripMenuItem();
			this.m_split3 = new System.Windows.Forms.SplitContainer();
			this.m_tsc.BottomToolStripPanel.SuspendLayout();
			this.m_tsc.ContentPanel.SuspendLayout();
			this.m_tsc.TopToolStripPanel.SuspendLayout();
			this.m_tsc.SuspendLayout();
			this.m_ss.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_split0)).BeginInit();
			this.m_split0.Panel1.SuspendLayout();
			this.m_split0.Panel2.SuspendLayout();
			this.m_split0.SuspendLayout();
			this.m_table0.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_grid_exchanges)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.m_grid_coins)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.m_split1)).BeginInit();
			this.m_split1.Panel1.SuspendLayout();
			this.m_split1.Panel2.SuspendLayout();
			this.m_split1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_split2)).BeginInit();
			this.m_split2.Panel1.SuspendLayout();
			this.m_split2.Panel2.SuspendLayout();
			this.m_split2.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_grid_balances)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.m_grid_positions)).BeginInit();
			this.m_menu.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_split3)).BeginInit();
			this.m_split3.Panel1.SuspendLayout();
			this.m_split3.Panel2.SuspendLayout();
			this.m_split3.SuspendLayout();
			this.SuspendLayout();
			// 
			// m_chk_run
			// 
			this.m_chk_run.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_chk_run.Appearance = System.Windows.Forms.Appearance.Button;
			this.m_chk_run.Location = new System.Drawing.Point(422, 3);
			this.m_chk_run.Name = "m_chk_run";
			this.m_chk_run.Size = new System.Drawing.Size(65, 63);
			this.m_chk_run.TabIndex = 1;
			this.m_chk_run.Text = "Start";
			this.m_chk_run.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			this.m_chk_run.UseVisualStyleBackColor = true;
			// 
			// m_tsc
			// 
			// 
			// m_tsc.BottomToolStripPanel
			// 
			this.m_tsc.BottomToolStripPanel.Controls.Add(this.m_ss);
			// 
			// m_tsc.ContentPanel
			// 
			this.m_tsc.ContentPanel.Controls.Add(this.m_split0);
			this.m_tsc.ContentPanel.Margin = new System.Windows.Forms.Padding(0);
			this.m_tsc.ContentPanel.Padding = new System.Windows.Forms.Padding(8, 0, 8, 0);
			this.m_tsc.ContentPanel.Size = new System.Drawing.Size(674, 592);
			this.m_tsc.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_tsc.Location = new System.Drawing.Point(0, 0);
			this.m_tsc.Margin = new System.Windows.Forms.Padding(0);
			this.m_tsc.Name = "m_tsc";
			this.m_tsc.Size = new System.Drawing.Size(674, 638);
			this.m_tsc.TabIndex = 2;
			this.m_tsc.Text = "tsc";
			// 
			// m_tsc.TopToolStripPanel
			// 
			this.m_tsc.TopToolStripPanel.Controls.Add(this.m_menu);
			// 
			// m_ss
			// 
			this.m_ss.Dock = System.Windows.Forms.DockStyle.None;
			this.m_ss.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_status});
			this.m_ss.Location = new System.Drawing.Point(0, 0);
			this.m_ss.Name = "m_ss";
			this.m_ss.Size = new System.Drawing.Size(674, 22);
			this.m_ss.TabIndex = 0;
			// 
			// m_status
			// 
			this.m_status.Name = "m_status";
			this.m_status.Size = new System.Drawing.Size(26, 17);
			this.m_status.Text = "Idle";
			// 
			// m_split0
			// 
			this.m_split0.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_split0.Location = new System.Drawing.Point(8, 0);
			this.m_split0.Margin = new System.Windows.Forms.Padding(0);
			this.m_split0.Name = "m_split0";
			this.m_split0.Orientation = System.Windows.Forms.Orientation.Horizontal;
			// 
			// m_split0.Panel1
			// 
			this.m_split0.Panel1.Controls.Add(this.m_split3);
			// 
			// m_split0.Panel2
			// 
			this.m_split0.Panel2.Controls.Add(this.m_split1);
			this.m_split0.Size = new System.Drawing.Size(658, 592);
			this.m_split0.SplitterDistance = 138;
			this.m_split0.TabIndex = 3;
			// 
			// m_table0
			// 
			this.m_table0.ColumnCount = 2;
			this.m_table0.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.m_table0.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 71F));
			this.m_table0.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
			this.m_table0.Controls.Add(this.m_grid_exchanges, 0, 0);
			this.m_table0.Controls.Add(this.m_chk_run, 1, 0);
			this.m_table0.Controls.Add(this.m_chk_allow_trades, 1, 1);
			this.m_table0.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_table0.Location = new System.Drawing.Point(0, 0);
			this.m_table0.Margin = new System.Windows.Forms.Padding(0);
			this.m_table0.Name = "m_table0";
			this.m_table0.RowCount = 2;
			this.m_table0.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
			this.m_table0.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
			this.m_table0.Size = new System.Drawing.Size(490, 138);
			this.m_table0.TabIndex = 1;
			// 
			// m_chk_allow_trades
			// 
			this.m_chk_allow_trades.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_chk_allow_trades.Appearance = System.Windows.Forms.Appearance.Button;
			this.m_chk_allow_trades.Location = new System.Drawing.Point(422, 72);
			this.m_chk_allow_trades.Name = "m_chk_allow_trades";
			this.m_chk_allow_trades.Size = new System.Drawing.Size(65, 63);
			this.m_chk_allow_trades.TabIndex = 3;
			this.m_chk_allow_trades.Text = "Enable Trading";
			this.m_chk_allow_trades.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			this.m_chk_allow_trades.UseVisualStyleBackColor = true;
			// 
			// m_grid_exchanges
			// 
			this.m_grid_exchanges.AllowUserToAddRows = false;
			this.m_grid_exchanges.AllowUserToDeleteRows = false;
			this.m_grid_exchanges.AllowUserToOrderColumns = true;
			this.m_grid_exchanges.AllowUserToResizeRows = false;
			this.m_grid_exchanges.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
			this.m_grid_exchanges.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
			this.m_grid_exchanges.BackgroundColor = System.Drawing.SystemColors.Window;
			this.m_grid_exchanges.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.None;
			this.m_grid_exchanges.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Window;
			dataGridViewCellStyle2.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.ControlText;
			dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
			this.m_grid_exchanges.DefaultCellStyle = dataGridViewCellStyle2;
			this.m_grid_exchanges.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_grid_exchanges.EditMode = System.Windows.Forms.DataGridViewEditMode.EditProgrammatically;
			this.m_grid_exchanges.Location = new System.Drawing.Point(0, 0);
			this.m_grid_exchanges.Margin = new System.Windows.Forms.Padding(0);
			this.m_grid_exchanges.Name = "m_grid_exchanges";
			this.m_grid_exchanges.ReadOnly = true;
			this.m_grid_exchanges.RowHeadersVisible = false;
			this.m_table0.SetRowSpan(this.m_grid_exchanges, 2);
			this.m_grid_exchanges.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.m_grid_exchanges.Size = new System.Drawing.Size(419, 138);
			this.m_grid_exchanges.TabIndex = 0;
			// 
			// m_grid_coins
			// 
			this.m_grid_coins.AllowUserToAddRows = false;
			this.m_grid_coins.AllowUserToDeleteRows = false;
			this.m_grid_coins.AllowUserToResizeColumns = false;
			this.m_grid_coins.AllowUserToResizeRows = false;
			this.m_grid_coins.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
			this.m_grid_coins.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
			this.m_grid_coins.BackgroundColor = System.Drawing.SystemColors.Window;
			this.m_grid_coins.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.None;
			this.m_grid_coins.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Window;
			dataGridViewCellStyle1.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.ControlText;
			dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
			this.m_grid_coins.DefaultCellStyle = dataGridViewCellStyle1;
			this.m_grid_coins.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_grid_coins.Location = new System.Drawing.Point(0, 0);
			this.m_grid_coins.Margin = new System.Windows.Forms.Padding(0);
			this.m_grid_coins.Name = "m_grid_coins";
			this.m_grid_coins.ReadOnly = true;
			this.m_grid_coins.RowHeadersVisible = false;
			this.m_grid_coins.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.m_grid_coins.Size = new System.Drawing.Size(164, 138);
			this.m_grid_coins.TabIndex = 2;
			// 
			// m_split1
			// 
			this.m_split1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_split1.Location = new System.Drawing.Point(0, 0);
			this.m_split1.Margin = new System.Windows.Forms.Padding(0);
			this.m_split1.Name = "m_split1";
			this.m_split1.Orientation = System.Windows.Forms.Orientation.Horizontal;
			// 
			// m_split1.Panel1
			// 
			this.m_split1.Panel1.Controls.Add(this.m_split2);
			// 
			// m_split1.Panel2
			// 
			this.m_split1.Panel2.Controls.Add(this.m_log);
			this.m_split1.Size = new System.Drawing.Size(658, 450);
			this.m_split1.SplitterDistance = 162;
			this.m_split1.TabIndex = 3;
			// 
			// m_split2
			// 
			this.m_split2.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_split2.Location = new System.Drawing.Point(0, 0);
			this.m_split2.Margin = new System.Windows.Forms.Padding(0);
			this.m_split2.Name = "m_split2";
			// 
			// m_split2.Panel1
			// 
			this.m_split2.Panel1.Controls.Add(this.m_grid_balances);
			// 
			// m_split2.Panel2
			// 
			this.m_split2.Panel2.Controls.Add(this.m_grid_positions);
			this.m_split2.Size = new System.Drawing.Size(658, 162);
			this.m_split2.SplitterDistance = 164;
			this.m_split2.TabIndex = 2;
			// 
			// m_grid_balances
			// 
			this.m_grid_balances.AllowUserToAddRows = false;
			this.m_grid_balances.AllowUserToDeleteRows = false;
			this.m_grid_balances.AllowUserToOrderColumns = true;
			this.m_grid_balances.AllowUserToResizeRows = false;
			this.m_grid_balances.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
			this.m_grid_balances.BackgroundColor = System.Drawing.SystemColors.Window;
			this.m_grid_balances.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.None;
			this.m_grid_balances.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle3.BackColor = System.Drawing.SystemColors.Window;
			dataGridViewCellStyle3.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle3.ForeColor = System.Drawing.SystemColors.ControlText;
			dataGridViewCellStyle3.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle3.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
			this.m_grid_balances.DefaultCellStyle = dataGridViewCellStyle3;
			this.m_grid_balances.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_grid_balances.Location = new System.Drawing.Point(0, 0);
			this.m_grid_balances.Margin = new System.Windows.Forms.Padding(0);
			this.m_grid_balances.Name = "m_grid_balances";
			this.m_grid_balances.ReadOnly = true;
			this.m_grid_balances.RowHeadersVisible = false;
			this.m_grid_balances.Size = new System.Drawing.Size(164, 162);
			this.m_grid_balances.TabIndex = 0;
			// 
			// m_grid_positions
			// 
			this.m_grid_positions.AllowUserToAddRows = false;
			this.m_grid_positions.AllowUserToDeleteRows = false;
			this.m_grid_positions.AllowUserToOrderColumns = true;
			this.m_grid_positions.AllowUserToResizeRows = false;
			this.m_grid_positions.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
			this.m_grid_positions.BackgroundColor = System.Drawing.SystemColors.Window;
			this.m_grid_positions.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.None;
			this.m_grid_positions.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			dataGridViewCellStyle4.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle4.BackColor = System.Drawing.SystemColors.Window;
			dataGridViewCellStyle4.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			dataGridViewCellStyle4.ForeColor = System.Drawing.SystemColors.ControlText;
			dataGridViewCellStyle4.SelectionBackColor = System.Drawing.SystemColors.Highlight;
			dataGridViewCellStyle4.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
			dataGridViewCellStyle4.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
			this.m_grid_positions.DefaultCellStyle = dataGridViewCellStyle4;
			this.m_grid_positions.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_grid_positions.Location = new System.Drawing.Point(0, 0);
			this.m_grid_positions.Margin = new System.Windows.Forms.Padding(0);
			this.m_grid_positions.Name = "m_grid_positions";
			this.m_grid_positions.ReadOnly = true;
			this.m_grid_positions.RowHeadersVisible = false;
			this.m_grid_positions.Size = new System.Drawing.Size(490, 162);
			this.m_grid_positions.TabIndex = 1;
			// 
			// m_log
			// 
			this.m_log.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_log.Freeze = false;
			this.m_log.LineDelimiter = '';
			this.m_log.Location = new System.Drawing.Point(0, 0);
			this.m_log.LogEntryPattern = null;
			this.m_log.LogFilepath = null;
			this.m_log.Margin = new System.Windows.Forms.Padding(0);
			this.m_log.MaxLines = 500;
			this.m_log.Name = "m_log";
			this.m_log.PopOutOnNewMessages = true;
			this.m_log.Size = new System.Drawing.Size(658, 284);
			this.m_log.TabIndex = 2;
			this.m_log.Title = "LogControl";
			// 
			// m_menu
			// 
			this.m_menu.Dock = System.Windows.Forms.DockStyle.None;
			this.m_menu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_menu_file});
			this.m_menu.Location = new System.Drawing.Point(0, 0);
			this.m_menu.Name = "m_menu";
			this.m_menu.Size = new System.Drawing.Size(674, 24);
			this.m_menu.TabIndex = 0;
			this.m_menu.Text = "menuStrip1";
			// 
			// m_menu_file
			// 
			this.m_menu_file.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.m_menu_file_show_pairs,
            this.m_menu_file_show_loops,
            this.toolStripSeparator1,
            this.m_menu_file_test,
            this.m_menu_file_sep0,
            this.m_menu_file_exit});
			this.m_menu_file.Name = "m_menu_file";
			this.m_menu_file.Size = new System.Drawing.Size(37, 20);
			this.m_menu_file.Text = "&File";
			// 
			// m_menu_file_show_pairs
			// 
			this.m_menu_file_show_pairs.Name = "m_menu_file_show_pairs";
			this.m_menu_file_show_pairs.Size = new System.Drawing.Size(138, 22);
			this.m_menu_file_show_pairs.Text = "Show &Pairs";
			// 
			// m_menu_file_show_loops
			// 
			this.m_menu_file_show_loops.Name = "m_menu_file_show_loops";
			this.m_menu_file_show_loops.Size = new System.Drawing.Size(138, 22);
			this.m_menu_file_show_loops.Text = "Show &Loops";
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(135, 6);
			// 
			// m_menu_file_test
			// 
			this.m_menu_file_test.Name = "m_menu_file_test";
			this.m_menu_file_test.Size = new System.Drawing.Size(138, 22);
			this.m_menu_file_test.Text = "&TEST";
			// 
			// m_menu_file_sep0
			// 
			this.m_menu_file_sep0.Name = "m_menu_file_sep0";
			this.m_menu_file_sep0.Size = new System.Drawing.Size(135, 6);
			// 
			// m_menu_file_exit
			// 
			this.m_menu_file_exit.Name = "m_menu_file_exit";
			this.m_menu_file_exit.Size = new System.Drawing.Size(138, 22);
			this.m_menu_file_exit.Text = "E&xit";
			// 
			// m_split3
			// 
			this.m_split3.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_split3.Location = new System.Drawing.Point(0, 0);
			this.m_split3.Margin = new System.Windows.Forms.Padding(0);
			this.m_split3.Name = "m_split3";
			// 
			// m_split3.Panel1
			// 
			this.m_split3.Panel1.Controls.Add(this.m_grid_coins);
			// 
			// m_split3.Panel2
			// 
			this.m_split3.Panel2.Controls.Add(this.m_table0);
			this.m_split3.Size = new System.Drawing.Size(658, 138);
			this.m_split3.SplitterDistance = 164;
			this.m_split3.TabIndex = 2;
			// 
			// MainUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(674, 638);
			this.Controls.Add(this.m_tsc);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MainMenuStrip = this.m_menu;
			this.Name = "MainUI";
			this.Text = "Coin Flip";
			this.m_tsc.BottomToolStripPanel.ResumeLayout(false);
			this.m_tsc.BottomToolStripPanel.PerformLayout();
			this.m_tsc.ContentPanel.ResumeLayout(false);
			this.m_tsc.TopToolStripPanel.ResumeLayout(false);
			this.m_tsc.TopToolStripPanel.PerformLayout();
			this.m_tsc.ResumeLayout(false);
			this.m_tsc.PerformLayout();
			this.m_ss.ResumeLayout(false);
			this.m_ss.PerformLayout();
			this.m_split0.Panel1.ResumeLayout(false);
			this.m_split0.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.m_split0)).EndInit();
			this.m_split0.ResumeLayout(false);
			this.m_table0.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.m_grid_exchanges)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.m_grid_coins)).EndInit();
			this.m_split1.Panel1.ResumeLayout(false);
			this.m_split1.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.m_split1)).EndInit();
			this.m_split1.ResumeLayout(false);
			this.m_split2.Panel1.ResumeLayout(false);
			this.m_split2.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.m_split2)).EndInit();
			this.m_split2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.m_grid_balances)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.m_grid_positions)).EndInit();
			this.m_menu.ResumeLayout(false);
			this.m_menu.PerformLayout();
			this.m_split3.Panel1.ResumeLayout(false);
			this.m_split3.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.m_split3)).EndInit();
			this.m_split3.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion
	}

	#region Helper Structs
	public class ListElementProxy<T>
	{
		private readonly List<T> m_list;
		private readonly int m_index;
		public ListElementProxy(List<T> list, int index)
		{
			m_list = list;
			m_index = index;
		}
		public T Value
		{
			get { return m_list[m_index]; }
			set { m_list[m_index] = value; }
		}
	}
	#endregion
}
