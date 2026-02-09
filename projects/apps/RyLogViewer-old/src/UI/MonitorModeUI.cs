using System;
using System.Windows.Forms;
using Rylogic.Gui.WinForms;
using Rylogic.Maths;
using Util = Rylogic.Utility.Util;

namespace RyLogViewer
{
	public class MonitorModeUI :Form
	{
		#region UI Elements
		private TrackBar m_trk_opacity;
		private Button m_btn_enable;
		private Button m_btn_cancel;
		private Label m_lbl_transparency;
		private CheckBox m_chk_click_thru;
		private Label label1;
		private CheckBox m_chk_always_on_top;
		private ToolTip m_tt;
		#endregion

		public MonitorModeUI(Main main)
		{
			InitializeComponent();
			Main = main;

			// Always on top
			AlwaysOnTop = false;
			m_chk_always_on_top.ToolTip(m_tt, "Check to make RyLogViewer always visible above other windows");
			m_chk_always_on_top.CheckedChanged += (s,a) =>
			{
				AlwaysOnTop = m_chk_always_on_top.Checked;
			};

			// Click through
			ClickThru = false;
			m_chk_click_thru.ToolTip(m_tt, "Check to make the window invisible to user input.\r\nCancel this mode by clicking on the system tray icon");
			m_chk_click_thru.Click += (s,a)=>
			{
				ClickThru = m_chk_click_thru.Checked;
			};

			// Transparency track
			Alpha = 1f;
			m_trk_opacity.ToolTip(m_tt, "The transparency of the main log view window");
			m_trk_opacity.ValueChanged += (s,a)=>
			{
				Alpha = m_trk_opacity.Value * 0.01f;
				Main.Opacity = Alpha;
			};
		}
		protected override void Dispose(bool disposing)
		{
			Main = null;
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}
		protected override void OnFormClosing(FormClosingEventArgs e)
		{
			base.OnFormClosing(e);
			Main.Opacity = 1f;
		}

		/// <summary>The main app</summary>
		public Main Main
		{
			get;
			private set
			{
				if (field == value) return;
				field = value;
			}
		}

		/// <summary>Always above other windows</summary>
		public bool AlwaysOnTop
		{
			get { return m_always_on_top; }
			set { m_always_on_top = m_chk_always_on_top.Checked = value; }
		}
		private bool m_always_on_top;

		/// <summary>Transparent to mouse clicks</summary>
		public bool ClickThru
		{
			get { return m_click_through; }
			set { m_click_through = m_chk_click_thru.Checked = value; }
		}
		private bool m_click_through;

		/// <summary>The level of transparency</summary>
		public float Alpha
		{
			get { return m_alpha; }
			set
			{
				if (Math.Abs(m_alpha - value) < float.Epsilon) return;
				m_alpha = Math_.Clamp(value, 0f, 1f);
				m_trk_opacity.Value = (int)Math_.Clamp(m_alpha * 100f, m_trk_opacity.Minimum, m_trk_opacity.Maximum);
			}
		}
		private float m_alpha;

		#region Windows Form Designer generated code
		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MonitorModeUI));
			this.m_trk_opacity = new System.Windows.Forms.TrackBar();
			this.m_btn_enable = new System.Windows.Forms.Button();
			this.m_btn_cancel = new System.Windows.Forms.Button();
			this.m_lbl_transparency = new System.Windows.Forms.Label();
			this.m_chk_click_thru = new System.Windows.Forms.CheckBox();
			this.label1 = new System.Windows.Forms.Label();
			this.m_chk_always_on_top = new System.Windows.Forms.CheckBox();
			this.m_tt = new System.Windows.Forms.ToolTip(this.components);
			((System.ComponentModel.ISupportInitialize)(this.m_trk_opacity)).BeginInit();
			this.SuspendLayout();
			// 
			// m_trk_opacity
			// 
			this.m_trk_opacity.Location = new System.Drawing.Point(11, 23);
			this.m_trk_opacity.Maximum = 100;
			this.m_trk_opacity.Name = "m_trk_opacity";
			this.m_trk_opacity.Size = new System.Drawing.Size(243, 45);
			this.m_trk_opacity.TabIndex = 0;
			this.m_trk_opacity.TickFrequency = 10;
			this.m_trk_opacity.Value = 100;
			// 
			// m_btn_enable
			// 
			this.m_btn_enable.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_enable.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btn_enable.Location = new System.Drawing.Point(98, 155);
			this.m_btn_enable.Name = "m_btn_enable";
			this.m_btn_enable.Size = new System.Drawing.Size(75, 23);
			this.m_btn_enable.TabIndex = 3;
			this.m_btn_enable.Text = "Enable";
			this.m_btn_enable.UseVisualStyleBackColor = true;
			// 
			// m_btn_cancel
			// 
			this.m_btn_cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.m_btn_cancel.Location = new System.Drawing.Point(179, 155);
			this.m_btn_cancel.Name = "m_btn_cancel";
			this.m_btn_cancel.Size = new System.Drawing.Size(75, 23);
			this.m_btn_cancel.TabIndex = 4;
			this.m_btn_cancel.Text = "Cancel";
			this.m_btn_cancel.UseVisualStyleBackColor = true;
			// 
			// m_lbl_transparency
			// 
			this.m_lbl_transparency.AutoSize = true;
			this.m_lbl_transparency.Location = new System.Drawing.Point(8, 8);
			this.m_lbl_transparency.Name = "m_lbl_transparency";
			this.m_lbl_transparency.Size = new System.Drawing.Size(75, 13);
			this.m_lbl_transparency.TabIndex = 4;
			this.m_lbl_transparency.Text = "Transparency:";
			// 
			// m_chk_click_thru
			// 
			this.m_chk_click_thru.AutoSize = true;
			this.m_chk_click_thru.Location = new System.Drawing.Point(12, 87);
			this.m_chk_click_thru.Name = "m_chk_click_thru";
			this.m_chk_click_thru.Size = new System.Drawing.Size(127, 17);
			this.m_chk_click_thru.TabIndex = 2;
			this.m_chk_click_thru.Text = "\"Click-through\" mode";
			this.m_chk_click_thru.UseVisualStyleBackColor = true;
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(38, 115);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(180, 26);
			this.label1.TabIndex = 5;
			this.label1.Text = "Exit \'monitor mode\' by clicking on the\r\n RyLogViewer icon in the system tray";
			// 
			// m_chk_always_on_top
			// 
			this.m_chk_always_on_top.AutoSize = true;
			this.m_chk_always_on_top.Location = new System.Drawing.Point(11, 64);
			this.m_chk_always_on_top.Name = "m_chk_always_on_top";
			this.m_chk_always_on_top.Size = new System.Drawing.Size(92, 17);
			this.m_chk_always_on_top.TabIndex = 1;
			this.m_chk_always_on_top.Text = "Always on top";
			this.m_chk_always_on_top.UseVisualStyleBackColor = true;
			// 
			// MonitorModeUI
			// 
			this.AcceptButton = this.m_btn_enable;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.m_btn_cancel;
			this.ClientSize = new System.Drawing.Size(267, 191);
			this.Controls.Add(this.m_chk_always_on_top);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.m_chk_click_thru);
			this.Controls.Add(this.m_lbl_transparency);
			this.Controls.Add(this.m_btn_cancel);
			this.Controls.Add(this.m_btn_enable);
			this.Controls.Add(this.m_trk_opacity);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "MonitorModeUI";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Monitor Mode";
			((System.ComponentModel.ISupportInitialize)(this.m_trk_opacity)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion
	}
}
