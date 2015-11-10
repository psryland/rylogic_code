using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using pr.common;
using pr.util;

namespace pr.extn
{
	public static class ToolStripExtensions
	{
		/// <summary>Add and return an item to this collection</summary>
		public static T Add2<T>(this ToolStripItemCollection items, T item) where T:ToolStripItem
		{
			items.Add(item);
			return item;
		}

		/// <summary>Add and return an menu item to this collection in order defined by 'cmp'</summary>
		public static T AddOrdered<T>(this ToolStripItemCollection items, T item, Comparer<ToolStripMenuItem> cmp) where T:ToolStripMenuItem
		{
			var idx = 0;
			for (idx = 0; idx != items.Count; ++idx)
			{
				var mi = items[idx] as ToolStripMenuItem;
				if (mi != null && cmp.Compare(mi, item) > 0)
					break;
			}

			items.Insert(idx, item);
			return item;
		}

		/// <summary>Add and return a menu item in alphabetical order</summary>
		public static T AddOrdered<T>(this ToolStripItemCollection items, T item) where T:ToolStripMenuItem
		{
			return items.AddOrdered(item, AlphabeticalOrder);
		}

		/// <summary>ToolStripMenuItem comparer for alphabetical order</summary>
		public static readonly Cmp<ToolStripMenuItem> AlphabeticalOrder = Cmp<ToolStripMenuItem>.From((l,r) => string.Compare(l.Text, r.Text, true));

		/// <summary>Exports location data for this tool strip container</summary>
		public static ToolStripLocations SaveLocations(this ToolStripContainer cont)
		{
			using (cont.MarkAsSaving())
				return new ToolStripLocations(cont);
		}

		/// <summary>Imports location data for this tool strip container</summary>
		public static void LoadLocations(this ToolStripContainer cont, ToolStripLocations data)
		{
			using (cont.MarkAsLoading())
				data.Apply(cont);
		}

		/// <summary>Returns the Top,Left,Right,Bottom panels</summary>
		public static IEnumerable<ToolStripPanel> Panels(this ToolStripContainer cont)
		{
			yield return cont.TopToolStripPanel;
			yield return cont.LeftToolStripPanel;
			yield return cont.RightToolStripPanel;
			yield return cont.BottomToolStripPanel;
		}

		/// <summary>Returns all contained ToolStrips within the Top,Left,Right,Bottom panels</summary>
		public static IEnumerable<ToolStrip> ToolStrips(this ToolStripContainer cont)
		{
			foreach (var panel in cont.Panels())
			foreach (var ts in panel.Controls.Cast<ToolStrip>())
				yield return ts;
		}

		/// <summary>Returns all contained toolbar items within the Top,Left,Right,Bottom panels</summary>
		public static IEnumerable<ToolStripItem> ToolStripItems(this ToolStripContainer cont)
		{
			foreach (var ts in cont.ToolStrips())
			foreach (var item in ts.Items.Cast<ToolStripItem>())
				yield return item;
		}

		/// <summary>Recursively search the collection removing any items with a Name property equal to 'key'</summary>
		public static void RemoveByKey(this ToolStripItemCollection cont, string[] keys, bool recursive)
		{
			for (int i = cont.Count; i-- != 0;)
			{
				var item = cont[i];
				if (keys.Contains(item.Name))
					cont.RemoveAt(i);
				else if (item is ToolStripMenuItem && recursive)
					item.As<ToolStripMenuItem>().DropDownItems.RemoveByKey(keys, recursive);
			}
		}
		public static void RemoveByKey(this ToolStripItemCollection cont, string key, bool recursive)
		{
			cont.RemoveByKey(new[]{key}, recursive);
		}

		/// <summary>Remove the tool strip item from it's owner</summary>
		public static void Remove(this ToolStripItem item)
		{
			if (item.Owner == null) return;
			item.Owner.Items.Remove(item);
		}

		/// <summary>A smarter set text that does sensible things with the caret position</summary>
		public static void SetText(this ToolStripComboBox cb, string text)
		{
			var idx = cb.SelectionStart;
			cb.SelectedText = text;
			cb.SelectionStart = idx + text.Length;
		}

