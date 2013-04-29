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

module dgui.messagebox;

import std.utf: toUTFz;
private import dgui.core.winapi;
public import dgui.core.dialogs.dialogresult;

enum MsgBoxButtons: uint
{
	OK = MB_OK,
	YES_NO = MB_YESNO,
	OK_CANCEL = MB_OKCANCEL,
	RETRY_CANCEL = MB_RETRYCANCEL,	
	YES_NO_CANCEL = MB_YESNOCANCEL,
	ABORT_RETRY_IGNORE = MB_ABORTRETRYIGNORE,
}

enum MsgBoxIcons: uint
{
	NONE = 0,
	WARNING = MB_ICONWARNING,
	INFORMATION = MB_ICONINFORMATION,
	QUESTION = MB_ICONQUESTION,
	ERROR = MB_ICONERROR,
}

final class MsgBox
{
	private this()
	{
		
	}
	
	public static DialogResult show(string title, string text, MsgBoxButtons button, MsgBoxIcons icon)
	{
		return cast(DialogResult)MessageBoxW(GetActiveWindow(), toUTFz!(wchar*)(text), toUTFz!(wchar*)(title), button | icon);
	}

	public static DialogResult show(string title, string text, MsgBoxButtons button)
	{
		return MsgBox.show(title, text, button, MsgBoxIcons.NONE);
	}

	public static DialogResult show(string title, string text, MsgBoxIcons icon)
	{
		return MsgBox.show(title, text, MsgBoxButtons.OK, icon);
	}

	public static DialogResult show(string title, string text)
	{
		return MsgBox.show(title, text, MsgBoxButtons.OK, MsgBoxIcons.NONE);
	}
}