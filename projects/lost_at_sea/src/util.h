//************************************
// Lost at Sea
//  Copyright © Rylogic Ltd 2011
//************************************
#ifndef LAS_UTIL_H
#define LAS_UTIL_H
#pragma once

#include "forward.h"

namespace las
{
	// Return the directory that this app is running in
	inline wstring ModuleDir()
	{
		wchar_t temp[MAX_PATH]; GetModuleFileNameW(0, temp, MAX_PATH);
		return pr::filesys::GetDirectory<wstring>(temp);
	}
	
	// Return true if the app is running as a portable app
	inline bool IsPortable()
	{
		// Look for a file called "portable" in the same directory as the main app
		return pr::filesys::FileExists(pr::filesys::CombinePath<wstring>(ModuleDir(), L"portable"));
	}
	
	// Return the path to the settings file
	inline wstring SettingsPath(HWND hwnd)
	{
		wstring dir = ModuleDir();
		
		// If portable use a local settings file
		if (IsPortable())
			return pr::filesys::CombinePath<wstring>(dir, L"settings.cfg");
		
		// Otherwise find the user app data folder
		wchar_t temp[MAX_PATH];
		if (pr::Failed(SHGetFolderPathW(hwnd, CSIDL_LOCAL_APPDATA, 0, CSIDL_FLAG_CREATE, temp)))
			throw las::Exception(EResult::SettingsPathNotFound, "The settings path could not be created");
		
		wstring path = temp;
		path += L"\\";
		path += pr::str::ToWString<wstring>(las::AppVendor());
		path += L"\\";
		path += pr::str::ToWString<wstring>(las::AppTitle());
		path += L"\\settings.cfg";
		return path;
	}
	
	// Return configuration settings for the renderer
	inline pr::rdr::RdrSettings RdrSettings(HWND hwnd, Settings const& settings, pr::rdr::Allocator& alloc, pr::IRect const& client_area)
	{
		pr::rdr::RdrSettings s;
		s.m_window_handle      = hwnd;
		s.m_device_config      = settings.m_fullscreen ?
			pr::rdr::GetDefaultDeviceConfigFullScreen(settings.m_res_x, settings.m_res_y, D3DDEVTYPE_HAL) :
			pr::rdr::GetDefaultDeviceConfigWindowed(D3DDEVTYPE_HAL);
		s.m_allocator          = &alloc;
		s.m_client_area        = client_area;
		s.m_zbuffer_format     = D3DFMT_D24S8;
		s.m_swap_effect        = D3DSWAPEFFECT_DISCARD;//D3DSWAPEFFECT_COPY;// - have to use discard for antialiasing, but that means no blt during resize
		s.m_back_buffer_count  = 1;
		s.m_geometry_quality   = settings.m_geometry_quality;
		s.m_texture_quality    = settings.m_texture_quality;
		s.m_background_colour  = pr::Colour32Black;
		s.m_max_shader_version = pr::rdr::EShaderVersion::v3_0;
		return s;
	}
	
	// Return renderer viewport settings
	inline pr::rdr::VPSettings VPSettings(pr::Renderer& rdr, pr::rdr::ViewportId id)
	{
		pr::rdr::VPSettings s;
		s.m_renderer   = &rdr;
		s.m_identifier = id;
		return s;
	}
	
	// Return the full
	inline string DataPath(string const& relpath)
	{
		return pr::filesys::CombinePath<string>("C:/Users/Paul/Rylogic_Code/projects/lost_at_sea/data", relpath);
	}
}

#endif
