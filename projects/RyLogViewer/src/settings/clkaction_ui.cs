using System;
using System.Collections;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using RyLogViewer.Properties;
using Rylogic.Extn;

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
		private Label m_lbl_preview;
		private TextBox m_text_preview;
		private Button m_btn_browse;

		/// <summary>The action configured by this UI</summary>
		public ClkAction Action { get; private set; }

		public ClkActionUI(ClkAction action)
		{
			InitializeComponent();
			m_tt = new ToolTip();
			Action = new ClkAction(action);
			FocusedEditbox = m_edit_exec;
			string tt;

			// Executable
			tt = "The program or batch file to execute";
			m_lbl_exec.ToolTip(m_tt, tt);
			m_edit_exec.ToolTip(m_tt, tt);
			m_edit_exec.Text = Action.Executable;
			m_edit_exec.GotFocus += (s,a) =>
				{
					FocusedEditbox = m_edit_exec;
				};
			m_edit_exec.TextChanged += (s,a)=>
				{
					Action.Executable = m_edit_exec.Text;
					UpdateUI();
				};

			// Arguments
			tt = "The arguments to pass to the executable.\r\n" +
			     "For sub string or wildcard patterns use {0} to represent the matched text.\r\n" +
			     "For regular expressions use {0},{1},{2},... to represent capture groups";
			m_lbl_args.ToolTip(m_tt, tt);
			m_edit_args.ToolTip(m_tt, tt);
			m_edit_args.Text = Action.Arguments;
			m_edit_args.GotFocus += (s,a) =>
				{
					FocusedEditbox = m_edit_args;
				};
			m_edit_args.TextChanged += (s,a)=>
				{
					Action.Arguments = m_edit_args.Text;
					UpdateUI();
				};

			// Start in directory
			tt = "The working directory to start the executable in";
			m_lbl_startin.ToolTip(m_tt, tt);
			m_edit_startin.ToolTip(m_tt,tt);
			m_edit_startin.Text = Action.WorkingDirectory;
			m_edit_startin.GotFocus += (s,a) =>
				{
					FocusedEditbox = m_edit_startin;
				};
			m_edit_startin.TextChanged += (s,a)=>
				{
					Action.WorkingDirectory = m_edit_startin.Text;
					UpdateUI();
				};

			// Browse button
			m_btn_browse.ToolTip(m_tt, "Browse for an executable to run");
			m_btn_browse.Click += (s,a)=>
				{
					var dg = new OpenFileDialog{Title = "Select an Executable", CheckPathExists = true, Filter = Constants.ExecutablesFilter};
					if (dg.ShowDialog(this) != DialogResult.OK) return;
					m_edit_exec.Text = dg.FileName;
					UpdateUI();
				};

			// Capture groups button
			m_btn_capture_groups.ToolTip(m_tt, "Displays a menu of the pattern match tags and special tags");
			m_btn_capture_groups.Click += (s,a) =>
				{
					ShowCaptureGroupsMenu();
					UpdateUI();
				};

			Disposed += (s,a) =>
				{
					m_tt.Dispose();
				};

			UpdateUI();
		}

		/// <summary>Returns the edit box that currently has focus (or null)</summary>
		private TextBox FocusedEditbox { get; set; }

		/// <summary>Display the capture groups menu</summary>
		private void ShowCaptureGroupsMenu()
		{
			var edit = FocusedEditbox;
			if (edit == null)
				return;

			// Pop up a menu with the special tags
			var menu = new ContextMenuStrip();
			foreach (var x in Action.CaptureGroupNames)
			{
				var group_name = x;
				menu.Items.Add($"Capture Group: {group_name}", null, (ss,aa) => edit.SelectedText = "{"+group_name+"}");
			}
			if (menu.Items.Count != 0) menu.Items.Add(new ToolStripSeparator());
			menu.Items.Add("Current file full path" , null, (ss,aa) => edit.SelectedText = "{" + SpecialTags.FilePath  + "}");
			menu.Items.Add("Current file name"      , null, (ss,aa) => edit.SelectedText = "{" + SpecialTags.FileName  + "}");
			menu.Items.Add("Current file directory" , null, (ss,aa) => edit.SelectedText = "{" + SpecialTags.FileDir   + "}");
			menu.Items.Add("Current file title"     , null, (ss,aa) => edit.SelectedText = "{" + SpecialTags.FileTitle + "}");
			menu.Items.Add("Current file extension" , null, (ss,aa) => edit.SelectedText = "{" + SpecialTags.FileExtn  + "}");
			menu.Items.Add("Current file drive"     , null, (ss,aa) => edit.SelectedText = "{" + SpecialTags.FileRoot  + "}");
			menu.Items.Add(new ToolStripSeparator());
			menu.Items.Add("Environment Variable...", null, (ss,aa) => ShowEnvironmentVariables(this, (k,v) => edit.SelectedText = "{"+k+"}"));
			menu.Show(m_btn_capture_groups, m_btn_capture_groups.Width, 0);
		}

		/// <summary>Display a dialog containing the environment variables</summary>
		private void ShowEnvironmentVariables(IWin32Window owner, Action<string,string> on_selected)
		{
			var form = new Form
				{
					FormBorderStyle = FormBorderStyle.SizableToolWindow,
					StartPosition = FormStartPosition.CenterParent,
					Size = new Size(620,300)
				};

			var list = new ListView
				{
					View          = View.Details,
					Dock          = DockStyle.Fill,
					FullRowSelect = true,
					Scrollable    = true,
					MultiSelect   = false,
				};

			list.Columns.Add("Key", -2, HorizontalAlignment.Left);
			list.Columns.Add("Value", -2, HorizontalAlignment.Left);
			foreach (var env_var in Environment.GetEnvironmentVariables().Cast<DictionaryEntry>())
			{
				var key = env_var.Key.ToString();
				var val = env_var.Value.ToString();
				list.Items.Add(new ListViewItem(new []{key, val}));
			}

			Action accept = () =>
				{
					var selected = list.SelectedItems;
					if (selected.Count == 0) return;
					var items = selected[0].SubItems;
					on_selected(items[0].Text, items[1].Text);
					form.Close();
				};

			list.MouseDoubleClick += (s,a) =>
				{
					accept();
				};
			list.KeyDown += (s,a) =>
				{
					if (a.KeyCode == Keys.Enter)
						accept();
				};

			form.Controls.Add(list);
			form.ShowDialog(owner);
		}

		/// <summary>Update the UI</summary>
		private void UpdateUI(object sender = null, EventArgs args = null)
		{
			m_text_preview.Text = Action.CommandLine(string.Empty, @"x:\directory\filename.extn");
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
			this.m_lbl_preview = new System.Windows.Forms.Label();
			this.m_text_preview = new System.Windows.Forms.TextBox();
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
			this.m_edit_exec.Size = new System.Drawing.Size(315, 20);
			this.m_edit_exec.TabIndex = 0;
			// 
			// m_edit_args
			// 
			this.m_edit_args.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_edit_args.Location = new System.Drawing.Point(71, 33);
			this.m_edit_args.Name = "m_edit_args";
			this.m_edit_args.Size = new System.Drawing.Size(315, 20);
			this.m_edit_args.TabIndex = 2;
			// 
			// m_edit_startin
			// 
			this.m_edit_startin.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_edit_startin.Location = new System.Drawing.Point(71, 58);
			this.m_edit_startin.Name = "m_edit_startin";
			this.m_edit_startin.Size = new System.Drawing.Size(315, 20);
			this.m_edit_startin.TabIndex = 4;
			// 
			// m_btn_ok
			// 
			this.m_btn_ok.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_ok.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btn_ok.Location = new System.Drawing.Point(269, 124);
			this.m_btn_ok.Name = "m_btn_ok";
			this.m_btn_ok.Size = new System.Drawing.Size(75, 23);
			this.m_btn_ok.TabIndex = 5;
			this.m_btn_ok.Text = "OK";
			this.m_btn_ok.UseVisualStyleBackColor = true;
			// 
			// m_btn_cancel
			// 
			this.m_btn_cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.m_btn_cancel.Location = new System.Drawing.Point(350, 124);
			this.m_btn_cancel.Name = "m_btn_cancel";
			this.m_btn_cancel.Size = new System.Drawing.Size(75, 23);
			this.m_btn_cancel.TabIndex = 6;
			this.m_btn_cancel.Text = "Cancel";
			this.m_btn_cancel.UseVisualStyleBackColor = true;
			// 
			// m_btn_browse
			// 
			this.m_btn_browse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_browse.Location = new System.Drawing.Point(392, 6);
			this.m_btn_browse.Name = "m_btn_browse";
			this.m_btn_browse.Size = new System.Drawing.Size(33, 23);
			this.m_btn_browse.TabIndex = 1;
			this.m_btn_browse.Text = "...";
			this.m_btn_browse.UseVisualStyleBackColor = true;
			// 
			// m_btn_capture_groups
			// 
			this.m_btn_capture_groups.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_capture_groups.Location = new System.Drawing.Point(392, 30);
			this.m_btn_capture_groups.Name = "m_btn_capture_groups";
			this.m_btn_capture_groups.Size = new System.Drawing.Size(33, 24);
			this.m_btn_capture_groups.TabIndex = 3;
			this.m_btn_capture_groups.Text = ">";
			this.m_btn_capture_groups.UseVisualStyleBackColor = true;
			// 
			// m_lbl_preview
			// 
			this.m_lbl_preview.AutoSize = true;
			this.m_lbl_preview.Location = new System.Drawing.Point(16, 86);
			this.m_lbl_preview.Name = "m_lbl_preview";
			this.m_lbl_preview.Size = new System.Drawing.Size(48, 13);
			this.m_lbl_preview.TabIndex = 7;
			this.m_lbl_preview.Text = "Preview:";
			this.m_lbl_preview.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// m_text_preview
			// 
			this.m_text_preview.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_text_preview.Location = new System.Drawing.Point(71, 83);
			this.m_text_preview.Multiline = true;
			this.m_text_preview.Name = "m_text_preview";
			this.m_text_preview.ReadOnly = true;
			this.m_text_preview.Size = new System.Drawing.Size(315, 35);
			this.m_text_preview.TabIndex = 8;
			this.m_text_preview.TabStop = false;
			// 
			// ClkActionUI
			// 
			this.AcceptButton = this.m_btn_ok;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.m_btn_cancel;
			this.ClientSize = new System.Drawing.Size(437, 151);
			this.Controls.Add(this.m_text_preview);
			this.Controls.Add(this.m_lbl_preview);
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
			this.MaximumSize = new System.Drawing.Size(3000, 190);
			this.MinimumSize = new System.Drawing.Size(200, 190);
			this.Name = "ClkActionUI";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Configure Click Action";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion
	}
}
