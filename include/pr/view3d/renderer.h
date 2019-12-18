//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
// Required libs:
//  d3d11.lib
//  dxgi.lib
//
// Feature Wish list:
// Ambient occlusion
//  - use the G-Buffer to sample normals in a spherical volume, scale
//    intensity based on average "up-ness"
// Thick Lines
//  - Use a geometry shader to support thick lines by turning a line list
//    into a tri strip
// Shadow Mapping
//  - Use 'Rylo-Shadows'
// Order Independent Alpha
//  - Try that thing you read about weighted alpha by screen depth

#pragma once

// To use runtime shaders:
//  - Set PR_RDR_RUNTIME_SHADERS=1 in the preprocessor defines.
//  - Rebuild
//  - Edit the HLSL files in '\projects\renderer11\shaders\hlsl'
//  - Run '\script\BuildShader.py <hlsl_filepath> x86 debug dbg' to build the HLSL file (with .cso files)
//  - Make sure the 'BuildShader.py' script is using the same version of 'fxc.exe' as VS.
//  - Note: Runtime shaders are hard coded to read from '\projects\renderer\shaders\hlsl\compiled\'
//  - Put a break point in '\projects\renderer11\shaders\shader.cpp:66' to ensure the compiled shader is being loaded
//  - Tips:
//      - Use Notepad++ and the NppExec->Execute plugin. Command: "py.exe P:\pr\script\BuildShader.py $(FULL_CURRENT_PATH) x86 debug dbg"  (use x86 or x64 depending on configuration being run)
//      - Check the Output window for 'Shader <myshader.cso> replaced'
//      - Use 'Start Graphics Debugging' -> Capture a frame -> Select the frame to launch VSGA -> Select the DrawIndexedPrimitive call -> then the green 'play' button
//      - Make sure fxc is run with the /Zi option to add debug symbols
//      - Add *Dependency "file.hlsl" to the LDraw script for testing the shader

// Set this in the project settings, not here
#ifndef PR_RDR_RUNTIME_SHADERS
#define PR_RDR_RUNTIME_SHADERS 0
#endif

// Forward
#include "pr/view3d/forward.h"

// Configuration
#include "pr/view3d/config/config.h"

// Render
#include "pr/view3d/render/renderer.h"
#include "pr/view3d/render/window.h"
#include "pr/view3d/render/scene.h"
#include "pr/view3d/render/state_block.h"
#include "pr/view3d/render/sortkey.h"

// Render Steps
#include "pr/view3d/steps/render_step.h"
#include "pr/view3d/steps/forward_render.h"
#include "pr/view3d/steps/gbuffer.h"
#include "pr/view3d/steps/dslighting.h"
#include "pr/view3d/steps/shadow_map.h"

// Models
#include "pr/view3d/models/model_manager.h"
#include "pr/view3d/models/model_settings.h"
#include "pr/view3d/models/model_buffer.h"
#include "pr/view3d/models/model.h"
#include "pr/view3d/models/nugget.h"
#include "pr/view3d/models/model_generator.h"

// Instances
#include "pr/view3d/instances/instance.h"

// Shaders
#include "pr/view3d/shaders/input_layout.h"
#include "pr/view3d/shaders/shader_manager.h"
#include "pr/view3d/shaders/shader.h"
#include "pr/view3d/shaders/shdr_fwd.h"
#include "pr/view3d/shaders/shdr_screen_space.h"

// Textures
#include "pr/view3d/textures/texture_manager.h"
#include "pr/view3d/textures/texture_base.h"
#include "pr/view3d/textures/texture_2d.h"
#include "pr/view3d/textures/texture_cube.h"
//#include "pr/view3d/textures/video.h"

// Lighting
#include "pr/view3d/lights/light.h"

// Utility
#include "pr/view3d/util/allocator.h"
#include "pr/view3d/util/wrappers.h"
#include "pr/view3d/util/lock.h"
#include "pr/view3d/util/util.h"
#include "pr/view3d/util/stock_resources.h"
#include "pr/view3d/util/event_args.h"

#undef PR_RENDERER_INTERFACE_INCLUDE
