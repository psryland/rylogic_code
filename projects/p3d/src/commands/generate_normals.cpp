//**********************************************
// P3D Graphics Tool
//  Copyright (c) Rylogic Ltd 2019
//**********************************************

#include "p3d/src/forward.h"
#include "pr/geometry/utility.h"

using namespace pr;
using namespace pr::geometry;

// Generate normals for 16 or 32 bit indices
template <typename VIdx>
void GenerateVertNormals(p3d::Nugget& nug, float smoothing_angle, p3d::Mesh::VCont& vcont, VIdx* icont)
{
	// Pointer to the first index of this nugget
	auto iptr = &icont[nug.m_irange.first];

	// Reset the vrange in the nugget, since generating normals will create new verts
	auto mn = pr::limits<VIdx>::max();
	auto mx = pr::limits<VIdx>::min();

	// Generate the normals
	GenerateNormals(nug.m_irange.count, iptr, DegreesToRadians(smoothing_angle),
		[&](VIdx idx) { return vcont[idx].pos; }, vcont.size(),
		[&](VIdx new_idx, VIdx orig_idx, v4 const& normal)
		{
			if (new_idx >= vcont.size()) vcont.resize(size_t(new_idx) + 1, vcont[orig_idx]);
			vcont[new_idx].norm = normal;
		},
		[&](VIdx i0, VIdx i1, VIdx i2)
		{
			*iptr++ = i0;
			*iptr++ = i1;
			*iptr++ = i2;
			mn = pr::min(mn, i0, i1, i2);
			mx = pr::max(mx, i0, i1, i2);
		});

	// Reset the vrange in the nugget, since generating normals will create new verts
	nug.m_vrange.first = mn;
	nug.m_vrange.count = mx - mn;
}

// Generate normals for the p3d file
void GenerateVertNormals(p3d::Mesh& mesh, float smoothing_angle, int verbosity)
{
	// No verts, no normals
	if (mesh.m_verts.empty() || mesh.m_idx.empty())
		return;

	if (verbosity >= 2)
		std::cout << "  Generating normals for mesh: " << mesh.m_name << std::endl;

	// Generate normals per nugget since the topology can change per nugget
	for (auto& nug : mesh.m_nugget)
	{
		// Can only generate normals for triangle lists
		if (nug.m_topo != EPrim::TriList)
			continue;

		switch (mesh.m_idx.m_stride)
		{
		case sizeof(uint16_t):
			GenerateVertNormals(nug, smoothing_angle, mesh.m_verts, mesh.m_idx.data<uint16_t>());
			break;
		case sizeof(uint32_t):
			GenerateVertNormals(nug, smoothing_angle, mesh.m_verts, mesh.m_idx.data<uint32_t>());
			break;
		default:
			throw std::runtime_error("Unsupported index format");
		}
	}
}

// Generate normals for the p3d file
void GenerateVertNormals(p3d::File& p3d, float smoothing_angle, int verbosity)
{
	// Generate normals for each mesh
	for (auto& mesh : p3d.m_scene.m_meshes)
		GenerateVertNormals(mesh, smoothing_angle, verbosity);
}
