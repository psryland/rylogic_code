//**********************************************
// P3D Graphics Tool
//  Copyright (c) Rylogic Ltd 2019
//**********************************************

#include "p3d/src/forward.h"
#include "pr/geometry/3ds.h"
#include "pr/geometry/stl.h"

using namespace pr;
using namespace pr::script;
using namespace pr::geometry;

// Populate the p3d data structures from a p3d file
std::unique_ptr<p3d::File> CreateFromP3D(std::wstring const& filepath)
{
	std::ifstream src(filepath, std::ifstream::binary);
	return std::make_unique<p3d::File>(p3d::Read(src));
}

// Populates the p3d data structures from a 3ds file
std::unique_ptr<p3d::File> CreateFrom3DS(std::wstring const& filepath)
{
	// Open the 3ds file
	std::ifstream src(filepath, std::ifstream::binary);

	// Material lookup
	std::unordered_map<std::string, max_3ds::Material> mats;
	auto matlookup = [&](std::string const& name) { return mats.at(name); };

	// Read the materials from the 3ds file and add them to a map.
	// We'll only add the materials that are actually used to the p3d scene.
	max_3ds::ReadMaterials(src, [&](max_3ds::Material const& m)
	{
		mats[m.m_name] = m;
		return false;
	});

	// Read the tri mesh objects from the 3ds file
	p3d::File p3d;
	max_3ds::ReadObjects(src, [&](max_3ds::Object const& o)
	{
		// If the object has no verts or faces, then ignore
		if (o.m_mesh.m_vert.empty() || o.m_mesh.m_face.empty())
			return false;

		// Add a mesh to the scene
		p3d.m_scene.m_meshes.emplace_back(o.m_name);
		auto& mesh = p3d.m_scene.m_meshes.back();

		// Reserve space
		mesh.m_verts.reserve(o.m_mesh.m_vert.size());
		mesh.m_nugget.reserve(o.m_mesh.m_matgroup.size());
		mesh.m_idx.reserve<uint16_t>(o.m_mesh.m_face.size() * 3);
		mesh.m_idx.m_stride = sizeof(uint16_t);

		// Bounding box (can't use 'mesh.m_bbox' directly because it's not a BBox)
		BBox bbox = BBoxReset;
		auto bb = [&](v4 const& v) { Encompass(bbox, v); return v; };

		// Get the 3ds code to extract the verts/faces/normals/nuggets
		// We may regenerate the normals later
		max_3ds::CreateModel(o, matlookup,
			[&](max_3ds::Material const& mat, EGeom geom, Range<uint16_t> vrange, Range<uint32_t> irange) // nugget out
			{
				p3d::Nugget nug = {};
				nug.m_topo = EPrim::TriList;
				nug.m_geom = geom;
				nug.m_vrange = vrange;
				nug.m_irange = irange;
				nug.m_mat = mat.m_name;
				mesh.m_nugget.push_back(nug);
			},
			[&](v4 const& p, Colour const& c, v4 const& n, v2 const& t) // vertex out
			{
				p3d::Vert vert = {};
				vert.pos = bb(p);
				vert.col = c;
				vert.norm = n;
				vert.uv = t;
				mesh.m_verts.push_back(vert);
			},
			[&](uint16_t i0, uint16_t i1, uint16_t i2) // index out
			{
				mesh.m_idx.push_back<uint16_t>(i0);
				mesh.m_idx.push_back<uint16_t>(i1);
				mesh.m_idx.push_back<uint16_t>(i2);
			});

		mesh.m_bbox = bbox;
		mesh.m_o2p = o.m_mesh.m_o2p;

		// Add the used materials to the p3d scene
		for (auto& nug : mesh.m_nugget)
		{
			if (pr::contains_if(p3d.m_scene.m_materials, [&](p3d::Material const& m) { return m.m_id == nug.m_mat; }))
				continue;

			// Add the material
			auto const& mat_3ds = matlookup(nug.m_mat);
			p3d.m_scene.m_materials.emplace_back(mat_3ds.m_name, mat_3ds.m_diffuse);
			auto& mat = p3d.m_scene.m_materials.back();

			// Add the texture filepaths to the material
			for (auto& tex : mat_3ds.m_textures)
			{
				// todo: translate 3ds tiling flags to p3d
				mat.m_tex_diffuse.emplace_back(tex.m_filepath, 0);
			}
		}

		// Don't stop, gimme more objects
		return false;
	});
	return std::make_unique<p3d::File>(p3d);
}

