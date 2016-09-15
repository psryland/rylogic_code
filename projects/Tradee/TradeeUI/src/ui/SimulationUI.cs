using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using pr.extn;
using pr.gui;
using pr.maths;
using pr.util;

namespace Tradee
{
	public class SimulationUI :ToolForm
	{
		#region UI Elements
		private Label m_lbl_start_time;
		private Button m_btn_run;
		private ImageList m_il;
		private Button m_btn_reset;
		private Button m_btn_run_to_trade;
		private ToolTip m_tt;
		private Button m_btn_step_fwd;
		private pr.gui.ComboBox m_cb_step_size;
		private Label m_lbl_step_size;
		private Label m_lbl_sim_date;
		private pr.gui.DateTimePicker m_dtp_start_date;
		private Label m_lbl_start_balance;
		private TextBox m_tb_start_balance;
		private TrackBar m_tbar_start_date;
		private Panel m_panel_sim_controls;
		private Panel m_panel_initial_conditions;
		private ValueBox m_tb_step_rate;
		private Label m_lbl_step_rate;
		private Label label1;
		private TextBox m_tb_balance;
		private Panel m_panel_sim_results;
		private TextBox m_tb_avr_profit;
		private Label m_lbl_avr_profit;
		private Panel m_panel_step_size;
		private Timer m_timer_sim;
		private NumericUpDown m_spinner_tbar_counts;
		private Button m_btn_step_back;
		private TextBox m_tb_sim_time;
		#endregion

		public SimulationUI(MainUI main, MainModel model)
			:base(main, EPin.TopRight)
		{
			InitializeComponent();
			Model = model;
			HideOnClose = true;

			SetupUI();

			m_timer_sim.Tick += (s,a) => SimStep();
		}
		protected override void Dispose(bool disposing)
		{
			Model = null;
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}
		public override void Show(IWin32Window owner)
		{
			base.Show(owner);
			Model.SimActive = true;
			SimReset();
		}
		public override void Hide()
		{
			Model.SimActive = false;
			base.Hide();
		}

		/// <summary>Application settings</summary>
		public Settings Settings
		{
			[DebuggerStepThrough] get { return Model.Settings; }
		}

		/// <summary>App logic</summary>
		public MainModel Model
		{
			[DebuggerStepThrough] get { return m_model; }
			private set
			{
				if (m_model == value) return;
				if (m_model != null)
				{
					m_model.Simulation.SimTimeChanged -= UpdateUI;
				}
				m_model = value;
				if (m_model != null)
				{
					m_model.Simulation.SimTimeChanged += UpdateUI;
				}
			}
		}
		private MainModel m_model;

		/// <summary>The app logic behind the simulation</summary>
		public Simulation Sim
		{
			[DebuggerStepThrough] get { return Model.Simulation; }
		}

		/// <summary>Reset the simulation</summary>
		public void SimReset()
		{
			SimRunning = false;
			Sim.Reset();
			UpdateUI();
		}

		/// <summary>Go one step backward in time</summary>
		public void SimStepBack()
		{
			Sim.StepBack(1);

			UpdateUI();
		}

		/// <summary>Advance the sim by one step</summary>
		public void SimStep()
		{
			Sim.Step();

			// Reached the current time, stop
			if (Sim.UtcNow >= DateTimeOffset.UtcNow)
				SimRunning = false;

			UpdateUI();
		}

		/// <summary>Start/Stop the simulation</summary>
		public bool SimRunning
		{
			get { return Sim.Running; }
			private set
			{
				if (SimRunning == value) return;
				if (SimRunning)
				{
					m_timer_sim.Enabled = false;
				}
				Sim.Running = value;
				if (SimRunning)
				{
					m_timer_sim.Interval = (int)(1000 / Sim.StepRate);
					m_timer_sim.Enabled = true;
				}
				UpdateUI();
			}
		}

