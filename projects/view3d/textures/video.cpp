//*********************************************
// Renderer
//  Copyright (C) Rylogic Ltd 2012
//*********************************************
#include "view3d/util/stdafx.h"
#include "pr/view3d/textures/video.h"
#include "pr/view3d/textures/texture2d.h"
//#include "pr/view3d/utility/globalfunctions.h"
//#include "pr/threads/critical_section.h"
//#include <dshow.h> // link to "strmiids.lib" - Don't put this in forward.h because it includes windows headers that break WTL :-/
//#include <vmr9.h>

#pragma comment(lib, "strmiids.lib") // for the IID_... guids

using namespace pr;
using namespace pr::rdr;
/*
namespace pr
{
	namespace rdr
	{
		// A custom allocator/presenter for rendering video to a dx texture
		struct AllocPres
			:IVMRSurfaceAllocator9
			,IVMRImagePresenter9
			,pr::RefCount<AllocPres>
		{
			typedef std::vector< D3DPtr<IDirect3DSurface9> > SurfCont;
			pr::rdr::Video&                     m_video;                   // The video object that owns this allocator/presenter
			D3DPtr<IDirect3DDevice9>            m_d3d_device;              // The d3d device
			D3DPtr<IVMRSurfaceAllocatorNotify9> m_surf_alloc_notify;
		//	D3DPtr<IDirect3DSurface9>           m_rdr_target;              // With VMR9 in mixing mode, the render target is changed and not restored. Keep this pointer so we can restore it
			pr::threads::CritSection            m_cs;              
			SurfCont                            m_surfaces;
			SIZE                                m_native_res;              // The native resolution of the video
			
			AllocPres(Video& video, D3DPtr<IDirect3DDevice9> d3d_device)
			:m_video(video)
			,m_d3d_device(d3d_device)
			,m_surf_alloc_notify()
			//,m_rdr_target()
			,m_cs()
			,m_surfaces()
			,m_native_res()
			{
				#if PR_DBG_RDR
				D3DDEVICE_CREATION_PARAMETERS params;
				Throw(m_d3d_device->GetCreationParameters(&params));
				PR_ASSERT(PR_DBG_RDR, params.BehaviorFlags & D3DCREATE_MULTITHREADED, "the d3d device must be thread safe");
				#endif
		
				//// Get a pointer to the current render target because the VMR9 changes it without restoring it
				//Throw(m_d3d_device->GetRenderTarget(0, &m_rdr_target.m_ptr));
			}
			
			DWORD_PTR UserId() const { return DWORD_PTR(this); }
			
			// IVMRSurfaceAllocator9 **************************************
			// The AdviseNotify method provides the allocator-presenter with the VMR-9 filter's interface
			// for notification callbacks. If you are using a custom allocator-presenter, the application
			// must call this method on the allocator-presenter, with a pointer to the VMR's IVMRSurfaceAllocatorNotify9
			// interface. The allocator-presenter uses this interface to communicate with the VMR.
			// If you are not using a custom allocator-presenter, the application does not have to call this method.
			HRESULT STDMETHODCALLTYPE AdviseNotify(IN IVMRSurfaceAllocatorNotify9 *lpIVMRSurfAllocNotify)
			{
				m_surf_alloc_notify = lpIVMRSurfAllocNotify;
				return S_OK;
			}
			
			// The InitializeDevice method is called by the VMR9 when it needs the allocator-presenter to allocate surfaces.
			HRESULT STDMETHODCALLTYPE InitializeDevice(IN DWORD_PTR dwUserID, IN VMR9AllocationInfo* lpAllocInfo, IN OUT DWORD *lpNumBuffers)
			{
				if (lpNumBuffers == 0) return E_POINTER;
				if (*lpNumBuffers == 0) return S_OK;
				if (m_surf_alloc_notify == 0) return E_FAIL;
				m_native_res = lpAllocInfo->szNativeSize;
				
				// If the device only supports pow2 textures, find the texture size to allocate
				D3DCAPS9 d3dcaps;
				Throw(m_d3d_device->GetDeviceCaps(&d3dcaps));
				if (d3dcaps.TextureCaps & D3DPTEXTURECAPS_POW2)
				{
					DWORD w = 1; while (w < lpAllocInfo->dwWidth ) w <<= 1;
					DWORD h = 1; while (w < lpAllocInfo->dwHeight) h <<= 1;
					lpAllocInfo->dwWidth  = w;
					lpAllocInfo->dwHeight = h;
				}
				
				// If format is unknown, get the display format
				if (lpAllocInfo->Format == D3DFMT_UNKNOWN)
				{
					D3DDISPLAYMODE dm;
					Throw(m_d3d_device->GetDisplayMode(0, &dm));
					lpAllocInfo->Format = dm.Format;
				}
				
				PR_ASSERT(PR_DBG_RDR, !AllSet(lpAllocInfo->dwFlags, VMR9AllocFlag_3DRenderTarget), "This alloc/pres is intended for renderless mode");
				
				// Allocate the required surfaces
				m_surfaces.resize(*lpNumBuffers);
				Throw(m_surf_alloc_notify->AllocateSurfaceHelper(lpAllocInfo, lpNumBuffers, reinterpret_cast<IDirect3DSurface9**>(&m_surfaces[0])));
				return S_OK;
			}
			
			// The TerminateDevice method releases the Direct3D device.
			HRESULT STDMETHODCALLTYPE TerminateDevice(IN DWORD_PTR dwID)
			{
				m_surfaces.resize(0);
				return S_OK;
			}
			
			// The GetSurface method gets a Direct3D surface from the allocator-presenter.
			HRESULT STDMETHODCALLTYPE GetSurface(IN DWORD_PTR dwUserID, IN DWORD SurfaceIndex, IN DWORD SurfaceFlags, OUT IDirect3DSurface9 **lplpSurface)
			{
				if (lplpSurface == 0) return E_POINTER;
				if (SurfaceIndex >= m_surfaces.size()) return E_FAIL;
				D3DPtr<IDirect3DSurface9>& surf = m_surfaces[SurfaceIndex];
				(*lplpSurface) = surf.m_ptr;
				(*lplpSurface)->AddRef();
				return S_OK;
			}
			
			// IVMRImagePresenter9 *********************************************************
			// The StartPresenting method is called just before the video starts playing.
			// The allocator-presenter should perform any necessary configuration in this method.
			HRESULT STDMETHODCALLTYPE StartPresenting(IN DWORD_PTR dwUserID)
			{
				//PR_ASSERT(PR_DBG_RDR, m_d3d_device, "no valid d3d device");
				return S_OK;
			}
			
			// The StopPresenting method is called just after the video stops playing.
			// The allocator-presenter should perform any necessary cleanup in this method.
			HRESULT STDMETHODCALLTYPE StopPresenting(IN DWORD_PTR dwUserID)
			{
				return S_OK;
			}
			
			// The PresentImage method is called at precisely the moment this video frame should be presented.
			// PresentImage can be called when the filter is in either a running or a paused state.
			// StartPresenting and StopPresenting can be called only in a running state. Therefore,
			// if the graph is paused before it is run, PresentImage will be called before StartPresenting.
			// Applications can create custom blending effects by using a single instance of an
			// allocator-presenter with multiple instances of the VMR either in a single filter graph
			// or in multiple filter graphs. Using the allocator presenter in this way enables applications
			// to blend streams from different filter graphs, or blend different streams within the same
			// filter graph. If you are using a single instance of the VMR, set this value to zero.
			HRESULT STDMETHODCALLTYPE PresentImage(IN DWORD_PTR dwUserID, IN VMR9PresentationInfo *lpPresInfo)
			{
				// NOTE: this method is called in the thread context of the VMR
				if (lpPresInfo == 0) return E_POINTER;
				if (lpPresInfo->lpSurf == 0) return E_POINTER;
				
				pr::threads::CSLock lock(m_cs);
				if (!m_d3d_device) return S_OK;
				
				//// Restore the render target, since the VMR9 will have changed it.
				//m_d3d_device->SetRenderTarget(0, m_rdr_target.m_ptr);
				
				// Copy the frame to the render target texture (if it's valid)
				// Note: The video is not responsible for this texture. It should be managed independently
				if (m_video.m_tex && m_video.m_tex->m_tex)
				{
					// Copy the full surface onto the texture's surface
					D3DPtr<IDirect3DSurface9> surface = m_video.m_tex->surf(0);
					HRESULT res = m_d3d_device->StretchRect(lpPresInfo->lpSurf, 0, surface.m_ptr, 0, D3DTEXF_NONE);
					if (res == D3DERR_DEVICELOST) { return S_OK; } // ignore device lost, the renderer will handle it
					Throw(res); 
				}
				return S_OK;
			}
	
			// IUnknown *********************************************
			ULONG STDMETHODCALLTYPE   AddRef()  { return (ULONG)pr::RefCount<AllocPres, true>::AddRef(); }
			ULONG STDMETHODCALLTYPE   Release() { return (ULONG)pr::RefCount<AllocPres, true>::Release(); }
			HRESULT STDMETHODCALLTYPE QueryInterface(REFIID riid, void** ppvObject)
			{
				if (ppvObject == 0) return E_POINTER;
				if (riid == IID_IVMRSurfaceAllocator9) { *ppvObject = static_cast<IVMRSurfaceAllocator9*>(this); AddRef(); return S_OK; }
				if (riid == IID_IVMRImagePresenter9)   { *ppvObject = static_cast<IVMRImagePresenter9*>(this); AddRef(); return S_OK; }
				if (riid == IID_IUnknown)              { *ppvObject = static_cast<IUnknown*>(static_cast<IVMRSurfaceAllocator9*>(this)); AddRef(); return S_OK; }
				return E_NOINTERFACE;
			}
	
		protected:
			AllocPres(AllocPres const&); // no copying
			AllocPres& operator = (AllocPres const&);
		};
	}
}
*/
// Construct the video object
pr::rdr::Video::Video()
	:m_tex()
	//,m_media_control()
	//,m_media_event()
	//,m_media_position()
	//,m_video_window()
	//,m_alloc_pres()
	,m_loop(false)
{}
pr::rdr::Video::~Video()
{
	Free();
}

