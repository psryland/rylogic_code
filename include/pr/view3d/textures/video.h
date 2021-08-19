//*********************************************
// Renderer
//  Copyright (C) Rylogic Ltd 2007
//*********************************************

#pragma once
#ifndef PR_RDR_TEXTURES_VIDEO_H
#define PR_RDR_TEXTURES_VIDEO_H

#include "pr/view3d/forward.h"

// dshow.h forward defs
struct IMediaControl;
struct IMediaEventEx;
struct IMediaPosition;
struct IVideoWindow;
struct IVMRSurfaceAllocatorNotify9;

namespace pr
{
	namespace rdr
	{
		// Create a custom allocator/presentor
		//AllocPresPtr CreateAllocPres(D3DPtr<ID3D11Device>& device, D3DPtr<IVMRSurfaceAllocatorNotify9>& surface_alloc_notify);

		// A video texture.
		// This object is analogous to the Texture2D class
		struct Video :pr::RefCount<Video>
		{
			Texture2D*             m_tex;             // The texture that receives blt'd video data (must be a render target)
			//D3DPtr<IMediaControl>  m_media_control;
			//D3DPtr<IMediaEventEx>  m_media_event;
			//D3DPtr<IMediaPosition> m_media_position;
			//D3DPtr<IVideoWindow>   m_video_window;
			//AllocPresPtr           m_alloc_pres;      // Our custom allocator presenter
			string32               m_filepath;
			bool                   m_loop;

			// There are two ways to display a video in an application:
			//  - Render a frame when the video says to render
			//  - Render whenever you want but synchronise access to the texture between the app/vmr9
			Video();
			~Video();

			// Create the dshow filter graph for playing the video 'filepath'
			void CreateFromFile(D3DPtr<ID3D11Device>& device, wchar_t const* filepath);

			// Release resources and interfaces
			void Free();

			// Return the width/height of the video
			pr::iv2 GetNativeResolution();

			// Play the video (async)
			void Play(bool loop);

			// Pause the video.
			void Pause();

			// Stop the video
			void Stop();

			// Refcounting cleanup function
			static void RefCountZero(pr::RefCount<Video>* doomed);
		};
	}
}

#endif