		/// <summary>Set up UI elements</summary>
		private void SetupUI()
		{
			// The step size
			m_cb_step_size.DataSource = Enum<ETimeFrame>.Values.Except(ETimeFrame.None).ToArray();
			m_cb_step_size.SelectedItem = Settings.Simulation.StepSize;
			m_cb_step_size.SelectedIndexChanged += (s,a) =>
			{
				if (m_updating_ui != 0) return;
				Sim.StepSize = Misc.TimeFrameToTimeSpan(1.0, (ETimeFrame)m_cb_step_size.SelectedValue);
				UpdateUI();
			};

			// Start time picker
			m_dtp_start_date.CustomFormat = "ddd' 'dd'-'MM'-'yyyy";
			m_dtp_start_date.Kind = DateTimeKind.Utc;
			m_dtp_start_date.Value = Sim.StartTime.DateTime.As(DateTimeKind.Utc);
			m_dtp_start_date.ValueChanged += (s,a) =>
			{
				if (m_updating_ui != 0) return;
				Sim.StartTime = m_dtp_start_date.Value;
				UpdateUI();
			};

			// Track bar counts
			const int initial_tbar_count = 1000;
			m_spinner_tbar_counts.Minimum = 0;
			m_spinner_tbar_counts.Maximum = int.MaxValue;
			m_spinner_tbar_counts.Value = initial_tbar_count;
			m_spinner_tbar_counts.ValueChanged += (s,a) =>
			{
				m_tbar_start_date.SetRange(0, (int)m_spinner_tbar_counts.Value);
				m_tbar_start_date.ValueClamped(m_tbar_start_date.Value);
			};

			// Start date track bar
			m_tbar_start_date.SetRange(0, initial_tbar_count);
			m_tbar_start_date.Value = m_tbar_start_date.Maximum;
			m_tbar_start_date.ValueChanged += (s,a) =>
			{
				if (m_updating_ui != 0) return;
				var i = m_tbar_start_date.Maximum - m_tbar_start_date.Value;
				Sim.StartTime = DateTimeOffset_.UtcToday - Misc.TimeFrameToTimeSpan(i, (ETimeFrame)m_cb_step_size.SelectedValue);
				UpdateUI();
			};

			// Step rate
			m_tb_step_rate.ToolTip(m_tt, "The number of increments per second (Units of Step Size)");
			m_tb_step_rate.ValueType = typeof(double);
			m_tb_step_rate.Value = Sim.StepRate;
			m_tb_step_rate.ValueChanged += (s,a) =>
			{
				if (m_updating_ui != 0) return;
				Sim.StepRate = Maths.Max((double)m_tb_step_rate.Value, 1.0);
				UpdateUI();
			};

			// Sim time text box
			m_tb_sim_time.ToolTip(m_tt, "The date that the simulation starts from");
			m_tb_sim_time.Text = Sim.StartTime.ToString("ddd dd-MM-yyyy  HH:mm:ss");

			// Button reset
			m_btn_reset.ToolTip(m_tt, "Reset the simulation time back to the start");
			m_btn_reset.Click += (s,a) =>
			{
				SimReset();
			};

			// Button run
			m_btn_run.ToolTip(m_tt, "Start/Stop the simulation running");
			m_btn_run.Click += (s,a) =>
			{
				SimRunning = !SimRunning;
			};

			// Step back
			m_btn_step_back.ToolTip(m_tt, "Step one candle backward");
			m_btn_step_back.Click += (s,a) =>
			{
				SimRunning = false;
				SimStepBack();
			};

			// Step
			m_btn_step_fwd.ToolTip(m_tt, "Step one candle forward");
			m_btn_step_fwd.Click += (s,a) =>
			{
				SimRunning = false;
				SimStep();
			};

			// Button run to next trade
			m_btn_run_to_trade.ToolTip(m_tt, "Run the simulation to the next trade");
			m_btn_run_to_trade.Click += (s,a) =>
			{
			};
		}

