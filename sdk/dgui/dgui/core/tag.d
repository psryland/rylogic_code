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

module dgui.core.tag;

public import std.variant;

mixin template TagProperty()
{
	private Variant _tt;
    
	/*
	 *	DMD 2.052 BUG: Cannot differentiate var(T)() and var(T)(T t) 
	 *	template functions, use variadic template with length check.
	 */
	@property public T[0] tag(T...)()
	{
		static assert(T.length == 1, "Multiple parameters not allowed");
		return this._tt.get!(T[0]);
	}
	
	@property public void tag(T)(T t)
	{
		this._tt = t;
	}
}