		/// <summary>Set the tooltip for this tool strip item</summary>
		public static void ToolTip(this ToolStripItem ctrl, ToolTip tt, string caption)
		{
			// Don't need 'tt', this method is just for consistency with the other overload
			ctrl.ToolTipText = caption;
		}

		/// <summary>Display the hint balloon.</summary>
		public static void ShowHintBalloon(this ToolStripItem item, ToolTip tt, string msg, int duration = 5000)
		{
			var parent = item.GetCurrentParent();
			if (parent == null) return;
			var pt = item.Bounds.Centre();

			tt.SetToolTip(parent, msg);
			tt.Show(msg, parent, pt, duration);
			tt.BeginInvokeDelayed(duration, () => tt.SetToolTip(parent, null));
		}

		/// <summary>Returns the location of this item in screen space</summary>
		public static Point ScreenLocation(this ToolStripItem item)
		{
			var parent = item.GetCurrentParent();
			if (parent == null) return item.Bounds.Location;
			return parent.PointToScreen(item.Bounds.Location);
		}

		/// <summary>Returns the bounds of this item in form space</summary>
		public static Rectangle ParentFormRectangle(this ToolStripItem item)
		{
			var parent = item.GetCurrentParent();
			var top    = parent != null ? parent.TopLevelControl : null;
			var srect  = parent == null ? item.Bounds : parent.RectangleToScreen(item.Bounds);
			return top != null ? top.RectangleToClient(srect) : srect;
		}

		/// <summary>Returns the bounds of this item in screen space</summary>
		public static Rectangle ScreenRectangle(this ToolStripItem item)
		{
			var parent = item.GetCurrentParent();
			if (parent == null) return item.Bounds;
			return parent.RectangleToScreen(item.Bounds);
		}

		/// <summary>
		/// Set a status label with text plus optional colours, detail tooltip, callback click handler, display time.
		/// Calling SetStatusMessage() with no msg resets the priority to the given value (default 0).</summary>
		public static void SetStatusMessage(this ToolStripStatusLabel status,
			string msg = null, string tt = null, bool bold = false, Color? fr_color = null, Color? bk_color = null,
			TimeSpan? display_time = null, EventHandler on_click = null, int priority = 0)
		{
			if (status == null)
				return;

			// Ensure the status has tag data
			if (status.Tag == null)
				new StatusTagData(status);
			else if (!(status.Tag is StatusTagData))
				throw new Exception("Tag property already used for non-status data");

			// Get the status data
			var data = status.Tag.As<StatusTagData>();

			// Ignore the status if it has lower priority than the current
			if (msg.HasValue() && priority < data.m_priority) return;
			data.m_priority = priority;

			// Set the text
			status.Text = msg ?? data.m_idle_msg;

			// Set the tool tip to the detailed message
			status.ToolTipText = tt ?? string.Empty;

			// Set colours
			status.ForeColor = fr_color ?? SystemColors.ControlText;
			status.BackColor = bk_color ?? SystemColors.Control;

			// Hide the status label if it has no value
			status.Visible = msg.HasValue();

			// Choose the font to use
			var font_style = FontStyle.Regular;
			if (bold            ) font_style |= FontStyle.Bold;
			if (on_click != null) font_style |= FontStyle.Underline;
			if (status.Font.Style != font_style)
				status.Font = new Font(status.Font, font_style);

			// If the status message has a timer, dispose it
			// If the status has a display time, set a timer
			Util.Dispose(ref data.m_timer);
			if (display_time != null)
			{
				data.m_timer = new Timer{Enabled = true, Interval = (int)display_time.Value.TotalMilliseconds};
				data.m_timer.Tick += (s,a) =>
					{
						// When the timer fires, if we're still associated with
						// the status message, null out the text and remove ourself
						if (!ReferenceEquals(s, data.m_timer)) return;
						data.m_priority = 0;
						status.SetStatusMessage(data.m_idle_msg);
						Util.Dispose(ref data.m_timer);
					};
			}

			// If a click handler has been provided, subscribe
			data.m_on_click = on_click;
		}

