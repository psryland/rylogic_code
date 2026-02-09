using System;
using System.Linq;
using System.Windows.Forms;
using Rylogic.Utility;

namespace Rylogic.Gui.WinForms
{
	public class FeatureTree :TreeView
	{
		[Flags] public enum ETriState
		{
			Unchecked = 1,
			Checked = 2,
			Indeterminate = 3,
		}

		public FeatureTree()
		{
			ImageList = new ImageList();
			ImageList.Images.AddStrip(Resources.check_marks);
		}
		protected override void OnKeyDown(KeyEventArgs e)
		{
			base.OnKeyDown(e);
			if (e.KeyCode == Keys.Space && SelectedNode != null)
			{
				var node = SelectedNode as FeatureTreeNode;
				node.TriState = NextState(node.TriState);
			}
		}
		protected override void OnMouseClick(MouseEventArgs e)
		{
			base.OnMouseClick(e);
			var hit = HitTest(e.X, e.Y);
			if (hit.Location == TreeViewHitTestLocations.Image)
			{
				var node = hit.Node as FeatureTreeNode;
				node.TriState = NextState(node.TriState);
			}
		}
		protected override void OnAfterCheck(TreeViewEventArgs args)
		{
			// Block the 'AfterCheck' events that occur as a result of the 'UpdateCheckStates' function
			if (m_in_after_check != 0) return;
			using (Scope.Create(() => ++m_in_after_check, () => --m_in_after_check))
			{
				// Set the permission on the selected feature
				var node = (FeatureTreeNode)args.Node;
				node.Feature.Allowed = node.TriState != ETriState.Unchecked;

				// Apply the permission to all child features
				foreach (var c in node.Feature.Children)
					c.Allowed = node.Feature.Allowed;

				// If allowed, set the allowed permission on all ancestors
				for (var p = node.Parent; p != null; p = p.Parent)
					((FeatureTreeNode)p).Feature.Allowed |= node.Feature.Allowed;

				// Update the tree control
				UpdateCheckStates(Nodes);
				Invalidate();
			}
			base.OnAfterCheck(args);
		}
		private int m_in_after_check;

		/// <summary>The root tree item</summary>
		public IFeatureTreeItem Root
		{
			get;
			set
			{
				if (field == value) return;
				field = value;
				PopulateFeatureTree();
			}
		}

		/// <summary>Include the root feature in the tree</summary>
		public bool ShowRoot
		{
			get;
			set
			{
				if (field == value) return;
				field = value;
				PopulateFeatureTree();
			}
		}

		/// <summary>Populate the tree control from the root features</summary>
		public void PopulateFeatureTree()
		{
			Nodes.Clear();
			if (Root == null)
				return;

			// Recursive lambda for building a node tree
			Func<IFeatureTreeItem, TreeNode> make_node = null;
			make_node = feat =>
			{
				var node = new FeatureTreeNode(feat.Name, feat);
				foreach (var child in feat.Children)
					node.Nodes.Add(make_node(child));
				return node;
			};

			// Create the node tree
			if (ShowRoot)
				Nodes.Add(make_node(Root));
			else
				foreach (var feat in Root.Children)
					Nodes.Add(make_node(feat));

			// Apply the check states
			UpdateCheckStates();
		}

		/// <summary>Recursively set the allowed state of all features in the tree</summary>
		public void CheckAll(bool state)
		{
			// Recursive lambda for setting the check state
			Action<IFeatureTreeItem, bool> check_node = null;
			check_node = (feat,allowed) =>
			{
				feat.Allowed = allowed;
				foreach (var ch in feat.Children)
					check_node(ch, allowed);
			};
			check_node(Root, state);

			// Update the tree control
			UpdateCheckStates();
			Invalidate();
		}

		/// <summary>Recursively set the check states on the tree nodes</summary>
		public void UpdateCheckStates()
		{
			UpdateCheckStates(Nodes);
		}
		private ETriState UpdateCheckStates(TreeNodeCollection nodes)
		{
			var group_state = ETriState.Unchecked;
			foreach (var node in nodes.Cast<FeatureTreeNode>())
			{
				// The state of the node is checked if it itself is checked, indeterminate if
				// any of it's children are checked, or unchecked if neither it or its children are checked.
				var state    = node.Feature.Allowed ? ETriState.Checked : ETriState.Unchecked;
				var children = node.Nodes.Count != 0 ? UpdateCheckStates(node.Nodes) : ETriState.Unchecked;
				node.TriState = state == ETriState.Checked ? state : children == ETriState.Unchecked ? children : ETriState.Indeterminate;

				group_state = group_state | state | children;
			}
			return group_state;
		}

		/// <summary>Cycle through the tri-states</summary>
		private static ETriState NextState(ETriState state)
		{
			switch (state)
			{
			default: throw new Exception("Unknown tri state");
			case ETriState.Unchecked: return ETriState.Checked;
			case ETriState.Checked: return ETriState.Unchecked;
			case ETriState.Indeterminate: return ETriState.Checked;
			}
		}
	}

	public class FeatureTreeNode :TreeNode
	{
		// Notes:
		// - Ignore the state of 'Checked', I'm just using it to cycle the 'TriState' property

		public FeatureTreeNode(string text, IFeatureTreeItem feature)
			: base(text)
		{
			Feature = feature;
		}

		/// <summary>The feature this node represents</summary>
		public IFeatureTreeItem Feature
		{
			get;
			private set;
		}

		/// <summary>The checked state of the node</summary>
		public FeatureTree.ETriState TriState
		{
			get;
			set
			{
				field = value;
				ImageIndex = (int)field - 1;
				SelectedImageIndex = (int)field - 1;
				Checked = TriState == FeatureTree.ETriState.Checked || TriState == FeatureTree.ETriState.Indeterminate;
			}
		}

		/// <summary>Hide the 'Checked' property, use 'TriState' instead</summary>
		protected new bool Checked
		{
			private get { return base.Checked; }
			set { base.Checked = value; }
		}
	}
}
