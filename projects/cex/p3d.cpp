//**********************************************
// Command line extensions
//  Copyright (c) Rylogic Ltd 2014
//**********************************************

#include "cex/forward.h"
#include "cex/icex.h"
#include "cex/p3d.h"
#include "pr/geometry/p3d.h"
#include "pr/geometry/3ds.h"
#include "pr/geometry/utility.h"

using namespace pr::geometry;

namespace cex
{
	enum class EP3dOp
	{
		Export,
		RemoveDegenerates,
		GenerateNormals,
	};

	struct P3d::Impl
	{
		p3d::File            m_p3d;
		std::string          m_infile;
		std::string          m_outfile;
		std::vector<EP3dOp>  m_ops;
		int                  m_quantisation;
		float                m_smooth_threshold;
		bool                 m_preserve_uvs;
		bool                 m_preserve_colours;

		Impl()
			:m_p3d()
			,m_infile()
			,m_outfile()
			,m_ops()
			,m_quantisation()
			,m_smooth_threshold()
			,m_preserve_uvs()
			,m_preserve_colours()
		{}
		int Run()
		{
			if (!pr::filesys::FileExists(m_infile))
				throw std::exception(pr::FmtS("'%s' does not exist", m_infile.c_str()));

			// Set the outfile based on the infile if not given
			if (m_outfile.empty())
				m_outfile = pr::filesys::ChangeExtn<std::string>(m_infile, "p3d");

			// Get the infile file extension
			m_infile = pr::filesys::StandardiseC(m_infile);
			auto extn = pr::filesys::GetExtension(m_infile);

			// Populate the p3d file from 'm_infile'
			try
			{
				if (extn.empty())  throw std::exception("unknown file extension");
				else if (extn == "p3d") CreateFromP3D();
				else if (extn == "3ds") CreateFrom3DS();
				else throw std::exception(pr::FmtS("unsupported file format: '*.%s'", extn.c_str()));
			}
			catch (std::exception const& ex)
			{
				std::cerr
					<< "Failed to create p3d file from source:" << m_infile << std::endl
					<< "Error: " << ex.what() << std::endl;
				return -1;
			}

			// Execute the operations on the model file
			for (auto op : m_ops)
			{
				switch (op)
				{
				case EP3dOp::Export:
					break; // already loaded the file
				case EP3dOp::RemoveDegenerates:
					RemoveDegenerateVerts();
					break;
				case EP3dOp::GenerateNormals:
					GenerateNormals();
					break;
				}
			}

			// Write out the p3d file
			WriteP3d();
			return 0;
		}

		// Populate the p3d data structures from a p3d file
		void CreateFromP3D()
		{
			std::ifstream src(m_infile, std::ifstream::binary);
			m_p3d = p3d::Read(src);
		}

		// Populates the p3d data structures from a 3ds file
		void CreateFrom3DS()
		{
			// Open the 3ds file
			std::ifstream src(m_infile, std::ifstream::binary);

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
			max_3ds::ReadObjects(src, [&](max_3ds::Object const& o)
			{
				// Add a trimesh to the scene
				m_p3d.m_scene.m_meshes.emplace_back(o.m_name);
				auto& mesh = m_p3d.m_scene.m_meshes.back();

				// Reserve space
				mesh.m_vert.reserve(o.m_mesh.m_vert.size());
				mesh.m_idx16.reserve(o.m_mesh.m_face.size() * 3);
				mesh.m_nugget.reserve(o.m_mesh.m_matgroup.size());

				// Bounding box
				pr::BBox bbox = pr::BBoxReset;
				auto bb = [&](pr::v4 const& v) { pr::Encompass(bbox, v); return v; };

				// Get the 3ds code to extract the verts/faces/normals/nuggets
				// We may regenerate the normals later
				max_3ds::CreateModel(o, matlookup,
					[&](max_3ds::Material const& mat, EGeom geom, pr::Range<pr::uint16> vrange, pr::Range<pr::uint16> irange) // nugget out
					{
						p3d::Nugget nug = {};
						nug.m_topo = EPrim::TriList;
						nug.m_geom = geom;
						nug.m_vrange = vrange;
						nug.m_irange = irange;
						nug.m_mat = mat.m_name;
						mesh.m_nugget.push_back(nug);
					},
					[&](pr::v4 const& p, pr::Colour const& c, pr::v4 const& n, pr::v2 const& t) // vertex out
					{
						p3d::Vert vert = {};
						vert.pos = bb(p);
						vert.col = c;
						vert.norm = n;
						vert.uv = t;
						mesh.m_vert.push_back(vert);
					},
					[&](pr::uint16 i0, pr::uint16 i1, pr::uint16 i2) // index out
					{
						mesh.m_idx16.push_back(i0);
						mesh.m_idx16.push_back(i1);
						mesh.m_idx16.push_back(i2);
					});

				// Add the bounding box
				mesh.m_bbox = bbox;

				// Add the used materials to the p3d scene
				for (auto& nug : mesh.m_nugget)
				{
					if (pr::contains_if(m_p3d.m_scene.m_materials, [&](p3d::Material const& m){ return m.m_id == nug.m_mat; }))
						continue;

					// Add the material
					auto const& mat_3ds = matlookup(nug.m_mat);
					m_p3d.m_scene.m_materials.emplace_back(mat_3ds.m_name, mat_3ds.m_diffuse);
					auto& mat = m_p3d.m_scene.m_materials.back();

					for (auto& tex : mat_3ds.m_textures)
					{
						// todo: translate 3ds tiling flags to p3d
						mat.m_tex_diffuse.emplace_back(tex.m_filepath, 0);
					}
				}

				return false;
			});
		}

