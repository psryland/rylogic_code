// Fluid
#pragma once

#include <cstdint>
#include <span>
#include <vector>
#include <stdexcept>
#include <windows.h>

#include "pr/common/cast.h"
#include "pr/common/fmt.h"
#include "pr/common/coalesce.h"
#include "pr/maths/maths.h"
#include "pr/container/vector.h"
#include "pr/container/kdtree.h"
#include "pr/gui/wingui.h"
#include "pr/win32/windows_com.h"

#include "pr/view3d-12/view3d.h"
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/scene/scene.h"
#include "pr/view3d-12/model/model_desc.h"
#include "pr/view3d-12/model/model.h"
#include "pr/view3d-12/model/nugget.h"
#include "pr/view3d-12/model/vertex_layout.h"
#include "pr/view3d-12/model/model_generator.h"
#include "pr/view3d-12/instance/instance.h"
#include "pr/view3d-12/shaders/shader_point_sprites.h"
#include "pr/view3d-12/utility/update_resource.h"
#include "pr/view3d-12/ldraw/ldr_object.h"

namespace pr::fluid
{
	inline static const int Dimensions = 2;

	struct Particle;
	struct Particles;
	struct FluidSimulation;
	struct FluidVisualisation;
}

