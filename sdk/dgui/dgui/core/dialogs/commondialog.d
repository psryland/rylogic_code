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

module dgui.core.dialogs.commondialog;

public import dgui.core.charset;
public import dgui.core.winapi;
public import dgui.canvas; // ???

class CommonDialog(T1, T2)
{
	protected T1 _dlgStruct;
	protected T2 _dlgRes;
	protected string _title;
	
	@property public string title()
	{
		return this._title;
	}

	@property public void title(string s)
	{
		this._title = s;
	}	
	
	@property public T2 result()
	{
		return this._dlgRes;
	}

	public abstract bool showDialog();
}