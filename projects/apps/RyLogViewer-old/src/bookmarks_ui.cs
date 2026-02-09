using System;
using System.Drawing;
using System.Windows.Forms;
using Rylogic.Gui.WinForms;
using Util = Rylogic.Utility.Util;

namespace RyLogViewer
{
	public class BookmarksUI :ToolForm
	{
		#region UI Elements
		private DataGridView m_grid;
		#endregion

		public BookmarksUI(Form owner, BindingSource<Bookmark> marks)
			:base(owner, EPin.TopRight, new Point(-200, +28), new Size(200,320), false)
		{
			InitializeComponent();
			Marks = marks;
			SetupUI();
		}
		protected override void Dispose(bool disposing)
		{
			Marks = null;
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}
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

		/// <summary>The collection of bookmarks</summary>
		private BindingSource<Bookmark> Marks
		{
			get;
			set
			{
				if (field == value) return;
				field = value;
			}
		}

		/// <summary>An event called whenever the dialog gets a NextBookmark command</summary>
		public event Action NextBookmark;
		public void RaiseNextBookmark()
		{
			NextBookmark?.Invoke();
		}

		/// <summary>An event called whenever the dialog gets a FindPrev command</summary>
		public event Action PrevBookmark;
		public void RaisePrevBookmark()
		{
			PrevBookmark?.Invoke();
		}

		/// <summary>Set up UI elements</summary>
		private void SetupUI()
		{
			m_grid.AutoGenerateColumns = false;
			m_grid.Columns.Add(new DataGridViewTextBoxColumn{Name = "FilePos" ,HeaderText = "File Position" ,DataPropertyName = "Position", ReadOnly = true ,FillWeight=1});
			m_grid.Columns.Add(new DataGridViewTextBoxColumn{Name = "Text"    ,HeaderText = "Line Text"     ,DataPropertyName = "Text"    , ReadOnly = true ,FillWeight=2});
			m_grid.DataSource = Marks;
		}

		#region Windows Form Designer generated code
		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BookmarksUI));
			this.m_grid = new RyLogViewer.DataGridView();
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
