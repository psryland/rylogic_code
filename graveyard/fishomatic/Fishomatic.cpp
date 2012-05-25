#include <stdio.h>
#include <windows.h>
#include "pr/maths/maths.h"
#include "pr/geometry/colour.h"
#include "pr/common/windowfunctions.h"
#include "pr/common/commandline.h"

using namespace pr;

inline bool KeyDown(int vkey)
{
	return (GetAsyncKeyState(vkey) & 0x8000) != 0;
}
inline bool KeyPressed(int vkey)
{
	if (!KeyDown(vkey)) return false;
	while (KeyDown(vkey)) Sleep(10);
	return true;
}

void SaveBitmap(char *szFilename,HBITMAP hBitmap);

struct Options : cmdline::IOptionReceiver
{
	bool CmdLineOption(std::string const& option, cmdline::TArgIter& arg, cmdline::TArgIter arg_end)
	{
		if	   ( str::EqualNoCase(option, "-fish_key") && arg != arg_end )			{ m_fish_key = (arg++)->c_str()[0]; return true; }
		else if( str::EqualNoCase(option, "-move_delta") && arg != arg_end )		{ m_move_delta = (int)strtol((arg++)->c_str(), 0, 10); return true; }
		else if( str::EqualNoCase(option, "-search") && arg_end - arg >= 4 )		{ m_search_bounds.left = (int)strtol((arg++)->c_str(), 0, 10); m_search_bounds.top = (int)strtol((arg++)->c_str(), 0, 10); m_search_bounds.right = (int)strtol((arg++)->c_str(), 0, 10); m_search_bounds.bottom = (int)strtol((arg++)->c_str(), 0, 10); return true; }
		else if( str::EqualNoCase(option, "-colour") && arg != arg_end )			{ m_target_colour.m_aarrggbb = (unsigned int)strtoul((arg++)->c_str(), 0, 16); return true; }
		else if( str::EqualNoCase(option, "-col_tol") && arg != arg_end )			{ m_col_tol = (int)strtol((arg++)->c_str(), 0, 10); return true; }
		else if( str::EqualNoCase(option, "-click_delay") && arg != arg_end )		{ m_click_delay = (DWORD)strtoul((arg++)->c_str(), 0, 10); return true; }
		else if( str::EqualNoCase(option, "-after_cast_wait") && arg != arg_end )	{ m_after_cast_wait = (DWORD)strtoul((arg++)->c_str(), 0, 10); return true; }
		else if( str::EqualNoCase(option, "-after_catch_wait") && arg != arg_end )	{ m_after_catch_wait = (DWORD)strtoul((arg++)->c_str(), 0, 10); return true; }
		else if( str::EqualNoCase(option, "-move_mouse") )							{ m_move_mouse = !m_move_mouse; return true; }
		else if( str::EqualNoCase(option, "-max_fish_cycle") && arg != arg_end )	{ m_max_fish_cycle = (DWORD)strtoul((arg++)->c_str(), 0, 10); return true; }
		else if( str::EqualNoCase(option, "-abort_time") && arg != arg_end )		{ m_abort_time = (DWORD)strtoul((arg++)->c_str(), 0, 10); return true; }
		else if( str::EqualNoCase(option, "-baubles_key") && arg != arg_end )		{ m_baubles_key = (arg++)->c_str()[0]; return true; }
		else if( str::EqualNoCase(option, "-rod_key") && arg != arg_end )			{ m_rod_key = (arg++)->c_str()[0]; return true; }
		else if( str::EqualNoCase(option, "-baubles_time") && arg != arg_end )		{ m_baubles_time = (DWORD)strtoul((arg++)->c_str(), 0, 10); return true; }
		else if( str::EqualNoCase(option, "-baubles_apply_wait") && arg != arg_end ){ m_baubles_apply_wait = (DWORD)strtoul((arg++)->c_str(), 0, 10); return true; }
		else if( str::EqualNoCase(option, "-h") )									{ ShowHelp(); return false; }
		else   { printf("Error: Unknown option '%s'\n", option.c_str()); ShowHelp(); return false; }
	}
	void ShowHelp() const
	{
		printf(
			"Use: Fishomatic [options]\n"
			" Options:\n"
			"  -fish_key X = the key to press to start\n"
			"                fishing (default '%c')\n"
			"  -move_delta X = the threshold distance the\n"
			"                bobber can move to trigger a\n"
			"                caught fish (default %d)\n"
			"  -search L T R B = sets the search area in which to look\n"
			"                for the bobber (default %d %d %d %d)\n"
			"  -colour AARRGGBB = the key to press to start\n"
			"                fishing (default '%8.8X')\n"
			"  -col_tol X = the tolerance used to decide when\n"
			"                the target colour has been found\n"
			"                (default %d)\n"
			"  -click_delay X = length of time to wait after\n"
			"                the bobber has moved before clicking\n"
			"                the right mouse button (default %d)\n"
			"  -after_cast_wait X = length of time to wait\n"
			"                after casting before looking for\n"
			"                the bobber in ms (default %d)\n"
			"  -after_catch_wait X = length of time to wait\n"
			"                after catching a fish before\n"
			"                recasting in ms (default %d)\n"
			"  -move_mouse = turns on moving the mouse\n"
			"  -max_fish_cycle X = the maximum length of time for a\n"
			"                complete fishing cycle (default %d)\n"
			"  -abort_time X = The length of time to wait before deciding\n"
			"                the bobber can't be found (default %d)\n"
			"  -baubles_key X = the key to press to 'use baubles' (default '%c')\n"
			"  -rod_key X = the key to press to select your fishing rod (default '%c')\n"
			"  -baubles_time X = the time to wait in minutes before applying\n"
			"                baubles (default %d)\n"
			"  -baubles_apply_wait X = the length of time to wait while applying\n"
			"                baubles to your poll (default %d)\n"
			"\n"
			" Use the 'scroll lock' key to toggle pause mode ON/OFF\n"
			" While in pause mode use:\n"
			"   'h' key to display this help\n"
			"   Use the 'ctrl' key to set the target colour\n"
			"   Use the 'tab' key to show the search bounds\n"
			"   Use 'shift' with:\n"
			"      '1' to increase the move delta tolerance\n"
			"      '2' to decrease the move delta tolerance\n"
			"      '3' to set the top left corner of the search area\n"
			"      '4' to set the bottom right corner of the search area\n"
			//"  -use_baubles = use this switch to enable using baubles (default false)\n"
			//"  -baubles_key X = the key to press that applies baubles to your rod (default '8')\n"
			//"  -baubles_freq X = the time in minutes between re-applying baubles to your rod (default 12min)\n"
			,m_fish_key
			,m_move_delta
			,m_search_bounds.left ,m_search_bounds.top ,m_search_bounds.right ,m_search_bounds.bottom
			,m_target_colour.m_aarrggbb
			,m_col_tol
			,m_click_delay
			,m_after_cast_wait
			,m_after_catch_wait
			,m_max_fish_cycle
			,m_abort_time
			,m_baubles_key
			,m_rod_key
			,m_baubles_time
			,m_baubles_apply_wait);
	}
	void Display() const
	{
		printf(
			"Current Settings:\n"
			"  Fish key:          '%c'\n"
			"  Move delta:         %d\n"
			"  Search Bounds:      %d %d %d %d\n"
			"  Search colour:      %8.8X\n"
			"  Colour find Tol:    %d\n"
			"  Click delay:        %d\n"
			"  After cast wait:    %d\n"
			"  After catch wait:   %d\n"
			"  Move mouse:         %s\n"
			"  Max fish cycle:     %d\n"
			"  Not Found time:     %d\n"
			"  Baubles key:       '%c'\n"
			"  Rod key:           '%c'\n"
			"  Baubles timer:      %d min\n"
			"  Apply baubles wait: %d\n"
			,m_fish_key
			,m_move_delta
			,m_search_bounds.left ,m_search_bounds.top ,m_search_bounds.right ,m_search_bounds.bottom
			,m_target_colour.m_aarrggbb
			,m_col_tol
			,m_click_delay
			,m_after_cast_wait
			,m_after_catch_wait
			,m_move_mouse ? "on" : "off"
			,m_max_fish_cycle
			,m_abort_time
			,m_baubles_key
			,m_rod_key
			,m_baubles_time
			,m_baubles_apply_wait);
	}
	void SetDefaultBounds(HWND hwnd)
	{
		GetClientRect(hwnd, &m_search_bounds);
		m_search_bounds.left   = (m_search_bounds.right - m_search_bounds.left) * 90 / 1280;
		m_search_bounds.top    = (m_search_bounds.bottom - m_search_bounds.top) * 90 / 1024;
		m_search_bounds.right  = (m_search_bounds.right * 1080) / 1280;
		m_search_bounds.bottom = (m_search_bounds.bottom * 850) / 1024;
	}
	RECT MakeRect(int l, int t, int r, int b) { RECT rect = {l,t,r,b}; return rect; }
	DWORD MinsToTicks(int minutes) { return minutes * 60 * 1000; }

