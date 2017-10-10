using System.Windows.Forms;
using pr.extn;
using pr.gui;
using pr.util;

namespace CoinFlip
{
	public class SimulationUI :ToolForm
	{
		private readonly Settings.BackTestingSettings m_settings;

		#region UI Elements
		private ValueBox m_tb_max_steps;
		private Label m_lbl_max_steps;
		private ValueBox m_tb_step_rate;
		private Label m_lbl_step_rate;
		private Button m_btn_ok;
		private Label m_lbl_spread;
		private ValueBox m_tb_spread;
		private pr.gui.DataGridView m_grid_pairs;
		private TabControl m_tab_ctrl;
		private TabPage m_tab_pairs;
		private TabPage m_tab_funds;
		private pr.gui.DataGridView m_grid_funds;
		private Label m_lbl_substeps;
		private ValueBox m_tb_subdivisions;
		private ToolTip m_tt;
		#endregion

		public SimulationUI(Control parent, Settings.BackTestingSettings settings)
			:base(parent, EPin.Centre)
		{
			InitializeComponent();
			Icon = (parent?.TopLevelControl as Form)?.Icon;
			HideOnClose = true;
			m_settings = settings;

			SetupUI();
		}
		protected override void Dispose(bool disposing)
		{
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}

		/// <summary></summary>
		private void SetupUI()
		{
			#region Fields

			// Max Steps
			m_tb_max_steps.ToolTip(m_tt, "The maximum number of candles into the past to allow testing from");
			m_tb_max_steps.ValueType = typeof(int);
			m_tb_max_steps.ValidateText = t => int.TryParse(t, out var v) && v > 0;
			m_tb_max_steps.Value = m_settings.MaxSteps;
			m_tb_max_steps.ValueCommitted += (s,a) =>
			{
				m_settings.MaxSteps = (int)m_tb_max_steps.Value;
			};

			// Step Rate
			m_tb_step_rate.ToolTip(m_tt, "The rate (in candle updates per second) that the back testing runs at");
			m_tb_step_rate.ValueType = typeof(double);
			m_tb_step_rate.ValidateText = t => double.TryParse(t, out var v) && v > 0;
			m_tb_step_rate.Value = m_settings.StepRate;
			m_tb_step_rate.ValueCommitted += (s,a) =>
			{
				m_settings.StepRate = (double)m_tb_step_rate.Value;
			};

			// Step Subdivisions
			m_tb_subdivisions.ToolTip(m_tt, "The number of steps per candle. Used to simulate higher resolution price movement");
			m_tb_subdivisions.ValueType = typeof(int);
			m_tb_subdivisions.ValidateText = t => int.TryParse(t, out var v) && v > 0;
			m_tb_subdivisions.Value = m_settings.SubStepsPerCandle;
			m_tb_subdivisions.ValueCommitted += (s,a) =>
			{
				m_settings.SubStepsPerCandle = (int)m_tb_step_rate.Value;
			};

			// Spread
			m_tb_spread.ToolTip(m_tt, "The spread to use when simulating prices");
			m_tb_spread.ValueType = typeof(double);
			m_tb_spread.ValidateText = t => double.TryParse(t, out var v) && v >= 0 && v <= 100;
			m_tb_spread.Value = m_settings.SpreadPC;
			m_tb_spread.ValueCommitted += (s,a) =>
			{
				m_settings.SpreadPC = (double)m_tb_spread.Value;
			};

			#endregion

			#region Grid Pairs
			m_grid_pairs.AutoGenerateColumns = false;
			m_grid_pairs.Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Pair",
				Name = nameof(TradePair.NameWithExchange),
				DataPropertyName = nameof(TradePair.NameWithExchange),
			});
			m_grid_pairs.Columns.Add(new DataGridViewImageColumn
			{
				HeaderText = "Active",
				Name = nameof(Settings.BackTestingSettings.PairData.Active),
				DataPropertyName = nameof(Settings.BackTestingSettings.PairData.Active),
				AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader,
				ImageLayout = DataGridViewImageCellLayout.Normal,
				FillWeight = 0.1f,
			});
			#endregion

