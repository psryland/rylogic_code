using System;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using cAlgo.API;
using pr.attrib;
using pr.extn;
using pr.gui;
using pr.maths;
using pr.util;

namespace Tradee
{
	public class TradeeBotUI :Form
	{
		#region UI Elements
		private pr.gui.ListBox m_lb_symbols;
		private pr.gui.ComboBox m_cb_symbols;
		private Panel m_panel_add;
		private System.Windows.Forms.Timer m_timer;
		private Button m_btn_add;
		#endregion

		public TradeeBotUI(Robot robot)
		{
			InitializeComponent();
			DoubleBuffered = true;
			StartPosition = FormStartPosition.Manual;
			Location = Cursor.Position;

			var settings_path = Util.ResolveAppDataPath("Rylogic","Tradee", "tradeebot_settings.xml");
			Model = new TradeeBotModel(robot, new Settings(settings_path) { AutoSaveOnChanges = true });

			SetupUI();
			UpdateUI();

			m_timer.Interval = 500;
			m_timer.Tick += (s,a) => Model.Step();
			m_timer.Enabled = true;
		}
		protected override void Dispose(bool disposing)
		{
			Model = null;
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}

		/// <summary>App settings</summary>
		private Settings Settings
		{
			get { return Model.Settings; }
		}

		/// <summary>Tradee bot app logic</summary>
		private TradeeBotModel Model
		{
			get { return m_impl_model; }
			set
			{
				if (m_impl_model == value) return;
				if (m_impl_model != null)
				{
					m_impl_model.Transmitters.ListChanging -= UpdateUI;
					m_impl_model.ConnectionChanged -= UpdateUI;
					m_impl_model.DataPosted -= UpdateUI;
					Util.Dispose(ref m_impl_model);
				}
				m_impl_model = value;
				if (m_impl_model != null)
				{
					m_impl_model.DataPosted += UpdateUI;
					m_impl_model.ConnectionChanged += UpdateUI;
					m_impl_model.Transmitters.ListChanging += UpdateUI;
				}
			}
		}
		private TradeeBotModel m_impl_model;

		/// <summary>Show a dialog to select the desired time frames</summary>
		public void ShowTimeFrameSelection(Transmitter trans)
		{
			// Get the list of currently selected time frames
			var existing = trans.TimeFrames;

			// Show a dialog for selecting time frames
			var dlg = new ToolForm
			{
				Text            = "Select Time Frames",
				FormBorderStyle = FormBorderStyle.SizableToolWindow,
				StartPosition   = FormStartPosition.CenterParent,
				ShowInTaskbar   = false,
				Size            = new Size(250, 360),
			};
			var chklist = dlg.Controls.Add2(new CheckedListBox { Dock = DockStyle.Fill });
			foreach (var tf in Enum<ETimeFrame>.Values)
				chklist.Items.Add(tf, existing.Contains(tf));
			dlg.ShowDialog();

			// Update the list of time frames
			var new_list = chklist.CheckedItems.OfType<ETimeFrame>().ToArray();
			trans.TimeFrames = new_list;

			// Make this list the new default set
			Settings.DefaultTimeFrames = new_list;
		}

