//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
// Required libs:
//  d3d11.lib
//  dxgi.lib
//
// Feature Wishlist:
// Ambient occlusion
//  - use the G-Buffer to sample normals in a spherical volume, scale
//    intensity based on average "up-ness"
// Thick Lines
//  - Use a geometry shader to support thick lines by turning a line list
//    into a tristrip
// Shadow Mapping
//  - Use Rylo-Shadows
// Order Independent Alpha
//  - Try that thing you read about weighted alpha by screen depth

#pragma once

// To use runtime shaders:
//  - Set PR_RDR_RUNTIME_SHADERS=1 in the preprocessor defines.
//  - Rebuild
//  - Edit the HLSL files in '\projects\renderer11\shaders\hlsl'
//  - Run '\script\BuildShader.py <hlsl_filepath> x86 debug dbg' to build the HLSL file (with .cso files)
//  - Note: Runtime shaders are hard coded to read from '\projects\renderer\shaders\hlsl\compiled\'
//  - Put a break point in '\projects\renderer11\shaders\shader.cpp:66' to ensure the compiled shader is being loaded

#ifndef PR_RDR_RUNTIME_SHADERS
#define PR_RDR_RUNTIME_SHADERS 0
#endif

// Forward
#include "pr/renderer11/forward.h"
#include "pr/renderer11/instance.h"

// Configuration
#include "pr/renderer11/config/config.h"

// Render
#include "pr/renderer11/render/renderer.h"
#include "pr/renderer11/render/window.h"
#include "pr/renderer11/render/scene.h"
#include "pr/renderer11/render/blend_state.h"
#include "pr/renderer11/render/depth_state.h"
#include "pr/renderer11/render/raster_state.h"
#include "pr/renderer11/render/sortkey.h"

// Render Steps
#include "pr/renderer11/steps/render_step.h"
#include "pr/renderer11/steps/forward_render.h"
#include "pr/renderer11/steps/gbuffer.h"
#include "pr/renderer11/steps/dslighting.h"
#include "pr/renderer11/steps/shadow_map.h"

// Models
#include "pr/renderer11/models/model_manager.h"
#include "pr/renderer11/models/model_settings.h"
#include "pr/renderer11/models/model_buffer.h"
#include "pr/renderer11/models/model.h"
#include "pr/renderer11/models/nugget.h"
#include "pr/renderer11/models/model_generator.h"

// Instances
#include "pr/renderer11/instances/instance.h"

// Shaders
#include "pr/renderer11/shaders/input_layout.h"
#include "pr/renderer11/shaders/shader_manager.h"
#include "pr/renderer11/shaders/shader.h"
#include "pr/renderer11/shaders/screen_space_shaders.h"

// Textures
#include "pr/renderer11/textures/texture_manager.h"
#include "pr/renderer11/textures/texture2d.h"
#include "pr/renderer11/textures/texture_gdi.h"
//#include "pr/renderer11/textures/video.h"

// Lighting
#include "pr/renderer11/lights/light.h"

// Utility
#include "pr/renderer11/util/allocator.h"
#include "pr/renderer11/util/wrappers.h"
#include "pr/renderer11/util/lock.h"
#include "pr/renderer11/util/util.h"
#include "pr/renderer11/util/stock_resources.h"
#include "pr/renderer11/util/event_types.h"

#undef PR_RENDERER_INTERFACE_INCLUDE
