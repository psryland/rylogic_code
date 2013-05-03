//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2012
//*********************************************
// Required libs:
//  d3d11.lib
//  dxgi.lib
//
// ToDo:
// Ambient occlusion -  use the G-Buffer to sample normals in a spherical volume, scale
//   intensity based on average "up-ness"
//
// Render method:
//  render opaques with one directional light
//  render skybox
//   =>G-buffer
//     pixels contain: depth, normal, colour (with directional light), lighting option (including none)
//  render light volumes
//  render alpha back faces
//  render alpha front faces

#pragma once
#ifndef PR_RDR_RENDERER_H
#define PR_RDR_RENDERER_H

// Forward
#include "pr/renderer11/forward.h"
#include "pr/renderer11/instance.h"

// Configuration
#include "pr/renderer11/config/config.h"
//#include "pr/renderer/configuration/configure.h"
//#include "pr/renderer/configuration/projectconfiguration.h"

// Renderer
#include "pr/renderer11/render/renderer.h"
#include "pr/renderer11/render/scene.h"
#include "pr/renderer11/render/blend_state.h"
#include "pr/renderer11/render/depth_state.h"
#include "pr/renderer11/render/raster_state.h"
#include "pr/renderer11/render/sortkey.h"

//// Drawlist
//#include "pr/renderer/viewport/viewport.h"
//#include "pr/renderer/viewport/drawlist.h"
//#include "pr/renderer/viewport/drawlist_element.h"

// Models
#include "pr/renderer11/models/model_manager.h"
#include "pr/renderer11/models/model_settings.h"
#include "pr/renderer11/models/model_buffer.h"
#include "pr/renderer11/models/model.h"
#include "pr/renderer11/models/nugget.h"
#include "pr/renderer11/models/input_layout.h"
#include "pr/renderer11/models/model_generator.h"

// Instances
#include "pr/renderer11/instances/instance.h"

// Shaders
#include "pr/renderer11/shaders/shader_manager.h"
#include "pr/renderer11/shaders/shader.h"

// Textures
#include "pr/renderer11/textures/texture_manager.h"
#include "pr/renderer11/textures/texture2d.h"
//#include "pr/renderer11/textures/video.h"

// Lighting
#include "pr/renderer11/lights/light.h"

// Utility
#include "pr/renderer11/util/allocator.h"
#include "pr/renderer11/util/wrappers.h"
#include "pr/renderer11/util/lock.h"

#undef PR_RENDERER_INTERFACE_INCLUDE
#endif
