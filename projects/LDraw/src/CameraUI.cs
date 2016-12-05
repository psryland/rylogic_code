using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using pr.extn;
using pr.gfx;
using pr.gui;
using pr.maths;
using pr.util;

namespace LDraw
{
	public class CameraUI :ToolForm
	{
		private readonly MainUI m_main_ui;

		#region UI Elements
		private ValueBox m_tb_focus_point;
		private ValueBox m_tb_focus_dist;
		private ValueBox m_tb_camera_up;
		private ValueBox m_tb_camera_fwd;
		private ValueBox m_tb_fovY;
		private ValueBox m_tb_fovX;
		private Label m_lbl_focus_point;
		private Label m_lbl_focus_dist;
		private Label m_lbl_camera_fwd;
		private Label m_lbl_camera_up;
		private Label m_lbl_fovX;
		private Label m_lbl_fovY;
		private Button m_btn_close;
		private Button m_btn_hidden;
		private Timer m_timer;
		#endregion

		public CameraUI(MainUI main_ui)
			:base(main_ui, EPin.TopLeft)
		{
			InitializeComponent();
			m_main_ui = main_ui;

			SetupUI();
			UpdateUI();
		}
		protected override void Dispose(bool disposing)
		{
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}
		protected override void OnVisibleChanged(EventArgs e)
		{
			base.OnVisibleChanged(e);
			m_timer.Enabled = Visible;
		}

		/// <summary>The camera whose properties are displayed</summary>
		private View3d.CameraControls Camera
		{
			get { return m_main_ui.Model.Scene.Scene.Camera; }
		}

		/// <summary>Set up UI Elements</summary>
		private void SetupUI()
		{
			// Timer for polling the camera state
			m_timer.Interval = 10;
			m_timer.Tick += UpdateUI;

			// Set the background colours for all value boxes
			foreach (var c in Controls.OfType<ValueBox>())
			{
				c.BackColorValid = Color_.FromArgb(0xFFD6FFC9);
				c.BackColorInvalid = Color_.FromArgb(0xFFFFDDAA);
			}

			// Focus point
			m_tb_focus_point.ValueToText = x => ((v4)x).ToString3();
			m_tb_focus_point.TextToValue = s => v4.Parse3(s, 1f);
			m_tb_focus_point.ValidateText = s => v4.TryParse3(s, 1f) != null;
			m_tb_focus_point.Value = Camera.FocusPoint;
			m_tb_focus_point.ValueChanged += (s,a) =>
			{
				if (!m_tb_focus_point.Focused) return;
				Camera.FocusPoint = (v4)m_tb_focus_point.Value;
				m_main_ui.Invalidate();
			};

			// Camera forward
			m_tb_camera_fwd.ValueToText = x => ((v4)x).ToString3();
			m_tb_camera_fwd.TextToValue = s => v4.Parse3(s, 1f);
			m_tb_camera_fwd.ValidateText = s => v4.TryParse3(s, 1f) != null;
			m_tb_camera_fwd.Value = -Camera.O2W.z;
			m_tb_camera_fwd.ReadOnly = true;

			// Camera up
			m_tb_camera_up.ValidateText = s => v4.TryParse3(s, 1f) != null;
			m_tb_camera_up.ValueToText = x => ((v4)x).ToString3();
			m_tb_camera_up.TextToValue = s => v4.Parse3(s, 1f);
			m_tb_camera_up.Value = Camera.O2W.y;
			m_tb_camera_up.ReadOnly = true;

			// FovX
			m_tb_fovX.ValidateText = s => { var v = float_.TryParse(s); return v != null && v.Value > 0 && v.Value < Maths.TauBy2; };
			m_tb_fovX.Value = Camera.FovX;
			m_tb_fovX.ValueChanged += (s,a) =>
			{
				if (!m_tb_fovX.Focused) return;
				Camera.FovX = (float)m_tb_fovX.Value;
				m_main_ui.Invalidate();
			};

			// FovY
			m_tb_fovY.ValidateText = s => { var v = float_.TryParse(s); return v != null && v.Value > 0 && v.Value < Maths.TauBy2; };
			m_tb_fovY.Value = Camera.FovY;
			m_tb_fovY.ValueChanged += (s,a) =>
			{
				if (!m_tb_fovY.Focused) return;
				Camera.FovY = (float)m_tb_fovY.Value;
				m_main_ui.Invalidate();
			};

			// Focus distance
			m_tb_focus_dist.ValidateText = s => { var v = float_.TryParse(s); return v != null && v.Value > 0; };
			m_tb_focus_dist.Value = Camera.FocusDist;
			m_tb_focus_dist.ValueChanged += (s,a) =>
			{
				if (!m_tb_focus_dist.Focused) return;
				Camera.FocusDist = (float)m_tb_focus_dist.Value;
				m_main_ui.Invalidate();
			};
		}

