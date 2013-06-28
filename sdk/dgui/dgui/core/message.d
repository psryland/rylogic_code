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

module dgui.core.message;

import dgui.core.winapi;

/* DGui Custom Messages in order to overcome WinAPI's limitation */
enum
{
	DGUI_BASE					= WM_APP + 1,	 // DGui's internal message start
	DGUI_ADDCHILDCONTROL	 	= DGUI_BASE, 	 // void DGUI_ADDCHILDCONTROL(Control childControl, NULL)
	DGUI_DOLAYOUT				= DGUI_BASE + 1, // void DGUI_DOLAYOUT(NULL, NULL)
	DGUI_SETDIALOGRESULT		= DGUI_BASE + 2, // void DGUI_SETDIALOGRESULT(DialogResult result, NULL)
	DGUI_REFLECTMESSAGE			= DGUI_BASE + 3, // void DGUI_REFLECTMESSAGE(Message m, NULL)
	DGUI_CHILDCONTROLCREATED	= DGUI_BASE + 4, // void DGUI_CHILDCONTROLCREATED(Control childControl, NULL)
	DGUI_CREATEONLY				= DGUI_BASE + 5, // void DGUI_CREATEONLY(NULL, NULL)
}

struct Message
{
	HWND hWnd;
	uint Msg;
	WPARAM wParam;
	LPARAM lParam;
	LRESULT Result;
	
	public static Message opCall(HWND h, uint msg, WPARAM wp, LPARAM lp)
	{
		Message m;

		m.hWnd = h;
		m.Msg = msg;
		m.wParam = wp;
		m.lParam = lp;

		return m;
	}
}