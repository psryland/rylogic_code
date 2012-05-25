//*********************************************
// Renderer
//  Copyright © Rylogic Ltd 2007
//*********************************************
// Link libs:
//   renderer.$(Platform).$(Configuration).lib
//   d3d9.lib
//   d3dx9.lib
//   dxerr.lib
//   strmiids.lib - for DirectShow video support
//   wininet.lib
//   xmllite.lib
//   shlwapi.lib
#pragma once
#ifndef PR_RENDERER_H
#define PR_RENDERER_H

#define PR_RENDERER_INTERFACE_INCLUDE 1

#pragma warning (push)
//#pragma warning (disable: 4996)

// Types
#include "pr/renderer/types/forward.h"

// Configuration
#include "pr/renderer/configuration/settings.h"
#include "pr/renderer/configuration/configure.h"
#include "pr/renderer/configuration/projectconfiguration.h"

// Renderer
#include "pr/renderer/renderer/renderer.h"

// Drawlist
#include "pr/renderer/viewport/sortkey.h"
#include "pr/renderer/viewport/viewport.h"
#include "pr/renderer/viewport/drawlist.h"
#include "pr/renderer/viewport/drawlistelement.h"

// Vertex Formats
#include "pr/renderer/vertexformats/vertexformat.h"
#include "pr/renderer/vertexformats/vertexformatmanager.h"

// Models
#include "pr/renderer/models/modelmanager.h"
#include "pr/renderer/models/quadbuffer.h"
#include "pr/renderer/models/modelgenerator.h"

// Materials
#include "pr/renderer/materials/material.h"
#include "pr/renderer/materials/material_manager.h"
#include "pr/renderer/materials/effects/effect.h"
#include "pr/renderer/materials/effects/fragments.h"
#include "pr/renderer/materials/textures/texture.h"
#include "pr/renderer/materials/textures/texturefilter.h"
#include "pr/renderer/materials/video/video.h"

// Lighting
#include "pr/renderer/lighting/light.h"
#include "pr/renderer/lighting/lightingmanager.h"

// Render states
#include "pr/renderer/renderstates/renderstate.h"
#include "pr/renderer/renderstates/stackframes.h"
#include "pr/renderer/renderstates/renderstatemanager.h"

// Instances
#include "pr/renderer/instances/instance.h"

// Utility
#include "pr/renderer/utility/errors.h"
#include "pr/renderer/utility/globalfunctions.h"

#pragma warning (pop)

#undef PR_RENDERER_INTERFACE_INCLUDE
#endif
