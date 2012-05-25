//***************************************************************************************
//
// A starting point for creating Direct 3D applications
//
//***************************************************************************************
#include "stdafx.h"
#include "D3DConsole2.h"
#include "PR/Common/Console.h"

using namespace ConsoleOutputNameSpace;

// External application globals. These parameters are extern'ed so that
// they can be used by the user program plus other object files
HWND					g_Main_Window_Handle;
HINSTANCE				g_Main_Window_Instance;
char*					g_Command_Line			= NULL;
DWORD					g_Game_Clock			= GetTickCount();
DWORD					g_Last_Frame			= 0;
DWORD					g_Elapsed_Milliseconds	= 0;
float					g_Max_Time_Step			= 1.0f;	// Seconds
bool					g_Application_Active	= true;
HRESULT					g_Last_Error			= 0;	// The last error value
D3DXMATRIX				g_Identity;

// These parameters are global only to this file
D3DAdapterInfo			g_Adapter_Info[MAX_ADAPTERS_PER_SYSTEM];	// Information about adapters, devices, and display modes
LPDIRECT3D8				g_D3D_Interface			= NULL;				// The Direct 3D interface
LPDIRECT3DDEVICE8		g_D3D_Device			= NULL;				// The 3D Device
D3DCAPS8				g_D3D_Device_Caps;
D3DPRESENT_PARAMETERS	g_Present_Parameters;						// The presentation parameters
RECT					g_Window_Bounds;							// The size of the window in windowed mode
RECT					g_Client_Area;								// The drawable area in windowed mode
ConsoleOutput			g_ConsoleOutput;

//	D3DDISPLAYMODE			g_D3D_Display_Mode;
//	D3DVIEWPORT8			g_D3D_Viewport;
//	D3DXMATRIX				g_D3D_Perspective_Transform;
//	LPDIRECT3DSURFACE8		g_Back_Buffer = NULL;
//	LPDIRECT3DSURFACE8		g_Back_Buffer_ZStencil = NULL;
//	D3DFORMAT				g_Back_Buffer_Format;
//	float					g_Feild_Of_View = (30.0f * D3DX_PI) / 180.0f;

// External application global variables. These parameters are extern'ed so
// that the user program can control the initial state of the program. They
// are all defaulted to sensible values in case the user program does not
// initialise them.
bool		g_Full_Screen		= false;
DWORD		g_Screen_Width		= 640;
DWORD		g_Screen_Height		= 480;
float		g_Screen_Depth		= 100.0f;
float		g_Screen_Shallowth	= 1.0f;
D3DFORMAT	g_Screen_Format		= D3DFMT_X8R8G8B8;
D3DFORMAT	g_Depth_Format		= D3DFMT_D16;
int			g_Screen_Refresh	= 0;	// Default
int			g_Screen_X			= (GetSystemMetrics(SM_CXFULLSCREEN) - g_Screen_Width)  / 2;// These only apply in windowed mode. They
int			g_Screen_Y			= (GetSystemMetrics(SM_CYFULLSCREEN) - g_Screen_Height) / 2;// describle the initial position of the window.
int			g_Frame_Rate		= 30;
char*		g_Window_Title		= "Direct3D Program";
HICON		g_Icon				= NULL;
HICON		g_IconSm			= NULL;
HCURSOR		g_Cursor			= NULL;
HMENU		g_Menu				= NULL;
DWORD		g_Window_Style		= WS_POPUP|WS_CAPTION|WS_SYSMENU|WS_THICKFRAME|WS_MINIMIZEBOX|WS_VISIBLE;

DWORD		g_Adapter			= D3DADAPTER_DEFAULT;		// The adapter we are using

// These parameters are global only to this file
char*		g_Window_Class_Name		= "D3D Window Class Name";


// Local function prototypes
bool CreateApplicationWindow();
LRESULT D3DConsole2WindowProc(HWND hwnd, UINT msg, WPARAM wparam, LPARAM lparam);
bool InitialiseDirect3D();
void SetDefaultRenderState();
void UnInitialiseDirect3D();
bool BuildAdapterList();
bool SelectDisplayMode();
bool Initialise3DEnvironment();
void RenderFrame();
bool TestCooperativeLevel();
bool ResetDevice();
void SyncFrameRate();

