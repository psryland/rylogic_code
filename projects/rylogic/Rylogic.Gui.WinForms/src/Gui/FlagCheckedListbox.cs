//***************************************************
// Utility Functions
//  Copyright (c) Rylogic Ltd 2008
//***************************************************

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using Rylogic.Attrib;
using Rylogic.Extn;
using Rylogic.Utility;

namespace Rylogic.Gui.WinForms
{
	/// <summary>A Checked list box set up for enum flags</summary>
	public class FlagCheckedListBox :CheckedListBox
	{
		public FlagCheckedListBox()
		{
			m_pending_itemcheck_events = new List<ItemCheckEventArgs>();
			RequireDescAttribute = false;
		}

		/// <summary>Set to true to only display enum member that have the 'Desc' attribute</summary>
		public bool RequireDescAttribute
		{
			get;
			set
			{
				if (field == value) return;
				field = value;
				if (EnumValue != null)
					FillEnumMembers();
			}
		}

		/// <summary>Gets the current bit value corresponding to all checked items</summary>
		public int Bitmask
		{
			get
			{
				int mask = 0;
				for (int i = 0; i != Items.Count; ++i)
				{
					// Allow indeterminate to used as disabled
					if (GetItemCheckState(i) != CheckState.Checked) continue;
					mask |= ((FlagCheckedListBoxItem)Items[i]).Value;
				}
				return mask;
			}
		}

		/// <summary>Get/Set an enum value that implicitly creates the members of the check list</summary>
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public Enum EnumValue
		{
			get { return m_enum_type != null ? (Enum)Enum.ToObject(m_enum_type, Bitmask) : null; }
			set
			{
				if (Equals(m_enum_value,value) && Equals(m_enum_type,value.GetType())) return;
				if (value != null)
				{
					m_enum_value = value; // Store the current enum value
					m_enum_type = value.GetType(); // Store enum type
					FillEnumMembers(); // Add items for enum members
					UpdateCheckedItems((int)Convert.ChangeType(m_enum_value, typeof(int))); // Check/uncheck items depending on enum value
				}
				else
				{
					m_enum_value = null;
					m_enum_type = null;
					Items.Clear();
				}
			}
		}
		private Enum m_enum_value;
		private Type m_enum_type;

		/// <summary>
		/// Adds a bitmask value and its associated description.
		/// The bitmask can contain more than one set bit.</summary>
		public FlagCheckedListBoxItem Add(int value, string description)
		{
			return Add(new FlagCheckedListBoxItem(this, value, description));
		}
		public FlagCheckedListBoxItem Add(FlagCheckedListBoxItem item)
		{
			Items.Add(item);
			return item;
		}

		/// <summary>Handle items in the list being checked or unchecked</summary>
		protected override void OnItemCheck(ItemCheckEventArgs e)
		{
			var item = (FlagCheckedListBoxItem)Items[e.Index];
			e.NewValue = item.Enabled ? e.NewValue : CheckState.Indeterminate;

			// While 'UpdateCheckedItems' is in progress, we need to buffer the
			// 'ItemCheck' events and only raise them once all items have been changed.
			// This is because clients that handle ItemCheck will want to read the 
			// Bitmask/EnumValue which we want to be correct
			if (m_is_updating_check_states)
				m_pending_itemcheck_events.Add(e);
			else
				UpdateCheckedItems(item, e.NewValue);
		}

		/// <summary>Get the first flag item where 'item.Value & mask' == mask</summary>
		public FlagCheckedListBoxItem GetItem(int mask)
		{
			return Items.OfType<FlagCheckedListBoxItem>().FirstOrDefault(x => (x.Value & mask) == mask);
		}

		/// <summary>
		/// Updates items in the checked list box
		/// 'item' = The item that was checked/unchecked (can represent more than 1 bit)
		/// 'cs' = The check state of that item</summary>
		protected void UpdateCheckedItems(FlagCheckedListBoxItem item, CheckState cs)
		{
			// If the value of the item is 0, uncheck all non-zero items
			if (item.Value == 0)
				UpdateCheckedItems(cs == CheckState.Checked ? 0 : Items.Cast<FlagCheckedListBoxItem>().ForEach(0, (x,i) => i |= x.Value));
			else
				UpdateCheckedItems(cs == CheckState.Checked ? (Bitmask | item.Value) : (Bitmask & ~item.Value));
		}

