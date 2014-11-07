//***************************************************
// Win32 wrapper
//  Copyright (c) Rylogic Ltd 2008
//***************************************************

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using pr.common;
using pr.extn;
using HWND=System.IntPtr;

namespace pr.util
{
	/// <summary>Win32 wrapper</summary>
	[System.Security.SuppressUnmanagedCodeSecurity] // We won't use this maliciously
	public static class Win32
	{
		#region Windows header constants

		#region Windows Messages
		public const uint WM_NULL                         = 0x0000;
		public const uint WM_CREATE                       = 0x0001;
		public const uint WM_DESTROY                      = 0x0002;
		public const uint WM_MOVE                         = 0x0003;
		public const uint WM_SIZE                         = 0x0005;
		public const uint WM_ACTIVATE                     = 0x0006;
		public const uint WM_SETFOCUS                     = 0x0007;
		public const uint WM_KILLFOCUS                    = 0x0008;
		public const uint WM_ENABLE                       = 0x000A;
		public const uint WM_SETREDRAW                    = 0x000B;
		public const uint WM_SETTEXT                      = 0x000C;
		public const uint WM_GETTEXT                      = 0x000D;
		public const uint WM_GETTEXTLENGTH                = 0x000E;
		public const uint WM_PAINT                        = 0x000F;
		public const uint WM_CLOSE                        = 0x0010;
		public const uint WM_QUERYENDSESSION              = 0x0011;
		public const uint WM_QUERYOPEN                    = 0x0013;
		public const uint WM_ENDSESSION                   = 0x0016;
		public const uint WM_QUIT                         = 0x0012;
		public const uint WM_ERASEBKGND                   = 0x0014;
		public const uint WM_SYSCOLORCHANGE               = 0x0015;
		public const uint WM_SHOWWINDOW                   = 0x0018;
		public const uint WM_WININICHANGE                 = 0x001A;
		public const uint WM_SETTINGCHANGE                = WM_WININICHANGE;
		public const uint WM_DEVMODECHANGE                = 0x001B;
		public const uint WM_ACTIVATEAPP                  = 0x001C;
		public const uint WM_FONTCHANGE                   = 0x001D;
		public const uint WM_TIMECHANGE                   = 0x001E;
		public const uint WM_CANCELMODE                   = 0x001F;
		public const uint WM_SETCURSOR                    = 0x0020;
		public const uint WM_MOUSEACTIVATE                = 0x0021;
		public const uint WM_CHILDACTIVATE                = 0x0022;
		public const uint WM_QUEUESYNC                    = 0x0023;
		public const uint WM_GETMINMAXINFO                = 0x0024;
		public const uint WM_PAINTICON                    = 0x0026;
		public const uint WM_ICONERASEBKGND               = 0x0027;
		public const uint WM_NEXTDLGCTL                   = 0x0028;
		public const uint WM_SPOOLERSTATUS                = 0x002A;
		public const uint WM_DRAWITEM                     = 0x002B;
		public const uint WM_MEASUREITEM                  = 0x002C;
		public const uint WM_DELETEITEM                   = 0x002D;
		public const uint WM_VKEYTOITEM                   = 0x002E;
		public const uint WM_CHARTOITEM                   = 0x002F;
		public const uint WM_SETFONT                      = 0x0030;
		public const uint WM_GETFONT                      = 0x0031;
		public const uint WM_SETHOTKEY                    = 0x0032;
		public const uint WM_GETHOTKEY                    = 0x0033;
		public const uint WM_QUERYDRAGICON                = 0x0037;
		public const uint WM_COMPAREITEM                  = 0x0039;
		public const uint WM_GETOBJECT                    = 0x003D;
		public const uint WM_COMPACTING                   = 0x0041;
		public const uint WM_COMMNOTIFY                   = 0x0044;  /* no longer suported */
		public const uint WM_WINDOWPOSCHANGING            = 0x0046;
		public const uint WM_WINDOWPOSCHANGED             = 0x0047;
		public const uint WM_POWER                        = 0x0048;
		public const uint WM_COPYDATA                     = 0x004A;
		public const uint WM_CANCELJOURNAL                = 0x004B;
		public const uint WM_NOTIFY                       = 0x004E;
		public const uint WM_INPUTLANGCHANGEREQUEST       = 0x0050;
		public const uint WM_INPUTLANGCHANGE              = 0x0051;
		public const uint WM_TCARD                        = 0x0052;
		public const uint WM_HELP                         = 0x0053;
		public const uint WM_USERCHANGED                  = 0x0054;
		public const uint WM_NOTIFYFORMAT                 = 0x0055;
		public const uint WM_CONTEXTMENU                  = 0x007B;
		public const uint WM_STYLECHANGING                = 0x007C;
		public const uint WM_STYLECHANGED                 = 0x007D;
		public const uint WM_DISPLAYCHANGE                = 0x007E;
		public const uint WM_GETICON                      = 0x007F;
		public const uint WM_SETICON                      = 0x0080;
		public const uint WM_NCCREATE                     = 0x0081;
		public const uint WM_NCDESTROY                    = 0x0082;
		public const uint WM_NCCALCSIZE                   = 0x0083;
		public const uint WM_NCHITTEST                    = 0x0084;
		public const uint WM_NCPAINT                      = 0x0085;
		public const uint WM_NCACTIVATE                   = 0x0086;
		public const uint WM_GETDLGCODE                   = 0x0087;
		public const uint WM_SYNCPAINT                    = 0x0088;
		public const uint WM_NCMOUSEMOVE                  = 0x00A0;
		public const uint WM_NCLBUTTONDOWN                = 0x00A1;
		public const uint WM_NCLBUTTONUP                  = 0x00A2;
		public const uint WM_NCLBUTTONDBLCLK              = 0x00A3;
		public const uint WM_NCRBUTTONDOWN                = 0x00A4;
		public const uint WM_NCRBUTTONUP                  = 0x00A5;
		public const uint WM_NCRBUTTONDBLCLK              = 0x00A6;
		public const uint WM_NCMBUTTONDOWN                = 0x00A7;
		public const uint WM_NCMBUTTONUP                  = 0x00A8;
		public const uint WM_NCMBUTTONDBLCLK              = 0x00A9;
		public const uint WM_NCXBUTTONDOWN                = 0x00AB;
		public const uint WM_NCXBUTTONUP                  = 0x00AC;
		public const uint WM_NCXBUTTONDBLCLK              = 0x00AD;
		public const uint WM_INPUT_DEVICE_CHANGE          = 0x00FE;
		public const uint WM_INPUT                        = 0x00FF;
		public const uint WM_KEYDOWN                      = 0x0100;
		public const uint WM_KEYUP                        = 0x0101;
		public const uint WM_CHAR                         = 0x0102;
		public const uint WM_DEADCHAR                     = 0x0103;
		public const uint WM_SYSKEYDOWN                   = 0x0104;
		public const uint WM_SYSKEYUP                     = 0x0105;
		public const uint WM_SYSCHAR                      = 0x0106;
		public const uint WM_SYSDEADCHAR                  = 0x0107;
		public const uint WM_UNICHAR                      = 0x0109;
		public const uint WM_INITDIALOG                   = 0x0110;
		public const uint WM_COMMAND                      = 0x0111;
		public const uint WM_SYSCOMMAND                   = 0x0112;
		public const uint WM_TIMER                        = 0x0113;
		public const uint WM_HSCROLL                      = 0x0114;
		public const uint WM_VSCROLL                      = 0x0115;
		public const uint WM_INITMENU                     = 0x0116;
		public const uint WM_INITMENUPOPUP                = 0x0117;
		public const uint WM_MENUSELECT                   = 0x011F;
		public const uint WM_MENUCHAR                     = 0x0120;
		public const uint WM_ENTERIDLE                    = 0x0121;
		public const uint WM_MENURBUTTONUP                = 0x0122;
		public const uint WM_MENUDRAG                     = 0x0123;
		public const uint WM_MENUGETOBJECT                = 0x0124;
		public const uint WM_UNINITMENUPOPUP              = 0x0125;
		public const uint WM_MENUCOMMAND                  = 0x0126;
		public const uint WM_CHANGEUISTATE                = 0x0127;
		public const uint WM_UPDATEUISTATE                = 0x0128;
		public const uint WM_QUERYUISTATE                 = 0x0129;
		public const uint WM_MOUSEMOVE                    = 0x0200;
		public const uint WM_LBUTTONDOWN                  = 0x0201;
		public const uint WM_LBUTTONUP                    = 0x0202;
		public const uint WM_LBUTTONDBLCLK                = 0x0203;
		public const uint WM_RBUTTONDOWN                  = 0x0204;
		public const uint WM_RBUTTONUP                    = 0x0205;
		public const uint WM_RBUTTONDBLCLK                = 0x0206;
		public const uint WM_MBUTTONDOWN                  = 0x0207;
		public const uint WM_MBUTTONUP                    = 0x0208;
		public const uint WM_MBUTTONDBLCLK                = 0x0209;
		public const uint WM_MOUSEWHEEL                   = 0x020A;
		public const uint WM_XBUTTONDOWN                  = 0x020B;
		public const uint WM_XBUTTONUP                    = 0x020C;
		public const uint WM_XBUTTONDBLCLK                = 0x020D;
		public const uint WM_MOUSEHWHEEL                  = 0x020E;
		public const uint WM_PARENTNOTIFY                 = 0x0210;
		public const uint WM_ENTERMENULOOP                = 0x0211;
		public const uint WM_EXITMENULOOP                 = 0x0212;
		public const uint WM_NEXTMENU                     = 0x0213;
		public const uint WM_SIZING                       = 0x0214;
		public const uint WM_CAPTURECHANGED               = 0x0215;
		public const uint WM_MOVING                       = 0x0216;
		public const uint WM_DEVICECHANGE                 = 0x0219;
		public const uint WM_MDICREATE                    = 0x0220;
		public const uint WM_MDIDESTROY                   = 0x0221;
		public const uint WM_MDIACTIVATE                  = 0x0222;
		public const uint WM_MDIRESTORE                   = 0x0223;
		public const uint WM_MDINEXT                      = 0x0224;
		public const uint WM_MDIMAXIMIZE                  = 0x0225;
		public const uint WM_MDITILE                      = 0x0226;
		public const uint WM_MDICASCADE                   = 0x0227;
		public const uint WM_MDIICONARRANGE               = 0x0228;
		public const uint WM_MDIGETACTIVE                 = 0x0229;
		public const uint WM_MDISETMENU                   = 0x0230;
		public const uint WM_ENTERSIZEMOVE                = 0x0231;
		public const uint WM_EXITSIZEMOVE                 = 0x0232;
		public const uint WM_DROPFILES                    = 0x0233;
		public const uint WM_MDIREFRESHMENU               = 0x0234;
		public const uint WM_IME_SETCONTEXT               = 0x0281;
		public const uint WM_IME_NOTIFY                   = 0x0282;
		public const uint WM_IME_CONTROL                  = 0x0283;
		public const uint WM_IME_COMPOSITIONFULL          = 0x0284;
		public const uint WM_IME_SELECT                   = 0x0285;
		public const uint WM_IME_CHAR                     = 0x0286;
		public const uint WM_IME_REQUEST                  = 0x0288;
		public const uint WM_IME_KEYDOWN                  = 0x0290;
		public const uint WM_IME_KEYUP                    = 0x0291;
		public const uint WM_MOUSEHOVER                   = 0x02A1;
		public const uint WM_MOUSELEAVE                   = 0x02A3;
		public const uint WM_NCMOUSEHOVER                 = 0x02A0;
		public const uint WM_NCMOUSELEAVE                 = 0x02A2;
		public const uint WM_WTSSESSION_CHANGE            = 0x02B1;
		public const uint WM_TABLET_FIRST                 = 0x02c0;
		public const uint WM_TABLET_LAST                  = 0x02df;
		public const uint WM_CUT                          = 0x0300;
		public const uint WM_COPY                         = 0x0301;
		public const uint WM_PASTE                        = 0x0302;
		public const uint WM_CLEAR                        = 0x0303;
		public const uint WM_UNDO                         = 0x0304;
		public const uint WM_RENDERFORMAT                 = 0x0305;
		public const uint WM_RENDERALLFORMATS             = 0x0306;
		public const uint WM_DESTROYCLIPBOARD             = 0x0307;
		public const uint WM_DRAWCLIPBOARD                = 0x0308;
		public const uint WM_PAINTCLIPBOARD               = 0x0309;
		public const uint WM_VSCROLLCLIPBOARD             = 0x030A;
		public const uint WM_SIZECLIPBOARD                = 0x030B;
		public const uint WM_ASKCBFORMATNAME              = 0x030C;
		public const uint WM_CHANGECBCHAIN                = 0x030D;
		public const uint WM_HSCROLLCLIPBOARD             = 0x030E;
		public const uint WM_QUERYNEWPALETTE              = 0x030F;
		public const uint WM_PALETTEISCHANGING            = 0x0310;
		public const uint WM_PALETTECHANGED               = 0x0311;
		public const uint WM_HOTKEY                       = 0x0312;
		public const uint WM_PRINT                        = 0x0317;
		public const uint WM_PRINTCLIENT                  = 0x0318;
		public const uint WM_APPCOMMAND                   = 0x0319;
		public const uint WM_THEMECHANGED                 = 0x031A;
		public const uint WM_CLIPBOARDUPDATE              = 0x031D;
		public const uint WM_DWMCOMPOSITIONCHANGED        = 0x031E;
		public const uint WM_DWMNCRENDERINGCHANGED        = 0x031F;
		public const uint WM_DWMCOLORIZATIONCOLORCHANGED  = 0x0320;
		public const uint WM_DWMWINDOWMAXIMIZEDCHANGE     = 0x0321;
		public const uint WM_GETTITLEBARINFOEX            = 0x033F;
		public const uint WM_HANDHELDFIRST                = 0x0358;
		public const uint WM_HANDHELDLAST                 = 0x035F;
		public const uint WM_AFXFIRST                     = 0x0360;
		public const uint WM_AFXLAST                      = 0x037F;
		public const uint WM_PENWINFIRST                  = 0x0380;
		public const uint WM_PENWINLAST                   = 0x038F;
		public const uint WM_APP                          = 0x8000;
		public const uint WM_USER                         = 0x0400;
		#endregion

		#region WM_SYSCOMMAND values
		public const uint SC_WPARAM_MASK                  = 0xFFF0; // In C# you need to mask the wparam with this as the lower bits contain magic stuff
		public const uint SC_SIZE                         = 0xF000; // Sizes the window.
		public const uint SC_MOVE                         = 0xF010; // Moves the window.
		public const uint SC_MINIMIZE                     = 0xF020; // Minimizes the window.
		public const uint SC_MAXIMIZE                     = 0xF030; // Maximizes the window.
		public const uint SC_NEXTWINDOW                   = 0xF040; // Moves to the next window.
		public const uint SC_PREVWINDOW                   = 0xF050; // Moves to the previous window.
		public const uint SC_CLOSE                        = 0xF060; // Closes the window.
		public const uint SC_VSCROLL                      = 0xF070; // Scrolls vertically.
		public const uint SC_HSCROLL                      = 0xF080; // Scrolls horizontally.
		public const uint SC_MOUSEMENU                    = 0xF090; // Retrieves the window menu as a result of a mouse click.
		public const uint SC_KEYMENU                      = 0xF100; // Retrieves the window menu as a result of a keystroke. For more information, see the Remarks section.
		public const uint SC_ARRANGE                      = 0xF110; //
		public const uint SC_RESTORE                      = 0xF120; // Restores the window to its normal position and size.
		public const uint SC_TASKLIST                     = 0xF130; // Activates the Start menu.
		public const uint SC_SCREENSAVE                   = 0xF140; // Executes the screen saver application specified in the [boot] section of the System.ini file.
		public const uint SC_HOTKEY                       = 0xF150; // Activates the window associated with the application-specified hot key. The lParam parameter identifies the window to activate.
		public const uint SC_DEFAULT                      = 0xF160; // Selects the default item; the user double-clicked the window menu.
		public const uint SC_MONITORPOWER                 = 0xF170; // Sets the state of the display. This command supports devices that have power-saving features, such as a battery-powered personal computer.
		public const uint SC_CONTEXTHELP                  = 0xF180; // Changes the cursor to a question mark with a pointer. If the user then clicks a control in the dialog box, the control receives a WM_HELP message.
		public const uint SC_SEPARATOR                    = 0xF00F; //
		#endregion