//*****
// WinMain: Entry point to the program. It all starts here
int WINAPI WinMain(HINSTANCE hinstance, HINSTANCE, LPSTR command_line, int show)
{
	g_Main_Window_Instance	= hinstance;
	g_Command_Line			= command_line;
	g_Icon					= LoadIcon(NULL, IDI_APPLICATION);
	g_IconSm				= LoadIcon(NULL, IDI_APPLICATION);
	g_Cursor				= LoadCursor(NULL, IDC_ARROW);
	D3DXMatrixIdentity(&g_Identity);

	// Allow the user program to set the application variables
	if( !PreWindowCreationInitialisation() ) return 0;

	// Create a window for this application
	if( !CreateApplicationWindow() ) return 0;

	// Display the window
	ShowWindow(g_Main_Window_Handle, show);

	// Create the D3D interface and device
	if( InitialiseDirect3D() )
	{
		// Initialise the user application's device-independent objects
		if( InitialiseApplication() )
		{
			// Initialise the user application's device-dependent objects
			if( CreateDeviceDependentObjects() )
			{
				g_Last_Frame = GetTickCount();

				// The main message pump
				MSG msg;
				memset(&msg, 0, sizeof(MSG));
				bool done = false;
				while( !done )
				{
					if( PeekMessage(&msg, NULL, 0, 0, PM_REMOVE) )
					{
						// Time to quit?
						if( msg.message == WM_QUIT )
							break;	// Program will end
				
						// Translate any accelerator keys
						TranslateMessage(&msg);

						// Send the message to be processed
						DispatchMessage(&msg);
					}
    
					// Main game processing
					if( g_Application_Active )
					{
						g_Game_Clock = GetTickCount();
						g_Elapsed_Milliseconds = g_Game_Clock - g_Last_Frame;
						if( g_Elapsed_Milliseconds > 0 )
							g_Last_Frame = g_Game_Clock;

						Main();
						SyncFrameRate();
						RenderFrame();
					}
					else
						Sleep( APPLICATION_INACTIVE_SLEEP_TIME );
				}
			}
			ReleaseDeviceDependentObjects();
		}

		UnInitialiseApplication();
	}
	UnInitialiseDirect3D();
	#ifdef _DEBUG
	_CrtDumpMemoryLeaks();
	#endif//_DEBUG
	return 0;
}

//*****
// 
void RenderFrame()
{
	// Test whether we are allowed to draw now. 
	if( !TestCooperativeLevel() ) return;

	Render();
}

//*****
// Test for device lost and reacquire the device if so.
bool TestCooperativeLevel()
{
	HRESULT hr = g_D3D_Device->TestCooperativeLevel();

    // Test the cooperative level to see if it's okay to render
    if( FAILED(hr) )
    {
        // If the device was lost, do not render until we get it back
        if( hr == D3DERR_DEVICELOST )
            return false;

        // Check if the device needs to be restored
        else if( hr == D3DERR_DEVICENOTRESET )
        {
            // If we are windowed, read the desktop mode and use the same format for the back buffer
            if( !g_Full_Screen )
            {
                D3DAdapterInfo& adapter = g_Adapter_Info[g_Adapter];
                g_D3D_Interface->GetAdapterDisplayMode(g_Adapter, &adapter.m_desktop_display_mode);
                g_Present_Parameters.BackBufferFormat = adapter.m_desktop_display_mode.Format;
            }

            if( ResetDevice() )
				return true;
		}

		// Some other error occurred
		else
			D3DError("TestCooperativeLevel", "TestCooperativeLevel failed");
				
		End();
        return false;
    }
	return true;
}

//*****
// Recover from a lost device
bool ResetDevice()
{
	// Release all video memory objects
	ReleaseDeviceDependentObjects();

    // Reset the device
	// NOTE: Reset will fail unless the application releases all resources that are allocated in
	// D3DPOOL_DEFAULT, including those created by the IDirect3DDevice8::CreateRenderTarget and
	// IDirect3DDevice8::CreateDepthStencilSurface methods
    if( FAILED(g_D3D_Device->Reset(&g_Present_Parameters)) )
	{	D3DError("ResetDevice", "Failed to reset the 3D device"); return false; }

    // Re - create the video memory objects
    if( !CreateDeviceDependentObjects() ) return false;
	
	return true;
}

//*****
// Lock the upper maximum frame rate
void SyncFrameRate()
{
	static DWORD g_Next_Frame = 0;

	g_Game_Clock = GetTickCount();
	while( g_Game_Clock < g_Next_Frame )
	{
		Sleep(g_Next_Frame - g_Game_Clock);
		g_Game_Clock = GetTickCount();
	}

	g_Next_Frame = g_Game_Clock + 1000 / g_Frame_Rate;
}

//*****
// Initialise the D3D interface, device, etc.
// If something goes wrong in this function and we return false we assume the
// UnInitialiseDirect3D function will be called that releases anything that is
// created here. 
bool InitialiseDirect3D()
{
	// Obtain an interface to Direct 3D
	g_D3D_Interface = Direct3DCreate8(D3D_SDK_VERSION);
	if( !g_D3D_Interface ) { D3DError("InitialiseDirect3D", "Direct3DCreate8() failed"); return false; }

	// Build a list of Direct3D adapters, modes and devices on this system. The
    // IsDeviceAcceptable() callback is used to confirm that only devices that
    // meet the app's requirements are considered.
    if( !BuildAdapterList() ) { D3DError("InitialiseDirect3D", "Failed to build the adapter list"); return false; }

	// Select the display mode to use
	if( !SelectDisplayMode() ) { D3DError("InitialiseDirect3D", "Failed to select an appropriate display mode"); return false; }

	// Initialise the Direct 3D Environment
	if( !Initialise3DEnvironment() ) { D3DError("InitialiseDirect3D", "Failed to create the 3D environment"); return false; }

	// Set Direct3D to know state
	SetDefaultRenderState();

	return true;
}

