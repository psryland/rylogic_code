using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using pr.gui;
using pr.util;

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

			// Rendering property grid
			m_propgrid_rendering.SelectedObject = Settings.Scene;
		}

		#region Windows Form Designer generated code
		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			this.m_tab = new System.Windows.Forms.TabControl();
			this.m_tab_general = new System.Windows.Forms.TabPage();
			this.m_grp_error_log = new System.Windows.Forms.GroupBox();
			this.m_chk_show_log_on_new_messages = new System.Windows.Forms.CheckBox();
			this.m_grp_camera = new System.Windows.Forms.GroupBox();
			this.m_tab_rendering = new System.Windows.Forms.TabPage();
			this.m_propgrid_rendering = new System.Windows.Forms.PropertyGrid();
			this.m_chk_clear_log_on_reload = new System.Windows.Forms.CheckBox();
			this.m_tab.SuspendLayout();
			this.m_tab_general.SuspendLayout();
			this.m_grp_error_log.SuspendLayout();
			this.m_tab_rendering.SuspendLayout();
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
			this.m_grp_camera.Location = new System.Drawing.Point(8, 82);
			this.m_grp_camera.Name = "m_grp_camera";
			this.m_grp_camera.Size = new System.Drawing.Size(222, 110);
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
			this.m_tab_rendering.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion
	}
}
