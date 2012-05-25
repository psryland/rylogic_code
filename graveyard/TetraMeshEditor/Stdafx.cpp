#include "Stdafx.h"

#ifndef NDEBUG	//i.e. debug
	#pragma comment(lib, "d3d9.lib")
	#pragma comment(lib, "d3dx9.lib")
	#pragma comment(lib, "LineDrawerD.lib")
#else//NDEBUG
	#pragma comment(lib, "d3d9.lib")
	#pragma comment(lib, "d3dx9.lib")
	#pragma comment(lib, "LineDrawer.lib")
#endif//NDEBUG
