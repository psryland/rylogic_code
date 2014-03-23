//***************************************************
// Utility Functions
//  Copyright © Rylogic Ltd 2008
//***************************************************

using System;
using System.ComponentModel;
using System.Drawing.Design;
using System.Windows.Forms;
using System.Windows.Forms.Design;
using pr.util;

namespace pr.gui
{
	/// <summary>A Checked list box setup for enum flags</summary>
	public class FlagCheckedListBox :CheckedListBox
	{
		private Type m_enum_type;
		private Enum m_enum_value;
		private bool m_is_updating_check_states;

		public FlagCheckedListBox()
		{
			InitializeComponent();
		}

		// Adds an integer value and its associated description
		public FlagCheckedListBoxItem Add(FlagCheckedListBoxItem item)
		{
			Items.Add(item);
			return item;
		}
		public FlagCheckedListBoxItem Add(int v, string c)
		{
			return Add(new FlagCheckedListBoxItem(v, c));
		}

		protected override void OnItemCheck(ItemCheckEventArgs e)
		{
			base.OnItemCheck(e);
			if (m_is_updating_check_states) return;
			UpdateCheckedItems(Items[e.Index] as FlagCheckedListBoxItem, e.NewValue);
		}

		/// <summary>Owner draw handler for custom drawing of list items</summary>
		public event DrawItemEventHandler OwnerDraw;
		protected override void OnDrawItem(DrawItemEventArgs e)
		{
			if (OwnerDraw == null)
				base.OnDrawItem(e);
			else
				OwnerDraw(this, e);
		}

		/// <summary>Checks / Un-checks items depending on the give bit value</summary>
		protected void UpdateCheckedItems(int value)
		{
			using (Scope.Create(() => m_is_updating_check_states = true, () => m_is_updating_check_states = false))
			{
				for (int i = 0; i != Items.Count; ++i)
				{
					var item = (FlagCheckedListBoxItem)Items[i];
					if (item.Value == 0)
					{
						SetItemChecked(i, value == 0);
					}
					else
					{
						// If the bit for the current item is on in the bit value, check it
						SetItemChecked(i, ((item.Value & value) == item.Value) && item.Value != 0);
					}
				}
			}
		}

		/// <summary>
		/// Updates items in the checked list box
		/// composite = The item that was checked/unchecked
		/// cs = The check state of that item</summary>
		protected void UpdateCheckedItems(FlagCheckedListBoxItem composite, CheckState cs)
		{
			// If the value of the item is 0, call directly.
			if (composite.Value == 0)
				UpdateCheckedItems(0);

			// If the item has been unchecked, remove its bits from the sum, otherwise combine its bits with the sum
			int sum = GetCurrentValue();
			if (cs == CheckState.Unchecked)
				sum &= ~composite.Value;
			else
				sum |= composite.Value;

			// Update all items in the check list box based on the final bit value
			UpdateCheckedItems(sum);
		}

		/// <summary>Gets the current bit value corresponding to all checked items</summary>
		public int GetCurrentValue()
		{
			int sum = 0;
			for (int i = 0; i != Items.Count; ++i)
			{
				if (!GetItemChecked(i)) continue;
				sum |= ((FlagCheckedListBoxItem)Items[i]).Value;
			}
			return sum;
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

		[DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Hidden)]
		public Enum EnumValue
		{
			get { return (Enum)Enum.ToObject(m_enum_type, GetCurrentValue()); }
			set
			{
				Items.Clear();
				m_enum_value = value; // Store the current enum value
				m_enum_type = value.GetType(); // Store enum type
				FillEnumMembers(); // Add items for enum members
				UpdateCheckedItems((int)Convert.ChangeType(m_enum_value, typeof(int))); // Check/uncheck items depending on enum value
			}
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
