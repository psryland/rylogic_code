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

module dgui.button;

import dgui.core.controls.abstractbutton;

/// Standarde windows _Button 
class Button: AbstractButton
{
	/**
	  Returns:
		A DialogResult enum (OK, IGNORE, CLOSE, YES, NO, CANCEL, ...)
	
	See_Also:
		Form.showDialog()
	  */
	@property public DialogResult dialogResult()
	{
		return this._dr;
	}
	
	/**
	  Sets DialogResult for a button
	
	  Params:
		dr = DialogResult of the button.
	
	  See_Also:
		Form.showDialog()
	  */
	@property public void dialogResult(DialogResult dr)
	{
		this._dr = dr;
	}
	
	protected override void createControlParams(ref CreateControlParams ccp)
	{
		switch(this._drawMode)
		{
			case OwnerDrawMode.NORMAL:
				this.setStyle(BS_DEFPUSHBUTTON, true);
				break;
			
			case OwnerDrawMode.FIXED, OwnerDrawMode.VARIABLE:
				this.setStyle(BS_OWNERDRAW, true);
				break;
			
			default:
				break;
		}
		
		ccp.ClassName = WC_DBUTTON;

		super.createControlParams(ccp);
	}
}

/// Standard windows _CheckBox
class CheckBox: CheckedButton
{	
	protected override void createControlParams(ref CreateControlParams ccp)
	{
		switch(this._drawMode)
		{
			case OwnerDrawMode.NORMAL:
				this.setStyle(BS_AUTOCHECKBOX, true);
				break;
			
			case OwnerDrawMode.FIXED, OwnerDrawMode.VARIABLE:
				this.setStyle(BS_OWNERDRAW, true);
				break;
			
			default:
				break;
		}		
		
		ccp.ClassName = WC_DCHECKBOX;

		super.createControlParams(ccp);
	}
}

/// Standard windows _RadioButton
class RadioButton: CheckedButton
{	
	protected override void createControlParams(ref CreateControlParams ccp)
	{
		switch(this._drawMode)
		{
			case OwnerDrawMode.NORMAL:
				this.setStyle(BS_AUTORADIOBUTTON, true);
				break;
			
			case OwnerDrawMode.FIXED, OwnerDrawMode.VARIABLE:
				this.setStyle(BS_OWNERDRAW, true);
				break;
			
			default:
				break;
		}
		
		ccp.ClassName = WC_DRADIOBUTTON;

		super.createControlParams(ccp);
	}
}