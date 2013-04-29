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

module dgui.colordialog;

public import dgui.core.dialogs.commondialog;

class ColorDialog: CommonDialog!(CHOOSECOLORW, Color)
{
	public override bool showDialog()
	{
		static COLORREF[16] custColors;
		custColors[] = RGB(255, 255, 255);
		
		this._dlgStruct.lStructSize = CHOOSECOLORW.sizeof;
		this._dlgStruct.lpCustColors = custColors.ptr; // Must be defined !!!
		this._dlgStruct.hwndOwner = GetActiveWindow();

		if(ChooseColorW(&this._dlgStruct))
		{
			this._dlgRes = Color.fromCOLORREF(this._dlgStruct.rgbResult);
			return true;
		}

		return false;
	}
}