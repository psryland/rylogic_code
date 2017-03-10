//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#pragma once

#include "pr/renderer11/forward.h"
#include "pr/renderer11/config/config.h"
#include "pr/renderer11/models/model_manager.h"
#include "pr/renderer11/shaders/shader_manager.h"
#include "pr/renderer11/textures/texture_manager.h"
#include "pr/renderer11/render/blend_state.h"
#include "pr/renderer11/render/depth_state.h"
#include "pr/renderer11/render/raster_state.h"
#include "pr/renderer11/util/allocator.h"

namespace pr
{
	namespace rdr
	{
		// Settings for constructing the renderer
		struct RdrSettings
		{
			struct BuildOptions
			{
				StdBuildOptions   m_std;
				MathsBuildOptions m_maths;
				int RunTimeShaders;
				BuildOptions() :m_std() ,m_maths() ,RunTimeShaders(PR_RDR_RUNTIME_SHADERS) {}
			};
			
			BuildOptions                  m_build_options;         // The state of #defines. Used to check for incompatibilities
			MemFuncs                      m_mem;                   // The manager of allocations/deallocations
			D3DPtr<IDXGIAdapter>          m_adapter;               // The adapter to use. nullptr means use the default
			D3D_DRIVER_TYPE               m_driver_type;           // HAL, REF, etc
			UINT                          m_device_layers;         // Add layers over the basic device (see D3D11_CREATE_DEVICE_FLAG)
			pr::vector<D3D_FEATURE_LEVEL> m_feature_levels;        // Features to support. Empty implies 9.1 -> 11.0
			bool                          m_fallback_to_sw_device; // True to use a software device if 'm_driver_type' fails

			// Keep this inline so that m_build_options can be verified.
			RdrSettings(BOOL gdi_compat)
				:m_build_options()
				,m_mem()
				,m_adapter()
				,m_driver_type(D3D_DRIVER_TYPE_HARDWARE)
				,m_device_layers(gdi_compat ? D3D11_CREATE_DEVICE_BGRA_SUPPORT : 0)
				,m_feature_levels()
				,m_fallback_to_sw_device(true)
			{
				// Add the debug layer in debug mode
				// Note: this automatically disables multi-sampling as well
				//PR_EXPAND(PR_DBG_RDR, m_device_layers |= D3D11_CREATE_DEVICE_DEBUG);
				//#pragma message(PR_LINK "WARNING: ************************************************** D3D11_CREATE_DEVICE_DEBUG enabled")
			}
		};

		// Renderer state variables
		struct RdrState
		{
			RdrSettings                    m_settings;
			D3D_FEATURE_LEVEL              m_feature_level;
			D3DPtr<ID3D11Device>           m_device;
			D3DPtr<ID3D11DeviceContext>    m_immediate;
			D3DPtr<ID2D1Factory>           m_d2dfactory;

			RdrState(RdrSettings const& settings);
			~RdrState();
		};
	}

	// The main renderer object
	class Renderer :rdr::RdrState
	{
		using TaskQueue = pr::vector<std::future<void>>;
		std::thread::id m_main_thread_id;
		std::mutex m_mutex_task_queue;
		TaskQueue m_task_queue;

	public:
		// These manager classes form part of the public interface of the renderer
		rdr::ModelManager       m_mdl_mgr;
		rdr::ShaderManager      m_shdr_mgr;
		rdr::TextureManager     m_tex_mgr;
		rdr::BlendStateManager  m_bs_mgr;
		rdr::DepthStateManager  m_ds_mgr;
		rdr::RasterStateManager m_rs_mgr;

		Renderer(rdr::RdrSettings const& settings);
		~Renderer();

		// Return the dx device
		D3DPtr<ID3D11Device> Device() const
		{
			return m_device;
		}

		// Return the immediate device context
		D3DPtr<ID3D11DeviceContext> ImmediateDC() const
		{
			return m_immediate;
		}

		// Create a new deferred device context
		D3DPtr<ID3D11DeviceContext> DeferredDC() const
		{
			return nullptr;
		}

		// Return the direct2d factory
		D3DPtr<ID2D1Factory> D2DFactory() const
		{
			return m_d2dfactory;
		}

		// Returns an allocator object suitable for allocating instances of 'T'
		template <class Type> rdr::Allocator<Type> Allocator() const
		{
			return rdr::Allocator<Type>(m_settings.m_mem);
		}

		// Read access to the initialisation settings
		rdr::RdrSettings const& Settings() const
		{
			return m_settings;
		}

		// Run the given function on the Main/GUI thread
		// 'policy = std::launch::deferred' means the function is executed by the main thread during 'RunTasks'
		// 'policy = std::launch::async' means the function is run at any time in a worker thread. The result is collected in 'RunTasks'
		// 'policy' can be a bitwise OR of both deferred and async
		template <typename Func, typename... Args> inline void RunOnMainThread(std::launch policy, Func&& func, Args&&... args)
		{
			std::lock_guard<std::mutex> lock(m_mutex_task_queue);
			m_task_queue.emplace_back(std::async(policy, func, args...));
		}
		template <typename Func, typename... Args> inline void RunOnMainThread(Func&& func, Args&&... args)
		{
			RunOnMainThread(std::launch::deferred, func, args...);
		}

		// Execute any pending tasks in the task queue. Must be called from the Main/GUI thread
		void RunTasks();
	};
}