//*****
// Set all of the render states to a known value
void SetDefaultRenderState()
{
	HRESULT r;
	r = g_D3D_Device->SetRenderState(D3DRS_ZENABLE, D3DZB_TRUE);			assert(r == D3D_OK);
	r = g_D3D_Device->SetRenderState(D3DRS_FILLMODE, D3DFILL_SOLID);		assert(r == D3D_OK);
	r = g_D3D_Device->SetRenderState(D3DRS_SHADEMODE, D3DSHADE_GOURAUD);	assert(r == D3D_OK);
	r = g_D3D_Device->SetRenderState(D3DRS_ZWRITEENABLE, TRUE);				assert(r == D3D_OK);
	r = g_D3D_Device->SetRenderState(D3DRS_ALPHATESTENABLE, FALSE);			assert(r == D3D_OK);
	r = g_D3D_Device->SetRenderState(D3DRS_LASTPIXEL, TRUE);				assert(r == D3D_OK);
	r = g_D3D_Device->SetRenderState(D3DRS_SRCBLEND, D3DBLEND_ONE);			assert(r == D3D_OK);
	r = g_D3D_Device->SetRenderState(D3DRS_DESTBLEND, D3DBLEND_ZERO);		assert(r == D3D_OK);
	r = g_D3D_Device->SetRenderState(D3DRS_CULLMODE, D3DCULL_CCW);			assert(r == D3D_OK);
	r = g_D3D_Device->SetRenderState(D3DRS_ZFUNC, D3DCMP_LESSEQUAL);		assert(r == D3D_OK);
	r = g_D3D_Device->SetRenderState(D3DRS_ALPHAREF, 0);					assert(r == D3D_OK);
	r = g_D3D_Device->SetRenderState(D3DRS_ALPHAFUNC, D3DCMP_ALWAYS);		assert(r == D3D_OK);
	r = g_D3D_Device->SetRenderState(D3DRS_DITHERENABLE, FALSE);			assert(r == D3D_OK);
	r = g_D3D_Device->SetRenderState(D3DRS_ALPHABLENDENABLE, FALSE);		assert(r == D3D_OK);
	r = g_D3D_Device->SetRenderState(D3DRS_FOGENABLE, FALSE);				assert(r == D3D_OK);
	r = g_D3D_Device->SetRenderState(D3DRS_SPECULARENABLE, FALSE);			assert(r == D3D_OK);
	r = g_D3D_Device->SetRenderState(D3DRS_FOGCOLOR, 0);					assert(r == D3D_OK);
	r = g_D3D_Device->SetRenderState(D3DRS_FOGTABLEMODE, D3DFOG_NONE);		assert(r == D3D_OK);
	r = g_D3D_Device->SetRenderState(D3DRS_FOGSTART, DWORD(0.0f));			assert(r == D3D_OK);
	r = g_D3D_Device->SetRenderState(D3DRS_FOGEND, DWORD(1.0f));			assert(r == D3D_OK);
	r = g_D3D_Device->SetRenderState(D3DRS_FOGDENSITY, DWORD(1.0f));		assert(r == D3D_OK);
	r = g_D3D_Device->SetRenderState(D3DRS_EDGEANTIALIAS, TRUE);			assert(r == D3D_OK);
	r = g_D3D_Device->SetRenderState(D3DRS_ZBIAS, 0);						assert(r == D3D_OK);
	r = g_D3D_Device->SetRenderState(D3DRS_RANGEFOGENABLE, FALSE);			assert(r == D3D_OK);
	r = g_D3D_Device->SetRenderState(D3DRS_STENCILENABLE, FALSE);			assert(r == D3D_OK);
	r = g_D3D_Device->SetRenderState(D3DRS_STENCILFAIL, D3DSTENCILOP_KEEP);	assert(r == D3D_OK);
	r = g_D3D_Device->SetRenderState(D3DRS_STENCILZFAIL, D3DSTENCILOP_KEEP);assert(r == D3D_OK);
	r = g_D3D_Device->SetRenderState(D3DRS_STENCILPASS, D3DSTENCILOP_KEEP);	assert(r == D3D_OK);
	r = g_D3D_Device->SetRenderState(D3DRS_STENCILFUNC, D3DCMP_ALWAYS);		assert(r == D3D_OK);
	r = g_D3D_Device->SetRenderState(D3DRS_STENCILREF, 0);					assert(r == D3D_OK);
	r = g_D3D_Device->SetRenderState(D3DRS_STENCILMASK, 0xFFFFFFFF);		assert(r == D3D_OK);
	r = g_D3D_Device->SetRenderState(D3DRS_STENCILWRITEMASK, 0xFFFFFFFF);	assert(r == D3D_OK);
	r = g_D3D_Device->SetRenderState(D3DRS_TEXTUREFACTOR, 0xFFFFFFFF);		assert(r == D3D_OK);
	r = g_D3D_Device->SetRenderState(D3DRS_WRAP0, 0);						assert(r == D3D_OK);
	r = g_D3D_Device->SetRenderState(D3DRS_WRAP1, 0);						assert(r == D3D_OK);
	r = g_D3D_Device->SetRenderState(D3DRS_WRAP2, 0);						assert(r == D3D_OK);
	r = g_D3D_Device->SetRenderState(D3DRS_WRAP3, 0);						assert(r == D3D_OK);
	r = g_D3D_Device->SetRenderState(D3DRS_WRAP4, 0);						assert(r == D3D_OK);
	r = g_D3D_Device->SetRenderState(D3DRS_WRAP5, 0);						assert(r == D3D_OK);
	r = g_D3D_Device->SetRenderState(D3DRS_WRAP6, 0);						assert(r == D3D_OK);
	r = g_D3D_Device->SetRenderState(D3DRS_WRAP7, 0);						assert(r == D3D_OK);
	r = g_D3D_Device->SetRenderState(D3DRS_CLIPPING, TRUE);					assert(r == D3D_OK);
	r = g_D3D_Device->SetRenderState(D3DRS_LIGHTING, TRUE);					assert(r == D3D_OK);
	r = g_D3D_Device->SetRenderState(D3DRS_AMBIENT, 0);						assert(r == D3D_OK);
	r = g_D3D_Device->SetRenderState(D3DRS_FOGVERTEXMODE, D3DFOG_NONE);		assert(r == D3D_OK);
	r = g_D3D_Device->SetRenderState(D3DRS_COLORVERTEX, TRUE);				assert(r == D3D_OK);
	r = g_D3D_Device->SetRenderState(D3DRS_LOCALVIEWER, TRUE);				assert(r == D3D_OK);
	r = g_D3D_Device->SetRenderState(D3DRS_NORMALIZENORMALS, FALSE);		assert(r == D3D_OK);
	r = g_D3D_Device->SetRenderState(D3DRS_DIFFUSEMATERIALSOURCE, D3DMCS_COLOR1);	assert(r == D3D_OK);
	r = g_D3D_Device->SetRenderState(D3DRS_SPECULARMATERIALSOURCE, D3DMCS_COLOR2);	assert(r == D3D_OK);
	r = g_D3D_Device->SetRenderState(D3DRS_AMBIENTMATERIALSOURCE, D3DMCS_COLOR2);	assert(r == D3D_OK);
	r = g_D3D_Device->SetRenderState(D3DRS_EMISSIVEMATERIALSOURCE, D3DMCS_MATERIAL);assert(r == D3D_OK);
	r = g_D3D_Device->SetRenderState(D3DRS_VERTEXBLEND, D3DVBF_DISABLE);	assert(r == D3D_OK);
	r = g_D3D_Device->SetRenderState(D3DRS_CLIPPLANEENABLE, 0);				assert(r == D3D_OK);
	r = g_D3D_Device->SetRenderState(D3DRS_SOFTWAREVERTEXPROCESSING, FALSE);assert(r == D3D_OK);
	r = g_D3D_Device->SetRenderState(D3DRS_POINTSIZE, DWORD(0.0f));			assert(r == D3D_OK);
	r = g_D3D_Device->SetRenderState(D3DRS_POINTSIZE_MIN, DWORD(0.0f));		assert(r == D3D_OK);
	r = g_D3D_Device->SetRenderState(D3DRS_POINTSPRITEENABLE, FALSE);		assert(r == D3D_OK);
	r = g_D3D_Device->SetRenderState(D3DRS_POINTSCALEENABLE, FALSE);		assert(r == D3D_OK);
	r = g_D3D_Device->SetRenderState(D3DRS_POINTSCALE_A, DWORD(1.0f));		assert(r == D3D_OK);
	r = g_D3D_Device->SetRenderState(D3DRS_POINTSCALE_B, DWORD(0.0f));		assert(r == D3D_OK);
	r = g_D3D_Device->SetRenderState(D3DRS_POINTSCALE_C, DWORD(0.0f));		assert(r == D3D_OK);
	r = g_D3D_Device->SetRenderState(D3DRS_MULTISAMPLEANTIALIAS, TRUE);		assert(r == D3D_OK);
	r = g_D3D_Device->SetRenderState(D3DRS_MULTISAMPLEMASK, 0xFFFFFFFF);	assert(r == D3D_OK);
	r = g_D3D_Device->SetRenderState(D3DRS_PATCHEDGESTYLE, D3DPATCHEDGE_DISCRETE);	assert(r == D3D_OK);
	r = g_D3D_Device->SetRenderState(D3DRS_PATCHSEGMENTS, DWORD(1.0f));		assert(r == D3D_OK);
	r = g_D3D_Device->SetRenderState(D3DRS_POINTSIZE_MAX, DWORD(64.0f));	assert(r == D3D_OK);
	r = g_D3D_Device->SetRenderState(D3DRS_INDEXEDVERTEXBLENDENABLE, FALSE);assert(r == D3D_OK);
	r = g_D3D_Device->SetRenderState(D3DRS_COLORWRITEENABLE, 0x0000000F);	assert(r == D3D_OK);
	r = g_D3D_Device->SetRenderState(D3DRS_TWEENFACTOR, DWORD(0.0f));		assert(r == D3D_OK);
	r = g_D3D_Device->SetRenderState(D3DRS_BLENDOP, D3DBLENDOP_ADD);		assert(r == D3D_OK);
//PSR...	r = g_D3D_Device->SetRenderState(D3DRS_POSITIONORDER, D3DORDER_CUBIC);	assert(r == D3D_OK);
//PSR...	r = g_D3D_Device->SetRenderState(D3DRS_NORMALORDER, D3DORDER_LINEAR);	assert(r == D3D_OK);
}

