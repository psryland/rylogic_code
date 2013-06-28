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

module dgui.core.events.eventargs;

class EventArgs
{
	private static EventArgs _empty;
	
	protected this()
	{
		
	}

	@property public static EventArgs empty()
	{
		if(!this._empty)	
		{
			_empty = new EventArgs();
		}

		return _empty;
	}	
}

class CancelEventArgs(T): EventArgs
{
	private bool _cancel = false;
	private T _t;
	
	public this(T t)
	{
		this._t = t;
	}
	
	@property public final bool cancel()
	{
		return this._cancel;
	}

	@property public final void cancel(bool b)
	{
		this._cancel = b;
	}
	
	@property public final T item()
	{
		return this._t;
	}
}

class ItemEventArgs(T): EventArgs
{
	private T _checkedItem;

	public this(T item)
	{
		this._checkedItem = item;
	}

	@property public T item()
	{
		return this._checkedItem;
	}
}

class ItemChangedEventArgs(T): EventArgs
{
	private T _oldItem;
	private T _newItem;

	public this(T oItem, T nItem)
	{
		this._oldItem = oItem;
		this._newItem = nItem;
	}

	@property public T oldItem()
	{
		return this._oldItem;
	}

	@property public T newItem()
	{
		return this._newItem;
	}
}