		/// <summary>Update UI Elements</summary>
		private void DoUpdateUI()
		{
			m_sig_update_ui = false;
			using (Scope.Create(() => ++m_updating_ui, () => --m_updating_ui))
			{
				// Step size
				m_cb_step_size.SelectedItem = Settings.Simulation.StepSize;

				// Enable/Disable UI parts
				m_panel_initial_conditions.Enabled = !Sim.Running;
				m_panel_sim_results.Enabled        = Sim.Running;
				m_tb_step_rate.Enabled             = !Sim.Running;

				// Update values from 'Sim'
				m_dtp_start_date.Value = Sim.StartTime.DateTime.As(DateTimeKind.Utc);
				m_tb_start_balance.Text = "{0:N2} {1}".Fmt(Sim.StartingBalance, Sim.Acct.Currency);

				m_tb_balance.Text = "{0:N2} {1}".Fmt(Sim.Acct.Balance, Sim.Acct.Currency);

				m_tb_step_rate.Text = "{0}".Fmt(Sim.StepRate);
				m_tb_sim_time.Text = Sim.UtcNow.ToString("ddd dd-MM-yyyy  HH:mm:ss");
				m_btn_run.ImageKey = Sim.Running ? "media_player_pause.png" : "media_player_play.png";
			}
		}
		private void UpdateUI(object sender = null, EventArgs args = null)
		{
			if (m_sig_update_ui) return;
			this.BeginInvoke(DoUpdateUI);
		}
		private bool m_sig_update_ui;
		private int m_updating_ui;