		/// <summary>Set up UI elements</summary>
		private void SetupUI()
		{
			m_cb_symbols.AutoCompleteMode = AutoCompleteMode.Suggest;
			m_cb_symbols.AutoCompleteSource = AutoCompleteSource.ListItems;
			m_cb_symbols.DataSource = Misc.KnownSymbols.Keys.ToArray();
			m_cb_symbols.Format += (s,a) =>
			{
				var sym = (string)a.ListItem;
				a.Value = "{0} - {1}".Fmt(sym, Misc.KnownSymbols[sym].Desc);
			};

			// Add trading pair
			m_btn_add.Click += (s,a) =>
			{
				Model.AddTransmitter((string)m_cb_symbols.SelectedItem);
			};

			// Show current pairs
			m_lb_symbols.BackColor = SystemColors.ControlDark;
			m_lb_symbols.DrawMode = DrawMode.OwnerDrawFixed;
			m_lb_symbols.DataSource = Model.Transmitters;
			m_lb_symbols.ItemHeight = 58;
			m_lb_symbols.DrawItem += (s,a) =>
			{
				if (Model == null || a.Index < 0 || a.Index >= Model.Transmitters.Count)
				{
					a.Graphics.FillRectangle(SystemBrushes.ControlLightLight, a.Bounds);
					return;
				}

				var item = Model.Transmitters[a.Index];
				var bnds = a.Bounds.Inflated(0, 0, -1, -1);
				var col = Bit.AllSet(a.State, DrawItemState.Selected) ? Brushes.WhiteSmoke : !item.Enabled ? SystemBrushes.ControlLight : SystemBrushes.ControlLightLight;
				var x = bnds.Left + 2f;
				var y = bnds.Top + 2f;
		
				a.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
				a.Graphics.FillRectangle(SystemBrushes.ControlDark, a.Bounds);
				a.Graphics.FillRectangleRounded(col, bnds, 6f);
				a.Graphics.DrawRectangleRounded(Pens.Black, bnds, 6f);

				{ // Paint the symbol name
					var str = "{0} - {1}".Fmt(item.SymbolCode, item.SymbolDescription.Desc);
					var font = a.Font.Dup(FontStyle.Bold);
					var sz = a.Graphics.MeasureString(str, font);
					a.Graphics.DrawString(str, font, !item.Enabled ? SystemBrushes.ControlDark : Brushes.Black, x, y);
					y += sz.Height;
				}
				{// Paint the time frames
					var tfs = item.Data.Keys.Select(t => t.ToTradeeTimeframe()).OrderBy(t => t).ToArray();
					var str = "Time Frames: {0}".Fmt(string.Join(" ", tfs.Select(t => t.Desc())));
					var sz = a.Graphics.MeasureString(str, a.Font);
					a.Graphics.DrawString(str, a.Font, !item.Enabled ? SystemBrushes.ControlDark : item.RequestingSeriesData ? Brushes.DarkRed : Brushes.Green, x, y);
					y += sz.Height;
				}
				{// Paint the last update time
					var tss = item.Data.Values.Select(v => v.LastTransmitUTC).ToArray();
					var ts  = tss.Length != 0 ? tss.MaxBy(t => t) : DateTimeOffset.MinValue;
					var str = "Last Update: {0}".Fmt(ts != DateTimeOffset.MinValue ? ts.ToString("yyyy/MM/dd HH:mm:ss") : "never");
					var sz = a.Graphics.MeasureString(str, a.Font);
					a.Graphics.DrawString(str, a.Font, !item.Enabled ? SystemBrushes.ControlDark : Brushes.Blue, x, y);
					y += sz.Height;
				}
				{// Paint the status
					var msg = item.StatusMsg;
					var str = "Status: {0}".Fmt(msg);
					var sz = a.Graphics.MeasureString(str, a.Font);
					a.Graphics.DrawString(str, a.Font, !item.Enabled ? SystemBrushes.ControlDark : SystemBrushes.ControlDarkDark, x, y);
					y += sz.Height;
				}
			};
			m_lb_symbols.MouseDown += (s,a) =>
			{
				if (a.Button == MouseButtons.Right)
					ShowContextMenu(a.Location);
			};
		}

		/// <summary>Update UI Elements</summary>
		private void UpdateUI(object sender = null, EventArgs args = null)
		{
			m_lb_symbols.Invalidate();

			// Set the title
			Text = "Tradee Bot - {0}".Fmt(Model.IsConnected ? "Connected" : "Disconnected");
		}

