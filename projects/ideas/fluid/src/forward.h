// Fluid
#pragma once

#include <cstdint>
#include <span>
#include <vector>

#include "pr/maths/maths.h"
#include "pr/container/vector.h"
#include "pr/container/kdtree.h"

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

namespace pr::fluid
{
	struct Particle;
	struct Particles;
	struct FluidSimulation;
	struct FluidVisualisation;
}
