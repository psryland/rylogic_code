﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using pr.extn;
using pr.gfx;
using pr.gui;
using pr.maths;

namespace LDraw
{
	public class LightingUI :ToolForm
	{
		private readonly MainUI m_main_ui;

		#region UI Elements
		private RadioButton m_radio_spot;
		private RadioButton m_radio_point;
		private RadioButton m_radio_directional;
		private ValueBox m_tb_position;
		private Label m_lbl_position;
		private Label m_lbl_direction;
		private ValueBox m_tb_direction;
		private CheckBox m_chk_camera_relative;
		private Label m_lbl_range;
		private ValueBox m_tb_range;
		private Label m_lbl_falloff;
		private ValueBox m_tb_falloff;
		private Label m_lbl_shadow_range;
		private ValueBox m_tb_shadow_range;
		private Label m_lbl_ambient;
		private ValueBox m_tb_ambient;
		private Label m_lbl_diffuse;
		private ValueBox m_tb_diffuse;
		private Label m_lbl_specular;
		private ValueBox m_tb_specular;
		private Label m_lbl_specular_power;
		private ValueBox m_tb_specular_power;
		private Label m_lbl_spot_angle_inner;
		private ValueBox m_tb_spot_angle_inner;
		private Label m_lbl_spot_angle_outer;
		private ValueBox m_tb_spot_angle_outer;
		private Button m_btn_ok;
		private RadioButton m_radio_ambient;
		#endregion

		public LightingUI(MainUI main_ui)
			:base(main_ui, EPin.Centre)
		{
			InitializeComponent();
			Icon = main_ui.Icon;

			m_main_ui = main_ui;
			Light = new View3d.Light(m_main_ui.Model.Window.LightProperties);

			SetupUI();
			UpdateUI();
		}
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
		}

		/// <summary>The light source</summary>
		public View3d.Light Light
		{
			get { return m_light; }
			private set
			{
				if (m_light == value) return;
				if (m_light != null)
				{
					m_light.PropertyChanged -= UpdateUI;
				}
				m_light = value;
				if (m_light != null)
				{
					m_light.PropertyChanged += UpdateUI;
				}
			}
		}
		private View3d.Light m_light;
		 
