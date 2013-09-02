using System.Collections.Generic;
using System.Windows.Forms;

namespace pr.gui
{
	/// <summary>A helper dialog for display a simple list of items for a user to select from</summary>
	public class ListForm :Form
	{
		private readonly ToolTip m_tt;

		/// <summary>Change how many items can be selected</summary>
		public SelectionMode SelectionMode
		{
			get { return m_list.SelectionMode; }
			set { m_list.SelectionMode = value; }
		}

		/// <summary>The text to display above the prompt edit box</summary>
		public string PromptText
		{
			get { return m_lbl_info.Text; }
			set { m_lbl_info.Text = value; }
		}

		/// <summary>The prompt dialog title</summary>
		public string Title
		{
			get { return Text; }
			set { Text = value; }
		}

		/// <summary>The items displayed in the list box</summary>
		public List<object> Items { get; private set; }

		/// <summary>The indices of the selected items</summary>
		public IEnumerable<int> SelectedIndices
		{
			get
			{
				foreach (var i in m_list.SelectedIndices)
					yield return (int)i;
			}
			set
			{
				m_list.ClearSelected();
				foreach (var i in value)
					m_list.SetSelected(i, true);
			}
		}

		/// <summary>The selected items</summary>
		public IEnumerable<object> SelectedItems
		{
			get
			{
				foreach (var o in m_list.SelectedItems)
					yield return o;
			}
			set
			{
				m_list.ClearSelected();
				foreach (var o in value)
				{
					int idx = m_list.Items.IndexOf(o);
					if (idx == -1) continue;
					m_list.SetSelected(idx, true);
				}
			}
		}
		
		public ListForm()
		{
			InitializeComponent();
			SelectionMode = SelectionMode.One;
			m_tt = new ToolTip();
			Items = new List<object>();
			m_list.DataSource = new BindingSource{DataSource = Items};
			m_list.SelectedIndexChanged += (s,a) =>
				{
					m_btn_ok.Enabled = m_list.SelectedItems.Count != 0;
				};
			Disposed += (s,a) =>
				{
					m_tt.Dispose();
				};
		}
		private Label m_lbl_info;
		private Button m_btn_ok;
		private Button m_btn_cancel;
		private ListBox m_list;
		
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
			this.m_lbl_info = new System.Windows.Forms.Label();
			this.m_btn_ok = new System.Windows.Forms.Button();
			this.m_btn_cancel = new System.Windows.Forms.Button();
			this.m_list = new System.Windows.Forms.ListBox();
			this.SuspendLayout();
			// 
			// m_lbl_info
			// 
			this.m_lbl_info.AutoSize = true;
			this.m_lbl_info.Location = new System.Drawing.Point(12, 10);
			this.m_lbl_info.Name = "m_lbl_info";
			this.m_lbl_info.Size = new System.Drawing.Size(80, 13);
			this.m_lbl_info.TabIndex = 1;
			this.m_lbl_info.Text = "Please choose:";
			// 
			// m_btn_ok
			// 
			this.m_btn_ok.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btn_ok.Location = new System.Drawing.Point(127, 215);
			this.m_btn_ok.Name = "m_btn_ok";
			this.m_btn_ok.Size = new System.Drawing.Size(75, 23);
			this.m_btn_ok.TabIndex = 2;
			this.m_btn_ok.Text = "OK";
			this.m_btn_ok.UseVisualStyleBackColor = true;
			// 
			// m_btn_cancel
			// 
			this.m_btn_cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.m_btn_cancel.Location = new System.Drawing.Point(208, 215);
			this.m_btn_cancel.Name = "m_btn_cancel";
			this.m_btn_cancel.Size = new System.Drawing.Size(75, 23);
			this.m_btn_cancel.TabIndex = 3;
			this.m_btn_cancel.Text = "Cancel";
			this.m_btn_cancel.UseVisualStyleBackColor = true;
			// 
			// m_list
			// 
			this.m_list.FormattingEnabled = true;
			this.m_list.Location = new System.Drawing.Point(15, 28);
			this.m_list.Name = "m_list";
			this.m_list.Size = new System.Drawing.Size(268, 173);
			this.m_list.TabIndex = 4;
			// 
			// PromptForm
			// 
			this.AcceptButton = this.m_btn_ok;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.m_btn_cancel;
			this.ClientSize = new System.Drawing.Size(295, 250);
			this.Controls.Add(this.m_list);
			this.Controls.Add(this.m_btn_cancel);
			this.Controls.Add(this.m_btn_ok);
			this.Controls.Add(this.m_lbl_info);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
			this.Name = "PromptForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Choose";
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion
	}
}