		/// <summary>Checks/Un-checks items depending on the given bit mask</summary>
		protected void UpdateCheckedItems(int bitmask)
		{
			// See comments in OnItemCheck
			// Update all the check states, buffering the events, then raise the events at the end
			if (m_is_updating_check_states) return;
			using (Scope.Create(() => m_is_updating_check_states = true, () => m_is_updating_check_states = false))
			{
				for (int i = 0; i != Items.Count; ++i)
				{
					// Note: Indeterminate state only works when CheckOnClick is false
					var item = (FlagCheckedListBoxItem)Items[i];
					if (!item.Enabled)
					{
						// If the item is disabled, show it's state as indeterminate
						SetItemCheckState(i, CheckState.Indeterminate);
					}
					else if (item.Value == 0)
					{
						// If the item value is zero, check when the bitmask is zero
						SetItemCheckState(i, bitmask == 0 ? CheckState.Checked : CheckState.Unchecked);
					}
					else
					{
						// If the bit for the current item is on in the bit value, check it
						var check = (item.Value & bitmask) == item.Value;
						SetItemCheckState(i, check ? CheckState.Checked : CheckState.Unchecked);
					}
				}
			}

			// Raise buffered ItemCheck events
			foreach (var e in m_pending_itemcheck_events)
				base.OnItemCheck(e);

			m_pending_itemcheck_events.Clear();
			Invalidate();
		}
		private List<ItemCheckEventArgs> m_pending_itemcheck_events;
		private bool m_is_updating_check_states;

		/// <summary>Adds items to the check list box based on the members of the enum</summary>
		private void FillEnumMembers()
		{
			Items.Clear();
			foreach (var val in Enum.GetValues(m_enum_type))
			{
				var name = Enum.GetName(m_enum_type, val);
				var desc = DescAttr.Desc(m_enum_type, name);
				int bits = (int)Convert.ChangeType(val, typeof(int));
				if (RequireDescAttribute && desc == null) continue;
				Add(bits, desc ?? name);
			}
		}

		/// <summary>Owner draw handler for custom drawing of list items</summary>
		public new event DrawItemEventHandler DrawItem;
		protected override void OnDrawItem(DrawItemEventArgs e)
		{
			if (DrawItem == null)
				base.OnDrawItem(e);
			else
				DrawItem(this, e);
		}

		/// <summary>Raised whenever the mouse moves over an item in the list</summary>
		public event EventHandler<HoveredItemEventArgs> HoveredItemChanged;
		protected virtual void OnHoveredItemChanged(HoveredItemEventArgs args)
		{
			HoveredItemChanged?.Invoke(this, args);
		}
		public class HoveredItemEventArgs :EventArgs
		{
			public HoveredItemEventArgs(FlagCheckedListBoxItem item)
			{
				Item = item;
			}

			/// <summary>The Item under the mouse</summary>
			public FlagCheckedListBoxItem Item { get; private set; }
		}

		/// <summary>Use mouse move to update the hovered item</summary>
		protected override void OnMouseMove(MouseEventArgs e)
		{
			base.OnMouseMove(e);
			var hovered_item = HoveredItem();
			if (m_last_hovered_item != hovered_item)
			{
				m_last_hovered_item = hovered_item;
				OnHoveredItemChanged(new HoveredItemEventArgs(hovered_item));
			}
		}

		/// <summary>Returns the item currently under the mouse</summary>
		private FlagCheckedListBoxItem HoveredItem()
		{
			var pos = PointToClient(MousePosition);
			int idx = IndexFromPoint(pos);
			return idx != -1 ? Items[idx] as FlagCheckedListBoxItem : null;
		}
		private FlagCheckedListBoxItem m_last_hovered_item;
	}

	/// <summary>Represents an item in the check list box</summary>
	public class FlagCheckedListBoxItem
	{
		internal FlagCheckedListBoxItem(FlagCheckedListBox owner, int value, string caption, bool enabled = true)
		{
			Owner   = owner; 
			Value   = value;
			Caption = caption;
			Enabled = enabled;
		}

		public FlagCheckedListBox Owner { get; private set; }

		/// <summary>The bitmask value of this flag</summary>
		public int Value
		{
			get;
			set
			{
				if (field == value) return;
				field = value;
				Owner.Invalidate();
			}
		}

		/// <summary>The string name corresponding to the flag</summary>
		public string Caption
		{
			get;
			set
			{
				if (field == value) return;
				field = value;
				Owner.Invalidate();
			}
		}

		/// <summary>True if the flag can be set or cleared via the UI</summary>
		public bool Enabled
		{
			get;
			set
			{
				if (field == value) return;
				field = value;
				var idx = Owner.Items.IndexOf(this);
				if (idx != -1) Owner.SetItemCheckState(idx, CheckState.Indeterminate);
				Owner.Invalidate();
			}
		}

		/// <summary>True if the value corresponds to a single bit being set</summary>
		public bool IsFlag
		{
			get { return (Value & (Value - 1)) == 0; }
		}

		// Returns true if this value is a member of the composite bit value
		public bool IsMemberFlag(FlagCheckedListBoxItem composite)
		{
			return IsFlag && (Value & composite.Value) == Value;
		}

		public override string ToString()
		{
			return Caption;
		}
	}
}