		#region WM_ACTIVATE state values WA_
		public const uint WA_INACTIVE                     = 0;
		public const uint WA_ACTIVE                       = 1;
		public const uint WA_CLICKACTIVE                  = 2;
		#endregion

		#region Mouse key MK_
		// Key State Masks for Mouse Messages
		public const int MK_LBUTTON                       = 0x0001;
		public const int MK_RBUTTON                       = 0x0002;
		public const int MK_SHIFT                         = 0x0004;
		public const int MK_CONTROL                       = 0x0008;
		public const int MK_MBUTTON                       = 0x0010;
		public const int MK_XBUTTON1                      = 0x0020;
		public const int MK_XBUTTON2                      = 0x0040;
		#endregion

		#region Show Window SW_
		public const int SW_HIDE                          = 0;
		public const int SW_SHOWNORMAL                    = 1;
		public const int SW_NORMAL                        = 1;
		public const int SW_SHOWMINIMIZED                 = 2;
		public const int SW_SHOWMAXIMIZED                 = 3;
		public const int SW_MAXIMIZE                      = 3;
		public const int SW_SHOWNOACTIVATE                = 4;
		public const int SW_SHOW                          = 5;
		public const int SW_MINIMIZE                      = 6;
		public const int SW_SHOWMINNOACTIVE               = 7;
		public const int SW_SHOWNA                        = 8;
		public const int SW_RESTORE                       = 9;
		public const int SW_SHOWDEFAULT                   = 10;
		public const int SW_FORCEMINIMIZE                 = 11;
		public const int SW_MAX                           = 11;
		#endregion

		#region Set Window Position SWP_
		public const int SWP_NOSIZE                       = 0x0001;
		public const int SWP_NOMOVE                       = 0x0002;
		public const int SWP_NOZORDER                     = 0x0004;
		public const int SWP_NOREDRAW                     = 0x0008;
		public const int SWP_NOACTIVATE                   = 0x0010;
		public const int SWP_FRAMECHANGED                 = 0x0020;
		public const int SWP_SHOWWINDOW                   = 0x0040;
		public const int SWP_HIDEWINDOW                   = 0x0080;
		public const int SWP_NOCOPYBITS                   = 0x0100;
		public const int SWP_NOOWNERZORDER                = 0x0200;
		public const int SWP_NOSENDCHANGING               = 0x0400;
		public const int SWP_DRAWFRAME                    = SWP_FRAMECHANGED;
		public const int SWP_NOREPOSITION                 = SWP_NOOWNERZORDER;
		public const int SWP_DEFERERASE                   = 0x2000;
		public const int SWP_ASYNCWINDOWPOS               = 0x4000;
		#endregion

		#region HWND constants HWND_
		public static readonly HWND HWND_TOP              = new HWND( 0);
		public static readonly HWND HWND_BOTTOM           = new HWND( 1);
		public static readonly HWND HWND_TOPMOST          = new HWND(-1);
		public static readonly HWND HWND_NOTOPMOST        = new HWND(-2);
		#endregion

		#region NF_,NFR_
		public const int NFR_ANSI                         = 1;
		public const int NFR_UNICODE                      = 2;
		public const int NF_QUERY                         = 3;
		public const int NF_REQUERY                       = 4;
		#endregion

		#region windows message high WH_
		public const int WH_MOUSE_LL                      = 14;
		public const int WH_KEYBOARD_LL                   = 13;
		public const int WH_MOUSE                         = 7;
		public const int WH_KEYBOARD                      = 2;
		#endregion

		#region Virtual key VK_
		// VK_0 - VK_9 are the same as ASCII '0' - '9' (0x30 - 0x39)
		// 0x40 : unassigned
		// VK_A - VK_Z are the same as ASCII 'A' - 'Z' (0x41 - 0x5A)
		public const byte VK_LBUTTON             = 0x01;
		public const byte VK_RBUTTON             = 0x02;
		public const byte VK_CANCEL              = 0x03;
		public const byte VK_MBUTTON             = 0x04;    /* NOT contiguous with L & RBUTTON */
		public const byte VK_XBUTTON1            = 0x05;    /* NOT contiguous with L & RBUTTON */
		public const byte VK_XBUTTON2            = 0x06;    /* NOT contiguous with L & RBUTTON */
		public const byte VK_BACK                = 0x08;
		public const byte VK_TAB                 = 0x09;
		public const byte VK_CLEAR               = 0x0C;
		public const byte VK_RETURN              = 0x0D;
		public const byte VK_SHIFT               = 0x10;
		public const byte VK_CONTROL             = 0x11;
		public const byte VK_MENU                = 0x12;
		public const byte VK_PAUSE               = 0x13;
		public const byte VK_CAPITAL             = 0x14;
		public const byte VK_KANA                = 0x15;
		public const byte VK_HANGEUL             = 0x15;  /* old name - should be here for compatibility */
		public const byte VK_HANGUL              = 0x15;
		public const byte VK_JUNJA               = 0x17;
		public const byte VK_FINAL               = 0x18;
		public const byte VK_HANJA               = 0x19;
		public const byte VK_KANJI               = 0x19;
		public const byte VK_ESCAPE              = 0x1B;
		public const byte VK_CONVERT             = 0x1C;
		public const byte VK_NONCONVERT          = 0x1D;
		public const byte VK_ACCEPT              = 0x1E;
		public const byte VK_MODECHANGE          = 0x1F;
		public const byte VK_SPACE               = 0x20;
		public const byte VK_PRIOR               = 0x21;
		public const byte VK_NEXT                = 0x22;
		public const byte VK_END                 = 0x23;
		public const byte VK_HOME                = 0x24;
		public const byte VK_LEFT                = 0x25;
		public const byte VK_UP                  = 0x26;
		public const byte VK_RIGHT               = 0x27;
		public const byte VK_DOWN                = 0x28;
		public const byte VK_SELECT              = 0x29;
		public const byte VK_PRINT               = 0x2A;
		public const byte VK_EXECUTE             = 0x2B;
		public const byte VK_SNAPSHOT            = 0x2C;
		public const byte VK_INSERT              = 0x2D;
		public const byte VK_DELETE              = 0x2E;
		public const byte VK_HELP                = 0x2F;
		public const byte VK_LWIN                = 0x5B;
		public const byte VK_RWIN                = 0x5C;
		public const byte VK_APPS                = 0x5D;
		public const byte VK_SLEEP               = 0x5F;
		public const byte VK_NUMPAD0             = 0x60;
		public const byte VK_NUMPAD1             = 0x61;
		public const byte VK_NUMPAD2             = 0x62;
		public const byte VK_NUMPAD3             = 0x63;
		public const byte VK_NUMPAD4             = 0x64;
		public const byte VK_NUMPAD5             = 0x65;
		public const byte VK_NUMPAD6             = 0x66;
		public const byte VK_NUMPAD7             = 0x67;
		public const byte VK_NUMPAD8             = 0x68;
		public const byte VK_NUMPAD9             = 0x69;
		public const byte VK_MULTIPLY            = 0x6A;
		public const byte VK_ADD                 = 0x6B;
		public const byte VK_SEPARATOR           = 0x6C;
		public const byte VK_SUBTRACT            = 0x6D;
		public const byte VK_DECIMAL             = 0x6E;
		public const byte VK_DIVIDE              = 0x6F;
		public const byte VK_F1                  = 0x70;
		public const byte VK_F2                  = 0x71;
		public const byte VK_F3                  = 0x72;
		public const byte VK_F4                  = 0x73;
		public const byte VK_F5                  = 0x74;
		public const byte VK_F6                  = 0x75;
		public const byte VK_F7                  = 0x76;
		public const byte VK_F8                  = 0x77;
		public const byte VK_F9                  = 0x78;
		public const byte VK_F10                 = 0x79;
		public const byte VK_F11                 = 0x7A;
		public const byte VK_F12                 = 0x7B;
		public const byte VK_F13                 = 0x7C;
		public const byte VK_F14                 = 0x7D;
		public const byte VK_F15                 = 0x7E;
		public const byte VK_F16                 = 0x7F;
		public const byte VK_F17                 = 0x80;
		public const byte VK_F18                 = 0x81;
		public const byte VK_F19                 = 0x82;
		public const byte VK_F20                 = 0x83;
		public const byte VK_F21                 = 0x84;
		public const byte VK_F22                 = 0x85;
		public const byte VK_F23                 = 0x86;
		public const byte VK_F24                 = 0x87;
		public const byte VK_NUMLOCK             = 0x90;
		public const byte VK_SCROLL              = 0x91;
		public const byte VK_OEM_NEC_EQUAL       = 0x92;   // '=' key on numpad
		public const byte VK_OEM_FJ_JISHO        = 0x92;   // 'Dictionary' key
		public const byte VK_OEM_FJ_MASSHOU      = 0x93;   // 'Unregister word' key
		public const byte VK_OEM_FJ_TOUROKU      = 0x94;   // 'Register word' key
		public const byte VK_OEM_FJ_LOYA         = 0x95;   // 'Left OYAYUBI' key
		public const byte VK_OEM_FJ_ROYA         = 0x96;   // 'Right OYAYUBI' key
		public const byte VK_LSHIFT              = 0xA0;
		public const byte VK_RSHIFT              = 0xA1;
		public const byte VK_LCONTROL            = 0xA2;
		public const byte VK_RCONTROL            = 0xA3;
		public const byte VK_LMENU               = 0xA4;
		public const byte VK_RMENU               = 0xA5;
		public const byte VK_BROWSER_BACK        = 0xA6;
		public const byte VK_BROWSER_FORWARD     = 0xA7;
		public const byte VK_BROWSER_REFRESH     = 0xA8;
		public const byte VK_BROWSER_STOP        = 0xA9;
		public const byte VK_BROWSER_SEARCH      = 0xAA;
		public const byte VK_BROWSER_FAVORITES   = 0xAB;
		public const byte VK_BROWSER_HOME        = 0xAC;
		public const byte VK_VOLUME_MUTE         = 0xAD;
		public const byte VK_VOLUME_DOWN         = 0xAE;
		public const byte VK_VOLUME_UP           = 0xAF;
		public const byte VK_MEDIA_NEXT_TRACK    = 0xB0;
		public const byte VK_MEDIA_PREV_TRACK    = 0xB1;
		public const byte VK_MEDIA_STOP          = 0xB2;
		public const byte VK_MEDIA_PLAY_PAUSE    = 0xB3;
		public const byte VK_LAUNCH_MAIL         = 0xB4;
		public const byte VK_LAUNCH_MEDIA_SELECT = 0xB5;
		public const byte VK_LAUNCH_APP1         = 0xB6;
		public const byte VK_LAUNCH_APP2         = 0xB7;
		public const byte VK_OEM_1               = 0xBA;   // ';:' for US
		public const byte VK_OEM_PLUS            = 0xBB;   // '+' any country
		public const byte VK_OEM_COMMA           = 0xBC;   // ',' any country
		public const byte VK_OEM_MINUS           = 0xBD;   // '-' any country
		public const byte VK_OEM_PERIOD          = 0xBE;   // '.' any country
		public const byte VK_OEM_2               = 0xBF;   // '/?' for US
		public const byte VK_OEM_3               = 0xC0;   // '`~' for US
		public const byte VK_OEM_4               = 0xDB;  //  '[{' for US
		public const byte VK_OEM_5               = 0xDC;  //  '\|' for US
		public const byte VK_OEM_6               = 0xDD;  //  ']}' for US
		public const byte VK_OEM_7               = 0xDE;  //  ''"' for US
		public const byte VK_OEM_8               = 0xDF;
		public const byte VK_OEM_AX              = 0xE1;  //  'AX' key on Japanese AX kbd
		public const byte VK_OEM_102             = 0xE2;  //  "<>" or "\|" on RT 102-key kbd.
		public const byte VK_ICO_HELP            = 0xE3;  //  Help key on ICO
		public const byte VK_ICO_00              = 0xE4;  //  00 key on ICO
		public const byte VK_PROCESSKEY          = 0xE5;
		public const byte VK_ICO_CLEAR           = 0xE6;
		public const byte VK_PACKET              = 0xE7;
		public const byte VK_OEM_RESET           = 0xE9;
		public const byte VK_OEM_JUMP            = 0xEA;
		public const byte VK_OEM_PA1             = 0xEB;
		public const byte VK_OEM_PA2             = 0xEC;
		public const byte VK_OEM_PA3             = 0xED;
		public const byte VK_OEM_WSCTRL          = 0xEE;
		public const byte VK_OEM_CUSEL           = 0xEF;
		public const byte VK_OEM_ATTN            = 0xF0;
		public const byte VK_OEM_FINISH          = 0xF1;
		public const byte VK_OEM_COPY            = 0xF2;
		public const byte VK_OEM_AUTO            = 0xF3;
		public const byte VK_OEM_ENLW            = 0xF4;
		public const byte VK_OEM_BACKTAB         = 0xF5;
		public const byte VK_ATTN                = 0xF6;
		public const byte VK_CRSEL               = 0xF7;
		public const byte VK_EXSEL               = 0xF8;
		public const byte VK_EREOF               = 0xF9;
		public const byte VK_PLAY                = 0xFA;
		public const byte VK_ZOOM                = 0xFB;
		public const byte VK_NONAME              = 0xFC;
		public const byte VK_PA1                 = 0xFD;
		public const byte VK_OEM_CLEAR           = 0xFE;

		#region MAPKV - map virtual key
		public const byte MAPVK_VK_TO_VSC    = 0x0;
		public const byte MAPVK_VSC_TO_VK    = 0x1;
		public const byte MAPVK_VK_TO_CHAR   = 0x2;
		public const byte MAPVK_VSC_TO_VK_EX = 0x3;
		#endregion

		#endregion

		#region GWL_
		public const int  GWL_WNDPROC                     = -4;
		public const int  GWL_HINSTANCE                   = -6;
		public const int  GWL_ID                          = -12;
		public const int  GWL_STYLE                       = -16;
		public const int  GWL_EXSTYLE                     = -20;
		public const int  GWL_USERDATA                    = -21;
		#endregion

		#region TM_
		public const int TM_PLAINTEXT       = 1;
		public const int TM_RICHTEXT        = 2; // default behaviour
		public const int TM_SINGLELEVELUNDO = 4;
		public const int TM_MULTILEVELUNDO  = 8; // default behaviour
		public const int TM_SINGLECODEPAGE  = 16;
		public const int TM_MULTICODEPAGE   = 32; // default behaviour
		#endregion

		#region Edit Control
		public static class EditCtrl
		{
			// Edit Control Styles
			public const int ES_LEFT        = 0x0000;
			public const int ES_CENTER      = 0x0001;
			public const int ES_RIGHT       = 0x0002;
			public const int ES_MULTILINE   = 0x0004;
			public const int ES_UPPERCASE   = 0x0008;
			public const int ES_LOWERCASE   = 0x0010;
			public const int ES_PASSWORD    = 0x0020;
			public const int ES_AUTOVSCROLL = 0x0040;
			public const int ES_AUTOHSCROLL = 0x0080;
			public const int ES_NOHIDESEL   = 0x0100;
			public const int ES_OEMCONVERT  = 0x0400;
			public const int ES_READONLY    = 0x0800;
			public const int ES_WANTRETURN  = 0x1000;
			public const int ES_NUMBER      = 0x2000;

			// Edit Control Notification Codes
			public const int EN_SETFOCUS     = 0x0100;
			public const int EN_KILLFOCUS    = 0x0200;
			public const int EN_CHANGE       = 0x0300;
			public const int EN_UPDATE       = 0x0400;
			public const int EN_ERRSPACE     = 0x0500;
			public const int EN_MAXTEXT      = 0x0501;
			public const int EN_HSCROLL      = 0x0601;
			public const int EN_VSCROLL      = 0x0602;
			public const int EN_ALIGN_LTR_EC = 0x0700;
			public const int EN_ALIGN_RTL_EC = 0x0701;

			// Edit control EM_SETMARGIN parameters
			public const int EC_LEFTMARGIN  = 0x0001;
			public const int EC_RIGHTMARGIN = 0x0002;
			public const int EC_USEFONTINFO = 0xffff;

