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

module dgui.fontdialog;

public import dgui.core.dialogs.commondialog;

class FontDialog: CommonDialog!(CHOOSEFONTW, Font)
{
	public override bool showDialog()
	{
		LOGFONTW lf = void;
		
		this._dlgStruct.lStructSize = CHOOSEFONTW.sizeof;
		this._dlgStruct.hwndOwner = GetActiveWindow();
		this._dlgStruct.Flags = CF_INITTOLOGFONTSTRUCT | CF_EFFECTS | CF_SCREENFONTS;
		this._dlgStruct.lpLogFont = &lf;

		if(ChooseFontW(&this._dlgStruct))
		{
			this._dlgRes = Font.fromHFONT(createFontIndirect(&lf));
			return true;
		}

		return false;
	}
}