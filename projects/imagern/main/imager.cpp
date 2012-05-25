//********************************
// ImagerN
//  Copyright © Rylogic Ltd 2011
//********************************
#include "imagern/main/stdafx.h"
#include "imagern/main/imager.h"
#include "imagern/gui/main_gui.h"
#include "imagern/media/media_file.h"

// Return the filename for the user settings file
// Look for a file in the same directory called 'portable'
// if found use the app directory to write settings
string GetUserSettingsFilename(HWND hwnd)
{
	// Determine the directory we're running in
	char temp[MAX_PATH];
	GetModuleFileNameA(0, temp, MAX_PATH);
	string path = temp;
	
	if (pr::filesys::FileExists(pr::filesys::GetDirectory(path) + "\\portable"))
		return pr::filesys::RmvExtension(path) + ".cfg";
	
	if (pr::Failed(SHGetFolderPathA(hwnd, CSIDL_LOCAL_APPDATA, 0, CSIDL_FLAG_CREATE, temp)))
		return pr::filesys::RmvExtension(path) + ".cfg";

	return string(temp) + "\\Rylogic\\ImagerN\\" + pr::filesys::GetFiletitle(path) + ".cfg";
}

// Return settings to configure the render
pr::rdr::RdrSettings GetRdrSettings(HWND hwnd, pr::rdr::Allocator& rdr_allocator, pr::IRect const& client_area)
{
	pr::rdr::RdrSettings s;
	s.m_window_handle      = hwnd;
	s.m_device_config      = pr::rdr::GetDefaultDeviceConfigWindowed(D3DDEVTYPE_HAL, D3DCREATE_MULTITHREADED);
	s.m_allocator          = &rdr_allocator;
	s.m_client_area        = client_area;
	s.m_zbuffer_format     = D3DFMT_D24S8;
	s.m_swap_effect        = D3DSWAPEFFECT_DISCARD;//D3DSWAPEFFECT_COPY;// - have to use discard for antialiasing, but that means no blt during resize
	s.m_back_buffer_count  = 1;
	s.m_geometry_quality   = pr::rdr::EQuality::High;
	s.m_texture_quality    = pr::rdr::EQuality::High;
	s.m_background_colour  = pr::Colour32Black;
	s.m_max_shader_version = pr::rdr::EShaderVersion::v3_0;
	return s;
}

// Return settings to configure the viewport
pr::rdr::VPSettings GetVPSettings(pr::Renderer& rdr, pr::rdr::ViewportId id)
{
	pr::rdr::VPSettings s;
	s.m_renderer   = &rdr;
	s.m_identifier = id;
	return s;
}

// The main app logic
Imager::Imager(MainGUI& gui)
:m_settings(GetUserSettingsFilename(gui.m_hWnd), true)
,m_alloc()
,m_rdr(GetRdrSettings(gui.m_hWnd, m_alloc, pr::ClientArea(gui.m_hWnd)))
,m_view0(GetVPSettings(m_rdr, 0))
,m_cam()
,m_db(m_settings.m_db_path.c_str())
,m_media(m_settings)
,m_gui(gui)
,m_photo_buf0(m_rdr)
,m_photo_buf1(m_rdr)
,m_photo0(&m_photo_buf0)
,m_photo1(&m_photo_buf1)
,m_my_thread_id(GetCurrentThreadId())
{
	// Position the camera
	m_cam.Aspect(1.0f);
	m_cam.FovY(pr::maths::tau_by_8);
	m_cam.LookAt(
		pr::v4::make(0, 0, 1.0f / (float)tan(m_cam.m_fovY/2.0f), 1.0f),
		pr::v4Origin, 
		pr::v4YAxis, true);
	m_view0.CameraToWorld(m_cam.CameraToWorld());
	
	// Configure a light
	pr::rdr::Light& light = m_rdr.m_light_mgr.m_light[0];
	light.m_type           = pr::rdr::ELight::Directional;
	light.m_direction      = -pr::v4ZAxis;
	light.m_ambient        = pr::Colour32Zero;
	light.m_diffuse        = pr::Colour32Gray;
	light.m_specular       = pr::Colour32Zero;
	light.m_specular_power = 0;
	light.m_cast_shadows   = false;
}
Imager::~Imager()
{
	m_settings.Save();
}

// Return the media type of a file implied by it's path
EMedia::Type MediaTypeFromExtn(UserSettings const& settings, char const* extn)
{
	if (*pr::str::FindStrNoCase(settings.m_image_extns.c_str(), extn) != 0) return EMedia::Image;
	if (*pr::str::FindStrNoCase(settings.m_video_extns.c_str(), extn) != 0) return EMedia::Video;
	if (*pr::str::FindStrNoCase(settings.m_audio_extns.c_str(), extn) != 0) return EMedia::Audio;
	return EMedia::Unknown;
}

// Get the currently displayed photo
Photo const& Imager::CurrentPhoto() const
{
	return *m_photo0;
}