			// Edit Control Messages
			public const int EM_GETSEL               = 0x00B0;
			public const int EM_SETSEL               = 0x00B1;
			public const int EM_GETRECT              = 0x00B2;
			public const int EM_SETRECT              = 0x00B3;
			public const int EM_SETRECTNP            = 0x00B4;
			public const int EM_SCROLL               = 0x00B5;
			public const int EM_LINESCROLL           = 0x00B6;
			public const int EM_SCROLLCARET          = 0x00B7;
			public const int EM_GETMODIFY            = 0x00B8;
			public const int EM_SETMODIFY            = 0x00B9;
			public const int EM_GETLINECOUNT         = 0x00BA;
			public const int EM_LINEINDEX            = 0x00BB;
			public const int EM_SETHANDLE            = 0x00BC;
			public const int EM_GETHANDLE            = 0x00BD;
			public const int EM_GETTHUMB             = 0x00BE;
			public const int EM_LINELENGTH           = 0x00C1;
			public const int EM_REPLACESEL           = 0x00C2;
			public const int EM_GETLINE              = 0x00C4;
			public const int EM_LIMITTEXT            = 0x00C5;
			public const int EM_CANUNDO              = 0x00C6;
			public const int EM_UNDO                 = 0x00C7;
			public const int EM_FMTLINES             = 0x00C8;
			public const int EM_LINEFROMCHAR         = 0x00C9;
			public const int EM_SETTABSTOPS          = 0x00CB;
			public const int EM_SETPASSWORDCHAR      = 0x00CC;
			public const int EM_EMPTYUNDOBUFFER      = 0x00CD;
			public const int EM_GETFIRSTVISIBLELINE  = 0x00CE;
			public const int EM_SETREADONLY          = 0x00CF;
			public const int EM_SETWORDBREAKPROC     = 0x00D0;
			public const int EM_GETWORDBREAKPROC     = 0x00D1;
			public const int EM_GETPASSWORDCHAR      = 0x00D2;
			public const int EM_SETMARGINS           = 0x00D3;
			public const int EM_GETMARGINS           = 0x00D4;
			public const int EM_SETLIMITTEXT         = EM_LIMITTEXT;//;win40 Name change 
			public const int EM_GETLIMITTEXT         = 0x00D5;
			public const int EM_POSFROMCHAR          = 0x00D6;
			public const int EM_CHARFROMPOS          = 0x00D7;
			public const int EM_SETIMESTATUS         = 0x00D8;
			public const int EM_GETIMESTATUS         = 0x00D9;

			// EDITWORDBREAKPROC code values
			public const int WB_LEFT        = 0;
			public const int WB_RIGHT       = 1;
			public const int WB_ISDELIMITER = 2;
		}
		#endregion

		#region Rich Edit Control
		public static class RichEditCtrl
		{
			public const uint EM_GETLIMITTEXT                 = (WM_USER + 37);
			public const uint EM_POSFROMCHAR                  = (WM_USER + 38);
			public const uint EM_CHARFROMPOS                  = (WM_USER + 39);
			public const uint EM_SCROLLCARET                  = (WM_USER + 49);
			public const uint EM_CANPASTE                     = (WM_USER + 50);
			public const uint EM_DISPLAYBAND                  = (WM_USER + 51);
			public const uint EM_EXGETSEL                     = (WM_USER + 52);
			public const uint EM_EXLIMITTEXT                  = (WM_USER + 53);
			public const uint EM_EXLINEFROMCHAR               = (WM_USER + 54);
			public const uint EM_EXSETSEL                     = (WM_USER + 55);
			public const uint EM_FINDTEXT                     = (WM_USER + 56);
			public const uint EM_FORMATRANGE                  = (WM_USER + 57);
			public const uint EM_GETCHARFORMAT                = (WM_USER + 58);
			public const uint EM_GETEVENTMASK                 = (WM_USER + 59);
			public const uint EM_GETOLEINTERFACE              = (WM_USER + 60);
			public const uint EM_GETPARAFORMAT                = (WM_USER + 61);
			public const uint EM_GETSELTEXT                   = (WM_USER + 62);
			public const uint EM_HIDESELECTION                = (WM_USER + 63);
			public const uint EM_PASTESPECIAL                 = (WM_USER + 64);
			public const uint EM_REQUESTRESIZE                = (WM_USER + 65);
			public const uint EM_SELECTIONTYPE                = (WM_USER + 66);
			public const uint EM_SETBKGNDCOLOR                = (WM_USER + 67);
			public const uint EM_SETCHARFORMAT                = (WM_USER + 68);
			public const uint EM_SETEVENTMASK                 = (WM_USER + 69);
			public const uint EM_SETOLECALLBACK               = (WM_USER + 70);
			public const uint EM_SETPARAFORMAT                = (WM_USER + 71);
			public const uint EM_SETTARGETDEVICE              = (WM_USER + 72);
			public const uint EM_STREAMIN                     = (WM_USER + 73);
			public const uint EM_STREAMOUT                    = (WM_USER + 74);
			public const uint EM_GETTEXTRANGE                 = (WM_USER + 75);
			public const uint EM_FINDWORDBREAK                = (WM_USER + 76);
			public const uint EM_SETOPTIONS                   = (WM_USER + 77);
			public const uint EM_GETOPTIONS                   = (WM_USER + 78);
			public const uint EM_FINDTEXTEX                   = (WM_USER + 79);
			public const uint EM_GETWORDBREAKPROCEX           = (WM_USER + 80);
			public const uint EM_SETWORDBREAKPROCEX           = (WM_USER + 81);

			// RichEdit 2.0 messages
			public const uint EM_SETUNDOLIMIT                 = (WM_USER + 82);
			public const uint EM_REDO                         = (WM_USER + 84);
			public const uint EM_CANREDO                      = (WM_USER + 85);
			public const uint EM_GETUNDONAME                  = (WM_USER + 86);
			public const uint EM_GETREDONAME                  = (WM_USER + 87);
			public const uint EM_STOPGROUPTYPING              = (WM_USER + 88);
			public const uint EM_SETTEXTMODE                  = (WM_USER + 89);
			public const uint EM_GETTEXTMODE                  = (WM_USER + 90);
		}
		#endregion

		#region Window Styles WS_
		public const int WS_BORDER                        = unchecked(0x00800000); // The window has a thin-line border.
		public const int WS_CAPTION                       = unchecked(0x00C00000); // The window has a title bar (includes the WS_BORDER style).
		public const int WS_CHILD                         = unchecked(0x40000000); // The window is a child window. A window with this style cannot have a menu bar. This style cannot be used with the WS_POPUP style.
		public const int WS_CHILDWINDOW                   = unchecked(0x40000000); // Same as the WS_CHILD style.
		public const int WS_CLIPCHILDREN                  = unchecked(0x02000000); // Excludes the area occupied by child windows when drawing occurs within the parent window. This style is used when creating the parent window.
		public const int WS_CLIPSIBLINGS                  = unchecked(0x04000000); // Clips child windows relative to each other; that is, when a particular child window receives a WM_PAINT message, the WS_CLIPSIBLINGS style clips all other overlapping child windows out of the region of the child window to be updated. If WS_CLIPSIBLINGS is not specified and child windows overlap, it is possible, when drawing within the client area of a child window, to draw within the client area of a neighboring child window.
		public const int WS_DISABLED                      = unchecked(0x08000000); // The window is initially disabled. A disabled window cannot receive input from the user. To change this after a window has been created, use the EnableWindow function.
		public const int WS_DLGFRAME                      = unchecked(0x00400000); // The window has a border of a style typically used with dialog boxes. A window with this style cannot have a title bar.
		public const int WS_GROUP                         = unchecked(0x00020000); // The window is the first control of a group of controls. The group consists of this first control and all controls defined after it, up to the next control with the WS_GROUP style. The first control in each group usually has the WS_TABSTOP style so that the user can move from group to group. The user can subsequently change the keyboard focus from one control in the group to the next control in the group by using the direction keys. You can turn this style on and off to change dialog box navigation. To change this style after a window has been created, use the SetWindowLong function.
		public const int WS_HSCROLL                       = unchecked(0x00100000); // The window has a horizontal scroll bar.
		public const int WS_ICONIC                        = unchecked(0x20000000); // The window is initially minimized. Same as the WS_MINIMIZE style.
		public const int WS_MAXIMIZE                      = unchecked(0x01000000); // The window is initially maximized.
		public const int WS_MAXIMIZEBOX                   = unchecked(0x00010000); // The window has a maximize button. Cannot be combined with the WS_EX_CONTEXTHELP style. The WS_SYSMENU style must also be specified.
		public const int WS_MINIMIZE                      = unchecked(0x20000000); // The window is initially minimized. Same as the WS_ICONIC style.
		public const int WS_MINIMIZEBOX                   = unchecked(0x00020000); // The window has a minimize button. Cannot be combined with the WS_EX_CONTEXTHELP style. The WS_SYSMENU style must also be specified.
		public const int WS_OVERLAPPED                    = unchecked(0x00000000); // The window is an overlapped window. An overlapped window has a title bar and a border. Same as the WS_TILED style.
		public const int WS_OVERLAPPEDWINDOW              = unchecked(WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX); // The window is an overlapped window. Same as the WS_TILEDWINDOW style.
		public const int WS_POPUP                         = unchecked((int)0x80000000); // The windows is a pop-up window. This style cannot be used with the WS_CHILD style.
		public const int WS_POPUPWINDOW                   = (WS_POPUP | WS_BORDER | WS_SYSMENU); // The window is a pop-up window. The WS_CAPTION and WS_POPUPWINDOW styles must be combined to make the window menu visible.
		public const int WS_SIZEBOX                       = unchecked(0x00040000); // The window has a sizing border. Same as the WS_THICKFRAME style.
		public const int WS_SYSMENU                       = unchecked(0x00080000); // The window has a window menu on its title bar. The WS_CAPTION style must also be specified.
		public const int WS_TABSTOP                       = unchecked(0x00010000); // The window is a control that can receive the keyboard focus when the user presses the TAB key. Pressing the TAB key changes the keyboard focus to the next control with the WS_TABSTOP style. You can turn this style on and off to change dialog box navigation. To change this style after a window has been created, use the SetWindowLong function. For user-created windows and modeless dialogs to work with tab stops, alter the message loop to call the IsDialogMessage function.
		public const int WS_THICKFRAME                    = unchecked(0x00040000); // The window has a sizing border. Same as the WS_SIZEBOX style.
		public const int WS_TILED                         = unchecked(0x00000000); // The window is an overlapped window. An overlapped window has a title bar and a border. Same as the WS_OVERLAPPED style.
		public const int WS_TILEDWINDOW                   = (WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX); // The window is an overlapped window. Same as the WS_OVERLAPPEDWINDOW style.
		public const int WS_VISIBLE                       = unchecked(0x10000000); // The window is initially visible. This style can be turned on and off by using the ShowWindow or SetWindowPos function.
		public const int WS_VSCROLL                       = unchecked(0x00200000); // The window has a vertical scroll bar.
		#endregion

		#region Extended window styles WS_EX_
		public const int WS_EX_DLGMODALFRAME              = 0x00000001;
		public const int WS_EX_NOPARENTNOTIFY             = 0x00000004;
		public const int WS_EX_TOPMOST                    = 0x00000008;
		public const int WS_EX_ACCEPTFILES                = 0x00000010;
		public const int WS_EX_TRANSPARENT                = 0x00000020;
		public const int WS_EX_MDICHILD                   = 0x00000040;
		public const int WS_EX_TOOLWINDOW                 = 0x00000080;
		public const int WS_EX_WINDOWEDGE                 = 0x00000100;
		public const int WS_EX_CLIENTEDGE                 = 0x00000200;
		public const int WS_EX_CONTEXTHELP                = 0x00000400;
		public const int WS_EX_RIGHT                      = 0x00001000;
		public const int WS_EX_LEFT                       = 0x00000000;
		public const int WS_EX_RTLREADING                 = 0x00002000;
		public const int WS_EX_LTRREADING                 = 0x00000000;
		public const int WS_EX_LEFTSCROLLBAR              = 0x00004000;
		public const int WS_EX_RIGHTSCROLLBAR             = 0x00000000;
		public const int WS_EX_CONTROLPARENT              = 0x00010000;
		public const int WS_EX_STATICEDGE                 = 0x00020000;
		public const int WS_EX_APPWINDOW                  = 0x00040000;
		public const int WS_EX_OVERLAPPEDWINDOW           = WS_EX_WINDOWEDGE | WS_EX_CLIENTEDGE;
		public const int WS_EX_PALETTEWINDOW              = WS_EX_WINDOWEDGE | WS_EX_TOOLWINDOW | WS_EX_TOPMOST;
		public const int WS_EX_LAYERED                    = 0x00080000;
		public const int WS_EX_NOINHERITLAYOUT            = 0x00100000; // Disable inheritence of mirroring by children
		public const int WS_EX_NOREDIRECTIONBITMAP        = 0x00200000;
		public const int WS_EX_COMPOSITED                 = 0x02000000;
		public const int WS_EX_LAYOUTRTL                  = 0x00400000;
		public const int WS_EX_NOACTIVATE                 = 0x08000000;
		#endregion

		#region Class styles CS_
		public const int CS_BYTEALIGNCLIENT               = 0x00001000;
		public const int CS_BYTEALIGNWINDOW               = 0x00002000;
		public const int CS_CLASSDC                       = 0x00000040;
		public const int CS_DBLCLKS                       = 0x00000008;
		public const int CS_DROPSHADOW                    = 0x00020000;
		public const int CS_GLOBALCLASS                   = 0x00004000;
		public const int CS_HREDRAW                       = 0x00000002;
		public const int CS_NOCLOSE                       = 0x00000200;
		public const int CS_OWNDC                         = 0x00000020;
		public const int CS_PARENTDC                      = 0x00000080;
		public const int CS_SAVEBITS                      = 0x00000800;
		public const int CS_VREDRAW                       = 0x00000001;
		#endregion

		#region Graphics modes GM_
		public const int GM_COMPATIBLE                    = 1;
		public const int GM_ADVANCED                      = 2;
		#endregion

		#region Mapping Modes MM_
		public const int MM_TEXT                          = 1;
		public const int MM_LOMETRIC                      = 2;
		public const int MM_HIMETRIC                      = 3;
		public const int MM_LOENGLISH                     = 4;
		public const int MM_HIENGLISH                     = 5;
		public const int MM_TWIPS                         = 6;
		public const int MM_ISOTROPIC                     = 7;
		public const int MM_ANISOTROPIC                   = 8;
		#endregion

		#region Menu Flags MF_, MFT_, MFS_
		public const int MF_INSERT           = 0x00000000;
		public const int MF_CHANGE           = 0x00000080;
		public const int MF_APPEND           = 0x00000100;
		public const int MF_DELETE           = 0x00000200;
		public const int MF_REMOVE           = 0x00001000;
		public const int MF_BYCOMMAND        = 0x00000000;
		public const int MF_BYPOSITION       = 0x00000400;
		public const int MF_SEPARATOR        = 0x00000800;
		public const int MF_ENABLED          = 0x00000000;
		public const int MF_GRAYED           = 0x00000001;
		public const int MF_DISABLED         = 0x00000002;
		public const int MF_UNCHECKED        = 0x00000000;
		public const int MF_CHECKED          = 0x00000008;
		public const int MF_USECHECKBITMAPS  = 0x00000200;
		public const int MF_STRING           = 0x00000000;
		public const int MF_BITMAP           = 0x00000004;
		public const int MF_OWNERDRAW        = 0x00000100;
		public const int MF_POPUP            = 0x00000010;
		public const int MF_MENUBARBREAK     = 0x00000020;
		public const int MF_MENUBREAK        = 0x00000040;
		public const int MF_UNHILITE         = 0x00000000;
		public const int MF_HILITE           = 0x00000080;
		public const int MF_DEFAULT          = 0x00001000;
		public const int MF_SYSMENU          = 0x00002000;
		public const int MF_HELP             = 0x00004000;
		public const int MF_RIGHTJUSTIFY     = 0x00004000;
		public const int MF_MOUSESELECT      = 0x00008000;
		public const int MF_END              = 0x00000080;  /* Obsolete -- only used by old RES files */

		public const int MFT_STRING          = MF_STRING;
		public const int MFT_BITMAP          = MF_BITMAP;
		public const int MFT_MENUBARBREAK    = MF_MENUBARBREAK;
		public const int MFT_MENUBREAK       = MF_MENUBREAK;
		public const int MFT_OWNERDRAW       = MF_OWNERDRAW;
		public const int MFT_RADIOCHECK      = 0x00000200;
		public const int MFT_SEPARATOR       = MF_SEPARATOR;
		public const int MFT_RIGHTORDER      = 0x00002000;
		public const int MFT_RIGHTJUSTIFY    = MF_RIGHTJUSTIFY;

