//**********************************************
// P3D Graphics Tool
//  Copyright (c) Rylogic Ltd 2019
//**********************************************

#include "p3d/src/forward.h"

using namespace pr;
using namespace pr::geometry;

// Remove degenerate verts
void RemoveDegenerateVerts(p3d::Mesh& mesh, int quantisation, float smoothing_angle, float colour_distance, float uv_distance, int verbosity)
{
	// No mesh, no degenerates
	if (mesh.m_verts.empty())
		return;

	if (verbosity >= 2)
		std::cout << "  Removing degenerate verts for mesh: " << mesh.m_name << std::endl;
	if (verbosity >= 3 && smoothing_angle >= 0)
		std::cout << "    Vert normals within " << smoothing_angle << " degrees are considered degenerate" << std::endl;
	if (verbosity >= 3 && colour_distance >= 0)
		std::cout << "    Vert colours within " << colour_distance << " are considered degenerate" << std::endl;
	if (verbosity >= 3 && uv_distance >= 0)
		std::cout << "    Vert texture coords within " << uv_distance << " are considered degenerate" << std::endl;

	// Quantise the verts.
	for (auto& vert : mesh.m_verts)
		vert.pos = pr::Quantise(static_cast<pr::v4>(vert.pos), 1 << quantisation);

	// A mapping from original vertex (orig) to kept vertex from all the verts degenerate with 'orig'
	struct VP { p3d::Vert* orig; p3d::Vert* kept; };
	std::vector<VP> map;
	map.resize(mesh.m_verts.size());

	// Initialise the map
	auto map_ptr = std::begin(map);
	for (auto& vert : mesh.m_verts)
	{
		map_ptr->orig = &vert;
		map_ptr->kept = &vert;
		++map_ptr;
	}

	// Sort the mapping vector such that degenerate verts are next to each other
	pr::sort(map, [&](VP const& lhs, VP const& rhs)
	{
		auto& v0 = *lhs.orig;
		auto& v1 = *rhs.orig;
		if (v0.pos.x != v1.pos.x) return v0.pos.x < v1.pos.x;
		if (v0.pos.y != v1.pos.y) return v0.pos.y < v1.pos.y;
		if (v0.pos.z != v1.pos.z) return v0.pos.z < v1.pos.z;
		return false;
	});

	// Preserved vertex properties
	auto cos_angle_normal_diff = smoothing_angle >= 0 ? Cos(DegreesToRadians(smoothing_angle)) : 0;
	auto colour_dist_sq = colour_distance >= 0 ? Sqr(colour_distance) : 0;
	auto uv_dist_sq = uv_distance >= 0 ? Sqr(uv_distance) : 0;

	// Set each 'kept' pointer to the first in the set of verts degenerate with 'orig'
	// Since the verts are only sorted by position, all verts need to be checked back to where the positions changes.
	auto unique_count = map.size();
	for (size_t i = 1, iend = map.size(); i != iend; ++i)
	{
		// The vert to test for degeneracy
		auto& vi = *map[i].orig;
		for (auto j = i; j-- != 0;)
		{
			// A previous vertex that may be degenerate with 'vi' and, if so,
			// will already have it's 'kept' pointer pointing at the first in the set.
			auto& vj = *map[j].kept;

			// If the vertex position is different, move on to the next vert
			if (vi.pos != vj.pos)
				break;

			// Not a degenerate if the normals don't match, but keep searching
			if (smoothing_angle >= 0 && Dot((v4)vi.norm, (v4)vj.norm) <= cos_angle_normal_diff)
				continue;

			// Not a degenerate if the colours don't match, but keep searching
			if (colour_distance >= 0 && LengthSq((v4)vi.col - (v4)vj.col) >= colour_dist_sq)
				continue;

			// Not a degenerate if the UVs don't match, but keep searching
			if (uv_distance >= 0 && LengthSq((v2)vi.uv - (v2)vj.uv) >= uv_dist_sq)
				continue;

			// 'vi' is degenerate with 'vj'.
			// Set the 'kept' pointer for 'vi' to the same as 'vj' which will
			// be the first in the set of all verts degenerate with 'vi' and 'vj'.
			map[i].kept = map[j].kept;
			--unique_count;
			break;
		}
	}

	// Report stats
	if (verbosity >= 3)
		std::cout
			<< "    " << (map.size() - unique_count) << " degenerate verts found\n"
			<< "    " << unique_count << " verts remaining." << std::endl;

	// Returns true if 'ptr' points within 'cont'
	auto is_within = [](std::vector<p3d::Vert> const& cont, p3d::Vert const* ptr)
	{
		return ptr >= cont.data() && ptr < cont.data() + cont.size();
	};

	// Create a collection of verts without degenerates
	std::vector<p3d::Vert> verts(unique_count);
	auto vout = std::begin(verts);
	for (size_t i = 0, j = 0, iend = map.size(); i != iend; ++i)
	{
		// If this vert is already pointing into the new container then it's
		// degenerate with a vert that has already been copied into 'verts'
		if (is_within(verts, map[i].kept))
			continue;

		// 'map[i]' should be the first in the next set of degenerate verts.
		auto norm = static_cast<v4>(map[i].orig->norm);
		auto col = static_cast<v4>(map[i].orig->col);
		auto uv = static_cast<v2>(map[i].orig->uv);

		// Redirect all pointers up to the next non-degenerate vert to the new container.
		// Test all verts after 'i' until the positions differ. The preserved fields mean that not all degenerate verts are adjacent in 'map'
		for (j = i + 1; j != iend; ++j)
		{
			// If the next vert has a different position, then we've reached the end of the degenerate verts
			if (map[j].orig->pos != map[i].orig->pos)
				break;

			// 'map[i]' is degenerate with 'map[j]' if their 'kept' pointers are the same.
			if (map[j].kept != map[i].kept)
				continue;

			// Set 'map[j].kept' to the location in 'verts' that will contain the vertex for this degenerate set (done after this for loop)
			map[j].kept = &*vout;

			// Perform averaging of the vertex fields that are being merged.
			if (smoothing_angle >= 0)
				norm += static_cast<v4>(map[j].orig->norm);
			if (colour_distance >= 0)
				col += static_cast<v4>(map[j].orig->col);
			if (uv_distance >= 0)
				uv += static_cast<v2>(map[j].orig->uv);
		}

		// Assign the kept vertex of the degenerate set into the new container
		map[i].kept = &*vout;
		*vout++ = *map[i].orig;

		// Perform averaging of the vertex fields that are being merged.
		auto degen_count = float(j - i);
		if (smoothing_angle >= 0)
			map[i].kept->norm = Normalise(norm);
		if (colour_distance >= 0)
			map[i].kept->col = col / degen_count;
		if (uv_distance >= 0)
			map[i].kept->uv = uv / degen_count;
	}

	// Re-sort the map back to the original order of mesh.m_vert
	pr::sort(map, [&](VP const& lhs, VP const& rhs)
	{
		return lhs.orig < rhs.orig;
	});

	// Update the mesh indices to use the non-degenerate vert indices
	for (auto& idx : mesh.m_idx16)
		idx = pr::checked_cast<p3d::u16>(map[idx].kept - verts.data());
	for (auto& idx : mesh.m_idx32)
		idx = pr::checked_cast<p3d::u32>(map[idx].kept - verts.data());

	// Update the vrange for each nugget
	for (auto& nug : mesh.m_nugget)
	{
		auto vrange = pr::Range<p3d::u32>::Reset();
		for (auto i = nug.m_vrange.first, iend = i + nug.m_vrange.count; i != iend; ++i)
			Encompass(vrange, static_cast<p3d::u32>(map[i].kept - verts.data()));

		nug.m_vrange = vrange;
	}

	// Replace the vert container in the mesh
	mesh.m_verts = std::move(verts);
}

// Remove degenerate verts. Use -1.0f to ignore normals, colours, or UVs
void RemoveDegenerateVerts(p3d::File& p3d, int quantisation, float smoothing_angle, float colour_distance, float uv_distance, int verbosity)
{
	// Apply to each mesh in the scene
	for (auto& mesh : p3d.m_scene.m_meshes)
		RemoveDegenerateVerts(mesh, quantisation, smoothing_angle, colour_distance, uv_distance, verbosity);
}
