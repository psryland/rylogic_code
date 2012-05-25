// stdafx.cpp : source file that includes just the standard includes
// PhysicsTestbed.pch will be the pre-compiled header
// stdafx.obj will contain the pre-compiled type information

#include "PhysicsTestbed/Stdafx.h"

#if PHYSICS_ENGINE==RYLOGIC_PHYSICS
#	pragma message("Using the Rylogic physics engine")
#	ifdef _DEBUG
#		pragma comment(lib, "PhysicsD.lib")
#	else
#		pragma comment(lib, "Physics.lib")
#	endif
#elif PHYSICS_ENGINE==REFLECTIONS_PHYSICS
#	pragma message("Using the Reflections physics engine")
#	ifdef _DEBUG
#		pragma comment(lib, "Maths_MTDebugWin32.lib")
#		pragma comment(lib, "TaskScheduler_MTDebugWin32.lib")
#		pragma comment(lib, "PhysicsAllDebugWin32.lib")
#	else
#		pragma comment(lib, "Maths_MTDevkitWin32.lib")
#		pragma comment(lib, "TaskScheduler_MTDevkitWin32.lib")
#		pragma comment(lib, "PhysicsAllDevkitWin32.lib")
#	endif
#endif

// Use project settings so that dependencies work
//#ifndef NDEBUG	//i.e. debug
//	#pragma comment(lib, "d3d9.lib")
//	#pragma comment(lib, "d3dx9.lib")
//	#pragma comment(lib, "LineDrawerD.lib")
//	#pragma comment(lib, "LineDrawerHelperD.lib")
//	#ifdef USE_REFLECTIONS_ENGINE
//		#pragma comment(lib, "Maths_MTDebugWin32.lib")
//		#pragma comment(lib, "TaskScheduler_MTDebugWin32.lib")
//		#pragma comment(lib, "PhysicsAllDebugWin32.lib")
//	#else//USE_REFLECTIONS_ENGINE
//		#pragma comment(lib, "PhysicsD.lib")
//	#endif//USE_REFLECTIONS_ENGINE
//#else//NDEBUG
//	#pragma comment(lib, "d3d9.lib")
//	#pragma comment(lib, "d3dx9.lib")
//	#pragma comment(lib, "LineDrawer.lib")
//	#pragma comment(lib, "LineDrawerHelper.lib")
//	#ifdef USE_REFLECTIONS_ENGINE
//		#pragma comment(lib, "Maths_MTDevkitWin32.lib")
//		#pragma comment(lib, "TaskScheduler_MTDevkitWin32.lib")
//		#pragma comment(lib, "PhysicsAllReleaseWin32.lib")
//	#else//USE_REFLECTIONS_ENGINE
//		#pragma comment(lib, "Physics.lib")
//	#endif//USE_REFLECTIONS_ENGINE
//#endif//NDEBUG
