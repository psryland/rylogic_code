//***************************************************************************************
//
// A starting point for creating Direct 3D applications
//
//***************************************************************************************
#ifndef D3DCONSOLE2_H
#define D3DCONSOLE2_H

#include <d3d8.h>
#include <d3dx8.h>
#include <dxerr8.h>

// D3DConsole2 macro definitions
#ifdef _DEBUG
#define ASSERT(x) assert(x)
#else
#define ASSERT(x) ((void)(0))
#endif
#undef  FAILED
#define FAILED(status)		( (g_Last_Error=(HRESULT)(status)) <  0)
#undef	SUCCEEDED
#define SUCCEEDED(status)	( (g_Last_Error=(HRESULT)(status)) >= 0)
#define DBSTR0(a)				_DBSTR(a,0,0,0,0,0,0)
#define DBSTR1(a,b)				_DBSTR(a,b,0,0,0,0,0)
#define DBSTR2(a,b,c)			_DBSTR(a,b,c,0,0,0,0)
#define DBSTR3(a,b,c,d)			_DBSTR(a,b,c,d,0,0,0)
#define DBSTR4(a,b,c,d,e)		_DBSTR(a,b,c,d,e,0,0)
#define DBSTR5(a,b,c,d,e,f)		_DBSTR(a,b,c,d,e,f,0)
#define DBSTR6(a,b,c,d,e,f,g)	_DBSTR(a,b,c,d,e,f,g)
#define _DBSTR(a,b,c,d,e,f,g)	{													\
									assert(strlen(a) < MAX_DEBUG_STRING_LENGTH);	\
									char str[MAX_DEBUG_STRING_LENGTH];				\
									sprintf(str, a, b, c, d, e, f, g);				\
									OutputDebugString(str);							\
								}

// Limits and Constants
const DWORD APPLICATION_INACTIVE_SLEEP_TIME = 500; // milliseconds
const int MAX_MODES_PER_DEVICE = 300;
const int MAX_FORMATS_PER_DEVICE = 20;
const int MAX_DEVICES_PER_ADAPTER = 5;
const int MAX_ADAPTERS_PER_SYSTEM = 5;
const int MAX_QUAD_FILENAME_LENGTH = 256;
const int MAX_DEBUG_STRING_LENGTH = 256;

// Application globals.
extern HWND					g_Main_Window_Handle;
extern HINSTANCE			g_Main_Window_Instance;
extern char*				g_Command_Line;
extern DWORD				g_Game_Clock;
extern DWORD				g_Last_Frame;
extern DWORD				g_Elapsed_Milliseconds;
extern float				g_Max_Time_Step;
extern bool					g_Application_Active;
extern HRESULT				g_Last_Error;
extern D3DXMATRIX			g_Identity;
extern LPDIRECT3DDEVICE8	g_D3D_Device;


// Application global variables. 
extern bool			g_Full_Screen;
extern DWORD		g_Screen_Width;
extern DWORD		g_Screen_Height;
extern int			g_Screen_X;
extern int			g_Screen_Y;
extern float		g_Screen_Depth;
extern float		g_Screen_Shallowth;
extern D3DFORMAT	g_Depth_Format;
extern D3DFORMAT	g_Screen_Format;
extern int			g_Frame_Rate;
extern char*		g_Window_Title;

enum Axis
{
	X_AXIS = 0,
	Y_AXIS = 1,
	Z_AXIS = 2,
};

//*****
// Structures
//#define D3DFVF_XYZ
struct XYZ
{
    D3DXVECTOR3 vertex;
};
#define D3DFVF_XYZ_DIFFUSE (D3DFVF_XYZ|D3DFVF_DIFFUSE)
struct XYZ_DIFFUSE
{
    D3DXVECTOR3 vertex;
    D3DCOLOR    colour;
};
#define D3DFVF_XYZ_NORMAL (D3DFVF_XYZ|D3DFVF_NORMAL)
struct XYZ_NORMAL
{
    D3DXVECTOR3 vertex;
    D3DXVECTOR3 normal;
};
#define D3DFVF_XYZ_NORMAL_DIFFUSE (D3DFVF_XYZ|D3DFVF_NORMAL|D3DFVF_DIFFUSE)
struct XYZ_NORMAL_DIFFUSE
{
    D3DXVECTOR3 vertex;
    D3DXVECTOR3 normal;
	D3DCOLOR	colour;
};
#define D3DFVF_XYZ_DIFFUSE_TEX1 (D3DFVF_XYZ|D3DFVF_DIFFUSE|D3DFVF_TEX1)
struct XYZ_DIFFUSE_TEX1
{
	D3DXVECTOR3	vertex;
	D3DCOLOR	colour;
	float		tu;
	float		tv;
};