		/// <summary>Set up UI elements</summary>
		private void SetupUI()
		{
			// Type selection
			m_radio_ambient    .Checked = Light.Type == View3d.ELight.Ambient;   
			m_radio_directional.Checked = Light.Type == View3d.ELight.Directional;
			m_radio_point      .Checked = Light.Type == View3d.ELight.Point;     
			m_radio_spot       .Checked = Light.Type == View3d.ELight.Spot;      
			m_radio_ambient    .CheckedChanged += (s,a) => { if (m_radio_ambient    .Checked) Light.Type = View3d.ELight.Ambient;     };
			m_radio_directional.CheckedChanged += (s,a) => { if (m_radio_directional.Checked) Light.Type = View3d.ELight.Directional; };
			m_radio_point      .CheckedChanged += (s,a) => { if (m_radio_point      .Checked) Light.Type = View3d.ELight.Point;       };
			m_radio_spot       .CheckedChanged += (s,a) => { if (m_radio_spot       .Checked) Light.Type = View3d.ELight.Spot;        };

			// Position
			m_tb_position.ValueToText = v => ((v4)v).ToString3();
			m_tb_position.TextToValue = t => v4.Parse3(t, 1f);
			m_tb_position.ValidateText = t => v4.TryParse3(t,1f) != null;
			m_tb_position.Value = Light.Position;
			m_tb_position.ValueChanged += (s,a) => Light.Position = (v4)m_tb_position.Value;

			// Direction
			m_tb_direction.ValueToText = v => ((v4)v).ToString3();
			m_tb_direction.TextToValue = t => v4.Parse3(t, 0f);
			m_tb_direction.ValidateText = t => { var v = v4.TryParse3(t,0f); return v != null && !v4.FEql3(v.Value, v4.Zero); };
			m_tb_direction.Value = Light.Direction;
			m_tb_direction.ValueChanged += (s,a) => Light.Direction = v4.Normalise3((v4)m_tb_direction.Value, -v4.ZAxis);

			// Camera relative
			m_chk_camera_relative.Checked = m_chk_camera_relative.Checked;
			m_chk_camera_relative.CheckedChanged += (s,a) => Light.CameraRelative = m_chk_camera_relative.Checked;

			// Range
			m_tb_range.Value = Light.Range;
			m_tb_range.ValueChanged += (s,a) => Light.Range = (float)m_tb_range.Value;

			// Fall Off
			m_tb_falloff.Value = Light.Falloff;
			m_tb_falloff.ValueChanged += (s,a) => Light.Falloff = (float)m_tb_falloff.Value;

			// Shadow range
			m_tb_shadow_range.Value = Light.CastShadow;
			m_tb_shadow_range.ValueChanged += (s,a) => Light.CastShadow = (float)m_tb_shadow_range.Value;

			// Ambient
			m_tb_ambient.ValueToText = v => ((Colour32)v).ToString();
			m_tb_ambient.TextToValue = t => Colour32.Parse(t);
			m_tb_ambient.ValidateText = t => Colour32.TryParse(t) != null;
			m_tb_ambient.Value = Light.Ambient;
			m_tb_ambient.ValueChanged += (s,a) => Light.Ambient = (Colour32)m_tb_ambient.Value;

			// Diffuse
			m_tb_diffuse.ValueToText = v => ((Colour32)v).ToString();
			m_tb_diffuse.TextToValue = t => Colour32.Parse(t);
			m_tb_diffuse.ValidateText = t => Colour32.TryParse(t) != null;
			m_tb_diffuse.Value = Light.Diffuse;
			m_tb_diffuse.ValueChanged += (s,a) => Light.Diffuse = (Colour32)m_tb_diffuse.Value;

			// Specular
			m_tb_specular.ValueToText = v => ((Colour32)v).ToString();
			m_tb_specular.TextToValue = t => Colour32.Parse(t);
			m_tb_specular.ValidateText = t => Colour32.TryParse(t) != null;
			m_tb_specular.Value = Light.Specular;
			m_tb_specular.ValueChanged += (s,a) => Light.Specular = (Colour32)m_tb_specular.Value;

			// Specular Power
			m_tb_specular_power.Value = Light.SpecularPower;
			m_tb_specular_power.ValueChanged += (s,a) => Light.SpecularPower = (float)m_tb_specular_power.Value;

			// Spot inner angle
			m_tb_spot_angle_inner.Value = Light.InnerAngle;
			m_tb_spot_angle_inner.ValueChanged += (s,a) => Light.InnerAngle = (float)m_tb_spot_angle_inner.Value;

			// Spot outer angle
			m_tb_spot_angle_outer.Value = Light.OuterAngle;
			m_tb_spot_angle_outer.ValueChanged += (s,a) => Light.OuterAngle = (float)m_tb_spot_angle_outer.Value;
		}

