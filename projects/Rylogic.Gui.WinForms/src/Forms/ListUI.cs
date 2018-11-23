using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Rylogic.Extn;
using Rylogic.Utility;

namespace Rylogic.Gui.WinForms
{
	/// <summary>A helper dialog for display a simple list of items for a user to select from</summary>
	public class ListUI :Form
	{
		private TableLayoutPanel m_table;
		private ListBox m_list;
		private Label m_lbl_info;
		private Button m_btn_ok;
		private Button m_btn_cancel;
		private Panel m_panel_btns;
		private ToolTip m_tt;

		public ListUI()
		{
			InitializeComponent();
			SelectionMode = SelectionMode.One;

			m_bs_items = new BindingSource<object>();
			Items = new List<object>();

			m_list.DataSource = m_bs_items;
			m_list.SelectedIndexChanged += (s,a) =>
			{
				m_btn_ok.Enabled = m_list.SelectedItems.Count != 0;
			};
			m_list.MouseDoubleClick += (s,a) =>
			{
				m_btn_ok.PerformClick();
			};
		}
		protected override void Dispose(bool disposing)
		{
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}
		protected override void OnParentChanged(EventArgs e)
		{
			base.OnParentChanged(e);
			Icon = Owner?.Icon;
		}

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
			set
			{
				m_lbl_info.Text = value;

				// Resize the dialog to fit the prompt text
				// Find the screen area to bound the message text
				var screen_area = (TopLevelControl != null ? Screen.FromControl(TopLevelControl) : Screen.PrimaryScreen).WorkingArea;
				screen_area.Inflate(-screen_area.Width / 8, -screen_area.Height / 8);

				// Measure the text to be displayed
				// If it's larger than the screen area, limit the size but enable scroll bars
				var text_area = m_lbl_info.PreferredSize;
				const float ReflowAspectRatio = 7f;
				if (text_area.Area() != 0f && text_area.Aspect() > ReflowAspectRatio)
				{
					var scale = Math.Sqrt(ReflowAspectRatio / text_area.Aspect());
					m_lbl_info.MaximumSize = new Size((int)(text_area.Width * scale), 0);
					text_area = m_lbl_info.PreferredSize;
					m_lbl_info.MaximumSize = Size.Empty;
				}
				ClientSize = new Size(text_area.Width + m_lbl_info.Margin.Left + m_lbl_info.Margin.Right, ClientSize.Height);
			}
		}

		/// <summary>The prompt dialog title</summary>
		public string Title
		{
			get { return Text; }
			set { Text = value; }
		}

		/// <summary>The items displayed in the list box</summary>
		public IEnumerable<object> Items
		{
			get { return (IEnumerable<object>)m_bs_items.DataSource; }
			set
			{
				m_bs_items.DataSource = value;
				m_bs_items.ResetBindings(false);
			}
		}
		private BindingSource<object> m_bs_items;

		/// <summary>The property name to display in the list box</summary>
		public string DisplayMember
		{
			get { return m_list.DisplayMember; }
			set { m_list.DisplayMember = value; }
		}

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

		/// <summary>Display a ListForm for choosing an item from a list</summary>
		public static T ChooseOne<T>(string prompt, string title, IEnumerable<T> items, string display_member = null) { return ChooseOne<T>(null, prompt, title, items, display_member); }
		public static T ChooseOne<T>(IWin32Window owner, string prompt, string title, IEnumerable<T> items, string display_member = null)
		{
			using (var dlg = new ListUI{Title = title, PromptText = prompt, Items = items.Cast<object>(), SelectionMode = SelectionMode.One, DisplayMember = display_member})
				return dlg.ShowDialog(owner) == DialogResult.OK
					? dlg.SelectedItems.Cast<T>().FirstOrDefault()
					: default(T);
		}

		/// <summary>Display a ListForm for choosing items from a list</summary>
		public static IEnumerable<T> ChooseMany<T>(string prompt, string title, IEnumerable<T> items, string display_member = null) { return ChooseMany<T>(null, prompt, title, items, display_member); }
		public static IEnumerable<T> ChooseMany<T>(IWin32Window owner, string prompt, string title, IEnumerable<T> items, string display_member = null)
		{
			using (var dlg = new ListUI{Title = title, PromptText = prompt, Items = items.Cast<object>(), SelectionMode = SelectionMode.MultiExtended, DisplayMember = display_member})
				return dlg.ShowDialog(owner) == DialogResult.OK
					? dlg.SelectedItems.Cast<T>()
					: Enumerable.Empty<T>();
		}