// Structure for holding information about a Direct3D device,
// including a list of modes compatible with this device
struct D3DDeviceInfo
{
	// Device data
	D3DDEVTYPE		m_device_type;		// Reference, HAL, etc.
	D3DCAPS8		m_caps;				// Capabilities of this device
	const TCHAR*	m_desc;				// Name of this device
	bool			m_can_do_windowed;	// Whether this device can work in windowed mode
	DWORD			m_behavior;			// Hardware / Software / Mixed vertex processing
	bool			m_acceptable;		// True if the capabilities of this device meet the application requirements

	// Modes for this device
	DWORD			m_num_modes;
	D3DDISPLAYMODE	m_modes[MAX_MODES_PER_DEVICE];

	// Current state
	DWORD			m_current_mode;
	bool			m_windowed;
	D3DMULTISAMPLE_TYPE m_multi_sample_type;
};

// Structure for holding information about an adapter,
// including a list of devices available on this adapter
struct D3DAdapterInfo
{
	// Adapter data
	D3DADAPTER_IDENTIFIER8	m_adapter_identifier;
	D3DDISPLAYMODE			m_desktop_display_mode;      // Desktop display mode for this adapter

	// Devices for this adapter
	DWORD			m_num_devices;
	D3DDeviceInfo	m_devices[MAX_DEVICES_PER_ADAPTER];

	// The device currently being used
	DWORD			m_current_device;
};


//*****
// External functions available to the user program
LRESULT D3DConsole2WindowProc(HWND hwnd, UINT msg, WPARAM wparam, LPARAM lparam);
void D3DWarning(const char *title, const char *warn_str, ...);	// Display a warning message
void D3DError(const char *title, const char *err_str, ...);		// Display a message box containing an error
inline void	End() {	SendMessage(g_Main_Window_Handle, WM_CLOSE, 0, 0); }

//*****
// Functions that must be declared in the user program

// This function is used to initialise the parameters of the application.
// e.g. Screen Width, Screen Height, Full Screen, etc...
bool PreWindowCreationInitialisation();

// Initialise/UnInitialise the application
// These two functions are used to create objects once Direct3D has been initialised. They
// will also be called if the device is re-created in scenarios such as a different device
// is selected. After UnInitialiseApplication is called, the device interface may be released
// therefore all device dependent objects should be released.
bool InitialiseApplication();
void UnInitialiseApplication();

// These two functions are called when the device is lost. The objects that must be
// initialised/released in these functions are:
// 1) Any extra swap chain surfaces created through the CreateAdditionalSwapChain method
// 2) Any render target surfaces created through CreateRenderTarget
// 3) Any depth stencil resources created through CreateDepthStencilSurface
// 4) Anything created in the D3DPOOL_DEFAULT memory class
bool CreateDeviceDependentObjects();
void ReleaseDeviceDependentObjects();

// This function is called to do the processing of every frame.
void Main();

// This function is called to draw each frame
void Render();

// This is the callback function for the main application window.
// The user program must declare this. It can however call the D3DConsole2WindowProc
// function for default handling of windows messages.
LRESULT CALLBACK WindowProc(HWND hwnd, UINT msg, WPARAM wparam, LPARAM lparam); // { return D3DConsole2WindowProc(hwnd, msg, wparam, lparam); } 

// This method should return true if the provided capabilities meet the requirements
// of the application. The application may also change the vertex_processing method to
// one of D3DCREATE_SOFTWARE_VERTEXPROCESSING, D3DCREATE_HARDWARE_VERTEXPROCESSING, etc
bool IsDeviceAcceptable(const D3DCAPS8& caps, DWORD& vertex_processing);