		// Generate normals for the p3d file
		void GenerateNormals()
		{
			using namespace pr::geometry;

			for (auto& mesh : m_p3d.m_scene.m_meshes)
			{
				if (mesh.m_vert.empty())
					continue;

				// Generate normals per nugget because the topology can change per nugget
				for (auto& nug : mesh.m_nugget)
				{
					// Can only generate normals for triangle lists
					if (nug.m_topo != EPrim::TriList)
						continue;

					if (!mesh.m_idx16.empty()) GenerateNormals(nug.m_irange, m_smooth_threshold, mesh.m_vert, mesh.m_idx16);
					if (!mesh.m_idx32.empty()) GenerateNormals(nug.m_irange, m_smooth_threshold, mesh.m_vert, mesh.m_idx32);
				}
			}
		}

		// Generate normals for 16 or 32 bit indices
		template <typename VCont, typename ICont> void GenerateNormals(pr::geometry::p3d::Range irange, float thres, VCont& vcont, ICont& icont)
		{
			typedef std::remove_reference<decltype(icont[0])>::type VIdx;
			auto iptr = std::begin(icont) + irange.first;
			pr::geometry::GenerateNormals(irange.count, iptr, thres,
				[&](VIdx idx)
				{
					return vcont[idx].pos;
				},
				[&](VIdx new_idx, VIdx orig_idx, pr::v4 const& normal)
				{
					if (new_idx == vcont.size()) vcont.push_back(vcont[orig_idx]);
					vcont[new_idx].norm = normal;
				},
				[&](VIdx i0, VIdx i1, VIdx i2)
				{
					*iptr++ = i0;
					*iptr++ = i1;
					*iptr++ = i2;
				});
		}

		// Remove degenerate verts
		void RemoveDegenerateVerts()
		{
			using namespace pr::geometry;

			std::vector<p3d::Vert*> map;
			for (auto& mesh : m_p3d.m_scene.m_meshes)
			{
				if (mesh.m_vert.empty()) continue;

				// Quantise all the verts
				for (auto& vert : mesh.m_vert)
					vert.pos = pr::Quantise(vert.pos, m_quantisation);

				// Initialise the map of pointers to verts
				map.resize(mesh.m_vert.size());
				auto map_ptr = std::begin(map);
				for (auto& vert : mesh.m_vert)
					*map_ptr++ = &vert;

				// Sort the pointer map such that degenerate verts are next to each other
				pr::sort(map,[&](p3d::Vert const* lhs, p3d::Vert const* rhs)
					{
						auto& v0 = *lhs;
						auto& v1 = *rhs;
						if (v0.pos.x != v1.pos.x) return v0.pos.x < v1.pos.x;
						if (v0.pos.y != v1.pos.y) return v0.pos.y < v1.pos.y;
						return v0.pos.z < v1.pos.z;
					});

				// Set each pointer to point at the first degenerate vert
				for (size_t i = 1, iend = map.size(); i != iend; ++i)
				{
					auto& vi = *map[i];
					for (size_t j = i; j-- != 0;)
					{
						auto& vj = *map[j];

						// If the vertex position is different, move to the next vert
						if (vi.pos.x != vj.pos.x) break;
						if (vi.pos.y != vj.pos.y) break;
						if (vi.pos.z != vj.pos.z) break;

						// Keep searching backward if the colours don't match
						if (m_preserve_colours && !(
							pr::FEql(vi.col.r, vj.col.r) &&
							pr::FEql(vi.col.g, vj.col.g) &&
							pr::FEql(vi.col.b, vj.col.b) &&
							pr::FEql(vi.col.a, vj.col.a)))
							continue;

						// Keep searching backward if the UVs don't match
						if (m_preserve_uvs && !(
							pr::FEql(vi.uv.u, vj.uv.u) && 
							pr::FEql(vi.uv.v, vj.uv.v)))
							continue;

						// Degenerate found
						map[i] = map[j];
						break;
					}
				}
			}
		}