// Setup this object ready to render a video.
void pr::rdr::Video::CreateFromFile(D3DPtr<ID3D11Device>& device, wchar_t const* filepath)
{
	(void)device;
	(void)filepath;
/*
	Free();

	// Create the filter graph
	D3DPtr<IGraphBuilder> graph_builder;
	pr::Throw(CoCreateInstance(CLSID_FilterGraph, 0, CLSCTX_INPROC_SERVER, IID_IGraphBuilder, (void**)&graph_builder.m_ptr));

	// Get other useful interfaces
	pr::Throw(graph_builder->QueryInterface(IID_IMediaControl  ,(void**)&m_media_control.m_ptr));
	pr::Throw(graph_builder->QueryInterface(IID_IMediaEvent    ,(void**)&m_media_event.m_ptr));
	pr::Throw(graph_builder->QueryInterface(IID_IMediaPosition ,(void**)&m_media_position.m_ptr));
	pr::Throw(graph_builder->QueryInterface(IID_IVideoWindow   ,(void**)&m_video_window.m_ptr));

	// Create a VMR9 filter
	D3DPtr<IBaseFilter> vmr9;
	pr::Throw(CoCreateInstance(CLSID_VideoMixingRenderer9, 0, CLSCTX_INPROC_SERVER, IID_IBaseFilter, (void**)&vmr9.m_ptr));

	// Careful with order of calls here

	// Get the IVMRFilterConfig9 interface and configure the vmr9
	// We need to put the VMR9 in mixing mode to have deinterlacing work, this is done by
	// calling 'SetNumberOfStreams' before connecting any video streams.
	// (If we don't need deinterlacing, perhaps I should use passthrough mode)
	// (it prevents the render target problems described below)
	D3DPtr<IVMRFilterConfig9> config;
	pr::Throw(vmr9->QueryInterface(IID_IVMRFilterConfig9, (void**)&config.m_ptr));
	pr::Throw(config->SetNumberOfStreams(1));
	pr::Throw(config->SetRenderingMode(VMR9Mode_Renderless));
	
	// Get the surface allocator notify interface
	D3DPtr<IVMRSurfaceAllocatorNotify9> surface_alloc_notify;
	pr::Throw(vmr9->QueryInterface(IID_IVMRSurfaceAllocatorNotify9, (void**)&surface_alloc_notify.m_ptr));
	pr::Throw(surface_alloc_notify->SetD3DDevice(device.m_ptr, pr::rdr::GetMonitor(device)));
	// Note: this throws E_NOINTERFACE if the debug dx runtimes are used with the D3D_DEBUG_INFO define

	// Create a custom allocator/presentor
	m_alloc_pres = new AllocPres(*this, device);

	//// Let the allocator and the notify know about each other
	//Throw(surface_alloc_notify->AdviseSurfaceAllocator(m_alloc_pres->UserId(), m_alloc_pres.m_ptr));
	//Throw(m_alloc_pres->AdviseNotify(surface_alloc_notify.m_ptr));
	//
	//// The VMR runs in its own thread, and normally sets the rendertarget whenever it
	//// feels like it. This buggers up normal rendering as there isn't a decent way to synchronise
	//// the vmr thread with the main thread. However, setting the mixer to YUV mode apparently
	//// prevents the vmr having to set the render target, so the collision is solved.
	//D3DPtr<IVMRMixerControl9> mix;
	//Throw(vmr9->QueryInterface(IID_IVMRMixerControl9, (void**)&mix.m_ptr));
	//DWORD prefs = 0; Throw(mix->GetMixingPrefs(&prefs));
	//prefs = pr::SetBits(prefs, MixerPref9_RenderTargetMask, MixerPref9_RenderTargetYUV);
	//Throw(mix->SetMixingPrefs(prefs));
	//
	//// Add the vmr9 to the graph builder first so that it uses it instead of the default renderer
	//Throw(graph_builder->AddFilter(vmr9.m_ptr, L"VMR9"));

	// Generate the graph
	std::wstring fpath = pr::To<std::wstring>(filepath);
	Throw(graph_builder->RenderFile(fpath.c_str(), 0));
	Throw(m_media_control->StopWhenReady());
	m_filepath = filepath;
*/
}

