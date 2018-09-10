using System.Windows.Forms;
using Rylogic.Extn;
using Rylogic.Gui.WinForms;
using Rylogic.Maths;
using Util = Rylogic.Utility.Util;

namespace LDraw
{
	public class SettingsUI :ToolForm
	{
		#region UI Elements
		private TabControl m_tab;
		private TabPage m_tab_general;
		private TabPage m_tab_rendering;
		private GroupBox m_grp_error_log;
		private CheckBox m_chk_show_log_on_new_messages;
		private GroupBox m_grp_camera;
		private CheckBox m_chk_clear_log_on_reload;
		private TrackBar m_track_focus_point_size;
		private Label m_lbl_focus_point_size;
		private CheckBox m_chk_origin_point_visible;
		private TrackBar m_track_origin_point_size;
		private Label m_lbl_origin_point_size;
		private CheckBox m_chk_focus_point_visible;
		private PropertyGrid m_propgrid_rendering;
		#endregion

		public SettingsUI(Form parent, Settings settings)
			:base(parent, EPin.Centre)
		{
			InitializeComponent();
			Settings = settings;
			Icon = parent.Icon;

			SetupUI();
		}
		protected override void Dispose(bool disposing)
		{
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}

		/// <summary>The settings being modified</summary>
		public Settings Settings
		{
			get;
			private set;
		}

		/// <summary>Set up UI elements</summary>
		private void SetupUI()
		{
			#region Error log

			// Clear on reload
			m_chk_clear_log_on_reload.Checked = Settings.UI.ClearErrorLogOnReload;
			m_chk_clear_log_on_reload.CheckedChanged += (s,a) =>
			{
				Settings.UI.ClearErrorLogOnReload = !Settings.UI.ClearErrorLogOnReload;
			};

			// Pop-out error log
			m_chk_show_log_on_new_messages.Checked = Settings.UI.ShowErrorLogOnNewMessages;
			m_chk_show_log_on_new_messages.CheckedChanged += (s,a) =>
			{
				Settings.UI.ShowErrorLogOnNewMessages = !Settings.UI.ShowErrorLogOnNewMessages;
			};
			#endregion

			#region Camera

			// Focus point visible
			m_chk_focus_point_visible.Checked = Settings.FocusPointVisible;
			m_chk_focus_point_visible.CheckedChanged += (s,a) =>
			{
				Settings.FocusPointVisible = m_chk_focus_point_visible.Checked;
			};

			// Use a quadratic to scale the focus point size;
			const float point_scale = 5.0f;
			var quadratic = Quadratic.FromPoints(new v2(0f, 1f/point_scale), new v2(0.5f, 1f), new v2(1f, point_scale));

			// Use a quadratic to scale the point size
			m_track_focus_point_size.ValueFrac((float)quadratic.Solve(Settings.FocusPointSize).Back());
			m_track_focus_point_size.ValueChanged += (s,a) =>
			{
				var size = (float)quadratic.F(m_track_focus_point_size.ValueFrac());
				Settings.FocusPointSize = size;
			};

			// Origin Point
			m_chk_origin_point_visible.Checked = Settings.OriginPointVisible;
			m_chk_origin_point_visible.CheckedChanged += (s,a) =>
			{
				Settings.OriginPointVisible = m_chk_origin_point_visible.Checked;
			};

			// Use a quadratic to scale the point size
			m_track_origin_point_size.ValueFrac((float)quadratic.Solve(Settings.OriginPointSize).Back());
			m_track_origin_point_size.ValueChanged += (s,a) =>
			{
				var size = (float)quadratic.F(m_track_origin_point_size.ValueFrac());
				Settings.OriginPointSize = size;
			};

			#endregion

			// Rendering property grid
			m_propgrid_rendering.SelectedObject = Settings.Scenes;
		}