		/// <summary>Update UI elements</summary>
		private void UpdateUI(object sender = null, EventArgs args = null)
		{
			// Update the UI from the light properties
			switch (Light.Type)
			{
			case View3d.ELight.Ambient:
				{
					m_tb_position        .Enabled = false;
					m_tb_direction       .Enabled = false;
					m_chk_camera_relative.Enabled = false;
					m_tb_range           .Enabled = false;
					m_tb_falloff         .Enabled = false;
					m_tb_shadow_range    .Enabled = false;
					m_tb_ambient         .Enabled = true;
					m_tb_diffuse         .Enabled = false;
					m_tb_specular        .Enabled = false;
					m_tb_specular_power  .Enabled = false;
					m_tb_spot_angle_inner.Enabled = false;
					m_tb_spot_angle_outer.Enabled = false;
					break;
				}
			case View3d.ELight.Directional:
				{
					m_tb_position        .Enabled = false;
					m_tb_direction       .Enabled = true;
					m_chk_camera_relative.Enabled = true;
					m_tb_range           .Enabled = false;
					m_tb_falloff         .Enabled = false;
					m_tb_shadow_range    .Enabled = true;
					m_tb_ambient         .Enabled = true;
					m_tb_diffuse         .Enabled = true;
					m_tb_specular        .Enabled = true;
					m_tb_specular_power  .Enabled = true;
					m_tb_spot_angle_inner.Enabled = false;
					m_tb_spot_angle_outer.Enabled = false;
					break;
				}
			case View3d.ELight.Point:
				{
					m_tb_position        .Enabled = true;
					m_tb_direction       .Enabled = false;
					m_chk_camera_relative.Enabled = true;
					m_tb_range           .Enabled = false;
					m_tb_falloff         .Enabled = false;
					m_tb_shadow_range    .Enabled = true;
					m_tb_ambient         .Enabled = true;
					m_tb_diffuse         .Enabled = true;
					m_tb_specular        .Enabled = true;
					m_tb_specular_power  .Enabled = true;
					m_tb_spot_angle_inner.Enabled = false;
					m_tb_spot_angle_outer.Enabled = false;
					break;
				}
			case View3d.ELight.Spot:
				{
					m_tb_position        .Enabled = true;
					m_tb_direction       .Enabled = true;
					m_chk_camera_relative.Enabled = true;
					m_tb_range           .Enabled = true;
					m_tb_falloff         .Enabled = true;
					m_tb_shadow_range    .Enabled = true;
					m_tb_ambient         .Enabled = true;
					m_tb_diffuse         .Enabled = true;
					m_tb_specular        .Enabled = true;
					m_tb_specular_power  .Enabled = true;
					m_tb_spot_angle_inner.Enabled = true;
					m_tb_spot_angle_outer.Enabled = true;
					break;
				}
			}

			// Update the light
			m_main_ui.Model.Window.LightProperties = m_light;
			m_main_ui.Model.Window.Invalidate();
		}

