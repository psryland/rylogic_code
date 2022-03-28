//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************

module;

#include "src/forward.h"

export module View3d:Settings;
import :Utility;
import :Config;
import :Wrappers;

namespace pr::rdr12
{
	// Debug features
	export enum EDebugLayer
	{
		None = 0,
		D2D1_DebugInfo = 1 << 4,
	};

	// Settings for constructing the renderer
	export struct Settings
	{
		struct BuildOptions
		{
			StdBuildOptions m_std;
			MathsBuildOptions m_maths;
			int RunTimeShaders;
			BuildOptions()
				:m_std()
				,m_maths()
				,RunTimeShaders(PR_RDR_RUNTIME_SHADERS)
			{}
		};

		HINSTANCE             m_instance;      // Executable instance 
		BuildOptions          m_build_options; // The state of #defines. Used to check for incompatibilities
		D3D_FEATURE_LEVEL     m_feature_level; // Features to support.
		D3DPtr<IDXGIAdapter1> m_adapter;       // The adapter to use. nullptr means use the default. See 'SystemConfig'
		D3DPtr<IDXGIOutput>   m_output;        // The display mode to use
		DisplayMode           m_display_mode;  // The resolution/refresh rate to run at.
		EDebugLayer           m_debug_layers;  // Add layers over the basic device (see D3D11_CREATE_DEVICE_FLAG)

		// Keep this inline so that m_build_options can be verified.
		Settings(HINSTANCE inst, D3D_FEATURE_LEVEL feature_level = D3D_FEATURE_LEVEL_11_0, EDebugLayer debug_layers = EDebugLayer::None)
			:m_instance(inst)
			,m_build_options()
			,m_feature_level(feature_level)
			,m_adapter()
			,m_output()
			,m_display_mode()
			,m_debug_layers(debug_layers)
		{}
	};
}

