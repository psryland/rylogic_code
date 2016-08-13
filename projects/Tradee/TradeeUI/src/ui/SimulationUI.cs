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
		private Button m_btn_step;
		private pr.gui.ComboBox m_cb_step_size;
		private Label m_lbl_step_size;
		private Label m_lbl_sim_date;
		private pr.gui.DateTimePicker m_dtp_start_date;
		private TextBox m_tb_sim_time;
		#endregion

		public SimulationUI(MainUI main, MainModel model)
			:base(main, EPin.TopRight)
		{
			InitializeComponent();
			Model = model;
			HideOnClose = true;

			SetupUI();
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
			set
			{
				if (m_model == value) return;
				if (m_model != null)
				{
					m_model.Simulation.SimTimeChanged -= HandleSimStep;
				}
				m_model = value;
				if (m_model != null)
				{
					m_model.Simulation.SimTimeChanged += HandleSimStep;
				}
			}
		}
		private MainModel m_model;

		/// <summary>The app logic behind the simulation</summary>
		public Simulation Sim
		{
			[DebuggerStepThrough] get { return Model.Simulation; }
		}

		/// <summary>Set up UI elements</summary>
		private void SetupUI()
		{
			// Start time picker
			m_dtp_start_date.Kind = DateTimeKind.Utc;
			m_dtp_start_date.Value = Sim.StartTime.DateTime.As(DateTimeKind.Utc);
			m_dtp_start_date.ValueChanged += (s,a) =>
			{
				Sim.StartTime = m_dtp_start_date.Value;
			};

			// Start time text box
			m_tb_sim_time.ToolTip(m_tt, "The date that the simulation starts from");
			m_tb_sim_time.Text = Sim.StartTime.ToString();

			// The step size
			m_cb_step_size.DataSource = Enum<ETimeFrame>.ValuesArray;
			m_cb_step_size.SelectedIndexChanged += (s,a) =>
			{
				Sim.StepSize = Misc.TimeFrameToTimeSpan(1.0, (ETimeFrame)m_cb_step_size.SelectedValue);
				UpdateUI();
			};

			// Button reset
			m_btn_reset.ToolTip(m_tt, "Reset the simulation time back to the start");
			m_btn_reset.Click += (s,a) =>
			{
				Sim.Reset();
				UpdateUI();
			};

			// Button run
			m_btn_run.ToolTip(m_tt, "Start/Stop the simulation running");
			m_btn_run.Click += (s,a) =>
			{
				Sim.Running = !Sim.Running;
				UpdateUI();
			};

			// Button run to next trade
			m_btn_run_to_trade.ToolTip(m_tt, "Run the simulation to the next trade");
			m_btn_run_to_trade.Click += (s,a) =>
			{
			};
		}

		/// <summary>Update UI Elements</summary>
		private void UpdateUI(object sender = null, EventArgs args = null)
		{
			m_btn_run.ImageKey = Sim.Running ? "media_player_pause.png" : "media_player_play.png";
		}

		/// <summary>Update when the simulation steps</summary>
		private void HandleSimStep(object sender, EventArgs e)
		{
			m_tb_sim_time.Text = Sim.UtcNow.ToString();
		}

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
			this.m_btn_step = new System.Windows.Forms.Button();
			this.m_cb_step_size = new pr.gui.ComboBox();
			this.m_lbl_step_size = new System.Windows.Forms.Label();
			this.m_lbl_sim_date = new System.Windows.Forms.Label();
			this.m_dtp_start_date = new pr.gui.DateTimePicker();
			this.SuspendLayout();
			// 
			// m_lbl_start_time
			// 
			this.m_lbl_start_time.AutoSize = true;
			this.m_lbl_start_time.Location = new System.Drawing.Point(8, 16);
			this.m_lbl_start_time.Name = "m_lbl_start_time";
			this.m_lbl_start_time.Size = new System.Drawing.Size(55, 13);
			this.m_lbl_start_time.TabIndex = 1;
			this.m_lbl_start_time.Text = "Start Date";
			// 
			// m_tb_sim_time
			// 
			this.m_tb_sim_time.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_tb_sim_time.Location = new System.Drawing.Point(73, 66);
			this.m_tb_sim_time.Name = "m_tb_sim_time";
			this.m_tb_sim_time.ReadOnly = true;
			this.m_tb_sim_time.Size = new System.Drawing.Size(180, 20);
			this.m_tb_sim_time.TabIndex = 2;
			// 
			// m_il
			// 
			this.m_il.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("m_il.ImageStream")));
			this.m_il.TransparentColor = System.Drawing.Color.Transparent;
			this.m_il.Images.SetKeyName(0, "media_player_play.png");
			this.m_il.Images.SetKeyName(1, "media_player_start.png");
			this.m_il.Images.SetKeyName(2, "media_player_step_fwd.png");
			this.m_il.Images.SetKeyName(3, "media_player_pause.png");
			this.m_il.Images.SetKeyName(4, "media_player_fwd.png");
			// 
			// m_btn_run_to_trade
			// 
			this.m_btn_run_to_trade.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.m_btn_run_to_trade.ImageKey = "media_player_fwd.png";
			this.m_btn_run_to_trade.ImageList = this.m_il;
			this.m_btn_run_to_trade.Location = new System.Drawing.Point(195, 335);
			this.m_btn_run_to_trade.Name = "m_btn_run_to_trade";
			this.m_btn_run_to_trade.Size = new System.Drawing.Size(55, 46);
			this.m_btn_run_to_trade.TabIndex = 5;
			this.m_btn_run_to_trade.UseVisualStyleBackColor = true;
			// 
			// m_btn_reset
			// 
			this.m_btn_reset.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.m_btn_reset.ImageKey = "media_player_start.png";
			this.m_btn_reset.ImageList = this.m_il;
			this.m_btn_reset.Location = new System.Drawing.Point(12, 335);
			this.m_btn_reset.Name = "m_btn_reset";
			this.m_btn_reset.Size = new System.Drawing.Size(55, 46);
			this.m_btn_reset.TabIndex = 4;
			this.m_btn_reset.UseVisualStyleBackColor = true;
			// 
			// m_btn_run
			// 
			this.m_btn_run.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.m_btn_run.ImageKey = "media_player_play.png";
			this.m_btn_run.ImageList = this.m_il;
			this.m_btn_run.Location = new System.Drawing.Point(73, 335);
			this.m_btn_run.Name = "m_btn_run";
			this.m_btn_run.Size = new System.Drawing.Size(55, 46);
			this.m_btn_run.TabIndex = 3;
			this.m_btn_run.UseVisualStyleBackColor = true;
			// 
			// m_btn_step
			// 
			this.m_btn_step.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.m_btn_step.ImageKey = "media_player_step_fwd.png";
			this.m_btn_step.ImageList = this.m_il;
			this.m_btn_step.Location = new System.Drawing.Point(134, 335);
			this.m_btn_step.Name = "m_btn_step";
			this.m_btn_step.Size = new System.Drawing.Size(55, 46);
			this.m_btn_step.TabIndex = 6;
			this.m_btn_step.UseVisualStyleBackColor = true;
			// 
			// m_cb_step_size
			// 
			this.m_cb_step_size.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_cb_step_size.DisplayProperty = null;
			this.m_cb_step_size.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.m_cb_step_size.FormattingEnabled = true;
			this.m_cb_step_size.Location = new System.Drawing.Point(73, 39);
			this.m_cb_step_size.Name = "m_cb_step_size";
			this.m_cb_step_size.PreserveSelectionThruFocusChange = false;
			this.m_cb_step_size.Size = new System.Drawing.Size(180, 21);
			this.m_cb_step_size.TabIndex = 7;
			// 
			// m_lbl_step_size
			// 
			this.m_lbl_step_size.AutoSize = true;
			this.m_lbl_step_size.Location = new System.Drawing.Point(12, 42);
			this.m_lbl_step_size.Name = "m_lbl_step_size";
			this.m_lbl_step_size.Size = new System.Drawing.Size(52, 13);
			this.m_lbl_step_size.TabIndex = 8;
			this.m_lbl_step_size.Text = "Step Size";
			// 
			// m_lbl_sim_date
			// 
			this.m_lbl_sim_date.AutoSize = true;
			this.m_lbl_sim_date.Location = new System.Drawing.Point(12, 69);
			this.m_lbl_sim_date.Name = "m_lbl_sim_date";
			this.m_lbl_sim_date.Size = new System.Drawing.Size(50, 13);
			this.m_lbl_sim_date.TabIndex = 9;
			this.m_lbl_sim_date.Text = "Sim Date";
			// 
			// m_dtp_start_date
			// 
			this.m_dtp_start_date.Kind = System.DateTimeKind.Unspecified;
			this.m_dtp_start_date.Location = new System.Drawing.Point(73, 10);
			this.m_dtp_start_date.MaxDate = new System.DateTime(9998, 12, 31, 0, 0, 0, 0);
			this.m_dtp_start_date.MinDate = new System.DateTime(1753, 1, 1, 0, 0, 0, 0);
			this.m_dtp_start_date.Name = "m_dtp_start_date";
			this.m_dtp_start_date.Size = new System.Drawing.Size(180, 20);
			this.m_dtp_start_date.TabIndex = 10;
			this.m_dtp_start_date.Value = new System.DateTime(2016, 8, 14, 0, 36, 23, 649);
			// 
			// SimulationUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(265, 393);
			this.Controls.Add(this.m_dtp_start_date);
			this.Controls.Add(this.m_lbl_sim_date);
			this.Controls.Add(this.m_lbl_step_size);
			this.Controls.Add(this.m_cb_step_size);
			this.Controls.Add(this.m_btn_step);
			this.Controls.Add(this.m_btn_run_to_trade);
			this.Controls.Add(this.m_btn_reset);
			this.Controls.Add(this.m_btn_run);
			this.Controls.Add(this.m_tb_sim_time);
			this.Controls.Add(this.m_lbl_start_time);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "SimulationUI";
			this.Text = "Simulation";
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion
	}
}