		/// <summary>Set the message to display when the status label has no status to display</summary>
		public static void SetStatusIdleMessage(this ToolStripStatusLabel status, string msg)
		{
			if (status == null)
				return;

			// Ensure the status has tag data
			if (status.Tag == null)
				new StatusTagData(status);
			else if (!(status.Tag is StatusTagData))
				throw new Exception("Tag property already used for non-status data");

			var data = status.Tag.As<StatusTagData>();
			data.m_idle_msg = msg;
		}

		/// <summary>Data added to the 'Tag' of a status label when used by the SetStatus function</summary>
		private class StatusTagData
		{
			public string m_idle_msg;
			public int m_priority;
			public Timer m_timer;
			public EventHandler m_on_click;

			public StatusTagData(ToolStripStatusLabel lbl)
			{
				m_idle_msg = string.Empty;
				m_priority = 0;
				m_timer    = null;
				m_on_click = null;

				if (lbl.Tag is StatusTagData)
					throw new Exception("Status label already has status data");
				if (lbl.Tag != null)
					throw new Exception("Status label Tag property already used for non-status data");

				lbl.Click += HandleStatusClick;
				lbl.MouseEnter += HandleMouseEnter;
				lbl.MouseLeave += HandleMouseLeave;
				lbl.Tag = this;
			}
			public void HandleStatusClick(object sender, EventArgs args)
			{
				m_on_click.Raise(sender, args); // Forward to the user handler
			}
			public void HandleMouseEnter(object sender, EventArgs args)
			{
				var lbl = sender.As<ToolStripStatusLabel>();
				var is_link = m_on_click != null;
				if (is_link) lbl.Owner.Cursor = Cursors.Hand;
			}
			public void HandleMouseLeave(object sender, EventArgs args)
			{
				var lbl = sender.As<ToolStripStatusLabel>();
				lbl.Owner.Cursor = Cursors.Default;
			}
		}

		/// <summary>
		/// Merge the contents of 'rhs' into this menu.
		/// Menus with the same *Name* (not Text) become one menu. Otherwise uses MergeIndex to define the order.
		/// 'choose_rhs' causes the rhs to replace the lhs when items are considered equal</summary>
		public static void Merge(this ToolStrip lhs, ToolStrip rhs, bool choose_rhs, bool permanent = false)
		{
			// Record the layout of 'cont' and 'rhs' if not seen before
			if (!permanent)
			{
				if (!m_ts_layout.ContainsKey(lhs))
					m_ts_layout.Add(lhs, new ToolStripLayout(lhs));
				if (!m_ts_layout.ContainsKey(rhs))
					m_ts_layout.Add(rhs, new ToolStripLayout(rhs));
			}
			DoMerge(lhs, lhs.Items, rhs, rhs.Items, choose_rhs);
		}
		public static void Merge(this ToolStripDropDownItem lhs, ToolStripDropDownItem rhs, bool choose_rhs, bool permanent = false)
		{
			// Record the layout of 'cont' and 'rhs' if not seen before
			if (!permanent)
			{
				if (!m_ts_layout.ContainsKey(lhs))
					m_ts_layout.Add(lhs, new ToolStripLayout(lhs));
				if (!m_ts_layout.ContainsKey(rhs))
					m_ts_layout.Add(rhs, new ToolStripLayout(rhs));
			}
			DoMerge(lhs.Owner, lhs.DropDownItems, rhs.Owner, rhs.DropDownItems, choose_rhs);
		}

		/// <summary>Restore this menu, removing it from any other menus it might be merged into</summary>
		public static void UnMerge(this ToolStrip strip)
		{
			ToolStripLayout layout;
			if (m_ts_layout.TryGetValue(strip, out layout))
			{
				layout.Rebuild();
				m_ts_layout.Remove(strip);
			}
		}
		public static void UnMerge(this ToolStripDropDownItem cont)
		{
			ToolStripLayout layout;
			if (m_ts_layout.TryGetValue(cont, out layout))
			{
				layout.Rebuild();
				m_ts_layout.Remove(cont);
			}
		}

		/// <summary>Keeps a record of dropdown menu layouts</summary>
		private static Dictionary<object, ToolStripLayout> m_ts_layout = new Dictionary<object,ToolStripLayout>();

		/// <summary>Records the layout of a toolstrip</summary>
		internal class ToolStripLayout
		{
			public object m_item;
			public List<ToolStripLayout> m_children;

