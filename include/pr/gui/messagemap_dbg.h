#ifndef PR_MESSAGE_MAP_DBG_H
#define PR_MESSAGE_MAP_DBG_H
#pragma once

#include <commctrl.h>
#include "pr/common/assert.h"
#include "pr/common/fmt.h"

namespace pr
{
	namespace debug_wm
	{
		// Convert a windows message to a string
		inline char const* WMtoString(UINT uMsg)
		{
			switch (uMsg)
			{
			default:
				return "WM_unknown";
			case WM_NULL: return "WM_NULL";
			case WM_CREATE: return "WM_CREATE";
			case WM_DESTROY: return "WM_DESTROY";
			case WM_MOVE: return "WM_MOVE";
			case WM_SIZE: return "WM_SIZE";
			case WM_ACTIVATE: return "WM_ACTIVATE";
			case WM_SETFOCUS: return "WM_SETFOCUS";
			case WM_KILLFOCUS: return "WM_KILLFOCUS";
			case WM_ENABLE: return "WM_ENABLE";
			case WM_SETREDRAW: return "WM_SETREDRAW";
			case WM_SETTEXT: return "WM_SETTEXT";
			case WM_GETTEXT: return "WM_GETTEXT";
			case WM_GETTEXTLENGTH: return "WM_GETTEXTLENGTH";
			case WM_PAINT: return "WM_PAINT";
			case WM_CLOSE: return "WM_CLOSE";

			#ifndef _WIN32_WCE
			case WM_QUERYENDSESSION: return "WM_QUERYENDSESSION";
			case WM_QUERYOPEN: return "WM_QUERYOPEN";
			case WM_ENDSESSION: return "WM_ENDSESSION";
			#endif

			case WM_QUIT: return "WM_QUIT";
			case WM_ERASEBKGND: return "WM_ERASEBKGND";
			case WM_SYSCOLORCHANGE: return "WM_SYSCOLORCHANGE";
			case WM_SHOWWINDOW: return "WM_SHOWWINDOW";

			#if(WINVER >= 0x0400)
			case WM_SETTINGCHANGE: return "WM_SETTINGCHANGE";
			#else
			case WM_WININICHANGE: return "WM_WININICHANGE";
			#endif /* WINVER >= 0x0400 */

			case WM_DEVMODECHANGE: return "WM_DEVMODECHANGE";
			case WM_ACTIVATEAPP: return "WM_ACTIVATEAPP";
			case WM_FONTCHANGE: return "WM_FONTCHANGE";
			case WM_TIMECHANGE: return "WM_TIMECHANGE";
			case WM_CANCELMODE: return "WM_CANCELMODE";
			case WM_SETCURSOR: return "WM_SETCURSOR";
			case WM_MOUSEACTIVATE: return "WM_MOUSEACTIVATE";
			case WM_CHILDACTIVATE: return "WM_CHILDACTIVATE";
			case WM_QUEUESYNC: return "WM_QUEUESYNC";
			case WM_GETMINMAXINFO: return "WM_GETMINMAXINFO";
			case WM_PAINTICON: return "WM_PAINTICON";
			case WM_ICONERASEBKGND: return "WM_ICONERASEBKGND";
			case WM_NEXTDLGCTL: return "WM_NEXTDLGCTL";
			case WM_SPOOLERSTATUS: return "WM_SPOOLERSTATUS";
			case WM_DRAWITEM: return "WM_DRAWITEM";
			case WM_MEASUREITEM: return "WM_MEASUREITEM";
			case WM_DELETEITEM: return "WM_DELETEITEM";
			case WM_VKEYTOITEM: return "WM_VKEYTOITEM";
			case WM_CHARTOITEM: return "WM_CHARTOITEM";
			case WM_SETFONT: return "WM_SETFONT";
			case WM_GETFONT: return "WM_GETFONT";
			case WM_SETHOTKEY: return "WM_SETHOTKEY";
			case WM_GETHOTKEY: return "WM_GETHOTKEY";
			case WM_QUERYDRAGICON: return "WM_QUERYDRAGICON";
			case WM_COMPAREITEM: return "WM_COMPAREITEM";

			#if(WINVER >= 0x0500)
			#ifndef _WIN32_WCE
			case WM_GETOBJECT: return "WM_GETOBJECT";
			#endif
			#endif /* WINVER >= 0x0500 */

			case WM_COMPACTING: return "WM_COMPACTING";
			case WM_COMMNOTIFY: return "WM_COMMNOTIFY";
			case WM_WINDOWPOSCHANGING: return "WM_WINDOWPOSCHANGING";
			case WM_WINDOWPOSCHANGED: return "WM_WINDOWPOSCHANGED";
			case WM_POWER: return "WM_POWER";
			case WM_COPYDATA: return "WM_COPYDATA";
			case WM_CANCELJOURNAL: return "WM_CANCELJOURNAL";

			#if(WINVER >= 0x0400)
			case WM_NOTIFY: return "WM_NOTIFY";
			case WM_INPUTLANGCHANGEREQUEST: return "WM_INPUTLANGCHANGEREQUEST";
			case WM_INPUTLANGCHANGE: return "WM_INPUTLANGCHANGE";
			case WM_TCARD: return "WM_TCARD";
			case WM_HELP: return "WM_HELP";
			case WM_USERCHANGED: return "WM_USERCHANGED";
			case WM_NOTIFYFORMAT: return "WM_NOTIFYFORMAT";
			case WM_CONTEXTMENU: return "WM_CONTEXTMENU";
			case WM_STYLECHANGING: return "WM_STYLECHANGING";
			case WM_STYLECHANGED: return "WM_STYLECHANGED";
			case WM_DISPLAYCHANGE: return "WM_DISPLAYCHANGE";
			case WM_GETICON: return "WM_GETICON";
			case WM_SETICON: return "WM_SETICON";
			#endif /* WINVER >= 0x0400 */

			case WM_NCCREATE: return "WM_NCCREATE";
			case WM_NCDESTROY: return "WM_NCDESTROY";
			case WM_NCCALCSIZE: return "WM_NCCALCSIZE";
			case WM_NCHITTEST: return "WM_NCHITTEST";
			case WM_NCPAINT: return "WM_NCPAINT";
			case WM_NCACTIVATE: return "WM_NCACTIVATE";
			case WM_GETDLGCODE: return "WM_GETDLGCODE";

			#ifndef _WIN32_WCE
			case WM_SYNCPAINT: return "WM_SYNCPAINT";
			#endif

			case WM_NCMOUSEMOVE: return "WM_NCMOUSEMOVE";
			case WM_NCLBUTTONDOWN: return "WM_NCLBUTTONDOWN";
			case WM_NCLBUTTONUP: return "WM_NCLBUTTONUP";
			case WM_NCLBUTTONDBLCLK: return "WM_NCLBUTTONDBLCLK";
			case WM_NCRBUTTONDOWN: return "WM_NCRBUTTONDOWN";
			case WM_NCRBUTTONUP: return "WM_NCRBUTTONUP";
			case WM_NCRBUTTONDBLCLK: return "WM_NCRBUTTONDBLCLK";
			case WM_NCMBUTTONDOWN: return "WM_NCMBUTTONDOWN";
			case WM_NCMBUTTONUP: return "WM_NCMBUTTONUP";
			case WM_NCMBUTTONDBLCLK: return "WM_NCMBUTTONDBLCLK";

			#if(_WIN32_WINNT >= 0x0500)
			case WM_NCXBUTTONDOWN: return "WM_NCXBUTTONDOWN";
			case WM_NCXBUTTONUP: return "WM_NCXBUTTONUP";
			case WM_NCXBUTTONDBLCLK: return "WM_NCXBUTTONDBLCLK";
			#endif /* _WIN32_WINNT >= 0x0500 */

			#if(_WIN32_WINNT >= 0x0501)
			case WM_INPUT_DEVICE_CHANGE: return "WM_INPUT_DEVICE_CHANGE";
			#endif /* _WIN32_WINNT >= 0x0501 */

			#if(_WIN32_WINNT >= 0x0501)
			case WM_INPUT: return "WM_INPUT";
			#endif /* _WIN32_WINNT >= 0x0501 */

			case WM_KEYDOWN: return "WM_KEYDOWN";
			case WM_KEYUP: return "WM_KEYUP";
			case WM_CHAR: return "WM_CHAR";
			case WM_DEADCHAR: return "WM_DEADCHAR";
			case WM_SYSKEYDOWN: return "WM_SYSKEYDOWN";
			case WM_SYSKEYUP: return "WM_SYSKEYUP";
			case WM_SYSCHAR: return "WM_SYSCHAR";
			case WM_SYSDEADCHAR: return "WM_SYSDEADCHAR";

			#if(_WIN32_WINNT >= 0x0501)
			case WM_UNICHAR: return "WM_UNICHAR";
			case UNICODE_NOCHAR: break;
			#else
			case WM_KEYLAST: return "WM_KEYLAST";
			#endif /* _WIN32_WINNT >= 0x0501 */

			#if(WINVER >= 0x0400)
			case WM_IME_STARTCOMPOSITION: return "WM_IME_STARTCOMPOSITION";
			case WM_IME_ENDCOMPOSITION: return "WM_IME_ENDCOMPOSITION";
			case WM_IME_COMPOSITION: return "WM_IME_COMPOSITION";
			#endif /* WINVER >= 0x0400 */

			case WM_INITDIALOG: return "WM_INITDIALOG";
			case WM_COMMAND: return "WM_COMMAND";
			case WM_SYSCOMMAND: return "WM_SYSCOMMAND";
			case WM_TIMER: return "WM_TIMER";
			case WM_HSCROLL: return "WM_HSCROLL";
			case WM_VSCROLL: return "WM_VSCROLL";
			case WM_INITMENU: return "WM_INITMENU";
			case WM_INITMENUPOPUP: return "WM_INITMENUPOPUP";
			case WM_MENUSELECT: return "WM_MENUSELECT";
			case WM_MENUCHAR: return "WM_MENUCHAR";
			case WM_ENTERIDLE: return "WM_ENTERIDLE";

			#if(WINVER >= 0x0500)
			#ifndef _WIN32_WCE
			case WM_MENURBUTTONUP: return "WM_MENURBUTTONUP";
			case WM_MENUDRAG: return "WM_MENUDRAG";
			case WM_MENUGETOBJECT: return "WM_MENUGETOBJECT";
			case WM_UNINITMENUPOPUP: return "WM_UNINITMENUPOPUP";
			case WM_MENUCOMMAND: return "WM_MENUCOMMAND";
			#ifndef _WIN32_WCE
			#if(_WIN32_WINNT >= 0x0500)
			case WM_CHANGEUISTATE: return "WM_CHANGEUISTATE";
			case WM_UPDATEUISTATE: return "WM_UPDATEUISTATE";
			case WM_QUERYUISTATE: return "WM_QUERYUISTATE";
			#endif /* _WIN32_WINNT >= 0x0500 */
			#endif
			#endif
			#endif /* WINVER >= 0x0500 */

			case WM_CTLCOLORMSGBOX: return "WM_CTLCOLORMSGBOX";
			case WM_CTLCOLOREDIT: return "WM_CTLCOLOREDIT";
			case WM_CTLCOLORLISTBOX: return "WM_CTLCOLORLISTBOX";
			case WM_CTLCOLORBTN: return "WM_CTLCOLORBTN";
			case WM_CTLCOLORDLG: return "WM_CTLCOLORDLG";
			case WM_CTLCOLORSCROLLBAR: return "WM_CTLCOLORSCROLLBAR";
			case WM_CTLCOLORSTATIC: return "WM_CTLCOLORSTATIC";
			case MN_GETHMENU: break;

			case WM_MOUSEMOVE: return "WM_MOUSEMOVE";
			case WM_LBUTTONDOWN: return "WM_LBUTTONDOWN";
			case WM_LBUTTONUP: return "WM_LBUTTONUP";
			case WM_LBUTTONDBLCLK: return "WM_LBUTTONDBLCLK";
			case WM_RBUTTONDOWN: return "WM_RBUTTONDOWN";
			case WM_RBUTTONUP: return "WM_RBUTTONUP";
			case WM_RBUTTONDBLCLK: return "WM_RBUTTONDBLCLK";
			case WM_MBUTTONDOWN: return "WM_MBUTTONDOWN";
			case WM_MBUTTONUP: return "WM_MBUTTONUP";
			case WM_MBUTTONDBLCLK: return "WM_MBUTTONDBLCLK";

			#if (_WIN32_WINNT >= 0x0400) || (_WIN32_WINDOWS > 0x0400)
			case WM_MOUSEWHEEL: return "WM_MOUSEWHEEL";
			#endif

			#if (_WIN32_WINNT >= 0x0500)
			case WM_XBUTTONDOWN: return "WM_XBUTTONDOWN";
			case WM_XBUTTONUP: return "WM_XBUTTONUP";
			case WM_XBUTTONDBLCLK: return "WM_XBUTTONDBLCLK";
			#endif

			#if (_WIN32_WINNT >= 0x0600)
			case WM_MOUSEHWHEEL: return "WM_MOUSEHWHEEL";
			#endif

			case WM_PARENTNOTIFY: return "WM_PARENTNOTIFY";
			case WM_ENTERMENULOOP: return "WM_ENTERMENULOOP";
			case WM_EXITMENULOOP: return "WM_EXITMENULOOP";

			#if(WINVER >= 0x0400)
			case WM_NEXTMENU: return "WM_NEXTMENU";
			case WM_SIZING: return "WM_SIZING";
			case WM_CAPTURECHANGED: return "WM_CAPTURECHANGED";
			case WM_MOVING: return "WM_MOVING";
			#endif /* WINVER >= 0x0400 */

			#if(WINVER >= 0x0400)
			case WM_POWERBROADCAST: return "WM_POWERBROADCAST";
			#endif /* WINVER >= 0x0400 */

			#if(WINVER >= 0x0400)
			case WM_DEVICECHANGE: return "WM_DEVICECHANGE";
			#endif /* WINVER >= 0x0400 */

			case WM_MDICREATE: return "WM_MDICREATE";
			case WM_MDIDESTROY: return "WM_MDIDESTROY";
			case WM_MDIACTIVATE: return "WM_MDIACTIVATE";
			case WM_MDIRESTORE: return "WM_MDIRESTORE";
			case WM_MDINEXT: return "WM_MDINEXT";
			case WM_MDIMAXIMIZE: return "WM_MDIMAXIMIZE";
			case WM_MDITILE: return "WM_MDITILE";
			case WM_MDICASCADE: return "WM_MDICASCADE";
			case WM_MDIICONARRANGE: return "WM_MDIICONARRANGE";
			case WM_MDIGETACTIVE: return "WM_MDIGETACTIVE";
			case WM_MDISETMENU: return "WM_MDISETMENU";
			case WM_ENTERSIZEMOVE: return "WM_ENTERSIZEMOVE";
			case WM_EXITSIZEMOVE: return "WM_EXITSIZEMOVE";
			case WM_DROPFILES: return "WM_DROPFILES";
			case WM_MDIREFRESHMENU: return "WM_MDIREFRESHMENU";

			#if(WINVER >= 0x0400)
			case WM_IME_SETCONTEXT: return "WM_IME_SETCONTEXT";
			case WM_IME_NOTIFY: return "WM_IME_NOTIFY";
			case WM_IME_CONTROL: return "WM_IME_CONTROL";
			case WM_IME_COMPOSITIONFULL: return "WM_IME_COMPOSITIONFULL";
			case WM_IME_SELECT: return "WM_IME_SELECT";
			case WM_IME_CHAR: return "WM_IME_CHAR";
			#endif /* WINVER >= 0x0400 */

			#if(WINVER >= 0x0500)
			case WM_IME_REQUEST: return "WM_IME_REQUEST";
			#endif /* WINVER >= 0x0500 */

			#if(WINVER >= 0x0400)
			case WM_IME_KEYDOWN: return "WM_IME_KEYDOWN";
			case WM_IME_KEYUP: return "WM_IME_KEYUP";
			#endif /* WINVER >= 0x0400 */

			#if((_WIN32_WINNT >= 0x0400) || (WINVER >= 0x0500))
			case WM_MOUSEHOVER: return "WM_MOUSEHOVER";
			case WM_MOUSELEAVE: return "WM_MOUSELEAVE";
			#endif

			#if(WINVER >= 0x0500)
			case WM_NCMOUSEHOVER: return "WM_NCMOUSEHOVER";
			case WM_NCMOUSELEAVE: return "WM_NCMOUSELEAVE";
			#endif /* WINVER >= 0x0500 */

			#if(_WIN32_WINNT >= 0x0501)
			case WM_WTSSESSION_CHANGE: return "WM_WTSSESSION_CHANGE";
			case WM_TABLET_FIRST: return "WM_TABLET_FIRST";
			case WM_TABLET_LAST: return "WM_TABLET_LAST";
			#endif /* _WIN32_WINNT >= 0x0501 */

			case WM_CUT: return "WM_CUT";
			case WM_COPY: return "WM_COPY";
			case WM_PASTE: return "WM_PASTE";
			case WM_CLEAR: return "WM_CLEAR";
			case WM_UNDO: return "WM_UNDO";
			case WM_RENDERFORMAT: return "WM_RENDERFORMAT";
			case WM_RENDERALLFORMATS: return "WM_RENDERALLFORMATS";
			case WM_DESTROYCLIPBOARD: return "WM_DESTROYCLIPBOARD";
			case WM_DRAWCLIPBOARD: return "WM_DRAWCLIPBOARD";
			case WM_PAINTCLIPBOARD: return "WM_PAINTCLIPBOARD";
			case WM_VSCROLLCLIPBOARD: return "WM_VSCROLLCLIPBOARD";
			case WM_SIZECLIPBOARD: return "WM_SIZECLIPBOARD";
			case WM_ASKCBFORMATNAME: return "WM_ASKCBFORMATNAME";
			case WM_CHANGECBCHAIN: return "WM_CHANGECBCHAIN";
			case WM_HSCROLLCLIPBOARD: return "WM_HSCROLLCLIPBOARD";
			case WM_QUERYNEWPALETTE: return "WM_QUERYNEWPALETTE";
			case WM_PALETTEISCHANGING: return "WM_PALETTEISCHANGING";
			case WM_PALETTECHANGED: return "WM_PALETTECHANGED";
			case WM_HOTKEY: return "WM_HOTKEY";

			#if(WINVER >= 0x0400)
			case WM_PRINT: return "WM_PRINT";
			case WM_PRINTCLIENT: return "WM_PRINTCLIENT";
			#endif /* WINVER >= 0x0400 */

			#if(_WIN32_WINNT >= 0x0500)
			case WM_APPCOMMAND: return "WM_APPCOMMAND";
			#endif /* _WIN32_WINNT >= 0x0500 */

			#if(_WIN32_WINNT >= 0x0501)
			case WM_THEMECHANGED: return "WM_THEMECHANGED";
			#endif /* _WIN32_WINNT >= 0x0501 */

			#if(_WIN32_WINNT >= 0x0501)
			case WM_CLIPBOARDUPDATE: return "WM_CLIPBOARDUPDATE";
			#endif /* _WIN32_WINNT >= 0x0501 */

			#if(_WIN32_WINNT >= 0x0600)
			case WM_DWMCOMPOSITIONCHANGED: return "WM_DWMCOMPOSITIONCHANGED";
			case WM_DWMNCRENDERINGCHANGED: return "WM_DWMNCRENDERINGCHANGED";
			case WM_DWMCOLORIZATIONCOLORCHANGED: return "WM_DWMCOLORIZATIONCOLORCHANGED";
			case WM_DWMWINDOWMAXIMIZEDCHANGE: return "WM_DWMWINDOWMAXIMIZEDCHANGE";
			#endif /* _WIN32_WINNT >= 0x0600 */

			#if(WINVER >= 0x0600)
			case WM_GETTITLEBARINFOEX: return "WM_GETTITLEBARINFOEX";
			#endif /* WINVER >= 0x0600 */

			#if(WINVER >= 0x0400)
			case WM_HANDHELDFIRST: return "WM_HANDHELDFIRST";
			case WM_HANDHELDLAST: return "WM_HANDHELDLAST";
			case WM_AFXFIRST: return "WM_AFXFIRST";
			case WM_AFXLAST: return "WM_AFXLAST";
			#endif /* WINVER >= 0x0400 */

			case WM_PENWINFIRST: return "WM_PENWINFIRST";
			case WM_PENWINLAST: return "WM_PENWINLAST";

			#if(WINVER >= 0x0400)
			case WM_APP: return "WM_APP";
			#endif /* WINVER >= 0x0400 */
			}
			return "";
		}

