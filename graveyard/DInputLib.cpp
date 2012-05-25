//******************************************************************************
//
// Direct Input Library
//
//******************************************************************************
#include "stdafx.h"

#define DIRECTINPUT_VERSION 0x0700

#include "DInputLib.h"
#include <stdio.h>
#include <stdlib.h>
#include <assert.h>
#include <time.h>

#ifdef _DEBUG
#define ASSERT(x) assert(x)
#else
#define ASSERT(x) ((void)(0))
#endif
#undef  FAILED
#define FAILED(status)		( (g_Last_DIError=(HRESULT)(status)) <  0)
#undef	SUCCEEDED
#define SUCCEEDED(status)	( (g_Last_DIError=(HRESULT)(status)) >= 0)

//***************
// Direct Input Globals
LPDIRECTINPUT       g_DInput_Interface = NULL;	// The Direct Input Interface
LPDIRECTINPUTDEVICE g_DInput_Keyboard = NULL;
LPDIRECTINPUTDEVICE g_DInput_Mouse = NULL;
LPDIRECTINPUTDEVICE g_DInput_Joystick = NULL;
DIKEYSTATE			g_KeyState;
DIMOUSESTATE2		g_MouseState;
DIJOYSTATE2			g_JoystickState;
long				g_LastX;
long				g_LastY;
long				g_LastZ;
HRESULT				g_Last_DIError;
// Direct Input Globals
//***************


//***************
// Direct Input Local Functions
// Direct Input Local Functions
//***************


//******************************************************************************
// Direct Input Functions

// *****
// DInputStart - Initialise the direct Input library
//    i.e. create the direct Input interface
bool DInputStart()
{
	// Obtain the Direct Input interface
	if( FAILED(DirectInputCreate(g_Main_Window_Instance, DIRECTINPUT_VERSION, &g_DInput_Interface, NULL)) )
	{	DIError("Failed to create the Direct Input interface", "DInputStart"); return false; }
	
	// Create the keyboard

	// Create the keyboard device
	if( FAILED(g_DInput_Interface->CreateDevice(GUID_SysKeyboard, &g_DInput_Keyboard, NULL)) )
	{	DIError("Failed to create the keyboard device", "DInputStart"); return false; }

	// We have to co-operate with windows...
	if( FAILED(g_DInput_Keyboard->SetCooperativeLevel(g_Main_Window_Handle, DISCL_BACKGROUND | DISCL_NONEXCLUSIVE)) )
	{	DIError("Windows is being unco-operative (keyboard)", "DInputStart"); return false; }

	// Set up the data format for the keyboard
	if( FAILED(g_DInput_Keyboard->SetDataFormat(&c_dfDIKeyboard)) )
	{	DIError("Failed to set the keyboard data format", "DInputStart"); return false; }

	// Create the Mouse

	// Create the mouse device
	if( FAILED(g_DInput_Interface->CreateDevice(GUID_SysMouse, &g_DInput_Mouse, NULL)) )
	{	DIError("Failed to create the mouse device", "DInputStart"); return false; }

	// We have to co-operate with windows...
	if( FAILED(g_DInput_Mouse->SetCooperativeLevel(g_Main_Window_Handle, DISCL_BACKGROUND | DISCL_NONEXCLUSIVE)) )
	{	DIError("Windows is being unco-operative (mouse)", "DInputStart"); return false; }

	// Set up the data format for the mouse
	if( FAILED(g_DInput_Mouse->SetDataFormat(&c_dfDIMouse)) )
	{	DIError("Failed to set the mouse data format", "DInputStart"); return false; }

	// Create the Joystick
	#pragma message(PR_LINK "TODO: support joysticks with DInput")

	if( !DInputReAcquire() ) return false;
	return true;
}