	Options()
	:m_fish_key('7')
	,m_move_delta(11)
	,m_search_bounds(MakeRect(90, 90, 1080, 850))
	,m_target_colour(pr::Colour32::make(0x00962C1E))
	,m_col_tol(20)
	,m_click_delay(250)
	,m_after_cast_wait(3000)
	,m_after_catch_wait(3000)
	,m_move_mouse(true)
	,m_max_fish_cycle(23000)
	,m_abort_time(10000)
	,m_baubles_key('9')
	,m_rod_key('0')
	,m_baubles_time(11)
	,m_baubles_apply_wait(6000)
	{}
	int			m_fish_key;
	int			m_move_delta;
	RECT		m_search_bounds;
	Colour32	m_target_colour;
	int			m_col_tol;
	DWORD		m_click_delay;
	DWORD		m_after_cast_wait;
	DWORD		m_after_catch_wait;
	bool		m_move_mouse;
	DWORD		m_max_fish_cycle;
	DWORD		m_abort_time;
	int			m_baubles_key;
	int			m_rod_key;
	DWORD		m_baubles_time;
	DWORD		m_baubles_apply_wait;
} g_options;

struct Screenie
{
	Colour32*	m_buffer;
	int			m_width;
	int			m_height;

	Screenie()
	:m_buffer(0)
	{}
	~Screenie()
	{
		free(m_buffer);
	}
	void Capture(HWND hwnd)
	{
		RECT rect; GetClientRect(hwnd, &rect);
		m_width  = rect.right - rect.left;
		m_height = rect.bottom - rect.top;
		HDC dc = GetDC(hwnd);
		HDC capt = CreateCompatibleDC(dc);
		HBITMAP bitmap = CreateCompatibleBitmap(dc, m_width, m_height);
		SelectObject(capt, bitmap); 
		BitBlt(capt, 0, 0, m_width, m_height, dc, 0, 0, SRCCOPY|CAPTUREBLT); 

		// Get the image size info
		BITMAPINFO bmp_info; ZeroMemory(&bmp_info, sizeof(BITMAPINFO));
		bmp_info.bmiHeader.biSize = sizeof(BITMAPINFOHEADER);
		GetDIBits(dc, bitmap, 0, 0, NULL, &bmp_info, DIB_RGB_COLORS);
		bmp_info.bmiHeader.biCompression = BI_RGB;
		
		// Read the bits
		m_buffer = (Colour32*)malloc(bmp_info.bmiHeader.biSizeImage);
		GetDIBits(dc, bitmap, 0, bmp_info.bmiHeader.biHeight, (void*)m_buffer, &bmp_info, DIB_RGB_COLORS);

		SaveBitmap("window_capture.bmp", bitmap);

		ReleaseDC(hwnd, dc);
		DeleteDC(capt);
		DeleteObject(bitmap);
	}
};