// Release resources and interfaces
void pr::rdr::Video::Free()
{
	//// Ensure playback has stopped
	//if (m_media_control)
	//{
	//	OAFilterState state;
	//	do { m_media_control->Stop(); m_media_control->GetState(0, &state); }
	//	while (state != State_Stopped);
	//}
	//
	//// Note: the video is not responsible for the render target texture
	//m_alloc_pres     = 0;
	//m_media_position = 0;
	//m_media_event    = 0;
	//m_media_control  = 0;
	//m_video_window   = 0;
}

// Return the width/height of the video
pr::iv2 pr::rdr::Video::GetNativeResolution()
{
//	return pr::iv2::make(m_alloc_pres->m_native_res.cx, m_alloc_pres->m_native_res.cy);
	return pr::iv2Zero;
}

// Play the video
void pr::rdr::Video::Play(bool loop)
{
	(void)loop;
	//// If the filter graph is stopped, this method pauses the graph before running.
	//// If the graph is already running, the method returns S_OK but has no effect.
	//// The graph runs until the application calls the IMediaControl::Pause or
	//// IMediaControl::Stop method. When playback reaches the end of the stream,
	//// the graph continues to run, but the filters do not stream any more data.
	//// At that point, the application can pause or stop the graph. For information
	//// about the end-of-stream event, see IMediaControl::Pause and EC_COMPLETE.
	//// This method does not seek to the beginning of the stream. Therefore, if
	//// you run the graph, pause it, and then run it again, playback resumes from
	//// the paused position. If you run the graph after it has reached the end of
	//// the stream, nothing is rendered. To seek the graph, use the IMediaSeeking interface.
	//// If the return value is S_FALSE, you can wait for the transition to complete
	//// by calling the IMediaControl::GetState method. If the method fails, some
	//// filters might be in a running state. In a multistream graph, entire streams
	//// might be playing successfully. The application must determine whether to stop the graph
	//m_loop = loop;
	//pr::Throw(m_media_control->Run());
}