//*****
// DDrawStop - Stop the direct draw library
void DInputStop()
{
	if( g_DInput_Keyboard )  { g_DInput_Keyboard->Unacquire();	g_DInput_Keyboard->Release();	}
	if( g_DInput_Mouse )	 { g_DInput_Mouse->Unacquire();		g_DInput_Mouse->Release();		}
	if( g_DInput_Joystick )  { g_DInput_Joystick->Unacquire();	g_DInput_Joystick->Release();	}
	if( g_DInput_Interface ) g_DInput_Interface->Release();
}

//*****
// When access to the input devices is lost use this function
// to get it back
bool DInputReAcquire()
{
	if( g_DInput_Keyboard )
		if( FAILED(g_DInput_Keyboard->Acquire()) )
		{	DIError("Failed to re-acquire the keyboard", "DInputReAcquire"); return false; }
	
	if( g_DInput_Mouse )
		if( FAILED(g_DInput_Mouse->Acquire()) )
		{	DIError("Failed to re-acquire the mouse", "DInputReAcquire"); return false; }
	
	if( g_DInput_Joystick )
		if( FAILED(g_DInput_Joystick->Acquire()) )
		{	DIError("Failed to re-acquire the joystick", "DInputReAcquire"); return false; }

	return true;
}

//*****
// Release access to the input devices
bool DInputUnAcquire()
{
	if( g_DInput_Keyboard )
		if( FAILED(g_DInput_Keyboard->Unacquire()) )
		{	DIError("Failed to un-acquire the keyboard", "DInputReAcquire"); return false; }
	
	if( g_DInput_Mouse )
		if( FAILED(g_DInput_Mouse->Unacquire()) )
		{	DIError("Failed to un-acquire the mouse", "DInputReAcquire"); return false; }
	
	if( g_DInput_Joystick )
		if( FAILED(g_DInput_Joystick->Unacquire()) )
		{	DIError("Failed to un-acquire the joystick", "DInputReAcquire"); return false; }

	return true;
}

//*****
// Update g_KeyState with the latest state of the keyboard
bool DInputUpdateKeyboard()
{
	while( g_DInput_Keyboard->GetDeviceState(sizeof(DIKEYSTATE),(void*)&g_KeyState) == DIERR_INPUTLOST )
		if( !DInputReAcquire() )
			return false;
	return true;
}

//*****
// Update g_MouseState with the latest state of the mouse
bool DInputUpdateMouse()
{
	while( g_DInput_Mouse->GetDeviceState(sizeof(DIMOUSESTATE),(void*)&g_MouseState) == DIERR_INPUTLOST )
		if( !DInputReAcquire() )
			return false;
	return true;
}

//*****
// Update g_JoystickState with the latest state of the joystick
bool DInputUpdateJoystick()
{
	while( g_DInput_Joystick->GetDeviceState(sizeof(DIJOYSTATE),(void*)&g_JoystickState) == DIERR_INPUTLOST )
		if( !DInputReAcquire() )
			return false;
	return true;
}

//*****
// Update all of the input devices
bool DInputUpdateAll()
{
	if( !DInputUpdateKeyboard() ) return false;
	if( !DInputUpdateMouse() ) return false;
	if( !DInputUpdateJoystick() ) return false;
	return true;
}

//*****
// Display an error message
void DIError( char *err_str, char *title )
{
	char err[550];
	sprintf(err, "%s\nDI Error: %d", err_str, g_Last_DIError);
	MessageBox( g_Main_Window_Handle, err, title, MB_OK | MB_ICONEXCLAMATION );
	ASSERT(false);
}

//*****
// Log a notice or warning
void DIWarning( char *warn_str, char *title )
{
#ifdef _DEBUG
	time_t now = time(NULL);
	char time_str[30];
	strcpy(time_str, ctime(&now) );
	time_str[strlen(time_str)-1] = '\0';

	FILE *fp = fopen("DILibErrorLog.txt", "a+");
	if( fp )
	{
		fprintf( fp, "[%s] %s: %s DIError: %d\n", time_str, title, warn_str, g_Last_DIError );
		fclose(fp);
	}
#else
	warn_str;
	title;
#endif
}

