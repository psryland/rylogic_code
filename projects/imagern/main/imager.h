//********************************
// ImagerN
//  Copyright © Rylogic Ltd 2011
//********************************
#pragma once
#ifndef IMAGERN_IMAGER_H
#define IMAGERN_IMAGER_H

#include "imagern/main/forward.h"
#include "imagern/settings/user_settings.h"
#include "imagern/main/photo_model.h"
#include "imagern/media/media_list.h"

class Imager
{
	UserSettings         m_settings;
	pr::rdr::Allocator   m_alloc;
	pr::Renderer         m_rdr;
	pr::rdr::Viewport    m_view0;
	pr::Camera           m_cam;
	pr::sqlite::Database m_db;
	MediaList            m_media;
	MainGUI&             m_gui;
	Photo                m_photo_buf0;
	Photo                m_photo_buf1;
	Photo*               m_photo0;
	Photo*               m_photo1;
	DWORD                m_my_thread_id;
	
	Imager(Imager const&);
	Imager& operator=(Imager const&);
	
	// Render to the window
	void DoRender();
	
public:
	Imager(MainGUI& gui);
	~Imager();
	
	// Access the user settings
	UserSettings& Settings() { return m_settings; }
	
	// Get the currently displayed photo
	Photo const& CurrentPhoto() const;
	
	// Set the current media file to show
	void SetMedia(MediaFile const& filepath);
	
	// Position the camera so that the image is zoomed appropriately
	void ResetZoom(pr::IRect const& rect);
	
	// Mouse navigation
	void Nav(pr::v2 const& pt, int btn_state, bool nav_start_stop);
	void NavZ(float delta);
	void ClampCameraPosition();
	
	// The size of the window has changed
	void Resize(pr::IRect const& client_area);
	
	// Refresh the window
	void Render();
};

#endif
