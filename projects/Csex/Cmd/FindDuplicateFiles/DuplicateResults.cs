using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using pr.container;
using pr.gui;
using pr.util;

namespace Csex
{
	public class DuplicateResults :UserControl
	{
		private readonly Model m_model;
		private SplitContainer m_split;
		private DataGridView m_grid_details;
		private DataGridView m_grid_dups;
		private readonly BindingSource<Model.FileInfo> m_dups;
		private readonly BindingSource<Model.FileInfo> m_copies;
		public DuplicateResults(Model model)
		{
			InitializeComponent();
			m_model = model;

			m_dups = new BindingSource<Model.FileInfo>{DataSource = m_model.Duplicates};
			m_copies = new BindingSource<Model.FileInfo>();

			m_grid_dups.AutoGenerateColumns = false;
			m_grid_dups.Columns.Add(new DataGridViewTextBoxColumn{HeaderText="Duplicates", FillWeight=1f, DataPropertyName=nameof(Model.FileInfo.FileName)});
			m_grid_dups.Columns.Add(new DataGridViewTextBoxColumn{HeaderText="Copies"    , FillWeight=1f, DataPropertyName=nameof(Model.FileInfo.CopyCount)});
			m_grid_dups.Columns.Add(new DataGridViewTextBoxColumn{HeaderText="Key"       , FillWeight=1f, DataPropertyName=nameof(Model.FileInfo.Key)});
			m_grid_dups.DataSource = m_dups;

			m_grid_details.AutoGenerateColumns = false;
			m_grid_details.Columns.Add(new DataGridViewTextBoxColumn{HeaderText="Location", FillWeight=4f, DataPropertyName=nameof(Model.FileInfo.FullPath)});
			m_grid_details.Columns.Add(new DataGridViewTextBoxColumn{HeaderText="File Size" , FillWeight=1f, DataPropertyName=nameof(Model.FileInfo.FileSize)});
			m_grid_details.DataSource = m_copies;

			m_dups.PositionChanged += (s,a) =>
				{
					if (m_dups.Current != null)
						m_copies.DataSource = m_dups.Current.Duplicates;
					else
						m_copies.DataSource = null;
				};
		}
		protected override void Dispose(bool disposing)
		{
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}

		#region Component Designer generated code

		/// <summary>Required designer variable.</summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.m_split = new System.Windows.Forms.SplitContainer();
			this.m_grid_details = new System.Windows.Forms.DataGridView();
			this.m_grid_dups = new System.Windows.Forms.DataGridView();
			((System.ComponentModel.ISupportInitialize)(this.m_split)).BeginInit();
			this.m_split.Panel1.SuspendLayout();
			this.m_split.Panel2.SuspendLayout();
			this.m_split.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.m_grid_details)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.m_grid_dups)).BeginInit();
			this.SuspendLayout();
			// 
			// m_split
			// 
			this.m_split.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_split.Location = new System.Drawing.Point(0, 0);
			this.m_split.Margin = new System.Windows.Forms.Padding(0);
			this.m_split.Name = "m_split";
			this.m_split.Orientation = System.Windows.Forms.Orientation.Horizontal;
			// 
			// m_split.Panel1
			// 
			this.m_split.Panel1.Controls.Add(this.m_grid_dups);
			// 
			// m_split.Panel2
			// 
			this.m_split.Panel2.Controls.Add(this.m_grid_details);
			this.m_split.Size = new System.Drawing.Size(677, 627);
			this.m_split.SplitterDistance = 461;
			this.m_split.TabIndex = 1;
			// 
			// m_grid_details
			// 
			this.m_grid_details.AllowUserToAddRows = false;
			this.m_grid_details.AllowUserToDeleteRows = false;
			this.m_grid_details.AllowUserToOrderColumns = true;
			this.m_grid_details.AllowUserToResizeRows = false;
			this.m_grid_details.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
			this.m_grid_details.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.m_grid_details.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_grid_details.Location = new System.Drawing.Point(0, 0);
			this.m_grid_details.Margin = new System.Windows.Forms.Padding(0);
			this.m_grid_details.Name = "m_grid_details";
			this.m_grid_details.RowHeadersVisible = false;
			this.m_grid_details.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.m_grid_details.Size = new System.Drawing.Size(677, 162);
			this.m_grid_details.TabIndex = 1;
			// 
			// m_grid_dups
			// 
			this.m_grid_dups.AllowUserToAddRows = false;
			this.m_grid_dups.AllowUserToDeleteRows = false;
			this.m_grid_dups.AllowUserToOrderColumns = true;
			this.m_grid_dups.AllowUserToResizeRows = false;
			this.m_grid_dups.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
			this.m_grid_dups.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.m_grid_dups.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_grid_dups.Location = new System.Drawing.Point(0, 0);
			this.m_grid_dups.Margin = new System.Windows.Forms.Padding(0);
			this.m_grid_dups.Name = "m_grid_dups";
			this.m_grid_dups.RowHeadersVisible = false;
			this.m_grid_dups.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.m_grid_dups.Size = new System.Drawing.Size(677, 461);
			this.m_grid_dups.TabIndex = 2;
			// 
			// DuplicateResults
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.m_split);
			this.Name = "DuplicateResults";
			this.Size = new System.Drawing.Size(677, 627);
			this.m_split.Panel1.ResumeLayout(false);
			this.m_split.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.m_split)).EndInit();
			this.m_split.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.m_grid_details)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.m_grid_dups)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion
	}
}
