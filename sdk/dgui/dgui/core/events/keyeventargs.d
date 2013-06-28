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

module dgui.core.events.keyeventargs;

public import dgui.core.events.eventargs;

enum Keys: uint // docmain
{
	NONE =     0, /// No keys specified.
	
	///
	SHIFT =    0x10000, /// Modifier keys.
	CONTROL =  0x20000, 
	ALT =      0x40000, 
	
	A = 'A', /// Letters.
	B = 'B', 
	C = 'C', 
	D = 'D', 
	E = 'E', 
	F = 'F', 
	G = 'G', 
	H = 'H', 
	I = 'I', 
	J = 'J', 
	K = 'K', 
	L = 'L', 
	M = 'M', 
	N = 'N', 
	O = 'O', 
	P = 'P', 
	Q = 'Q', 
	R = 'R', 
	S = 'S', 
	T = 'T', 
	U = 'U', 
	V = 'V', 
	W = 'W', 
	X = 'X', 
	Y = 'Y', 
	Z = 'Z', 
	
	D0 = '0', /// Digits.
	D1 = '1', 
	D2 = '2', 
	D3 = '3', 
	D4 = '4', 
	D5 = '5', 
	D6 = '6', 
	D7 = '7', 
	D8 = '8', 
	D9 = '9', 
	
	F1 = 112, /// F - function keys.
	F2 = 113, 
	F3 = 114, 
	F4 = 115, 
	F5 = 116, 
	F6 = 117, 
	F7 = 118, 
	F8 = 119, 
	F9 = 120, 
	F10 = 121, 
	F11 = 122, 
	F12 = 123, 
	F13 = 124, 
	F14 = 125, 
	F15 = 126, 
	F16 = 127, 
	F17 = 128, 
	F18 = 129, 
	F19 = 130, 
	F20 = 131, 
	F21 = 132, 
	F22 = 133, 
	F23 = 134, 
	F24 = 135, 
	
	NUM_PAD0 = 96, /// Numbers on keypad.
	NUM_PAD1 = 97, 
	NUM_PAD2 = 98, 
	NUM_PAD3 = 99, 
	NUM_PAD4 = 100, 
	NUM_PAD5 = 101, 
	NUM_PAD6 = 102, 
	NUM_PAD7 = 103, 
	NUM_PAD8 = 104, 
	NUM_PAD9 = 105, 
	
	ADD = 107, ///
	APPS = 93, /// Application.
	ATTN = 246, ///
	BACK = 8, /// Backspace.
	CANCEL = 3, ///
	CAPITAL = 20, ///
	CAPS_LOCK = 20, 
	CLEAR = 12, ///
	CONTROL_KEY = 17, ///
	CRSEL = 247, ///
	DECIMAL = 110, ///
	DEL = 46, ///
	DELETE = DEL, ///
	PERIOD = 190, ///
	DOT = PERIOD, 
	DIVIDE = 111, ///
	DOWN = 40, /// Down arrow.
	END = 35, ///
	ENTER = 13, ///
	ERASE_EOF = 249, ///
	ESCAPE = 27, ///
	EXECUTE = 43, ///
	EXSEL = 248, ///
	FINAL_MODE = 4, /// IME final mode.
	HANGUL_MODE = 21, /// IME Hangul mode.
	HANGUEL_MODE = 21, 
	HANJA_MODE = 25, /// IME Hanja mode.
	HELP = 47, ///
	HOME = 36, ///
	IME_ACCEPT = 30, ///
	IME_CONVERT = 28, ///
	IME_MODE_CHANGE = 31, ///
	IME_NONCONVERT = 29, ///
	INSERT = 45, ///
	JUNJA_MODE = 23, ///
	KANA_MODE = 21, ///
	KANJI_MODE = 25, ///
	LEFT_CONTROL = 162, /// Left Ctrl.
	LEFT = 37, /// Left arrow.
	LINE_FEED = 10, ///
	LEFT_MENU = 164, /// Left Alt.
	LEFT_SHIFT = 160, ///
	LEFT_WIN = 91, /// Left Windows logo.
	MENU = 18, /// Alt.
	MULTIPLY = 106, ///
	NEXT = 34, /// Page down.
	NO_NAME = 252, // Reserved for future use.
	NUM_LOCK = 144, ///
	OEM8 = 223, // OEM specific.
	OEM_CLEAR = 254,
	PA1 = 253,
	PAGE_DOWN = 34, ///
	PAGE_UP = 33, ///
	PAUSE = 19, ///
	PLAY = 250, ///
	PRINT = 42, ///
	PRINT_SCREEN = 44, ///
	PROCESS_KEY = 229, ///
	RIGHT_CONTROL = 163, /// Right Ctrl.
	RETURN = 13, ///
	RIGHT = 39, /// Right arrow.
	RIGHT_MENU = 165, /// Right Alt.
	RIGHT_SHIFT = 161, ///
	RIGHT_WIN = 92, /// Right Windows logo.
	SCROLL = 145, /// Scroll lock.
	SELECT = 41, ///
	SEPARATOR = 108, ///
	SHIFT_KEY = 16, ///
	SNAPSHOT = 44, /// Print screen.
	SPACE = 32, ///
	SPACEBAR = SPACE, // Extra.
	SUBTRACT = 109, ///
	TAB = 9, ///
	UP = 38, /// Up arrow.
	ZOOM = 251, ///
	
	// Windows 2000+
	BROWSER_BACK = 166, ///
	BROWSER_FAVORITES = 171, 
	BROWSER_FORWARD = 167, 
	BROWSER_HOME = 172, 
	BROWSER_REFRESH = 168, 
	BROWSER_SEARCH = 170, 
	BROWSER_STOP = 169, 
	LAUNCH_APPLICATION1 = 182, ///
	LAUNCH_APPLICATION2 = 183, 
	LAUNCH_MAIL = 180, 
	MEDIA_NEXT_TRACK = 176, ///
	MEDIA_PLAY_PAUSE = 179, 
	MEDIA_PREVIOUS_TRACK = 177, 
	MEDIA_STOP = 178, 
	OEM_BACKSLASH = 226, // OEM angle bracket or backslash.
	OEM_CLOSE_BRACKETS = 221,
	OEM_COMMA = 188,
	OEM_MINUS = 189,
	OEM_OPEN_BRACKETS = 219,
	OEM_PERIOD = 190,
	OEM_PIPE = 220,
	OEM_PLUS = 187,
	OEM_QUESTION = 191,
	OEM_QUOTES = 222,
	OEM_SEMICOLON = 186,
	OEM_TILDE = 192,
	SELECT_MEDIA = 181, ///
	VOLUME_DOWN = 174, ///
	VOLUME_MUTE = 173, 
	VOLUME_UP = 175, 
	
	/// Bit mask to extract key code from key value.
	KEY_CODE = 0xFFFF,
	
	/// Bit mask to extract modifiers from key value.
	MODIFIERS = 0xFFFF0000,
}

class KeyEventArgs: EventArgs
{
	private Keys _keys;
	private bool _handled = true;

	public this(Keys keys)
	{
		this._keys = keys;
	}

	@property public Keys keyCode()
	{
		return this._keys;
	}

	@property public bool handled()
	{
		return this._handled;
	}

	@property public void handled(bool b)
	{
		this._handled = b;
	}
}

class KeyCharEventArgs: KeyEventArgs
{
	private char _keyChar;	

	public this(Keys keys, char keyCh)
	{
		super(keys);
		this._keyChar = keyCh;
	}

	@property public char keyChar()
	{
		return this._keyChar;
	}
}