		// Convert a VK_* define into a string
		inline char const* VKtoString(int vk)
		{
			switch (vk)
			{
			default:
				{
					static char str[2] = {'\0','\0'};
					if ((vk >= '0' && vk <= '9') || (vk >= 'A' && vk <= 'Z'))
						str[0] = (char)vk;
					return str;
				}
			case VK_LBUTTON: return "VK_LBUTTON";
			case VK_RBUTTON: return "VK_RBUTTON";
			case VK_CANCEL : return "VK_CANCEL";
			case VK_MBUTTON: return "VK_MBUTTON";

			#if(_WIN32_WINNT >= 0x0500)
			case VK_XBUTTON1: return "VK_XBUTTON1";
			case VK_XBUTTON2: return "VK_XBUTTON2";
			#endif /* _WIN32_WINNT >= 0x0500 */

			case VK_BACK: return "VK_BACK";
			case VK_TAB:  return "VK_TAB";
			case VK_CLEAR: return "VK_CLEAR";
			case VK_RETURN: return "VK_RETURN";
			case VK_SHIFT: return "VK_SHIFT";
			case VK_CONTROL: return "VK_CONTROL";
			case VK_MENU: return "VK_MENU";
			case VK_PAUSE: return "VK_PAUSE";
			case VK_CAPITAL: return "VK_CAPITAL";
			case VK_KANA: return "VK_KANA";
			case VK_JUNJA: return "VK_JUNJA";
			case VK_FINAL: return "VK_FINAL";
			case VK_HANJA: return "VK_HANJA";
			case VK_ESCAPE: return "VK_ESCAPE";
			case VK_CONVERT: return "VK_CONVERT";
			case VK_NONCONVERT: return "VK_NONCONVERT";
			case VK_ACCEPT: return "VK_ACCEPT";
			case VK_MODECHANGE: return "VK_MODECHANGE";
			case VK_SPACE: return "VK_SPACE";
			case VK_PRIOR: return "VK_PRIOR";
			case VK_NEXT: return "VK_NEXT";
			case VK_END: return "VK_END";
			case VK_HOME: return "VK_HOME";
			case VK_LEFT: return "VK_LEFT";
			case VK_UP: return "VK_UP";
			case VK_RIGHT: return "VK_RIGHT";
			case VK_DOWN: return "VK_DOWN";
			case VK_SELECT: return "VK_SELECT";
			case VK_PRINT: return "VK_PRINT";
			case VK_EXECUTE: return "VK_EXECUTE";
			case VK_SNAPSHOT: return "VK_SNAPSHOT";
			case VK_INSERT: return "VK_INSERT";
			case VK_DELETE: return "VK_DELETE";
			case VK_HELP: return "VK_HELP";
			case VK_LWIN: return "VK_LWIN";
			case VK_RWIN: return "VK_RWIN";
			case VK_APPS: return "VK_APPS";
			case VK_SLEEP: return "VK_SLEEP";
			case VK_NUMPAD0: return "VK_NUMPAD0";
			case VK_NUMPAD1: return "VK_NUMPAD1";
			case VK_NUMPAD2: return "VK_NUMPAD2";
			case VK_NUMPAD3: return "VK_NUMPAD3";
			case VK_NUMPAD4: return "VK_NUMPAD4";
			case VK_NUMPAD5: return "VK_NUMPAD5";
			case VK_NUMPAD6: return "VK_NUMPAD6";
			case VK_NUMPAD7: return "VK_NUMPAD7";
			case VK_NUMPAD8: return "VK_NUMPAD8";
			case VK_NUMPAD9: return "VK_NUMPAD9";
			case VK_MULTIPLY: return "VK_MULTIPLY";
			case VK_ADD: return "VK_ADD";
			case VK_SEPARATOR: return "VK_SEPARATOR";
			case VK_SUBTRACT: return "VK_SUBTRACT";
			case VK_DECIMAL: return "VK_DECIMAL";
			case VK_DIVIDE: return "VK_DIVIDE";
			case VK_F1: return "VK_F1";
			case VK_F2: return "VK_F2";
			case VK_F3: return "VK_F3";
			case VK_F4: return "VK_F4";
			case VK_F5: return "VK_F5";
			case VK_F6: return "VK_F6";
			case VK_F7: return "VK_F7";
			case VK_F8: return "VK_F8";
			case VK_F9: return "VK_F9";
			case VK_F10: return "VK_F10";
			case VK_F11: return "VK_F11";
			case VK_F12: return "VK_F12";
			case VK_F13: return "VK_F13";
			case VK_F14: return "VK_F14";
			case VK_F15: return "VK_F15";
			case VK_F16: return "VK_F16";
			case VK_F17: return "VK_F17";
			case VK_F18: return "VK_F18";
			case VK_F19: return "VK_F19";
			case VK_F20: return "VK_F20";
			case VK_F21: return "VK_F21";
			case VK_F22: return "VK_F22";
			case VK_F23: return "VK_F23";
			case VK_F24: return "VK_F24";
			case VK_NUMLOCK: return "VK_NUMLOCK";
			case VK_SCROLL: return "VK_SCROLL";
			case VK_OEM_NEC_EQUAL: return "VK_OEM_NEC_EQUAL";
			case VK_OEM_FJ_MASSHOU: return "VK_OEM_FJ_MASSHOU";
			case VK_OEM_FJ_TOUROKU: return "VK_OEM_FJ_TOUROKU";
			case VK_OEM_FJ_LOYA: return "VK_OEM_FJ_LOYA";
			case VK_OEM_FJ_ROYA: return "VK_OEM_FJ_ROYA";
			case VK_LSHIFT: return "VK_LSHIFT";
			case VK_RSHIFT: return "VK_RSHIFT";
			case VK_LCONTROL: return "VK_LCONTROL";
			case VK_RCONTROL: return "VK_RCONTROL";
			case VK_LMENU: return "VK_LMENU";
			case VK_RMENU: return "VK_RMENU";

			#if(_WIN32_WINNT >= 0x0500)
			case VK_BROWSER_BACK: return "VK_BROWSER_BACK";
			case VK_BROWSER_FORWARD: return "VK_BROWSER_FORWARD";
			case VK_BROWSER_REFRESH: return "VK_BROWSER_REFRESH";
			case VK_BROWSER_STOP: return "VK_BROWSER_STOP";
			case VK_BROWSER_SEARCH: return "VK_BROWSER_SEARCH";
			case VK_BROWSER_FAVORITES: return "VK_BROWSER_FAVORITES";
			case VK_BROWSER_HOME: return "VK_BROWSER_HOME";
			case VK_VOLUME_MUTE: return "VK_VOLUME_MUTE";
			case VK_VOLUME_DOWN: return "VK_VOLUME_DOWN";
			case VK_VOLUME_UP: return "VK_VOLUME_UP";
			case VK_MEDIA_NEXT_TRACK: return "VK_MEDIA_NEXT_TRACK";
			case VK_MEDIA_PREV_TRACK: return "VK_MEDIA_PREV_TRACK";
			case VK_MEDIA_STOP: return "VK_MEDIA_STOP";
			case VK_MEDIA_PLAY_PAUSE: return "VK_MEDIA_PLAY_PAUSE";
			case VK_LAUNCH_MAIL: return "VK_LAUNCH_MAIL";
			case VK_LAUNCH_MEDIA_SELECT: return "VK_LAUNCH_MEDIA_SELECT";
			case VK_LAUNCH_APP1: return "VK_LAUNCH_APP1";
			case VK_LAUNCH_APP2: return "VK_LAUNCH_APP2";
			#endif /* _WIN32_WINNT >= 0x0500 */

			case VK_OEM_1: return "VK_OEM_1";
			case VK_OEM_PLUS: return "VK_OEM_PLUS";
			case VK_OEM_COMMA: return "VK_OEM_COMMA";
			case VK_OEM_MINUS: return "VK_OEM_MINUS";
			case VK_OEM_PERIOD: return "VK_OEM_PERIOD";
			case VK_OEM_2: return "VK_OEM_2";
			case VK_OEM_3: return "VK_OEM_3";
			case VK_OEM_4: return "VK_OEM_4";
			case VK_OEM_5: return "VK_OEM_5";
			case VK_OEM_6: return "VK_OEM_6";
			case VK_OEM_7: return "VK_OEM_7";
			case VK_OEM_8: return "VK_OEM_8";
			case VK_OEM_AX: return "VK_OEM_AX";
			case VK_OEM_102: return "VK_OEM_102";
			case VK_ICO_HELP: return "VK_ICO_HELP";
			case VK_ICO_00: return "VK_ICO_00";

			#if(WINVER >= 0x0400)
			case VK_PROCESSKEY: return "VK_PROCESSKEY";
			#endif /* WINVER >= 0x0400 */

			case VK_ICO_CLEAR: return "VK_ICO_CLEAR";

			#if(_WIN32_WINNT >= 0x0500)
			case VK_PACKET: return "VK_PACKET";
			#endif /* _WIN32_WINNT >= 0x0500 */

			case VK_OEM_RESET: return "VK_OEM_RESET";
			case VK_OEM_JUMP: return "VK_OEM_JUMP";
			case VK_OEM_PA1: return "VK_OEM_PA1";
			case VK_OEM_PA2: return "VK_OEM_PA2";
			case VK_OEM_PA3: return "VK_OEM_PA3";
			case VK_OEM_WSCTRL: return "VK_OEM_WSCTRL";
			case VK_OEM_CUSEL: return "VK_OEM_CUSEL";
			case VK_OEM_ATTN: return "VK_OEM_ATTN";
			case VK_OEM_FINISH: return "VK_OEM_FINISH";
			case VK_OEM_COPY: return "VK_OEM_COPY";
			case VK_OEM_AUTO: return "VK_OEM_AUTO";
			case VK_OEM_ENLW: return "VK_OEM_ENLW";
			case VK_OEM_BACKTAB: return "VK_OEM_BACKTAB";
			case VK_ATTN: return "VK_ATTN";
			case VK_CRSEL: return "VK_CRSEL";
			case VK_EXSEL: return "VK_EXSEL";
			case VK_EREOF: return "VK_EREOF";
			case VK_PLAY: return "VK_PLAY";
			case VK_ZOOM: return "VK_ZOOM";
			case VK_NONAME: return "VK_NONAME";
			case VK_PA1: return "VK_PA1";
			case VK_OEM_CLEAR: return "VK_OEM_CLEAR";
			}
		}

