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

module dgui.progressbar;

import dgui.core.controls.subclassedcontrol;

class ProgressBar: SubclassedControl
{
	private uint _minRange = 0;
	private uint _maxRange = 100;
	private uint _step = 10;
	private uint _value = 0;

	@property public uint minRange()
	{
		return this._minRange;
	}

	@property public void minRange(uint mr)
	{
		this._minRange = mr;

		if(this.created)
		{
			this.sendMessage(PBM_SETRANGE32, this._minRange, this._maxRange);
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
			this.sendMessage(PBM_SETRANGE32, this._minRange, this._maxRange);
		}
	}

	@property public uint step()
	{
		return this._minRange;
	}

	@property public void step(uint s)
	{
		this._step = s;

		if(this.created)
		{
			this.sendMessage(PBM_SETSTEP, this._step, 0);
		}
	}

	@property public uint value()
	{
		if(this.created)
		{
			return this.sendMessage(PBM_GETPOS, 0, 0);
		}
		
		return this._value;
	}

	@property public void value(uint p)
	{
		this._value = p;

		if(this.created)
		{
			this.sendMessage(PBM_SETPOS, p, 0);
		}
	}

	public void increment()
	{
		if(this.created)
		{
			this.sendMessage(PBM_STEPIT, 0, 0);
		}
		else
		{
			throwException!(DGuiException)("Cannot increment the progress bar");
		}
	}

	protected override void createControlParams(ref CreateControlParams ccp)
	{
		ccp.SuperclassName = WC_PROGRESSBAR;
		ccp.ClassName = WC_DPROGRESSBAR;

		assert(this._dock !is DockStyle.FILL, "ProgressBar: Invalid Dock Style");

		if(this._dock is DockStyle.LEFT || this._dock is DockStyle.RIGHT)
		{
			this.setStyle(PBS_VERTICAL, true);
		}
		
		super.createControlParams(ccp);
	}
	
	protected override void onHandleCreated(EventArgs e)
	{
		this.sendMessage(PBM_SETRANGE32, this._minRange, this._maxRange);
		this.sendMessage(PBM_SETSTEP, this._step, 0);
		this.sendMessage(PBM_SETPOS, this._value, 0);
		
		super.onHandleCreated(e);
	}
}