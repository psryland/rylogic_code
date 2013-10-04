//***************************************************
// Win32 wrapper
//  Copyright © Rylogic Ltd 2008
//***************************************************

using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using HWND=System.IntPtr;

namespace pr.util
{
	/// <summary>Win32 wrapper</summary>
	public static class Win32
	{
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

		// WM_SYSCOMMAND values
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

		// WM_ACTIVATE state values
		public const uint WA_INACTIVE                     = 0;
		public const uint WA_ACTIVE                       = 1;
		public const uint WA_CLICKACTIVE                  = 2;

		// Key State Masks for Mouse Messages
		public const int MK_LBUTTON                       = 0x0001;
		public const int MK_RBUTTON                       = 0x0002;
		public const int MK_SHIFT                         = 0x0004;
		public const int MK_CONTROL                       = 0x0008;
		public const int MK_MBUTTON                       = 0x0010;
		public const int MK_XBUTTON1                      = 0x0020;
		public const int MK_XBUTTON2                      = 0x0040;

		public const int SW_HIDE                          = 0;
		public const int SW_SHOW                          = 5;

		public const int NFR_ANSI                         = 1;
		public const int NFR_UNICODE                      = 2;
		public const int NF_QUERY                         = 3;
		public const int NF_REQUERY                       = 4;

		public const int WH_MOUSE_LL                      = 14;
		public const int WH_KEYBOARD_LL                   = 13;
		public const int WH_MOUSE                         = 7;
		public const int WH_KEYBOARD                      = 2;

		public const byte VK_SHIFT                        = 0x10;
		public const byte VK_CAPITAL                      = 0x14;
		public const byte VK_NUMLOCK                      = 0x90;

		public const int  GWL_WNDPROC                     = -4;
		public const int  GWL_HINSTANCE                   = -6;
		public const int  GWL_ID                          = -12;
		public const int  GWL_STYLE                       = -16;
		public const int  GWL_EXSTYLE                     = -20;
		public const int  GWL_USERDATA                    = -21;

		public const int TM_PLAINTEXT                     = 1;
		public const int TM_RICHTEXT                      = 2; // default behaviour
		public const int TM_SINGLELEVELUNDO               = 4;
		public const int TM_MULTILEVELUNDO                = 8; // default behaviour
		public const int TM_SINGLECODEPAGE                = 16;
		public const int TM_MULTICODEPAGE                 = 32; // default behaviour

		// RichEdit messages
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

		public const int WS_EX_LAYERED                    = 0x80000;
		public const int WS_EX_TRANSPARENT                = 0x20;

		// Class styles
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

		// Graphics modes
		public const int GM_COMPATIBLE                    = 1;
		public const int GM_ADVANCED                      = 2;

		// Mapping Modes
		public const int MM_TEXT                          = 1;
		public const int MM_LOMETRIC                      = 2;
		public const int MM_HIMETRIC                      = 3;
		public const int MM_LOENGLISH                     = 4;
		public const int MM_HIENGLISH                     = 5;
		public const int MM_TWIPS                         = 6;
		public const int MM_ISOTROPIC                     = 7;
		public const int MM_ANISOTROPIC                   = 8;
		#endregion

		public enum ScrollBarDirection
		{
			SB_HORZ = 0,
			SB_VERT = 1,
			SB_CTL = 2,
			SB_BOTH = 3
		}

		public enum ScrollInfoMask :uint
		{
			SIF_RANGE = 0x1,
			SIF_PAGE = 0x2,
			SIF_POS = 0x4,
			SIF_DISABLENOSCROLL = 0x8,
			SIF_TRACKPOS = 0x10,
			SIF_ALL = SIF_RANGE + SIF_PAGE + SIF_POS + SIF_TRACKPOS
		}

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

		public static bool KeyDown(Keys vkey)
		{
			return (GetAsyncKeyState(vkey) & 0x8000) != 0;
		}

