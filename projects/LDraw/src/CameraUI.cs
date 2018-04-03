using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Rylogic.Extn;
using Rylogic.Gui;
using Rylogic.Maths;
using Rylogic.Utility;
using Rylogic.Graphix;

namespace LDraw
{
	public class CameraUI :ToolForm
	{
		private readonly SceneUI m_scene;

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
		private Button m_btn_bong;
		private CheckBox m_chk_preserve_aspect;
		private Label m_lbl_aspect_ratio;
		private ValueBox m_tb_aspect;
		private ValueBox m_tb_zoom;
		private Label m_lbl_zoom;
		private ValueBox m_tb_near;
		private Label m_lbl_near;
		private ValueBox m_tb_far;
		private Label m_lbl_far;
		private Timer m_timer;
		#endregion

		public CameraUI(SceneUI scene)
			:base(scene, EPin.Centre, Point.Empty)
		{
			InitializeComponent();
			m_scene = scene;
			Camera = m_scene.Camera;

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
		private View3d.Camera Camera
		{
			get;
			set;
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
			m_tb_focus_point.ValidateText = s => v4.TryParse3(s, 1f) != null;
			m_tb_focus_point.TextToValue = s => v4.Parse3(s, 1f);
			m_tb_focus_point.ValueToText = x => x != null ? ((v4)x).ToString3() : string.Empty;
			m_tb_focus_point.Value = Camera.FocusPoint;
			m_tb_focus_point.ValueChanged += (s,a) =>
			{
				if (!m_tb_focus_point.Focused) return;
				Camera.FocusPoint = (v4)m_tb_focus_point.Value;
				m_scene.Invalidate();
			};

			// Focus distance
			m_tb_focus_dist.ValidateText = s => { var v = float_.TryParse(s); return v != null && v.Value > 0; };
			m_tb_focus_dist.Value = Camera.FocusDist;
			m_tb_focus_dist.ValueChanged += (s,a) =>
			{
				if (!m_tb_focus_dist.Focused) return;
				var pt = Camera.FocusPoint;
				Camera.FocusDist = (float)m_tb_focus_dist.Value;
				Camera.FocusPoint = pt;
				m_scene.Invalidate();
			};

			// Camera forward
			m_tb_camera_fwd.ValidateText = s => v4.TryParse3(s, 0f) != null;
			m_tb_camera_fwd.TextToValue = s => Math_.Normalise(v4.Parse3(s, 0f));
			m_tb_camera_fwd.ValueToText = x => x != null ? ((v4)x).ToString3() : string.Empty;
			m_tb_camera_fwd.Value = -Camera.O2W.z;
			m_tb_camera_fwd.ReadOnly = true;

			// Camera up
			m_tb_camera_up.ValidateText = s => v4.TryParse3(s, 0f) != null;
			m_tb_camera_up.TextToValue = s => Math_.Normalise(v4.Parse3(s, 0f));
			m_tb_camera_up.ValueToText = x => x != null ? ((v4)x).ToString3() : string.Empty;
			m_tb_camera_up.Value = Camera.O2W.y;
			m_tb_camera_up.ReadOnly = true;

			// Zoom
			m_tb_zoom.ValidateText = s => { var v = float_.TryParse(s); return v != null && v.Value >= 0; };
			m_tb_zoom.Value = Camera.Zoom;
			m_tb_zoom.ValueChanged += (s,a) =>
			{
				if (!m_tb_zoom.Focused) return;
				Camera.Zoom = (float)m_tb_zoom.Value;
				m_scene.Invalidate();
			};

			// Near
			m_tb_near.ValidateText = s => { var v = float_.TryParse(s); return v != null && v.Value > 0; };
			m_tb_near.Value = Camera.NearPlane;
			m_tb_near.ValueChanged += (s,a) =>
			{
				if (!m_tb_near.Focused) return;
				Camera.NearPlane = (float)m_tb_near.Value;
				if (Camera.FarPlane <= Camera.NearPlane)
					Camera.FarPlane = Camera.NearPlane * 100f;
				m_scene.Invalidate();
			};

			// Far
			m_tb_far.ValidateText = s => { var v = float_.TryParse(s); return v != null && v.Value > 0; };
			m_tb_far.Value = Camera.FarPlane;
			m_tb_far.ValueChanged += (s,a) =>
			{
				if (!m_tb_far.Focused) return;
				Camera.FarPlane = (float)m_tb_far.Value;
				if (Camera.NearPlane >= Camera.FarPlane)
					Camera.NearPlane = Camera.FarPlane * 0.01f;
				m_scene.Invalidate();
			};

			// FovX
			m_tb_fovX.ValidateText = s => { var v = float_.TryParse(s); return v != null && v.Value > 0 && v.Value < 180f; };
			m_tb_fovX.Value = Math_.RadiansToDegrees(Camera.FovX);
			m_tb_fovX.ValueChanged += (s,a) =>
			{
				if (!m_tb_fovX.Focused) return;

				var fov = Math_.DegreesToRadians((float)m_tb_fovX.Value);
				if (m_chk_preserve_aspect.Checked)
					Camera.FovX = fov;
				else
					Camera.SetFov(fov, Camera.FovY);

				m_scene.Invalidate();
			};

			// FovY
			m_tb_fovY.ValidateText = s => { var v = float_.TryParse(s); return v != null && v.Value > 0 && v.Value < 180f; };
			m_tb_fovY.Value = Math_.RadiansToDegrees(Camera.FovY);
			m_tb_fovY.ValueChanged += (s,a) =>
			{
				if (!m_tb_fovY.Focused) return;

				var fov = Math_.DegreesToRadians((float)m_tb_fovY.Value);
				if (m_chk_preserve_aspect.Checked)
					Camera.FovY = fov;
				else
					Camera.SetFov(Camera.FovX, fov);

				m_scene.Invalidate();
			};

			// Aspect ratio
			m_tb_aspect.ValidateText = s => { var v = float_.TryParse(s); return v != null && v.Value > 0; };
			m_tb_aspect.Value = Camera.Aspect;
			m_tb_aspect.ValueChanged += (s,a) =>
			{
				if (!m_tb_aspect.Focused) return;
				Camera.Aspect = (float)m_tb_aspect.Value;
				m_scene.Invalidate();
			};

			// Preserve aspect
			m_chk_preserve_aspect.Checked = true;
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
			Update(m_tb_focus_dist, Camera.FocusDist);
			Update(m_tb_camera_fwd, -Camera.O2W.z);
			Update(m_tb_camera_up, Camera.O2W.y);
			Update(m_tb_zoom, Camera.Zoom);
			Update(m_tb_near, Camera.NearPlane);
			Update(m_tb_far, Camera.FarPlane);
			Update(m_tb_fovX, Math_.RadiansToDegrees(Camera.FovX));
			Update(m_tb_fovY, Math_.RadiansToDegrees(Camera.FovY));
			Update(m_tb_aspect, Camera.Aspect);
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
			this.m_btn_bong = new System.Windows.Forms.Button();
			this.m_tb_focus_dist = new Rylogic.Gui.ValueBox();
			this.m_tb_fovX = new Rylogic.Gui.ValueBox();
			this.m_tb_fovY = new Rylogic.Gui.ValueBox();
			this.m_tb_camera_fwd = new Rylogic.Gui.ValueBox();
			this.m_tb_camera_up = new Rylogic.Gui.ValueBox();
			this.m_tb_focus_point = new Rylogic.Gui.ValueBox();
			this.m_chk_preserve_aspect = new System.Windows.Forms.CheckBox();
			this.m_lbl_aspect_ratio = new System.Windows.Forms.Label();
			this.m_tb_aspect = new Rylogic.Gui.ValueBox();
			this.m_tb_zoom = new Rylogic.Gui.ValueBox();
			this.m_lbl_zoom = new System.Windows.Forms.Label();
			this.m_tb_near = new Rylogic.Gui.ValueBox();
			this.m_lbl_near = new System.Windows.Forms.Label();
			this.m_tb_far = new Rylogic.Gui.ValueBox();
			this.m_lbl_far = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// m_lbl_focus_point
			// 
			this.m_lbl_focus_point.AutoSize = true;
			this.m_lbl_focus_point.Location = new System.Drawing.Point(42, 15);
			this.m_lbl_focus_point.Name = "m_lbl_focus_point";
			this.m_lbl_focus_point.Size = new System.Drawing.Size(66, 13);
			this.m_lbl_focus_point.TabIndex = 0;
			this.m_lbl_focus_point.Text = "Focus Point:";
			this.m_lbl_focus_point.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// m_lbl_camera_up
			// 
			this.m_lbl_camera_up.AutoSize = true;
			this.m_lbl_camera_up.Location = new System.Drawing.Point(45, 99);
			this.m_lbl_camera_up.Name = "m_lbl_camera_up";
			this.m_lbl_camera_up.Size = new System.Drawing.Size(63, 13);
			this.m_lbl_camera_up.TabIndex = 2;
			this.m_lbl_camera_up.Text = "Camera Up:";
			this.m_lbl_camera_up.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// m_lbl_camera_fwd
			// 
			this.m_lbl_camera_fwd.AutoSize = true;
			this.m_lbl_camera_fwd.Location = new System.Drawing.Point(21, 71);
			this.m_lbl_camera_fwd.Name = "m_lbl_camera_fwd";
			this.m_lbl_camera_fwd.Size = new System.Drawing.Size(87, 13);
			this.m_lbl_camera_fwd.TabIndex = 4;
			this.m_lbl_camera_fwd.Text = "Camera Forward:";
			this.m_lbl_camera_fwd.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// m_btn_close
			// 
			this.m_btn_close.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_close.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btn_close.Location = new System.Drawing.Point(145, 307);
			this.m_btn_close.Name = "m_btn_close";
			this.m_btn_close.Size = new System.Drawing.Size(100, 28);
			this.m_btn_close.TabIndex = 8;
			this.m_btn_close.Text = "Close";
			this.m_btn_close.UseVisualStyleBackColor = true;
			// 
			// m_lbl_fovX
			// 
			this.m_lbl_fovX.AutoSize = true;
			this.m_lbl_fovX.Location = new System.Drawing.Point(28, 203);
			this.m_lbl_fovX.Name = "m_lbl_fovX";
			this.m_lbl_fovX.Size = new System.Drawing.Size(80, 13);
			this.m_lbl_fovX.TabIndex = 8;
			this.m_lbl_fovX.Text = "Field of View X:";
			this.m_lbl_fovX.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// m_lbl_fovY
			// 
			this.m_lbl_fovY.AutoSize = true;
			this.m_lbl_fovY.Location = new System.Drawing.Point(28, 232);
			this.m_lbl_fovY.Name = "m_lbl_fovY";
			this.m_lbl_fovY.Size = new System.Drawing.Size(80, 13);
			this.m_lbl_fovY.TabIndex = 10;
			this.m_lbl_fovY.Text = "Field of View Y:";
			this.m_lbl_fovY.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// m_lbl_focus_dist
			// 
			this.m_lbl_focus_dist.AutoSize = true;
			this.m_lbl_focus_dist.Location = new System.Drawing.Point(24, 43);
			this.m_lbl_focus_dist.Name = "m_lbl_focus_dist";
			this.m_lbl_focus_dist.Size = new System.Drawing.Size(84, 13);
			this.m_lbl_focus_dist.TabIndex = 12;
			this.m_lbl_focus_dist.Text = "Focus Distance:";
			this.m_lbl_focus_dist.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// m_btn_bong
			// 
			this.m_btn_bong.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.m_btn_bong.Location = new System.Drawing.Point(11, 307);
			this.m_btn_bong.Name = "m_btn_bong";
			this.m_btn_bong.Size = new System.Drawing.Size(121, 28);
			this.m_btn_bong.TabIndex = 13;
			this.m_btn_bong.Text = "BONG!";
			this.m_btn_bong.UseVisualStyleBackColor = true;
			this.m_btn_bong.Visible = false;
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
			this.m_tb_focus_dist.Size = new System.Drawing.Size(130, 20);
			this.m_tb_focus_dist.TabIndex = 1;
			this.m_tb_focus_dist.UseValidityColours = true;
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
			this.m_tb_fovX.Location = new System.Drawing.Point(114, 200);
			this.m_tb_fovX.Name = "m_tb_fovX";
			this.m_tb_fovX.Size = new System.Drawing.Size(130, 20);
			this.m_tb_fovX.TabIndex = 4;
			this.m_tb_fovX.UseValidityColours = true;
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
			this.m_tb_fovY.Location = new System.Drawing.Point(114, 229);
			this.m_tb_fovY.Name = "m_tb_fovY";
			this.m_tb_fovY.Size = new System.Drawing.Size(130, 20);
			this.m_tb_fovY.TabIndex = 5;
			this.m_tb_fovY.UseValidityColours = true;
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
			this.m_tb_camera_fwd.Size = new System.Drawing.Size(130, 20);
			this.m_tb_camera_fwd.TabIndex = 2;
			this.m_tb_camera_fwd.UseValidityColours = true;
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
			this.m_tb_camera_up.Size = new System.Drawing.Size(130, 20);
			this.m_tb_camera_up.TabIndex = 3;
			this.m_tb_camera_up.UseValidityColours = true;
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
			this.m_tb_focus_point.Size = new System.Drawing.Size(130, 20);
			this.m_tb_focus_point.TabIndex = 0;
			this.m_tb_focus_point.UseValidityColours = true;
			this.m_tb_focus_point.Value = null;
			// 
			// m_chk_preserve_aspect
			// 
			this.m_chk_preserve_aspect.AutoSize = true;
			this.m_chk_preserve_aspect.Location = new System.Drawing.Point(114, 281);
			this.m_chk_preserve_aspect.Name = "m_chk_preserve_aspect";
			this.m_chk_preserve_aspect.Size = new System.Drawing.Size(132, 17);
			this.m_chk_preserve_aspect.TabIndex = 7;
			this.m_chk_preserve_aspect.Text = "Preserve Aspect Ratio";
			this.m_chk_preserve_aspect.UseVisualStyleBackColor = true;
			// 
			// m_lbl_aspect_ratio
			// 
			this.m_lbl_aspect_ratio.AutoSize = true;
			this.m_lbl_aspect_ratio.Location = new System.Drawing.Point(37, 258);
			this.m_lbl_aspect_ratio.Name = "m_lbl_aspect_ratio";
			this.m_lbl_aspect_ratio.Size = new System.Drawing.Size(71, 13);
			this.m_lbl_aspect_ratio.TabIndex = 16;
			this.m_lbl_aspect_ratio.Text = "Aspect Ratio:";
			this.m_lbl_aspect_ratio.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// m_tb_aspect
			// 
			this.m_tb_aspect.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_tb_aspect.BackColorInvalid = System.Drawing.Color.White;
			this.m_tb_aspect.BackColorValid = System.Drawing.Color.White;
			this.m_tb_aspect.ForeColor = System.Drawing.Color.Black;
			this.m_tb_aspect.ForeColorInvalid = System.Drawing.Color.Gray;
			this.m_tb_aspect.ForeColorValid = System.Drawing.Color.Black;
			this.m_tb_aspect.Location = new System.Drawing.Point(114, 255);
			this.m_tb_aspect.Name = "m_tb_aspect";
			this.m_tb_aspect.Size = new System.Drawing.Size(130, 20);
			this.m_tb_aspect.TabIndex = 6;
			this.m_tb_aspect.UseValidityColours = true;
			this.m_tb_aspect.Value = null;
			// 
			// m_tb_zoom
			// 
			this.m_tb_zoom.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_tb_zoom.BackColorInvalid = System.Drawing.Color.White;
			this.m_tb_zoom.BackColorValid = System.Drawing.Color.White;
			this.m_tb_zoom.ForeColor = System.Drawing.Color.Black;
			this.m_tb_zoom.ForeColorInvalid = System.Drawing.Color.Gray;
			this.m_tb_zoom.ForeColorValid = System.Drawing.Color.Black;
			this.m_tb_zoom.Location = new System.Drawing.Point(114, 122);
			this.m_tb_zoom.Name = "m_tb_zoom";
			this.m_tb_zoom.Size = new System.Drawing.Size(130, 20);
			this.m_tb_zoom.TabIndex = 17;
			this.m_tb_zoom.UseValidityColours = true;
			this.m_tb_zoom.Value = null;
			// 
			// m_lbl_zoom
			// 
			this.m_lbl_zoom.AutoSize = true;
			this.m_lbl_zoom.Location = new System.Drawing.Point(71, 125);
			this.m_lbl_zoom.Name = "m_lbl_zoom";
			this.m_lbl_zoom.Size = new System.Drawing.Size(37, 13);
			this.m_lbl_zoom.TabIndex = 18;
			this.m_lbl_zoom.Text = "Zoom:";
			this.m_lbl_zoom.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// m_tb_near
			// 
			this.m_tb_near.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_tb_near.BackColorInvalid = System.Drawing.Color.White;
			this.m_tb_near.BackColorValid = System.Drawing.Color.White;
			this.m_tb_near.ForeColor = System.Drawing.Color.Black;
			this.m_tb_near.ForeColorInvalid = System.Drawing.Color.Gray;
			this.m_tb_near.ForeColorValid = System.Drawing.Color.Black;
			this.m_tb_near.Location = new System.Drawing.Point(114, 148);
			this.m_tb_near.Name = "m_tb_near";
			this.m_tb_near.Size = new System.Drawing.Size(130, 20);
			this.m_tb_near.TabIndex = 19;
			this.m_tb_near.UseValidityColours = true;
			this.m_tb_near.Value = null;
			// 
			// m_lbl_near
			// 
			this.m_lbl_near.AutoSize = true;
			this.m_lbl_near.Location = new System.Drawing.Point(45, 151);
			this.m_lbl_near.Name = "m_lbl_near";
			this.m_lbl_near.Size = new System.Drawing.Size(63, 13);
			this.m_lbl_near.TabIndex = 20;
			this.m_lbl_near.Text = "Near Plane:";
			this.m_lbl_near.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// m_tb_far
			// 
			this.m_tb_far.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_tb_far.BackColorInvalid = System.Drawing.Color.White;
			this.m_tb_far.BackColorValid = System.Drawing.Color.White;
			this.m_tb_far.ForeColor = System.Drawing.Color.Black;
			this.m_tb_far.ForeColorInvalid = System.Drawing.Color.Gray;
			this.m_tb_far.ForeColorValid = System.Drawing.Color.Black;
			this.m_tb_far.Location = new System.Drawing.Point(114, 174);
			this.m_tb_far.Name = "m_tb_far";
			this.m_tb_far.Size = new System.Drawing.Size(130, 20);
			this.m_tb_far.TabIndex = 21;
			this.m_tb_far.UseValidityColours = true;
			this.m_tb_far.Value = null;
			// 
			// m_lbl_far
			// 
			this.m_lbl_far.AutoSize = true;
			this.m_lbl_far.Location = new System.Drawing.Point(53, 177);
			this.m_lbl_far.Name = "m_lbl_far";
			this.m_lbl_far.Size = new System.Drawing.Size(55, 13);
			this.m_lbl_far.TabIndex = 22;
			this.m_lbl_far.Text = "Far Plane:";
			this.m_lbl_far.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// CameraUI
			// 
			this.AcceptButton = this.m_btn_bong;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.m_btn_close;
			this.ClientSize = new System.Drawing.Size(257, 347);
			this.Controls.Add(this.m_tb_far);
			this.Controls.Add(this.m_lbl_far);
			this.Controls.Add(this.m_tb_near);
			this.Controls.Add(this.m_lbl_near);
			this.Controls.Add(this.m_tb_zoom);
			this.Controls.Add(this.m_lbl_zoom);
			this.Controls.Add(this.m_lbl_aspect_ratio);
			this.Controls.Add(this.m_tb_aspect);
			this.Controls.Add(this.m_chk_preserve_aspect);
			this.Controls.Add(this.m_btn_bong);
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
			this.Margin = new System.Windows.Forms.Padding(5);
			this.MinimumSize = new System.Drawing.Size(268, 386);
			this.Name = "CameraUI";
			this.Text = "Camera Properties";
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion
	}
}
