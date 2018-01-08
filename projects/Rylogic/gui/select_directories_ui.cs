using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Security;
using System.Security.Permissions;
using System.Windows.Forms;
using Rylogic.Common;
using Rylogic.Container;
using Rylogic.Extn;
using Rylogic.Utility;

namespace Rylogic.Gui
{
	public class SelectDirectoriesUI :Form
	{
		// Maintains the 'Paths' collection by adding and removing
		// paths that incorporate other paths as paths are added/removed.
		// The tree control is just a representation of the 'Paths' list

		public SelectDirectoriesUI()
		{
			InitializeComponent();
			StartPosition = FormStartPosition.CenterParent;

			SetupTree();
			SetupList();
			SetupPathsList();

			m_hs = new HoverScroll(m_tree.Handle, m_listbox.Handle);
			m_listbox.DataSource = new BindingSource{DataSource = Paths};
		}
		public SelectDirectoriesUI(IEnumerable<string> paths) :this()
		{
			AddPaths(paths);
		}
		protected override void Dispose(bool disposing)
		{
			Util.Dispose(ref components);
			base.Dispose(disposing);
		}

		/// <summary>Populate the directory tree</summary>
		private void SetupTree()
		{
			// Sort directories alphabetically
			m_tree.TreeViewNodeSorter = Cmp<TreeNode>.From((l,r) => string.Compare(l.Name, r.Name));

			// Dynamically add sub directory nodes as needed
			m_tree.NodesNeeded += (s,a) =>
				{
					if (a.Node.Nodes.Count != 0) return;
					PopulateChildNodes(a.Node);
				};

			// Populate the tree with the drives
			foreach (var dv in DriveInfo.GetDrives())
			{
				if (!dv.IsReady) continue;
				var drive = dv.Name.TrimEnd('\\');
				var node = new TreeNode(drive, TreeNode.ETriState.Unchecked);
				m_tree.Nodes.Add(node);
				PopulateChildNodes(node);
			}

			// Checking a node adds or removes that path from the list
			m_tree.AfterCheck += (s,a) =>
				{
					if (m_updating_check_marks != null)
						return;

					// Add or remove the path
					var path = new Path(a.Node.FullPath);
					if (a.Node.Checked)
					{
						Paths.AddOrdered(path, Path.Compare);
					}
					else
					{
						// If 'path' is in the list, then the ListChanging handler will call HandlePathRemoved
						// If not in the list, then we need to call it explicitly
						if (!Paths.Remove(path))
							HandlePathRemoved(path);
					}
				};
		}

		/// <summary>Set up the list box</summary>
		private void SetupList()
		{
			m_listbox.DoubleClick += (s,a) =>
			{
				var path = (Path)m_listbox.SelectedItem;
				m_tree.SelectedNode = m_tree.GetNode(path.Name);
				m_tree.SelectedNode.EnsureVisible();
			};
			m_listbox.KeyDown += ListBox_.SelectAll;
			m_listbox.KeyDown += ListBox_.Copy;
		}

		/// <summary>Initialise the list of directories to search</summary>
		private void SetupPathsList()
		{
			Paths = new BindingListEx<Path>();

			// Whenever the collections of paths changes, update the check boxes
			Paths.ListChanging += (s,a) =>
				{
					switch (a.ChangeType)
					{
					case ListChg.ItemPreAdd:
						{
							// Ensure items are added in order
							var idx = Paths.BinarySearch(a.Item, Path.Compare);
							if (idx != a.Index && ~idx != a.Index) throw new Exception("Paths must be inserted in order. Use Paths.AddOrdered()");
							break;
						}
					case ListChg.ItemAdded:
						{
							HandlePathAdded(a.Item);
							break;
						}
					case ListChg.ItemRemoved:
						{
							HandlePathRemoved(a.Item);
							break;
						}
					case ListChg.Reset:
						{
							UpdateTree();
							break;
						}
					}
				};
		}

