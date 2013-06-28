using System.IO;
using System.Windows.Forms;
using RyLogViewer.Properties;
using pr.extn;
using pr.util;

namespace RyLogViewer
{
	public class ClkActionUI :Form
	{
		private readonly ToolTip m_tt;
		private Label m_lbl_exec;
		private Label m_lbl_args;
		private Label m_lbl_startin;
		private TextBox m_edit_exec;
		private TextBox m_edit_args;
		private TextBox m_edit_startin;
		private Button m_btn_ok;
		private Button m_btn_cancel;
		private Button m_btn_capture_groups;
		private Button m_btn_browse;
		
		/// <summary>The action configured by this UI</summary>
		public ClkAction Action { get; private set; }
		
		public ClkActionUI(ClkAction action)
		{
			InitializeComponent();
			m_tt = new ToolTip();
			Action = new ClkAction(action);
			string tt;
			
			// Executable
			tt = "The program or batch file to execute";
			m_lbl_exec.ToolTip(m_tt, tt);
			m_edit_exec.ToolTip(m_tt, tt);
			m_edit_exec.Text = Action.Executable;
			m_edit_exec.TextChanged += (s,a)=>
				{
					Action.Executable = m_edit_exec.Text;
				};
			
			// Arguments
			tt = "The arguments to pass to the executable.\r\n" +
			     "For sub string or wildcard patterns use {1} to represent the matched text.\r\n" +
			     "For regular expressions use {1},{2},{3},... to represent capture groups";
			m_lbl_args.ToolTip(m_tt, tt);
			m_edit_args.ToolTip(m_tt, tt);
			m_edit_args.Text = Action.Arguments;
			m_edit_args.TextChanged += (s,a)=>
				{
					Action.Arguments = m_edit_args.Text;
				};
			
			// Start in directory
			tt = "The working directory to start the executable in";
			m_lbl_startin.ToolTip(m_tt, tt);
			m_edit_startin.ToolTip(m_tt,tt);
			m_edit_startin.Text = Action.WorkingDirectory;
			m_edit_startin.TextChanged += (s,a)=>
				{
					Action.WorkingDirectory = m_edit_startin.Text;
				};
			
			// Browse button
			m_btn_browse.ToolTip(m_tt, "Browse for an executable to run");
			m_btn_browse.Click += (s,a)=>
				{
					var dg = new OpenFileDialog{Title = "Select an Executable", CheckPathExists = true, Filter = Resources.ExecutablesFilter};
					if (dg.ShowDialog(this) != DialogResult.OK) return;
					m_edit_exec.Text = dg.FileName;
				};

			// Capture groups button
			m_btn_capture_groups.ToolTip(m_tt, "Displays a menu of the pattern match tags and special tags");
			m_btn_capture_groups.Click += (s,a) =>
				{
					// Pop up a menu with the special tags
					var menu = new ContextMenuStrip();
					foreach (var x in Action.CaptureGroupNames)
					{
						var group_name = x;
						menu.Items.Add("Capture Group: {0}".Fmt(group_name), null, (ss,aa) => m_edit_args.SelectedText = "{"+group_name+"}");
					}
					if (menu.Items.Count != 0)
					{
						menu.Items.Add(new ToolStripSeparator());
					}
					menu.Items.Add("Current file name"     , null, (ss,aa) => m_edit_args.SelectedText = SpecialTags.FileName);
					menu.Items.Add("Current file directory", null, (ss,aa) => m_edit_args.SelectedText = SpecialTags.FileDir);
					menu.Show(m_btn_capture_groups, m_btn_capture_groups.Width, 0);
				};
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ClkActionUI));
			this.m_lbl_exec = new System.Windows.Forms.Label();
			this.m_lbl_args = new System.Windows.Forms.Label();
			this.m_lbl_startin = new System.Windows.Forms.Label();
			this.m_edit_exec = new System.Windows.Forms.TextBox();
			this.m_edit_args = new System.Windows.Forms.TextBox();
			this.m_edit_startin = new System.Windows.Forms.TextBox();
			this.m_btn_ok = new System.Windows.Forms.Button();
			this.m_btn_cancel = new System.Windows.Forms.Button();
			this.m_btn_browse = new System.Windows.Forms.Button();
			this.m_btn_capture_groups = new System.Windows.Forms.Button();
			this.SuspendLayout();
			// 
			// m_lbl_exec
			// 
			this.m_lbl_exec.AutoSize = true;
			this.m_lbl_exec.Location = new System.Drawing.Point(16, 11);
			this.m_lbl_exec.Name = "m_lbl_exec";
			this.m_lbl_exec.Size = new System.Drawing.Size(49, 13);
			this.m_lbl_exec.TabIndex = 0;
			this.m_lbl_exec.Text = "Execute:";
			this.m_lbl_exec.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// m_lbl_args
			// 
			this.m_lbl_args.AutoSize = true;
			this.m_lbl_args.Location = new System.Drawing.Point(5, 35);
			this.m_lbl_args.Name = "m_lbl_args";
			this.m_lbl_args.Size = new System.Drawing.Size(60, 13);
			this.m_lbl_args.TabIndex = 1;
			this.m_lbl_args.Text = "Arguments:";
			this.m_lbl_args.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// m_lbl_startin
			// 
			this.m_lbl_startin.AutoSize = true;
			this.m_lbl_startin.Location = new System.Drawing.Point(22, 61);
			this.m_lbl_startin.Name = "m_lbl_startin";
			this.m_lbl_startin.Size = new System.Drawing.Size(43, 13);
			this.m_lbl_startin.TabIndex = 2;
			this.m_lbl_startin.Text = "Start in:";
			this.m_lbl_startin.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// m_edit_exec
			// 
			this.m_edit_exec.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_edit_exec.Location = new System.Drawing.Point(71, 8);
			this.m_edit_exec.Name = "m_edit_exec";
			this.m_edit_exec.Size = new System.Drawing.Size(185, 20);
			this.m_edit_exec.TabIndex = 0;
			// 
			// m_edit_args
			// 
			this.m_edit_args.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_edit_args.Location = new System.Drawing.Point(71, 33);
			this.m_edit_args.Name = "m_edit_args";
			this.m_edit_args.Size = new System.Drawing.Size(185, 20);
			this.m_edit_args.TabIndex = 1;
			// 
			// m_edit_startin
			// 
			this.m_edit_startin.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_edit_startin.Location = new System.Drawing.Point(71, 58);
			this.m_edit_startin.Name = "m_edit_startin";
			this.m_edit_startin.Size = new System.Drawing.Size(185, 20);
			this.m_edit_startin.TabIndex = 2;
			// 
			// m_btn_ok
			// 
			this.m_btn_ok.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_ok.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btn_ok.Location = new System.Drawing.Point(139, 84);
			this.m_btn_ok.Name = "m_btn_ok";
			this.m_btn_ok.Size = new System.Drawing.Size(75, 23);
			this.m_btn_ok.TabIndex = 4;
			this.m_btn_ok.Text = "OK";
			this.m_btn_ok.UseVisualStyleBackColor = true;
			// 
			// m_btn_cancel
			// 
			this.m_btn_cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.m_btn_cancel.Location = new System.Drawing.Point(220, 84);
			this.m_btn_cancel.Name = "m_btn_cancel";
			this.m_btn_cancel.Size = new System.Drawing.Size(75, 23);
			this.m_btn_cancel.TabIndex = 3;
			this.m_btn_cancel.Text = "Cancel";
			this.m_btn_cancel.UseVisualStyleBackColor = true;
			// 
			// m_btn_browse
			// 
			this.m_btn_browse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_browse.Location = new System.Drawing.Point(262, 6);
			this.m_btn_browse.Name = "m_btn_browse";
			this.m_btn_browse.Size = new System.Drawing.Size(33, 23);
			this.m_btn_browse.TabIndex = 5;
			this.m_btn_browse.Text = "...";
			this.m_btn_browse.UseVisualStyleBackColor = true;
			// 
			// m_btn_capture_groups
			// 
			this.m_btn_capture_groups.Location = new System.Drawing.Point(262, 30);
			this.m_btn_capture_groups.Name = "m_btn_capture_groups";
			this.m_btn_capture_groups.Size = new System.Drawing.Size(33, 24);
			this.m_btn_capture_groups.TabIndex = 6;
			this.m_btn_capture_groups.Text = ">";
			this.m_btn_capture_groups.UseVisualStyleBackColor = true;
			// 
			// ClkActionUI
			// 
			this.AcceptButton = this.m_btn_ok;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.m_btn_cancel;
			this.ClientSize = new System.Drawing.Size(307, 116);
			this.Controls.Add(this.m_btn_capture_groups);
			this.Controls.Add(this.m_btn_browse);
			this.Controls.Add(this.m_btn_cancel);
			this.Controls.Add(this.m_btn_ok);
			this.Controls.Add(this.m_edit_startin);
			this.Controls.Add(this.m_edit_args);
			this.Controls.Add(this.m_edit_exec);
			this.Controls.Add(this.m_lbl_startin);
			this.Controls.Add(this.m_lbl_args);
			this.Controls.Add(this.m_lbl_exec);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MaximumSize = new System.Drawing.Size(3000, 150);
			this.MinimumSize = new System.Drawing.Size(200, 150);
			this.Name = "ClkActionUI";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Configure Click Action";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion
	}
}
