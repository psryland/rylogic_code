
#define GUIAPP 0 // remember to change linker->system->subsystem
#define D3D_DEBUG_INFO

#if GUIAPP
#   define MainFunc() APIENTRY tWinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance, LPTSTR lpCmdLine, int nCmdShow)
#else
#   define _CONSOLE
#   define MainFunc() main(int argc, char* argv[])
#endif

#define PR_MATHS_USE_D3DX 1
#define PR_MATHS_USE_INTRINSICS 1

#include "pr/common/min_max_fix.h"
//#include "pr/common/atl.h"
//#include <windows.h>
//#include <stdio.h>
#include <conio.h>
//#include <stdlib.h>
//#include <string.h>
//#include <string>
//#include <sstream>
//#include <fstream>
//#include <d3d9.h>
//#include <d3dx9.h>
//#include <vector>
//#include "pr/common/assert.h"
//#include "pr/common/array.h"
//#include "pr/common/fmt.h"
//#include "pr/common/timers.h"
//#include "pr/common/imposter.h"
//#include "pr/common/proxy.h"
//#include "pr/maths/maths.h"
//#include "pr/maths/stat.h"
//#include "pr/filesys/fileex.h"
//#include "pr/gui/misc.h"
//#include "pr/gui/progress_dlg.h"
//#include "pr/geometry/geometry.h"
//#include "pr/gui/grid_ctrl.h"
//#include "pr/gui/gdiplus.h"
//#include "pr/gui/context_menu.h"
//#include "pr/gui/graph_ctrl.h"
#include "pr/hardware/comm_port_io.h"
//#include "pr/threads/background_task.h"
//#include "pr/linedrawer/ldr_helper.h"
//#include "pr/input/dinput.h"

#pragma warning (disable:4100) // unused params
int MainFunc()
{
	pr::CommPortIO io;
	
	std::string data = "Hello Comm Port";
	io.Open(3);
	io.Write(&data[0], data.size(), INFINITE);
	for (;!_kbhit();)
	{
		size_t size = io.BytesAvailable();
		printf("bytes available: %d\n", size);
	}

	//using namespace pr::gui;
	//pr::GdiPlus gdiplus;
	
	//AllocConsole();
	//HWND hwnd = GetConsoleWindow();
	
	//pr::gui::CGraphCtrl<> graph;
	
	//StylePtr st0(new ContextMenuStyle());
	//st0->m_col_bkgd.SetValue(Gdiplus::ARGB(Gdiplus::Color::Black));
	//st0->m_col_text.SetValue(Gdiplus::ARGB(Gdiplus::Color::Red));

	//ContextMenu menu(L"Menu", 10, StylePtr(new ContextMenuStyle()));
	//menu.AddItem(new ContextMenu::Label(L"Pauls 0", 11, StylePtr(), 1));
	//menu.AddItem(new ContextMenu::Label(L"Pauls 1", 12, st0, 2));
	//ContextMenu& sub = menu.AddItem(new ContextMenu(L"Sub Menu", 100));
	//sub.AddItem(new ContextMenu::Label(L"Rules 0", 101));
	//sub.AddItem(new ContextMenu::Label(L"Rules 1", 102));

	//int res = menu.Show(hwnd, 100, 100);
	//
	//switch (res)
	//{
	//case 11: printf("Pauls 0\n"); break;
	//case 12: printf("Pauls 1\n"); break;
	//}
	return 0;
}