//*****
// Release everything that was created in the InitialiseDirect3D function in reverse order.
void UnInitialiseDirect3D()
{
	if( g_D3D_Device )		{ g_D3D_Device->Release();		g_D3D_Device = NULL;	}
	if( g_D3D_Interface )	{ g_D3D_Interface->Release();	g_D3D_Interface = NULL;	}
}

//*****
//
bool Initialise3DEnvironment()
{
	D3DAdapterInfo& adapter = g_Adapter_Info[g_Adapter];
	D3DDeviceInfo&  device	= adapter.m_devices[adapter.m_current_device];

    // Set up the presentation parameters
	ZeroMemory(&g_Present_Parameters, sizeof(g_Present_Parameters));
	g_Present_Parameters.Windowed				= !g_Full_Screen;
    g_Present_Parameters.BackBufferCount        = 1;
    g_Present_Parameters.MultiSampleType        = device.m_multi_sample_type;
    g_Present_Parameters.SwapEffect             = D3DSWAPEFFECT_DISCARD;
    g_Present_Parameters.EnableAutoDepthStencil = TRUE;
    g_Present_Parameters.AutoDepthStencilFormat = g_Depth_Format;
    g_Present_Parameters.hDeviceWindow          = g_Main_Window_Handle;
    if( !g_Full_Screen )
    {
        g_Present_Parameters.BackBufferWidth  = RectWidth(g_Client_Area);
        g_Present_Parameters.BackBufferHeight = RectHeight(g_Client_Area);
        g_Present_Parameters.BackBufferFormat = adapter.m_desktop_display_mode.Format;
    }
    else
    {
        g_Present_Parameters.BackBufferWidth  = g_Screen_Width;
        g_Present_Parameters.BackBufferHeight = g_Screen_Height;
        g_Present_Parameters.BackBufferFormat = g_Screen_Format;
    }

    // Create the device
    HRESULT hr = g_D3D_Interface->CreateDevice( g_Adapter, device.m_device_type, g_Main_Window_Handle,
												device.m_behavior, &g_Present_Parameters, &g_D3D_Device );
	if( FAILED(hr) ) { D3DError("Initialise3DEnvironment", "Failed to create the Direct 3D device"); return false; }

    // When moving from fullscreen to windowed mode, it is important to adjust the window size after recreating
	// the device rather than beforehand to ensure that you get the window size you want. For example, when
	// switching from 640x480 fullscreen to windowed with a 1000x600 window on a 1024x768 desktop, it is impossible
	// to set the window size to 1000x600 until after the display mode has changed to 1024x768, because windows
	// cannot be larger than the desktop.
    if( !g_Full_Screen )
    {
        SetWindowPos( g_Main_Window_Handle, HWND_NOTOPMOST, g_Window_Bounds.left, g_Window_Bounds.top,
                      RectWidth(g_Window_Bounds), RectHeight(g_Window_Bounds), SWP_SHOWWINDOW );
    }

	// Store device caps for the render device (I think these are the same as the caps in the D3DDeviceInfo struct
	g_D3D_Device->GetDeviceCaps(&g_D3D_Device_Caps);
	return true;
}

