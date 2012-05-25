//******************************************************************************
//
// Direct Input Library
//
//******************************************************************************
#ifndef DINPUTLIB_H
#define DINPUTLIB_H

#ifndef DIRECTINPUT_VERSION
#define DIRECTINPUT_VERSION 0x0700
#endif

#include <dinput.h>

//***************
// Direct Input Constants
// Direct Input Constants
//***************


//***************
// Direct Input Structures
typedef unsigned char DIKEYSTATE[256];
// Direct Input Structures
//***************


//***************
// Direct Input Globals
extern HWND					g_Main_Window_Handle;
extern HINSTANCE			g_Main_Window_Instance;
extern LPDIRECTINPUT		g_DInput_Interface;
extern LPDIRECTINPUTDEVICE	g_DInput_Keyboard;
extern LPDIRECTINPUTDEVICE	g_DInput_Mouse;
extern LPDIRECTINPUTDEVICE	g_DInput_Joystick;
extern DIKEYSTATE			g_KeyState;
extern DIMOUSESTATE2		g_MouseState;
extern DIJOYSTATE2			g_JoystickState;
extern long					g_LastX;
extern long					g_LastY;
extern long					g_LastZ;
// Direct Input Globals
//***************


//***************
// Direct Input Functions
bool DInputStart();
void DInputStop();
bool DInputReAcquire();
bool DInputUnAcquire();

bool DInputUpdateKeyboard();
bool DInputUpdateMouse();
bool DInputUpdateJoystick();
bool DInputUpdateAll();

void DIError( char *err_str, char *title );
void DIWarning( char *warn_str, char *title );
// Direct Input Functions
//***************


//***************
// Direct Input Macros
// Direct Input Macros
//***************


//***************
// Direct Input inlined Functions
inline bool KeyDown( int key )	{ return (g_KeyState[key] & 0x80) == 0x80; }

inline void MouseXY(long &x, long &y) { x = g_MouseState.lX; y = g_MouseState.lY; }
inline long MouseX()			{ return g_MouseState.lX; }
inline long MouseDX()			{ DWORD diff = g_MouseState.lX - g_LastX; g_LastX = g_MouseState.lX; return diff; }

inline long MouseY()			{ return g_MouseState.lY; }
inline long MouseDY()			{ DWORD diff = g_MouseState.lY - g_LastY; g_LastY = g_MouseState.lY; return diff; }
	
inline long MouseZ()			{ return g_MouseState.lZ; }
inline long MouseDZ()			{ DWORD diff = g_MouseState.lZ - g_LastZ; g_LastZ = g_MouseState.lZ; return diff; }

inline bool MouseLeft()			{ return (g_MouseState.rgbButtons[0] & 0x80) == 0x80; }
inline bool MouseRight()		{ return (g_MouseState.rgbButtons[1] & 0x80) == 0x80; }
inline bool MouseMiddle()		{ return (g_MouseState.rgbButtons[2] & 0x80) == 0x80; }
inline bool MouseLeftLeft()		{ return (g_MouseState.rgbButtons[3] & 0x80) == 0x80; }
inline bool MouseRightRight()	{ return (g_MouseState.rgbButtons[4] & 0x80) == 0x80; }
inline bool Mouse5()			{ return (g_MouseState.rgbButtons[5] & 0x80) == 0x80; }
inline bool Mouse6()			{ return (g_MouseState.rgbButtons[6] & 0x80) == 0x80; }
inline bool Mouse7()			{ return (g_MouseState.rgbButtons[7] & 0x80) == 0x80; }
// Direct Input inlined Functions
//***************

#endif  DINPUTLIB_H