		/// <summary>The collection of selected directories (sorted)</summary>
		public BindingListEx<Path> Paths { get; private set; }
		public class Path
		{
			/// <summary>The path in all lower case and with a trailing backslash</summary>
			public string Name { get; private set; }

			/// <summary>The path with preserved case</summary>
			public string Text { get; private set; }

			public Path(string path)
			{
				// Ensure 'path' has a trailing backslash
				if (!path.EndsWith("\\")) path += "\\";

				Text = path;
				Name = Text.ToLowerInvariant();
			}
			public override bool Equals(object obj)
			{
				var rhs = obj as Path;
				return rhs != null && Equals(Name, rhs.Name);
			}
			public override int GetHashCode()
			{
				return Name.GetHashCode();
			}
			public override string ToString()
			{
				return Text;
			}
			public static implicit operator string(Path p)
			{
				return p.Text;
			}
			public static implicit operator Path(string s)
			{
				return new Path(s);
			}

			/// <summary>Compare function for Path</summary>
			public int CompareTo(Path rhs) { return string.CompareOrdinal(Name, rhs.Name); }
			public static Cmp<Path> Compare = Cmp<Path>.From((l,r) => l.CompareTo(r));
		}

		/// <summary>Add a path to 'Paths' obeying the ordering requirements</summary>
		public void AddPath(string path)
		{
			Paths.AddOrdered(path, Path.Compare);
		}

		/// <summary>Add a collection of paths to 'Paths' obeying the ordering requirements</summary>
		public void AddPaths(IEnumerable<string> paths)
		{
			foreach (var path in paths)
				AddPath(path);
		}

		/// <summary>Called after 'path' has been added to the collection</summary>
		private void HandlePathAdded(Path path)
		{
			// Look for paths that would include 'path' already
			var p = new StringBuilder();
			var parts = path.Name.Split(new[]{'\\'}, StringSplitOptions.RemoveEmptyEntries);
			for (int i = 0, iend = parts.Length - 1; i < iend; ++i)
			{
				p.Append(parts[i]).Append("\\");

				// If a parent path is in the collection already, we don't need 'path'
				var idx = Paths.BinarySearch(p.ToString(), Path.Compare);
				if (idx >= 0)
				{
					idx = Paths.BinarySearch(path, Path.Compare);
					if (idx >= 0) Paths.RemoveAt(idx);
					return;
				}
			}

			// Remove any paths that are children of 'path'
			{
				var idx = Paths.BinarySearch(path, Path.Compare);
				if (idx < 0) idx = ~idx;
				else ++idx;

				// Find the range of paths to remove
				var count = Paths.Skip(idx).Count(x => x.Name.StartsWith(path.Name));
				Paths.RemoveRange(idx, count);
			}

			// Check the siblings of 'path', if all are in 'Paths' then we can remove
			// all of these child paths and replace it with the parent path
			p.Clear().Append(path.Name);
			for (int i = parts.Length; i-- != 0;)
			{
				p.Length -= parts[i].Length + 1;
				if (p.Length == 0 ||
					!Shell_.EnumFileSystem(p.ToString(), SearchOption.TopDirectoryOnly)
					.Where(x => x.IsDirectory)
					.All(x => Paths.BinarySearch(x.FullPath, Path.Compare) >= 0))
					break; // Not all child paths of 'p' are in 'Paths'

				// Do this as we go, because the test above needs paths we add here
				using (Paths.SuspendEvents(true))
				{
					var parent = new Path(path.Text.Substring(0, p.Length));
					var idx = Paths.BinarySearch(parent, Path.Compare);
					if (idx < 0) idx = ~idx; else throw new Exception($"Paths contains {parent} and {Paths[idx]}. {Paths[idx]} should not be in 'Paths' as it's covered by {parent}");

					// Remove all children of 'parent'
					var count = Paths.Skip(idx).Count(x => x.Name.StartsWith(parent.Name));
					Paths.RemoveRange(idx, count);

					// Add 'parent'
					Paths.Insert(idx, parent);
				}
			}

			// Update the check boxes
			UpdateTree();
		}