//*****
// Fill out the g_Adapter_Info structure with information about the adapters,
// devices, and display modes available on this system
bool BuildAdapterList()
{
    const DWORD MAX_DEVICE_TYPES	= 2;
    const char* device_desc[]		= { "HAL", "REF" };
    const D3DDEVTYPE device_type[]	= { D3DDEVTYPE_HAL, D3DDEVTYPE_REF };

    // Loop through all the adapters on the system (usually, there's just one
    // unless more than one graphics card is present).
    for( DWORD adapter_count = 0; adapter_count < g_D3D_Interface->GetAdapterCount(); ++adapter_count )
    {
		// Check that our array is big enough to hold this adapter
		if( adapter_count == MAX_ADAPTERS_PER_SYSTEM ) { ASSERT(false);	break; }

		D3DAdapterInfo& adapter = g_Adapter_Info[adapter_count];

		// Fill in adapter info
		g_D3D_Interface->GetAdapterIdentifier(adapter_count, 0, &adapter.m_adapter_identifier);
		g_D3D_Interface->GetAdapterDisplayMode(adapter_count, &adapter.m_desktop_display_mode);
		adapter.m_current_device = 0;
		
		for( adapter.m_num_devices = 0; adapter.m_num_devices < MAX_DEVICE_TYPES; ++adapter.m_num_devices )
		{
			// Check that our array is big enough to hold this device
			if( adapter.m_num_devices == MAX_DEVICES_PER_ADAPTER) { ASSERT(false); break; }

			D3DDeviceInfo& device = adapter.m_devices[adapter.m_num_devices];

			// Fill in device info
			device.m_device_type	= device_type[adapter.m_num_devices];
			device.m_desc			= device_desc[adapter.m_num_devices];
			g_D3D_Interface->GetDeviceCaps(adapter_count, device.m_device_type, &device.m_caps);
			device.m_current_mode	= 0;
			device.m_windowed		= (device.m_caps.Caps2 & D3DCAPS2_CANRENDERWINDOWED) > 0;
			device.m_can_do_windowed= false;
			device.m_multi_sample_type = D3DMULTISAMPLE_NONE;
			
			// Choose a vertex processing behaviour based on whether there is hardware support
			if( device.m_caps.DevCaps & D3DDEVCAPS_HWTRANSFORMANDLIGHT )
			{
				device.m_behavior = D3DCREATE_HARDWARE_VERTEXPROCESSING;
				if( device.m_caps.DevCaps & D3DDEVCAPS_PUREDEVICE )
					device.m_behavior |= D3DCREATE_PUREDEVICE;
			}
			else
				device.m_behavior = D3DCREATE_SOFTWARE_VERTEXPROCESSING;

			// Establish whether the capabilities of this device meet the requirements of the application 
			device.m_acceptable = IsDeviceAcceptable(device.m_caps, device.m_behavior);
			if( !device.m_acceptable )
				continue;

			// Enumerate all display modes on this adapter
			DWORD num_adapter_modes = g_D3D_Interface->GetAdapterModeCount(adapter_count);
			for( device.m_num_modes = 0; device.m_num_modes < num_adapter_modes; ++device.m_num_modes )
			{
				// Check that our array is big enough to hold this mode
				if( device.m_num_modes == MAX_MODES_PER_DEVICE) { ASSERT(false); break; }

				// Fill in the display mode info
				g_D3D_Interface->EnumAdapterModes(adapter_count, device.m_num_modes, &device.m_modes[device.m_num_modes]);
			}
		}
	}

	return true;
}