		// Output info about a windows message
		inline void DebugMessage(HWND hWnd, UINT uMsg, WPARAM wParam, LPARAM lParam)
		{
			INT wParamLo = LOWORD(wParam);
			INT wParamHi = HIWORD(wParam);
			INT lParamLo = LOWORD(lParam);
			INT lParamHi = HIWORD(lParam);

			char const* msg_str = WMtoString(uMsg);
			char const* wnd_text = "";//[256]; ::GetWindowTextA(hWnd, wnd_text, sizeof(wnd_text));

			switch (uMsg)
			{
			case WM_SETCURSOR:
			case WM_NCHITTEST:
			case WM_NCMOUSEMOVE:
			case WM_MOUSEMOVE:
			case WM_ERASEBKGND:
			case WM_CTLCOLORDLG:
			case WM_AFXLAST:
			case WM_ENTERIDLE:
				break;//ignore
			default:
				PR_INFO(1, pr::FmtS(
					"%s: 0x%04x\n"
					"\thWnd: %p - %s\n"
					"\twParam: %x(%x,%x)\n"
					"\tlParam: %x(%x,%x)\n"
					,msg_str ,uMsg
					,hWnd ,wnd_text
					,wParam ,wParamHi ,wParamLo
					,lParam ,lParamHi ,lParamLo
					));
				break;
			case WM_LBUTTONDOWN:
				PR_INFO(1, pr::FmtS(
					"%s: 0x%04x\n"
					"\thWnd: %p - %s\n"
					"\twParam: button state = %s%s%s%s%s%s%s\n"
					"\tlParam: x=%d, y=%d\n"
					,msg_str ,uMsg
					,hWnd ,wnd_text
					,(wParam&MK_CONTROL?"|Ctrl":"")
					,(wParam&MK_LBUTTON?"|LBtn":"")
					,(wParam&MK_MBUTTON?"|MBtn":"")
					,(wParam&MK_RBUTTON?"|RBtn":"")
					,(wParam&MK_SHIFT?"|Shift":"")
					,(wParam&MK_XBUTTON1?"|XBtn1":"")
					,(wParam&MK_XBUTTON2?"|XBtn2":"")
					,lParamLo ,lParamHi
					));
				break;
			case WM_MOUSEACTIVATE:
				{
					PR_INFO(1, pr::FmtS(
						"%s: 0x%04x\n"
						"\thWnd: %p - %s\n"
						"\twParam: top level parent window = %p\n"
						"\tlParam: %x(%x,%x)\n"
						,msg_str ,uMsg
						,hWnd ,wnd_text
						,wParam
						,lParam ,lParamHi ,lParamLo
						));
					break;
				}
			case WM_NOTIFY:
				{
					char const* notify_type = "unknown";
					NMHDR* nmhdr = (NMHDR*)lParam;
					if      (NM_LAST   <= nmhdr->code) notify_type = "NM";
					else if (LVN_LAST  <= nmhdr->code) notify_type = "LVN";
					else if (HDN_LAST  <= nmhdr->code) notify_type = "HDN";
					else if (TVN_LAST  <= nmhdr->code) notify_type = "TVN";
					else if (TTN_LAST  <= nmhdr->code) notify_type = "TTN";
					else if (TCN_LAST  <= nmhdr->code) notify_type = "TCN";
					else if (CDN_LAST  <= nmhdr->code) notify_type = "CDN";
					else if (TBN_LAST  <= nmhdr->code) notify_type = "TBN";
					else if (UDN_LAST  <= nmhdr->code) notify_type = "UDN";
					#if (_WIN32_IE >= 0x0300)
					else if (DTN_LAST  <= nmhdr->code) notify_type = "DTN";
					else if (MCN_LAST  <= nmhdr->code) notify_type = "MCN";
					else if (DTN_LAST2 <= nmhdr->code) notify_type = "DTN";
					else if (CBEN_LAST <= nmhdr->code) notify_type = "CBEN";
					else if (RBN_LAST  <= nmhdr->code) notify_type = "RBN";
					#endif
					#if (_WIN32_IE >= 0x0400)
					else if (IPN_LAST  <= nmhdr->code) notify_type = "IPN";
					else if (SBN_LAST  <= nmhdr->code) notify_type = "SBN";
					else if (PGN_LAST  <= nmhdr->code) notify_type = "PGN";
					#endif
					#if (_WIN32_IE >= 0x0500)
					#ifndef WMN_FIRST
					else if (WMN_LAST  <= nmhdr->code) notify_type = "WMN";
					#endif
					#endif
					#if (_WIN32_WINNT >= 0x0501)
					else if (BCN_LAST  <= nmhdr->code) notify_type = "BCN";
					#endif
					#if (_WIN32_WINNT >= 0x0600)
					else if (TRBN_LAST <= nmhdr->code) notify_type = "TRBN";
					#endif

					// Ignore
					if (nmhdr->code == LVN_HOTTRACK) break;

					PR_INFO(1, pr::FmtS(
						"%s: 0x%04x\n"
						"\thWnd: %p - %s\n"
						"\twParam: source ctrl id = %d\n"
						"\tfrom_hWnd: %p\n"
						"\tfrom_id: %d\n"
						"\tcode: %d:%s\n"
						,msg_str ,uMsg
						,hWnd ,wnd_text
						,wParam
						,nmhdr->hwndFrom
						,nmhdr->idFrom
						,nmhdr->code
						,notify_type
						));

					break;
				}
			case WM_SYSKEYDOWN:
				{
					PR_INFO(1, pr::FmtS(
						"%s: 0x%04x\n"
						"\thWnd: %p - %s\n"
						"\twParam: vk_key = %d (%s)\n"
						"\tRepeats: %d\n"
						"\tlParam: %d\n"
						,msg_str ,uMsg
						,hWnd ,wnd_text
						,wParam ,VKtoString((int)wParam)
						,lParamLo
						,lParam
						));
					break;
				}
			}
		}

		// Displays a text description of a windows message.
		// Use this in PreTranslateMessage()
		template <typename Pred> inline void DebugMessage(MSG const* msg, Pred pred)
		{
			static bool enable_dbg_mm = true;
			if (!enable_dbg_mm) return;

			static UINT break_on_message = 0;
			if (break_on_message == msg->message)
				_CrtDbgBreak();

			if (pred(msg->message))
				DebugMessage(msg->hwnd, msg->message, msg->wParam, msg->lParam);
		}
		inline void DebugMessage(MSG const* msg)
		{
			DebugMessage(msg, [](int){ return true; });
		}
	}
}

#define MESSAGE_MAP_DBG_IMPL0
#define MESSAGE_MAP_DBG_IMPL1 pr::DebugMessage(hWnd, uMsg, wParam, lParam);
#define MESSAGE_MAP_DBG(grp)  PR_JOIN(MESSAGE_MAP_DBG_IMPL, grp)

#endif