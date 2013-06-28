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

module dgui.core.events.controlcodeeventargs;

public import dgui.core.events.eventargs;
public import dgui.core.winapi;

enum ControlCode: uint
{
	IGNORE					= 0,
	BUTTON 			    	= DLGC_BUTTON,
	DEFAULT_PUSH_BUTTON 	= DLGC_DEFPUSHBUTTON,
	HAS_SETSEL				= DLGC_HASSETSEL,
	RADIO_BUTTON			= DLGC_RADIOBUTTON,
	STATIC					= DLGC_STATIC,
	NO_DEFAULT_PUSH_BUTTON  = DLGC_UNDEFPUSHBUTTON,
	WANT_ALL_KEYS			= DLGC_WANTALLKEYS,
	WANT_ARROWS				= DLGC_WANTARROWS,
	WANT_CHARS				= DLGC_WANTCHARS,
	WANT_TAB				= DLGC_WANTTAB,
}

class ControlCodeEventArgs: EventArgs
{
	private ControlCode _ctrlCode = ControlCode.IGNORE;
	
	@property public ControlCode controlCode()
	{
		return this._ctrlCode;
	}
	
	@property public void controlCode(ControlCode cc)
	{
		this._ctrlCode = cc;
	}
}