		public const int MFS_GRAYED          = 0x00000003;
		public const int MFS_DISABLED        = MFS_GRAYED;
		public const int MFS_CHECKED         = MF_CHECKED;
		public const int MFS_HILITE          = MF_HILITE;
		public const int MFS_ENABLED         = MF_ENABLED;
		public const int MFS_UNCHECKED       = MF_UNCHECKED;
		public const int MFS_UNHILITE        = MF_UNHILITE;
		public const int MFS_DEFAULT         = MF_DEFAULT;
		#endregion

		#region Scroll bar direction SB_
		public enum ScrollBarDirection
		{
			SB_HORZ = 0,
			SB_VERT = 1,
			SB_CTL = 2,
			SB_BOTH = 3
		}
		#endregion

		#region Scroll info flags SIF_
		public enum ScrollInfoMask :uint
		{
			SIF_RANGE = 0x1,
			SIF_PAGE = 0x2,
			SIF_POS = 0x4,
			SIF_DISABLENOSCROLL = 0x8,
			SIF_TRACKPOS = 0x10,
			SIF_ALL = SIF_RANGE + SIF_PAGE + SIF_POS + SIF_TRACKPOS
		}
		#endregion

		#region Format Message
		public const uint FORMAT_MESSAGE_IGNORE_INSERTS  = 0x00000200U;
		public const uint FORMAT_MESSAGE_FROM_STRING     = 0x00000400U;
		public const uint FORMAT_MESSAGE_FROM_HMODULE    = 0x00000800U;
		public const uint FORMAT_MESSAGE_FROM_SYSTEM     = 0x00001000U;
		public const uint FORMAT_MESSAGE_ARGUMENT_ARRAY  = 0x00002000U;
		public const uint FORMAT_MESSAGE_MAX_WIDTH_MASK  = 0x000000FFU;
		#endregion

		#region Restart Manager

		public const int RmRebootReasonNone = 0;
		public const int CCH_RM_MAX_APP_NAME = 255;
		public const int CCH_RM_MAX_SVC_NAME = 63;
		public const int CCH_RM_SESSION_KEY = 16 + 1; // The returned session key is a wchar GUID with a null terminator

		public enum RM_APP_TYPE
		{
			RmUnknownApp  = 0,
			RmMainWindow  = 1,
			RmOtherWindow = 2,
			RmService     = 3,
			RmExplorer    = 4,
			RmConsole     = 5,
			RmCritical    = 1000
		}

		#endregion

		#endregion