// Pause the video.
void pr::rdr::Video::Pause()
{
	//// Pausing the filter graph cues the graph for immediate rendering when the
	//// graph is next run. While the graph is paused, filters process data but do
	//// not render it. Data is pushed through the graph and processed by transform
	//// filters as far as buffering permits, but renderer filters do not render the
	//// data. However, video renderers display a static poster frame of the first sample.
	//// If the method returns S_FALSE, call the IMediaControl::GetState method to wait for the
	//// state transition to complete, or to check if the transition has completed.
	//// When you call Pause to display the first frame of a video file, always follow it
	//// immediately with a call to GetState to ensure that the state transition has completed.
	//// Failure to do this can result in the video rectangle being painted black.
	//// If the method fails, it stops the graph before returning.
	//pr::Throw(m_media_control->Pause());
}

// Stop the video.
void pr::rdr::Video::Stop()
{
	//// If the graph is running, this method pauses the graph before stopping it.
	//// While paused, video renderers can copy the current frame to display as a poster frame.
	//// This method does not seek to the beginning of the stream. If you call this method and
	//// then call the IMediaControl::Run method, playback resumes from the stopped position.
	//// To seek, use the IMediaSeeking interface.
	//// The Filter Graph Manager pauses all the filters in the graph, and then calls the
	//// IMediaFilter::Stop method on all filters, without waiting for the pause operations
	//// to complete. Therefore, some filters might have their Stop method called before they
	//// complete their pause operation. If you develop a custom rendering filter, you might
	//// need to handle this case by pausing the filter if it receives a stop command while
	//// in a running state. However, most filters do not need to take any special action in this regard
	//pr::Throw(m_media_control->Stop());
}

