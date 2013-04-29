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

module dgui.core.exception;

import std.string: format;
import std.windows.syserror;
import dgui.core.winapi: GetLastError;

mixin template ExceptionBody()
{
	public this(string msg)
	{
		super(msg);
	}
}

final class DGuiException: Exception
{
	mixin ExceptionBody;
}

final class Win32Exception: Exception
{
	mixin ExceptionBody;
}

final class RegistryException: Exception
{
	mixin ExceptionBody;
}

final class GdiException: Exception
{
	mixin ExceptionBody;
}

final class WindowsNotSupportedException: Exception
{
	mixin ExceptionBody;
}

void throwException(T1, T2...)(string fmt, T2 args)
{
	static if(is(T1: Win32Exception))
	{
		throw new T1(format(fmt ~ "\nWindows Message: '%s'", args, sysErrorString(GetLastError())));
	}
	else
	{
		throw new T1(format(fmt, args));
	}
}