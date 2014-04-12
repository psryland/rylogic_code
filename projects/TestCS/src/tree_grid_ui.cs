using System.Drawing;
using System.Windows.Forms;
using pr.extn;
using pr.gui;

namespace TestCS
{
	public class TreeGridUI :Form
	{
		private TreeGridView m_tree_grid;

		public TreeGridUI()
		{
			InitializeComponent();

			m_tree_grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

			m_tree_grid.Columns.Add(new TreeGridColumn{HeaderText="Tree"});
			m_tree_grid.Columns.Add(new DataGridViewTextBoxColumn{HeaderText="Col1"});
			m_tree_grid.Columns.Add(new DataGridViewTextBoxColumn{HeaderText="Col2"});
			m_tree_grid.Columns.Add(new DataGridViewTrackBarColumn{HeaderText="Col3"});
			m_tree_grid.ImageList = m_image_list;
			
			var node1 = m_tree_grid.Nodes.Add("node1", "bob", Color.Blue, 20);
			node1.ImageIndex = 0;
			
			var node1_1 = node1.Nodes.Add("node1_1", "Bob's child", Color.Red, 40);
			node1_1.ImageIndex = 1;
			
			node1.Nodes.Add("node1_3", "A", Color.Yellow, 60);
			node1.Nodes.Insert(1, "node1_2", "da", Color.Green, 30);
			
			var node1_1_1 = node1_1.Nodes.Add("node1_1_1", "Bob's child child", Color.Purple, 70);
			node1_1.Nodes.Add("node1_1_2", "a", Color.OldLace, 90);
			node1_1.Nodes.Add("node1_1_3", "a", Color.PowderBlue, 10);
			
			var node2 = m_tree_grid.Nodes.Add("node2", "alice", Color.Plum, 40);
			node2  .Nodes.Add("Fluff", "something", Color.Wheat, 20);
			
			m_tree_grid.Nodes.Add("node3", "frank", Color.Orange);
		}

		private ImageList m_image_list;

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
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(TreeGridUI));
			this.m_tree_grid = new pr.gui.TreeGridView();
			this.m_image_list = new System.Windows.Forms.ImageList(this.components);
			((System.ComponentModel.ISupportInitialize)(this.m_tree_grid)).BeginInit();
			this.SuspendLayout();
			// 
			// m_tree_grid
			// 
			this.m_tree_grid.AllowUserToAddRows = false;
			this.m_tree_grid.AllowUserToDeleteRows = false;
			this.m_tree_grid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
			this.m_tree_grid.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_tree_grid.ImageList = null;
			this.m_tree_grid.Location = new System.Drawing.Point(0, 0);
			this.m_tree_grid.Margin = new System.Windows.Forms.Padding(0);
			this.m_tree_grid.Name = "m_tree_grid";
			this.m_tree_grid.NodeCount = 0;
			this.m_tree_grid.ShowLines = true;
			this.m_tree_grid.Size = new System.Drawing.Size(284, 262);
			this.m_tree_grid.TabIndex = 0;
			this.m_tree_grid.VirtualNodes = false;
			// 
			// m_image_list
			// 
			this.m_image_list.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("m_image_list.ImageStream")));
			this.m_image_list.TransparentColor = System.Drawing.Color.Transparent;
			this.m_image_list.Images.SetKeyName(0, "Smiling gekko 150x121.jpg");
			// 
			// TreeGridUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(284, 262);
			this.Controls.Add(this.m_tree_grid);
			this.Name = "TreeGridUI";
			this.Text = "tree_grid_ui";
			((System.ComponentModel.ISupportInitialize)(this.m_tree_grid)).EndInit();
			this.ResumeLayout(false);

		}

		#endregion

	}
}