void ReadPixelColour(HWND hwnd, Colour32& target)
{
	Screenie scn; scn.Capture(hwnd);

	POINT pt;
	GetCursorPos(&pt);
	ScreenToClient(hwnd, &pt);
	
	pt.x = Clamp<LONG>(pt.x, 0, scn.m_width);
	pt.y = Clamp<LONG>(pt.y, 0, scn.m_height);
	target = *(scn.m_buffer + (scn.m_height - 1 - pt.y)*scn.m_width + pt.x);
}

void PressKey(HWND hwnd, int key)
{
	SendMessage(hwnd, WM_KEYDOWN, key, 0x00080001);
	SendMessage(hwnd, WM_KEYUP, key, 0x00080001);
	Sleep(200);
}

void Click(HWND hwnd, iv2 const& pos, int button = MK_LBUTTON)
{
	POINT pt = {pos.x, pos.y};

	DWORD down_msg = button == MK_LBUTTON ? WM_LBUTTONDOWN : WM_RBUTTONDOWN;
	DWORD up_msg   = button == MK_LBUTTON ? WM_LBUTTONDOWN : WM_RBUTTONUP;

	POINT current_pos;
	GetCursorPos(&current_pos);

	POINT scn_pt = pt;
	ClientToScreen(hwnd, &scn_pt);
	SetCursorPos(scn_pt.x, scn_pt.y);
	SendMessage(hwnd, down_msg, button, MAKELPARAM(pt.x, pt.y));
	SendMessage(hwnd, up_msg, button, MAKELPARAM(pt.x, pt.y));
	ClipCursor(0);

	SetCursorPos(current_pos.x, current_pos.y);
	Sleep(100);
}