		#region ERROR Codes
		#region Code Values
		public const int ERROR_SUCCESS                                              = 0  ;//(0x0) The operation completed successfully.
		public const int ERROR_INVALID_FUNCTION                                     = 1  ;//(0x1) Incorrect function.
		public const int ERROR_FILE_NOT_FOUND                                       = 2  ;//(0x2) The system cannot find the file specified.
		public const int ERROR_PATH_NOT_FOUND                                       = 3  ;//(0x3) The system cannot find the path specified.
		public const int ERROR_TOO_MANY_OPEN_FILES                                  = 4  ;//(0x4) The system cannot open the file.
		public const int ERROR_ACCESS_DENIED                                        = 5  ;//(0x5) Access is denied.
		public const int ERROR_INVALID_HANDLE                                       = 6  ;//(0x6) The handle is invalid.
		public const int ERROR_ARENA_TRASHED                                        = 7  ;//(0x7) The storage control blocks were destroyed.
		public const int ERROR_NOT_ENOUGH_MEMORY                                    = 8  ;//(0x8) Not enough storage is available to process this command.
		public const int ERROR_INVALID_BLOCK                                        = 9  ;//(0x9) The storage control block address is invalid.
		public const int ERROR_BAD_ENVIRONMENT                                      = 10 ;//(0xA) The environment is incorrect.
		public const int ERROR_BAD_FORMAT                                           = 11 ;//(0xB) An attempt was made to load a program with an incorrect format.
		public const int ERROR_INVALID_ACCESS                                       = 12 ;//(0xC) The access code is invalid.
		public const int ERROR_INVALID_DATA                                         = 13 ;//(0xD) The data is invalid.
		public const int ERROR_OUTOFMEMORY                                          = 14 ;//(0xE) Not enough storage is available to complete this operation.
		public const int ERROR_INVALID_DRIVE                                        = 15 ;//(0xF) The system cannot find the drive specified.
		public const int ERROR_CURRENT_DIRECTORY                                    = 16 ;//(0x10) The directory cannot be removed.
		public const int ERROR_NOT_SAME_DEVICE                                      = 17 ;//(0x11) The system cannot move the file to a different disk drive.
		public const int ERROR_NO_MORE_FILES                                        = 18 ;//(0x12) There are no more files.
		public const int ERROR_WRITE_PROTECT                                        = 19 ;//(0x13) The media is write protected.
		public const int ERROR_BAD_UNIT                                             = 20 ;//(0x14) The system cannot find the device specified.
		public const int ERROR_NOT_READY                                            = 21 ;//(0x15) The device is not ready.
		public const int ERROR_BAD_COMMAND                                          = 22 ;//(0x16) The device does not recognize the command.
		public const int ERROR_CRC                                                  = 23 ;//(0x17) Data error (cyclic redundancy check).
		public const int ERROR_BAD_LENGTH                                           = 24 ;//(0x18) The program issued a command but the command length is incorrect.
		public const int ERROR_SEEK                                                 = 25 ;//(0x19) The drive cannot locate a specific area or track on the disk.
		public const int ERROR_NOT_DOS_DISK                                         = 26 ;//(0x1A) The specified disk or diskette cannot be accessed.
		public const int ERROR_SECTOR_NOT_FOUND                                     = 27 ;//(0x1B) The drive cannot find the sector requested.
		public const int ERROR_OUT_OF_PAPER                                         = 28 ;//(0x1C) The printer is out of paper.
		public const int ERROR_WRITE_FAULT                                          = 29 ;//(0x1D) The system cannot write to the specified device.
		public const int ERROR_READ_FAULT                                           = 30 ;//(0x1E) The system cannot read from the specified device.
		public const int ERROR_GEN_FAILURE                                          = 31 ;//(0x1F) A device attached to the system is not functioning.
		public const int ERROR_SHARING_VIOLATION                                    = 32 ;//(0x20) The process cannot access the file because it is being used by another process.
		public const int ERROR_LOCK_VIOLATION                                       = 33 ;//(0x21) The process cannot access the file because another process has locked a portion of the file.
		public const int ERROR_WRONG_DISK                                           = 34 ;//(0x22) The wrong diskette is in the drive. Insert %2 (Volume Serial Number: %3) into drive %1.
		public const int ERROR_SHARING_BUFFER_EXCEEDED                              = 36 ;//(0x24) Too many files opened for sharing.
		public const int ERROR_HANDLE_EOF                                           = 38 ;//(0x26) Reached the end of the file.
		public const int ERROR_HANDLE_DISK_FULL                                     = 39 ;//(0x27) The disk is full.
		public const int ERROR_NOT_SUPPORTED                                        = 50 ;//(0x32) The request is not supported.
		public const int ERROR_REM_NOT_LIST                                         = 51 ;//(0x33) Windows cannot find the network path. Verify that the network path is correct and the destination computer is not busy or turned off. If Windows still cannot find the network path, contact your network administrator.
		public const int ERROR_DUP_NAME                                             = 52 ;//(0x34) You were not connected because a duplicate name exists on the network. If joining a domain, go to System in Control Panel to change the computer name and try again. If joining a workgroup, choose another workgroup name.
		public const int ERROR_BAD_NETPATH                                          = 53 ;//(0x35) The network path was not found.
		public const int ERROR_NETWORK_BUSY                                         = 54 ;//(0x36) The network is busy.
		public const int ERROR_DEV_NOT_EXIST                                        = 55 ;//(0x37) The specified network resource or device is no longer available.
		public const int ERROR_TOO_MANY_CMDS                                        = 56 ;//(0x38) The network BIOS command limit has been reached.
		public const int ERROR_ADAP_HDW_ERR                                         = 57 ;//(0x39) A network adapter hardware error occurred.
		public const int ERROR_BAD_NET_RESP                                         = 58 ;//(0x3A) The specified server cannot perform the requested operation.
		public const int ERROR_UNEXP_NET_ERR                                        = 59 ;//(0x3B) An unexpected network error occurred.
		public const int ERROR_BAD_REM_ADAP                                         = 60 ;//(0x3C) The remote adapter is not compatible.
		public const int ERROR_PRINTQ_FULL                                          = 61 ;//(0x3D) The printer queue is full.
		public const int ERROR_NO_SPOOL_SPACE                                       = 62 ;//(0x3E) Space to store the file waiting to be printed is not available on the server.
		public const int ERROR_PRINT_CANCELLED                                      = 63 ;//(0x3F) Your file waiting to be printed was deleted.
		public const int ERROR_NETNAME_DELETED                                      = 64 ;//(0x40) The specified network name is no longer available.
		public const int ERROR_NETWORK_ACCESS_DENIED                                = 65 ;//(0x41) Network access is denied.
		public const int ERROR_BAD_DEV_TYPE                                         = 66 ;//(0x42) The network resource type is not correct.
		public const int ERROR_BAD_NET_NAME                                         = 67 ;//(0x43) The network name cannot be found.
		public const int ERROR_TOO_MANY_NAMES                                       = 68 ;//(0x44) The name limit for the local computer network adapter card was exceeded.
		public const int ERROR_TOO_MANY_SESS                                        = 69 ;//(0x45) The network BIOS session limit was exceeded.
		public const int ERROR_SHARING_PAUSED                                       = 70 ;//(0x46) The remote server has been paused or is in the process of being started.
		public const int ERROR_REQ_NOT_ACCEP                                        = 71 ;//(0x47) No more connections can be made to this remote computer at this time because there are already as many connections as the computer can accept.
		public const int ERROR_REDIR_PAUSED                                         = 72 ;//(0x48) The specified printer or disk device has been paused.
		public const int ERROR_FILE_EXISTS                                          = 80 ;//(0x50) The file exists.
		public const int ERROR_CANNOT_MAKE                                          = 82 ;//(0x52) The directory or file cannot be created.
		public const int ERROR_FAIL_I24                                             = 83 ;//(0x53) Fail on INT 24.
		public const int ERROR_OUT_OF_STRUCTURES                                    = 84 ;//(0x54) Storage to process this request is not available.
		public const int ERROR_ALREADY_ASSIGNED                                     = 85 ;//(0x55) The local device name is already in use.
		public const int ERROR_INVALID_PASSWORD                                     = 86 ;//(0x56) The specified network password is not correct.
		public const int ERROR_INVALID_PARAMETER                                    = 87 ;//(0x57) The parameter is incorrect.
		public const int ERROR_NET_WRITE_FAULT                                      = 88 ;//(0x58) A write fault occurred on the network.
		public const int ERROR_NO_PROC_SLOTS                                        = 89 ;//(0x59) The system cannot start another process at this time.
		public const int ERROR_TOO_MANY_SEMAPHORES                                  = 100;//(0x64) Cannot create another system semaphore.
		public const int ERROR_EXCL_SEM_ALREADY_OWNED                               = 101;//(0x65) The exclusive semaphore is owned by another process.
		public const int ERROR_SEM_IS_SET                                           = 102;//(0x66) The semaphore is set and cannot be closed.
		public const int ERROR_TOO_MANY_SEM_REQUESTS                                = 103;//(0x67) The semaphore cannot be set again.
		public const int ERROR_INVALID_AT_INTERRUPT_TIME                            = 104;//(0x68) Cannot request exclusive semaphores at interrupt time.
		public const int ERROR_SEM_OWNER_DIED                                       = 105;//(0x69) The previous ownership of this semaphore has ended.
		public const int ERROR_SEM_USER_LIMIT                                       = 106;//(0x6A) Insert the diskette for drive %1.
		public const int ERROR_DISK_CHANGE                                          = 107;//(0x6B) The program stopped because an alternate diskette was not inserted.
		public const int ERROR_DRIVE_LOCKED                                         = 108;//(0x6C) The disk is in use or locked by another process.
		public const int ERROR_BROKEN_PIPE                                          = 109;//(0x6D) The pipe has been ended.
		public const int ERROR_OPEN_FAILED                                          = 110;//(0x6E) The system cannot open the device or file specified.
		public const int ERROR_BUFFER_OVERFLOW                                      = 111;//(0x6F) The file name is too long.
		public const int ERROR_DISK_FULL                                            = 112;//(0x70) There is not enough space on the disk.
		public const int ERROR_NO_MORE_SEARCH_HANDLES                               = 113;//(0x71) No more internal file identifiers available.
		public const int ERROR_INVALID_TARGET_HANDLE                                = 114;//(0x72) The target internal file identifier is incorrect.
		public const int ERROR_INVALID_CATEGORY                                     = 117;//(0x75) The IOCTL call made by the application program is not correct.
		public const int ERROR_INVALID_VERIFY_SWITCH                                = 118;//(0x76) The verify-on-write switch parameter value is not correct.
		public const int ERROR_BAD_DRIVER_LEVEL                                     = 119;//(0x77) The system does not support the command requested.
		public const int ERROR_CALL_NOT_IMPLEMENTED                                 = 120;//(0x78) This function is not supported on this system.
		public const int ERROR_SEM_TIMEOUT                                          = 121;//(0x79) The semaphore timeout period has expired.
		public const int ERROR_INSUFFICIENT_BUFFER                                  = 122;//(0x7A) The data area passed to a system call is too small.
		public const int ERROR_INVALID_NAME                                         = 123;//(0x7B) The filename, directory name, or volume label syntax is incorrect.
		public const int ERROR_INVALID_LEVEL                                        = 124;//(0x7C) The system call level is not correct.
		public const int ERROR_NO_VOLUME_LABEL                                      = 125;//(0x7D) The disk has no volume label.
		public const int ERROR_MOD_NOT_FOUND                                        = 126;//(0x7E) The specified module could not be found.
		public const int ERROR_PROC_NOT_FOUND                                       = 127;//(0x7F) The specified procedure could not be found.
		public const int ERROR_WAIT_NO_CHILDREN                                     = 128;//(0x80) There are no child processes to wait for.
		public const int ERROR_CHILD_NOT_COMPLETE                                   = 129;//(0x81) The %1 application cannot be run in Win32 mode.
		public const int ERROR_DIRECT_ACCESS_HANDLE                                 = 130;//(0x82) Attempt to use a file handle to an open disk partition for an operation other than raw disk I/O.
		public const int ERROR_NEGATIVE_SEEK                                        = 131;//(0x83) An attempt was made to move the file pointer before the beginning of the file.
		public const int ERROR_SEEK_ON_DEVICE                                       = 132;//(0x84) The file pointer cannot be set on the specified device or file.
		public const int ERROR_IS_JOIN_TARGET                                       = 133;//(0x85) A JOIN or SUBST command cannot be used for a drive that contains previously joined drives.
		public const int ERROR_IS_JOINED                                            = 134;//(0x86) An attempt was made to use a JOIN or SUBST command on a drive that has already been joined.
		public const int ERROR_IS_SUBSTED                                           = 135;//(0x87) An attempt was made to use a JOIN or SUBST command on a drive that has already been substituted.
		public const int ERROR_NOT_JOINED                                           = 136;//(0x88) The system tried to delete the JOIN of a drive that is not joined.
		public const int ERROR_NOT_SUBSTED                                          = 137;//(0x89) The system tried to delete the substitution of a drive that is not substituted.
		public const int ERROR_JOIN_TO_JOIN                                         = 138;//(0x8A) The system tried to join a drive to a directory on a joined drive.
		public const int ERROR_SUBST_TO_SUBST                                       = 139;//(0x8B) The system tried to substitute a drive to a directory on a substituted drive.
		public const int ERROR_JOIN_TO_SUBST                                        = 140;//(0x8C) The system tried to join a drive to a directory on a substituted drive.
		public const int ERROR_SUBST_TO_JOIN                                        = 141;//(0x8D) The system tried to SUBST a drive to a directory on a joined drive.
		public const int ERROR_BUSY_DRIVE                                           = 142;//(0x8E) The system cannot perform a JOIN or SUBST at this time.
		public const int ERROR_SAME_DRIVE                                           = 143;//(0x8F) The system cannot join or substitute a drive to or for a directory on the same drive.
		public const int ERROR_DIR_NOT_ROOT                                         = 144;//(0x90) The directory is not a subdirectory of the root directory.
		public const int ERROR_DIR_NOT_EMPTY                                        = 145;//(0x91) The directory is not empty.
		public const int ERROR_IS_SUBST_PATH                                        = 146;//(0x92) The path specified is being used in a substitute.
		public const int ERROR_IS_JOIN_PATH                                         = 147;//(0x93) Not enough resources are available to process this command.
		public const int ERROR_PATH_BUSY                                            = 148;//(0x94) The path specified cannot be used at this time.
		public const int ERROR_IS_SUBST_TARGET                                      = 149;//(0x95) An attempt was made to join or substitute a drive for which a directory on the drive is the target of a previous substitute.
		public const int ERROR_SYSTEM_TRACE                                         = 150;//(0x96) System trace information was not specified in your CONFIG.SYS file, or tracing is disallowed.
		public const int ERROR_INVALID_EVENT_COUNT                                  = 151;//(0x97) The number of specified semaphore events for DosMuxSemWait is not correct.
		public const int ERROR_TOO_MANY_MUXWAITERS                                  = 152;//(0x98) DosMuxSemWait did not execute; too many semaphores are already set.
		public const int ERROR_INVALID_LIST_FORMAT                                  = 153;//(0x99) The DosMuxSemWait list is not correct.
		public const int ERROR_LABEL_TOO_LONG                                       = 154;//(0x9A) The volume label you entered exceeds the label character limit of the target file system.
		public const int ERROR_TOO_MANY_TCBS                                        = 155;//(0x9B) Cannot create another thread.
		public const int ERROR_SIGNAL_REFUSED                                       = 156;//(0x9C) The recipient process has refused the signal.
		public const int ERROR_DISCARDED                                            = 157;//(0x9D) The segment is already discarded and cannot be locked.
		public const int ERROR_NOT_LOCKED                                           = 158;//(0x9E) The segment is already unlocked.
		public const int ERROR_BAD_THREADID_ADDR                                    = 159;//(0x9F) The address for the thread ID is not correct.
		public const int ERROR_BAD_ARGUMENTS                                        = 160;//(0xA0) One or more arguments are not correct.
		public const int ERROR_BAD_PATHNAME                                         = 161;//(0xA1) The specified path is invalid.
		public const int ERROR_SIGNAL_PENDING                                       = 162;//(0xA2) A signal is already pending.
		public const int ERROR_MAX_THRDS_REACHED                                    = 164;//(0xA4) No more threads can be created in the system.
		public const int ERROR_LOCK_FAILED                                          = 167;//(0xA7) Unable to lock a region of a file.
		public const int ERROR_BUSY                                                 = 170;//(0xAA) The requested resource is in use.
		public const int ERROR_DEVICE_SUPPORT_IN_PROGRESS                           = 171;//(0xAB) Device's command support detection is in progress.
		public const int ERROR_CANCEL_VIOLATION                                     = 173;//(0xAD) A lock request was not outstanding for the supplied cancel region.
		public const int ERROR_ATOMIC_LOCKS_NOT_SUPPORTED                           = 174;//(0xAE) The file system does not support atomic changes to the lock type.
		public const int ERROR_INVALID_SEGMENT_NUMBER                               = 180;//(0xB4) The system detected a segment number that was not correct.
		public const int ERROR_INVALID_ORDINAL                                      = 182;//(0xB6) The operating system cannot run %1.
		public const int ERROR_ALREADY_EXISTS                                       = 183;//(0xB7) Cannot create a file when that file already exists.
		public const int ERROR_INVALID_FLAG_NUMBER                                  = 186;//(0xBA) The flag passed is not correct.
		public const int ERROR_SEM_NOT_FOUND                                        = 187;//(0xBB) The specified system semaphore name was not found.
		public const int ERROR_INVALID_STARTING_CODESEG                             = 188;//(0xBC) The operating system cannot run %1.
		public const int ERROR_INVALID_STACKSEG                                     = 189;//(0xBD) The operating system cannot run %1.
		public const int ERROR_INVALID_MODULETYPE                                   = 190;//(0xBE) The operating system cannot run %1.
		public const int ERROR_INVALID_EXE_SIGNATURE                                = 191;//(0xBF) Cannot run %1 in Win32 mode.
		public const int ERROR_EXE_MARKED_INVALID                                   = 192;//(0xC0) The operating system cannot run %1.
		public const int ERROR_BAD_EXE_FORMAT                                       = 193;//(0xC1) %1 is not a valid Win32 application.
		public const int ERROR_ITERATED_DATA_EXCEEDS_64k                            = 194;//(0xC2) The operating system cannot run %1.
		public const int ERROR_INVALID_MINALLOCSIZE                                 = 195;//(0xC3) The operating system cannot run %1.
		public const int ERROR_DYNLINK_FROM_INVALID_RING                            = 196;//(0xC4) The operating system cannot run this application program.
		public const int ERROR_IOPL_NOT_ENABLED                                     = 197;//(0xC5) The operating system is not presently configured to run this application.
		public const int ERROR_INVALID_SEGDPL                                       = 198;//(0xC6) The operating system cannot run %1.
		public const int ERROR_AUTODATASEG_EXCEEDS_64k                              = 199;//(0xC7) The operating system cannot run this application program.
		public const int ERROR_RING2SEG_MUST_BE_MOVABLE                             = 200;//(0xC8) The code segment cannot be greater than or equal to 64K.
		public const int ERROR_RELOC_CHAIN_XEEDS_SEGLIM                             = 201;//(0xC9) The operating system cannot run %1.
		public const int ERROR_INFLOOP_IN_RELOC_CHAIN                               = 202;//(0xCA) The operating system cannot run %1.
		public const int ERROR_ENVVAR_NOT_FOUND                                     = 203;//(0xCB) The system could not find the environment option that was entered.
		public const int ERROR_NO_SIGNAL_SENT                                       = 205;//(0xCD) No process in the command subtree has a signal handler.
		public const int ERROR_FILENAME_EXCED_RANGE                                 = 206;//(0xCE) The filename or extension is too long.
		public const int ERROR_RING2_STACK_IN_USE                                   = 207;//(0xCF) The ring 2 stack is in use.
		public const int ERROR_META_EXPANSION_TOO_LONG                              = 208;//(0xD0) The global filename characters, * or ?, are entered incorrectly or too many global filename characters are specified.
		public const int ERROR_INVALID_SIGNAL_NUMBER                                = 209;//(0xD1) The signal being posted is not correct.
		public const int ERROR_THREAD_1_INACTIVE                                    = 210;//(0xD2) The signal handler cannot be set.
		public const int ERROR_LOCKED                                               = 212;//(0xD4) The segment is locked and cannot be reallocated.
		public const int ERROR_TOO_MANY_MODULES                                     = 214;//(0xD6) Too many dynamic-link modules are attached to this program or dynamic-link module.
		public const int ERROR_NESTING_NOT_ALLOWED                                  = 215;//(0xD7) Cannot nest calls to LoadModule.
		public const int ERROR_EXE_MACHINE_TYPE_MISMATCH                            = 216;//(0xD8) This version of %1 is not compatible with the version of Windows you're running. Check your computer's system information and then contact the software publisher.
		public const int ERROR_EXE_CANNOT_MODIFY_SIGNED_BINARY                      = 217;//(0xD9) The image file %1 is signed, unable to modify.
		public const int ERROR_EXE_CANNOT_MODIFY_STRONG_SIGNED_BINARY               = 218;//(0xDA) The image file %1 is strong signed, unable to modify.
		public const int ERROR_FILE_CHECKED_OUT                                     = 220;//(0xDC) This file is checked out or locked for editing by another user.
		public const int ERROR_CHECKOUT_REQUIRED                                    = 221;//(0xDD) The file must be checked out before saving changes.
		public const int ERROR_BAD_FILE_TYPE                                        = 222;//(0xDE) The file type being saved or retrieved has been blocked.
		public const int ERROR_FILE_TOO_LARGE                                       = 223;//(0xDF) The file size exceeds the limit allowed and cannot be saved.
		public const int ERROR_FORMS_AUTH_REQUIRED                                  = 224;//(0xE0) Access Denied. Before opening files in this location, you must first add the web site to your trusted sites list, browse to the web site, and select the option to login automatically.
		public const int ERROR_VIRUS_INFECTED                                       = 225;//(0xE1) Operation did not complete successfully because the file contains a virus or potentially unwanted software.
		public const int ERROR_VIRUS_DELETED                                        = 226;//(0xE2) This file contains a virus or potentially unwanted software and cannot be opened. Due to the nature of this virus or potentially unwanted software, the file has been removed from this location.
		public const int ERROR_PIPE_LOCAL                                           = 229;//(0xE5) The pipe is local.
		public const int ERROR_BAD_PIPE                                             = 230;//(0xE6) The pipe state is invalid.
		public const int ERROR_PIPE_BUSY                                            = 231;//(0xE7) All pipe instances are busy.
		public const int ERROR_NO_DATA                                              = 232;//(0xE8) The pipe is being closed.
		public const int ERROR_PIPE_NOT_CONNECTED                                   = 233;//(0xE9) No process is on the other end of the pipe.
		public const int ERROR_MORE_DATA                                            = 234;//(0xEA) More data is available.
		public const int ERROR_VC_DISCONNECTED                                      = 240;//(0xF0) The session was canceled.
		public const int ERROR_INVALID_EA_NAME                                      = 254;//(0xFE) The specified extended attribute name was invalid.
		public const int ERROR_EA_LIST_INCONSISTENT                                 = 255;//(0xFF) The extended attributes are inconsistent.
		public const int WAIT_TIMEOUT                                               = 258;//(0x102) The wait operation timed out.
		public const int ERROR_NO_MORE_ITEMS                                        = 259;//(0x103) No more data is available.
		public const int ERROR_CANNOT_COPY                                          = 266;//(0x10A) The copy functions cannot be used.
		public const int ERROR_DIRECTORY                                            = 267;//(0x10B) The directory name is invalid.
		public const int ERROR_EAS_DIDNT_FIT                                        = 275;//(0x113) The extended attributes did not fit in the buffer.
		public const int ERROR_EA_FILE_CORRUPT                                      = 276;//(0x114) The extended attribute file on the mounted file system is corrupt.
		public const int ERROR_EA_TABLE_FULL                                        = 277;//(0x115) The extended attribute table file is full.
		public const int ERROR_INVALID_EA_HANDLE                                    = 278;//(0x116) The specified extended attribute handle is invalid.
		public const int ERROR_EAS_NOT_SUPPORTED                                    = 282;//(0x11A) The mounted file system does not support extended attributes.
		public const int ERROR_NOT_OWNER                                            = 288;//(0x120) Attempt to release mutex not owned by caller.
		public const int ERROR_TOO_MANY_POSTS                                       = 298;//(0x12A) Too many posts were made to a semaphore.
		public const int ERROR_PARTIAL_COPY                                         = 299;//(0x12B) Only part of a ReadProcessMemory or WriteProcessMemory request was completed.
		public const int ERROR_OPLOCK_NOT_GRANTED                                   = 300;//(0x12C) The oplock request is denied.
		public const int ERROR_INVALID_OPLOCK_PROTOCOL                              = 301;//(0x12D) An invalid oplock acknowledgment was received by the system.
		public const int ERROR_DISK_TOO_FRAGMENTED                                  = 302;//(0x12E) The volume is too fragmented to complete this operation.
		public const int ERROR_DELETE_PENDING                                       = 303;//(0x12F) The file cannot be opened because it is in the process of being deleted.
		public const int ERROR_INCOMPATIBLE_WITH_GLOBAL_SHORT_NAME_REGISTRY_SETTING = 304;//(0x130) Short name settings may not be changed on this volume due to the global registry setting.
		public const int ERROR_SHORT_NAMES_NOT_ENABLED_ON_VOLUME                    = 305;//(0x131) Short names are not enabled on this volume.
		public const int ERROR_SECURITY_STREAM_IS_INCONSISTENT                      = 306;//(0x132) The security stream for the given volume is in an inconsistent state. Please run CHKDSK on the volume.
		public const int ERROR_INVALID_LOCK_RANGE                                   = 307;//(0x133) A requested file lock operation cannot be processed due to an invalid byte range.
		public const int ERROR_IMAGE_SUBSYSTEM_NOT_PRESENT                          = 308;//(0x134) The subsystem needed to support the image type is not present.
		public const int ERROR_NOTIFICATION_GUID_ALREADY_DEFINED                    = 309;//(0x135) The specified file already has a notification GUID associated with it.
		public const int ERROR_INVALID_EXCEPTION_HANDLER                            = 310;//(0x136) An invalid exception handler routine has been detected.
		public const int ERROR_DUPLICATE_PRIVILEGES                                 = 311;//(0x137) Duplicate privileges were specified for the token.
		public const int ERROR_NO_RANGES_PROCESSED                                  = 312;//(0x138) No ranges for the specified operation were able to be processed.
		public const int ERROR_NOT_ALLOWED_ON_SYSTEM_FILE                           = 313;//(0x139) Operation is not allowed on a file system internal file.
		public const int ERROR_DISK_RESOURCES_EXHAUSTED                             = 314;//(0x13A) The physical resources of this disk have been exhausted.
		public const int ERROR_INVALID_TOKEN                                        = 315;//(0x13B) The token representing the data is invalid.
		public const int ERROR_DEVICE_FEATURE_NOT_SUPPORTED                         = 316;//(0x13C) The device does not support the command feature.
		public const int ERROR_MR_MID_NOT_FOUND                                     = 317;//(0x13D) The system cannot find message text for message number 0x%1 in the message file for %2.
		public const int ERROR_SCOPE_NOT_FOUND                                      = 318;//(0x13E) The scope specified was not found.
		public const int ERROR_UNDEFINED_SCOPE                                      = 319;//(0x13F) The Central Access Policy specified is not defined on the target machine.
		public const int ERROR_INVALID_CAP                                          = 320;//(0x140) The Central Access Policy obtained from Active Directory is invalid.
		public const int ERROR_DEVICE_UNREACHABLE                                   = 321;//(0x141) The device is unreachable.
		public const int ERROR_DEVICE_NO_RESOURCES                                  = 322;//(0x142) The target device has insufficient resources to complete the operation.
		public const int ERROR_DATA_CHECKSUM_ERROR                                  = 323;//(0x143) A data integrity checksum error occurred. Data in the file stream is corrupt.
		public const int ERROR_INTERMIXED_KERNEL_EA_OPERATION                       = 324;//(0x144) An attempt was made to modify both a KERNEL and normal Extended Attribute (EA) in the same operation.
		public const int ERROR_FILE_LEVEL_TRIM_NOT_SUPPORTED                        = 326;//(0x146) Device does not support file-level TRIM.
		public const int ERROR_OFFSET_ALIGNMENT_VIOLATION                           = 327;//(0x147) The command specified a data offset that does not align to the device's granularity/alignment.
		public const int ERROR_INVALID_FIELD_IN_PARAMETER_LIST                      = 328;//(0x148) The command specified an invalid field in its parameter list.
		public const int ERROR_OPERATION_IN_PROGRESS                                = 329;//(0x149) An operation is currently in progress with the device.
		public const int ERROR_BAD_DEVICE_PATH                                      = 330;//(0x14A) An attempt was made to send down the command via an invalid path to the target device.
		public const int ERROR_TOO_MANY_DESCRIPTORS                                 = 331;//(0x14B) The command specified a number of descriptors that exceeded the maximum supported by the device.
		public const int ERROR_SCRUB_DATA_DISABLED                                  = 332;//(0x14C) Scrub is disabled on the specified file.
		public const int ERROR_NOT_REDUNDANT_STORAGE                                = 333;//(0x14D) The storage device does not provide redundancy.
		public const int ERROR_RESIDENT_FILE_NOT_SUPPORTED                          = 334;//(0x14E) An operation is not supported on a resident file.
		public const int ERROR_COMPRESSED_FILE_NOT_SUPPORTED                        = 335;//(0x14F) An operation is not supported on a compressed file.
		public const int ERROR_DIRECTORY_NOT_SUPPORTED                              = 336;//(0x150) An operation is not supported on a directory.
		public const int ERROR_NOT_READ_FROM_COPY                                   = 337;//(0x151) The specified copy of the requested data could not be read.
		public const int ERROR_FAIL_NOACTION_REBOOT                                 = 350;//(0x15E) No action was taken as a system reboot is required.
		public const int ERROR_FAIL_SHUTDOWN                                        = 351;//(0x15F) The shutdown operation failed.
		public const int ERROR_FAIL_RESTART                                         = 352;//(0x160) The restart operation failed.
		public const int ERROR_MAX_SESSIONS_REACHED                                 = 353;//(0x161) The maximum number of sessions has been reached.
		public const int ERROR_THREAD_MODE_ALREADY_BACKGROUND                       = 400;//(0x190) The thread is already in background processing mode.
		public const int ERROR_THREAD_MODE_NOT_BACKGROUND                           = 401;//(0x191) The thread is not in background processing mode.
		public const int ERROR_PROCESS_MODE_ALREADY_BACKGROUND                      = 402;//(0x192) The process is already in background processing mode.
		public const int ERROR_PROCESS_MODE_NOT_BACKGROUND                          = 403;//(0x193) The process is not in background processing mode.
		public const int ERROR_INVALID_ADDRESS                                      = 487;//(0x1E7) Attempt to access invalid address.
		#endregion