			public ToolStripLayout(ToolStrip strip)
			{
				m_item = strip;
				m_children = strip.Items.Cast<ToolStripItem>().Select(x => new ToolStripLayout(x)).ToList();
			}
			public ToolStripLayout(ToolStripItem item)
			{
				m_item = item;
				m_children = item is ToolStripDropDownItem
					? item.As<ToolStripDropDownItem>().DropDownItems.Cast<ToolStripItem>().Select(x => new ToolStripLayout(x)).ToList()
					: new List<ToolStripLayout>();
			}
			public override string ToString()
			{
				return m_item is ToolStrip
					? m_item.As<ToolStrip>().Name
					: "{0} idx:{1}".Fmt(m_item.As<ToolStripItem>().Text, m_item.As<ToolStripItem>().MergeIndex);
			}
			/// <summary>Rebuilds 'm_item' to the stored layout</summary>
			public void Rebuild()
			{
				Rebuild(this);
			}
			private static void Rebuild(ToolStripLayout item)
			{
				item.m_children.ForEach(Rebuild);
				var dd = item.m_item as ToolStripDropDownItem;
				if (dd != null)
				{
					dd.DropDownItems.Clear();
					item.m_children.ForEach(c => dd.DropDownItems.Add(c.m_item.As<ToolStripItem>()));
				}
				var ts = item.m_item as ToolStrip;
				if (ts != null)
				{
					ts.Items.Clear();
					item.m_children.ForEach(c => ts.Items.Add(c.m_item.As<ToolStripItem>()));
				}
			}
		}

