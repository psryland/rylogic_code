using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using RyLogViewer.Properties;
using pr.extn;
using pr.util;

namespace RyLogViewer
{
	public sealed class AggregateFilesUI :Form
	{
		private readonly BindingList<FileInfo> m_filepaths; 
		private readonly BindingSource m_bs_filepaths;
		private readonly DragDrop m_dragdrop;
		private DataGridView m_grid;
		private Label m_lbl_instructions;
		private Button m_btn_ok;
		private Button m_btn_add_files;
		private Button m_btn_cancel;
		private readonly ToolTip m_tt;

		/// <summary>The selected filepaths</summary>
		public IEnumerable<string> Filepaths { get { return m_filepaths.Select(x => x.FullName); } }

		public AggregateFilesUI()
		{
			InitializeComponent();
			m_filepaths = new BindingList<FileInfo>();
			m_bs_filepaths = new BindingSource{DataSource = m_filepaths};
			m_tt = new ToolTip();

			m_dragdrop = new DragDrop();
			m_dragdrop.DoDrop += DropFiles;
			m_dragdrop.DoDrop += DataGridView_Extensions.DragDrop_DoDropMoveRow;

			// Allow file drop on the form
			AllowDrop = true;
			m_dragdrop.Attach(this);

			m_grid.AllowDrop = true;
			m_grid.AutoGenerateColumns = false;
			m_grid.Columns.Add(new DataGridViewTextBoxColumn{HeaderText = "File Path", DataPropertyName = Reflect<FileInfo>.MemberName(x => x.FullName)});
			m_grid.DataSource = m_bs_filepaths;
			m_grid.MouseDown += DataGridView_Extensions.DragDrop_DragRow;
			m_dragdrop.Attach(m_grid);

			m_btn_add_files.ToolTip(m_tt, "Browse for files to add");
			m_btn_add_files.Click += (s,a) =>
				{
					var dlg = new OpenFileDialog{Filter = Resources.LogFileFilter, Multiselect = true};
					if (dlg.ShowDialog(this) != DialogResult.OK) return;
					m_filepaths.AddRange(dlg.FileNames.Select(x => new FileInfo(x)));
				};

			// Enabled the ok button when there is one or more files
			m_btn_ok.Enabled = false;
			m_bs_filepaths.ListChanged += (s,a) =>
				{
					m_btn_ok.Enabled = m_bs_filepaths.Count != 0;
				};
		}

		/// <summary>Drop file paths into the grid</summary>
		private bool DropFiles(object sender, DragEventArgs args, DragDrop.EDrop mode)
		{
			args.Effect = DragDropEffects.None;
			if (!args.Data.GetDataPresent(DataFormats.FileDrop))
				return false;

			args.Effect = DragDropEffects.Copy;
			if (mode != pr.util.DragDrop.EDrop.Drop)
				return true;

			var filepaths = (string[])args.Data.GetData(DataFormats.FileDrop);
			m_filepaths.AddRange(filepaths.Select(x => new FileInfo(x)));
			return true;
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AggregateFilesUI));
			this.m_grid = new System.Windows.Forms.DataGridView();
			this.m_btn_ok = new System.Windows.Forms.Button();
			this.m_btn_cancel = new System.Windows.Forms.Button();
			this.m_lbl_instructions = new System.Windows.Forms.Label();
			this.m_btn_add_files = new System.Windows.Forms.Button();
			((System.ComponentModel.ISupportInitialize)(this.m_grid)).BeginInit();
			this.SuspendLayout();
			// 
			// m_grid
			// 
			this.m_grid.AllowUserToResizeRows = false;
			this.m_grid.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_grid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
			this.m_grid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.m_grid.Location = new System.Drawing.Point(7, 38);
			this.m_grid.Name = "m_grid";
			this.m_grid.RowHeadersWidth = 28;
			this.m_grid.RowHeadersWidthSizeMode = System.Windows.Forms.DataGridViewRowHeadersWidthSizeMode.DisableResizing;
			this.m_grid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.m_grid.Size = new System.Drawing.Size(423, 328);
			this.m_grid.TabIndex = 0;
			// 
			// m_btn_ok
			// 
			this.m_btn_ok.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_ok.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btn_ok.Location = new System.Drawing.Point(269, 372);
			this.m_btn_ok.Name = "m_btn_ok";
			this.m_btn_ok.Size = new System.Drawing.Size(75, 23);
			this.m_btn_ok.TabIndex = 1;
			this.m_btn_ok.Text = "OK";
			this.m_btn_ok.UseVisualStyleBackColor = true;
			// 
			// m_btn_cancel
			// 
			this.m_btn_cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.m_btn_cancel.Location = new System.Drawing.Point(350, 372);
			this.m_btn_cancel.Name = "m_btn_cancel";
			this.m_btn_cancel.Size = new System.Drawing.Size(75, 23);
			this.m_btn_cancel.TabIndex = 2;
			this.m_btn_cancel.Text = "Cancel";
			this.m_btn_cancel.UseVisualStyleBackColor = true;
			// 
			// m_lbl_instructions
			// 
			this.m_lbl_instructions.AutoSize = true;
			this.m_lbl_instructions.Location = new System.Drawing.Point(4, 9);
			this.m_lbl_instructions.Name = "m_lbl_instructions";
			this.m_lbl_instructions.Size = new System.Drawing.Size(306, 26);
			this.m_lbl_instructions.TabIndex = 3;
			this.m_lbl_instructions.Text = "Add files by dragging and dropping, or via the \'Add Files\' button.\r\nDrag files wi" +
    "thin the table below to change the order.";
			// 
			// m_btn_add_files
			// 
			this.m_btn_add_files.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_add_files.Location = new System.Drawing.Point(7, 372);
			this.m_btn_add_files.Name = "m_btn_add_files";
			this.m_btn_add_files.Size = new System.Drawing.Size(93, 23);
			this.m_btn_add_files.TabIndex = 4;
			this.m_btn_add_files.Text = "Add Files";
			this.m_btn_add_files.UseVisualStyleBackColor = true;
			// 
			// AggregateFilesUI
			// 
			this.AcceptButton = this.m_btn_ok;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.m_btn_cancel;
			this.ClientSize = new System.Drawing.Size(437, 406);
			this.Controls.Add(this.m_btn_add_files);
			this.Controls.Add(this.m_lbl_instructions);
			this.Controls.Add(this.m_btn_cancel);
			this.Controls.Add(this.m_btn_ok);
			this.Controls.Add(this.m_grid);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "AggregateFilesUI";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Aggregate Log Files";
			((System.ComponentModel.ISupportInitialize)(this.m_grid)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion
	}
}