		/// <summary>Create a context menu for the list box</summary>
		private void ShowContextMenu(Point pt)
		{
			var cmenu = new ContextMenuStrip();
			var idx = m_lb_symbols.IndexFromPoint(pt);
			if (idx < 0 || idx >= Model.Transmitters.Count)
				return;

			var item = Model.Transmitters[idx];
			using (cmenu.SuspendLayout(true))
			{
				{
					var opt = cmenu.Items.Add2(new ToolStripMenuItem(item.Enabled ? "Disable" : "Enable"));
					opt.Click += (s,a) =>
					{
						item.Enabled = !item.Enabled;
					};
				}
				{
					var opt = cmenu.Items.Add2(new ToolStripMenuItem("Remove"));
					opt.Click += (s,a) =>
					{
						Model.Transmitters.RemoveAt(idx);
					};
				}
				{
					var opt = cmenu.Items.Add2(new ToolStripMenuItem("Choose Time Frames"));
					opt.Click += (s,a) =>
					{
						ShowTimeFrameSelection(item);
					};
				}
				//{
				//	var opt = cmenu.Items.Add2(new ToolStripMenuItem("Send Historic Data"));
				//	opt.DropDownItemClicked += (s,a) =>
				//	{
				//		var item = Model.Transmitters[idx];
				//		item.SendHistoricData();
				//	};
				//}

			}
			cmenu.Show(m_lb_symbols, pt);
		}

		#region Windows Form Designer generated code
		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.m_lb_symbols = new pr.gui.ListBox();
			this.m_cb_symbols = new pr.gui.ComboBox();
			this.m_panel_add = new System.Windows.Forms.Panel();
			this.m_btn_add = new System.Windows.Forms.Button();
			this.m_timer = new System.Windows.Forms.Timer(this.components);
			this.m_panel_add.SuspendLayout();
			this.SuspendLayout();
			// 
			// m_lb_symbols
			// 
			this.m_lb_symbols.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.m_lb_symbols.DisplayProperty = null;
			this.m_lb_symbols.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_lb_symbols.FormattingEnabled = true;
			this.m_lb_symbols.IntegralHeight = false;
			this.m_lb_symbols.Location = new System.Drawing.Point(3, 34);
			this.m_lb_symbols.Name = "m_lb_symbols";
			this.m_lb_symbols.Size = new System.Drawing.Size(268, 316);
			this.m_lb_symbols.TabIndex = 0;
			// 
			// m_cb_symbols
			// 
			this.m_cb_symbols.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_cb_symbols.DisplayProperty = null;
			this.m_cb_symbols.DropDownWidth = 160;
			this.m_cb_symbols.FormattingEnabled = true;
			this.m_cb_symbols.Location = new System.Drawing.Point(3, 4);
			this.m_cb_symbols.Name = "m_cb_symbols";
			this.m_cb_symbols.PreserveSelectionThruFocusChange = false;
			this.m_cb_symbols.Size = new System.Drawing.Size(230, 21);
			this.m_cb_symbols.TabIndex = 1;
			// 
			// m_panel_add
			// 
			this.m_panel_add.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.m_panel_add.Controls.Add(this.m_cb_symbols);
			this.m_panel_add.Controls.Add(this.m_btn_add);
			this.m_panel_add.Dock = System.Windows.Forms.DockStyle.Top;
			this.m_panel_add.Location = new System.Drawing.Point(3, 3);
			this.m_panel_add.Name = "m_panel_add";
			this.m_panel_add.Size = new System.Drawing.Size(268, 31);
			this.m_panel_add.TabIndex = 2;
			// 
			// m_btn_add
			// 
			this.m_btn_add.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_add.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
			this.m_btn_add.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.m_btn_add.Location = new System.Drawing.Point(237, 3);
			this.m_btn_add.Margin = new System.Windows.Forms.Padding(0);
			this.m_btn_add.Name = "m_btn_add";
			this.m_btn_add.Size = new System.Drawing.Size(25, 23);
			this.m_btn_add.TabIndex = 2;
			this.m_btn_add.Text = "+";
			this.m_btn_add.UseVisualStyleBackColor = true;
			// 
			// TradeeBotUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(274, 353);
			this.Controls.Add(this.m_lb_symbols);
			this.Controls.Add(this.m_panel_add);
			this.MaximizeBox = false;
			this.Name = "TradeeBotUI";
			this.Padding = new System.Windows.Forms.Padding(3);
			this.ShowIcon = false;
			this.Text = "Tradee Bot";
			this.m_panel_add.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion
	}
}
