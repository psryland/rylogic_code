using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using pr.extn;
using pr.gfx;
using pr.gui;
using pr.maths;
using pr.util;

namespace CoreCalc
{
	public class CoreOrientationUI :UserControl ,IDockable
	{
		#region UI Elements
		private DockControl m_impl_dock_control;
		private Panel m_panel_core;
		private View3dControl m_view3d_core;
		private Label m_lbl_azimuth_deg;
		private Label m_lbl_inclination_deg;
		private Label m_lbl_azimuth;
		private Label m_lbl_inclination;
		private Label m_lbl_core;
		private ValueBox m_tb_core_azimuth;
		private ValueBox m_tb_core_inclination;
		private ToolTip m_tt;
		private TableLayoutPanel m_table0;
		#endregion

		public CoreOrientationUI(Model model)
		{
			Model = model;
			InitializeComponent();
			DockControl = new DockControl(this, "Core Orientation") { TabText = "Core Orientation" };

			SetupUI();
			UpdateUI();
		}
		protected override void Dispose(bool disposing)
		{
			GfxCore = null;
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}

		/// <summary>App logic</summary>
		private Model Model
		{
			get;
			set;
		}

		/// <summary>Provides support for the DockContainer</summary>
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public DockControl DockControl
		{
			[DebuggerStepThrough] get { return m_impl_dock_control; }
			private set
			{
				if (m_impl_dock_control == value) return;
				Util.Dispose(ref m_impl_dock_control);
				m_impl_dock_control = value;
			}
		}

		/// <summary>The 3d scene</summary>
		private View3d.Window View
		{
			get { return m_view3d_core.Window; }
		}

		/// <summary>Graphics object for the core model</summary>
		private View3d.Object GfxCore
		{
			get { return m_gfx_core; }
			set
			{
				if (m_gfx_core == value) return;
				Util.Dispose(ref m_gfx_core);
				m_gfx_core = value;
			}
		}
		private View3d.Object m_gfx_core;

		/// <summary>Set up the UI</summary>
		private void SetupUI()
		{
			// Core inclination
			m_tb_core_inclination.ToolTip(m_tt, "The inclination of the core axis.\r\n+90° = Straight down\r\n  0° = Horizontal\r\n-90° = Straight Up");
			m_tb_core_inclination.ValueType = typeof(double);
			m_tb_core_inclination.ValidateText = t => { var v = double_.TryParse(t); return v != null && v.Value >= -90 && v.Value <= +90; };
			m_tb_core_inclination.ValueToText = v => ((double)v).ToString("N0");
			m_tb_core_inclination.TextToValue = t => double.Parse(t);
			m_tb_core_inclination.Value = Model.CoreInclinationDeg;
			m_tb_core_inclination.ValueCommitted += (s,a) =>
			{
				Model.CoreInclinationDeg = (double)m_tb_core_inclination.Value;
				UpdateUI();
			};

			// Core Azimuth
			m_tb_core_azimuth.ToolTip(m_tt, "The azimuth of the core axis.\r\n  0° = North, 90° = East, 180° = South, 270° = West");
			m_tb_core_azimuth.ValueType = typeof(double);
			m_tb_core_azimuth.ValidateText = t => { var v = double_.TryParse(t); return v != null && v.Value >= 0 && v.Value <= +360; };
			m_tb_core_azimuth.ValueToText = v => ((double)v).ToString("N0");
			m_tb_core_azimuth.TextToValue = t => double.Parse(t);
			m_tb_core_azimuth.Value = Model.CoreAzimuthDeg;
			m_tb_core_azimuth.ValueCommitted += (s,a) =>
			{
				Model.CoreAzimuthDeg = (double)m_tb_core_azimuth.Value;
				UpdateUI();
			};

			#region Core View
			// Add graphics to the screen
			View.AddObjects(m_view3d_core.View3d.LoadScript(Str.Build(
				"*Plane Ground 80A0A000 ",
				"{",
				"  0 0 0",
				"  0 0 1",
				"  1.0 1.0",
				"}",
				"*Arrow North FFFF0000 ",
				"{",
				"  Fwd",
				"  0 0 0",
				"  0 1 0",
				"  *Width {20}",
				"}"), false, false, null));

			GfxCore = new View3d.Object(Str.Build(
				"*CylinderHR Core FF00AF00 ",
				"{",
				"  3 0.6 0.2",
				"  *Arrow CoreAxis FF000000",
				"  {",
				"     Fwd",
				"     0 0 -1",
				"     0 0 +1 ",
				"     *Width {10}",
				"  }",
				"}"), false);
			View.AddObject(GfxCore);
			View.Camera.ResetView(new v4(0, 1, -1, 0), v4.ZAxis, dist:2.5f);
			m_view3d_core.Invalidate();

			// Core View
			View.OriginVisible = false;
			View.FocusPointVisible = false;
			View.Camera.AlignAxis = v4.ZAxis;

			// Only allow rotation
			m_view3d_core.MouseNavigation = false;
			m_view3d_core.MouseDown += (s, a) =>
			{
				if (View == null || a.Button != MouseButtons.Left) return;
				m_view3d_core.Cursor = Cursors.SizeAll;
				if (View.MouseNavigate(a.Location, a.Button, true))
					View.Invalidate();
			};
			m_view3d_core.MouseMove += (s, a) =>
			{
				if (View == null || a.Button != MouseButtons.Left) return;
				if (View.MouseNavigate(a.Location, a.Button, false))
					m_view3d_core.Invalidate();
			};
			m_view3d_core.MouseUp += (s, a) =>
			{
				if (View == null || a.Button != MouseButtons.Left) return;
				m_view3d_core.Cursor = Cursors.Default;
				if (View.MouseNavigate(a.Location, View3d.ENavOp.None, true))
					m_view3d_core.Invalidate();
			};
			#endregion
		}

