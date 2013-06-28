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

module dgui.core.utils;

import dgui.core.winapi;
import dgui.core.charset;

enum WindowsVersion
{
	UNKNOWN       = 0,
	WINDOWS_2000  = 1,
	WINDOWS_XP    = 2,
	WINDOWS_VISTA = 4,
	WINDOWS_7     = 8,
}

T winCast(T)(Object o)
{
	return cast(T)(cast(void*)o);
}

T winCast(T)(size_t st)
{
	return cast(T)(cast(void*)st);
}

HINSTANCE getHInstance()
{
	static HINSTANCE hInst = null;

	if(!hInst)
	{
		hInst = GetModuleHandleW(null);
	}	

	return hInst;
}

string getExecutablePath()
{
	static string exePath;

	if(!exePath.length)
	{
		exePath = getModuleFileName(null);
	}

	return exePath;
}

string getStartupPath()
{
	static string startPath;

	if(!startPath.length)
	{
		startPath = std.path.dirName(getExecutablePath());
	}

	return startPath;
}

string getTempPath()
{
	static string tempPath;

	if(!tempPath.length)
	{
		dgui.core.charset.getTempPath(tempPath);
	}

	return tempPath;	
}

string makeFilter(string userFilter)
{
	char[] newFilter = cast(char[])userFilter;

	foreach(ref char ch; newFilter)
	{
		if(ch == '|')
		{
			ch = '\0';
		}
	}

	newFilter ~= '\0';
	return newFilter.idup;
}

public WindowsVersion getWindowsVersion()
{
	static WindowsVersion ver = WindowsVersion.UNKNOWN;
	static WindowsVersion[uint][uint] versions;
		
	if(ver is WindowsVersion.UNKNOWN)
	{
		if(!versions.length)
		{
			versions[5][0] = WindowsVersion.WINDOWS_2000;
			versions[5][1] = WindowsVersion.WINDOWS_XP;
			versions[6][0] = WindowsVersion.WINDOWS_VISTA;
			versions[6][1] = WindowsVersion.WINDOWS_7;
		}

		OSVERSIONINFOW ovi;
		ovi.dwOSVersionInfoSize = OSVERSIONINFOW.sizeof;
		
		GetVersionExW(&ovi);
		
		WindowsVersion[uint]* pMajVer = (ovi.dwMajorVersion in versions);
		
		if(pMajVer)
		{
			WindowsVersion* pMinVer = (ovi.dwMinorVersion in *pMajVer);
			
			if(pMinVer)
			{
				ver = versions[ovi.dwMajorVersion][ovi.dwMinorVersion];
			}
		}
	}
	
	return ver;
}