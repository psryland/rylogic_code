using System;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using pr.gui;
using System.Xml.Linq;
using pr.extn;
using pr.container;
using pr.util;
using DataGridView = pr.gui.DataGridView;

namespace Csex
{
	public class XmlTable :DataGridView ,IDockable
	{
		private DragDrop m_dd;

		private XmlTable() {}
		public XmlTable(string base_name, XElement elem)
		{
			AllowDrop = true;
			AllowUserToAddRows = true;
			AutoGenerateColumns = false;
			RowHeadersVisible = false;
			ColumnHeadersVisible = true;
			GridColor = SystemColors.ControlLight;
			AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
			ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
			ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.False;
			SelectionMode = DataGridViewSelectionMode.CellSelect;
			RowTemplate.Height = 20;

			if (elem.Parent == null)
				throw new Exception("Cannot create a table for a root-level node");

			BaseName = base_name;
			Element = elem;
			ParentNode = elem.Parent;

			m_dd = new DragDrop();
			m_dd.DoDrop += DataGridViewEx.DragDrop_DoDropMoveRow;

			var name = TabName;
			DockControl = new DockControl(this, name) { TabText = name, TabCMenu = new ContextMenuStrip() };
			DockControl.TabCMenu.Items.Add2("Save", null, (s,a) => SaveChanges());
			DockControl.TabCMenu.Items.Add2("Close", null, (s,a) => Close());
			DockControl.PaneChanged += (s,a) =>
			{
				if (DockControl.DockPane != null) return;
				Close();
			};

			// Add column headers for each child value
			foreach (var e in elem.Elements().Where(x => !x.HasElements))
			{
				Columns.Add(new DataGridViewTextBoxColumn
				{
					HeaderText = e.Name.LocalName,
					DataPropertyName = e.Name.LocalName,
					DefaultCellStyle = new DataGridViewCellStyle {Alignment = DataGridViewContentAlignment.TopLeft, WrapMode = DataGridViewTriState.True },
				});
			}
		}
		public void Close()
		{
			if (CancelDueToUnsavedChanges()) return;
			DockControl.Dispose();
		}

		/// <summary>The name of the XML data this table came from</summary>
		private string BaseName { get; set; }

		/// <summary>The example element this table is based on</summary>
		private XElement Element { get; set; }

		/// <summary>The name of the child element that this is a table of</summary>
		private string ElementName
		{
			get { return Element.Name.LocalName; }
		}

		/// <summary>The XML node that is the root for the table</summary>
		private XElement ParentNode
		{
			get { return m_parent; }
			set
			{
				if (m_parent == value) return;
				if (m_parent != null)
				{
					if (CancelDueToUnsavedChanges())
						return;

					DataSource = null;
					m_list.ListChanging -= HandleListChanged;
					m_list = null;
				}
				m_parent = value;
				if (m_parent != null)
				{
					m_list = new BindingListEx<XElement>(ParentNode.Elements(ElementName));
					m_list.ListChanging += HandleListChanged;
					DataSource = m_list;

					Modified = false;
				}
			}
		}
		private XElement m_parent;
		private BindingListEx<XElement> m_list;

		/// <summary>Docking functionality</summary>
		public DockControl DockControl { get; private set; }

		/// <summary>The name to display on the tab for this table</summary>
		public string TabName
		{
			get { return "{2}{0}: {1}".Fmt(BaseName, string.Join("/", Element.AncestorsAndSelf().Reversed().Select(x => x.Name.LocalName)), Modified ? "*" : ""); }
		}

		/// <summary>True if elements have been added/removed/modified</summary>
		public bool Modified
		{
			get { return m_modified; }
			set
			{
				if (m_modified == value) return;
				m_modified = value;
				DockControl.TabText = TabName;
			}
		}
		private bool m_modified;

		/// <summary>Prompt to save changes</summary>
		private bool CancelDueToUnsavedChanges()
		{
			if (!Modified) return false;
			var res = MsgBox.Show(this, "Data has been modified. Save changes?", "Save Changes", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
			if (res == DialogResult.Yes) SaveChanges();
			if (res == DialogResult.No) Modified = false;
			return res == DialogResult.Cancel;
		}

		/// <summary>Save the changes made to the table back to the ParentNode</summary>
		public void SaveChanges()
		{
			if (!Modified)
				return;

			ParentNode.RemoveNodes(ElementName);
			foreach (var elem in m_list)
				ParentNode.Add(elem);

			Modified = false;
		}

		/// <summary>Supply the data for the grid</summary>
		protected override void OnCellFormatting(DataGridViewCellFormattingEventArgs e)
		{
			base.OnCellFormatting(e);

			DataGridViewColumn col;
			if (this.Within(e.ColumnIndex, e.RowIndex, out col))
			{
				e.Value = m_list[e.RowIndex].Element(col.DataPropertyName).Value;
				e.FormattingApplied = true;
			}
		}

		/// <summary>Show/hide columns</summary>
		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);
			DataGridViewEx.ColumnVisibility(this, e);
			DataGridViewEx.DragDrop_DragRow(this, e);
		}

		/// <summary>Update all row heights together</summary>
		protected override void OnRowHeightInfoNeeded(DataGridViewRowHeightInfoNeededEventArgs args)
		{
			args.Height = RowTemplate.Height;
		}
		protected override void OnRowHeightInfoPushed(DataGridViewRowHeightInfoPushedEventArgs args)
		{
			RowTemplate.Height = args.Height;
			Invalidate();
		}

		/// <summary>When the list of elements changes, the data is modified</summary>
		private void HandleListChanged(object sender, ListChgEventArgs<XElement> e)
		{
			Modified = true;
		}
	}
}
