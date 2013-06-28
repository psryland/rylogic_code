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

/*
   From MSDN
  
   Rich Edit version	DLL	Window Class
   ----
   1.0	Riched32.dll	RICHEDIT_CLASS
   2.0	Riched20.dll	RICHEDIT_CLASS
   3.0	Riched20.dll	RICHEDIT_CLASS
   4.1	Msftedit.dll	MSFTEDIT_CLASS
  
   Windows XP SP1:  Includes Microsoft Rich Edit 4.1, Microsoft Rich Edit 3.0, and a Microsoft Rich Edit 1.0 emulator.
   Windows XP: 		Includes Microsoft Rich Edit 3.0 with a Microsoft Rich Edit 1.0 emulator.
   Windows Me: 		Includes Microsoft Rich Edit 1.0 and 3.0.
   Windows 2000:	Includes Microsoft Rich Edit 3.0 with a Microsoft Rich Edit 1.0 emulator.
   Windows NT 4.0:	Includes Microsoft Rich Edit 1.0 and 2.0.
   Windows 98:		Includes Microsoft Rich Edit 1.0 and 2.0.
   Windows 95:		Includes only Microsoft Rich Edit 1.0. However, Riched20.dll is compatible with Windows 95 and may be installed by an application that requires it.
 */

module dgui.richtextbox;

public import dgui.core.controls.textcontrol;

class RichTextBox: TextControl
{
	private static int _refCount = 0;
	private static HMODULE _hRichDll;

	public override void dispose()
	{
		--_refCount;
		
		if(!_refCount)
		{
			FreeLibrary(_hRichDll);
			_hRichDll = null;
		}
		
		super.dispose();
	}

	public void redo()
	{
		this.sendMessage(EM_REDO, 0, 0);
	}

	protected override void createControlParams(ref CreateControlParams ccp)
	{
		// Probably the RichTextbox ignores the wParam parameter in WM_PAINT
		
		++_refCount;

		if(!_hRichDll)
		{
			_hRichDll = loadLibrary("RichEd20.dll"); // Load the standard version
		}
		
		this.setStyle(ES_MULTILINE | ES_WANTRETURN, true);
		ccp.SuperclassName = WC_RICHEDIT;
		ccp.ClassName = WC_DRICHEDIT;
		
		super.createControlParams(ccp);
	}

	protected override void onHandleCreated(EventArgs e)
	{
		super.onHandleCreated(e);

		this.sendMessage(EM_SETEVENTMASK, 0, ENM_CHANGE | ENM_UPDATE);
		this.sendMessage(EM_SETBKGNDCOLOR, 0, this._backColor.colorref);
	}
}