		#region Windows Form Designer generated code
		private void InitializeComponent()
		{
			this.m_radio_ambient = new System.Windows.Forms.RadioButton();
			this.m_radio_spot = new System.Windows.Forms.RadioButton();
			this.m_radio_point = new System.Windows.Forms.RadioButton();
			this.m_radio_directional = new System.Windows.Forms.RadioButton();
			this.m_tb_position = new pr.gui.ValueBox();
			this.m_lbl_position = new System.Windows.Forms.Label();
			this.m_lbl_direction = new System.Windows.Forms.Label();
			this.m_tb_direction = new pr.gui.ValueBox();
			this.m_chk_camera_relative = new System.Windows.Forms.CheckBox();
			this.m_lbl_range = new System.Windows.Forms.Label();
			this.m_tb_range = new pr.gui.ValueBox();
			this.m_lbl_falloff = new System.Windows.Forms.Label();
			this.m_tb_falloff = new pr.gui.ValueBox();
			this.m_lbl_shadow_range = new System.Windows.Forms.Label();
			this.m_tb_shadow_range = new pr.gui.ValueBox();
			this.m_lbl_ambient = new System.Windows.Forms.Label();
			this.m_tb_ambient = new pr.gui.ValueBox();
			this.m_lbl_diffuse = new System.Windows.Forms.Label();
			this.m_tb_diffuse = new pr.gui.ValueBox();
			this.m_lbl_specular = new System.Windows.Forms.Label();
			this.m_tb_specular = new pr.gui.ValueBox();
			this.m_lbl_specular_power = new System.Windows.Forms.Label();
			this.m_tb_specular_power = new pr.gui.ValueBox();
			this.m_lbl_spot_angle_inner = new System.Windows.Forms.Label();
			this.m_tb_spot_angle_inner = new pr.gui.ValueBox();
			this.m_lbl_spot_angle_outer = new System.Windows.Forms.Label();
			this.m_tb_spot_angle_outer = new pr.gui.ValueBox();
			this.m_btn_ok = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// m_radio_ambient
			// 
			this.m_radio_ambient.AutoSize = true;
			this.m_radio_ambient.Location = new System.Drawing.Point(12, 12);
			this.m_radio_ambient.Name = "m_radio_ambient";
			this.m_radio_ambient.Size = new System.Drawing.Size(75, 20);
			this.m_radio_ambient.TabIndex = 0;
			this.m_radio_ambient.TabStop = true;
			this.m_radio_ambient.Text = "Ambient";
			this.m_radio_ambient.UseVisualStyleBackColor = true;
			// 
			// m_radio_spot
			// 
			this.m_radio_spot.AutoSize = true;
			this.m_radio_spot.Location = new System.Drawing.Point(12, 90);
			this.m_radio_spot.Name = "m_radio_spot";
			this.m_radio_spot.Size = new System.Drawing.Size(54, 20);
			this.m_radio_spot.TabIndex = 3;
			this.m_radio_spot.TabStop = true;
			this.m_radio_spot.Text = "Spot";
			this.m_radio_spot.UseVisualStyleBackColor = true;
			// 
			// m_radio_point
			// 
			this.m_radio_point.AutoSize = true;
			this.m_radio_point.Location = new System.Drawing.Point(12, 64);
			this.m_radio_point.Name = "m_radio_point";
			this.m_radio_point.Size = new System.Drawing.Size(56, 20);
			this.m_radio_point.TabIndex = 2;
			this.m_radio_point.TabStop = true;
			this.m_radio_point.Text = "Point";
			this.m_radio_point.UseVisualStyleBackColor = true;
			// 
			// m_radio_directional
			// 
			this.m_radio_directional.AutoSize = true;
			this.m_radio_directional.Location = new System.Drawing.Point(12, 38);
			this.m_radio_directional.Name = "m_radio_directional";
			this.m_radio_directional.Size = new System.Drawing.Size(90, 20);
			this.m_radio_directional.TabIndex = 1;
			this.m_radio_directional.TabStop = true;
			this.m_radio_directional.Text = "Directional";
			this.m_radio_directional.UseVisualStyleBackColor = true;
			// 
			// m_tb_position
			// 
			this.m_tb_position.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_tb_position.BackColor = System.Drawing.Color.White;
			this.m_tb_position.BackColorInvalid = System.Drawing.Color.White;
			this.m_tb_position.BackColorValid = System.Drawing.Color.White;
			this.m_tb_position.ForeColor = System.Drawing.Color.Black;
			this.m_tb_position.ForeColorInvalid = System.Drawing.Color.Gray;
			this.m_tb_position.ForeColorValid = System.Drawing.Color.Black;
			this.m_tb_position.Location = new System.Drawing.Point(177, 11);
			this.m_tb_position.Name = "m_tb_position";
			this.m_tb_position.Size = new System.Drawing.Size(131, 22);
			this.m_tb_position.TabIndex = 4;
			this.m_tb_position.Value = null;
			// 
			// m_lbl_position
			// 
			this.m_lbl_position.AutoSize = true;
			this.m_lbl_position.Location = new System.Drawing.Point(112, 14);
			this.m_lbl_position.Name = "m_lbl_position";
			this.m_lbl_position.Size = new System.Drawing.Size(59, 16);
			this.m_lbl_position.TabIndex = 5;
			this.m_lbl_position.Text = "Position:";
			this.m_lbl_position.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// m_lbl_direction
			// 
			this.m_lbl_direction.AutoSize = true;
			this.m_lbl_direction.Location = new System.Drawing.Point(107, 42);
			this.m_lbl_direction.Name = "m_lbl_direction";
			this.m_lbl_direction.Size = new System.Drawing.Size(64, 16);
			this.m_lbl_direction.TabIndex = 7;
			this.m_lbl_direction.Text = "Direction:";
			this.m_lbl_direction.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// m_tb_direction
			// 
			this.m_tb_direction.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_tb_direction.BackColor = System.Drawing.Color.White;
			this.m_tb_direction.BackColorInvalid = System.Drawing.Color.White;
			this.m_tb_direction.BackColorValid = System.Drawing.Color.White;
			this.m_tb_direction.ForeColor = System.Drawing.Color.Black;
			this.m_tb_direction.ForeColorInvalid = System.Drawing.Color.Gray;
			this.m_tb_direction.ForeColorValid = System.Drawing.Color.Black;
			this.m_tb_direction.Location = new System.Drawing.Point(177, 39);
			this.m_tb_direction.Name = "m_tb_direction";
			this.m_tb_direction.Size = new System.Drawing.Size(131, 22);
			this.m_tb_direction.TabIndex = 5;
			this.m_tb_direction.Value = null;
			// 
			// m_chk_camera_relative
			// 
			this.m_chk_camera_relative.AutoSize = true;
			this.m_chk_camera_relative.CheckAlign = System.Drawing.ContentAlignment.MiddleRight;
			this.m_chk_camera_relative.Location = new System.Drawing.Point(177, 67);
			this.m_chk_camera_relative.Name = "m_chk_camera_relative";
			this.m_chk_camera_relative.Size = new System.Drawing.Size(131, 20);
			this.m_chk_camera_relative.TabIndex = 6;
			this.m_chk_camera_relative.Text = "Camera Relative:";
			this.m_chk_camera_relative.UseVisualStyleBackColor = true;
			// 
			// m_lbl_range
			// 
			this.m_lbl_range.AutoSize = true;
			this.m_lbl_range.Location = new System.Drawing.Point(119, 96);
			this.m_lbl_range.Name = "m_lbl_range";
			this.m_lbl_range.Size = new System.Drawing.Size(52, 16);
			this.m_lbl_range.TabIndex = 10;
			this.m_lbl_range.Text = "Range:";
			this.m_lbl_range.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// m_tb_range
			// 
			this.m_tb_range.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_tb_range.BackColor = System.Drawing.Color.White;
			this.m_tb_range.BackColorInvalid = System.Drawing.Color.White;
			this.m_tb_range.BackColorValid = System.Drawing.Color.White;
			this.m_tb_range.ForeColor = System.Drawing.Color.Black;
			this.m_tb_range.ForeColorInvalid = System.Drawing.Color.Gray;
			this.m_tb_range.ForeColorValid = System.Drawing.Color.Black;
			this.m_tb_range.Location = new System.Drawing.Point(177, 93);
			this.m_tb_range.Name = "m_tb_range";
			this.m_tb_range.Size = new System.Drawing.Size(131, 22);
			this.m_tb_range.TabIndex = 7;
			this.m_tb_range.Value = null;
			// 
			// m_lbl_falloff
			// 
			this.m_lbl_falloff.AutoSize = true;
			this.m_lbl_falloff.Location = new System.Drawing.Point(119, 124);
			this.m_lbl_falloff.Name = "m_lbl_falloff";
			this.m_lbl_falloff.Size = new System.Drawing.Size(52, 16);
			this.m_lbl_falloff.TabIndex = 12;
			this.m_lbl_falloff.Text = "Fall Off:";
			this.m_lbl_falloff.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// m_tb_falloff
			// 
			this.m_tb_falloff.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_tb_falloff.BackColor = System.Drawing.Color.White;
			this.m_tb_falloff.BackColorInvalid = System.Drawing.Color.White;
			this.m_tb_falloff.BackColorValid = System.Drawing.Color.White;
			this.m_tb_falloff.ForeColor = System.Drawing.Color.Black;
			this.m_tb_falloff.ForeColorInvalid = System.Drawing.Color.Gray;
			this.m_tb_falloff.ForeColorValid = System.Drawing.Color.Black;
			this.m_tb_falloff.Location = new System.Drawing.Point(177, 121);
			this.m_tb_falloff.Name = "m_tb_falloff";
			this.m_tb_falloff.Size = new System.Drawing.Size(131, 22);
			this.m_tb_falloff.TabIndex = 8;
			this.m_tb_falloff.Value = null;
			// 
			// m_lbl_shadow_range
			// 
			this.m_lbl_shadow_range.AutoSize = true;
			this.m_lbl_shadow_range.Location = new System.Drawing.Point(67, 152);
			this.m_lbl_shadow_range.Name = "m_lbl_shadow_range";
			this.m_lbl_shadow_range.Size = new System.Drawing.Size(104, 16);
			this.m_lbl_shadow_range.TabIndex = 14;
			this.m_lbl_shadow_range.Text = "Shadow Range:";
			this.m_lbl_shadow_range.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// m_tb_shadow_range
			// 
			this.m_tb_shadow_range.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_tb_shadow_range.BackColor = System.Drawing.Color.White;
			this.m_tb_shadow_range.BackColorInvalid = System.Drawing.Color.White;
			this.m_tb_shadow_range.BackColorValid = System.Drawing.Color.White;
			this.m_tb_shadow_range.ForeColor = System.Drawing.Color.Black;
			this.m_tb_shadow_range.ForeColorInvalid = System.Drawing.Color.Gray;
			this.m_tb_shadow_range.ForeColorValid = System.Drawing.Color.Black;
			this.m_tb_shadow_range.Location = new System.Drawing.Point(177, 149);
			this.m_tb_shadow_range.Name = "m_tb_shadow_range";
			this.m_tb_shadow_range.Size = new System.Drawing.Size(131, 22);
			this.m_tb_shadow_range.TabIndex = 9;
			this.m_tb_shadow_range.Value = null;
			// 
			// m_lbl_ambient
			// 
			this.m_lbl_ambient.AutoSize = true;
			this.m_lbl_ambient.Location = new System.Drawing.Point(44, 180);
			this.m_lbl_ambient.Name = "m_lbl_ambient";
			this.m_lbl_ambient.Size = new System.Drawing.Size(127, 16);
			this.m_lbl_ambient.TabIndex = 16;
			this.m_lbl_ambient.Text = "Ambient (aarrggbb):";
			this.m_lbl_ambient.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// m_tb_ambient
			// 
			this.m_tb_ambient.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_tb_ambient.BackColor = System.Drawing.Color.White;
			this.m_tb_ambient.BackColorInvalid = System.Drawing.Color.White;
			this.m_tb_ambient.BackColorValid = System.Drawing.Color.White;
			this.m_tb_ambient.ForeColor = System.Drawing.Color.Black;
			this.m_tb_ambient.ForeColorInvalid = System.Drawing.Color.Gray;
			this.m_tb_ambient.ForeColorValid = System.Drawing.Color.Black;
			this.m_tb_ambient.Location = new System.Drawing.Point(177, 177);
			this.m_tb_ambient.Name = "m_tb_ambient";
			this.m_tb_ambient.Size = new System.Drawing.Size(131, 22);
			this.m_tb_ambient.TabIndex = 10;
			this.m_tb_ambient.Value = null;
			// 
			// m_lbl_diffuse
			// 
			this.m_lbl_diffuse.AutoSize = true;
			this.m_lbl_diffuse.Location = new System.Drawing.Point(52, 208);
			this.m_lbl_diffuse.Name = "m_lbl_diffuse";
			this.m_lbl_diffuse.Size = new System.Drawing.Size(119, 16);
			this.m_lbl_diffuse.TabIndex = 18;
			this.m_lbl_diffuse.Text = "Diffuse (aarrggbb):";
			this.m_lbl_diffuse.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// m_tb_diffuse
			// 
			this.m_tb_diffuse.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_tb_diffuse.BackColor = System.Drawing.Color.White;
			this.m_tb_diffuse.BackColorInvalid = System.Drawing.Color.White;
			this.m_tb_diffuse.BackColorValid = System.Drawing.Color.White;
			this.m_tb_diffuse.ForeColor = System.Drawing.Color.Black;
			this.m_tb_diffuse.ForeColorInvalid = System.Drawing.Color.Gray;
			this.m_tb_diffuse.ForeColorValid = System.Drawing.Color.Black;
			this.m_tb_diffuse.Location = new System.Drawing.Point(177, 205);
			this.m_tb_diffuse.Name = "m_tb_diffuse";
			this.m_tb_diffuse.Size = new System.Drawing.Size(131, 22);
			this.m_tb_diffuse.TabIndex = 11;
			this.m_tb_diffuse.Value = null;
			// 
			// m_lbl_specular
			// 
			this.m_lbl_specular.AutoSize = true;
			this.m_lbl_specular.Location = new System.Drawing.Point(39, 236);
			this.m_lbl_specular.Name = "m_lbl_specular";
			this.m_lbl_specular.Size = new System.Drawing.Size(132, 16);
			this.m_lbl_specular.TabIndex = 20;
			this.m_lbl_specular.Text = "Specular (aarrggbb):";
			this.m_lbl_specular.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// m_tb_specular
			// 
			this.m_tb_specular.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_tb_specular.BackColor = System.Drawing.Color.White;
			this.m_tb_specular.BackColorInvalid = System.Drawing.Color.White;
			this.m_tb_specular.BackColorValid = System.Drawing.Color.White;
			this.m_tb_specular.ForeColor = System.Drawing.Color.Black;
			this.m_tb_specular.ForeColorInvalid = System.Drawing.Color.Gray;
			this.m_tb_specular.ForeColorValid = System.Drawing.Color.Black;
			this.m_tb_specular.Location = new System.Drawing.Point(177, 233);
			this.m_tb_specular.Name = "m_tb_specular";
			this.m_tb_specular.Size = new System.Drawing.Size(131, 22);
			this.m_tb_specular.TabIndex = 12;
			this.m_tb_specular.Value = null;
			// 
			// m_lbl_specular_power
			// 
			this.m_lbl_specular_power.AutoSize = true;
			this.m_lbl_specular_power.Location = new System.Drawing.Point(65, 264);
			this.m_lbl_specular_power.Name = "m_lbl_specular_power";
			this.m_lbl_specular_power.Size = new System.Drawing.Size(106, 16);
			this.m_lbl_specular_power.TabIndex = 22;
			this.m_lbl_specular_power.Text = "Specular Power:";
			this.m_lbl_specular_power.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// m_tb_specular_power
			// 
			this.m_tb_specular_power.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_tb_specular_power.BackColor = System.Drawing.Color.White;
			this.m_tb_specular_power.BackColorInvalid = System.Drawing.Color.White;
			this.m_tb_specular_power.BackColorValid = System.Drawing.Color.White;
			this.m_tb_specular_power.ForeColor = System.Drawing.Color.Black;
			this.m_tb_specular_power.ForeColorInvalid = System.Drawing.Color.Gray;
			this.m_tb_specular_power.ForeColorValid = System.Drawing.Color.Black;
			this.m_tb_specular_power.Location = new System.Drawing.Point(177, 261);
			this.m_tb_specular_power.Name = "m_tb_specular_power";
			this.m_tb_specular_power.Size = new System.Drawing.Size(131, 22);
			this.m_tb_specular_power.TabIndex = 13;
			this.m_tb_specular_power.Value = null;
			// 
			// m_lbl_spot_angle_inner
			// 
			this.m_lbl_spot_angle_inner.AutoSize = true;
			this.m_lbl_spot_angle_inner.Location = new System.Drawing.Point(62, 292);
			this.m_lbl_spot_angle_inner.Name = "m_lbl_spot_angle_inner";
			this.m_lbl_spot_angle_inner.Size = new System.Drawing.Size(109, 16);
			this.m_lbl_spot_angle_inner.TabIndex = 24;
			this.m_lbl_spot_angle_inner.Text = "Inner Spot Angle:";
			this.m_lbl_spot_angle_inner.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// m_tb_spot_angle_inner
			// 
			this.m_tb_spot_angle_inner.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_tb_spot_angle_inner.BackColor = System.Drawing.Color.White;
			this.m_tb_spot_angle_inner.BackColorInvalid = System.Drawing.Color.White;
			this.m_tb_spot_angle_inner.BackColorValid = System.Drawing.Color.White;
			this.m_tb_spot_angle_inner.ForeColor = System.Drawing.Color.Black;
			this.m_tb_spot_angle_inner.ForeColorInvalid = System.Drawing.Color.Gray;
			this.m_tb_spot_angle_inner.ForeColorValid = System.Drawing.Color.Black;
			this.m_tb_spot_angle_inner.Location = new System.Drawing.Point(177, 289);
			this.m_tb_spot_angle_inner.Name = "m_tb_spot_angle_inner";
			this.m_tb_spot_angle_inner.Size = new System.Drawing.Size(131, 22);
			this.m_tb_spot_angle_inner.TabIndex = 14;
			this.m_tb_spot_angle_inner.Value = null;
			// 
			// m_lbl_spot_angle_outer
			// 
			this.m_lbl_spot_angle_outer.AutoSize = true;
			this.m_lbl_spot_angle_outer.Location = new System.Drawing.Point(59, 320);
			this.m_lbl_spot_angle_outer.Name = "m_lbl_spot_angle_outer";
			this.m_lbl_spot_angle_outer.Size = new System.Drawing.Size(112, 16);
			this.m_lbl_spot_angle_outer.TabIndex = 26;
			this.m_lbl_spot_angle_outer.Text = "Outer Spot Angle:";
			this.m_lbl_spot_angle_outer.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// m_tb_spot_angle_outer
			// 
			this.m_tb_spot_angle_outer.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_tb_spot_angle_outer.BackColor = System.Drawing.Color.White;
			this.m_tb_spot_angle_outer.BackColorInvalid = System.Drawing.Color.White;
			this.m_tb_spot_angle_outer.BackColorValid = System.Drawing.Color.White;
			this.m_tb_spot_angle_outer.ForeColor = System.Drawing.Color.Black;
			this.m_tb_spot_angle_outer.ForeColorInvalid = System.Drawing.Color.Gray;
			this.m_tb_spot_angle_outer.ForeColorValid = System.Drawing.Color.Black;
			this.m_tb_spot_angle_outer.Location = new System.Drawing.Point(177, 317);
			this.m_tb_spot_angle_outer.Name = "m_tb_spot_angle_outer";
			this.m_tb_spot_angle_outer.Size = new System.Drawing.Size(131, 22);
			this.m_tb_spot_angle_outer.TabIndex = 15;
			this.m_tb_spot_angle_outer.Value = null;
			// 
			// m_btn_ok
			// 
			this.m_btn_ok.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_ok.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btn_ok.Location = new System.Drawing.Point(190, 350);
			this.m_btn_ok.Name = "m_btn_ok";
			this.m_btn_ok.Size = new System.Drawing.Size(118, 30);
			this.m_btn_ok.TabIndex = 17;
			this.m_btn_ok.Text = "Close";
			this.m_btn_ok.UseVisualStyleBackColor = true;
			// 
			// LightingUI
			// 
			this.AcceptButton = this.m_btn_ok;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(320, 392);
			this.Controls.Add(this.m_btn_ok);
			this.Controls.Add(this.m_lbl_spot_angle_outer);
			this.Controls.Add(this.m_tb_spot_angle_outer);
			this.Controls.Add(this.m_lbl_spot_angle_inner);
			this.Controls.Add(this.m_tb_spot_angle_inner);
			this.Controls.Add(this.m_lbl_specular_power);
			this.Controls.Add(this.m_tb_specular_power);
			this.Controls.Add(this.m_lbl_specular);
			this.Controls.Add(this.m_tb_specular);
			this.Controls.Add(this.m_lbl_diffuse);
			this.Controls.Add(this.m_tb_diffuse);
			this.Controls.Add(this.m_lbl_ambient);
			this.Controls.Add(this.m_tb_ambient);
			this.Controls.Add(this.m_lbl_shadow_range);
			this.Controls.Add(this.m_tb_shadow_range);
			this.Controls.Add(this.m_lbl_falloff);
			this.Controls.Add(this.m_tb_falloff);
			this.Controls.Add(this.m_lbl_range);
			this.Controls.Add(this.m_tb_range);
			this.Controls.Add(this.m_chk_camera_relative);
			this.Controls.Add(this.m_lbl_direction);
			this.Controls.Add(this.m_tb_direction);
			this.Controls.Add(this.m_lbl_position);
			this.Controls.Add(this.m_tb_position);
			this.Controls.Add(this.m_radio_directional);
			this.Controls.Add(this.m_radio_point);
			this.Controls.Add(this.m_radio_spot);
			this.Controls.Add(this.m_radio_ambient);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
			this.Margin = new System.Windows.Forms.Padding(7, 6, 7, 6);
			this.MinimumSize = new System.Drawing.Size(336, 431);
			this.Name = "LightingUI";
			this.PinOffset = new System.Drawing.Point(-300, 0);
			this.Text = "Lighting Properties";
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion
	}
}