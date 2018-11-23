//**********************************************
// Command line extensions
//  Copyright (c) Rylogic Ltd 2014
//**********************************************

#include "cex/src/forward.h"
#include "cex/src/icex.h"
#include "cex/src/p3d.h"
#include "pr/geometry/p3d.h"
#include "pr/geometry/3ds.h"
#include "pr/geometry/stl.h"
#include "pr/geometry/utility.h"
#include "pr/maths/bit_fields.h"

using namespace pr::geometry;

namespace cex
{
	enum class EP3dOp
	{
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
		bool                 m_preserve_normals;
		bool                 m_preserve_colours;
		bool                 m_preserve_uvs;
		bool                 m_codeout;
		std::string          m_indent;
		int                  m_verbosity; // 0 = quiet, 1 = minimal, 2 = normal, 3 = verbose

		Impl()
			:m_p3d()
			,m_infile()
			,m_outfile()
			,m_ops()
			,m_quantisation(16)
			,m_smooth_threshold(10)
			,m_preserve_normals(false)
			,m_preserve_colours(false)
			,m_preserve_uvs(false)
			,m_codeout(false)
			,m_indent()
			,m_verbosity(2)
		{}

		// Run the command
		int Run()
		{
			if (!pr::filesys::FileExists(m_infile))
				throw std::exception(pr::FmtS("Input file '%s' does not exist", m_infile.c_str()));

			// Set the out file based on the in file if not given
			if (m_outfile.empty())
			{
				m_outfile = m_codeout
					? pr::filesys::ChangeExtn<std::string>(m_infile, "cpp")
					: pr::filesys::ChangeExtn<std::string>(m_infile, "p3d");
			}

			// Get the in file extension
			m_infile = pr::filesys::Standardise(m_infile);
			auto extn = pr::filesys::GetExtension(m_infile);

			// Populate the p3d object from 'm_infile'
			try
			{
				if (extn.empty())  throw std::exception("unknown file extension");
				else if (extn == "p3d") CreateFromP3D();
				else if (extn == "3ds") CreateFrom3DS();
				else if (extn == "stl") CreateFromSTL();
				else throw std::exception(pr::FmtS("unsupported file format: '*.%s'", extn.c_str()));

				if (m_verbosity >= 2)
				{
					std::cout << "  Loaded '" << m_infile << "'\n";
					if (m_verbosity >= 3)
						for (auto& mesh : m_p3d.m_scene.m_meshes)
							std::cout 
								<< "         Mesh: " << mesh.m_name << "\n"
								<< "        Verts: " << mesh.m_verts.size() << "\n"
								<< "        Idx16: " << mesh.m_idx16.size() << "\n"
								<< "        Idx32: " << mesh.m_idx32.size() << "\n"
								<< "      Nuggets: " << mesh.m_nugget.size() << "\n";
				}
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
				// Add a mesh to the scene
				m_p3d.m_scene.m_meshes.emplace_back(o.m_name);
				auto& mesh = m_p3d.m_scene.m_meshes.back();

				// Reserve space
				mesh.m_verts.reserve(o.m_mesh.m_vert.size());
				mesh.m_idx16.reserve(o.m_mesh.m_face.size() * 3);
				mesh.m_nugget.reserve(o.m_mesh.m_matgroup.size());

				// Bounding box (can't use 'mesh.m_bbox' directly because it's not a pr::BBox)
				pr::BBox bbox = pr::BBoxReset;
				auto bb = [&](pr::v4 const& v) { pr::Encompass(mesh.m_bbox, v); return v; };

				// Get the 3ds code to extract the verts/faces/normals/nuggets
				// We may regenerate the normals later
				max_3ds::CreateModel(o, matlookup,
					[&](max_3ds::Material const& mat, EGeom geom, pr::Range<pr::uint16> vrange, pr::Range<pr::uint32> irange) // nugget out
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
						mesh.m_verts.push_back(vert);
					},
					[&](pr::uint16 i0, pr::uint16 i1, pr::uint16 i2) // index out
					{
						mesh.m_idx16.push_back(i0);
						mesh.m_idx16.push_back(i1);
						mesh.m_idx16.push_back(i2);
					});
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

		// Populates the p3d data structures from a stl file
		void CreateFromSTL()
		{
			// Open the file
			std::ifstream src(m_infile, std::ifstream::binary);

			stl::Options opts = {};
			stl::Read(src, opts, [&](stl::Model const& o)
			{
				// Add a mesh to the scene
				m_p3d.m_scene.m_meshes.emplace_back(o.m_header);
				auto& mesh = m_p3d.m_scene.m_meshes.back();

				// Bounding box (can't use 'mesh.m_bbox' directly because it's not a pr::BBox)
				pr::BBox bbox = pr::BBoxReset;
				auto bb = [&](pr::v4 const& v) { pr::Encompass(bbox, v); return v; };

				// Copy the verts
				mesh.m_verts.reserve(o.m_verts.size());
				for (int i = 0, iend = int(o.m_verts.size()); i != iend; ++i)
				{
					p3d::Vert vert = {};
					vert.pos = bb(o.m_verts[i]);
					vert.col = pr::Colour32White;
					vert.norm = o.m_norms[i/3];
					vert.uv = pr::v2Zero;
					mesh.m_verts.push_back(vert);
				}
				mesh.m_bbox = bbox;

				// Generate the indices
				auto vcount = int(o.m_verts.size());
				if (vcount < 0x10000)
				{
					// Use 16bit indices
					mesh.m_idx16.resize(vcount);
					auto ibuf = mesh.m_idx16.data();
					for (pr::uint16 i = 0; vcount-- != 0;)
						*ibuf++ = i++;
				}
				else
				{
					// Use 32bit indices
					mesh.m_idx32.resize(vcount);
					auto ibuf = mesh.m_idx32.data();
					for (pr::uint32 i = 0; vcount-- != 0;)
						*ibuf++ = i++;
				}

				// Generate a nugget
				p3d::Nugget nug = {};
				nug.m_topo = EPrim::TriList;
				nug.m_geom = EGeom::Vert | EGeom::Norm;
				nug.m_vrange = pr::Range<size_t>(0, mesh.m_verts.size());
				nug.m_irange = pr::Range<size_t>(0, mesh.m_verts.size());
				nug.m_mat = "default";
				mesh.m_nugget.push_back(nug);

				// Generate a material
				p3d::Material mat = {};
				mat.m_id = "default";
				mat.m_diffuse = pr::Colour32White;
				m_p3d.m_scene.m_materials.push_back(mat);
			});
		}

		// Generate normals for the p3d file
		void GenerateNormals()
		{
			using namespace pr::geometry;

			// Generate normals for each mesh
			for (auto& mesh : m_p3d.m_scene.m_meshes)
			{
				if (mesh.m_verts.empty())
					continue;

				if (m_verbosity >= 2)
					std::cout << "  Generating normals for mesh: " << mesh.m_name << std::endl;

				// Generate normals per nugget since the topology can change per nugget
				for (auto& nug : mesh.m_nugget)
				{
					// Can only generate normals for triangle lists
					if (nug.m_topo != EPrim::TriList)
						continue;

					if (!mesh.m_idx16.empty()) GenerateNormals(nug, mesh.m_verts, mesh.m_idx16);
					if (!mesh.m_idx32.empty()) GenerateNormals(nug, mesh.m_verts, mesh.m_idx32);
				}
			}
		}

		// Generate normals for 16 or 32 bit indices
		template <typename VCont, typename ICont>
		void GenerateNormals(pr::geometry::p3d::Nugget& nug, VCont& vcont, ICont& icont)
		{
			typedef std::remove_reference<decltype(icont[0])>::type VIdx;

			// Pointer to the first index of this nugget
			auto iptr = std::begin(icont) + nug.m_irange.first;

			// Reset the vrange in the nugget, since generating normals will create new verts
			VIdx mn = pr::limits<VIdx>::max();
			VIdx mx = pr::limits<VIdx>::min();

			// Generate the normals
			pr::geometry::GenerateNormals(nug.m_irange.count, iptr, pr::DegreesToRadians(m_smooth_threshold),
				[&](VIdx idx) { return vcont[idx].pos; }, vcont.size(),
				[&](VIdx new_idx, VIdx orig_idx, pr::v4 const& normal)
				{
					if (new_idx >= vcont.size()) vcont.resize(new_idx + 1, vcont[orig_idx]);
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

		// Remove degenerate verts
		void RemoveDegenerateVerts()
		{
			using namespace pr::geometry;

			// A map from original vertex to non-degenerate vertex
			struct VP { p3d::Vert *orig, *nondeg; };
			std::vector<VP> map;

			for (auto& mesh : m_p3d.m_scene.m_meshes)
			{
				if (mesh.m_verts.empty())
					continue;

				if (m_verbosity >= 2)
					std::cout << "  Removing degenerate verts for mesh: " << mesh.m_name << std::endl;

				// Quantise all the verts
				for (auto& vert : mesh.m_verts)
					vert.pos = pr::Quantise((pr::v4)vert.pos, 1 << m_quantisation);

				// Initialise the map
				map.resize(mesh.m_verts.size());
				auto map_ptr = std::begin(map);
				for (auto& vert : mesh.m_verts)
				{
					map_ptr->orig = map_ptr->nondeg = &vert;
					++map_ptr;
				}

				// Sort the map such that degenerate verts are next to each other
				pr::sort(map,[&](VP const& lhs, VP const& rhs)
				{
					auto& v0 = *lhs.orig;
					auto& v1 = *rhs.orig;
					if (v0.pos.x != v1.pos.x) return v0.pos.x < v1.pos.x;
					if (v0.pos.y != v1.pos.y) return v0.pos.y < v1.pos.y;
					return v0.pos.z < v1.pos.z;
				});

				// Set each pointer equal to the first degenerate vert
				size_t unique_count = map.size();
				for (size_t i = 1, iend = map.size(); i != iend; ++i)
				{
					auto& vi = *map[i].nondeg;
					for (size_t j = i; j-- != 0;)
					{
						auto& vj = *map[j].nondeg;

						// If the vertex position is different, move to the next vert
						if (vi.pos != vj.pos)
							break;

						// The verts are only sorted by position, so we need to check
						// all verts back to where the positions no longer match

						// Keep searching backward if the normals don't match
						if (m_preserve_normals && !FEql(vi.norm, vj.norm))
							continue;

						// Keep searching backward if the colours don't match
						if (m_preserve_colours && !FEql(vi.col,vj.col))
							continue;

						// Keep searching backward if the UVs don't match
						if (m_preserve_uvs && !FEql(vi.uv,vj.uv))
							continue;

						// Degenerate found
						map[i].nondeg = map[j].nondeg;
						--unique_count;
						break;
					}
				}
				if (m_verbosity >= 3)
				{
					std::cout
						<< "    " << (map.size() - unique_count) << " degenerate verts found\n"
						<< "    " << unique_count << " verts remaining." << std::endl;
				}

				// Returns true if 'ptr' points within 'cont'
				auto is_within = [](std::vector<p3d::Vert> const& cont, p3d::Vert const* ptr) { return ptr >= cont.data() && ptr < cont.data() + cont.size(); };

				// Create a collection of verts without degenerates
				std::vector<p3d::Vert> verts(unique_count);
				auto vout = std::begin(verts);
				for (size_t i = 0, iend = map.size(); i != iend; ++i)
				{
					// If this vert is already pointing into the new container, skip on the next
					if (is_within(verts, map[i].nondeg))
						continue;

					// Save the pointer to the vert in the old container
					auto ptr = map[i].nondeg;
					auto pos = ptr->pos;

					// Map all pointers up to the next non-degenerate to the new container.
					// Test all verts after 'i' until the positions differ. The preserved properties
					// mean that not all degenerate verts are adjacent in 'map'
					for (size_t j = i; j != iend && map[j].nondeg->pos == pos; ++j)
					{
						// If 'map[j]' is actually degenerate with 'map[i]' set the pointer to
						// point into the new container at where 'map[i]' is pointing.
						// (Note: map[i] is set by this for loop as well)
						if (map[j].nondeg == ptr)
							map[j].nondeg = &*vout;
					}

					// Copy the vert to the new collection
					*vout++  = *ptr;
				}

				// Resort the map back to the order of mesh.m_vert
				pr::sort(map, [&](VP const& lhs, VP const& rhs)
					{
						return lhs.orig < rhs.orig;
					});

				// Update the indices to the non-degenerate vert indices
				for (auto& idx : mesh.m_idx16)
					idx = pr::checked_cast<p3d::u16>(map[idx].nondeg - verts.data());
				for (auto& idx : mesh.m_idx32)
					idx = pr::checked_cast<p3d::u32>(map[idx].nondeg - verts.data());

				// Update the vrange for each nugget
				for (auto& nug : mesh.m_nugget)
				{
					auto vrange = pr::Range<p3d::u32>::Reset();
					for (auto i = nug.m_vrange.first, iend = i + nug.m_vrange.count; i != iend; ++i)
						Encompass(vrange, static_cast<p3d::u32>(map[i].nondeg - verts.data()));
					nug.m_vrange = vrange;
				}

				// Replace the vert container in the mesh
				mesh.m_verts = std::move(verts);
			}
		}

		// Write the p3d file to a file stream
		void WriteP3d()
		{
			if (!m_codeout)
			{
				std::ofstream ofile(m_outfile, std::ofstream::binary);
				p3d::Write(ofile, m_p3d);
			}
			else
			{
				std::ofstream ofile(m_outfile);
				p3d::WriteAsCode(ofile, m_p3d, m_indent.c_str());
			}
			if (m_verbosity >= 1)
				std::cout << " '" << m_outfile << "' saved." << std::endl;
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
  cex -p3d -fi 'filepath.ext' [-fo 'output_filepath.p3d']
  cex -p3d -fi 'filepath.p3d' [-fo 'output_filepath.p3d'] -remove_degenerates [<tolerance>] [-preserve_uvs] [-preserve_colours]
  cex -p3d -fi 'filepath.p3d' [-fo 'output_filepath.p3d'] -generate_normals [<threshold>]

    -fi filepath - the input 3d model file to be converted to p3d.
          File type is determined from the file extension.
          Supported Formats: 3DS, STL (so far)

    -fo output_filepath - The p3d file that will be created, if omitted, then the output
          file will be named 'filepath.p3d' in the same directory.

    -codeout - Output the model as C++ code.
    -indent s - A string to indent each line with

    -remove_degenerates [<tolerance>] - Strip duplicate verts from the model.
          By default only position is used to determine degeneracy, use the 'preserve'
          options to include other vertex fields. <tolerance> is a value used to quantise
          the vertex positions (quantised to '1 / 2^tolerence') (default 16).

    -preserve_normals - Verts with differing normals will not be considered degenerate

    -preserve_colours - Verts with differing colours will not be considered degenerate

    -preserve_uvs - Verts with differing UV coordinates will not be considered degenerate

    -generate_normals [<threshold>] - overwrite the model normal data using 'threshold' as the
          tolerance for coplanar faces (in degrees) (default 10)

    -verbosity level - amount of text written to stdout, 0=quiet, 1=minimal, 2=normal, 3=verbose

  Note: All commands can be given on one command line, order of operations is in the order
  specified on the command line.
)" << std::endl;
	}
	bool P3d::CmdLineOption(std::string const& option, TArgIter& arg, TArgIter arg_end)
	{
		if (pr::str::EqualI(option, "-p3d"))
		{
			return true;
		}
		if (pr::str::EqualI(option, "-remove_degenerates"))
		{
			m_ptr->m_ops.push_back(EP3dOp::RemoveDegenerates);
			if (arg != arg_end && !IsOption(*arg))
			{
				if (!pr::str::ExtractIntC(m_ptr->m_quantisation, 10, arg->c_str()))
					throw std::exception("Tolerance value expected following -remove_degenerates");
				if (m_ptr->m_quantisation < 0 || m_ptr->m_quantisation >= 32)
					throw std::exception("Tolerance value should be an integer in the range [0,32).");
				++arg;
			}
			return true;
		}
		if (pr::str::EqualI(option, "-generate_normals"))
		{
			m_ptr->m_ops.push_back(EP3dOp::GenerateNormals);
			if (arg != arg_end && !IsOption(*arg))
			{
				if (!pr::str::ExtractRealC(m_ptr->m_smooth_threshold, arg->c_str()))
					throw std::exception("Smoothing threshold expected following -gen_normals");
				if (m_ptr->m_smooth_threshold < 0 || m_ptr->m_smooth_threshold >= 180)
					throw std::exception("Smoothing threshold value should be a value in the range [0,180).");
				++arg;
			}
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
		if (pr::str::EqualI(option, "-preserve_normals"))
		{
			m_ptr->m_preserve_normals = true;
			return true;
		}
		if (pr::str::EqualI(option, "-preserve_colours"))
		{
			m_ptr->m_preserve_colours = true;
			return true;
		}
		if (pr::str::EqualI(option, "-preserve_uvs"))
		{
			m_ptr->m_preserve_uvs = true;
			return true;
		}
		if (pr::str::EqualI(option, "-codeout"))
		{
			m_ptr->m_codeout = true;
			return true;
		}
		if (pr::str::EqualI(option, "-indent") && arg != arg_end)
		{
			m_ptr->m_indent = *arg++;
			return true;
		}
		if (pr::str::EqualI(option, "-verbosity") && arg != arg_end)
		{
			if (!pr::str::ExtractIntC(m_ptr->m_verbosity, 10, arg->c_str()))
				throw std::exception("Verbosity level expected after -verbosity");
			++arg;
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