		/// <summary>
		/// Merge item collections into 'lhs'
		/// When two items are considered equal, 'choose_rhs' causes the item from 'rhs' to
		/// replace the item in 'lhs'. Otherwise the item from 'lhs' is used</summary>
		private static void DoMerge(object lhs_owner, ToolStripItemCollection lhs, object rhs_owner, ToolStripItemCollection rhs, bool choose_rhs)
		{
			if (ReferenceEquals(lhs_owner, rhs_owner))
				throw new Exception("Merge menus failed. Cannot merge menu items belonging to the same menu");
			if (lhs.Cast<ToolStripItem>().Any(x => x.Owner != lhs_owner))
				throw new Exception("Merge menus failed. All items in the collection must belong to {0}".Fmt(lhs_owner.ToString()));
			if (rhs.Cast<ToolStripItem>().Any(x => x.Owner != rhs_owner))
				throw new Exception("Merge menus failed. All items in the collection must belong to {0}".Fmt(rhs_owner.ToString()));

			// Replace the -1 merge indices by their index position
			// In 'lhs', set the merge index of all items (including separators) because
			// we want to preserve the 'lhs' menu as much as possible
			// In 'rhs', don't set the merge index for separators because we use the default
			// index to indicate that the separator should be ignored for merging
			for (int i = 0; i != lhs.Count; ++i)
				if (lhs[i].MergeIndex == -1)
					lhs[i].MergeIndex = i;
			for (int i = 0; i != rhs.Count; ++i)
				if (rhs[i].MergeIndex == -1 && !(rhs[i] is ToolStripSeparator))
					rhs[i].MergeIndex = i;

			// Sort the items in lhs by MergeIndex
			for (bool sorted = false; !sorted;)
			{
				sorted = true;
				for (int i = 1; i < lhs.Count; ++i)
				{
					if (lhs[i].MergeIndex >= lhs[i-1].MergeIndex) continue;
					lhs.Insert(i-1, lhs[i]);
					sorted = false;
					--i;
				}
			}

			// For each item in 'rhs' look for a match in 'lhs'. If found, merge the item.
			// If not found, insert the item based on MergeIndex.
			var dst = lhs.Cast<ToolStripItem>().ToList();
			var src = rhs.Cast<ToolStripItem>().ToList();
			foreach (var r in src)
			{
				// Ignore separators without a name or merge index
				var sep = r as ToolStripSeparator;
				if (sep != null && !sep.Name.HasValue() && sep.MergeIndex == -1)
					continue;

				// Look for a match in 'lhs'
				var l = r.Name.HasValue()
					? dst.FirstOrDefault(x => x.Name == r.Name)
					: dst.FirstOrDefault(x => x.Text == r.Text);

				// Merge 'l' and 'r'
				if (l != null)
				{
					var ldd = l as ToolStripDropDownItem;
					var rdd = r as ToolStripDropDownItem;

					// If one menu replaces the other, remove all items from the replacee
					if (l.MergeAction == MergeAction.Replace)
						rdd.DropDownItems.Clear();
					if (r.MergeAction == MergeAction.Replace)
						ldd.DropDownItems.Clear();

					// If one menu is marked with remove, then neither menu is added
					if (l.MergeAction == MergeAction.Remove || r.MergeAction == MergeAction.Remove)
					{
						var idx = l.Name.HasValue()
							? lhs.Cast<ToolStripItem>().IndexOf(x => x.Name == r.Name)
							: lhs.Cast<ToolStripItem>().IndexOf(x => x.Text == r.Text);
						if (idx != -1) lhs.RemoveAt(idx);
					}

					// Merge 'r' into 'l', then keep 'l'
					else if (!choose_rhs)
					{
						if (ldd != null && rdd != null && rdd.DropDownItems.Count != 0)
							DoMerge(ldd.DropDown, ldd.DropDownItems, rdd.DropDown, rdd.DropDownItems, false);
					}

					// Otherwise, merge 'l' into 'r', then keep 'r'
					else
					{
						if (ldd != null && rdd != null && ldd.DropDownItems.Count != 0)
							DoMerge(rdd.DropDown, rdd.DropDownItems, ldd.DropDown, ldd.DropDownItems, false);

						// If 'rhs' has more than one item with the same name, it's possible that the first
						// occurrence was merged and replace the item in 'lhs'. The second item needs to replace
						// the first item in lhs. We can't use lhs.IndexOf() because 'l' may no longer be in 'lhs'.
						var idx = l.Name.HasValue()
							? lhs.Cast<ToolStripItem>().IndexOf(x => x.Name == r.Name)
							: lhs.Cast<ToolStripItem>().IndexOf(x => x.Text == r.Text);
						lhs.RemoveAt(idx);
						lhs.Insert(idx, r);
					}
				}

				// No match found, merge using MergeIndex
				else
				{
					var idx = (r.MergeIndex != -1 ? r.MergeIndex : lhs.Count);
					int ins; for (ins = 0; ins != lhs.Count && lhs[ins].MergeIndex < idx; ++ins) {}
					lhs.Insert(ins, r);
				}
			}
		}
	}

	/// <summary>Used to persist control locations and sizes in xml</summary>
	public class ToolStripLocations
	{
		private static class Tag
		{
			public const string Name     = "name";
			public const string Top      = "top";
			public const string Left     = "left";
			public const string Right    = "right";
			public const string Bottom   = "bottom";
		}
		private string m_name;
		private ControlLocations m_top;
		private ControlLocations m_left;
		private ControlLocations m_right;
		private ControlLocations m_bottom;

		public ToolStripLocations()
		{
			m_name      = string.Empty;
			m_top       = new ControlLocations();
			m_left      = new ControlLocations();
			m_right     = new ControlLocations();
			m_bottom    = new ControlLocations();
		}
		public ToolStripLocations(ToolStripContainer cont)
		{
			Read(cont);
		}
		public ToolStripLocations(XElement node)
		{
			m_name      = node.Element(Tag.Name    ).As<string>();
			m_top       = node.Element(Tag.Top     ).As<ControlLocations>();
			m_left      = node.Element(Tag.Left    ).As<ControlLocations>();
			m_right     = node.Element(Tag.Right   ).As<ControlLocations>();
			m_bottom    = node.Element(Tag.Bottom  ).As<ControlLocations>();
		}
		public XElement ToXml(XElement node)
		{
			node.Add
			(
				m_name  .ToXml(Tag.Name    ,false),
				m_top   .ToXml(Tag.Top     ,false),
				m_left  .ToXml(Tag.Left    ,false),
				m_right .ToXml(Tag.Right   ,false),
				m_bottom.ToXml(Tag.Bottom  ,false)
			);
			return node;
		}
		public void Read(ToolStripContainer cont)
		{
			m_name      = cont.Name;
			m_top       = new ControlLocations(cont.TopToolStripPanel   );
			m_left      = new ControlLocations(cont.LeftToolStripPanel  );
			m_right     = new ControlLocations(cont.RightToolStripPanel );
			m_bottom    = new ControlLocations(cont.BottomToolStripPanel);
		}
		public void Apply(ToolStripContainer cont, bool layout_on_resume = true)
		{
			// If these locations are for a different container, don't apply.
			if (m_name != cont.Name)
			{
				System.Diagnostics.Debug.WriteLine("ToolStripContainer locations ignored due to name mismatch.\nToolStripContainer Name {0} != Layout Data Name {1}".Fmt(cont.Name, m_name));
				return;
			}

			// Apply the layout to each panel
			m_top   .Apply(cont.TopToolStripPanel   );
			m_left  .Apply(cont.LeftToolStripPanel  );
			m_right .Apply(cont.RightToolStripPanel );
			m_bottom.Apply(cont.BottomToolStripPanel);
		}
	}
}

