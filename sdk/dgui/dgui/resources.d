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

module dgui.resources;

import dgui.core.charset;
import dgui.core.winapi;
import dgui.core.geometry;
import dgui.core.utils;
import dgui.core.exception;
import dgui.canvas;

final class Resources
{
	private static Resources _rsrc;
	
	private this()
	{
		
	}	

	public Icon getIcon(ushort id)
	{
		return getIcon(id, NullSize);
	}
	
	public Icon getIcon(ushort id, Size sz)
	{
		HICON hIcon = loadImage(getHInstance(), cast(wchar*)id, IMAGE_ICON, sz.width, sz.height, LR_LOADTRANSPARENT | (sz == NullSize ? LR_DEFAULTSIZE : 0));

		if(!hIcon)
		{
			throwException!(GdiException)("Cannot load Icon: '%d'", id);
		}
		
		return Icon.fromHICON(hIcon);
	}

	public Bitmap getBitmap(ushort id)
	{
		HBITMAP hBitmap = loadImage(getHInstance(), cast(wchar*)id, IMAGE_BITMAP, 0, 0, LR_LOADTRANSPARENT | LR_DEFAULTSIZE);

		if(!hBitmap)
		{
			throwException!(GdiException)("Cannot load Bitmap: '%d'", id);
		}

		return Bitmap.fromHBITMAP(hBitmap);
	}

	public T* getRaw(T)(ushort id, char* rt)
	{
		HRSRC hRsrc = FindResourceW(null, MAKEINTRESOURCEW(id), rt);

		if(!hRsrc)
		{
			throwException!(GdiException)("Cannot load Custom Resource: '%d'", id);
		}

		return cast(T*)LockResource(LoadResource(null, hRsrc));
	}

	@property public static Resources instance()
	{
		if(!_rsrc)
		{
			_rsrc = new Resources();
		}

		return _rsrc;
	}
}