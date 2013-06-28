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

module dgui.filebrowserdialog;

private import std.utf: toUTFz, toUTF8;
private import std.conv: to;
public import dgui.core.dialogs.commondialog;
private import dgui.core.utils;

enum FileBrowseMode
{
	OPEN = 0,
	SAVE = 1,
}

class FileBrowserDialog: CommonDialog!(OPENFILENAMEW, string)
{
	private string _filter;
	private FileBrowseMode _fbm = FileBrowseMode.OPEN;

	@property public void browseMode(FileBrowseMode fbm)
	{
		this._fbm = fbm;
	}
	
	@property public string filter()
	{
		return this._filter;
	}

	@property public void filter(string f)
	{
		this._filter = makeFilter(f);
	}
	
	public override bool showDialog()
	{
		wchar[MAX_PATH + 1] buffer;
		buffer[] = '\0';
		
		this._dlgStruct.lStructSize = OPENFILENAMEW.sizeof;
		this._dlgStruct.hwndOwner = GetActiveWindow();
		this._dlgStruct.lpstrFilter = toUTFz!(wchar*)(this._filter);
		this._dlgStruct.lpstrTitle = toUTFz!(wchar*)(this._title);
		this._dlgStruct.lpstrFile = buffer.ptr;
		this._dlgStruct.nMaxFile = MAX_PATH;
		this._dlgStruct.Flags = OFN_PATHMUSTEXIST | OFN_FILEMUSTEXIST | OFN_CREATEPROMPT | OFN_OVERWRITEPROMPT;
		
		bool res = false;
		
		switch(this._fbm)
		{
			case FileBrowseMode.OPEN:
				res = cast(bool)GetOpenFileNameW(&this._dlgStruct);
				break;
			
			case FileBrowseMode.SAVE:
				res = cast(bool)GetSaveFileNameW(&this._dlgStruct);
				break;
			
			default:
				assert(false, "Unknown browse mode");
		}
		
		if(res)
		{
			this._dlgRes = to!(string)(toUTF8(buffer).ptr);
		}

		return res;
	}
}