		/// <summary>Called after 'path' has been removed from the collection</summary>
		private void HandlePathRemoved(Path path)
		{
			// Remember, 'path' may not have been in the collection, but may be a child
			// of a path that is in the collection.
			var p = new StringBuilder();
			var parts = path.Name.Split(new[]{'\\'}, StringSplitOptions.RemoveEmptyEntries);
			
			// Look for a path that is an ancestral parent of 'path'
			var part_idx = 0;
			for (; part_idx != parts.Length; ++part_idx)
			{
				p.Append(parts[part_idx]).Append("\\");

				// If the parent path is in the collection, we need to replace
				// it with its children except for 'p.Append(parts[i+1])'
				var idx = Paths.BinarySearch(p.ToString(), Path.Compare);
				if (idx >= 0)
					break;
			}

			// If a parent is found, we need to add child paths for the ancestral siblings of 'path'
			if (part_idx != parts.Length)
			{
				// Remove the parent path
				var idx = Paths.BinarySearch(p.ToString(), Path.Compare);
				Paths.RemoveAt(idx);

				// Add the child paths for the ancestral siblings of 'path'
				for (++part_idx; part_idx != parts.Length; ++part_idx)
				{
					foreach (var sib in Shell_.EnumFileSystem(path.Text.Substring(0, p.Length), SearchOption.TopDirectoryOnly).Where(x => x.IsDirectory))
					{
						if (string.CompareOrdinal(parts[part_idx], sib.FileName.ToLowerInvariant()) == 0) continue;
						Paths.AddOrdered(sib.FullPath, Path.Compare);
					}
					p.Append(parts[part_idx]).Append("\\");
				}
			}

			// Update the check boxes
			UpdateTree();
		}

		/// <summary>Update the StateImageIndex for all nodes in the tree</summary>
		private void UpdateTree()
		{
			// Prevent re-entry
			if (m_updating_check_marks != null) return;
			using (Scope.Create(() => m_updating_check_marks = this, () => m_updating_check_marks = null))
			using (Scope.Create(() => m_tree.BeginUpdate(), () => m_tree.EndUpdate()))
			{
				foreach (var node in m_tree.Nodes.Cast<TreeNode>())
					node.TriState = TreeNode.ETriState.Unchecked;

				foreach (var path in Paths)
				{
					var node = (TreeNode)m_tree.GetNode(path.Name);
					node.TriState = TreeNode.ETriState.Checked;
				}
			}
		}
		private object m_updating_check_marks;

		/// <summary>Update the child nodes of 'node' with its subdirectories</summary>
		private void PopulateChildNodes(TreeNode node)
		{
			// Only add child nodes once, to refresh the
			// child nodes call node.Nodes.Clear() first
			if (node.Nodes.Count != 0)
				return;

			node.Nodes.AddRange(GetChildNodes(node).ToArray());
		}

		/// <summary>Return the child nodes for 'node'</summary>
		private static IEnumerable<TreeNode> GetChildNodes(TreeNode node)
		{
			var path = new Path(node.FullPath);
			return Shell_.EnumFileSystem(path.Name, SearchOption.TopDirectoryOnly)
				.Where(fi => fi.IsDirectory)
				.Select(fi => new TreeNode(fi.FileName, node.TriState));
		}