//*****
// Find a display mode to use that matches the width, height, format, and refresh rate choosen by the user application
bool SelectDisplayMode()
{
	// If we're running in a window then the desktop display mode is the one we're using
	if( !g_Full_Screen ) return true;

	D3DDeviceInfo& device = g_Adapter_Info[g_Adapter].m_devices[g_Adapter_Info[g_Adapter].m_current_device];

	bool mode_selected = false;
	for( DWORD i = 0; i < device.m_num_modes; ++i )
	{
		if( device.m_modes[i].Width  == g_Screen_Width  &&
			device.m_modes[i].Height == g_Screen_Height &&
			device.m_modes[i].Format == g_Screen_Format )
		{
			if( !mode_selected || device.m_modes[i].RefreshRate > device.m_modes[device.m_current_mode].RefreshRate )
			{
				device.m_current_mode = i;
				mode_selected = true;
			}
		}
	}
	return mode_selected;
}

//*****
// Create and register a window class,
bool CreateApplicationWindow()
{
	// Fill in the window class stucture
	WNDCLASSEX winclass;
	memset(&winclass, 0, sizeof(WNDCLASSEX));
	winclass.cbSize			= sizeof(WNDCLASSEX);
	winclass.style			= CS_HREDRAW | CS_VREDRAW;
	winclass.lpfnWndProc	= WindowProc;
	winclass.cbClsExtra		= 0;
	winclass.cbWndExtra		= 0;
	winclass.hInstance		= g_Main_Window_Instance;
	winclass.hIcon			= g_Icon;
	winclass.hIconSm		= g_IconSm;
	winclass.hCursor		= g_Cursor;
	winclass.hbrBackground	= NULL;
	winclass.lpszMenuName	= NULL; 
	winclass.lpszClassName	= g_Window_Class_Name;
	
	// Register the window class
	if( !RegisterClassEx(&winclass) )
		return false;

	// Adjust the width and height of the window to allow for the window's border
	RECT rc;
	SetRect(&rc, 0, 0, g_Screen_Width, g_Screen_Height);
	AdjustWindowRect(&rc, g_Window_Style, g_Menu != NULL);

	// Create a window
	g_Main_Window_Handle = CreateWindowEx(	0,						// Extra styles
											g_Window_Class_Name,	// Class name
											g_Window_Title,			// Window caption
											WS_OVERLAPPEDWINDOW,	// Style, WS_POPUP | WS_VISIBLE
					 						g_Screen_X,				// X
											g_Screen_Y,				// Y
											RectWidth(rc),			// Width
											RectHeight(rc),			// Height
											NULL,					// Handle to parent 
											g_Menu,					// Handle to menu
											g_Main_Window_Instance,	// Instance
											NULL );					// Creation parms
	if( !g_Main_Window_Handle )
		return false;

   // Save the window properties
    g_Window_Style = GetWindowLong(g_Main_Window_Handle, GWL_STYLE);
    GetWindowRect(g_Main_Window_Handle, &g_Window_Bounds);
    GetClientRect(g_Main_Window_Handle, &g_Client_Area);

	return true;
}

