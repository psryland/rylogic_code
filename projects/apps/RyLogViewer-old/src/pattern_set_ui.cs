using System.Windows.Forms;
using Rylogic.Gui.WinForms;
using Util = Rylogic.Utility.Util;

namespace RyLogViewer
{
	public class PatternSetUI :Form
	{
		#region UI Elements
		private FeatureTree m_tree;
		private Button m_btn_cancel;
		private Button m_btn_ok;
		private ToolTip m_tt;
		#endregion

		public PatternSetUI(PatternSet set)
		{
			InitializeComponent();
			m_tree.ShowRoot = false;
			Set = set;
		}
		protected override void Dispose(bool disposing)
		{
			Set = null;
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}

		/// <summary>The set displayed in the UI</summary>
		public PatternSet Set
		{
			get { return m_set; }
			set
			{
				if (m_set == value) return;
				if (m_set != null)
				{
					m_tree.Root = null;
				}
				m_set = value;
				if (m_set != null)
				{
					m_tree.Root = m_set;
					m_tree.CheckAll(true);
				}
			}
		}
		private PatternSet m_set;

		#region Windows Form Designer generated code
		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.Windows.Forms.TreeNode treeNode1 = new System.Windows.Forms.TreeNode("Highlights");
			System.Windows.Forms.TreeNode treeNode2 = new System.Windows.Forms.TreeNode("Filters");
			System.Windows.Forms.TreeNode treeNode3 = new System.Windows.Forms.TreeNode("Transforms");
			System.Windows.Forms.TreeNode treeNode4 = new System.Windows.Forms.TreeNode("Actions");
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PatternSetUI));
			this.m_btn_cancel = new System.Windows.Forms.Button();
			this.m_btn_ok = new System.Windows.Forms.Button();
			this.m_tree = new Rylogic.Gui.WinForms.FeatureTree();
			this.m_tt = new System.Windows.Forms.ToolTip(this.components);
			this.SuspendLayout();
			// 
			// m_btn_cancel
			// 
			this.m_btn_cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.m_btn_cancel.Location = new System.Drawing.Point(294, 249);
			this.m_btn_cancel.Name = "m_btn_cancel";
			this.m_btn_cancel.Size = new System.Drawing.Size(75, 23);
			this.m_btn_cancel.TabIndex = 2;
			this.m_btn_cancel.Text = "Cancel";
			this.m_btn_cancel.UseVisualStyleBackColor = true;
			// 
			// m_btn_ok
			// 
			this.m_btn_ok.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_ok.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btn_ok.Location = new System.Drawing.Point(213, 249);
			this.m_btn_ok.Name = "m_btn_ok";
			this.m_btn_ok.Size = new System.Drawing.Size(75, 23);
			this.m_btn_ok.TabIndex = 1;
			this.m_btn_ok.Text = "OK";
			this.m_btn_ok.UseVisualStyleBackColor = true;
			// 
			// m_tree
			// 
			this.m_tree.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_tree.ImageIndex = 0;
			this.m_tree.Location = new System.Drawing.Point(12, 12);
			this.m_tree.Name = "m_tree";
			treeNode1.Name = "Node0";
			treeNode1.Text = "Highlights";
			treeNode2.Name = "Node1";
			treeNode2.Text = "Filters";
			treeNode3.Name = "Node2";
			treeNode3.Text = "Transforms";
			treeNode4.Name = "Node3";
			treeNode4.Text = "Actions";
			this.m_tree.Nodes.AddRange(new System.Windows.Forms.TreeNode[] {
            treeNode1,
            treeNode2,
            treeNode3,
            treeNode4});
			this.m_tree.Root = null;
			this.m_tree.SelectedImageIndex = 0;
			this.m_tree.ShowRoot = false;
			this.m_tree.Size = new System.Drawing.Size(357, 231);
			this.m_tree.TabIndex = 0;
			// 
			// PatternSetUI
			// 
			this.AcceptButton = this.m_btn_ok;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.m_btn_cancel;
			this.ClientSize = new System.Drawing.Size(381, 284);
			this.Controls.Add(this.m_tree);
			this.Controls.Add(this.m_btn_ok);
			this.Controls.Add(this.m_btn_cancel);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "PatternSetUI";
			this.Text = "Pattern Set";
			this.ResumeLayout(false);

		}
		#endregion
	}
}
