using System.Windows.Forms;
using pr.util;

namespace LDraw
{
	public class ObjectManagerUI :Form
	{
		#region UI Elements
		private SplitContainer m_split0;
		private TreeView m_tree;
		#endregion

		public ObjectManagerUI()
		{
			InitializeComponent();
		}
		protected override void Dispose(bool disposing)
		{
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code
		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			this.m_split0 = new System.Windows.Forms.SplitContainer();
			this.m_tree = new System.Windows.Forms.TreeView();
			((System.ComponentModel.ISupportInitialize)(this.m_split0)).BeginInit();
			this.m_split0.Panel1.SuspendLayout();
			this.m_split0.SuspendLayout();
			this.SuspendLayout();
			// 
			// m_split0
			// 
			this.m_split0.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_split0.Location = new System.Drawing.Point(0, 0);
			this.m_split0.Name = "m_split0";
			// 
			// m_split0.Panel1
			// 
			this.m_split0.Panel1.Controls.Add(this.m_tree);
			this.m_split0.Size = new System.Drawing.Size(596, 598);
			this.m_split0.SplitterDistance = 208;
			this.m_split0.TabIndex = 0;
			// 
			// m_tree
			// 
			this.m_tree.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_tree.Location = new System.Drawing.Point(0, 0);
			this.m_tree.Name = "m_tree";
			this.m_tree.Size = new System.Drawing.Size(208, 598);
			this.m_tree.TabIndex = 0;
			// 
			// ObjectManagerUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(596, 598);
			this.Controls.Add(this.m_split0);
			this.Name = "ObjectManagerUI";
			this.Text = "Scene Manager";
			this.m_split0.Panel1.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.m_split0)).EndInit();
			this.m_split0.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion
	}
}