// Populates the p3d data structures from a stl file
std::unique_ptr<p3d::File> CreateFromSTL(std::wstring const& filepath)
{
	// Open the file
	std::ifstream src(filepath, std::ifstream::binary);

	p3d::File p3d;
	stl::Options opts = {};
	stl::Read(src, opts, [&](stl::Model const& o)
	{
		// Add a mesh to the scene
		p3d.m_scene.m_meshes.emplace_back(o.m_header);
		auto& mesh = p3d.m_scene.m_meshes.back();

		// Bounding box (can't use 'mesh.m_bbox' directly because it's not a BBox)
		BBox bbox = BBoxReset;
		auto bb = [&](v4 const& v) { Encompass(bbox, v); return v; };

		// Copy the verts
		mesh.m_verts.reserve(o.m_verts.size());
		for (int i = 0, iend = int(o.m_verts.size()); i != iend; ++i)
		{
			p3d::Vert vert = {};
			vert.pos = bb(o.m_verts[i]);
			vert.col = Colour32White;
			vert.norm = o.m_norms[i / 3];
			vert.uv = v2Zero;
			mesh.m_verts.push_back(vert);
		}
		mesh.m_bbox = bbox;

		// Generate the indices
		auto vcount = int(o.m_verts.size());
		if (vcount < 0x10000)
		{
			// Use 16bit indices
			mesh.m_idx.resize<uint16_t>(vcount);
			mesh.m_idx.m_stride = sizeof(uint16_t);
			auto ibuf = mesh.m_idx.data<uint16_t>();
			for (uint16_t i = 0; vcount-- != 0;)
				*ibuf++ = i++;
		}
		else
		{
			// Use 32bit indices
			mesh.m_idx.resize<uint32_t>(vcount);
			mesh.m_idx.m_stride = sizeof(uint32_t);
			auto ibuf = mesh.m_idx.data<uint32_t>();
			for (uint32_t i = 0; vcount-- != 0;)
				*ibuf++ = i++;
		}

		// Generate a nugget
		p3d::Nugget nug = {};
		nug.m_topo = EPrim::TriList;
		nug.m_geom = EGeom::Vert | EGeom::Norm;
		nug.m_vrange = Range<size_t>(0, mesh.m_verts.size());
		nug.m_irange = Range<size_t>(0, mesh.m_verts.size());
		nug.m_mat = "default";
		mesh.m_nugget.push_back(nug);

		// Generate a material
		p3d::Material mat = {};
		mat.m_id = "default";
		mat.m_diffuse = Colour32White;
		p3d.m_scene.m_materials.push_back(mat);
	});
	return std::make_unique<p3d::File>(p3d);
}

// Write 'p3d' as a p3d format file
void WriteP3d(std::unique_ptr<p3d::File> const& p3d, std::filesystem::path const& outfile, pr::geometry::p3d::EFlags flags)
{
	std::ofstream ofile(outfile, std::ofstream::binary);
	p3d::Write(ofile, *p3d, flags);
}

// Write 'p3d' as a cpp source file
void WriteCpp(std::unique_ptr<p3d::File> const& p3d, std::filesystem::path const& outfile, std::string indent)
{
	std::ofstream ofile(outfile);
	p3d::WriteAsCode(ofile, *p3d, indent.c_str());
}

// Write 'p3d' as ldr script
void WriteLdr(std::unique_ptr<p3d::File> const& p3d, std::filesystem::path const& outfile, std::string indent)
{
	std::ofstream ofile(outfile);
	p3d::WriteAsScript(ofile, *p3d, indent.c_str());
}