		public static bool KeyPress(Keys vkey)
		{
			if   (!KeyDown(vkey)) return false;
			while (KeyDown(vkey)) Thread.Sleep(10);
			return true;
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

		[System.Security.SuppressUnmanagedCodeSecurity] // We won't use this maliciously
		[DllImport("user32.dll", EntryPoint="PeekMessage", CharSet=CharSet.Auto)] public static extern bool PeekMessage(out Message msg, IntPtr hWnd, uint messageFilterMin, uint messageFilterMax, uint flags);
		[DllImport("user32.dll", EntryPoint="SendMessage")]       public static extern int    SendMessage(HWND hwnd, uint msg, int wparam, int lparam);
		[DllImport("user32.dll", EntryPoint="PostThreadMessage")] public static extern int    PostThreadMessage(int idThread, uint msg, int wParam, int lParam);
		[DllImport("user32.dll")]                                 public static extern int    CallNextHookEx(int idHook, int nCode, int wParam, IntPtr lParam);
		[DllImport("user32.dll")]                                 public static extern int    SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, int dwThreadId);
		[DllImport("user32.dll")]                                 public static extern int    UnhookWindowsHookEx(int idHook);
		[DllImport("user32.dll")]                                 public static extern int    GetDoubleClickTime();
		[DllImport("user32.dll")]                                 public static extern int    ToAscii(int uVirtKey, int uScanCode, byte[] lpbKeyState, byte[] lpwTransKey, int fuState);
		[DllImport("user32.dll")]                                 public static extern int    GetKeyboardState(byte[] pbKeyState);
		[DllImport("user32.dll")]                                 public static extern short  GetKeyState(int vKey);
		[DllImport("user32.dll")]                                 public static extern short  GetAsyncKeyState(Keys vKey);
		[DllImport("user32.dll")]                                 public static extern bool   ShowWindowAsync(HWND hwnd, int nCmdShow);
		[DllImport("user32.dll")]                                 public static extern bool   SetForegroundWindow(HWND hwnd);
		[DllImport("user32.dll")]                                 public static extern bool   IsIconic(HWND hwnd);
		[DllImport("user32.dll")]                                 public static extern bool   IsZoomed(HWND hwnd);
		[DllImport("user32.dll")]                                 public static extern bool   IsWindow(HWND hwnd);
		[DllImport("user32.dll")]                                 public static extern bool   IsWindowVisible(HWND hwnd);
		[DllImport("user32.dll")]                                 public static extern HWND   GetForegroundWindow();
		[DllImport("user32.dll")]                                 public static extern bool   GetClientRect(HWND hwnd, out RECT rect);
		[DllImport("user32.dll")]                                 public static extern bool   GetWindowRect(HWND hwnd, out RECT rect);
		[DllImport("user32.dll"  , SetLastError = true)]          public static extern IntPtr GetWindowThreadProcessId(HWND hWnd, ref IntPtr lpdwProcessId);
		[DllImport("user32.dll")]                                 public static extern IntPtr AttachThreadInput(IntPtr idAttach, IntPtr idAttachTo, int fAttach);
		[DllImport("user32.dll")]                                 public static extern HWND   WindowFromPoint(POINT Point);
		[DllImport("user32.dll")]                                 public static extern bool   LockWindowUpdate(HWND hWndLock);
		[DllImport("user32.dll")]                                 public static extern bool   GetScrollInfo(HWND hwnd, int BarType, ref SCROLLINFO lpsi);
		[DllImport("user32.dll")]                                 public static extern int    SetScrollInfo(HWND hwnd, int fnBar, ref SCROLLINFO lpsi, bool fRedraw);
		[DllImport("user32.dll")]                                 public static extern int    GetScrollPos(HWND hWnd, int nBar);
		[DllImport("user32.dll")]                                 public static extern int    SetScrollPos(HWND hWnd, int nBar, int nPos, bool bRedraw);
		[DllImport("user32.dll")]                                 public static extern int    HideCaret(IntPtr hwnd);
		[DllImport("user32.dll")]                                 public static extern IntPtr SetParent(HWND hWndChild, HWND hWndNewParent);
		[DllImport("user32.dll")]                                 public static extern int    SetWindowLong(HWND hWnd, int nIndex, uint dwNewLong);
		[DllImport("user32.dll", SetLastError=true)]              public static extern uint   GetWindowLong(HWND hWnd, int nIndex);
		[DllImport("kernel32.dll", SetLastError = true)]          public static extern bool   AllocConsole();
		[DllImport("kernel32.dll", SetLastError = true)]          public static extern bool   FreeConsole();
		[DllImport("kernel32.dll", SetLastError = true)]          public static extern bool   AttachConsole(int dwProcessId);
		[DllImport("kernel32.dll", SetLastError = true)]          public static extern bool   WriteConsole(IntPtr hConsoleOutput, string lpBuffer, uint nNumberOfCharsToWrite, out uint lpNumberOfCharsWritten, IntPtr lpReserved);
		[DllImport("ole32.dll")]                                  public static extern void   CoTaskMemFree(IntPtr ptr);
		[DllImport("gdi32.dll")]                                  public static extern int    SetGraphicsMode(IntPtr hdc, int iGraphicsMode);
		[DllImport("gdi32.dll")]                                  public static extern int    SetMapMode(IntPtr hdc, int fnMapMode);
		[DllImport("gdi32.dll")]                                  public static extern bool   SetWorldTransform(IntPtr hdc, ref XFORM lpXform);
		[DllImport("gdi32.dll", EntryPoint="DeleteObject")]       public static extern bool   DeleteObject(IntPtr hObject);
	}
}