		/// <summary>Update up UI Elements</summary>
		private void UpdateUI(object sender = null, EventArgs args = null)
		{
			// Only update fields that don't have input focus
			Action<ValueBox,object> Update = (c,v) =>
			{
				if (!c.Focused)
					c.Value = v;
			};

			Update(m_tb_focus_point, Camera.FocusPoint);
			Update(m_tb_camera_fwd, -Camera.O2W.z);
			Update(m_tb_camera_up, Camera.O2W.y);
			Update(m_tb_fovX, Camera.FovX);
			Update(m_tb_fovY, Camera.FovY);
			Update(m_tb_focus_dist, Camera.FocusDist);
		}

		#region Windows Form Designer generated code
		private System.ComponentModel.IContainer components;
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.m_lbl_focus_point = new System.Windows.Forms.Label();
			this.m_lbl_camera_up = new System.Windows.Forms.Label();
			this.m_lbl_camera_fwd = new System.Windows.Forms.Label();
			this.m_btn_close = new System.Windows.Forms.Button();
			this.m_lbl_fovX = new System.Windows.Forms.Label();
			this.m_lbl_fovY = new System.Windows.Forms.Label();
			this.m_lbl_focus_dist = new System.Windows.Forms.Label();
			this.m_timer = new System.Windows.Forms.Timer(this.components);
			this.m_btn_hidden = new System.Windows.Forms.Button();
			this.m_tb_focus_dist = new pr.gui.ValueBox();
			this.m_tb_fovX = new pr.gui.ValueBox();
			this.m_tb_fovY = new pr.gui.ValueBox();
			this.m_tb_camera_fwd = new pr.gui.ValueBox();
			this.m_tb_camera_up = new pr.gui.ValueBox();
			this.m_tb_focus_point = new pr.gui.ValueBox();
			this.SuspendLayout();
			// 
			// m_lbl_focus_point
			// 
			this.m_lbl_focus_point.AutoSize = true;
			this.m_lbl_focus_point.Location = new System.Drawing.Point(31, 15);
			this.m_lbl_focus_point.Name = "m_lbl_focus_point";
			this.m_lbl_focus_point.Size = new System.Drawing.Size(81, 16);
			this.m_lbl_focus_point.TabIndex = 0;
			this.m_lbl_focus_point.Text = "Focus Point:";
			this.m_lbl_focus_point.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// m_lbl_camera_up
			// 
			this.m_lbl_camera_up.AutoSize = true;
			this.m_lbl_camera_up.Location = new System.Drawing.Point(32, 99);
			this.m_lbl_camera_up.Name = "m_lbl_camera_up";
			this.m_lbl_camera_up.Size = new System.Drawing.Size(80, 16);
			this.m_lbl_camera_up.TabIndex = 2;
			this.m_lbl_camera_up.Text = "Camera Up:";
			this.m_lbl_camera_up.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// m_lbl_camera_fwd
			// 
			this.m_lbl_camera_fwd.AutoSize = true;
			this.m_lbl_camera_fwd.Location = new System.Drawing.Point(1, 71);
			this.m_lbl_camera_fwd.Name = "m_lbl_camera_fwd";
			this.m_lbl_camera_fwd.Size = new System.Drawing.Size(111, 16);
			this.m_lbl_camera_fwd.TabIndex = 4;
			this.m_lbl_camera_fwd.Text = "Camera Forward:";
			this.m_lbl_camera_fwd.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// m_btn_close
			// 
			this.m_btn_close.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_close.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btn_close.Location = new System.Drawing.Point(133, 184);
			this.m_btn_close.Name = "m_btn_close";
			this.m_btn_close.Size = new System.Drawing.Size(100, 28);
			this.m_btn_close.TabIndex = 6;
			this.m_btn_close.Text = "Close";
			this.m_btn_close.UseVisualStyleBackColor = true;
			// 
			// m_lbl_fovX
			// 
			this.m_lbl_fovX.AutoSize = true;
			this.m_lbl_fovX.Location = new System.Drawing.Point(14, 127);
			this.m_lbl_fovX.Name = "m_lbl_fovX";
			this.m_lbl_fovX.Size = new System.Drawing.Size(98, 16);
			this.m_lbl_fovX.TabIndex = 8;
			this.m_lbl_fovX.Text = "Field of View X:";
			this.m_lbl_fovX.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// m_lbl_fovY
			// 
			this.m_lbl_fovY.AutoSize = true;
			this.m_lbl_fovY.Location = new System.Drawing.Point(13, 156);
			this.m_lbl_fovY.Name = "m_lbl_fovY";
			this.m_lbl_fovY.Size = new System.Drawing.Size(99, 16);
			this.m_lbl_fovY.TabIndex = 10;
			this.m_lbl_fovY.Text = "Field of View Y:";
			this.m_lbl_fovY.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// m_lbl_focus_dist
			// 
			this.m_lbl_focus_dist.AutoSize = true;
			this.m_lbl_focus_dist.Location = new System.Drawing.Point(8, 43);
			this.m_lbl_focus_dist.Name = "m_lbl_focus_dist";
			this.m_lbl_focus_dist.Size = new System.Drawing.Size(104, 16);
			this.m_lbl_focus_dist.TabIndex = 12;
			this.m_lbl_focus_dist.Text = "Focus Distance:";
			this.m_lbl_focus_dist.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// m_btn_hidden
			// 
			this.m_btn_hidden.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.m_btn_hidden.Location = new System.Drawing.Point(11, 184);
			this.m_btn_hidden.Name = "m_btn_hidden";
			this.m_btn_hidden.Size = new System.Drawing.Size(121, 28);
			this.m_btn_hidden.TabIndex = 13;
			this.m_btn_hidden.Text = "Prevent the Bong";
			this.m_btn_hidden.UseVisualStyleBackColor = true;
			this.m_btn_hidden.Visible = false;
			// 
			// m_tb_focus_dist
			// 
			this.m_tb_focus_dist.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_tb_focus_dist.BackColorInvalid = System.Drawing.Color.White;
			this.m_tb_focus_dist.BackColorValid = System.Drawing.Color.White;
			this.m_tb_focus_dist.ForeColor = System.Drawing.Color.Black;
			this.m_tb_focus_dist.ForeColorInvalid = System.Drawing.Color.Gray;
			this.m_tb_focus_dist.ForeColorValid = System.Drawing.Color.Black;
			this.m_tb_focus_dist.Location = new System.Drawing.Point(114, 40);
			this.m_tb_focus_dist.Name = "m_tb_focus_dist";
			this.m_tb_focus_dist.Size = new System.Drawing.Size(118, 22);
			this.m_tb_focus_dist.TabIndex = 5;
			this.m_tb_focus_dist.Value = null;
			// 
			// m_tb_fovX
			// 
			this.m_tb_fovX.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_tb_fovX.BackColorInvalid = System.Drawing.Color.White;
			this.m_tb_fovX.BackColorValid = System.Drawing.Color.White;
			this.m_tb_fovX.ForeColor = System.Drawing.Color.Black;
			this.m_tb_fovX.ForeColorInvalid = System.Drawing.Color.Gray;
			this.m_tb_fovX.ForeColorValid = System.Drawing.Color.Black;
			this.m_tb_fovX.Location = new System.Drawing.Point(114, 124);
			this.m_tb_fovX.Name = "m_tb_fovX";
			this.m_tb_fovX.Size = new System.Drawing.Size(118, 22);
			this.m_tb_fovX.TabIndex = 3;
			this.m_tb_fovX.Value = null;
			// 
			// m_tb_fovY
			// 
			this.m_tb_fovY.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_tb_fovY.BackColorInvalid = System.Drawing.Color.White;
			this.m_tb_fovY.BackColorValid = System.Drawing.Color.White;
			this.m_tb_fovY.ForeColor = System.Drawing.Color.Black;
			this.m_tb_fovY.ForeColorInvalid = System.Drawing.Color.Gray;
			this.m_tb_fovY.ForeColorValid = System.Drawing.Color.Black;
			this.m_tb_fovY.Location = new System.Drawing.Point(114, 153);
			this.m_tb_fovY.Name = "m_tb_fovY";
			this.m_tb_fovY.Size = new System.Drawing.Size(118, 22);
			this.m_tb_fovY.TabIndex = 4;
			this.m_tb_fovY.Value = null;
			// 
			// m_tb_camera_fwd
			// 
			this.m_tb_camera_fwd.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_tb_camera_fwd.BackColorInvalid = System.Drawing.Color.White;
			this.m_tb_camera_fwd.BackColorValid = System.Drawing.Color.White;
			this.m_tb_camera_fwd.ForeColor = System.Drawing.Color.Black;
			this.m_tb_camera_fwd.ForeColorInvalid = System.Drawing.Color.Gray;
			this.m_tb_camera_fwd.ForeColorValid = System.Drawing.Color.Black;
			this.m_tb_camera_fwd.Location = new System.Drawing.Point(114, 68);
			this.m_tb_camera_fwd.Name = "m_tb_camera_fwd";
			this.m_tb_camera_fwd.Size = new System.Drawing.Size(118, 22);
			this.m_tb_camera_fwd.TabIndex = 1;
			this.m_tb_camera_fwd.Value = null;
			// 
			// m_tb_camera_up
			// 
			this.m_tb_camera_up.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_tb_camera_up.BackColorInvalid = System.Drawing.Color.White;
			this.m_tb_camera_up.BackColorValid = System.Drawing.Color.White;
			this.m_tb_camera_up.ForeColor = System.Drawing.Color.Black;
			this.m_tb_camera_up.ForeColorInvalid = System.Drawing.Color.Gray;
			this.m_tb_camera_up.ForeColorValid = System.Drawing.Color.Black;
			this.m_tb_camera_up.Location = new System.Drawing.Point(114, 96);
			this.m_tb_camera_up.Name = "m_tb_camera_up";
			this.m_tb_camera_up.Size = new System.Drawing.Size(118, 22);
			this.m_tb_camera_up.TabIndex = 2;
			this.m_tb_camera_up.Value = null;
			// 
			// m_tb_focus_point
			// 
			this.m_tb_focus_point.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_tb_focus_point.BackColorInvalid = System.Drawing.Color.White;
			this.m_tb_focus_point.BackColorValid = System.Drawing.Color.White;
			this.m_tb_focus_point.ForeColor = System.Drawing.Color.Black;
			this.m_tb_focus_point.ForeColorInvalid = System.Drawing.Color.Gray;
			this.m_tb_focus_point.ForeColorValid = System.Drawing.Color.Black;
			this.m_tb_focus_point.Location = new System.Drawing.Point(114, 12);
			this.m_tb_focus_point.Name = "m_tb_focus_point";
			this.m_tb_focus_point.Size = new System.Drawing.Size(118, 22);
			this.m_tb_focus_point.TabIndex = 0;
			this.m_tb_focus_point.Value = null;
			// 
			// CameraUI
			// 
			this.AcceptButton = this.m_btn_hidden;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.m_btn_close;
			this.ClientSize = new System.Drawing.Size(245, 224);
			this.Controls.Add(this.m_btn_hidden);
			this.Controls.Add(this.m_tb_focus_dist);
			this.Controls.Add(this.m_lbl_focus_dist);
			this.Controls.Add(this.m_tb_fovX);
			this.Controls.Add(this.m_lbl_fovY);
			this.Controls.Add(this.m_tb_fovY);
			this.Controls.Add(this.m_lbl_fovX);
			this.Controls.Add(this.m_btn_close);
			this.Controls.Add(this.m_tb_camera_fwd);
			this.Controls.Add(this.m_lbl_camera_fwd);
			this.Controls.Add(this.m_tb_camera_up);
			this.Controls.Add(this.m_lbl_camera_up);
			this.Controls.Add(this.m_tb_focus_point);
			this.Controls.Add(this.m_lbl_focus_point);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
			this.Margin = new System.Windows.Forms.Padding(5, 5, 5, 5);
			this.MinimumSize = new System.Drawing.Size(243, 263);
			this.Name = "CameraUI";
			this.PinOffset = new System.Drawing.Point(-300, 0);
			this.Text = "Camera Properties";
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion
	}
}