bool FindBobberPosition(HWND hwnd, iv2& position, Colour32 target, Colour32& found_colour)
{
	Screenie scn; scn.Capture(hwnd);

	int min_x = Clamp<int>(g_options.m_search_bounds.left  , 0, scn.m_width);
	int max_x = Clamp<int>(g_options.m_search_bounds.right , 0, scn.m_width);
	int min_y = Clamp<int>(g_options.m_search_bounds.top   , 0, scn.m_height);
	int max_y = Clamp<int>(g_options.m_search_bounds.bottom, 0, scn.m_height);

	int distance = maths::int_max;
	for( int y = min_y; y != max_y; ++y )
	{
		Colour32* col = scn.m_buffer + (scn.m_height-1-y) * scn.m_width + min_x;
		for( int x = min_x; x != max_x; ++x, ++col )
		{
			int distsq = DistanceSq(target, *col);
			if( distsq < distance )
			{
				found_colour = *col;
				position.x = x;
				position.y = y;
				distance = distsq;
			}
		}
	}
	distance = IntegerSqrt(distance);
	if( distance < g_options.m_col_tol )
	{
		POINT pt = {position.x, position.y};
		ClientToScreen(hwnd, &pt);
		if( g_options.m_move_mouse ) SetCursorPos(pt.x, pt.y);
		return true;
	}

	static int output = 0;
	if( (++output %= 100) == 1 )
	{
		printf("Best matching colour difference: %d\n", distance);
	}
	return false;
}

bool BobberMoved(HWND hwnd, iv2 const& position, Colour32 target, int& max_delta, DWORD start_cast)
{
	Screenie scn; scn.Capture(hwnd);
	int radius = 50;
	int min_x = Clamp(position.x - radius, 0, scn.m_width);
	int max_x = Clamp(position.x + radius, 0, scn.m_width);
	int min_y = Clamp(position.y - radius, 0, scn.m_height);
	int max_y = Clamp(position.y + radius, 0, scn.m_height);

	iv2 nearest; int distance = maths::int_max;
	for( int y = min_y; y != max_y; ++y )
	{
		Colour32* col = scn.m_buffer + (scn.m_height-1-y) * scn.m_width + min_x;
		for( int x = min_x; x != max_x; ++x, ++col )
		{
			int distsq = DistanceSq(target, *col);
			if( distsq < distance )
			{
				distance = distsq;
				nearest.x = x;
				nearest.y = y;
			}
		}
	}
	distance = IntegerSqrt(distance);
	int dist = static_cast<int>(Length2(position - nearest));
	if( dist > max_delta ) max_delta = dist;
	POINT pt = {nearest.x, nearest.y};
	ClientToScreen(hwnd, &pt);
	if( g_options.m_move_mouse ) SetCursorPos(pt.x, pt.y);
	printf("Position delta: %d (max: %d)  Colour delta: %d   Remaining time: %d      \r", dist, max_delta, distance, (start_cast + g_options.m_max_fish_cycle - GetTickCount()) / 1000);
	return dist > g_options.m_move_delta;
}

