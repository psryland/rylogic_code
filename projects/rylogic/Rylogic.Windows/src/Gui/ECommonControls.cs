using System;
using Rylogic.Interop.Win32;

namespace Rylogic.Gui.Native
{
	// The common control classes
	[Flags] public enum ECommonControls
	{
		None            = 0,
		ListViewClasses = Win32.ICC_LISTVIEW_CLASSES,   // listview, header
		TreeViewClasses = Win32.ICC_TREEVIEW_CLASSES,   // treeview, tooltips
		BarClasses      = Win32.ICC_BAR_CLASSES,        // toolbar, statusbar, trackbar, tooltips
		TabClasses      = Win32.ICC_TAB_CLASSES,        // tab, tooltips
		UpDown          = Win32.ICC_UPDOWN_CLASS,       // updown
		Progress        = Win32.ICC_PROGRESS_CLASS,     // progress
		Hotkey          = Win32.ICC_HOTKEY_CLASS,       // hotkey
		Animate         = Win32.ICC_ANIMATE_CLASS,      // animate
		Win95Classes    = Win32.ICC_WIN95_CLASSES,      //
		DateClasses     = Win32.ICC_DATE_CLASSES,       // month picker, date picker, time picker, updown
		ComboEx         = Win32.ICC_USEREX_CLASSES,     // comboex
		Rebar           = Win32.ICC_COOL_CLASSES,       // rebar (coolbar) control
		Internet        = Win32.ICC_INTERNET_CLASSES,   //
		PageScroller    = Win32.ICC_PAGESCROLLER_CLASS, // page scroller
		NativeFontCtrl  = Win32.ICC_NATIVEFNTCTL_CLASS, // native font control
		StandardClasses = Win32.ICC_STANDARD_CLASSES,
		LinkClass       = Win32.ICC_LINK_CLASS,
		All             = ~None,
	};
}
