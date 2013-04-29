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

module dgui.trackbar;

import dgui.core.controls.subclassedcontrol;

class TrackBar: SubclassedControl
{
	public Event!(Control, EventArgs) valueChanged;
	
	private int _minRange = 0;
	private int _maxRange = 100;
	private int _value = 0;
	private int _lastValue = 0;

	@property public uint minRange()
	{
		return this._minRange;
	}

	@property public void minRange(uint mr)
	{
		this._minRange = mr;

		if(this.created)
		{
			this.sendMessage(TBM_SETRANGE, true, MAKELPARAM(this._minRange, this._maxRange));
		}
	}

	@property public uint maxRange()
	{
		return this._maxRange;
	}

	@property public void maxRange(uint mr)
	{
		this._maxRange = mr;

		if(this.created)
		{
			this.sendMessage(TBM_SETRANGE, true, MAKELPARAM(this._minRange, this._maxRange));
		}
	}

	@property public int value()
	{
		if(this.created)
		{
			return this.sendMessage(TBM_GETPOS, 0, 0);
		}
		
		return this._value;
	}
	
	@property public void value(int p)
	{
		this._value = p;
		
		if(this.created)
		{
			this.sendMessage(TBM_SETPOS, true, p);
		}
	}
	
	protected override void createControlParams(ref CreateControlParams ccp)
	{
		ccp.SuperclassName = WC_TRACKBAR;
		ccp.ClassName = WC_DTRACKBAR;
		this.setStyle(TBS_AUTOTICKS, true);

		assert(this._dock is DockStyle.FILL, "TrackBar: Invalid Dock Style");

		if(this._dock is DockStyle.TOP || this._dock is DockStyle.BOTTOM || (this._dock is DockStyle.NONE && this._bounds.width >= this._bounds.height))
		{
			this.setStyle(TBS_HORZ, true);
		}
		else if(this._dock is DockStyle.LEFT || this._dock is DockStyle.RIGHT || (this._dock is DockStyle.NONE && this._bounds.height < this._bounds.width))
		{
			this.setStyle(TBS_VERT, true);
		}
		
		super.createControlParams(ccp);
	}
	
	protected override void onHandleCreated(EventArgs e)
	{
		this.sendMessage(TBM_SETRANGE, true, MAKELPARAM(this._minRange, this._maxRange));
		this.sendMessage(TBM_SETTIC, 20, 0);
		this.sendMessage(TBM_SETPOS, true, this._value);
		
		super.onHandleCreated(e);
	}

	protected override void wndProc(ref Message m)
	{
		if(m.Msg == WM_MOUSEMOVE && (cast(MouseKeys)m.wParam) is MouseKeys.LEFT || 
		   m.Msg == WM_KEYDOWN && ((cast(Keys)m.wParam) is Keys.LEFT || 
		   (cast(Keys)m.wParam) is Keys.UP || 
		   (cast(Keys)m.wParam) is Keys.RIGHT || 
		   (cast(Keys)m.wParam) is Keys.DOWN))
		{
			int val = this.value;
			
			if(this._lastValue != val)
			{
				this._lastValue = val; //Save last position.
				this.onValueChanged(EventArgs.empty);
			}
		}

		super.wndProc(m);
	}

	private void onValueChanged(EventArgs e)
	{
		this.valueChanged(this, e);
	}
}