int main(int argc, char* argv[])
{
	printf("**** Pauls Owesome Fish-o-matic ****\n");
	HWND hwnd = pr::GetWindowByName(_T("World of Warcraft"), true);
	if (hwnd == 0)
	{
		printf("Couldn't find 'World of Warcraft' window. Quitting\n");
		return 0;
	}
	g_options.SetDefaultBounds(hwnd);

	if( !EnumCommandLine(argc, argv, g_options) ) return 0;

	Colour32 found_colour = Colour32Black;
	g_options.Display();

	bool paused = true;
	printf("Use Scroll Lock to toggle pause mode ON/OFF\nPress 'h' for more help\n");
	printf("Pause %s\n", paused ? "ON" : "OFF");

	DWORD mins_elaspsed = 0;
	DWORD baubles_start_time = GetTickCount();
	for(;;)
	{
		int state = 0;
		iv2 bobber_position;
		int max_delta;
		bool setting_target_colour = false;
		DWORD start_cast = 0, state_change = 0;
		DWORD start_time = GetTickCount();
		while( state != -1 )
		{
			// Toggle pause
			if (GetAsyncKeyState(VK_SCROLL) & 0x8000)
			{
				paused = !paused;
				printf("Pause %s\n", paused ? "ON" : "OFF");
				Sleep(500);
				baubles_start_time = GetTickCount();
				state = 0;
			}

			if (paused)
			{
				if (GetAsyncKeyState(VK_TAB) & 0x8000)
				{
					PAINTSTRUCT ps;
					HDC hdc = BeginPaint(hwnd, &ps);
					Rectangle(hdc, g_options.m_search_bounds.left, g_options.m_search_bounds.top, g_options.m_search_bounds.right, g_options.m_search_bounds.bottom);
					EndPaint(hwnd, &ps);
					//HDC dc = GetDC(hwnd);
					//Rectangle(dc, g_options.m_search_bounds.left, g_options.m_search_bounds.top, g_options.m_search_bounds.right, g_options.m_search_bounds.bottom);
					//ReleaseDC(hwnd, dc);
					//PostMessage(hwnd, WM_PAINT, 0, 0);
				}

				if( GetAsyncKeyState('H') & 0x8000 )
				{
					g_options.ShowHelp();
					Sleep(200);
				}				
				if (GetForegroundWindow() == GetConsoleWindow())//GetAsyncKeyState(VK_SHIFT) & 0x8000)
				{
					if( GetAsyncKeyState('1') & 0x8000 )
					{
						--g_options.m_move_delta;
						printf("Move delta set to: %d\n", g_options.m_move_delta);
						Sleep(200);
					}
					if( GetAsyncKeyState('2') & 0x8000 )
					{
						++g_options.m_move_delta;
						printf("Move delta set to: %d\n", g_options.m_move_delta);
						Sleep(200);
					}
					if( GetAsyncKeyState('3') & 0x8000 )
					{
						POINT pos;
						GetCursorPos(&pos);
						ScreenToClient(hwnd, &pos);
						g_options.m_search_bounds.left = pos.x;
						g_options.m_search_bounds.top = pos.y;
						printf("Search bounds set to (%d,%d - %d,%d)\n"
							,g_options.m_search_bounds.left
							,g_options.m_search_bounds.top
							,g_options.m_search_bounds.right
							,g_options.m_search_bounds.bottom);
						Sleep(200);
					}
					if( GetAsyncKeyState('4') & 0x8000 )
					{
						POINT pos;
						GetCursorPos(&pos);
						ScreenToClient(hwnd, &pos);
						g_options.m_search_bounds.right = pos.x;
						g_options.m_search_bounds.bottom = pos.y;
						printf("Search bounds set to (%d,%d - %d,%d)\n"
							,g_options.m_search_bounds.left
							,g_options.m_search_bounds.top
							,g_options.m_search_bounds.right
							,g_options.m_search_bounds.bottom);
						Sleep(200);
					}
				}
				if( GetAsyncKeyState(VK_CONTROL) & 0x8000 )
				{
					ReadPixelColour(hwnd, g_options.m_target_colour);
					printf("Target colour set to: %8.8X      \r", g_options.m_target_colour.m_aarrggbb);
					setting_target_colour = true;
				}
				else if( setting_target_colour )
				{
					printf("\n");
					setting_target_colour = false;
				}
			}
			else // !paused
			{
				if( GetTickCount() - baubles_start_time > mins_elaspsed * 60000 )
				{
					if( mins_elaspsed < g_options.m_baubles_time )
					{
						printf("%d mins till baubles will be applied\n", g_options.m_baubles_time - mins_elaspsed);
						++mins_elaspsed;
					}
				}

				if( GetTickCount() < state_change )
				{
					int num = printf(" <%d>        ", (state_change - GetTickCount()) / 1000);
					while( num-- ) printf("\b");
					Sleep(50);
				}
				else
				{
					switch( state )
					{
					case 0: // Send start fishing button press
						printf("Casting\n");
						PressKey(hwnd, g_options.m_fish_key);
						state_change = GetTickCount() + g_options.m_after_cast_wait;
						start_cast = GetTickCount();
						++state;
						break;
					case 1:
						printf("Looking for bobber\n");
						++state;
						break;
					case 2: // Find the bobber position
						if( FindBobberPosition(hwnd, bobber_position, g_options.m_target_colour, found_colour) )
						{
							printf("Bobber found at (%d,%d)\n", bobber_position.x, bobber_position.y);
							max_delta = 0;
							++state;
						}
						if( GetTickCount() - start_cast > g_options.m_abort_time )
						{
							printf("Bobber not found\n");
							state = 5;
						}
						break;
					case 3: // Keep checking that spot
						if( BobberMoved(hwnd, bobber_position, found_colour, max_delta, start_cast) )
						{
							printf("\nBobber moved\n");
							state_change = GetTickCount() + g_options.m_click_delay;
							++state;
						}
						if( GetTickCount() - start_time > g_options.m_max_fish_cycle )
						{
							printf("\nNo bobber movement detected\n");
							state = 5;
						}
						break;
					case 4: // Bobber has moved
						Click(hwnd, bobber_position, MK_RBUTTON);
						printf("Catching Fish (hopefully)\n");
						state_change = GetTickCount() + g_options.m_after_catch_wait;
						++state;
						break;
					case 5:
						if( mins_elaspsed < g_options.m_baubles_time )
							state = -1;
						else
							state = 100;
						break;
					
					// Baubles
					case 100:
						printf("Applying baubles...");
						PressKey(hwnd, g_options.m_baubles_key);
						state_change = GetTickCount() + 500;
						++state;
						break;
					case 101:
						printf("to your fishing poll...");
						PressKey(hwnd, g_options.m_rod_key);
						state_change = GetTickCount() + 500;
						++state;
						break;
					case 102:
						printf("waiting...");
						state_change = GetTickCount() + g_options.m_baubles_apply_wait;
						++state;
						break;
					case 103:
						printf("done.\n");
						baubles_start_time = GetTickCount();
						mins_elaspsed = 0;
						state = -1;
					}
				}
			}
		}
	}
}

