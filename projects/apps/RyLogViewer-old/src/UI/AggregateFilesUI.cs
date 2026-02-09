using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Rylogic.Container;
using Rylogic.Gui.WinForms;
using Util = Rylogic.Utility.Util;

namespace RyLogViewer
{
	public sealed class AggregateFilesUI :Form
	{
		#region UI Elements
		private DataGridView m_grid;
		private Label m_lbl_instructions;
		private Button m_btn_ok;
		private Button m_btn_add_files;
		private Button m_btn_cancel;
		private ToolTip m_tt;
		#endregion

		public AggregateFilesUI(Main main)
		{
			InitializeComponent();
			AllowDrop = true;
			Main = main;
			FileInfos = new BindingSource<FileInfo> { DataSource = new BindingListEx<FileInfo>() };
			DD = new DragDrop();

			SetupUI();
		}
		protected override void Dispose(bool disposing)
		{
			DD = null;
			FileInfos = null;
			Main = null;
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}
		protected override void OnShown(EventArgs e)
		{
			base.OnShown(e);
			Main.UseLicensedFeature(FeatureName.AggregateFiles, new AggregateFileLimiter(Main, this));
		}
		protected override void OnClosed(EventArgs e)
		{
			base.OnClosed(e);
			Main.UseLicensedFeature(FeatureName.AggregateFiles, new AggregateFileLimiter(Main, null));
		}

		/// <summary>The main app</summary>
		private Main Main
		{
			get;
			set
			{
				if (field == value) return;
				field = value;
			}
		}

		/// <summary>The selected filepaths</summary>
		public IEnumerable<string> Filepaths
		{
			get { return FileInfos.Select(x => x.FullName); }
		}

		/// <summary>The filepaths that make up the aggregate file</summary>
		private BindingSource<FileInfo> FileInfos
		{
			get;
			set
			{
				if (field == value) return;
				if (field != null)
				{
					field.ListChanging -= HandleFileInfosListChanging;
				}
				field = value;
				if (field != null)
				{
					field.ListChanging += HandleFileInfosListChanging;
				}
			}
		}
		private void HandleFileInfosListChanging(object sender, ListChgEventArgs<FileInfo> e)
		{
			m_btn_ok.Enabled = FileInfos.Count != 0;
		}

		/// <summary>Drag drop handler</summary>
		private DragDrop DD
		{
			get { return m_dd; }
			set
			{
				if (m_dd == value) return;
				if (m_dd != null)
				{
					m_dd.DoDrop -= DataGridView_.DragDrop_DoDropMoveRow;
					m_dd.DoDrop -= DropFiles;
					m_dd.Detach(m_grid);
					m_dd.Detach(this);
				}
				m_dd = value;
				if (m_dd != null)
				{
					m_dd.Attach(this);
					m_dd.Attach(m_grid);
					m_dd.DoDrop += DropFiles;
					m_dd.DoDrop += DataGridView_.DragDrop_DoDropMoveRow;
				}
			}
		}
		private DragDrop m_dd;

		/// <summary>Set up UI elements</summary>
		private void SetupUI()
		{
			// Files grid
			m_grid.AllowDrop = true;
			m_grid.AutoGenerateColumns = false;
			m_grid.Columns.Add(new DataGridViewTextBoxColumn{HeaderText = "File Path", DataPropertyName = nameof(FileInfo.FullName)});
			m_grid.DataSource = FileInfos;
			m_grid.MouseDown += DataGridView_.DragDrop_DragRow;

			// Add files
			m_btn_add_files.ToolTip(m_tt, "Browse for files to add");
			m_btn_add_files.Click += (s,a) =>
			{
				var dlg = new OpenFileDialog{Filter = Constants.LogFileFilter, Multiselect = true};
				if (dlg.ShowDialog(this) != DialogResult.OK) return;
				FileInfos.AddRange(dlg.FileNames.Select(x => new FileInfo(x)));
			};

			// Disable OK till there are files
			m_btn_ok.Enabled = false;
		}

		/// <summary>Drop file paths into the grid</summary>
		private bool DropFiles(object sender, DragEventArgs args, DragDrop.EDrop mode)
		{
			args.Effect = DragDropEffects.None;
			if (!args.Data.GetDataPresent(DataFormats.FileDrop))
				return false;

			args.Effect = DragDropEffects.Copy;
			if (mode != Rylogic.Gui.WinForms.DragDrop.EDrop.Drop)
				return true;

			var filepaths = (string[])args.Data.GetData(DataFormats.FileDrop);
			FileInfos.AddRange(filepaths.Select(x => new FileInfo(x)));
			return true;
		}

		#region Windows Form Designer generated code
		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AggregateFilesUI));
			this.m_grid = new RyLogViewer.DataGridView();
			this.m_btn_ok = new System.Windows.Forms.Button();
			this.m_btn_cancel = new System.Windows.Forms.Button();
			this.m_lbl_instructions = new System.Windows.Forms.Label();
			this.m_btn_add_files = new System.Windows.Forms.Button();
			this.m_tt = new System.Windows.Forms.ToolTip(this.components);
			((System.ComponentModel.ISupportInitialize)(this.m_grid)).BeginInit();
			this.SuspendLayout();
			// 
			// m_grid
			// 
			this.m_grid.AllowUserToAddRows = false;
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
			this.m_grid.Size = new System.Drawing.Size(350, 245);
			this.m_grid.TabIndex = 0;
			// 
			// m_btn_ok
			// 
			this.m_btn_ok.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_ok.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btn_ok.Location = new System.Drawing.Point(196, 289);
			this.m_btn_ok.Name = "m_btn_ok";
			this.m_btn_ok.Size = new System.Drawing.Size(75, 23);
			this.m_btn_ok.TabIndex = 2;
			this.m_btn_ok.Text = "OK";
			this.m_btn_ok.UseVisualStyleBackColor = true;
			// 
			// m_btn_cancel
			// 
			this.m_btn_cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.m_btn_cancel.Location = new System.Drawing.Point(277, 289);
			this.m_btn_cancel.Name = "m_btn_cancel";
			this.m_btn_cancel.Size = new System.Drawing.Size(75, 23);
			this.m_btn_cancel.TabIndex = 3;
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
			this.m_btn_add_files.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.m_btn_add_files.Location = new System.Drawing.Point(7, 289);
			this.m_btn_add_files.Name = "m_btn_add_files";
			this.m_btn_add_files.Size = new System.Drawing.Size(93, 23);
			this.m_btn_add_files.TabIndex = 1;
			this.m_btn_add_files.Text = "Add Files";
			this.m_btn_add_files.UseVisualStyleBackColor = true;
			// 
			// AggregateFilesUI
			// 
			this.AcceptButton = this.m_btn_ok;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.m_btn_cancel;
			this.ClientSize = new System.Drawing.Size(364, 323);
			this.Controls.Add(this.m_btn_add_files);
			this.Controls.Add(this.m_lbl_instructions);
			this.Controls.Add(this.m_btn_cancel);
			this.Controls.Add(this.m_btn_ok);
			this.Controls.Add(this.m_grid);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MinimumSize = new System.Drawing.Size(329, 138);
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