//*****
// The main message handler
LRESULT D3DConsole2WindowProc(HWND hwnd, UINT msg, WPARAM wparam, LPARAM lparam)
{
	switch( msg )
	{
	case WM_PAINT:
        // Handle paint messages when the app is not ready
        if( g_D3D_Device )
        {
            if( !g_Full_Screen )
                g_D3D_Device->Present(NULL, NULL, NULL, NULL);
        }
        break;

    case WM_ACTIVATE:
		// If our App is active...
		if( LOWORD(wparam) )
		{
			SetWindowText(g_Main_Window_Handle, "Active");
			g_Application_Active = true;
		}
		else
		{
			SetWindowText(g_Main_Window_Handle, "Inactive");
			g_Application_Active = false;
		}
		return 1;

	case WM_EXITSIZEMOVE:
		if( g_Application_Active && !g_Full_Screen )
		{
			RECT old_client_area = g_Client_Area;

			// Update window properties
			GetWindowRect(g_Main_Window_Handle, &g_Window_Bounds);
			GetClientRect(g_Main_Window_Handle, &g_Client_Area);

			if( RectWidth(old_client_area) != RectWidth(g_Client_Area) || RectHeight(old_client_area) != RectHeight(g_Client_Area) )
			{
				// A new window size will require a new backbuffer
				// size, so the 3D structures must be changed accordingly.
				g_Present_Parameters.BackBufferWidth  = RectWidth(g_Client_Area);
				g_Present_Parameters.BackBufferHeight = RectHeight(g_Client_Area);

				// Reset the 3D environment
				if( !ResetDevice() ) { D3DError("D3DConsole2WindowProc", "Failed to reset the 3D device"); return 0; }
			}
		}
		break;

	case WM_CLOSE:
	case WM_DESTROY: 
		// kill the application			
		PostQuitMessage(0);
		return 1;
    }

	// Process any unhandled messages
	return DefWindowProc(hwnd, msg, wparam, lparam);
}

//*****
// Display a warning message box
void D3DWarning( const char *title, const char *warn_str, ...)
{
	if( !g_ConsoleOutput.IsOpen() ) g_ConsoleOutput.Open();
	g_ConsoleOutput << title << ": ";
	va_list arglist;
    va_start(arglist, warn_str);
	g_ConsoleOutput.Output(warn_str, arglist);
	g_ConsoleOutput << "\n";
}

//*****
// Display an error message
void D3DError( const char *title, const char *err_str, ...)
{
	if( !g_ConsoleOutput.IsOpen() ) g_ConsoleOutput.Open();
	g_ConsoleOutput << title << ": ";
	va_list arglist;
    va_start(arglist, err_str);
	g_ConsoleOutput.Output(err_str, arglist);
	
	g_ConsoleOutput << "\nD3D Error: " << DXGetErrorString8(g_Last_Error) << ccENDLINE;
}

//****************************************************************************************************
// D3DCamera methods
//*****
// Get the forward vector (normalised)
D3DXVECTOR3 D3DCamera::GetForward() const
{
	D3DXVECTOR3 forward;
	if( m_righthanded )
	{
		forward[0] = float(cos(m_pitch) * sin(-m_yaw));
		forward[1] = float(sin(m_pitch));
		forward[2] = float(-cos(m_pitch) * cos(-m_yaw));
	}
	else
	{
		forward[0] = float(cos(m_pitch) * sin(m_yaw));
		forward[1] = float(sin(m_pitch));
		forward[2] = float(-cos(m_pitch) * cos(m_yaw));
	}
	return forward;
}

//*****
// Update the camera's position, orientation, view matrix
// 'elapsed_time' is in milli seconds
void D3DCamera::Update(float elapsed_seconds)
{
	if( elapsed_seconds > g_Max_Time_Step )
		elapsed_seconds = g_Max_Time_Step;

	// Update the camera position
	D3DXVECTOR3 velocity = m_velocity * elapsed_seconds;
	D3DXVec3TransformNormal( &velocity, &velocity, &m_orientation );
	if( m_lock_axis[0] ) velocity[0] = 0.0f;
	if( m_lock_axis[1] ) velocity[1] = 0.0f;
	if( m_lock_axis[2] ) velocity[2] = 0.0f;
	m_position += velocity;

	// Update the yaw-pitch-roll
	m_yaw	+= m_yaw_velocity	* elapsed_seconds;
	m_pitch += m_pitch_velocity * elapsed_seconds;
	m_roll	+= m_roll_velocity	* elapsed_seconds;

	// Set the view matrix
	D3DXQUATERNION rotation_q;
	D3DXQuaternionRotationYawPitchRoll(&rotation_q, m_yaw, m_pitch, m_roll);
	D3DXMatrixAffineTransformation(&m_orientation, 1.0f, NULL, &rotation_q, &m_position);
	D3DXMatrixInverse(&m_view_matrix, NULL, &m_orientation);
}