// Set the current media file to show
void Imager::SetMedia(MediaFile const& mf)
{
	try
	{
		PR_ASSERT(DBG, GetCurrentThreadId() == m_my_thread_id, "Cross thread call to SetMedia");
		
		m_gui.Status(pr::FmtS("Loading: %s", mf.m_path.c_str()), false);
		m_photo1->Update(MediaTypeFromExtn(m_settings, mf.Extn().c_str()), mf.m_path);
	
		// Transition from m_photo0 to m_photo1
		std::swap(m_photo0,m_photo1);
		m_view0.RemoveInstance(*m_photo1);
		m_view0.AddInstance(*m_photo0);
	
		// Notify observers
		pr::events::Send(Event_MediaSet(&mf, pr::uint(m_photo0->Width()), pr::uint(m_photo0->Height())));
	}
	catch (std::exception const&)
	{
		pr::events::Send(Event_Message(pr::FmtS("Failed to load: %s", mf.File().c_str()), Event_Message::Error));
	}
}

// Position the camera so that the image is zoomed appropriately
void Imager::ResetZoom(pr::IRect const& rect)
{
	float cam_aspect = m_cam.m_aspect;
	float img_aspect = m_photo0->Aspect();
	bool x_bound = img_aspect > cam_aspect;
	
	// 'dist' is the distance to fit the photo's largest dimension within the camera field of view
	// The largest axis of the photo model has length = 1.0f
	float dist;
	if (x_bound)
	{
		float size = img_aspect >= 1.0f ? 1.0f : img_aspect;
		dist = size / pr::Tan(m_cam.FovX() * 0.5f);
	}
	else
	{
		float size = img_aspect <= 1.0f ? 1.0f : 1.0f/img_aspect;
		dist = size / pr::Tan(m_cam.FovY() * 0.5f);
	}
	
	// 'scale' is the amount the image would be scaled by in order to fit it to the window
	// Use this with the zoom type to decide how to actually scale the image.
	float scale = (x_bound) ? ((float)rect.SizeX() / m_photo0->Width()) : ((float)rect.SizeY() / m_photo0->Height());
	if (m_settings.m_zoom_fill && scale > 1.0f) scale = 1.0f;
	if (m_settings.m_zoom_fit  && scale < 1.0f) scale = 1.0f;
	
	// Position the camera
	m_cam.LookAt(pr::v4::make(0.0f, 0.0f, scale * dist, 1.0f), pr::v4Origin, pr::v4YAxis, true);
}

// Mouse navigation
void Imager::Nav(pr::v2 const& pt, int btn_state, bool nav_start_stop)
{
	if (nav_start_stop) m_cam.MoveRef(pt, btn_state);
	else                m_cam.Move   (pt, btn_state);
	ClampCameraPosition();
	Render();
}
void Imager::NavZ(float delta)
{
	m_cam.MoveZ(delta, true);
	ClampCameraPosition();
	Render();
}

/// <summary>Clamps the view3d camera to within the allowed position space</summary>
void Imager::ClampCameraPosition()
{
	pr::m4x4 c2w = m_cam.CameraToWorld();
	c2w.pos.z = pr::Clamp(c2w.pos.z, 0.01f, 100.0f);
	float img_aspect = m_photo0->Aspect();
	float xsize = img_aspect >= 1.0f ? 1.0f :        img_aspect;  // the normalised x dimension of the image
	float ysize = img_aspect <= 1.0f ? 1.0f : 1.0f / img_aspect;  // the normalised y dimension of the image
	float xmaxz = xsize / (float)pr::Tan(m_cam.FovX() * 0.5f);    // the z distance that fits the image to screen in the x direction
	float ymaxz = ysize / (float)pr::Tan(m_cam.FovY() * 0.5f);    // the z distance that fits the image to screen in the y direction
	float xlim = pr::Max(xsize * (xmaxz - c2w.pos.z) / xmaxz, 0.0f);
	float ylim = pr::Max(ysize * (ymaxz - c2w.pos.z) / ymaxz, 0.0f);
	c2w.pos.x = pr::Clamp(c2w.pos.x, -xlim, xlim);
	c2w.pos.y = pr::Clamp(c2w.pos.y, -ylim, ylim);
	
	m_cam.CameraToWorld(c2w, false);
	m_cam.FocusDist(c2w.pos.z);
}

// The size of the window has changed
void Imager::Resize(pr::IRect const& client_area)
{
	m_rdr.Resize(client_area);
	m_cam.Aspect(client_area.Aspect());
	ResetZoom(client_area);
}

// Batch render requests
void Imager::Render()
{
	DoRender();
	//if (m_rdr_pending)
	//	return;
	//
	//m_rdr_pending = true;
	//PostMessage(m_gui.m_hWnd, 
}

// Update the display
void Imager::DoRender()
{
	// Render the viewports
	if (pr::Failed(m_rdr.RenderStart()))
		return;
	
	// Set the viewport view
	m_view0.SetView(m_cam);
	
	// Add objects from the store to the viewport
	//m_view0.ClearDrawlist();
	//for (std::size_t i = 0, iend = m_store.Count(); i != iend; ++i)
	//	m_store[i].AddToViewport(m_view0);
	
	// Render the m_view0
	m_view0.Render();
	
	m_rdr.RenderEnd();
	m_rdr.Present();
}