		#region Windows Form Designer generated code
		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SimulationUI));
			this.m_lbl_start_time = new System.Windows.Forms.Label();
			this.m_tb_sim_time = new System.Windows.Forms.TextBox();
			this.m_il = new System.Windows.Forms.ImageList(this.components);
			this.m_tt = new System.Windows.Forms.ToolTip(this.components);
			this.m_btn_run_to_trade = new System.Windows.Forms.Button();
			this.m_btn_reset = new System.Windows.Forms.Button();
			this.m_btn_run = new System.Windows.Forms.Button();
			this.m_btn_step_fwd = new System.Windows.Forms.Button();
			this.m_cb_step_size = new pr.gui.ComboBox();
			this.m_lbl_step_size = new System.Windows.Forms.Label();
			this.m_lbl_sim_date = new System.Windows.Forms.Label();
			this.m_dtp_start_date = new pr.gui.DateTimePicker();
			this.m_lbl_start_balance = new System.Windows.Forms.Label();
			this.m_tb_start_balance = new System.Windows.Forms.TextBox();
			this.m_tbar_start_date = new System.Windows.Forms.TrackBar();
			this.m_panel_sim_controls = new System.Windows.Forms.Panel();
			this.m_tb_step_rate = new pr.gui.ValueBox();
			this.m_lbl_step_rate = new System.Windows.Forms.Label();
			this.m_panel_initial_conditions = new System.Windows.Forms.Panel();
			this.m_spinner_tbar_counts = new System.Windows.Forms.NumericUpDown();
			this.label1 = new System.Windows.Forms.Label();
			this.m_tb_balance = new System.Windows.Forms.TextBox();
			this.m_panel_sim_results = new System.Windows.Forms.Panel();
			this.m_tb_avr_profit = new System.Windows.Forms.TextBox();
			this.m_lbl_avr_profit = new System.Windows.Forms.Label();
			this.m_panel_step_size = new System.Windows.Forms.Panel();
			this.m_timer_sim = new System.Windows.Forms.Timer(this.components);
			this.m_btn_step_back = new System.Windows.Forms.Button();
			((System.ComponentModel.ISupportInitialize)(this.m_tbar_start_date)).BeginInit();
			this.m_panel_sim_controls.SuspendLayout();
			this.m_panel_initial_conditions.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_spinner_tbar_counts)).BeginInit();
			this.m_panel_sim_results.SuspendLayout();
			this.m_panel_step_size.SuspendLayout();
			this.SuspendLayout();
			// 
			// m_lbl_start_time
			// 
			this.m_lbl_start_time.AutoSize = true;
			this.m_lbl_start_time.Location = new System.Drawing.Point(3, 7);
			this.m_lbl_start_time.Name = "m_lbl_start_time";
			this.m_lbl_start_time.Size = new System.Drawing.Size(55, 13);
			this.m_lbl_start_time.TabIndex = 1;
			this.m_lbl_start_time.Text = "Start Date";
			// 
			// m_tb_sim_time
			// 
			this.m_tb_sim_time.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_tb_sim_time.Location = new System.Drawing.Point(59, 29);
			this.m_tb_sim_time.Name = "m_tb_sim_time";
			this.m_tb_sim_time.ReadOnly = true;
			this.m_tb_sim_time.Size = new System.Drawing.Size(176, 20);
			this.m_tb_sim_time.TabIndex = 2;
			// 
			// m_il
			// 
			this.m_il.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("m_il.ImageStream")));
			this.m_il.TransparentColor = System.Drawing.Color.Transparent;
			this.m_il.Images.SetKeyName(0, "media_player_start.png");
			this.m_il.Images.SetKeyName(1, "media_player_step_bck.png");
			this.m_il.Images.SetKeyName(2, "media_player_step_fwd.png");
			this.m_il.Images.SetKeyName(3, "media_player_fwd.png");
			this.m_il.Images.SetKeyName(4, "media_player_pause.png");
			this.m_il.Images.SetKeyName(5, "media_player_play.png");
			// 
			// m_btn_run_to_trade
			// 
			this.m_btn_run_to_trade.ImageKey = "media_player_fwd.png";
			this.m_btn_run_to_trade.ImageList = this.m_il;
			this.m_btn_run_to_trade.Location = new System.Drawing.Point(144, 58);
			this.m_btn_run_to_trade.Name = "m_btn_run_to_trade";
			this.m_btn_run_to_trade.Size = new System.Drawing.Size(45, 46);
			this.m_btn_run_to_trade.TabIndex = 5;
			this.m_btn_run_to_trade.UseVisualStyleBackColor = true;
			// 
			// m_btn_reset
			// 
			this.m_btn_reset.ImageKey = "media_player_start.png";
			this.m_btn_reset.ImageList = this.m_il;
			this.m_btn_reset.Location = new System.Drawing.Point(0, 58);
			this.m_btn_reset.Name = "m_btn_reset";
			this.m_btn_reset.Size = new System.Drawing.Size(45, 46);
			this.m_btn_reset.TabIndex = 4;
			this.m_btn_reset.UseVisualStyleBackColor = true;
			// 
			// m_btn_run
			// 
			this.m_btn_run.ImageKey = "media_player_play.png";
			this.m_btn_run.ImageList = this.m_il;
			this.m_btn_run.Location = new System.Drawing.Point(192, 58);
			this.m_btn_run.Name = "m_btn_run";
			this.m_btn_run.Size = new System.Drawing.Size(45, 46);
			this.m_btn_run.TabIndex = 3;
			this.m_btn_run.UseVisualStyleBackColor = true;
			// 
			// m_btn_step_fwd
			// 
			this.m_btn_step_fwd.ImageKey = "media_player_step_fwd.png";
			this.m_btn_step_fwd.ImageList = this.m_il;
			this.m_btn_step_fwd.Location = new System.Drawing.Point(96, 58);
			this.m_btn_step_fwd.Name = "m_btn_step_fwd";
			this.m_btn_step_fwd.Size = new System.Drawing.Size(45, 46);
			this.m_btn_step_fwd.TabIndex = 6;
			this.m_btn_step_fwd.UseVisualStyleBackColor = true;
			// 
			// m_cb_step_size
			// 
			this.m_cb_step_size.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_cb_step_size.DisplayProperty = null;
			this.m_cb_step_size.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_cb_step_size.FormattingEnabled = true;
			this.m_cb_step_size.Location = new System.Drawing.Point(76, 4);
			this.m_cb_step_size.Name = "m_cb_step_size";
			this.m_cb_step_size.PreserveSelectionThruFocusChange = false;
			this.m_cb_step_size.Size = new System.Drawing.Size(159, 21);
			this.m_cb_step_size.TabIndex = 7;
			// 
			// m_lbl_step_size
			// 
			this.m_lbl_step_size.AutoSize = true;
			this.m_lbl_step_size.Location = new System.Drawing.Point(12, 7);
			this.m_lbl_step_size.Name = "m_lbl_step_size";
			this.m_lbl_step_size.Size = new System.Drawing.Size(52, 13);
			this.m_lbl_step_size.TabIndex = 8;
			this.m_lbl_step_size.Text = "Step Size";
			// 
			// m_lbl_sim_date
			// 
			this.m_lbl_sim_date.AutoSize = true;
			this.m_lbl_sim_date.Location = new System.Drawing.Point(3, 33);
			this.m_lbl_sim_date.Name = "m_lbl_sim_date";
			this.m_lbl_sim_date.Size = new System.Drawing.Size(53, 13);
			this.m_lbl_sim_date.TabIndex = 9;
			this.m_lbl_sim_date.Text = "Sim Time:";
			// 
			// m_dtp_start_date
			// 
			this.m_dtp_start_date.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_dtp_start_date.CustomFormat = "ddd yyyy-MM-dd HH:mm:ss";
			this.m_dtp_start_date.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
			this.m_dtp_start_date.Kind = System.DateTimeKind.Unspecified;
			this.m_dtp_start_date.Location = new System.Drawing.Point(64, 3);
			this.m_dtp_start_date.MaxDate = new System.DateTime(9998, 12, 31, 0, 0, 0, 0);
			this.m_dtp_start_date.MinDate = new System.DateTime(1753, 1, 1, 0, 0, 0, 0);
			this.m_dtp_start_date.Name = "m_dtp_start_date";
			this.m_dtp_start_date.Size = new System.Drawing.Size(171, 20);
			this.m_dtp_start_date.TabIndex = 10;
			this.m_dtp_start_date.Value = new System.DateTime(2016, 8, 14, 0, 0, 0, 0);
			// 
			// m_lbl_start_balance
			// 
			this.m_lbl_start_balance.AutoSize = true;
			this.m_lbl_start_balance.Location = new System.Drawing.Point(4, 55);
			this.m_lbl_start_balance.Name = "m_lbl_start_balance";
			this.m_lbl_start_balance.Size = new System.Drawing.Size(88, 13);
			this.m_lbl_start_balance.TabIndex = 11;
			this.m_lbl_start_balance.Text = "Starting Balance:";
			// 
			// m_tb_start_balance
			// 
			this.m_tb_start_balance.Location = new System.Drawing.Point(95, 52);
			this.m_tb_start_balance.Name = "m_tb_start_balance";
			this.m_tb_start_balance.Size = new System.Drawing.Size(94, 20);
			this.m_tb_start_balance.TabIndex = 12;
			// 
			// m_tbar_start_date
			// 
			this.m_tbar_start_date.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_tbar_start_date.AutoSize = false;
			this.m_tbar_start_date.Location = new System.Drawing.Point(64, 29);
			this.m_tbar_start_date.Name = "m_tbar_start_date";
			this.m_tbar_start_date.Size = new System.Drawing.Size(171, 23);
			this.m_tbar_start_date.TabIndex = 13;
			this.m_tbar_start_date.TickStyle = System.Windows.Forms.TickStyle.None;
			// 
			// m_panel_sim_controls
			// 
			this.m_panel_sim_controls.Controls.Add(this.m_btn_step_back);
			this.m_panel_sim_controls.Controls.Add(this.m_tb_step_rate);
			this.m_panel_sim_controls.Controls.Add(this.m_lbl_step_rate);
			this.m_panel_sim_controls.Controls.Add(this.m_btn_step_fwd);
			this.m_panel_sim_controls.Controls.Add(this.m_btn_run_to_trade);
			this.m_panel_sim_controls.Controls.Add(this.m_btn_reset);
			this.m_panel_sim_controls.Controls.Add(this.m_btn_run);
			this.m_panel_sim_controls.Controls.Add(this.m_tb_sim_time);
			this.m_panel_sim_controls.Controls.Add(this.m_lbl_sim_date);
			this.m_panel_sim_controls.Dock = System.Windows.Forms.DockStyle.Top;
			this.m_panel_sim_controls.Location = new System.Drawing.Point(0, 198);
			this.m_panel_sim_controls.Name = "m_panel_sim_controls";
			this.m_panel_sim_controls.Size = new System.Drawing.Size(238, 104);
			this.m_panel_sim_controls.TabIndex = 14;
			// 
			// m_tb_step_rate
			// 
			this.m_tb_step_rate.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_tb_step_rate.Location = new System.Drawing.Point(137, 3);
			this.m_tb_step_rate.Name = "m_tb_step_rate";
			this.m_tb_step_rate.Size = new System.Drawing.Size(98, 20);
			this.m_tb_step_rate.TabIndex = 16;
			this.m_tb_step_rate.Value = null;
			// 
			// m_lbl_step_rate
			// 
			this.m_lbl_step_rate.AutoSize = true;
			this.m_lbl_step_rate.Location = new System.Drawing.Point(5, 6);
			this.m_lbl_step_rate.Name = "m_lbl_step_rate";
			this.m_lbl_step_rate.Size = new System.Drawing.Size(114, 13);
			this.m_lbl_step_rate.TabIndex = 10;
			this.m_lbl_step_rate.Text = "Step Rate (steps/sec):";
			// 
			// m_panel_initial_conditions
			// 
			this.m_panel_initial_conditions.Controls.Add(this.m_spinner_tbar_counts);
			this.m_panel_initial_conditions.Controls.Add(this.m_lbl_start_time);
			this.m_panel_initial_conditions.Controls.Add(this.m_dtp_start_date);
			this.m_panel_initial_conditions.Controls.Add(this.m_tbar_start_date);
			this.m_panel_initial_conditions.Controls.Add(this.m_tb_start_balance);
			this.m_panel_initial_conditions.Controls.Add(this.m_lbl_start_balance);
			this.m_panel_initial_conditions.Dock = System.Windows.Forms.DockStyle.Top;
			this.m_panel_initial_conditions.Location = new System.Drawing.Point(0, 30);
			this.m_panel_initial_conditions.Name = "m_panel_initial_conditions";
			this.m_panel_initial_conditions.Size = new System.Drawing.Size(238, 79);
			this.m_panel_initial_conditions.TabIndex = 15;
			// 
			// m_spinner_tbar_counts
			// 
			this.m_spinner_tbar_counts.Location = new System.Drawing.Point(6, 29);
			this.m_spinner_tbar_counts.Name = "m_spinner_tbar_counts";
			this.m_spinner_tbar_counts.Size = new System.Drawing.Size(52, 20);
			this.m_spinner_tbar_counts.TabIndex = 14;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(3, 9);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(49, 13);
			this.label1.TabIndex = 16;
			this.label1.Text = "Balance:";
			// 
			// m_tb_balance
			// 
			this.m_tb_balance.Location = new System.Drawing.Point(58, 6);
			this.m_tb_balance.Name = "m_tb_balance";
			this.m_tb_balance.ReadOnly = true;
			this.m_tb_balance.Size = new System.Drawing.Size(80, 20);
			this.m_tb_balance.TabIndex = 17;
			// 
			// m_panel_sim_results
			// 
			this.m_panel_sim_results.Controls.Add(this.m_tb_avr_profit);
			this.m_panel_sim_results.Controls.Add(this.m_lbl_avr_profit);
			this.m_panel_sim_results.Controls.Add(this.label1);
			this.m_panel_sim_results.Controls.Add(this.m_tb_balance);
			this.m_panel_sim_results.Dock = System.Windows.Forms.DockStyle.Top;
			this.m_panel_sim_results.Location = new System.Drawing.Point(0, 109);
			this.m_panel_sim_results.Name = "m_panel_sim_results";
			this.m_panel_sim_results.Size = new System.Drawing.Size(238, 89);
			this.m_panel_sim_results.TabIndex = 18;
			// 
			// m_tb_avr_profit
			// 
			this.m_tb_avr_profit.Location = new System.Drawing.Point(58, 30);
			this.m_tb_avr_profit.Name = "m_tb_avr_profit";
			this.m_tb_avr_profit.ReadOnly = true;
			this.m_tb_avr_profit.Size = new System.Drawing.Size(80, 20);
			this.m_tb_avr_profit.TabIndex = 19;
			// 
			// m_lbl_avr_profit
			// 
			this.m_lbl_avr_profit.AutoSize = true;
			this.m_lbl_avr_profit.Location = new System.Drawing.Point(3, 33);
			this.m_lbl_avr_profit.Name = "m_lbl_avr_profit";
			this.m_lbl_avr_profit.Size = new System.Drawing.Size(53, 13);
			this.m_lbl_avr_profit.TabIndex = 18;
			this.m_lbl_avr_profit.Text = "Avr Profit:";
			// 
			// m_panel_step_size
			// 
			this.m_panel_step_size.Controls.Add(this.m_lbl_step_size);
			this.m_panel_step_size.Controls.Add(this.m_cb_step_size);
			this.m_panel_step_size.Dock = System.Windows.Forms.DockStyle.Top;
			this.m_panel_step_size.Location = new System.Drawing.Point(0, 0);
			this.m_panel_step_size.Name = "m_panel_step_size";
			this.m_panel_step_size.Size = new System.Drawing.Size(238, 30);
			this.m_panel_step_size.TabIndex = 19;
			// 
			// m_btn_step_back
			// 
			this.m_btn_step_back.ImageKey = "media_player_step_bck.png";
			this.m_btn_step_back.ImageList = this.m_il;
			this.m_btn_step_back.Location = new System.Drawing.Point(48, 58);
			this.m_btn_step_back.Name = "m_btn_step_back";
			this.m_btn_step_back.Size = new System.Drawing.Size(45, 46);
			this.m_btn_step_back.TabIndex = 17;
			this.m_btn_step_back.UseVisualStyleBackColor = true;
			// 
			// SimulationUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(238, 326);
			this.Controls.Add(this.m_panel_sim_controls);
			this.Controls.Add(this.m_panel_sim_results);
			this.Controls.Add(this.m_panel_initial_conditions);
			this.Controls.Add(this.m_panel_step_size);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MinimumSize = new System.Drawing.Size(254, 39);
			this.Name = "SimulationUI";
			this.PinOffset = new System.Drawing.Point(-300, 0);
			this.Text = "Simulation";
			((System.ComponentModel.ISupportInitialize)(this.m_tbar_start_date)).EndInit();
			this.m_panel_sim_controls.ResumeLayout(false);
			this.m_panel_sim_controls.PerformLayout();
			this.m_panel_initial_conditions.ResumeLayout(false);
			this.m_panel_initial_conditions.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_spinner_tbar_counts)).EndInit();
			this.m_panel_sim_results.ResumeLayout(false);
			this.m_panel_sim_results.PerformLayout();
			this.m_panel_step_size.ResumeLayout(false);
			this.m_panel_step_size.PerformLayout();
			this.ResumeLayout(false);

		}
		#endregion
	}
}