		// Write the p3d file to a file stream
		void WriteP3d()
		{
			std::ofstream ofile(m_outfile, std::ofstream::binary);
			p3d::Write(ofile, m_p3d);
		}
	};

	// *****

	P3d::P3d()
		:m_ptr(std::make_unique<Impl>())
	{}
	void P3d::ShowHelp() const
	{
		std::cout << R"(
P3D Export : Tools for creating p3d files
Syntax:
  Cex -p3d -export -fi 'filepath.ext' [-fo 'output_filepath.p3d']
  Cex -p3d -remove_degenerates 'tolerence' -fi 'filepath.p3d' [-fo 'output_filepath.p3d'] [-preserve_uvs] [-preserve_colours]
  Cex -p3d -gen_normals 'threshold' -fi 'filepath.p3d' [-fo 'output_filepath.p3d']

    -fi 'filepath.ext' - the input 3d model file to be converted to p3d.
          File type is determined from the file extension. (3ds only so far)

    -fo 'output_filepath' - The p3d file that will be created, if omitted, then the output
          file will be named 'filepath.p3d' in the same directory.

    -remove_degenerates 'tolerence' - Strip duplicate verts from the model.
          By default only position is used to determine degeneracy. 'tolerence' is
          a power of 2 value such that verts are quantised to '1/tolerence'.

    -preserve_uvs - Verts with differing UV coordinates will not be considered degenerate

    -preserve_colours - Verts with differing colours will not be considered degenerate

    -gen_normals 'threshold' - overwrite the model normal data using 'threshold' is the
          tolerence for coplanar faces (in degrees)

  Note: All commands can be given on one command line, order of operations is in the order
  specified on the command line.
)";
	}
	bool P3d::CmdLineOption(std::string const& option, pr::cmdline::TArgIter& arg, pr::cmdline::TArgIter arg_end)
	{
		if (pr::str::EqualI(option, "-p3d"))
		{
			return true;
		}
		if (pr::str::EqualI(option, "-export"))
		{
			m_ptr->m_ops.push_back(EP3dOp::Export);
			return true;
		}
		if (pr::str::EqualI(option, "-remove_degenerates") && arg != arg_end)
		{
			m_ptr->m_ops.push_back(EP3dOp::RemoveDegenerates);
			if (!pr::str::ExtractIntC(m_ptr->m_quantisation, 10, arg->c_str()))
				throw std::exception("Tolerence value expected following -remove_degenerates");
			if (!pr::IsPowerOfTwo(m_ptr->m_quantisation))
				throw std::exception("Tolerence value should be a power of 2, i.e. 256,1024,etc.");
			++arg;
			return true;
		}
		if (pr::str::EqualI(option, "-gen_normals") && arg != arg_end)
		{
			m_ptr->m_ops.push_back(EP3dOp::GenerateNormals);
			if (!pr::str::ExtractRealC(m_ptr->m_smooth_threshold, arg->c_str()))
				throw std::exception("Smoothing threshold expected following -gen_normals");
			m_ptr->m_smooth_threshold = pr::DegreesToRadians(m_ptr->m_smooth_threshold);
			++arg;
			return true;
		}
		if (pr::str::EqualI(option, "-fi") && arg != arg_end)
		{
			m_ptr->m_infile = *arg++;
			return true;
		}
		if (pr::str::EqualI(option, "-fo") && arg != arg_end)
		{
			m_ptr->m_outfile = *arg++;
			return true;
		}
		if (pr::str::EqualI(option, "-preserve_uvs"))
		{
			m_ptr->m_preserve_uvs = true;
			return true;
		}
		if (pr::str::EqualI(option, "-preserve_colours"))
		{
			m_ptr->m_preserve_colours = true;
			return true;
		}
		return ICex::CmdLineOption(option, arg, arg_end);
	}
	int P3d::Run()
	{
		ShowConsole();
		return m_ptr->Run();
	}
}
