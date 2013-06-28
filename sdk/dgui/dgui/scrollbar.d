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

module dgui.scrollbar;

import dgui.core.controls.control; //?? Control ??
import dgui.core.winapi;

enum ScrollBarType
{
	VERTICAL = SB_VERT,
	HORIZONTAL = SB_HORZ,
	SEPARATE = SB_CTL,
}

class ScrollBar: Control
{
	private ScrollBarType _sbt;
	
	public this()
	{
		this._sbt = ScrollBarType.SEPARATE;
	}
	
	private this(Control c, ScrollBarType sbt)
	{
		this._handle = c.handle;
		this._sbt = sbt;
	}
	
	private void setInfo(uint mask, SCROLLINFO* si)
	{
		si.cbSize = SCROLLINFO.sizeof;
		si.fMask = mask | SIF_DISABLENOSCROLL;
		
		SetScrollInfo(this._handle, this._sbt, si, true);
	}
	
	private void getInfo(uint mask, SCROLLINFO* si)
	{
		si.cbSize = SCROLLINFO.sizeof;
		si.fMask = mask;
		
		GetScrollInfo(this._handle, this._sbt, si);
	}
	
	public void setRange(uint min, uint max)
	{
		if(this.created)
		{
			SCROLLINFO si;
			si.nMin = min;
			si.nMax = max;
			
			this.setInfo(SIF_RANGE, &si);
		}
	}
	
	public void increment(int amount = 1)
	{
		this.position = this.position + amount;
	}

	public void decrement(int amount = 1)
	{
		this.position = this.position - amount;
	}
	
	@property public uint minRange()
	{
		if(this.created)
		{
			SCROLLINFO si;
			
			this.getInfo(SIF_RANGE, &si);
			return si.nMin;
		}
		
		return -1;
	}
	
	@property public uint maxRange()
	{
		if(this.created)
		{
			SCROLLINFO si;
			
			this.getInfo(SIF_RANGE, &si);
			return si.nMax;
		}
		
		return -1;
	}
	
	@property public uint position()
	{
		if(this.created)
		{
			SCROLLINFO si;
			
			this.getInfo(SIF_POS, &si);
			return si.nPos;
		}
		
		return -1;		
	}
	
	@property public void position(uint p)
	{
		if(this.created)
		{
			SCROLLINFO si;
			si.nPos = p;
			
			this.setInfo(SIF_POS, &si);
		}
	}
	
	@property public uint page()
	{
		if(this.created)
		{
			SCROLLINFO si;
			
			this.getInfo(SIF_PAGE, &si);
			return si.nPage;
		}
		
		return -1;		
	}
	
	@property public void page(uint p)
	{
		if(this.created)
		{
			SCROLLINFO si;
			si.nPage = p;
			
			this.setInfo(SIF_PAGE, &si);
		}
	}	
	
	public static ScrollBar fromControl(Control c, ScrollBarType sbt)
	{
		assert(sbt !is ScrollBarType.SEPARATE, "ScrollBarType.SEPARATE not allowed here");
		return new ScrollBar(c, sbt);
	}
}