//****************************************************************************************************
// Quad methods
//*****
//
bool Quad::Initialise(char* filename, D3DCOLOR colour)
{
	strcpy(m_filename, filename);
	m_colour = colour;

	// Load the texture
	if( FAILED(D3DXCreateTextureFromFileEx(	g_D3D_Device, m_filename, 0, 0, 1, 0, g_Screen_Format, D3DPOOL_MANAGED,
											D3DX_FILTER_LINEAR, D3DX_FILTER_LINEAR, 0xFF00FFFF, NULL, NULL, &m_texture)) )
	{	D3DError("Quad::Initialise", "Failed to load the quad texture"); return false; }

	// Create vertex buffer
	if( FAILED(g_D3D_Device->CreateVertexBuffer(4 * sizeof(XYZ_DIFFUSE_TEX1), D3DUSAGE_WRITEONLY, 
												D3DFVF_XYZ_DIFFUSE_TEX1, D3DPOOL_MANAGED, &m_vertex_buffer)) )
	{	D3DError("Quad::Initialise", "Failed to create a vertex buffer for the quad"); return false; }

	// Fill the vertex buffer
	XYZ_DIFFUSE_TEX1 *vertices;
	if( FAILED(m_vertex_buffer->Lock(0, 4 * sizeof(XYZ_DIFFUSE_TEX1), (BYTE **)&vertices, D3DLOCK_DISCARD)) )
	{	D3DError("Quad::Quad", "Failed to lock the quad vertex buffer"); return false; }

	vertices->vertex = D3DXVECTOR3(1.0f, 0.0f, 0.0f);
	vertices->colour = m_colour;
	vertices->tu = 1.0f;	vertices->tv = 0.0f;
	vertices++;
	
	vertices->vertex = D3DXVECTOR3(0.0f, 0.0f, 0.0f);
	vertices->colour = m_colour;
	vertices->tu = 0.0f;	vertices->tv = 0.0f;
	vertices++;
	
	vertices->vertex = D3DXVECTOR3(1.0f, 1.0f, 0.0f);
	vertices->colour = m_colour;
	vertices->tu = 1.0f;	vertices->tv = 1.0f;
	vertices++;
	
	vertices->vertex = D3DXVECTOR3(0.0f, 1.0f, 0.0f);
	vertices->colour = m_colour;
	vertices->tu = 0.0f;	vertices->tv = 1.0f;
	vertices++;
	
	m_vertex_buffer->Unlock();
	return true;
}

//*****
//
void Quad::UnInitialise()
{
	if( m_texture ) m_texture->Release(), m_texture = NULL;
	if( m_vertex_buffer ) m_vertex_buffer->Release(), m_vertex_buffer = NULL;
}

//*****
// Draw the quad
void Quad::Render()
{	
	g_D3D_Device->SetTexture( 0, m_texture );

	g_D3D_Device->SetTextureStageState( 0, D3DTSS_COLORARG1, D3DTA_TEXTURE );
	g_D3D_Device->SetTextureStageState( 0, D3DTSS_COLORARG2, D3DTA_DIFFUSE );
	g_D3D_Device->SetTextureStageState( 0, D3DTSS_COLOROP, D3DTOP_MODULATE );
	
	g_D3D_Device->SetTextureStageState( 0, D3DTSS_ALPHAARG1, D3DTA_TEXTURE );
	g_D3D_Device->SetTextureStageState( 0, D3DTSS_ALPHAARG2, D3DTA_DIFFUSE );
	g_D3D_Device->SetTextureStageState( 0, D3DTSS_ALPHAOP, D3DTOP_SELECTARG1 );

	g_D3D_Device->SetRenderState( D3DRS_ALPHABLENDENABLE,	TRUE );
	g_D3D_Device->SetRenderState( D3DRS_SRCBLEND,			D3DBLEND_SRCALPHA );
	g_D3D_Device->SetRenderState( D3DRS_DESTBLEND,			D3DBLEND_INVSRCALPHA );

	g_D3D_Device->SetStreamSource( 0, m_vertex_buffer, sizeof(XYZ_DIFFUSE_TEX1) );
	g_D3D_Device->SetVertexShader( D3DFVF_XYZ_DIFFUSE_TEX1 );

	if( FAILED(g_D3D_Device->DrawPrimitive(D3DPT_TRIANGLESTRIP, 0, 2)) )
	{	D3DError("Quad::Render", "Failed to render quad"); }

	g_D3D_Device->SetTexture( 0, NULL );
}