		/// <summary>Translate an error code to a string message</summary>
		public static string ErrorCodeToString(int error_code)
		{
			switch (error_code)
			{
			default: return "Error Code {0}".Fmt(error_code);
			case ERROR_SUCCESS                                              : return "(0x0) The operation completed successfully.";
			case ERROR_INVALID_FUNCTION                                     : return "(0x1) Incorrect function.";
			case ERROR_FILE_NOT_FOUND                                       : return "(0x2) The system cannot find the file specified.";
			case ERROR_PATH_NOT_FOUND                                       : return "(0x3) The system cannot find the path specified.";
			case ERROR_TOO_MANY_OPEN_FILES                                  : return "(0x4) The system cannot open the file.";
			case ERROR_ACCESS_DENIED                                        : return "(0x5) Access is denied.";
			case ERROR_INVALID_HANDLE                                       : return "(0x6) The handle is invalid.";
			case ERROR_ARENA_TRASHED                                        : return "(0x7) The storage control blocks were destroyed.";
			case ERROR_NOT_ENOUGH_MEMORY                                    : return "(0x8) Not enough storage is available to process this command.";
			case ERROR_INVALID_BLOCK                                        : return "(0x9) The storage control block address is invalid.";
			case ERROR_BAD_ENVIRONMENT                                      : return "(0xA) The environment is incorrect.";
			case ERROR_BAD_FORMAT                                           : return "(0xB) An attempt was made to load a program with an incorrect format.";
			case ERROR_INVALID_ACCESS                                       : return "(0xC) The access code is invalid.";
			case ERROR_INVALID_DATA                                         : return "(0xD) The data is invalid.";
			case ERROR_OUTOFMEMORY                                          : return "(0xE) Not enough storage is available to complete this operation.";
			case ERROR_INVALID_DRIVE                                        : return "(0xF) The system cannot find the drive specified.";
			case ERROR_CURRENT_DIRECTORY                                    : return "(0x10) The directory cannot be removed.";
			case ERROR_NOT_SAME_DEVICE                                      : return "(0x11) The system cannot move the file to a different disk drive.";
			case ERROR_NO_MORE_FILES                                        : return "(0x12) There are no more files.";
			case ERROR_WRITE_PROTECT                                        : return "(0x13) The media is write protected.";
			case ERROR_BAD_UNIT                                             : return "(0x14) The system cannot find the device specified.";
			case ERROR_NOT_READY                                            : return "(0x15) The device is not ready.";
			case ERROR_BAD_COMMAND                                          : return "(0x16) The device does not recognize the command.";
			case ERROR_CRC                                                  : return "(0x17) Data error (cyclic redundancy check).";
			case ERROR_BAD_LENGTH                                           : return "(0x18) The program issued a command but the command length is incorrect.";
			case ERROR_SEEK                                                 : return "(0x19) The drive cannot locate a specific area or track on the disk.";
			case ERROR_NOT_DOS_DISK                                         : return "(0x1A) The specified disk or diskette cannot be accessed.";
			case ERROR_SECTOR_NOT_FOUND                                     : return "(0x1B) The drive cannot find the sector requested.";
			case ERROR_OUT_OF_PAPER                                         : return "(0x1C) The printer is out of paper.";
			case ERROR_WRITE_FAULT                                          : return "(0x1D) The system cannot write to the specified device.";
			case ERROR_READ_FAULT                                           : return "(0x1E) The system cannot read from the specified device.";
			case ERROR_GEN_FAILURE                                          : return "(0x1F) A device attached to the system is not functioning.";
			case ERROR_SHARING_VIOLATION                                    : return "(0x20) The process cannot access the file because it is being used by another process.";
			case ERROR_LOCK_VIOLATION                                       : return "(0x21) The process cannot access the file because another process has locked a portion of the file.";
			case ERROR_WRONG_DISK                                           : return "(0x22) The wrong diskette is in the drive. Insert %2 (Volume Serial Number: %3) into drive %1.";
			case ERROR_SHARING_BUFFER_EXCEEDED                              : return "(0x24) Too many files opened for sharing.";
			case ERROR_HANDLE_EOF                                           : return "(0x26) Reached the end of the file.";
			case ERROR_HANDLE_DISK_FULL                                     : return "(0x27) The disk is full.";
			case ERROR_NOT_SUPPORTED                                        : return "(0x32) The request is not supported.";
			case ERROR_REM_NOT_LIST                                         : return "(0x33) Windows cannot find the network path. Verify that the network path is correct and the destination computer is not busy or turned off. If Windows still cannot find the network path, contact your network administrator.";
			case ERROR_DUP_NAME                                             : return "(0x34) You were not connected because a duplicate name exists on the network. If joining a domain, go to System in Control Panel to change the computer name and try again. If joining a workgroup, choose another workgroup name.";
			case ERROR_BAD_NETPATH                                          : return "(0x35) The network path was not found.";
			case ERROR_NETWORK_BUSY                                         : return "(0x36) The network is busy.";
			case ERROR_DEV_NOT_EXIST                                        : return "(0x37) The specified network resource or device is no longer available.";
			case ERROR_TOO_MANY_CMDS                                        : return "(0x38) The network BIOS command limit has been reached.";
			case ERROR_ADAP_HDW_ERR                                         : return "(0x39) A network adapter hardware error occurred.";
			case ERROR_BAD_NET_RESP                                         : return "(0x3A) The specified server cannot perform the requested operation.";
			case ERROR_UNEXP_NET_ERR                                        : return "(0x3B) An unexpected network error occurred.";
			case ERROR_BAD_REM_ADAP                                         : return "(0x3C) The remote adapter is not compatible.";
			case ERROR_PRINTQ_FULL                                          : return "(0x3D) The printer queue is full.";
			case ERROR_NO_SPOOL_SPACE                                       : return "(0x3E) Space to store the file waiting to be printed is not available on the server.";
			case ERROR_PRINT_CANCELLED                                      : return "(0x3F) Your file waiting to be printed was deleted.";
			case ERROR_NETNAME_DELETED                                      : return "(0x40) The specified network name is no longer available.";
			case ERROR_NETWORK_ACCESS_DENIED                                : return "(0x41) Network access is denied.";
			case ERROR_BAD_DEV_TYPE                                         : return "(0x42) The network resource type is not correct.";
			case ERROR_BAD_NET_NAME                                         : return "(0x43) The network name cannot be found.";
			case ERROR_TOO_MANY_NAMES                                       : return "(0x44) The name limit for the local computer network adapter card was exceeded.";
			case ERROR_TOO_MANY_SESS                                        : return "(0x45) The network BIOS session limit was exceeded.";
			case ERROR_SHARING_PAUSED                                       : return "(0x46) The remote server has been paused or is in the process of being started.";
			case ERROR_REQ_NOT_ACCEP                                        : return "(0x47) No more connections can be made to this remote computer at this time because there are already as many connections as the computer can accept.";
			case ERROR_REDIR_PAUSED                                         : return "(0x48) The specified printer or disk device has been paused.";
			case ERROR_FILE_EXISTS                                          : return "(0x50) The file exists.";
			case ERROR_CANNOT_MAKE                                          : return "(0x52) The directory or file cannot be created.";
			case ERROR_FAIL_I24                                             : return "(0x53) Fail on INT 24.";
			case ERROR_OUT_OF_STRUCTURES                                    : return "(0x54) Storage to process this request is not available.";
			case ERROR_ALREADY_ASSIGNED                                     : return "(0x55) The local device name is already in use.";
			case ERROR_INVALID_PASSWORD                                     : return "(0x56) The specified network password is not correct.";
			case ERROR_INVALID_PARAMETER                                    : return "(0x57) The parameter is incorrect.";
			case ERROR_NET_WRITE_FAULT                                      : return "(0x58) A write fault occurred on the network.";
			case ERROR_NO_PROC_SLOTS                                        : return "(0x59) The system cannot start another process at this time.";
			case ERROR_TOO_MANY_SEMAPHORES                                  : return "(0x64) Cannot create another system semaphore.";
			case ERROR_EXCL_SEM_ALREADY_OWNED                               : return "(0x65) The exclusive semaphore is owned by another process.";
			case ERROR_SEM_IS_SET                                           : return "(0x66) The semaphore is set and cannot be closed.";
			case ERROR_TOO_MANY_SEM_REQUESTS                                : return "(0x67) The semaphore cannot be set again.";
			case ERROR_INVALID_AT_INTERRUPT_TIME                            : return "(0x68) Cannot request exclusive semaphores at interrupt time.";
			case ERROR_SEM_OWNER_DIED                                       : return "(0x69) The previous ownership of this semaphore has ended.";
			case ERROR_SEM_USER_LIMIT                                       : return "(0x6A) Insert the diskette for drive %1.";
			case ERROR_DISK_CHANGE                                          : return "(0x6B) The program stopped because an alternate diskette was not inserted.";
			case ERROR_DRIVE_LOCKED                                         : return "(0x6C) The disk is in use or locked by another process.";
			case ERROR_BROKEN_PIPE                                          : return "(0x6D) The pipe has been ended.";
			case ERROR_OPEN_FAILED                                          : return "(0x6E) The system cannot open the device or file specified.";
			case ERROR_BUFFER_OVERFLOW                                      : return "(0x6F) The file name is too long.";
			case ERROR_DISK_FULL                                            : return "(0x70) There is not enough space on the disk.";
			case ERROR_NO_MORE_SEARCH_HANDLES                               : return "(0x71) No more internal file identifiers available.";
			case ERROR_INVALID_TARGET_HANDLE                                : return "(0x72) The target internal file identifier is incorrect.";
			case ERROR_INVALID_CATEGORY                                     : return "(0x75) The IOCTL call made by the application program is not correct.";
			case ERROR_INVALID_VERIFY_SWITCH                                : return "(0x76) The verify-on-write switch parameter value is not correct.";
			case ERROR_BAD_DRIVER_LEVEL                                     : return "(0x77) The system does not support the command requested.";
			case ERROR_CALL_NOT_IMPLEMENTED                                 : return "(0x78) This function is not supported on this system.";
			case ERROR_SEM_TIMEOUT                                          : return "(0x79) The semaphore timeout period has expired.";
			case ERROR_INSUFFICIENT_BUFFER                                  : return "(0x7A) The data area passed to a system call is too small.";
			case ERROR_INVALID_NAME                                         : return "(0x7B) The filename, directory name, or volume label syntax is incorrect.";
			case ERROR_INVALID_LEVEL                                        : return "(0x7C) The system call level is not correct.";
			case ERROR_NO_VOLUME_LABEL                                      : return "(0x7D) The disk has no volume label.";
			case ERROR_MOD_NOT_FOUND                                        : return "(0x7E) The specified module could not be found.";
			case ERROR_PROC_NOT_FOUND                                       : return "(0x7F) The specified procedure could not be found.";
			case ERROR_WAIT_NO_CHILDREN                                     : return "(0x80) There are no child processes to wait for.";
			case ERROR_CHILD_NOT_COMPLETE                                   : return "(0x81) The %1 application cannot be run in Win32 mode.";
			case ERROR_DIRECT_ACCESS_HANDLE                                 : return "(0x82) Attempt to use a file handle to an open disk partition for an operation other than raw disk I/O.";
			case ERROR_NEGATIVE_SEEK                                        : return "(0x83) An attempt was made to move the file pointer before the beginning of the file.";
			case ERROR_SEEK_ON_DEVICE                                       : return "(0x84) The file pointer cannot be set on the specified device or file.";
			case ERROR_IS_JOIN_TARGET                                       : return "(0x85) A JOIN or SUBST command cannot be used for a drive that contains previously joined drives.";
			case ERROR_IS_JOINED                                            : return "(0x86) An attempt was made to use a JOIN or SUBST command on a drive that has already been joined.";
			case ERROR_IS_SUBSTED                                           : return "(0x87) An attempt was made to use a JOIN or SUBST command on a drive that has already been substituted.";
			case ERROR_NOT_JOINED                                           : return "(0x88) The system tried to delete the JOIN of a drive that is not joined.";
			case ERROR_NOT_SUBSTED                                          : return "(0x89) The system tried to delete the substitution of a drive that is not substituted.";
			case ERROR_JOIN_TO_JOIN                                         : return "(0x8A) The system tried to join a drive to a directory on a joined drive.";
			case ERROR_SUBST_TO_SUBST                                       : return "(0x8B) The system tried to substitute a drive to a directory on a substituted drive.";
			case ERROR_JOIN_TO_SUBST                                        : return "(0x8C) The system tried to join a drive to a directory on a substituted drive.";
			case ERROR_SUBST_TO_JOIN                                        : return "(0x8D) The system tried to SUBST a drive to a directory on a joined drive.";
			case ERROR_BUSY_DRIVE                                           : return "(0x8E) The system cannot perform a JOIN or SUBST at this time.";
			case ERROR_SAME_DRIVE                                           : return "(0x8F) The system cannot join or substitute a drive to or for a directory on the same drive.";
			case ERROR_DIR_NOT_ROOT                                         : return "(0x90) The directory is not a subdirectory of the root directory.";
			case ERROR_DIR_NOT_EMPTY                                        : return "(0x91) The directory is not empty.";
			case ERROR_IS_SUBST_PATH                                        : return "(0x92) The path specified is being used in a substitute.";
			case ERROR_IS_JOIN_PATH                                         : return "(0x93) Not enough resources are available to process this command.";
			case ERROR_PATH_BUSY                                            : return "(0x94) The path specified cannot be used at this time.";
			case ERROR_IS_SUBST_TARGET                                      : return "(0x95) An attempt was made to join or substitute a drive for which a directory on the drive is the target of a previous substitute.";
			case ERROR_SYSTEM_TRACE                                         : return "(0x96) System trace information was not specified in your CONFIG.SYS file, or tracing is disallowed.";
			case ERROR_INVALID_EVENT_COUNT                                  : return "(0x97) The number of specified semaphore events for DosMuxSemWait is not correct.";
			case ERROR_TOO_MANY_MUXWAITERS                                  : return "(0x98) DosMuxSemWait did not execute; too many semaphores are already set.";
			case ERROR_INVALID_LIST_FORMAT                                  : return "(0x99) The DosMuxSemWait list is not correct.";
			case ERROR_LABEL_TOO_LONG                                       : return "(0x9A) The volume label you entered exceeds the label character limit of the target file system.";
			case ERROR_TOO_MANY_TCBS                                        : return "(0x9B) Cannot create another thread.";
			case ERROR_SIGNAL_REFUSED                                       : return "(0x9C) The recipient process has refused the signal.";
			case ERROR_DISCARDED                                            : return "(0x9D) The segment is already discarded and cannot be locked.";
			case ERROR_NOT_LOCKED                                           : return "(0x9E) The segment is already unlocked.";
			case ERROR_BAD_THREADID_ADDR                                    : return "(0x9F) The address for the thread ID is not correct.";
			case ERROR_BAD_ARGUMENTS                                        : return "(0xA0) One or more arguments are not correct.";
			case ERROR_BAD_PATHNAME                                         : return "(0xA1) The specified path is invalid.";
			case ERROR_SIGNAL_PENDING                                       : return "(0xA2) A signal is already pending.";
			case ERROR_MAX_THRDS_REACHED                                    : return "(0xA4) No more threads can be created in the system.";
			case ERROR_LOCK_FAILED                                          : return "(0xA7) Unable to lock a region of a file.";
			case ERROR_BUSY                                                 : return "(0xAA) The requested resource is in use.";
			case ERROR_DEVICE_SUPPORT_IN_PROGRESS                           : return "(0xAB) Device's command support detection is in progress.";
			case ERROR_CANCEL_VIOLATION                                     : return "(0xAD) A lock request was not outstanding for the supplied cancel region.";
			case ERROR_ATOMIC_LOCKS_NOT_SUPPORTED                           : return "(0xAE) The file system does not support atomic changes to the lock type.";
			case ERROR_INVALID_SEGMENT_NUMBER                               : return "(0xB4) The system detected a segment number that was not correct.";
			case ERROR_INVALID_ORDINAL                                      : return "(0xB6) The operating system cannot run %1.";
			case ERROR_ALREADY_EXISTS                                       : return "(0xB7) Cannot create a file when that file already exists.";
			case ERROR_INVALID_FLAG_NUMBER                                  : return "(0xBA) The flag passed is not correct.";
			case ERROR_SEM_NOT_FOUND                                        : return "(0xBB) The specified system semaphore name was not found.";
			case ERROR_INVALID_STARTING_CODESEG                             : return "(0xBC) The operating system cannot run %1.";
			case ERROR_INVALID_STACKSEG                                     : return "(0xBD) The operating system cannot run %1.";
			case ERROR_INVALID_MODULETYPE                                   : return "(0xBE) The operating system cannot run %1.";
			case ERROR_INVALID_EXE_SIGNATURE                                : return "(0xBF) Cannot run %1 in Win32 mode.";
			case ERROR_EXE_MARKED_INVALID                                   : return "(0xC0) The operating system cannot run %1.";
			case ERROR_BAD_EXE_FORMAT                                       : return "(0xC1) %1 is not a valid Win32 application.";
			case ERROR_ITERATED_DATA_EXCEEDS_64k                            : return "(0xC2) The operating system cannot run %1.";
			case ERROR_INVALID_MINALLOCSIZE                                 : return "(0xC3) The operating system cannot run %1.";
			case ERROR_DYNLINK_FROM_INVALID_RING                            : return "(0xC4) The operating system cannot run this application program.";
			case ERROR_IOPL_NOT_ENABLED                                     : return "(0xC5) The operating system is not presently configured to run this application.";
			case ERROR_INVALID_SEGDPL                                       : return "(0xC6) The operating system cannot run %1.";
			case ERROR_AUTODATASEG_EXCEEDS_64k                              : return "(0xC7) The operating system cannot run this application program.";
			case ERROR_RING2SEG_MUST_BE_MOVABLE                             : return "(0xC8) The code segment cannot be greater than or equal to 64K.";
			case ERROR_RELOC_CHAIN_XEEDS_SEGLIM                             : return "(0xC9) The operating system cannot run %1.";
			case ERROR_INFLOOP_IN_RELOC_CHAIN                               : return "(0xCA) The operating system cannot run %1.";
			case ERROR_ENVVAR_NOT_FOUND                                     : return "(0xCB) The system could not find the environment option that was entered.";
			case ERROR_NO_SIGNAL_SENT                                       : return "(0xCD) No process in the command subtree has a signal handler.";
			case ERROR_FILENAME_EXCED_RANGE                                 : return "(0xCE) The filename or extension is too long.";
			case ERROR_RING2_STACK_IN_USE                                   : return "(0xCF) The ring 2 stack is in use.";
			case ERROR_META_EXPANSION_TOO_LONG                              : return "(0xD0) The global filename characters, * or ?, are entered incorrectly or too many global filename characters are specified.";
			case ERROR_INVALID_SIGNAL_NUMBER                                : return "(0xD1) The signal being posted is not correct.";
			case ERROR_THREAD_1_INACTIVE                                    : return "(0xD2) The signal handler cannot be set.";
			case ERROR_LOCKED                                               : return "(0xD4) The segment is locked and cannot be reallocated.";
			case ERROR_TOO_MANY_MODULES                                     : return "(0xD6) Too many dynamic-link modules are attached to this program or dynamic-link module.";
			case ERROR_NESTING_NOT_ALLOWED                                  : return "(0xD7) Cannot nest calls to LoadModule.";
			case ERROR_EXE_MACHINE_TYPE_MISMATCH                            : return "(0xD8) This version of %1 is not compatible with the version of Windows you're running. Check your computer's system information and then contact the software publisher.";
			case ERROR_EXE_CANNOT_MODIFY_SIGNED_BINARY                      : return "(0xD9) The image file %1 is signed, unable to modify.";
			case ERROR_EXE_CANNOT_MODIFY_STRONG_SIGNED_BINARY               : return "(0xDA) The image file %1 is strong signed, unable to modify.";
			case ERROR_FILE_CHECKED_OUT                                     : return "(0xDC) This file is checked out or locked for editing by another user.";
			case ERROR_CHECKOUT_REQUIRED                                    : return "(0xDD) The file must be checked out before saving changes.";
			case ERROR_BAD_FILE_TYPE                                        : return "(0xDE) The file type being saved or retrieved has been blocked.";
			case ERROR_FILE_TOO_LARGE                                       : return "(0xDF) The file size exceeds the limit allowed and cannot be saved.";
			case ERROR_FORMS_AUTH_REQUIRED                                  : return "(0xE0) Access Denied. Before opening files in this location, you must first add the web site to your trusted sites list, browse to the web site, and select the option to login automatically.";
			case ERROR_VIRUS_INFECTED                                       : return "(0xE1) Operation did not complete successfully because the file contains a virus or potentially unwanted software.";
			case ERROR_VIRUS_DELETED                                        : return "(0xE2) This file contains a virus or potentially unwanted software and cannot be opened. Due to the nature of this virus or potentially unwanted software, the file has been removed from this location.";
			case ERROR_PIPE_LOCAL                                           : return "(0xE5) The pipe is local.";
			case ERROR_BAD_PIPE                                             : return "(0xE6) The pipe state is invalid.";
			case ERROR_PIPE_BUSY                                            : return "(0xE7) All pipe instances are busy.";
			case ERROR_NO_DATA                                              : return "(0xE8) The pipe is being closed.";
			case ERROR_PIPE_NOT_CONNECTED                                   : return "(0xE9) No process is on the other end of the pipe.";
			case ERROR_MORE_DATA                                            : return "(0xEA) More data is available.";
			case ERROR_VC_DISCONNECTED                                      : return "(0xF0) The session was canceled.";
			case ERROR_INVALID_EA_NAME                                      : return "(0xFE) The specified extended attribute name was invalid.";
			case ERROR_EA_LIST_INCONSISTENT                                 : return "(0xFF) The extended attributes are inconsistent.";
			case WAIT_TIMEOUT                                               : return "(0x102) The wait operation timed out.";
			case ERROR_NO_MORE_ITEMS                                        : return "(0x103) No more data is available.";
			case ERROR_CANNOT_COPY                                          : return "(0x10A) The copy functions cannot be used.";
			case ERROR_DIRECTORY                                            : return "(0x10B) The directory name is invalid.";
			case ERROR_EAS_DIDNT_FIT                                        : return "(0x113) The extended attributes did not fit in the buffer.";
			case ERROR_EA_FILE_CORRUPT                                      : return "(0x114) The extended attribute file on the mounted file system is corrupt.";
			case ERROR_EA_TABLE_FULL                                        : return "(0x115) The extended attribute table file is full.";
			case ERROR_INVALID_EA_HANDLE                                    : return "(0x116) The specified extended attribute handle is invalid.";
			case ERROR_EAS_NOT_SUPPORTED                                    : return "(0x11A) The mounted file system does not support extended attributes.";
			case ERROR_NOT_OWNER                                            : return "(0x120) Attempt to release mutex not owned by caller.";
			case ERROR_TOO_MANY_POSTS                                       : return "(0x12A) Too many posts were made to a semaphore.";
			case ERROR_PARTIAL_COPY                                         : return "(0x12B) Only part of a ReadProcessMemory or WriteProcessMemory request was completed.";
			case ERROR_OPLOCK_NOT_GRANTED                                   : return "(0x12C) The oplock request is denied.";
			case ERROR_INVALID_OPLOCK_PROTOCOL                              : return "(0x12D) An invalid oplock acknowledgment was received by the system.";
			case ERROR_DISK_TOO_FRAGMENTED                                  : return "(0x12E) The volume is too fragmented to complete this operation.";
			case ERROR_DELETE_PENDING                                       : return "(0x12F) The file cannot be opened because it is in the process of being deleted.";
			case ERROR_INCOMPATIBLE_WITH_GLOBAL_SHORT_NAME_REGISTRY_SETTING : return "(0x130) Short name settings may not be changed on this volume due to the global registry setting.";
			case ERROR_SHORT_NAMES_NOT_ENABLED_ON_VOLUME                    : return "(0x131) Short names are not enabled on this volume.";
			case ERROR_SECURITY_STREAM_IS_INCONSISTENT                      : return "(0x132) The security stream for the given volume is in an inconsistent state. Please run CHKDSK on the volume.";
			case ERROR_INVALID_LOCK_RANGE                                   : return "(0x133) A requested file lock operation cannot be processed due to an invalid byte range.";
			case ERROR_IMAGE_SUBSYSTEM_NOT_PRESENT                          : return "(0x134) The subsystem needed to support the image type is not present.";
			case ERROR_NOTIFICATION_GUID_ALREADY_DEFINED                    : return "(0x135) The specified file already has a notification GUID associated with it.";
			case ERROR_INVALID_EXCEPTION_HANDLER                            : return "(0x136) An invalid exception handler routine has been detected.";
			case ERROR_DUPLICATE_PRIVILEGES                                 : return "(0x137) Duplicate privileges were specified for the token.";
			case ERROR_NO_RANGES_PROCESSED                                  : return "(0x138) No ranges for the specified operation were able to be processed.";
			case ERROR_NOT_ALLOWED_ON_SYSTEM_FILE                           : return "(0x139) Operation is not allowed on a file system internal file.";
			case ERROR_DISK_RESOURCES_EXHAUSTED                             : return "(0x13A) The physical resources of this disk have been exhausted.";
			case ERROR_INVALID_TOKEN                                        : return "(0x13B) The token representing the data is invalid.";
			case ERROR_DEVICE_FEATURE_NOT_SUPPORTED                         : return "(0x13C) The device does not support the command feature.";
			case ERROR_MR_MID_NOT_FOUND                                     : return "(0x13D) The system cannot find message text for message number 0x%1 in the message file for %2.";
			case ERROR_SCOPE_NOT_FOUND                                      : return "(0x13E) The scope specified was not found.";
			case ERROR_UNDEFINED_SCOPE                                      : return "(0x13F) The Central Access Policy specified is not defined on the target machine.";
			case ERROR_INVALID_CAP                                          : return "(0x140) The Central Access Policy obtained from Active Directory is invalid.";
			case ERROR_DEVICE_UNREACHABLE                                   : return "(0x141) The device is unreachable.";
			case ERROR_DEVICE_NO_RESOURCES                                  : return "(0x142) The target device has insufficient resources to complete the operation.";
			case ERROR_DATA_CHECKSUM_ERROR                                  : return "(0x143) A data integrity checksum error occurred. Data in the file stream is corrupt.";
			case ERROR_INTERMIXED_KERNEL_EA_OPERATION                       : return "(0x144) An attempt was made to modify both a KERNEL and normal Extended Attribute (EA) in the same operation.";
			case ERROR_FILE_LEVEL_TRIM_NOT_SUPPORTED                        : return "(0x146) Device does not support file-level TRIM.";
			case ERROR_OFFSET_ALIGNMENT_VIOLATION                           : return "(0x147) The command specified a data offset that does not align to the device's granularity/alignment.";
			case ERROR_INVALID_FIELD_IN_PARAMETER_LIST                      : return "(0x148) The command specified an invalid field in its parameter list.";
			case ERROR_OPERATION_IN_PROGRESS                                : return "(0x149) An operation is currently in progress with the device.";
			case ERROR_BAD_DEVICE_PATH                                      : return "(0x14A) An attempt was made to send down the command via an invalid path to the target device.";
			case ERROR_TOO_MANY_DESCRIPTORS                                 : return "(0x14B) The command specified a number of descriptors that exceeded the maximum supported by the device.";
			case ERROR_SCRUB_DATA_DISABLED                                  : return "(0x14C) Scrub is disabled on the specified file.";
			case ERROR_NOT_REDUNDANT_STORAGE                                : return "(0x14D) The storage device does not provide redundancy.";
			case ERROR_RESIDENT_FILE_NOT_SUPPORTED                          : return "(0x14E) An operation is not supported on a resident file.";
			case ERROR_COMPRESSED_FILE_NOT_SUPPORTED                        : return "(0x14F) An operation is not supported on a compressed file.";
			case ERROR_DIRECTORY_NOT_SUPPORTED                              : return "(0x150) An operation is not supported on a directory.";
			case ERROR_NOT_READ_FROM_COPY                                   : return "(0x151) The specified copy of the requested data could not be read.";
			case ERROR_FAIL_NOACTION_REBOOT                                 : return "(0x15E) No action was taken as a system reboot is required.";
			case ERROR_FAIL_SHUTDOWN                                        : return "(0x15F) The shutdown operation failed.";
			case ERROR_FAIL_RESTART                                         : return "(0x160) The restart operation failed.";
			case ERROR_MAX_SESSIONS_REACHED                                 : return "(0x161) The maximum number of sessions has been reached.";
			case ERROR_THREAD_MODE_ALREADY_BACKGROUND                       : return "(0x190) The thread is already in background processing mode.";
			case ERROR_THREAD_MODE_NOT_BACKGROUND                           : return "(0x191) The thread is not in background processing mode.";
			case ERROR_PROCESS_MODE_ALREADY_BACKGROUND                      : return "(0x192) The process is already in background processing mode.";
			case ERROR_PROCESS_MODE_NOT_BACKGROUND                          : return "(0x193) The process is not in background processing mode.";
			case ERROR_INVALID_ADDRESS                                      : return "(0x1E7) Attempt to access invalid address.";
			}
		}
		#endregion

