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

module dgui.core.events.event;

struct Event(T1, T2)
{
	private alias void delegate(T1, T2) SlotDelegate;
	private alias void function(T1, T2) SlotFunction;	

	private SlotDelegate[] _slotDg;
	private SlotFunction[] _slotFn;

	public alias opCall call;

	public void opCall(T1 t1, T2 t2)
	{
		synchronized
		{
			for(int i = 0; i < this._slotDg.length; i++)
			{
				this._slotDg[i](t1, t2);
			}

			for(int i = 0; i < this._slotFn.length; i++)
			{
				this._slotFn[i](t1, t2);
			}
		}
	}

	public void attach(SlotDelegate dg)
	{
		if(dg)
		{
			this._slotDg ~= dg;
		}
	}

	public void attach(SlotFunction fn)
	{
		if(fn)
		{
			this._slotFn ~= fn;
		}
	}
}