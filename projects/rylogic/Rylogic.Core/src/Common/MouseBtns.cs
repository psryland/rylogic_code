using System;

namespace Rylogic.Common
{
	[Flags]
	public enum EMouseBtns
	{
		// Compatible with native MK_ constants.
		// Use the extension 'WinFormsUtil.ToMouseBtns' to convert from 'System.Windows.Forms.MouseButtons'
		// Use the extension 'WPFUtil.ToMouseBtns' to convert from 'System.Windows.Input.MouseEventArgs'
		None = 0,
		Left     = 0x0001, // MK_LBUTTON 
		Right    = 0x0002, // MK_RBUTTON 
		Shift    = 0x0004, // MK_SHIFT   
		Ctrl     = 0x0008, // MK_CONTROL 
		Middle   = 0x0010, // MK_MBUTTON 
		XButton1 = 0x0020, // MK_XBUTTON1
		XButton2 = 0x0040, // MK_XBUTTON2
		Alt      = 0x0080, // There is no MK_ defined for alt, this is tested using GetKeyState
	}
}