		/// <summary>Subclass TreeView</summary>
		private class TreeView :System.Windows.Forms.TreeView
		{
			public TreeView()
			{
				StateImageList = new ImageList();
				StateImageList.Images.AddStrip(Resources.check_marks);
				CreateHandle(); // So that Expand triggers the event in construction
			}
			protected override void OnKeyDown(KeyEventArgs e)
			{
				if (e.KeyCode == Keys.Space && SelectedNode != null)
					SelectedNode.Checked = !SelectedNode.Checked;
				else
					base.OnKeyDown(e);
			}
			protected override void OnMouseClick(MouseEventArgs e)
			{
				var hit = HitTest(e.X, e.Y);
				if (hit.Location == TreeViewHitTestLocations.StateImage)
					hit.Node.Checked = !hit.Node.Checked;
				else
					base.OnMouseClick(e);
			}
			protected override void OnBeforeExpand(TreeViewCancelEventArgs e)
			{
				base.OnBeforeExpand(e);

				// Get a node array for each child of 'e.Node'
				var nodes = new List<TreeNode[]>();
				ProgressForm.WorkerFunc open_dir = (d,o,p) =>
					{
						foreach (var n in e.Node.Nodes.Cast<TreeNode>())
						{
							if (n.Nodes.Count != 0)
								nodes.Add(null);
							else
							{
								if (d.CancelPending) break;
								p(new ProgressForm.UserState{Description = n.FullPath});
								nodes.Add(GetChildNodes(n).ToArray());
							}
						}
					};
				using (var dlg = new ProgressForm("Opening...", string.Empty, System.Drawing.SystemIcons.Information, ProgressBarStyle.Marquee, open_dir))
				{
					if (dlg.ShowDialog(this, 500) != DialogResult.OK)
					{
						e.Cancel = true;
						return;
					}
					for (int i = 0; i != nodes.Count; ++i)
					{
						if (nodes[i] == null) continue;
						var n = e.Node.Nodes[i];
						n.Nodes.AddRange(nodes[i]);
					}
				}
			}

			/// <summary>Raised whenever a node is expanded or accessed</summary>
			public event EventHandler<NodesNeededEventArgs> NodesNeeded;
			public class NodesNeededEventArgs :EventArgs
			{
				/// <summary>The parent node whose child nodes are needed</summary>
				public TreeNode Node { get; private set; }

				[System.Diagnostics.DebuggerStepThrough] public NodesNeededEventArgs(TreeNode node)
				{
					if (node == null) throw new ArgumentNullException("node");
					Node = node;
				}
			}

			/// <summary>
			/// Return the node corresponding to the given path. Uses the tree's path separator as the delimiter.
			/// 'ignore_case' ignores case differences between the parts of 'fullpath' and the node.Name's.
			/// If 'add_nodes' is true, child nodes are added where necessary.</summary>
			public TreeNode GetNode(string fullpath)
			{
				var parts = fullpath.Split(new[]{PathSeparator}, StringSplitOptions.RemoveEmptyEntries);

				var node = (TreeNode)null;
				var nodes = Nodes;
				foreach (var part in parts)
				{
					if      (part == ".") continue;
					else if (part == ".." && node != null) node = node.Parent;
					else node = nodes.Cast<TreeNode>().FirstOrDefault(x => string.Compare(x.Name, part) == 0);
					if (node == null) throw new Exception($"Node path {fullpath} does not exist");

					if (node.Nodes.Count == 0)
						NodesNeeded.Raise(this, new NodesNeededEventArgs((TreeNode)node));

					nodes = node.Nodes;
				}
				return node;
			}

			/// <summary>
			/// Expands branches of the tree down to 'path'. Uses the tree's path separator as the delimiter.
			/// 'ignore_case' ignores case differences between the parts of 'fullpath' and the node.Name's.</summary>
			public void Expand(string fullpath)
			{
				GetNode(fullpath).EnsureVisible();
			}
		}

		/// <summary>Subclass TreeNode</summary>
		private class TreeNode :System.Windows.Forms.TreeNode
		{
			public TreeNode(string text, ETriState state)
			{
				Name = text.ToLowerInvariant();
				Text = text;
				TriState = state;
			}

			/// <summary>Get the owning tree</summary>
			public new TreeView TreeView
			{
				get { return (TreeView)base.TreeView; }
			}

			/// <summary>Get the parent node</summary>
			public new TreeNode Parent
			{
				get { return (TreeNode)base.Parent; }
			}

