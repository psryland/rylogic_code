using System;
using System.Drawing;
using System.Windows.Forms;
using pr.gui;

namespace RyLogViewer
{
	public class BookmarksUI :ToolForm
	{
		private readonly BindingSource m_marks;
		private DataGridView m_grid;

		/// <summary>An event called whenever the dialog gets a NextBookmark command</summary>
		public event Action NextBookmark;
		public void RaiseNextBookmark() { if (NextBookmark != null) NextBookmark(); }

		/// <summary>An event called whenever the dialog gets a FindPrev command</summary>
		public event Action PrevBookmark;
		public void RaisePrevBookmark() { if (PrevBookmark != null) PrevBookmark(); }

		public BookmarksUI(Form owner, BindingSource marks)
		:base(owner, new Size(-200, +28), new Size(200,320), EPin.TopRight, false)
		{
			InitializeComponent();
			m_marks = marks;

			m_grid.AutoGenerateColumns = false;
			m_grid.Columns.Add(new DataGridViewTextBoxColumn{Name = "FilePos" ,HeaderText = "File Position" ,DataPropertyName = "Position", ReadOnly = true ,FillWeight=1});
			m_grid.Columns.Add(new DataGridViewTextBoxColumn{Name = "Text"    ,HeaderText = "Line Text"     ,DataPropertyName = "Text"    , ReadOnly = true ,FillWeight=2});
			m_grid.DataSource = m_marks;
		}

		/// <summary>Handle global command keys</summary>
		protected override bool ProcessCmdKey(ref Message msg, Keys key_data)
		{
			switch (key_data)
			{
			default:
				var main = Owner as Main;
				if (main != null && main.HandleKeyDown(this, key_data)) return true;
				return base.ProcessCmdKey(ref msg, key_data);
			case Keys.Escape:
				Close();
				return true;
			}
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
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BookmarksUI));
			this.m_grid = new DataGridView();
			((System.ComponentModel.ISupportInitialize)(this.m_grid)).BeginInit();
			this.SuspendLayout();
			// 
			// m_grid
			// 
			this.m_grid.AllowUserToAddRows = false;
			this.m_grid.AllowUserToResizeRows = false;
			dataGridViewCellStyle1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(240)))));
			this.m_grid.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle1;
			this.m_grid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
			this.m_grid.BackgroundColor = System.Drawing.SystemColors.ControlLightLight;
			this.m_grid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.m_grid.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_grid.Location = new System.Drawing.Point(0, 0);
			this.m_grid.MultiSelect = false;
			this.m_grid.Name = "m_grid";
			this.m_grid.ReadOnly = true;
			this.m_grid.RowHeadersVisible = false;
			this.m_grid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
			this.m_grid.Size = new System.Drawing.Size(284, 262);
			this.m_grid.TabIndex = 0;
			// 
			// BookmarksUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(284, 262);
			this.Controls.Add(this.m_grid);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.KeyPreview = true;
			this.Name = "BookmarksUI";
			this.Text = "Bookmarks";
			((System.ComponentModel.ISupportInitialize)(this.m_grid)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion
	}
}