			#region Grid Funds
			m_grid_funds.AutoGenerateColumns = false;
			m_grid_funds.Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Currency",
				Name = nameof(Coin.Symbol),
				DataPropertyName = nameof(Coin.Symbol),
			});
			m_grid_pairs.Columns.Add(new DataGridViewTextBoxColumn
			{
				HeaderText = "Initial Balance",
				Name = nameof(Settings.BackTestingSettings.CoinData.InitialBalance),
				DataPropertyName = nameof(Settings.BackTestingSettings.CoinData.InitialBalance),
			});
			#endregion
		}

		#region Windows Form Designer generated code
		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.m_tb_max_steps = new pr.gui.ValueBox();
			this.m_lbl_max_steps = new System.Windows.Forms.Label();
			this.m_tb_step_rate = new pr.gui.ValueBox();
			this.m_lbl_step_rate = new System.Windows.Forms.Label();
			this.m_tt = new System.Windows.Forms.ToolTip(this.components);
			this.m_btn_ok = new System.Windows.Forms.Button();
			this.m_lbl_spread = new System.Windows.Forms.Label();
			this.m_tb_spread = new pr.gui.ValueBox();
			this.m_grid_pairs = new pr.gui.DataGridView();
			this.m_tab_ctrl = new System.Windows.Forms.TabControl();
			this.m_tab_pairs = new System.Windows.Forms.TabPage();
			this.m_tab_funds = new System.Windows.Forms.TabPage();
			this.m_grid_funds = new pr.gui.DataGridView();
			this.m_lbl_substeps = new System.Windows.Forms.Label();
			this.m_tb_subdivisions = new pr.gui.ValueBox();
			((System.ComponentModel.ISupportInitialize)(this.m_grid_pairs)).BeginInit();
			this.m_tab_ctrl.SuspendLayout();
			this.m_tab_pairs.SuspendLayout();
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
			this.m_lbl_spread.Location = new System.Drawing.Point(276, 9);
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
			this.m_tb_spread.Location = new System.Drawing.Point(279, 25);
			this.m_tb_spread.Name = "m_tb_spread";
			this.m_tb_spread.Size = new System.Drawing.Size(74, 20);
			this.m_tb_spread.TabIndex = 7;
			this.m_tb_spread.UseValidityColours = true;
			this.m_tb_spread.Value = null;
			// 
			// m_grid_pairs
			// 
			this.m_grid_pairs.AllowUserToAddRows = false;
			this.m_grid_pairs.AllowUserToDeleteRows = false;
			this.m_grid_pairs.AllowUserToResizeRows = false;
			this.m_grid_pairs.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
			this.m_grid_pairs.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells;
			this.m_grid_pairs.BackgroundColor = System.Drawing.SystemColors.Window;
			this.m_grid_pairs.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.None;
			this.m_grid_pairs.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.m_grid_pairs.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_grid_pairs.Location = new System.Drawing.Point(3, 3);
			this.m_grid_pairs.Margin = new System.Windows.Forms.Padding(0);
			this.m_grid_pairs.MultiSelect = false;
			this.m_grid_pairs.Name = "m_grid_pairs";
			this.m_grid_pairs.ReadOnly = true;
			this.m_grid_pairs.RowHeadersVisible = false;
			this.m_grid_pairs.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.m_grid_pairs.Size = new System.Drawing.Size(387, 200);
			this.m_grid_pairs.TabIndex = 8;
			// 
			// m_tab_ctrl
			// 
			this.m_tab_ctrl.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_tab_ctrl.Controls.Add(this.m_tab_pairs);
			this.m_tab_ctrl.Controls.Add(this.m_tab_funds);
			this.m_tab_ctrl.Location = new System.Drawing.Point(12, 51);
			this.m_tab_ctrl.Name = "m_tab_ctrl";
			this.m_tab_ctrl.SelectedIndex = 0;
			this.m_tab_ctrl.Size = new System.Drawing.Size(401, 232);
			this.m_tab_ctrl.TabIndex = 9;
			// 
			// m_tab_pairs
			// 
			this.m_tab_pairs.Controls.Add(this.m_grid_pairs);
			this.m_tab_pairs.Location = new System.Drawing.Point(4, 22);
			this.m_tab_pairs.Name = "m_tab_pairs";
			this.m_tab_pairs.Padding = new System.Windows.Forms.Padding(3);
			this.m_tab_pairs.Size = new System.Drawing.Size(393, 206);
			this.m_tab_pairs.TabIndex = 0;
			this.m_tab_pairs.Text = "Pairs";
			this.m_tab_pairs.UseVisualStyleBackColor = true;
			// 
			// m_tab_funds
			// 
			this.m_tab_funds.Controls.Add(this.m_grid_funds);
			this.m_tab_funds.Location = new System.Drawing.Point(4, 22);
			this.m_tab_funds.Name = "m_tab_funds";
			this.m_tab_funds.Padding = new System.Windows.Forms.Padding(3);
			this.m_tab_funds.Size = new System.Drawing.Size(379, 202);
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
			this.m_grid_funds.ReadOnly = true;
			this.m_grid_funds.RowHeadersVisible = false;
			this.m_grid_funds.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.m_grid_funds.Size = new System.Drawing.Size(373, 196);
			this.m_grid_funds.TabIndex = 9;
			// 
			// m_lbl_substeps
			// 
			this.m_lbl_substeps.AutoSize = true;
			this.m_lbl_substeps.Location = new System.Drawing.Point(188, 9);
			this.m_lbl_substeps.Name = "m_lbl_substeps";
			this.m_lbl_substeps.Size = new System.Drawing.Size(69, 13);
			this.m_lbl_substeps.TabIndex = 10;
			this.m_lbl_substeps.Text = "Subdivisions:";
			// 
			// m_tb_subdivisions
			// 
			this.m_tb_subdivisions.BackColor = System.Drawing.Color.White;
			this.m_tb_subdivisions.BackColorInvalid = System.Drawing.Color.White;
			this.m_tb_subdivisions.BackColorValid = System.Drawing.Color.White;
			this.m_tb_subdivisions.CommitValueOnFocusLost = true;
			this.m_tb_subdivisions.ForeColor = System.Drawing.Color.Gray;
			this.m_tb_subdivisions.ForeColorInvalid = System.Drawing.Color.Gray;
			this.m_tb_subdivisions.ForeColorValid = System.Drawing.Color.Black;
			this.m_tb_subdivisions.Location = new System.Drawing.Point(191, 25);
			this.m_tb_subdivisions.Name = "m_tb_subdivisions";
			this.m_tb_subdivisions.Size = new System.Drawing.Size(74, 20);
			this.m_tb_subdivisions.TabIndex = 11;
			this.m_tb_subdivisions.UseValidityColours = true;
			this.m_tb_subdivisions.Value = null;
			// 
			// SimulationUI
			// 
			this.AcceptButton = this.m_btn_ok;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(425, 324);
			this.Controls.Add(this.m_tb_subdivisions);
			this.Controls.Add(this.m_lbl_substeps);
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
			((System.ComponentModel.ISupportInitialize)(this.m_grid_pairs)).EndInit();
			this.m_tab_ctrl.ResumeLayout(false);
			this.m_tab_pairs.ResumeLayout(false);
			this.m_tab_funds.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.m_grid_funds)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion
	}
}
