using System.Windows.Forms;
using Rylogic.Extn;
using Rylogic.Gui.WinForms;
using Rylogic.Utility;
using DataGridView = Rylogic.Gui.WinForms.DataGridView;
using Util = Rylogic.Utility.Util;

namespace CoinFlip
{
	public class SimulationUI :ToolForm
	{
		#region UI Elements
		private ValueBox m_tb_max_steps;
		private Label m_lbl_max_steps;
		private ValueBox m_tb_step_rate;
		private Label m_lbl_step_rate;
		private Button m_btn_ok;
		private Label m_lbl_spread;
		private ValueBox m_tb_spread;
		private TabControl m_tab_ctrl;
		private TabPage m_tab_funds;
		private DataGridView m_grid_funds;
		private ToolTip m_tt;
		#endregion

		public SimulationUI(Control parent, Settings settings)
			:base(parent, EPin.Centre)
		{
			InitializeComponent();
			Icon = (parent?.TopLevelControl as Form)?.Icon;
			HideOnClose = true;
			Settings = settings;

			SetupUI();
		}
		protected override void Dispose(bool disposing)
		{
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}

		/// <summary>Main settings</summary>
		private Settings Settings { get; set; }

		/// <summary></summary>
		private void SetupUI()
		{
			#region Fields

			// Max Steps
			m_tb_max_steps.ToolTip(m_tt, "The maximum number of candles into the past to allow testing from");
			m_tb_max_steps.ValueType = typeof(int);
			m_tb_max_steps.ValidateText = t => int.TryParse(t, out var v) && v > 0;
			m_tb_max_steps.Value = Settings.BackTesting.MaxSteps;
			m_tb_max_steps.ValueCommitted += (s,a) =>
			{
				Settings.BackTesting.MaxSteps = (int)m_tb_max_steps.Value;
			};

			// Step Rate
			m_tb_step_rate.ToolTip(m_tt, "The simulation step rate (in steps per candle)");
			m_tb_step_rate.ValueType = typeof(double);
			m_tb_step_rate.ValidateText = t => double.TryParse(t, out var v) && v >= 1;
			m_tb_step_rate.Value = Settings.BackTesting.StepsPerCandle;
			m_tb_step_rate.ValueCommitted += (s,a) =>
			{
				Settings.BackTesting.StepsPerCandle = (double)m_tb_step_rate.Value;
			};

			// Spread
			m_tb_spread.ToolTip(m_tt, "The spread to use when simulating prices");
			m_tb_spread.ValueType = typeof(double);
			m_tb_spread.ValidateText = t => double.TryParse(t, out var v) && v >= 0 && v <= 100;
			m_tb_spread.Value = Settings.BackTesting.SpreadPC;
			m_tb_spread.ValueCommitted += (s,a) =>
			{
				Settings.BackTesting.SpreadPC = (double)m_tb_spread.Value;
			};

			#endregion

			#region Grid Funds
			m_grid_funds.AutoGenerateColumns = false;
			m_grid_funds.Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Currency",
				Name = nameof(Settings.CoinData.Symbol),
				DataPropertyName = nameof(Settings.CoinData.Symbol),
			});
			m_grid_funds.Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Initial Balance",
				Name = nameof(Settings.CoinData.BackTestingInitialBalance),
				DataPropertyName = nameof(Settings.CoinData.BackTestingInitialBalance),
			});
			m_grid_funds.DataSource = Settings.Coins;
			#endregion
		}

		#region Windows Form Designer generated code
		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.m_tb_max_steps = new Rylogic.Gui.WinForms.ValueBox();
			this.m_lbl_max_steps = new System.Windows.Forms.Label();
			this.m_tb_step_rate = new Rylogic.Gui.WinForms.ValueBox();
			this.m_lbl_step_rate = new System.Windows.Forms.Label();
			this.m_tt = new System.Windows.Forms.ToolTip(this.components);
			this.m_btn_ok = new System.Windows.Forms.Button();
			this.m_lbl_spread = new System.Windows.Forms.Label();
			this.m_tb_spread = new Rylogic.Gui.WinForms.ValueBox();
			this.m_tab_ctrl = new System.Windows.Forms.TabControl();
			this.m_tab_funds = new System.Windows.Forms.TabPage();
			this.m_grid_funds = new Rylogic.Gui.WinForms.DataGridView();
			this.m_tab_ctrl.SuspendLayout();
			this.m_tab_funds.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_grid_funds)).BeginInit();
			this.SuspendLayout();
			// 
			// m_tb_max_steps
			// 
			this.m_tb_max_steps.BackColor = System.Drawing.Color.White;
			this.m_tb_max_steps.BackColorInvalid = System.Drawing.Color.White;
			this.m_tb_max_steps.BackColorValid = System.Drawing.Color.White;
			this.m_tb_max_steps.CommitValueOnFocusLost = true;
			this.m_tb_max_steps.ForeColor = System.Drawing.Color.Gray;
			this.m_tb_max_steps.ForeColorInvalid = System.Drawing.Color.Gray;
			this.m_tb_max_steps.ForeColorValid = System.Drawing.Color.Black;
			this.m_tb_max_steps.Location = new System.Drawing.Point(15, 25);
			this.m_tb_max_steps.Name = "m_tb_max_steps";
			this.m_tb_max_steps.Size = new System.Drawing.Size(74, 20);
			this.m_tb_max_steps.TabIndex = 0;
			this.m_tb_max_steps.UseValidityColours = true;
			this.m_tb_max_steps.Value = null;
			// 
			// m_lbl_max_steps
			// 
			this.m_lbl_max_steps.AutoSize = true;
			this.m_lbl_max_steps.Location = new System.Drawing.Point(12, 9);
			this.m_lbl_max_steps.Name = "m_lbl_max_steps";
			this.m_lbl_max_steps.Size = new System.Drawing.Size(77, 13);
			this.m_lbl_max_steps.TabIndex = 1;
			this.m_lbl_max_steps.Text = "History Range:";
			// 
			// m_tb_step_rate
			// 
			this.m_tb_step_rate.BackColor = System.Drawing.Color.White;
			this.m_tb_step_rate.BackColorInvalid = System.Drawing.Color.White;
			this.m_tb_step_rate.BackColorValid = System.Drawing.Color.White;
			this.m_tb_step_rate.CommitValueOnFocusLost = true;
			this.m_tb_step_rate.ForeColor = System.Drawing.Color.Gray;
			this.m_tb_step_rate.ForeColorInvalid = System.Drawing.Color.Gray;
			this.m_tb_step_rate.ForeColorValid = System.Drawing.Color.Black;
			this.m_tb_step_rate.Location = new System.Drawing.Point(103, 25);
			this.m_tb_step_rate.Name = "m_tb_step_rate";
			this.m_tb_step_rate.Size = new System.Drawing.Size(74, 20);
			this.m_tb_step_rate.TabIndex = 1;
			this.m_tb_step_rate.UseValidityColours = true;
			this.m_tb_step_rate.Value = null;
			// 
			// m_lbl_step_rate
			// 
			this.m_lbl_step_rate.AutoSize = true;
			this.m_lbl_step_rate.Location = new System.Drawing.Point(100, 9);
			this.m_lbl_step_rate.Name = "m_lbl_step_rate";
			this.m_lbl_step_rate.Size = new System.Drawing.Size(58, 13);
			this.m_lbl_step_rate.TabIndex = 3;
			this.m_lbl_step_rate.Text = "Step Rate:";
			// 
			// m_btn_ok
			// 
			this.m_btn_ok.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_ok.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btn_ok.Location = new System.Drawing.Point(338, 289);
			this.m_btn_ok.Name = "m_btn_ok";
			this.m_btn_ok.Size = new System.Drawing.Size(75, 23);
			this.m_btn_ok.TabIndex = 2;
			this.m_btn_ok.Text = "OK";
			this.m_btn_ok.UseVisualStyleBackColor = true;
			// 
			// m_lbl_spread
			// 
			this.m_lbl_spread.AutoSize = true;
			this.m_lbl_spread.Location = new System.Drawing.Point(186, 9);
			this.m_lbl_spread.Name = "m_lbl_spread";
			this.m_lbl_spread.Size = new System.Drawing.Size(61, 13);
			this.m_lbl_spread.TabIndex = 6;
			this.m_lbl_spread.Text = "Spread (%):";
			// 
			// m_tb_spread
			// 
			this.m_tb_spread.BackColor = System.Drawing.Color.White;
			this.m_tb_spread.BackColorInvalid = System.Drawing.Color.White;
			this.m_tb_spread.BackColorValid = System.Drawing.Color.White;
			this.m_tb_spread.CommitValueOnFocusLost = true;
			this.m_tb_spread.ForeColor = System.Drawing.Color.Gray;
			this.m_tb_spread.ForeColorInvalid = System.Drawing.Color.Gray;
			this.m_tb_spread.ForeColorValid = System.Drawing.Color.Black;
			this.m_tb_spread.Location = new System.Drawing.Point(189, 25);
			this.m_tb_spread.Name = "m_tb_spread";
			this.m_tb_spread.Size = new System.Drawing.Size(74, 20);
			this.m_tb_spread.TabIndex = 7;
			this.m_tb_spread.UseValidityColours = true;
			this.m_tb_spread.Value = null;
			// 
			// m_tab_ctrl
			// 
			this.m_tab_ctrl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_tab_ctrl.Controls.Add(this.m_tab_funds);
			this.m_tab_ctrl.Location = new System.Drawing.Point(12, 51);
			this.m_tab_ctrl.Name = "m_tab_ctrl";
			this.m_tab_ctrl.SelectedIndex = 0;
			this.m_tab_ctrl.Size = new System.Drawing.Size(401, 232);
			this.m_tab_ctrl.TabIndex = 9;
			// 
			// m_tab_funds
			// 
			this.m_tab_funds.Controls.Add(this.m_grid_funds);
			this.m_tab_funds.Location = new System.Drawing.Point(4, 22);
			this.m_tab_funds.Name = "m_tab_funds";
			this.m_tab_funds.Padding = new System.Windows.Forms.Padding(3);
			this.m_tab_funds.Size = new System.Drawing.Size(393, 206);
			this.m_tab_funds.TabIndex = 1;
			this.m_tab_funds.Text = "Funds";
			this.m_tab_funds.UseVisualStyleBackColor = true;
			// 
			// m_grid_funds
			// 
			this.m_grid_funds.AllowUserToAddRows = false;
			this.m_grid_funds.AllowUserToDeleteRows = false;
			this.m_grid_funds.AllowUserToResizeRows = false;
			this.m_grid_funds.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
			this.m_grid_funds.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
			this.m_grid_funds.BackgroundColor = System.Drawing.SystemColors.Window;
			this.m_grid_funds.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.None;
			this.m_grid_funds.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.m_grid_funds.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_grid_funds.Location = new System.Drawing.Point(3, 3);
			this.m_grid_funds.Margin = new System.Windows.Forms.Padding(0);
			this.m_grid_funds.MultiSelect = false;
			this.m_grid_funds.Name = "m_grid_funds";
			this.m_grid_funds.RowHeadersVisible = false;
			this.m_grid_funds.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.m_grid_funds.Size = new System.Drawing.Size(387, 200);
			this.m_grid_funds.TabIndex = 9;
			// 
			// SimulationUI
			// 
			this.AcceptButton = this.m_btn_ok;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(425, 324);
			this.Controls.Add(this.m_tab_ctrl);
			this.Controls.Add(this.m_tb_spread);
			this.Controls.Add(this.m_lbl_spread);
			this.Controls.Add(this.m_btn_ok);
			this.Controls.Add(this.m_lbl_step_rate);
			this.Controls.Add(this.m_tb_step_rate);
			this.Controls.Add(this.m_lbl_max_steps);
			this.Controls.Add(this.m_tb_max_steps);
			this.MinimumSize = new System.Drawing.Size(386, 258);
			this.Name = "SimulationUI";
			this.Text = "Back Testing";
			this.m_tab_ctrl.ResumeLayout(false);
			this.m_tab_funds.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.m_grid_funds)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion
	}
}
