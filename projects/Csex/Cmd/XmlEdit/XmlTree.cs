using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using pr.gui;
using pr.util;
using pr.common;
using pr.extn;
using System.Xml.Linq;

namespace Csex
{
	public class XmlTree :TreeGridView ,IDockable
	{
		private XmlTree() { }
		public XmlTree(string filename) :this()
		{
			AllowUserToAddRows                     = false;
			AllowUserToDeleteRows                  = false;
			AllowUserToResizeRows                  = true;
			AllowUserToOrderColumns                = false;
			RowHeadersVisible                      = false;
			ColumnHeadersVisible                   = true;
			GridColor                              = SystemColors.ControlLight;
			AutoSizeColumnsMode                    = DataGridViewAutoSizeColumnsMode.Fill;
			ClipboardCopyMode                      = DataGridViewClipboardCopyMode.EnableWithoutHeaderText;
			SelectionMode                          = DataGridViewSelectionMode.FullRowSelect;
			ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.False;
			RowTemplate.Height                     = 20;
			
			XmlName = Path_.FileTitle(filename);
			DockControl = new DockControl(this, "XmlFile-{0}".Fmt(XmlName)) { TabText = XmlName, TabCMenu = new ContextMenuStrip() };
			DockControl.TabCMenu.Items.Add2("Close", null, (s,a) => Dispose());

			Root = XDocument.Load(filename).Root;
		}
		protected override void Dispose(bool disposing)
		{
			DockControl = Util.Dispose(DockControl);
			base.Dispose(disposing);
		}

		/// <summary>Docking functionality</summary>
		public DockControl DockControl { get; private set; }

		/// <summary>A name for the XML data</summary>
		public string XmlName { get; private set; }

		/// <summary>The tree of XML data</summary>
		public XElement Root
		{
			get { return m_root; }
			private set
			{
				if (m_root == value) return;
				if (m_root != null)
				{
					using (this.SuspendLayout(true))
					{
						Columns.Clear();
					}
				}
				m_root = value;
				if (m_root != null)
				{
					using (this.SuspendLayout(true))
					{
						Columns.Add(new TreeGridColumn
						{
							HeaderText = "Element",
							DefaultCellStyle = new DataGridViewCellStyle {Alignment = DataGridViewContentAlignment.TopLeft, WrapMode = DataGridViewTriState.True },
							ReadOnly = true,
						});
						Columns.Add(new DataGridViewTextBoxColumn
						{
							HeaderText = "Value",
							DefaultCellStyle = new DataGridViewCellStyle {Alignment = DataGridViewContentAlignment.TopLeft, WrapMode = DataGridViewTriState.True },
						});
						foreach (var elem in m_root.Elements())
						{
							var node = Nodes.Add(elem.Name.LocalName, elem.HasElements ? string.Empty : elem.Value);
							node.Tag = elem;
							node.VirtualNodes = elem.HasElements;
						}
					}
				}
			}
		}
		private XElement m_root;

		/// <summary>Update all row heights together</summary>
		protected override void OnRowHeightInfoNeeded(DataGridViewRowHeightInfoNeededEventArgs args)
		{
			args.Height = RowTemplate.Height;
		}
		protected override void OnRowHeightInfoPushed(DataGridViewRowHeightInfoPushedEventArgs args)
		{
			RowTemplate.Height = args.Height;
			Refresh();
		}

		/// <summary>Handle a node expanding</summary>
		protected override void OnNodeExpanding(ExpandingEventArgs e)
		{
			base.OnNodeExpanding(e);

			var root = e.Node;
			var elem = e.Node.Tag as XElement;

			// Add the child elements
			foreach (var child in elem.Elements())
			{
				var node = root.Nodes.Add(child.Name.LocalName, child.HasElements ? string.Empty : child.Value);
				node.Tag = child;
				node.VirtualNodes = child.HasElements;
			}
		}

		/// <summary>Handle a node collapsing</summary>
		protected override void OnNodeCollapsing(CollapsingEventArgs e)
		{
			base.OnNodeCollapsing(e);

			var root = e.Node;
			var elem = e.Node.Tag as XElement;

			root.Nodes.Clear();
			root.VirtualNodes = elem.HasElements;
		}

		/// <summary>Handle keyboard shortcuts</summary>
		protected override void OnKeyDown(KeyEventArgs e)
		{
			DataGridView_.CutCopyPasteReplace(this, e);

			if (e.KeyCode >= Keys.D0 && e.KeyCode <= Keys.D9 && Nodes.Count != 0 && !IsCurrentCellInEditMode)
			{
				var level = (int)(e.KeyValue - Keys.D0);
				var collapse = ModifierKeys.HasFlag(Keys.Shift);

				Action<TreeGridNode, int> toggle = null;
				toggle = (node, lvl) =>
				{
					if (!collapse) node.Expand();
					if (lvl < level) node.Nodes.ForEach(x => toggle(x, lvl+1));
					if ( collapse) node.Collapse();
				};

				using (this.SuspendLayout(true))
					Nodes.ForEach(x => toggle(x, 1));

				e.Handled = true;
			}

			base.OnKeyDown(e);
		}

		/// <summary>Show/hide columns</summary>
		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);
			DataGridView_.ColumnVisibility(this, e);

			if (e.Button == MouseButtons.Right)
			{
				var hit = this.HitTestEx(e.X, e.Y);
				if (hit.Type == DataGridView_.HitTestInfo.EType.Cell && hit.ColumnIndex == 0 && hit.Cell != null)
				{
					var elem = (XElement)hit.Row.Tag;
					var sib_count = elem.Parent?.Elements().Count(x => x.Name == elem.Name) ?? 0;
					var cmenu = new ContextMenuStrip();
					{
						var opt = cmenu.Items.Add2(new ToolStripMenuItem("View as Table"));
						opt.Enabled = sib_count > 1;
						opt.Click += (s,a) =>
						{
							var tbl = new XmlTable(XmlName, elem);
							DockControl.DockContainer.Add(tbl, EDockSite.Bottom);
						};
					}
					cmenu.Show(this, hit.GridPoint);
				}
			}
		}
	}
}