		private void UpdateUI()
		{
			GfxCore.O2P = m4x4.Transform(v4.ZAxis, Model.CoreAxis, v4.Origin);
			m_view3d_core.Invalidate();
		}

		#region Component Designer generated code
		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.m_panel_core = new System.Windows.Forms.Panel();
			this.m_lbl_azimuth_deg = new System.Windows.Forms.Label();
			this.m_lbl_inclination_deg = new System.Windows.Forms.Label();
			this.m_lbl_azimuth = new System.Windows.Forms.Label();
			this.m_lbl_inclination = new System.Windows.Forms.Label();
			this.m_lbl_core = new System.Windows.Forms.Label();
			this.m_table0 = new System.Windows.Forms.TableLayoutPanel();
			this.m_tt = new System.Windows.Forms.ToolTip(this.components);
			this.m_view3d_core = new pr.gui.View3dControl();
			this.m_tb_core_azimuth = new pr.gui.ValueBox();
			this.m_tb_core_inclination = new pr.gui.ValueBox();
			this.m_panel_core.SuspendLayout();
			this.m_table0.SuspendLayout();
			this.SuspendLayout();
			// 
			// m_panel_core
			// 
			this.m_panel_core.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_panel_core.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(242)))), ((int)(((byte)(250)))), ((int)(((byte)(255)))));
			this.m_panel_core.Controls.Add(this.m_lbl_azimuth_deg);
			this.m_panel_core.Controls.Add(this.m_lbl_inclination_deg);
			this.m_panel_core.Controls.Add(this.m_lbl_azimuth);
			this.m_panel_core.Controls.Add(this.m_lbl_inclination);
			this.m_panel_core.Controls.Add(this.m_lbl_core);
			this.m_panel_core.Controls.Add(this.m_tb_core_azimuth);
			this.m_panel_core.Controls.Add(this.m_tb_core_inclination);
			this.m_panel_core.Location = new System.Drawing.Point(0, 0);
			this.m_panel_core.Margin = new System.Windows.Forms.Padding(0);
			this.m_panel_core.Name = "m_panel_core";
			this.m_panel_core.Size = new System.Drawing.Size(316, 103);
			this.m_panel_core.TabIndex = 1;
			// 
			// m_lbl_azimuth_deg
			// 
			this.m_lbl_azimuth_deg.AutoSize = true;
			this.m_lbl_azimuth_deg.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.m_lbl_azimuth_deg.Location = new System.Drawing.Point(162, 73);
			this.m_lbl_azimuth_deg.Name = "m_lbl_azimuth_deg";
			this.m_lbl_azimuth_deg.Size = new System.Drawing.Size(51, 13);
			this.m_lbl_azimuth_deg.TabIndex = 6;
			this.m_lbl_azimuth_deg.Text = "(degrees)";
			this.m_lbl_azimuth_deg.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// m_lbl_inclination_deg
			// 
			this.m_lbl_inclination_deg.AutoSize = true;
			this.m_lbl_inclination_deg.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.m_lbl_inclination_deg.Location = new System.Drawing.Point(162, 41);
			this.m_lbl_inclination_deg.Name = "m_lbl_inclination_deg";
			this.m_lbl_inclination_deg.Size = new System.Drawing.Size(51, 13);
			this.m_lbl_inclination_deg.TabIndex = 5;
			this.m_lbl_inclination_deg.Text = "(degrees)";
			this.m_lbl_inclination_deg.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// m_lbl_azimuth
			// 
			this.m_lbl_azimuth.AutoSize = true;
			this.m_lbl_azimuth.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.m_lbl_azimuth.Location = new System.Drawing.Point(25, 68);
			this.m_lbl_azimuth.Name = "m_lbl_azimuth";
			this.m_lbl_azimuth.Size = new System.Drawing.Size(71, 20);
			this.m_lbl_azimuth.TabIndex = 4;
			this.m_lbl_azimuth.Text = "Azimuth:";
			this.m_lbl_azimuth.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// m_lbl_inclination
			// 
			this.m_lbl_inclination.AutoSize = true;
			this.m_lbl_inclination.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.m_lbl_inclination.Location = new System.Drawing.Point(11, 36);
			this.m_lbl_inclination.Name = "m_lbl_inclination";
			this.m_lbl_inclination.Size = new System.Drawing.Size(85, 20);
			this.m_lbl_inclination.TabIndex = 3;
			this.m_lbl_inclination.Text = "Inclination:";
			this.m_lbl_inclination.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// m_lbl_core
			// 
			this.m_lbl_core.AutoSize = true;
			this.m_lbl_core.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.m_lbl_core.Location = new System.Drawing.Point(9, 6);
			this.m_lbl_core.Name = "m_lbl_core";
			this.m_lbl_core.Size = new System.Drawing.Size(193, 24);
			this.m_lbl_core.TabIndex = 2;
			this.m_lbl_core.Text = "Core Axis Orientation:";
			// 
			// m_table0
			// 
			this.m_table0.ColumnCount = 1;
			this.m_table0.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.m_table0.Controls.Add(this.m_view3d_core, 0, 1);
			this.m_table0.Controls.Add(this.m_panel_core, 0, 0);
			this.m_table0.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_table0.Location = new System.Drawing.Point(0, 0);
			this.m_table0.Margin = new System.Windows.Forms.Padding(0);
			this.m_table0.Name = "m_table0";
			this.m_table0.RowCount = 2;
			this.m_table0.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.m_table0.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.m_table0.Size = new System.Drawing.Size(316, 539);
			this.m_table0.TabIndex = 8;
			// 
			// m_view3d_core
			// 
			this.m_view3d_core.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.m_view3d_core.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_view3d_core.Location = new System.Drawing.Point(3, 106);
			this.m_view3d_core.Name = "m_view3d_core";
			this.m_view3d_core.Size = new System.Drawing.Size(310, 430);
			this.m_view3d_core.TabIndex = 7;
			// 
			// m_tb_core_azimuth
			// 
			this.m_tb_core_azimuth.BackColor = System.Drawing.Color.White;
			this.m_tb_core_azimuth.BackColorInvalid = System.Drawing.Color.White;
			this.m_tb_core_azimuth.BackColorValid = System.Drawing.Color.White;
			this.m_tb_core_azimuth.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.m_tb_core_azimuth.ForeColor = System.Drawing.Color.Gray;
			this.m_tb_core_azimuth.ForeColorInvalid = System.Drawing.Color.Gray;
			this.m_tb_core_azimuth.ForeColorValid = System.Drawing.Color.Black;
			this.m_tb_core_azimuth.Location = new System.Drawing.Point(102, 65);
			this.m_tb_core_azimuth.Name = "m_tb_core_azimuth";
			this.m_tb_core_azimuth.Size = new System.Drawing.Size(54, 26);
			this.m_tb_core_azimuth.TabIndex = 1;
			this.m_tb_core_azimuth.UseValidityColours = true;
			this.m_tb_core_azimuth.Value = null;
			// 
			// m_tb_core_inclination
			// 
			this.m_tb_core_inclination.BackColor = System.Drawing.Color.White;
			this.m_tb_core_inclination.BackColorInvalid = System.Drawing.Color.White;
			this.m_tb_core_inclination.BackColorValid = System.Drawing.Color.White;
			this.m_tb_core_inclination.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.m_tb_core_inclination.ForeColor = System.Drawing.Color.Gray;
			this.m_tb_core_inclination.ForeColorInvalid = System.Drawing.Color.Gray;
			this.m_tb_core_inclination.ForeColorValid = System.Drawing.Color.Black;
			this.m_tb_core_inclination.Location = new System.Drawing.Point(102, 33);
			this.m_tb_core_inclination.Name = "m_tb_core_inclination";
			this.m_tb_core_inclination.Size = new System.Drawing.Size(54, 26);
			this.m_tb_core_inclination.TabIndex = 0;
			this.m_tb_core_inclination.UseValidityColours = true;
			this.m_tb_core_inclination.Value = null;
			// 
			// CoreOrientationUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.m_table0);
			this.Name = "CoreOrientationUI";
			this.Size = new System.Drawing.Size(316, 539);
			this.m_panel_core.ResumeLayout(false);
			this.m_panel_core.PerformLayout();
			this.m_table0.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion
	}
}
