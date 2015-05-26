//***************************************************
// Win32 wrapper
//  Copyright (c) Rylogic Ltd 2008
//***************************************************

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using pr.common;
using pr.extn;

namespace pr.win32
{
	using HWND = System.IntPtr;

	/// <summary>Win32 wrapper</summary>
	[System.Security.SuppressUnmanagedCodeSecurity] // We won't use this maliciously
	public static partial class Win32
	{
		#region Windows Versions
		public static bool IsWindowsVistaOrLater
		{
			get { return Environment.OSVersion.Platform == PlatformID.Win32NT && Environment.OSVersion.Version >= new Version(6, 0, 6000); }
		}
		public static bool IsWindowsXPOrLater
		{
			get { return Environment.OSVersion.Platform == PlatformID.Win32NT && Environment.OSVersion.Version >= new Version(5, 1, 2600); }
		}
		#endregion

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

		#region Common Control Messages CCM_
		public const uint CCM_FIRST               = 0x2000;    // Common control shared messages
		public const uint CCM_LAST                = (CCM_FIRST + 0x200);
		public const uint CCM_SETBKCOLOR          = (CCM_FIRST + 0x1); // lParam is bkColor
		public const uint CCM_SETCOLORSCHEME      = (CCM_FIRST + 0x2); // lParam is color scheme
		public const uint CCM_GETCOLORSCHEME      = (CCM_FIRST + 0x3); // fills in COLORSCHEME pointed to by lParam
		public const uint CCM_GETDROPTARGET       = (CCM_FIRST + 0x4);
		public const uint CCM_SETUNICODEFORMAT    = (CCM_FIRST + 0x5);
		public const uint CCM_GETUNICODEFORMAT    = (CCM_FIRST + 0x6);
		public const uint CCM_SETVERSION          = (CCM_FIRST + 0x7);
		public const uint CCM_GETVERSION          = (CCM_FIRST + 0x8);
		public const uint CCM_SETNOTIFYWINDOW     = (CCM_FIRST + 0x9); // wParam == hwndParent.
		public const uint CCM_SETWINDOWTHEME      = (CCM_FIRST + 0xb);
		public const uint CCM_DPISCALE            = (CCM_FIRST + 0xc); // wParam == Awareness
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

		#region Progress Bar
		public static class ProgressBar
		{
			public const uint PBS_SMOOTH              = 0x01;
			public const uint PBS_VERTICAL            = 0x04;
			public const uint PBS_MARQUEE             = 0x08;
			public const uint PBS_SMOOTHREVERSE       = 0x10;

			public const uint PBM_SETRANGE            = (WM_USER+1);
			public const uint PBM_SETPOS              = (WM_USER+2);
			public const uint PBM_DELTAPOS            = (WM_USER+3);
			public const uint PBM_SETSTEP             = (WM_USER+4);
			public const uint PBM_STEPIT              = (WM_USER+5);
			public const uint PBM_SETRANGE32          = (WM_USER+6);  // lParam = high, wParam = low

			public const uint PBM_GETRANGE            = (WM_USER+7);    // wParam = return (TRUE ? low : high). lParam = PPBRANGE or NULL
			public const uint PBM_GETPOS              = (WM_USER+8);
			public const uint PBM_SETBARCOLOR         = (WM_USER+9);    // lParam = bar color
			public const uint PBM_SETMARQUEE          = (WM_USER+10);
			public const uint PBM_GETSTEP             = (WM_USER+13);
			public const uint PBM_GETBKCOLOR          = (WM_USER+14);
			public const uint PBM_GETBARCOLOR         = (WM_USER+15);
			public const uint PBM_SETSTATE            = (WM_USER+16); // wParam = PBST_[State] (NORMAL, ERROR, PAUSED)
			public const uint PBM_GETSTATE            = (WM_USER+17);
			public const uint PBM_SETBKCOLOR          = CCM_SETBKCOLOR; // lParam = bkColor

			public const int PBST_NORMAL = 0x0001;
			public const int PBST_ERROR  = 0x0002;
			public const int PBST_PAUSED = 0x0003;
		}
		#endregion

