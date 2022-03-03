//***************************************************
// Utility Functions
//  Copyright (c) Rylogic Ltd 2008
//***************************************************

using System;
using System.ComponentModel;
using System.Drawing.Design;
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace Rylogic.Gui.WinForms
{
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