#if PR_UNITTESTS
namespace pr.unittests
{
	using extn;

	[TestFixture] public class TestTSExtns
	{
		[Test] public void MergeMenus()
		{
			var m0 = new MenuStrip{Name = "m0", Text = "m0"};
			{
				var i0 = m0.Items.Add2(new ToolStripMenuItem{Name = "i0", Text = "i0", MergeIndex = 10});
				var i1 = m0.Items.Add2(new ToolStripMenuItem{Name = "i1", Text = "i1", MergeIndex = 20});
				var i2 = m0.Items.Add2(new ToolStripMenuItem{Name = "i2", Text = "i2", MergeIndex = 30});
				var i10 = i1.DropDownItems.Add2(new ToolStripMenuItem{Name = "i10", Text = "i10", MergeIndex = 10});
				var i12 = i1.DropDownItems.Add2(new ToolStripMenuItem{Name = "i12", Text = "i12", MergeIndex = 20});
			}
			var m1 = new MenuStrip{Name = "m1", Text = "m1"};
			{
				var i1 = m1.Items.Add2(new ToolStripMenuItem{Name = "i1", Text = "i1", MergeIndex = 15});
				var i3 = m1.Items.Add2(new ToolStripMenuItem{Name = "i3", Text = "i3", MergeIndex = 35});
				var i10 = i1.DropDownItems.Add2(new ToolStripMenuItem{Name = "i10", Text = "i10", MergeIndex = 15});
				var i11 = i1.DropDownItems.Add2(new ToolStripMenuItem{Name = "i11", Text = "i11", MergeIndex = 15});
			}

			m0.Merge(m1, false);
			Assert.True(m0.Text == "m0");
			Assert.True(m0.Items.Count == 4);
			Assert.True(m0.Items[0].Text == "i0");
			Assert.True(m0.Items[1].Text == "i1");
			Assert.True(m0.Items[2].Text == "i2");
			Assert.True(m0.Items[3].Text == "i3");
			Assert.True(m0.Items[1].As<ToolStripMenuItem>().DropDownItems.Count == 3);
			Assert.True(m0.Items[1].As<ToolStripMenuItem>().DropDownItems[0].Text == "i10");
			Assert.True(m0.Items[1].As<ToolStripMenuItem>().DropDownItems[1].Text == "i11");
			Assert.True(m0.Items[1].As<ToolStripMenuItem>().DropDownItems[2].Text == "i12");

			m0.UnMerge();
			Assert.True(m0.Text == "m0");
			Assert.True(m0.Items.Count == 3);
			Assert.True(m0.Items[0].Text == "i0");
			Assert.True(m0.Items[1].Text == "i1");
			Assert.True(m0.Items[2].Text == "i2");
			Assert.True(m0.Items[1].As<ToolStripMenuItem>().DropDownItems.Count == 2);
			Assert.True(m0.Items[1].As<ToolStripMenuItem>().DropDownItems[0].Text == "i10");
			Assert.True(m0.Items[1].As<ToolStripMenuItem>().DropDownItems[1].Text == "i12");
		}
	}
}
#endif