// Refcounting cleanup function
void pr::rdr::Video::RefCountZero(pr::RefCount<Video>* doomed)
{
	pr::rdr::Video* vid = static_cast<pr::rdr::Video*>(doomed);
	delete vid;//vid->m_mat_mgr->DeleteVideoTexture(vid);
}

//
//// Returns true if the currently loaded file has a video stream
//bool pr::rdr::Video::HasVideo() const
//{
//	D3DPtr<IVideoWindow> video_window;
//	return Succeeded(m_graph_builder->QueryInterface(IID_IVideoWindow ,(void**)&video_window.m_ptr));
//}
//
//// Attach this video component to a window for playback
//void pr::rdr::Video::AttachToWindow(HWND hwnd)
//{
//	D3DPtr<IVideoWindow> video_window;
//	if (Failed(m_graph_builder->QueryInterface(IID_IVideoWindow ,(void**)&video_window.m_ptr)) && video_window != 0)
//		return;
//	
//	if (hwnd != 0)
//	{
//		pr::Throw(video_window->put_Owner(OAHWND(hwnd)));                                    // Parent the window
//		pr::Throw(video_window->put_WindowStyle(WS_CHILD|WS_CLIPCHILDREN|WS_CLIPSIBLINGS));  // Set the window style
//		pr::Throw(video_window->put_MessageDrain(OAHWND(hwnd)));                             // Set the destination for messages
//	}
//	else
//	{
//		pr::Throw(video_window->put_Visible(FALSE));             // Make the window invisible
//		pr::Throw(video_window->put_MessageDrain(OAHWND(hwnd))); // Set the destination for messages
//		pr::Throw(video_window->put_Owner(0));                   // Unparent it
//	}
//}



		//// A custom allocator/presenter for rendering video to a d3d texture
		//struct AllocPres :IVMRSurfaceAllocator9 ,IVMRImagePresenter9 ,pr::RefCount<AllocPres>
		//{
		//	typedef std::vector< D3DPtr<IDirect3DSurface9> > SurfCont;
		//	
		//	D3DPtr<IDirect3DDevice9>            m_d3d_device;              // The d3d device
		//	D3DPtr<IDirect3DSurface9>           m_rdr_target;              // With VMR9 in mixing mode, the render target is changed and not restored. Keep this pointer so we can restore it
		//	D3DPtr<IVMRSurfaceAllocatorNotify9> m_surf_alloc_notify;
		//	pr::rdr::Video&                     m_video;                   //
		//	pr::threads::CritSection            m_cs;              
		//	SurfCont                            m_surfaces;
		//	bool                                m_local_tex;               // True if we created a local texture for the video output
		//	
		//	DWORD_PTR UserId() const { return DWORD_PTR(this); }
		//	
		//	AllocPres(Video& video, D3DPtr<IDirect3DDevice9> d3d_device)
		//	:m_d3d_device(d3d_device)
		//	,m_rdr_target()
		//	,m_surf_alloc_notify()
		//	,m_video(video)
		//	,m_cs()
		//	,m_surfaces()
		//	,m_local_tex(false)
		//	{
		//		pr::threads::CSLock lock(m_cs);
		//
		//		#if PR_DBG_RDR
		//		D3DDEVICE_CREATION_PARAMETERS params;
		//		pr::Throw(m_d3d_device->GetCreationParameters(&params));
		//		PR_ASSERT(PR_DBG_RDR, params.BehaviorFlags & D3DCREATE_MULTITHREADED, "the d3d device must be thread safe");
		//		#endif
		//
		//		// Get a pointer to the current render target because the VMR9 changes it without restoring it
		//		pr::Throw(m_d3d_device->GetRenderTarget(0, &m_rdr_target.m_ptr));
		//	}
		//	~AllocPres()
		//	{
		//		DeleteSurfaces();
		//	}
	
		//	// IVMRSurfaceAllocator9
		//	// The InitializeDevice method is called by the Video Mixing Renderer 9 (VMR-9)
		//	// when it needs the allocator-presenter to allocate surfaces.
		//	HRESULT STDMETHODCALLTYPE InitializeDevice(IN DWORD_PTR dwUserID, IN VMR9AllocationInfo* lpAllocInfo, IN OUT DWORD *lpNumBuffers)
		//	{
		//		if (lpNumBuffers == 0) return E_POINTER;
		//		if (*lpNumBuffers == 0) return S_OK;
		//		if (m_surf_alloc_notify == 0) return E_FAIL;
		//
		//		TexInfo info;
		//
		//		// Record the requested width/height rather than the pow2 sizes
		//		info.Width  = lpAllocInfo->dwWidth;
		//		info.Height = lpAllocInfo->dwHeight;
		//
		//		// If the device only supports pow2 textures, find the texture size to allocate
		//		D3DCAPS9 d3dcaps;
		//		pr::Throw(m_d3d_device->GetDeviceCaps(&d3dcaps));
		//		if (d3dcaps.TextureCaps & D3DPTEXTURECAPS_POW2)
		//		{
		//			// todo, test this
		//			DWORD w = 1; while (w < lpAllocInfo->dwWidth ) w <<= 1;
		//			DWORD h = 1; while (w < lpAllocInfo->dwHeight) h <<= 1;
		//			lpAllocInfo->dwWidth  = w;
		//			lpAllocInfo->dwHeight = h;
		//			//float fTU = 1.f;
		//			//float fTV = 1.f;
		//			//fTU = (float)(lpAllocInfo->dwWidth) / (float)(dwWidth);
		//			//fTV = (float)(lpAllocInfo->dwHeight) / (float)(dwHeight);
		//			//m_scene.SetSrcRect( fTU, fTV );
		//		}
		//
		//		// We want the VMR9 to create textures because surfaces can not be textured onto a primitive.
		//		// However, some devices can't do this, in which case we create our own texture and blt from the surface to it
		//		lpAllocInfo->dwFlags |= VMR9AllocFlag_TextureSurface;
		//
		//		D3DPtr<IDirect3DTexture9> tex;
		//
		//		// Create the surfaces using the helper method
		//		DeleteSurfaces();
		//		m_surfaces.resize(*lpNumBuffers);
		//		HRESULT hr = m_surf_alloc_notify->AllocateSurfaceHelper(lpAllocInfo, lpNumBuffers, reinterpret_cast<IDirect3DSurface9**>(&m_surfaces[0]));
		//		if (pr::Succeeded(hr))
		//		{
		//			pr::Throw(m_surfaces[0]->GetContainer(__uuidof(IDirect3DTexture9), (void**)&tex.m_ptr));
		//			info.Depth           = 1;
		//			info.MipLevels       = tex->GetLevelCount();
		//			info.Format          = D3DFMT_UNKNOWN;
		//			info.ImageFileFormat = D3DXIFF_FORCE_DWORD;
		//			info.ResourceType    = tex->GetType();
		//			m_local_tex = false;
		//		}
		//
		//		// If we couldn't create a texture surface and the format is not an alpha format,
		//		// then we probably cannot create a texture. So we create a local texture and copy the decoded images onto it.
		//		else if (pr::Failed(hr) && !(lpAllocInfo->dwFlags & VMR9AllocFlag_3DRenderTarget))
		//		{
		//			DeleteSurfaces();

		//			// is surface YUV ?
		//			if (lpAllocInfo->Format > '0000')//0x30303030
		//			{
		//				// Create the private texture
		//				D3DDISPLAYMODE dm; pr::Throw(m_d3d_device->GetDisplayMode(0, &dm));
		//				//todo: m_video.Create(m_d3d_device, pr::rdr::AutoId, 0, 0, lpAllocInfo->dwWidth, lpAllocInfo->dwHeight, 1, D3DUSAGE_RENDERTARGET, dm.Format, D3DPOOL_DEFAULT);
		//				m_local_tex = true;
		//			}
		//			lpAllocInfo->dwFlags &= ~VMR9AllocFlag_TextureSurface;
		//			lpAllocInfo->dwFlags |= VMR9AllocFlag_OffscreenSurface;
		//	
		//			m_surfaces.resize(*lpNumBuffers);
		//			pr::Throw(m_surf_alloc_notify->AllocateSurfaceHelper(lpAllocInfo, lpNumBuffers, reinterpret_cast<IDirect3DSurface9**>(&m_surfaces[0])));
		//		}
		//		return S_OK;
		//	}
	
		//	// The TerminateDevice method releases the Direct3D device.
		//	virtual HRESULT STDMETHODCALLTYPE TerminateDevice(IN DWORD_PTR dwID)
		//	{
		//		DeleteSurfaces();
		//		return S_OK;
		//	}
	
		//	// The GetSurface method gets a Direct3D surface from the allocator-presenter.
		//	virtual HRESULT STDMETHODCALLTYPE GetSurface(IN DWORD_PTR dwUserID, IN DWORD SurfaceIndex, IN DWORD SurfaceFlags, OUT IDirect3DSurface9 **lplpSurface)
		//	{
		//		if (lplpSurface == 0) return E_POINTER;
		//		if (SurfaceIndex >= m_surfaces.size()) return E_FAIL;

		//		pr::threads::CSLock lock(m_cs);
		//		(*lplpSurface) = m_surfaces[SurfaceIndex].m_ptr;
		//		(*lplpSurface)->AddRef();
		//		return S_OK;
		//	}
	
		//	// The AdviseNotify method provides the allocator-presenter with the VMR-9 filter's interface
		//	// for notification callbacks. If you are using a custom allocator-presenter, the application
		//	// must call this method on the allocator-presenter, with a pointer to the VMR's IVMRSurfaceAllocatorNotify9
		//	// interface. The allocator-presenter uses this interface to communicate with the VMR.
		//	// If you are not using a custom allocator-presenter, the application does not have to call this method.
		//	virtual HRESULT STDMETHODCALLTYPE AdviseNotify(IN IVMRSurfaceAllocatorNotify9 *lpIVMRSurfAllocNotify)
		//	{
		//		pr::threads::CSLock lock(m_cs);
		//		m_surf_alloc_notify = lpIVMRSurfAllocNotify;
		//
		//		HRESULT hr;
		//		if (pr::Failed(hr = m_surf_alloc_notify->SetD3DDevice(m_d3d_device.m_ptr, Monitor())))
		//		{
		//			// This throws E_NOINTERFACE if you use the debug d3d runtimes... fan-fk'ing-tastic...
		//			if (hr == E_NOINTERFACE) throw std::exception("IVMRSurfaceAllocatorNotify9::SetD3DDevice returned E_NOINTERFACE. This happens when the debug d3d runtime dlls are used");
		//			pr::Throw(hr);
		//		}
		//		return S_OK;
		//	}
	
		//	// IVMRImagePresenter9
		//	// The PresentImage method is called at precisely the moment this video frame should be presented.
		//	// PresentImage can be called when the filter is in either a running or a paused state.
		//	// StartPresenting and StopPresenting can be called only in a running state. Therefore,
		//	// if the graph is paused before it is run, PresentImage will be called before StartPresenting.
		//	// Applications can create custom blending effects by using a single instance of an
		//	// allocator-presenter with multiple instances of the VMR either in a single filter graph
		//	// or in multiple filter graphs. Using the allocator presenter in this way enables applications
		//	// to blend streams from different filter graphs, or blend different streams within the same
		//	// filter graph. If you are using a single instance of the VMR, set this value to zero.
		//	virtual HRESULT STDMETHODCALLTYPE PresentImage(IN DWORD_PTR dwUserID, IN VMR9PresentationInfo *lpPresInfo)
		//	{
		//		if (lpPresInfo == 0 || lpPresInfo->lpSurf == 0)
		//			return E_POINTER;
		//
		//		pr::threads::CSLock lock(m_cs);
		//
		//		try
		//		{
		//			//// if we are in the middle of the display change
		//			//bool NeedToHandleDisplayChange()
		//			//{
		//			//	if (!m_surf_alloc_notify) return false;

		//			//	D3DDEVICE_CREATION_PARAMETERS Parameters;
		//			//	if (FAILED(m_video.m_d3d_device->GetCreationParameters(&Parameters)))
		//			//	{
		//			//		ASSERT(false);
		//			//		return false;
		//			//	}
		//			//	return d3d->GetAdapterMonitor(D3DADAPTER_DEFAULT) != d3d->GetAdapterMonitor(params.AdapterOrdinal);
		//			//}
		//			//if( NeedToHandleDisplayChange() )
		//			//{
		//			//	// NOTE: this piece of code is left as a user exercise.  
		//			//	// The D3DDevice here needs to be switched
		//			//	// to the device that is using another adapter
		//			//}
		//	
		//			// Restore the render target, since the VMR9 will have changed it.
		//			m_d3d_device->SetRenderTarget(0, m_rdr_target.m_ptr);
		//	
		//			if (m_video.m_tex)
		//			{
		//				// Copy the full surface onto the texture's surface
		//				D3DPtr<IDirect3DSurface9> surface = m_video.m_tex->surf(0);
		//				pr::Throw(m_d3d_device->StretchRect(lpPresInfo->lpSurf, 0, surface.m_ptr, 0, D3DTEXF_NONE));
		//			}
		//			// We have got the textures allocated by VMR, all we need to do is to get them from the surface
		//			else if (!m_local_tex)
		//			{
		//				//todo: m_video.m_texture = 0;
		//				//pr::Throw(lpPresInfo->lpSurf->GetContainer(__uuidof(IDirect3DTexture9), (void**)&m_video.m_texture.m_ptr));
		//			}
		//			// We're using a local texture, blt the video output surface to our texture
		//			else
		//			{
		//				// Copy the full surface onto the texture's surface
		//				//D3DPtr<IDirect3DSurface9> surface = m_video.Surface(0);
		//				//pr::Throw(m_d3d_device->StretchRect(lpPresInfo->lpSurf, 0, surface.m_ptr, 0, D3DTEXF_NONE));
		//			}


		//			//if (m_frame_ready_cb)
		//			//	m_frame_ready_cb(
		//			//if (pr::Failed(hr = m_video.m_d3d_device->Present(0,0,0,0))) break;
		//			return S_OK;
		//		}
		//		catch (pr::Exception<HRESULT> const& e)
		//		{
		//			PR_ASSERT(1, false, "not implemented");
		//			// IMPORTANT: device can be lost when user changes the resolution
		//			// or presses Ctrl + Alt + Delete. We need to restore our video memory after that
		//			if (e.code() == D3DERR_DEVICELOST)
		//			{
		//				if (m_d3d_device->TestCooperativeLevel() == D3DERR_DEVICENOTRESET) 
		//				{
		//					DeleteSurfaces();
		//					//todo
		//					//FAIL_RET( CreateDevice() );
		//					//HMONITOR hMonitor = m_D3D->GetAdapterMonitor( D3DADAPTER_DEFAULT );
		//					//FAIL_RET( m_surf_alloc_notify->ChangeD3DDevice( m_video.m_d3d_device, hMonitor ) );
		//				}
		//				return S_OK;
		//			}
		//			throw;
		//		}
		//	}
	
		//	// The StartPresenting method is called just before the video starts playing.
		//	// The allocator-presenter should perform any necessary configuration in this method.
		//	HRESULT STDMETHODCALLTYPE StartPresenting(IN DWORD_PTR dwUserID)
		//	{
		//		PR_ASSERT(PR_DBG_RDR, m_d3d_device, "no valid d3d device");
		//		return S_OK;
		//	}
	
		//	// The StopPresenting method is called just after the video stops playing.
		//	// The allocator-presenter should perform any necessary cleanup in this method.
		//	HRESULT STDMETHODCALLTYPE StopPresenting(IN DWORD_PTR dwUserID)
		//	{
		//		return S_OK;
		//	}
		//	
		//	// IUnknown
		//	ULONG STDMETHODCALLTYPE   AddRef()  { return (ULONG)pr::RefCount<AllocPres, true>::AddRef(); }
		//	ULONG STDMETHODCALLTYPE   Release() { return (ULONG)pr::RefCount<AllocPres, true>::Release(); }
		//	HRESULT STDMETHODCALLTYPE QueryInterface(REFIID riid, void** ppvObject)
		//	{
		//		if (ppvObject == 0) return E_POINTER;
		//		if (riid == IID_IVMRSurfaceAllocator9) { *ppvObject = static_cast<IVMRSurfaceAllocator9*>(this); AddRef(); return S_OK; }
		//		if (riid == IID_IVMRImagePresenter9)   { *ppvObject = static_cast<IVMRImagePresenter9*>(this); AddRef(); return S_OK; }
		//		if (riid == IID_IUnknown)              { *ppvObject = static_cast<IUnknown*>(static_cast<IVMRSurfaceAllocator9*>(this)); AddRef(); return S_OK; }
		//		return E_NOINTERFACE;
		//	}
	
		//protected:
		//	AllocPres(AllocPres const&); // no copying
		//	AllocPres& operator = (AllocPres const&);
		//	
		//	// Release all surfaces
		//	void DeleteSurfaces()
		//	{
		//		pr::threads::CSLock lock(m_cs);
		//		m_surfaces.resize(0);
		//	}
		//	
		//	// Return the monitor associated with the device
		//	HMONITOR Monitor() const
		//	{
		//		D3DPtr<IDirect3D9> d3d;
		//		D3DDEVICE_CREATION_PARAMETERS params;
		//		pr::Throw(m_d3d_device->GetCreationParameters(&params));
		//		pr::Throw(m_d3d_device->GetDirect3D(&d3d.m_ptr));
		//		return d3d->GetAdapterMonitor(params.AdapterOrdinal);
		//	}
		//};