		#region Window Native Structs

		[StructLayout(LayoutKind.Sequential)]
		public struct RECT
		{
			public int left;
			public int top;
			public int right;
			public int bottom;

			public int Width()                                  { return right - left; }
			public int Height()                                 { return bottom - top; }
			public static RECT FromRectangle(Rectangle rect)    { return new RECT{left=rect.Left, top=rect.Top, right=rect.Right, bottom=rect.Bottom}; }
			public Rectangle   ToRectangle()                    { return new Rectangle(left, top, Width(), Height()); }
			public static RECT FromSize(Size size)              { return new RECT{left=0, top=0, right=size.Width, bottom=size.Height}; }
			public Size        ToSize()                         { return new Size(right - left, bottom - top); }
		};

		[StructLayout(LayoutKind.Sequential)]
		public struct POINT
		{
			public int X;
			public int Y;
			public static POINT FromPoint(Point pt)             { return new POINT{X=pt.X, Y=pt.Y}; }
			public Point ToPoint()                              { return new Point(X, Y); }
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct SCROLLINFO
		{
			public uint cbSize;
			public uint fMask;
			public int nMin;
			public int nMax;
			public uint nPage;
			public int nPos;
			public int nTrackPos;
			public static SCROLLINFO Default                     { get {return new SCROLLINFO{cbSize = (uint)Marshal.SizeOf(typeof(SCROLLINFO))};} }
		}

		// Structure pointed to by WM_GETMINMAXINFO lParam
		[StructLayout(LayoutKind.Sequential)]
		public struct MINMAXINFO
		{
			public POINT ptReserved;
			public POINT ptMaxSize;
			public POINT ptMaxPosition;
			public POINT ptMinTrackSize;
			public POINT ptMaxTrackSize;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct  XFORM
		{
			public float eM11;
			public float eM12;
			public float eM21;
			public float eM22;
			public float eDx;
			public float eDy;
		}

		// Used by the FindFirstFile or FindNextFile functions.
		[Serializable, StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto), BestFitMapping(false)]
		public class WIN32_FIND_DATA
		{
			public FileAttributes dwFileAttributes;
			public uint ftCreationTime_dwLowDateTime;
			public uint ftCreationTime_dwHighDateTime;
			public uint ftLastAccessTime_dwLowDateTime;
			public uint ftLastAccessTime_dwHighDateTime;
			public uint ftLastWriteTime_dwLowDateTime;
			public uint ftLastWriteTime_dwHighDateTime;
			public uint nFileSizeHigh;
			public uint nFileSizeLow;
			public int dwReserved0;
			public int dwReserved1;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string cFileName;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)] public string cAlternateFileName;

