/*
	Copyright (c) 2011 - 2012 Trogu Antonio Davide

	This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU Lesser General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU Lesser General Public License for more details.

    You should have received a copy of the GNU Lesser General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

module dgui.core.windowclass;

import std.utf: toUTFz;
import dgui.core.charset;
import dgui.core.winapi;
import dgui.core.exception;
import dgui.core.utils;
import dgui.canvas;

enum
{
	// Windows Classes
	WC_BUTTON 		= "Button",
	WC_COMBOBOXEX 	= "ComboBoxEx32",
	WC_LISTBOX 		= "ListBox",
	WC_LISTVIEW 	= "SysListView32",
	WC_PROGRESSBAR  = "msctls_progress32",
	WC_RICHEDIT 	= "RichEdit20A",
	WC_STATUSBAR 	= "msctls_statusbar32",
	WC_TABCONTROL   = "SysTabControl32",
	WC_EDIT			= "EDIT",
	WC_TOOLBAR 		= "ToolBarWindow32",
	WC_TRACKBAR		= "msctls_trackbar32",
	WC_TOOLTIP      = "tooltips_class32",
	WC_TREEVIEW 	= "SysTreeView32",
	//WC_STATIC 		= "STATIC",
	
	// DGui Classes
	WC_DPANEL		= "DPanel",
	WC_FORM 		= "DForm",
	WC_DBUTTON 		= "DButton",
	WC_DCHECKBOX 	= "DCheckBox",
	WC_DRADIOBUTTON = "DRadioButton",
	WC_DCOMBOBOX 	= "DComboBox",
	WC_DLABEL 		= "DLabel",
	WC_DLISTBOX 	= "DListBox",
	WC_DPICTUREBOX  = "DPicturebox",
	WC_DLISTVIEW 	= "DListView",
	WC_DPROGRESSBAR = "DProgressBar",
	WC_DRICHEDIT 	= "DRichTextBox",
	WC_DSTATUSBAR 	= "DStatusBar",
	WC_DTABCONTROL  = "DTabControl",
	WC_DEDIT 		= "DTextBox",
	WC_DTOOLBAR 	= "DToolBar",
	WC_DTRACKBAR 	= "DTrackBar",
	WC_DTOOLTIP     = "DToolTip",
	WC_DTREEVIEW 	= "DTreeView",
	WC_DGRIDPANEL   = "DGridPanel",
	WC_DSPLITPANEL  = "DSplitPanel",
}

enum ClassStyles: uint 
{
	NONE 			= 0x00000000,
	VREDRAW			= 0x00000001,
	HREDRAW			= 0x00000002,
	KEYCVTWINDOW	= 0x00000004,
	DBLCLKS			= 0x00000008,
	OWNDC			= 0x00000020,
	CLASSDC			= 0x00000040,
	PARENTDC		= 0x00000080,
	NOKEYCVT		= 0x00000100,
	NOCLOSE			= 0x00000200,
	SAVEBITS		= 0x00000800,
	BYTEALIGNCLIENT	= 0x00001000,
	BYTEALIGNWINDOW	= 0x00002000,
	GLOBALCLASS		= 0x00004000,
	IME				= 0x00010000,
}

final class WindowClass
{
	public static void register(string className, ClassStyles classStyle, Cursor cursor, WNDPROC wndProc)
	{
		WNDCLASSEXW wc;
		wc.cbSize = WNDCLASSEXW.sizeof;

		if(!getClassInfoEx(className, &wc))
		{			
			if(!registerClassEx(className, cursor ? cursor.handle : SystemCursors.arrow.handle, null, wndProc, classStyle))
			{
				throwException!(Win32Exception)("Windows Class '%s' not created", className);
			}
		}
	}	
	
	public static WNDPROC superclass(string oldClassName, string newClassName, WNDPROC newWndProc)
	{
		WNDCLASSEXW oldWc = void, newWc = void;

		oldWc.cbSize = WNDCLASSEXW.sizeof;
		newWc.cbSize = WNDCLASSEXW.sizeof;
		
		const(wchar)* pNewClassName = toUTFz!(const(wchar)*)(newClassName);
		getClassInfoEx(oldClassName, &oldWc);

		if(!getClassInfoEx(newClassName, &newWc)) // IF Class Non Found THEN
		{
			newWc = oldWc;
			newWc.style &= ~ClassStyles.GLOBALCLASS; // Remove Global Class
			
			newWc.lpfnWndProc = newWndProc;
			newWc.lpszClassName = pNewClassName;
			newWc.hInstance = getHInstance();
			//newWc.hbrBackground = null;

			if(!registerClassEx(&newWc))
			{
				throwException!(Win32Exception)("Windows Class '%s' not created", newClassName);
			}
		}

		return oldWc.lpfnWndProc; //Back to the original window procedure
	}
}