void SaveBitmap(char *szFilename,HBITMAP hBitmap)
{
	HDC					hdc=NULL;
	FILE*				fp=NULL;
	LPVOID				pBuf=NULL;
	BITMAPINFO			bmpInfo;
	BITMAPFILEHEADER	bmpFileHeader;

	for(;;)
	{
		hdc=GetDC(NULL);
		ZeroMemory(&bmpInfo,sizeof(BITMAPINFO));
		bmpInfo.bmiHeader.biSize=sizeof(BITMAPINFOHEADER);
		GetDIBits(hdc,hBitmap,0,0,NULL,&bmpInfo,DIB_RGB_COLORS);

		if(bmpInfo.bmiHeader.biSizeImage<=0)
			bmpInfo.bmiHeader.biSizeImage=bmpInfo.bmiHeader.biWidth*abs(bmpInfo.bmiHeader.biHeight)*(bmpInfo.bmiHeader.biBitCount+7)/8;

		if((pBuf=malloc(bmpInfo.bmiHeader.biSizeImage))==NULL)
		{
			MessageBox(NULL,_T("Unable to Allocate Bitmap Memory"),_T("Error"),MB_OK|MB_ICONERROR);
			break;
		}
		
		bmpInfo.bmiHeader.biCompression=BI_RGB;
		GetDIBits(hdc,hBitmap,0,bmpInfo.bmiHeader.biHeight,pBuf,&bmpInfo,DIB_RGB_COLORS);	

		if((fp=fopen(szFilename,"wb"))==NULL)
		{
			MessageBox(NULL,_T("Unable to Create Bitmap File"),_T("Error"),MB_OK|MB_ICONERROR);
			break;
		}

		bmpFileHeader.bfReserved1=0;
		bmpFileHeader.bfReserved2=0;
		bmpFileHeader.bfSize=sizeof(BITMAPFILEHEADER)+sizeof(BITMAPINFOHEADER)+bmpInfo.bmiHeader.biSizeImage;
		bmpFileHeader.bfType='MB';
		bmpFileHeader.bfOffBits=sizeof(BITMAPFILEHEADER)+sizeof(BITMAPINFOHEADER);

		fwrite(&bmpFileHeader,sizeof(BITMAPFILEHEADER),1,fp);
		fwrite(&bmpInfo.bmiHeader,sizeof(BITMAPINFOHEADER),1,fp);
		fwrite(pBuf,bmpInfo.bmiHeader.biSizeImage,1,fp);
		break;
	}

	if(hdc)
		ReleaseDC(NULL,hdc);

	if(pBuf)
		free(pBuf);

	if(fp)
		fclose(fp);
}