			/// <summary>The filename.extn of the file</summary>
			public string FileName { get { return cFileName; } }

			/// <summary>Attributes of the file.</summary>
			public FileAttributes Attributes { get { return dwFileAttributes; } }

			/// <summary>The file size</summary>
			public long FileSize { get { return ToLong(nFileSizeHigh, nFileSizeLow); } }

			/// <summary>File creation time in local time</summary>
			public DateTime CreationTime { get { return CreationTimeUtc.ToLocalTime(); } }

			/// <summary>File creation time in UTC</summary>
			public DateTime CreationTimeUtc { get { return ToDateTime(ftCreationTime_dwHighDateTime, ftCreationTime_dwLowDateTime); } }

			/// <summary>Gets the last access time in local time.</summary>
			public DateTime LastAccessTime { get { return LastAccessTimeUtc.ToLocalTime(); } }

			/// <summary>File last access time in UTC</summary>
			public DateTime LastAccessTimeUtc { get { return ToDateTime(ftLastAccessTime_dwHighDateTime, ftLastAccessTime_dwLowDateTime); } }

			/// <summary>Gets the last access time in local time.</summary>
			public DateTime LastWriteTime { get { return LastWriteTimeUtc.ToLocalTime(); } }

			/// <summary>File last write time in UTC</summary>
			public DateTime LastWriteTimeUtc { get { return ToDateTime(ftLastWriteTime_dwHighDateTime, ftLastWriteTime_dwLowDateTime); } }

			public override string ToString()                       { return cFileName; }
			private static DateTime ToDateTime(uint high, uint low) { return DateTime.FromFileTimeUtc(ToLong(high,low)); }
			private static long ToLong(uint high, uint low)         { return ((long)high << 0x20) | low; }
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct RM_UNIQUE_PROCESS
		{
			public int dwProcessId;
			public System.Runtime.InteropServices.ComTypes.FILETIME ProcessStartTime;
		}

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		public struct RM_PROCESS_INFO
		{
			public RM_UNIQUE_PROCESS Process;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCH_RM_MAX_APP_NAME + 1)] public string strAppName;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = CCH_RM_MAX_SVC_NAME + 1)] public string strServiceShortName;
			public RM_APP_TYPE ApplicationType;
			public uint AppStatus;
			public uint TSSessionId;
			[MarshalAs(UnmanagedType.Bool)] public bool bRestartable;
		}

		#endregion

		/// <summary>Helper method for loading a dll from a platform specific path</summary>
		public static IntPtr LoadDll(string dllname, string dir = @".\lib\$(platform)")
		{
			Func<string,IntPtr> TryLoad = path =>
				{
					var module = LoadLibrary(path);
					if (module != IntPtr.Zero)
						return module;
				
					var msg = GetLastErrorString();
					throw new Exception("Found dependent library '{0}' but it failed to load.\r\nLast Error: {1}".Fmt(path, msg));
				};

			var searched = new List<string>();

			// Search the local directory first
			var dllpath = searched.Add2(Path.GetFullPath(dllname));
			if (PathEx.FileExists(dllpath))
				TryLoad(dllpath);

			// Try the 'dir' folder. Load the appropriate dll for the platform
			dllpath = searched.Add2(Path.GetFullPath(Path.Combine(dir.Replace("$(platform)", Environment.Is64BitProcess ? "x64" : "x86"), dllname)));
			if (PathEx.FileExists(dllpath))
				TryLoad(dllpath);

			throw new DllNotFoundException("Could not find dependent library '{0}'\r\nLocations searched:\r\n{1}".Fmt(dllname, string.Join("\r\n", searched.ToArray())));
		}

		/// <summary>Returns the upper 16bits of a 32bit dword such as LPARAM or WPARAM</summary>
		public static uint GetHiword(int dword)
		{
			return ((uint)dword >> 16 & 0xFFFF);
		}

		/// <summary>Returns the lower 16bits of a 32bit dword such as LPARAM or WPARAM</summary>
		public static uint GetLoword(int dword)
		{
			return ((uint)dword & 0xFFFF);
		}

		/// <summary>Test the async state of a key</summary>
		public static bool KeyDown(Keys vkey)
		{
			return (GetAsyncKeyState(vkey) & 0x8000) != 0;
		}

		/// <summary>Detect a key press using its async state</summary>
		public static bool KeyPress(Keys vkey)
		{
			if   (!KeyDown(vkey)) return false;
			while (KeyDown(vkey)) Thread.Sleep(10);
			return true;
		}

		/// <summary>Convert a 'Keys' key value to a unicode char</summary>
		public static char CharFromVKey(Keys vkey)
		{
			char ch = ' ';
			var keyboardState = new byte[256];
			GetKeyboardState(keyboardState);

			var scan_code = MapVirtualKey((uint)vkey, MAPVK_VK_TO_VSC);
			var sb = new StringBuilder(2);
			var r = ToUnicode((uint)vkey, scan_code, keyboardState, sb, sb.Capacity, 0);
			switch (r)
			{
			default: ch = sb[0]; break;
			case 0: case -1: break;
			}
			return ch;
		}
		public static char CharFromVKey(System.Windows.Input.Key vkey)
		{
			return CharFromVKey((Keys)System.Windows.Input.KeyInterop.VirtualKeyFromKey(vkey));
		}

		/// <summary>Pack a Point into an LPARAM</summary>
		public static IntPtr PointToLParam(Point pt)
		{
			return new IntPtr((((pt.Y & 0xffff) << 16) | (pt.X & 0xffff)));
		}

		/// <summary>Unpack a Point from an LPARAM</summary>
		public static Point LParamToPoint(IntPtr lparam)
		{
			return new Point(lparam.ToInt32() & 0xffff, lparam.ToInt32() >> 16);
		}

		// Return the window under a screen space point
		public static HWND WindowFromPoint(Point pt)
		{
			return WindowFromPoint(POINT.FromPoint(pt));
		}

		public static int GetScrollBarPos(IntPtr hWnd, int nBar)
		{
			SCROLLINFO info = SCROLLINFO.Default;
			info.fMask = (int)ScrollInfoMask.SIF_POS;
			GetScrollInfo(hWnd, nBar, ref info);
			return info.nPos;
		}
		public static void SetScrollBarPos(IntPtr hWnd, int nBar, int nPos, bool bRedraw)
		{
			SCROLLINFO info = SCROLLINFO.Default;
			info.fMask = (int)ScrollInfoMask.SIF_POS;
			info.nPos = nPos;
			SetScrollInfo(hWnd, nBar, ref info, bRedraw);
		}

		public delegate int HookProc(int nCode, int wParam, IntPtr lParam);

		/// <summary>Returns the last error set by the last called native function with the "SetLastError" attribute</summary>
		public static uint GetLastError()
		{
			return unchecked((uint)Marshal.GetLastWin32Error());
		}
		public static string GetLastErrorString()
		{
			return new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error()).Message;
		}

		// Kernel32
		[DllImport("Kernel32.dll", SetLastError = true)] public static extern IntPtr LoadLibrary(string path);
		[DllImport("Kernel32.dll", SetLastError = true)] public static extern bool   FreeLibrary(IntPtr module);
		[DllImport("kernel32.dll", SetLastError = true)] public static extern bool   AllocConsole();
		[DllImport("kernel32.dll", SetLastError = true)] public static extern bool   FreeConsole();
		[DllImport("kernel32.dll", SetLastError = true)] public static extern bool   AttachConsole(int dwProcessId);
		[DllImport("kernel32.dll", SetLastError = true)] public static extern bool   WriteConsole(IntPtr hConsoleOutput, string lpBuffer, uint nNumberOfCharsToWrite, out uint lpNumberOfCharsWritten, IntPtr lpReserved);
		[DllImport("kernel32.dll", SetLastError = true)] public static extern uint   FormatMessage(uint dwFlags, IntPtr lpSource, uint dwMessageId, uint dwLanguageId, ref IntPtr lpBuffer, uint nSize, IntPtr pArguments);

		// User32 (in alphabetical order)
		[DllImport("user32.dll", EntryPoint="AppendMenuW", CharSet=CharSet.Unicode)] public static extern bool   AppendMenu(IntPtr hMenu, uint uFlags, int uIDNewItem, string lpNewItem);
		[DllImport("user32.dll", EntryPoint="AppendMenuW", CharSet=CharSet.Unicode)] public static extern bool   AppendMenu(IntPtr hMenu, uint uFlags, IntPtr uIDNewItem, string lpNewItem);
		[DllImport("user32.dll")]                                                    public static extern IntPtr AttachThreadInput(IntPtr idAttach, IntPtr idAttachTo, int fAttach);
		[DllImport("user32.dll")]                                                    public static extern int    CallNextHookEx(int idHook, int nCode, int wParam, IntPtr lParam);
		[DllImport("user32.dll", EntryPoint="CheckMenuItem")]                        public static extern int    CheckMenuItem(IntPtr hMenu,int uIDCheckItem, int uCheck);
		[DllImport("user32.dll")]                                                    public static extern IntPtr CreatePopupMenu();
		[DllImport("user32.dll", SetLastError = true, CharSet=CharSet.Unicode)]      public static extern IntPtr CreateWindowEx(int dwExStyle, string lpClassName, string lpWindowName, int dwStyle, int x, int y, int nWidth, int nHeight, IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);
		[DllImport("user32.dll", CharSet=CharSet.Unicode)]                           public static extern bool   DestroyWindow(IntPtr hwnd);
		[DllImport("user32.dll")]                                                    public static extern short  GetAsyncKeyState(Keys vKey);
		[DllImport("user32.dll")]                                                    public static extern bool   GetClientRect(HWND hwnd, out RECT rect);
		[DllImport("user32.dll")]                                                    public static extern int    GetDoubleClickTime();
		[DllImport("user32.dll")]                                                    public static extern HWND   GetFocus();
		[DllImport("user32.dll")]                                                    public static extern HWND   GetForegroundWindow();
		[DllImport("user32.dll")]                                                    public static extern int    GetKeyboardState(byte[] pbKeyState);
		[DllImport("user32.dll")]                                                    public static extern short  GetKeyState(int vKey);
		[DllImport("user32.dll")]                                                    public static extern bool   GetScrollInfo(HWND hwnd, int BarType, ref SCROLLINFO lpsi);
		[DllImport("user32.dll")]                                                    public static extern int    GetScrollPos(HWND hWnd, int nBar);
		[DllImport("user32.dll")]                                                    public static extern IntPtr GetSystemMenu(HWND hwnd, bool bRevert);
		[DllImport("user32.dll", SetLastError=true)]                                 public static extern uint   GetWindowLong(HWND hWnd, int nIndex);
		[DllImport("user32.dll")]                                                    public static extern bool   GetWindowRect(HWND hwnd, out RECT rect);
		[DllImport("user32.dll", SetLastError = true)]                               public static extern IntPtr GetWindowThreadProcessId(HWND hWnd, ref IntPtr lpdwProcessId);
		[DllImport("user32.dll")]                                                    public static extern int    HideCaret(IntPtr hwnd);
		[DllImport("user32.dll", EntryPoint="InsertMenu", CharSet=CharSet.Unicode)]  public static extern bool   InsertMenu(IntPtr hMenu, int wPosition, int wFlags, int wIDNewItem, string lpNewItem);
		[DllImport("user32.dll", EntryPoint="InsertMenu", CharSet=CharSet.Unicode)]  public static extern bool   InsertMenu(IntPtr hMenu, int wPosition, int wFlags, IntPtr wIDNewItem, string lpNewItem);
		[DllImport("user32.dll")]                                                    public static extern bool   IsIconic(HWND hwnd);
		[DllImport("user32.dll")]                                                    public static extern bool   IsWindow(HWND hwnd);
		[DllImport("user32.dll")]                                                    public static extern bool   IsWindowVisible(HWND hwnd);
		[DllImport("user32.dll")]                                                    public static extern bool   IsZoomed(HWND hwnd);
		[DllImport("user32.dll")]                                                    public static extern bool   LockWindowUpdate(HWND hWndLock);
		[DllImport("user32.dll")]                                                    public static extern uint   MapVirtualKey(uint uCode, uint uMapType);
		[DllImport("user32.dll")]                                                    public static extern bool   MoveWindow(HWND hWnd, int X, int Y, int nWidth, int nHeight, bool repaint);
		[DllImport("user32.dll", EntryPoint="PeekMessage", CharSet=CharSet.Auto)]    public static extern bool   PeekMessage(out Message msg, IntPtr hWnd, uint messageFilterMin, uint messageFilterMax, uint flags);
		[DllImport("user32.dll", EntryPoint="PostThreadMessage")]                    public static extern int    PostThreadMessage(int idThread, uint msg, int wParam, int lParam);
		[DllImport("user32.dll", EntryPoint="SendMessage")]                          public static extern int    SendMessage(HWND hwnd, uint msg, int wparam, int lparam);
		[DllImport("user32.dll", EntryPoint="SendMessage")]                          public static extern int    SendMessage(HWND hwnd, uint msg, IntPtr wparam, IntPtr lparam);
		[DllImport("user32.dll")]                                                    public static extern HWND   SetFocus(HWND hwnd);
		[DllImport("user32.dll")]                                                    public static extern bool   SetForegroundWindow(HWND hwnd);
		[DllImport("user32.dll")]                                                    public static extern IntPtr SetParent(HWND hWndChild, HWND hWndNewParent);
		[DllImport("user32.dll")]                                                    public static extern int    SetScrollInfo(HWND hwnd, int fnBar, ref SCROLLINFO lpsi, bool fRedraw);
		[DllImport("user32.dll")]                                                    public static extern int    SetScrollPos(HWND hWnd, int nBar, int nPos, bool bRedraw);
		[DllImport("user32.dll")]                                                    public static extern int    SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, int dwThreadId);
		[DllImport("user32.dll")]                                                    public static extern int    SetWindowLong(HWND hWnd, int nIndex, uint dwNewLong);
		[DllImport("user32.dll")]                                                    public static extern bool   SetWindowPos(HWND hWnd, HWND hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
		[DllImport("user32.dll")]                                                    public static extern bool   ShowWindow(IntPtr hWnd, int nCmdShow);
		[DllImport("user32.dll")]                                                    public static extern bool   ShowWindowAsync(HWND hwnd, int nCmdShow);
		[DllImport("user32.dll")]                                                    public static extern int    ToAscii(int uVirtKey, int uScanCode, byte[] lpbKeyState, byte[] lpwTransKey, int fuState);
		[DllImport("user32.dll")]                                                    public static extern int    ToUnicode(uint wVirtKey, uint wScanCode, byte[] lpKeyState, [Out, MarshalAs(UnmanagedType.LPWStr, SizeParamIndex=4)] StringBuilder pwszBuff, int cchBuff, uint wFlags);
		[DllImport("user32.dll")]                                                    public static extern int    UnhookWindowsHookEx(int idHook);
		[DllImport("user32.dll")]                                                    public static extern HWND   WindowFromPoint(POINT Point);

		// gdi32
		[DllImport("gdi32.dll")] public static extern int  SetGraphicsMode(IntPtr hdc, int iGraphicsMode);
		[DllImport("gdi32.dll")] public static extern int  SetMapMode(IntPtr hdc, int fnMapMode);
		[DllImport("gdi32.dll")] public static extern bool SetWorldTransform(IntPtr hdc, ref XFORM lpXform);
		[DllImport("gdi32.dll")] public static extern bool DeleteObject(IntPtr hObject);

		// ole32
		[DllImport("ole32.dll")] public static extern void CoTaskMemFree(IntPtr ptr);

		// Restart manager
		[DllImport("rstrtmgr.dll", CharSet = CharSet.Unicode)] public static extern int RmRegisterResources(uint pSessionHandle, UInt32 nFiles, string[] rgsFilenames, UInt32 nApplications, [In] RM_UNIQUE_PROCESS[] rgApplications, UInt32 nServices, string[] rgsServiceNames);
		[DllImport("rstrtmgr.dll", CharSet = CharSet.Auto)]    public static extern int RmStartSession(out uint pSessionHandle, int dwSessionFlags, string strSessionKey);
		[DllImport("rstrtmgr.dll")]                            public static extern int RmEndSession(uint pSessionHandle);
		[DllImport("rstrtmgr.dll")]                            public static extern int RmGetList(uint dwSessionHandle, out uint pnProcInfoNeeded, ref uint pnProcInfo, [In, Out] RM_PROCESS_INFO[] rgAffectedApps, ref uint lpdwRebootReasons);
	}
}