//*****
// General utility functions
inline double DRand(double mn, double mx)	{ return ((rand()/(double)RAND_MAX)*(mx - mn)) + mn; }
inline int	  Rand(int mn, int mx)			{ if( mx == mn ) return mx; return (rand()%(mx - mn)) + mn; }
inline long   LRand(long mn, long mx)		{ if( mx == mn ) return mx; return ((rand()*rand())%(mx - mn)) + mn; }
inline double URand(DWORD mn, DWORD mx)		{ if( mx == mn ) return mx; return ((rand()*rand())%(mx - mn)) + mn; }
inline int	  RectWidth(RECT rect)			{ return rect.right - rect.left; }
inline int	  RectHeight(RECT rect)			{ return rect.bottom - rect.top; }
inline DWORD  FtoDW( float f )				{ return *((DWORD*)&f); }	// Helper function to stuff a float into a DWORD argument

//*****
// A base class for managing the view matrix
class D3DCamera
{
public:
	D3DCamera() :
		m_position(0.0f,0.0f,0.0f),
		m_velocity(0.0f,0.0f,0.0f),
		m_yaw(0.0f),	m_yaw_velocity(0.0f),
		m_pitch(0.0f),	m_pitch_velocity(0.0f),
		m_roll(0.0f),	m_roll_velocity(0.0f)
		{	D3DXMatrixIdentity(&m_view_matrix); m_lock_axis[0] = m_lock_axis[1] = m_lock_axis[2] = false; }
	virtual ~D3DCamera() {}
	
//	void SetViewMatrix(const D3DXMATRIX &mat)	{ m_view_matrix = mat;  }
	const D3DXMATRIX &GetViewMatrix() const		{ return m_view_matrix; }
	D3DXVECTOR3 GetForward() const;
	void LockAxis(Axis which, bool locked)		{ m_lock_axis[which] = locked; }

	void RightHanded(bool righthanded)			{ m_righthanded = righthanded; }
	void SetPosition(const D3DXVECTOR3 &pos)	{ m_position = pos; }
	void SetYaw(const float yaw)				{ m_yaw = yaw; }
	void SetPitch(const float pitch)			{ m_pitch = pitch; }
	void SetRoll(const float roll)				{ m_roll = roll; }
	D3DXVECTOR3 GetPosition() const 			{ return m_position; }
	float GetYaw() const						{ return m_yaw; }
	float GetPitch() const						{ return m_pitch; }
	float GetRoll() const						{ return m_roll; }
	
	void Accelerate(D3DXVECTOR3 &accel)				{ m_velocity += accel;	}
	void Rotate(float yaw, float pitch, float roll)	{ m_yaw_velocity += yaw; m_pitch_velocity += pitch; m_roll_velocity += roll; }

	void Update(float elapsed_seconds);
	void DecelerateLinear(float percentage)		{ m_velocity *= percentage;	}
	void DecelerateRotational(float percentage)	{ m_yaw_velocity *= percentage; m_pitch_velocity *= percentage; m_roll_velocity *= percentage; }

protected:
	D3DXMATRIX  m_view_matrix;
	D3DXMATRIX  m_orientation;
	D3DXVECTOR3 m_position;
	D3DXVECTOR3 m_velocity;
	float		m_yaw;
	float		m_yaw_velocity;
	float		m_pitch;
	float		m_pitch_velocity;
	float		m_roll;
	float		m_roll_velocity;
	bool		m_righthanded;
	bool		m_lock_axis[3];
};

//*****
// A generic quad for texture surfaces
class Quad
{
public:
	Quad() : m_texture(NULL), m_vertex_buffer(NULL) {}
	~Quad() { ASSERT(m_texture == NULL); ASSERT(m_vertex_buffer == NULL); }

	bool Initialise(char* filename, D3DCOLOR colour);
	void UnInitialise();
	void Render();

private:
	char m_filename[MAX_QUAD_FILENAME_LENGTH];
	D3DCOLOR m_colour;
	LPDIRECT3DTEXTURE8 m_texture;
	LPDIRECT3DVERTEXBUFFER8 m_vertex_buffer;
};

#endif//D3DCONSOLE2_H