		#region Windows Form Designer generated code
		private System.ComponentModel.IContainer components = null;
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.m_lbl_info = new System.Windows.Forms.Label();
			this.m_btn_ok = new System.Windows.Forms.Button();
			this.m_btn_cancel = new System.Windows.Forms.Button();
			this.m_list = new Rylogic.Gui.WinForms.ListBox();
			this.m_tt = new System.Windows.Forms.ToolTip(this.components);
			this.m_table = new System.Windows.Forms.TableLayoutPanel();
			this.m_panel_btns = new System.Windows.Forms.Panel();
			this.m_table.SuspendLayout();
			this.m_panel_btns.SuspendLayout();
			this.SuspendLayout();
			// 
			// m_lbl_info
			// 
			this.m_lbl_info.AutoSize = true;
			this.m_lbl_info.Location = new System.Drawing.Point(6, 6);
			this.m_lbl_info.Margin = new System.Windows.Forms.Padding(6);
			this.m_lbl_info.Name = "m_lbl_info";
			this.m_lbl_info.Size = new System.Drawing.Size(80, 13);
			this.m_lbl_info.TabIndex = 1;
			this.m_lbl_info.Text = "Please choose:";
			// 
			// m_btn_ok
			// 
			this.m_btn_ok.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_ok.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btn_ok.Location = new System.Drawing.Point(105, 6);
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
			this.m_btn_cancel.Location = new System.Drawing.Point(184, 6);
			this.m_btn_cancel.Name = "m_btn_cancel";
			this.m_btn_cancel.Size = new System.Drawing.Size(75, 23);
			this.m_btn_cancel.TabIndex = 3;
			this.m_btn_cancel.Text = "Cancel";
			this.m_btn_cancel.UseVisualStyleBackColor = true;
			// 
			// m_list
			// 
			this.m_list.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_list.FormattingEnabled = true;
			this.m_list.IntegralHeight = false;
			this.m_list.Location = new System.Drawing.Point(6, 31);
			this.m_list.Margin = new System.Windows.Forms.Padding(6);
			this.m_list.Name = "m_list";
			this.m_list.Size = new System.Drawing.Size(253, 167);
			this.m_list.TabIndex = 4;
			// 
			// m_table
			// 
			this.m_table.ColumnCount = 1;
			this.m_table.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.m_table.Controls.Add(this.m_lbl_info, 0, 0);
			this.m_table.Controls.Add(this.m_list, 0, 1);
			this.m_table.Controls.Add(this.m_panel_btns, 0, 2);
			this.m_table.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_table.Location = new System.Drawing.Point(0, 0);
			this.m_table.Margin = new System.Windows.Forms.Padding(0);
			this.m_table.Name = "m_table";
			this.m_table.RowCount = 3;
			this.m_table.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.m_table.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.m_table.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.m_table.Size = new System.Drawing.Size(265, 242);
			this.m_table.TabIndex = 5;
			// 
			// m_panel_btns
			// 
			this.m_panel_btns.Controls.Add(this.m_btn_cancel);
			this.m_panel_btns.Controls.Add(this.m_btn_ok);
			this.m_panel_btns.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_panel_btns.Location = new System.Drawing.Point(0, 204);
			this.m_panel_btns.Margin = new System.Windows.Forms.Padding(0);
			this.m_panel_btns.Name = "m_panel_btns";
			this.m_panel_btns.Size = new System.Drawing.Size(265, 38);
			this.m_panel_btns.TabIndex = 5;
			// 
			// ListUI
			// 
			this.AcceptButton = this.m_btn_ok;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.m_btn_cancel;
			this.ClientSize = new System.Drawing.Size(265, 242);
			this.Controls.Add(this.m_table);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
			this.MinimumSize = new System.Drawing.Size(180, 134);
			this.Name = "ListUI";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Choose";
			this.m_table.ResumeLayout(false);
			this.m_table.PerformLayout();
			this.m_panel_btns.ResumeLayout(false);
			this.ResumeLayout(false);

		}
		#endregion
	}
}
