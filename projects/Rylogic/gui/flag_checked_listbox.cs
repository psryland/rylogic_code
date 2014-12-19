//***************************************************
// Utility Functions
//  Copyright (c) Rylogic Ltd 2008
//***************************************************

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Design;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using pr.extn;
using pr.util;

namespace pr.gui
{
	/// <summary>A Checked list box setup for enum flags</summary>
	public class FlagCheckedListBox :CheckedListBox
	{
		private Type m_enum_type;
		private Enum m_enum_value;
		private bool m_is_updating_check_states;
		private List<ItemCheckEventArgs> m_pending_itemcheck_events;

		public FlagCheckedListBox()
		{
			m_pending_itemcheck_events = new List<ItemCheckEventArgs>();
			InitializeComponent();
		}

		/// <summary>
		/// Adds a bitmask value and its associated description.
		/// The bitmask can contain more than one set bit.</summary>
		public FlagCheckedListBoxItem Add(int value, string description)
		{
			return Add(new FlagCheckedListBoxItem(value, description));
		}
		public FlagCheckedListBoxItem Add(FlagCheckedListBoxItem item)
		{
			Items.Add(item);
			return item;
		}

		/// <summary>Handle items in the list being checked or unchecked</summary>
		protected override void OnItemCheck(ItemCheckEventArgs e)
		{
			// While 'UpdateCheckedItems' is in progress, we need to buffer the
			// 'ItemCheck' events and only raise them once all items have been changed.
			// This is because clients that handle ItemCheck will want to read the 
			// Bitmask/EnumValue which we want to be correct
			if (m_is_updating_check_states)
				m_pending_itemcheck_events.Add(e);
			else
				UpdateCheckedItems(Items[e.Index].As<FlagCheckedListBoxItem>(), e.NewValue);
		}

		/// <summary>
		/// Updates items in the checked list box
		/// item = The item that was checked/unchecked (can represent more than 1 bit)
		/// cs = The check state of that item</summary>
		protected void UpdateCheckedItems(FlagCheckedListBoxItem item, CheckState cs)
		{
			// If the value of the item is 0, uncheck all non-zero items
			if (item.Value == 0)
				UpdateCheckedItems(cs == CheckState.Checked ? 0 : Items.Cast<FlagCheckedListBoxItem>().ForEach(0, (x,i) => i |= x.Value));
			else
				UpdateCheckedItems(cs == CheckState.Checked ? (Bitmask | item.Value) : (Bitmask & ~item.Value));
		}

		/// <summary>Checks/Unchecks items depending on the given bit mask</summary>
		protected void UpdateCheckedItems(int bitmask)
		{
			// See comments in OnItemCheck
			// Update all the check states, buffering the events, then raise the events at the end
			if (m_is_updating_check_states) return;
			using (Scope.Create(() => m_is_updating_check_states = true, () => m_is_updating_check_states = false))
			{
				for (int i = 0; i != Items.Count; ++i)
				{
					var item = (FlagCheckedListBoxItem)Items[i];
					if (item.Value == 0)
					{
						SetItemChecked(i, bitmask == 0);
					}
					else
					{
						// If the bit for the current item is on in the bit value, check it
						var check = (item.Value & bitmask) == item.Value;
						SetItemChecked(i, check);
					}
				}
			}

			// Raise buffered ItemCheck events
			foreach (var e in m_pending_itemcheck_events)
				base.OnItemCheck(e);

			m_pending_itemcheck_events.Clear();
			Invalidate();
		}

		/// <summary>Gets the current bit value corresponding to all checked items</summary>
		public int Bitmask
		{
			get
			{
				int mask = 0;
				for (int i = 0; i != Items.Count; ++i)
				{
					if (!GetItemChecked(i)) continue;
					mask |= Items[i].As<FlagCheckedListBoxItem>().Value;
				}
				return mask;
			}
		}

		/// <summary>Adds items to the check list box based on the members of the enum</summary>
		private void FillEnumMembers()
		{
			foreach (var name in Enum.GetNames(m_enum_type))
			{
				object val = Enum.Parse(m_enum_type, name);
				int intVal = (int)Convert.ChangeType(val, typeof(int));
				Add(intVal, name);
			}
		}

		/// <summary>Set an enum value that implicitly creates the members of the checklist</summary>
		[DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Hidden)]
		public Enum EnumValue
		{
			get { return (Enum)Enum.ToObject(m_enum_type, Bitmask); }
			set
			{
				if (Equals(m_enum_value,value) && Equals(m_enum_type,value.GetType())) return;
				Items.Clear();
				m_enum_value = value; // Store the current enum value
				m_enum_type = value.GetType(); // Store enum type
				FillEnumMembers(); // Add items for enum members
				UpdateCheckedItems((int)Convert.ChangeType(m_enum_value, typeof(int))); // Check/uncheck items depending on enum value
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

		#region Component Designer generated code

		private Container components = null;

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (components != null)
					components.Dispose();
			}
			base.Dispose(disposing);
		}

		private void InitializeComponent()
		{
			//
			// FlaggedCheckedListBox
			//
			this.CheckOnClick = true;
		}
		#endregion
	}

	/// <summary>Represents an item in the check list box</summary>
	public class FlagCheckedListBoxItem
	{
		public int Value { get; set; }
		public string Caption { get; set; }

		public FlagCheckedListBoxItem(int v, string c) { Value = v; Caption = c; }
		public override string ToString() { return Caption; }

		/// <summary>True if the value corresponds to a single bit being set</summary>
		public bool IsFlag { get { return (Value & (Value - 1)) == 0; } }

		// Returns true if this value is a member of the composite bit value
		public bool IsMemberFlag(FlagCheckedListBoxItem composite) { return IsFlag && (Value & composite.Value) == Value; }
	}

	/// <summary>UITypeEditor for flag enums</summary>
	public class FlagEnumUIEditor :UITypeEditor
	{
		private readonly FlagCheckedListBox m_fclb;

		public FlagEnumUIEditor()
		{
			m_fclb = new FlagCheckedListBox();
			m_fclb.BorderStyle = BorderStyle.None;
		}

		public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
		{
			var edSvc = (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));
			Enum e = (Enum)Convert.ChangeType(value, context.PropertyDescriptor.PropertyType);
			m_fclb.EnumValue = e;
			edSvc.DropDownControl(m_fclb);
			return m_fclb.EnumValue;
		}

		public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
		{
			return UITypeEditorEditStyle.DropDown;
		}
	}
}