		#region Window Styles WS_
		public const int WS_POPUP                         = unchecked((int)0x80000000); // The windows is a pop-up window. This style cannot be used with the WS_CHILD style.
		public const int WS_CHILD                         = unchecked(0x40000000); // The window is a child window. A window with this style cannot have a menu bar. This style cannot be used with the WS_POPUP style.
		public const int WS_CHILDWINDOW                   = unchecked(0x40000000); // Same as the WS_CHILD style.
		public const int WS_ICONIC                        = unchecked(0x20000000); // The window is initially minimized. Same as the WS_MINIMIZE style.
		public const int WS_MINIMIZE                      = unchecked(0x20000000); // The window is initially minimized. Same as the WS_ICONIC style.
		public const int WS_VISIBLE                       = unchecked(0x10000000); // The window is initially visible. This style can be turned on and off by using the ShowWindow or SetWindowPos function.
		public const int WS_DISABLED                      = unchecked(0x08000000); // The window is initially disabled. A disabled window cannot receive input from the user. To change this after a window has been created, use the EnableWindow function.
		public const int WS_CLIPSIBLINGS                  = unchecked(0x04000000); // Clips child windows relative to each other; that is, when a particular child window receives a WM_PAINT message, the WS_CLIPSIBLINGS style clips all other overlapping child windows out of the region of the child window to be updated. If WS_CLIPSIBLINGS is not specified and child windows overlap, it is possible, when drawing within the client area of a child window, to draw within the client area of a neighboring child window.
		public const int WS_CLIPCHILDREN                  = unchecked(0x02000000); // Excludes the area occupied by child windows when drawing occurs within the parent window. This style is used when creating the parent window.
		public const int WS_MAXIMIZE                      = unchecked(0x01000000); // The window is initially maximized.
		public const int WS_CAPTION                       = unchecked(0x00C00000); // The window has a title bar (includes the WS_BORDER style).
		public const int WS_BORDER                        = unchecked(0x00800000); // The window has a thin-line border.
		public const int WS_DLGFRAME                      = unchecked(0x00400000); // The window has a border of a style typically used with dialog boxes. A window with this style cannot have a title bar.
		public const int WS_VSCROLL                       = unchecked(0x00200000); // The window has a vertical scroll bar.
		public const int WS_HSCROLL                       = unchecked(0x00100000); // The window has a horizontal scroll bar.
		public const int WS_SYSMENU                       = unchecked(0x00080000); // The window has a window menu on its title bar. The WS_CAPTION style must also be specified.
		public const int WS_SIZEBOX                       = unchecked(0x00040000); // The window has a sizing border. Same as the WS_THICKFRAME style.
		public const int WS_THICKFRAME                    = unchecked(0x00040000); // The window has a sizing border. Same as the WS_SIZEBOX style.
		public const int WS_GROUP                         = unchecked(0x00020000); // The window is the first control of a group of controls. The group consists of this first control and all controls defined after it, up to the next control with the WS_GROUP style. The first control in each group usually has the WS_TABSTOP style so that the user can move from group to group. The user can subsequently change the keyboard focus from one control in the group to the next control in the group by using the direction keys. You can turn this style on and off to change dialog box navigation. To change this style after a window has been created, use the SetWindowLong function.
		public const int WS_MINIMIZEBOX                   = unchecked(0x00020000); // The window has a minimize button. Cannot be combined with the WS_EX_CONTEXTHELP style. The WS_SYSMENU style must also be specified.
		public const int WS_MAXIMIZEBOX                   = unchecked(0x00010000); // The window has a maximize button. Cannot be combined with the WS_EX_CONTEXTHELP style. The WS_SYSMENU style must also be specified.
		public const int WS_TABSTOP                       = unchecked(0x00010000); // The window is a control that can receive the keyboard focus when the user presses the TAB key. Pressing the TAB key changes the keyboard focus to the next control with the WS_TABSTOP style. You can turn this style on and off to change dialog box navigation. To change this style after a window has been created, use the SetWindowLong function. For user-created windows and modeless dialogs to work with tab stops, alter the message loop to call the IsDialogMessage function.
		public const int WS_TILED                         = unchecked(0x00000000); // The window is an overlapped window. An overlapped window has a title bar and a border. Same as the WS_OVERLAPPED style.
		public const int WS_OVERLAPPED                    = unchecked(0x00000000); // The window is an overlapped window. An overlapped window has a title bar and a border. Same as the WS_TILED style.
		public const int WS_POPUPWINDOW                   = unchecked(WS_POPUP | WS_BORDER | WS_SYSMENU); // The window is a pop-up window. The WS_CAPTION and WS_POPUPWINDOW styles must be combined to make the window menu visible.
		public const int WS_TILEDWINDOW                   = unchecked(WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX); // The window is an overlapped window. Same as the WS_OVERLAPPEDWINDOW style.
		public const int WS_OVERLAPPEDWINDOW              = unchecked(WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX); // The window is an overlapped window. Same as the WS_TILEDWINDOW style.
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
	}
}