		#region Windows Form Designer generated code
		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			this.m_tab = new System.Windows.Forms.TabControl();
			this.m_tab_general = new System.Windows.Forms.TabPage();
			this.m_grp_error_log = new System.Windows.Forms.GroupBox();
			this.m_chk_clear_log_on_reload = new System.Windows.Forms.CheckBox();
			this.m_chk_show_log_on_new_messages = new System.Windows.Forms.CheckBox();
			this.m_grp_camera = new System.Windows.Forms.GroupBox();
			this.m_tab_rendering = new System.Windows.Forms.TabPage();
			this.m_propgrid_rendering = new System.Windows.Forms.PropertyGrid();
			this.m_lbl_focus_point_size = new System.Windows.Forms.Label();
			this.m_track_focus_point_size = new System.Windows.Forms.TrackBar();
			this.m_chk_focus_point_visible = new System.Windows.Forms.CheckBox();
			this.m_chk_origin_point_visible = new System.Windows.Forms.CheckBox();
			this.m_track_origin_point_size = new System.Windows.Forms.TrackBar();
			this.m_lbl_origin_point_size = new System.Windows.Forms.Label();
			this.m_tab.SuspendLayout();
			this.m_tab_general.SuspendLayout();
			this.m_grp_error_log.SuspendLayout();
			this.m_grp_camera.SuspendLayout();
			this.m_tab_rendering.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_track_focus_point_size)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.m_track_origin_point_size)).BeginInit();
			this.SuspendLayout();
			// 
			// m_tab
			// 
			this.m_tab.Controls.Add(this.m_tab_general);
			this.m_tab.Controls.Add(this.m_tab_rendering);
			this.m_tab.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_tab.Location = new System.Drawing.Point(0, 0);
			this.m_tab.Name = "m_tab";
			this.m_tab.SelectedIndex = 0;
			this.m_tab.Size = new System.Drawing.Size(441, 468);
			this.m_tab.TabIndex = 0;
			// 
			// m_tab_general
			// 
			this.m_tab_general.Controls.Add(this.m_grp_error_log);
			this.m_tab_general.Controls.Add(this.m_grp_camera);
			this.m_tab_general.Location = new System.Drawing.Point(4, 22);
			this.m_tab_general.Name = "m_tab_general";
			this.m_tab_general.Padding = new System.Windows.Forms.Padding(3);
			this.m_tab_general.Size = new System.Drawing.Size(433, 442);
			this.m_tab_general.TabIndex = 0;
			this.m_tab_general.Text = "General";
			this.m_tab_general.UseVisualStyleBackColor = true;
			// 
			// m_grp_error_log
			// 
			this.m_grp_error_log.Controls.Add(this.m_chk_clear_log_on_reload);
			this.m_grp_error_log.Controls.Add(this.m_chk_show_log_on_new_messages);
			this.m_grp_error_log.Location = new System.Drawing.Point(8, 6);
			this.m_grp_error_log.Name = "m_grp_error_log";
			this.m_grp_error_log.Size = new System.Drawing.Size(222, 70);
			this.m_grp_error_log.TabIndex = 2;
			this.m_grp_error_log.TabStop = false;
			this.m_grp_error_log.Text = "Error Log";
			// 
			// m_chk_clear_log_on_reload
			// 
			this.m_chk_clear_log_on_reload.AutoSize = true;
			this.m_chk_clear_log_on_reload.Location = new System.Drawing.Point(6, 19);
			this.m_chk_clear_log_on_reload.Name = "m_chk_clear_log_on_reload";
			this.m_chk_clear_log_on_reload.Size = new System.Drawing.Size(114, 17);
			this.m_chk_clear_log_on_reload.TabIndex = 2;
			this.m_chk_clear_log_on_reload.Text = "Clear log on reload";
			this.m_chk_clear_log_on_reload.UseVisualStyleBackColor = true;
			// 
			// m_chk_show_log_on_new_messages
			// 
			this.m_chk_show_log_on_new_messages.AutoSize = true;
			this.m_chk_show_log_on_new_messages.Location = new System.Drawing.Point(6, 42);
			this.m_chk_show_log_on_new_messages.Name = "m_chk_show_log_on_new_messages";
			this.m_chk_show_log_on_new_messages.Size = new System.Drawing.Size(201, 17);
			this.m_chk_show_log_on_new_messages.TabIndex = 1;
			this.m_chk_show_log_on_new_messages.Text = "Show Log window on new messages";
			this.m_chk_show_log_on_new_messages.UseVisualStyleBackColor = true;
			// 
			// m_grp_camera
			// 
			this.m_grp_camera.Controls.Add(this.m_chk_origin_point_visible);
			this.m_grp_camera.Controls.Add(this.m_track_origin_point_size);
			this.m_grp_camera.Controls.Add(this.m_lbl_origin_point_size);
			this.m_grp_camera.Controls.Add(this.m_chk_focus_point_visible);
			this.m_grp_camera.Controls.Add(this.m_track_focus_point_size);
			this.m_grp_camera.Controls.Add(this.m_lbl_focus_point_size);
			this.m_grp_camera.Location = new System.Drawing.Point(8, 82);
			this.m_grp_camera.Name = "m_grp_camera";
			this.m_grp_camera.Size = new System.Drawing.Size(222, 186);
			this.m_grp_camera.TabIndex = 0;
			this.m_grp_camera.TabStop = false;
			this.m_grp_camera.Text = "Camera";
			// 
			// m_tab_rendering
			// 
			this.m_tab_rendering.Controls.Add(this.m_propgrid_rendering);
			this.m_tab_rendering.Location = new System.Drawing.Point(4, 22);
			this.m_tab_rendering.Name = "m_tab_rendering";
			this.m_tab_rendering.Padding = new System.Windows.Forms.Padding(3);
			this.m_tab_rendering.Size = new System.Drawing.Size(433, 442);
			this.m_tab_rendering.TabIndex = 1;
			this.m_tab_rendering.Text = "Rendering";
			this.m_tab_rendering.UseVisualStyleBackColor = true;
			// 
			// m_propgrid_rendering
			// 
			this.m_propgrid_rendering.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_propgrid_rendering.HelpVisible = false;
			this.m_propgrid_rendering.Location = new System.Drawing.Point(3, 3);
			this.m_propgrid_rendering.Name = "m_propgrid_rendering";
			this.m_propgrid_rendering.PropertySort = System.Windows.Forms.PropertySort.NoSort;
			this.m_propgrid_rendering.Size = new System.Drawing.Size(427, 436);
			this.m_propgrid_rendering.TabIndex = 0;
			this.m_propgrid_rendering.ToolbarVisible = false;
			// 
			// m_lbl_focus_point_size
			// 
			this.m_lbl_focus_point_size.AutoSize = true;
			this.m_lbl_focus_point_size.Location = new System.Drawing.Point(6, 45);
			this.m_lbl_focus_point_size.Name = "m_lbl_focus_point_size";
			this.m_lbl_focus_point_size.Size = new System.Drawing.Size(89, 13);
			this.m_lbl_focus_point_size.TabIndex = 0;
			this.m_lbl_focus_point_size.Text = "Focus Point Size:";
			// 
			// m_track_focus_point_size
			// 
			this.m_track_focus_point_size.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_track_focus_point_size.AutoSize = false;
			this.m_track_focus_point_size.BackColor = System.Drawing.SystemColors.Window;
			this.m_track_focus_point_size.Location = new System.Drawing.Point(16, 63);
			this.m_track_focus_point_size.Maximum = 100;
			this.m_track_focus_point_size.Name = "m_track_focus_point_size";
			this.m_track_focus_point_size.Size = new System.Drawing.Size(191, 25);
			this.m_track_focus_point_size.TabIndex = 1;
			this.m_track_focus_point_size.TickStyle = System.Windows.Forms.TickStyle.None;
			this.m_track_focus_point_size.Value = 50;
			// 
			// m_chk_focus_point_visible
			// 
			this.m_chk_focus_point_visible.AutoSize = true;
			this.m_chk_focus_point_visible.Location = new System.Drawing.Point(9, 19);
			this.m_chk_focus_point_visible.Name = "m_chk_focus_point_visible";
			this.m_chk_focus_point_visible.Size = new System.Drawing.Size(115, 17);
			this.m_chk_focus_point_visible.TabIndex = 3;
			this.m_chk_focus_point_visible.Text = "Focus Point Visible";
			this.m_chk_focus_point_visible.UseVisualStyleBackColor = true;
			// 
			// m_chk_origin_point_visible
			// 
			this.m_chk_origin_point_visible.AutoSize = true;
			this.m_chk_origin_point_visible.Location = new System.Drawing.Point(9, 94);
			this.m_chk_origin_point_visible.Name = "m_chk_origin_point_visible";
			this.m_chk_origin_point_visible.Size = new System.Drawing.Size(113, 17);
			this.m_chk_origin_point_visible.TabIndex = 6;
			this.m_chk_origin_point_visible.Text = "Origin Point Visible";
			this.m_chk_origin_point_visible.UseVisualStyleBackColor = true;
			// 
			// m_track_origin_point_size
			// 
			this.m_track_origin_point_size.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_track_origin_point_size.AutoSize = false;
			this.m_track_origin_point_size.BackColor = System.Drawing.SystemColors.Window;
			this.m_track_origin_point_size.Location = new System.Drawing.Point(16, 138);
			this.m_track_origin_point_size.Maximum = 100;
			this.m_track_origin_point_size.Name = "m_track_origin_point_size";
			this.m_track_origin_point_size.Size = new System.Drawing.Size(191, 25);
			this.m_track_origin_point_size.TabIndex = 5;
			this.m_track_origin_point_size.TickStyle = System.Windows.Forms.TickStyle.None;
			this.m_track_origin_point_size.Value = 50;
			// 
			// m_lbl_origin_point_size
			// 
			this.m_lbl_origin_point_size.AutoSize = true;
			this.m_lbl_origin_point_size.Location = new System.Drawing.Point(6, 120);
			this.m_lbl_origin_point_size.Name = "m_lbl_origin_point_size";
			this.m_lbl_origin_point_size.Size = new System.Drawing.Size(87, 13);
			this.m_lbl_origin_point_size.TabIndex = 4;
			this.m_lbl_origin_point_size.Text = "Origin Point Size:";
			// 
			// SettingsUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(441, 468);
			this.Controls.Add(this.m_tab);
			this.Name = "SettingsUI";
			this.Text = "Options";
			this.m_tab.ResumeLayout(false);
			this.m_tab_general.ResumeLayout(false);
			this.m_grp_error_log.ResumeLayout(false);
			this.m_grp_error_log.PerformLayout();
			this.m_grp_camera.ResumeLayout(false);
			this.m_grp_camera.PerformLayout();
			this.m_tab_rendering.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.m_track_focus_point_size)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.m_track_origin_point_size)).EndInit();
			this.ResumeLayout(false);

		}
		#endregion
	}
}