			/// <summary>Node check box state</summary>
			public ETriState TriState
			{
				get { return m_tristate; }
				set
				{
					if (m_tristate == value) return;
					SetTriStateInternal(value);

					// Update all parent nodes
					for (var parent = Parent; parent != null; parent = parent.Parent)
					{
						TreeNode.ETriState parent_state = 0;
						parent.Nodes.Cast<TreeNode>().ForEach(x => parent_state |= x.TriState);
						parent.SetTriStateInternal(parent_state);
					}

					// Update all child nodes (recursively)
					if (m_tristate != TreeNode.ETriState.Indeterminate)
					{
						foreach (var c in this.AllNodes(TreeView_.ERecursionOrder.DepthFirst).Cast<TreeNode>())
							c.SetTriStateInternal(m_tristate);
					}
				}
			}
			private void SetTriStateInternal(ETriState state)
			{
				m_tristate      = state;
				Checked         = state == ETriState.Checked;
				StateImageIndex = (int)state - 1;
			}
			private ETriState m_tristate;
			[Flags] public enum ETriState { Unchecked = 1, Checked = 2, Indeterminate = 3 }
		}

		private Button m_btn_ok;
		private TreeView m_tree;
		private ListBox m_listbox;
		private SplitContainer m_split;
		private HoverScroll m_hs;

		#region Windows Form Designer generated code

		/// <summary>Required designer variable.</summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SelectDirectoriesUI));
			this.m_btn_ok = new System.Windows.Forms.Button();
			this.m_tree = new Rylogic.Gui.SelectDirectoriesUI.TreeView();
			this.m_listbox = new ListBox();
			this.m_split = new System.Windows.Forms.SplitContainer();
			((System.ComponentModel.ISupportInitialize)(this.m_split)).BeginInit();
			this.m_split.Panel1.SuspendLayout();
			this.m_split.Panel2.SuspendLayout();
			this.m_split.SuspendLayout();
			this.SuspendLayout();
			// 
			// m_btn_ok
			// 
			this.m_btn_ok.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.m_btn_ok.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.m_btn_ok.Location = new System.Drawing.Point(293, 386);
			this.m_btn_ok.Name = "m_btn_ok";
			this.m_btn_ok.Size = new System.Drawing.Size(75, 23);
			this.m_btn_ok.TabIndex = 0;
			this.m_btn_ok.Text = "OK";
			this.m_btn_ok.UseVisualStyleBackColor = true;
			// 
			// m_tree
			// 
			this.m_tree.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.m_tree.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_tree.Location = new System.Drawing.Point(0, 0);
			this.m_tree.Name = "m_tree";
			this.m_tree.Size = new System.Drawing.Size(195, 368);
			this.m_tree.TabIndex = 2;
			// 
			// m_listbox
			// 
			this.m_listbox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_listbox.FormattingEnabled = true;
			this.m_listbox.IntegralHeight = false;
			this.m_listbox.Location = new System.Drawing.Point(0, 0);
			this.m_listbox.Margin = new System.Windows.Forms.Padding(0);
			this.m_listbox.Name = "m_listbox";
			this.m_listbox.SelectionMode = System.Windows.Forms.SelectionMode.MultiExtended;
			this.m_listbox.Size = new System.Drawing.Size(157, 368);
			this.m_listbox.TabIndex = 3;
			// 
			// m_split
			// 
			this.m_split.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.m_split.Location = new System.Drawing.Point(12, 12);
			this.m_split.Margin = new System.Windows.Forms.Padding(0);
			this.m_split.Name = "m_split";
			// 
			// m_split.Panel1
			// 
			this.m_split.Panel1.Controls.Add(this.m_tree);
			// 
			// m_split.Panel2
			// 
			this.m_split.Panel2.Controls.Add(this.m_listbox);
			this.m_split.Size = new System.Drawing.Size(356, 368);
			this.m_split.SplitterDistance = 195;
			this.m_split.TabIndex = 4;
			// 
			// SelectDirectoriesUI
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(380, 421);
			this.Controls.Add(this.m_split);
			this.Controls.Add(this.m_btn_ok);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.Name = "SelectDirectoriesUI";
			this.Text = "Choose Directories to Search";
			this.m_split.Panel1.ResumeLayout(false);
			this.m_split.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.m_split)).EndInit();
			this.m_split.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion
	}
}
