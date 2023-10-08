//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#include "pr/view3d-12/model/model_generator.h"
#include "pr/view3d-12/main/renderer.h"
#include "pr/view3d-12/model/vertex_layout.h"
#include "pr/view3d-12/model/model_tree.h"
#include "pr/view3d-12/texture/texture_desc.h"

namespace pr::rdr12
{

	// Implementation functions
	struct Impl
	{
		// Bake a transform into 'cache'
		template <typename VType>
		static void BakeTransform(ModelGenerator::Cache<VType>& cache, m4x4 const& a2b)
		{
			// Apply the 'bake' transform to every vertex
			cache.m_bbox = a2b * cache.m_bbox;
			for (auto& v : cache.m_vcont)
			{
				v.m_vert = a2b * v.m_vert;
				v.m_norm = a2b * v.m_norm;
			}

			// If the transform is left handed, flip the faces
			if (Determinant3(a2b) < 0)
			{
				// Check each nugget for faces
				for (auto& nug : cache.m_ncont)
				{
					switch (nug.m_topo)
					{
					case ETopo::TriList:
					{
						switch (cache.m_icont.stride())
						{
							case sizeof(uint32_t) : FlipTriListFaces<VType, uint32_t>(cache, nug.m_irange); break;
								case sizeof(uint16_t) : FlipTriListFaces<VType, uint16_t>(cache, nug.m_irange); break;
								default: throw std::runtime_error("Unsupported index stride");
						}
						break;
					}
					case ETopo::TriStrip:
					{
						switch (cache.m_icont.stride())
						{
							case sizeof(uint32_t) : FlipTriStripFaces<VType, uint32_t>(cache, nug.m_irange); break;
								case sizeof(uint16_t) : FlipTriStripFaces<VType, uint16_t>(cache, nug.m_irange); break;
								default: throw std::runtime_error("Unsupported index stride");
						}
						break;
					}
					}
				}
			}
		}

		// Flip the winding order of faces in a triangle list
		template <typename VType, typename IType>
		static void FlipTriListFaces(ModelGenerator::Cache<VType>& cache, Range irange)
		{
			assert((irange.size() % 3) == 0);
			auto ibuf = cache.m_icont.data<IType>();
			for (size_t i = irange.begin(), iend = irange.end(); i != iend; i += 3)
				std::swap(ibuf[i + 1], ibuf[i + 2]);
		}

		// Flip the winding order of faces in a triangle strip
		template <typename VType, typename IType>
		static void FlipTriStripFaces(ModelGenerator::Cache<VType>& cache, Range irange)
		{
			assert((irange.size() % 2) == 0);
			auto ibuf = cache.m_icont.data<IType>();
			for (size_t i = irange.begin(), iend = irange.end(); i != iend; i += 2)
				std::swap(ibuf[i + 0], ibuf[i + 1]);
		}

		// Generate normals for the triangle list nuggets in 'cache'
		template <typename VType>
		static void GenerateNormals(ModelGenerator::Cache<VType>& cache, float gen_normals)
		{
			assert(gen_normals >= 0 && "Smoothing threshold must be a positive number");

			// Check each nugget for faces
			for (auto& nug : cache.m_ncont)
			{
				switch (nug.m_topo)
				{
				case ETopo::TriList:
				{
					switch (cache.m_icont.stride())
					{
						case sizeof(uint32_t) : GenerateNormals<VType, uint32_t>(cache, nug.m_irange, gen_normals); break;
							case sizeof(uint16_t) : GenerateNormals<VType, uint16_t>(cache, nug.m_irange, gen_normals); break;
							default: throw std::runtime_error("Unsupported index stride");
					}
					break;
				}
				case ETopo::TriStrip:
				{
					throw std::exception("Generate normals isn't supported for TriStrip");
				}
				}
			}
		}

		// Generate normals for the triangle list given by index range 'irange' in 'cache'
		template <typename VType, typename IType>
		static void GenerateNormals(ModelGenerator::Cache<VType>& cache, Range irange, float gen_normals)
		{
			auto ibuf = cache.m_icont.data<IType>() + irange.begin();
			geometry::GenerateNormals(
				irange.size(), ibuf, gen_normals, cache.m_vcont.size(),
				[&](IType idx)
				{
				return GetP(cache.m_vcont[idx]);
				},
				[&](IType idx, IType orig, v4 const& norm)
				{
				assert(idx <= cache.m_vcont.size());
				if (idx == cache.m_vcont.size()) cache.m_vcont.push_back(cache.m_vcont[orig]);
				SetN(cache.m_vcont[idx], norm);
				},
				[&](IType i0, IType i1, IType i2)
				{
				*ibuf++ = i0;
				*ibuf++ = i1;
				*ibuf++ = i2;
				});
		}
	};

	// Create a model from 'cache'
	// 'bake' is a transform to bake into the model
	// 'gen_normals' generates normals for the model if >= 0f. Value is the threshold for smoothing (in rad)
	template <typename VType>
	ModelPtr Create(Renderer& rdr, ModelGenerator::Cache<VType>& cache, ModelGenerator::CreateOptions const& opts = ModelGenerator::CreateOptions{})
	{
		// Sanity check 'cache'
		#if PR_DBG_RDR
		assert(!cache.m_ncont.empty() && "No nuggets given");
		for (auto& nug : cache.m_ncont)
		{
			assert(nug.m_vrange.begin() < cache.VCount() && "Nugget range invalid");
			assert(nug.m_irange.begin() < cache.ICount() && "Nugget range invalid");
			assert(nug.m_vrange.end() <= cache.VCount() && "Nugget range invalid");
			assert(nug.m_irange.end() <= cache.ICount() && "Nugget range invalid");
		}
		#endif

		// Bake a transform into the model
		if (opts.m_bake != nullptr)
			Impl::BakeTransform(cache, *opts.m_bake);

		// Generate normals
		if (opts.m_gen_normals != nullptr)
			Impl::GenerateNormals(cache, *opts.m_gen_normals);

		// Create the model
		ModelDesc mdesc(
			ResDesc::VBuf<VType>(cache.VCount(), cache.m_vcont.data()),
			ResDesc::IBuf(cache.ICount(), cache.m_icont.stride(), cache.m_icont.data()),
			cache.m_bbox, cache.m_name.c_str());
		auto model = rdr.res_mgr().CreateModel(mdesc);

		// Create the render nuggets
		for (auto& nug : cache.m_ncont)
		{
			// If the model geom has valid texture data but no texture, use white
			if (AllSet(nug.m_geom, EGeom::Tex0) && nug.m_tex_diffuse == nullptr)
				nug.m_tex_diffuse = rdr.res_mgr().FindTexture(EStockTexture::White);

			// Create the nugget
			model->CreateNugget(nug);
		}

		// Return the freshly minted model.
		return model;
	}

	// Points/Sprites *********************************************************************
	// Generate a cloud of points from an array of points
	ModelPtr ModelGenerator::Points(Renderer& rdr, int num_points, v4 const* points, int num_colours, Colour32 const* colours, NuggetData const* mat)
	{
		// Calculate the required buffer sizes
		auto [vcount, icount] = geometry::PointSize(num_points);

		// Generate the geometry
		Cache cache{vcount, icount, 0, sizeof(uint16_t)};
		auto vptr = cache.m_vcont.data();
		auto iptr = cache.m_icont.data<uint16_t>();
		auto props = geometry::Points(num_points, points, num_colours, colours,
			[&](v4_cref<> p, Colour32 c) { SetPC(*vptr++, p, Colour(c)); },
			[&](int idx) { *iptr++ = s_cast<uint16_t>(idx); });

		// Create a nugget
		cache.AddNugget(ETopo::PointList, props.m_geom, props.m_has_alpha, false, mat);
		cache.m_bbox = props.m_bbox;

		// Create the model
		return Create(rdr, cache);
	}

	// Lines ******************************************************************************
	// Generate lines from an array of start point, end point pairs.
	// 'num_lines' is the number of start/end point pairs in the following arrays
	// 'points' is the input array of start and end points for lines.
	// 'num_colours' should be either, 0, 1, or num_lines * 2
	// 'colours' is an input array of colour values or a pointer to a single colour.
	// 'mat' is an optional material to use for the lines
	ModelPtr ModelGenerator::Lines(Renderer& rdr, int num_lines, v4 const* points, int num_colours, Colour32 const* colours, NuggetData const* mat)
	{
		// Calculate the required buffer sizes
		auto [vcount, icount] = geometry::LineSize(num_lines);

		// Generate the geometry
		Cache cache{vcount, icount, 0, sizeof(uint16_t)};
		auto vptr = cache.m_vcont.data();
		auto iptr = cache.m_icont.data<uint16_t>();
		auto props = geometry::Lines(num_lines, points, num_colours, colours, 
			[&](v4_cref<> p, Colour32 c) { SetPC(*vptr++, p, Colour(c)); },
			[&](int idx) { *iptr++ = s_cast<uint16_t>(idx); });

		// Create a nugget
		cache.AddNugget(ETopo::LineList, props.m_geom, props.m_has_alpha, false, mat);
		cache.m_bbox = props.m_bbox;

		// Create the model
		return Create(rdr, cache);
	}
	ModelPtr ModelGenerator::LinesD(Renderer& rdr, int num_lines, v4 const* points, v4 const* directions, int num_colours, Colour32 const* colours, NuggetData const* mat)
	{
		// Calculate the required buffer sizes
		auto [vcount, icount] = geometry::LineSize(num_lines);

		// Generate the geometry
		Cache cache(vcount, icount, 0, 2);
		auto vptr = cache.m_vcont.data();
		auto iptr = cache.m_icont.data<uint16_t>();
		auto props = geometry::LinesD(num_lines, points, directions, num_colours, colours,
			[&](v4_cref<> p, Colour32 c) { SetPC(*vptr++, p, Colour(c)); },
			[&](int idx) { *iptr++ = s_cast<uint16_t>(idx); });
		
		// Create a nugget
		cache.AddNugget(ETopo::LineList, props.m_geom, props.m_has_alpha, false, mat);
		cache.m_bbox = props.m_bbox;

		// Create the model
		return Create(rdr, cache);
	}
	ModelPtr ModelGenerator::LineStrip(Renderer& rdr, int num_lines, v4 const* points, int num_colours, Colour32 const* colour, NuggetData const* mat)
	{
		// Calculate the required buffer sizes
		auto [vcount, icount] = geometry::LineStripSize(num_lines);
		auto big_indices = num_lines > 0xFFFF;

		// Generate the geometry
		Cache cache{vcount, icount, 0, s_cast<int>(big_indices ? sizeof(uint32_t) : sizeof(uint16_t))};
		auto vptr = cache.m_vcont.data();
		auto iptr32 = cache.m_icont.data<uint32_t>();
		auto iptr16 = cache.m_icont.data<uint16_t>();
		auto props = geometry::LinesStrip(num_lines, points, num_colours, colour,
			[&](v4_cref<> p, Colour32 c)
			{
				SetPC(*vptr++, p, Colour(c));
			},
			[&](int idx)
			{
				big_indices
					? *iptr32++ = s_cast<uint32_t>(idx)
					: *iptr16++ = s_cast<uint16_t>(idx);
			});

		// Create a nugget
		cache.AddNugget(ETopo::LineStrip, props.m_geom, props.m_has_alpha, false, mat);
		cache.m_bbox = props.m_bbox;

		// Create the model
		return Create(rdr, cache);
	}

	// Quad *******************************************************************************
	ModelPtr ModelGenerator::Quad(Renderer& rdr, NuggetData const* mat)
	{
		v4 const verts[] = { v4{-1,-1,0,1}, v4{+1,-1,0,1}, v4{-1,+1,0,1}, v4{+1,+1,0,1} };
		return Quad(rdr, 1, &verts[0], 0, nullptr, m4x4Identity, mat);
	}
	ModelPtr ModelGenerator::Quad(Renderer& rdr, int num_quads, v4 const* verts, int num_colours, Colour32 const* colours, m4x4 const& t2q, NuggetData const* mat)
	{
		// Calculate the required buffer sizes
		auto [vcount, icount] = geometry::QuadSize(num_quads);

		// Generate the geometry
		Cache cache{vcount, icount, 0, sizeof(uint16_t)};
		auto vptr = cache.m_vcont.data();
		auto iptr = cache.m_icont.data<uint16_t>();
		auto props = geometry::Quad(num_quads, verts, num_colours, colours, t2q,
			[&](v4_cref<> p, Colour32 c, v4_cref<> n, v2_cref<> t) { SetPCNT(*vptr++, p, Colour(c), n, t); },
			[&](int idx) { *iptr++ = s_cast<uint16_t>(idx); });

		// Create a nugget
		cache.AddNugget(ETopo::TriList, props.m_geom, props.m_has_alpha, false, mat);
		cache.m_bbox = props.m_bbox;

		// Create the model
		return Create(rdr, cache);
	}
	ModelPtr ModelGenerator::Quad(Renderer& rdr, v2 const& anchor, v4 const& quad_w, v4 const& quad_h, iv2 const& divisions, Colour32 colour, m4x4 const& t2q, NuggetData const* mat)
	{
		// Calculate the required buffer sizes
		auto [vcount, icount] = geometry::QuadSize(divisions);

		// Generate the geometry
		Cache cache(vcount, icount, 0, 2);
		auto vptr = cache.m_vcont.data();
		auto iptr = cache.m_icont.data<uint16_t>();
		auto props = geometry::Quad(anchor, quad_w, quad_h, divisions, colour, t2q,
			[&](v4_cref<> p, Colour32 c, v4_cref<> n, v2_cref<> t) { SetPCNT(*vptr++, p, Colour(c), n, t); },
			[&](int idx) { *iptr++ = s_cast<uint16_t>(idx); });

		// Create a nugget
		cache.AddNugget(ETopo::TriList, props.m_geom, props.m_has_alpha, false, mat);
		cache.m_bbox = props.m_bbox;

		// Create the model
		return Create(rdr, cache);
	}
	ModelPtr ModelGenerator::Quad(Renderer& rdr, AxisId axis_id, v2 const& anchor, float width, float height, iv2 const& divisions, Colour32 colour, m4x4 const& t2q, NuggetData const* mat)
	{
		// Calculate the required buffer sizes
		auto [vcount, icount] = geometry::QuadSize(divisions);

		// Generate the geometry
		Cache cache{vcount, icount, 0, sizeof(uint16_t)};
		auto vptr = cache.m_vcont.data();
		auto iptr = cache.m_icont.data<uint16_t>();
		auto props = geometry::Quad(axis_id, anchor, width, height, divisions, colour, t2q,
			[&](v4_cref<> p, Colour32 c, v4_cref<> n, v2_cref<> t) { SetPCNT(*vptr++, p, Colour(c), n, t); },
			[&](int idx) { *iptr++ = s_cast<uint16_t>(idx); });

		// Create a nugget
		cache.AddNugget(ETopo::TriList, props.m_geom, props.m_has_alpha, false, mat);
		cache.m_bbox = props.m_bbox;

		// Create the model
		return Create(rdr, cache);
	}
	ModelPtr ModelGenerator::QuadStrip(Renderer& rdr, int num_quads, v4 const* verts, float width, int num_normals, v4 const* normals, int num_colours, Colour32 const* colours, NuggetData const* mat)
	{
		// Calculate the required buffer sizes
		auto [vcount, icount] = geometry::QuadStripSize(num_quads);

		// Generate the geometry
		Cache cache(vcount, icount, 0, 2);
		auto vptr = cache.m_vcont.data();
		auto iptr = cache.m_icont.data<uint16_t>();
		auto props = geometry::QuadStrip(num_quads, verts, width, num_normals, normals, num_colours, colours,
			[&](v4_cref<> p, Colour32 c, v4_cref<> n, v2_cref<> t) { SetPCNT(*vptr++, p, Colour(c), n, t); },
			[&](int idx) { *iptr++ = s_cast<uint16_t>(idx); });

		// Create a nugget
		cache.AddNugget(ETopo::TriStrip, props.m_geom, props.m_has_alpha, false, mat);
		cache.m_bbox = props.m_bbox;

		// Create the model
		return Create(rdr, cache);
	}
	ModelPtr ModelGenerator::QuadPatch(Renderer& rdr, int dimx, int dimy, NuggetData const* mat)
	{
		// Calculate the required buffer sizes
		auto [vcount, icount] = geometry::QuadPatchSize(dimx, dimy);

		// Generate the geometry
		Cache cache{vcount, icount, 0, sizeof(uint16_t)};
		auto vptr = cache.m_vcont.data();
		auto iptr = cache.m_icont.data<uint16_t>();
		auto props = geometry::QuadPatch(dimx, dimy,
			[&](v4_cref<> p, Colour32 c, v4_cref<> n, v2_cref<> t) { SetPCNT(*vptr++, p, Colour(c), n, t); },
			[&](int idx) { *iptr++ = s_cast<uint16_t>(idx); });

		// Create a nugget
		cache.AddNugget(ETopo::TriStrip, props.m_geom, props.m_has_alpha, false, mat);
		cache.m_bbox = props.m_bbox;

		// Create the model
		return Create(rdr, cache);
	}

	// Shape2d ****************************************************************************
	ModelPtr ModelGenerator::Ellipse(Renderer& rdr, float dimx, float dimy, bool solid, int facets, Colour32 colour, m4x4 const* o2w, NuggetData const* mat)
	{
		// Calculate the required buffer sizes
		auto [vcount, icount] = geometry::EllipseSize(solid, facets);

		// Generate the geometry
		Cache cache{vcount, icount, 0, sizeof(uint16_t)};
		auto vptr = cache.m_vcont.data();
		auto iptr = cache.m_icont.data<uint16_t>();
		auto props = geometry::Ellipse(dimx, dimy, solid, facets, colour,
			[&](v4_cref<> p, Colour32 c, v4_cref<> n, v2_cref<> t) { SetPCNT(*vptr++, p, Colour(c), n, t); },
			[&](int idx) { *iptr++ = s_cast<uint16_t>(idx); });

		// Create a nugget
		cache.AddNugget(solid ? ETopo::TriStrip : ETopo::LineStrip, props.m_geom, props.m_has_alpha, false, mat);
		cache.m_bbox = props.m_bbox;

		// Create the model
		CreateOptions opts = {.m_bake = o2w};
		return Create(rdr, cache, opts);
	}
	ModelPtr ModelGenerator::Pie(Renderer& rdr, float dimx, float dimy, float ang0, float ang1, float radius0, float radius1, bool solid, int facets, Colour32 colour, m4x4 const* o2w, NuggetData const* mat)
	{
		// Calculate the required buffer sizes
		auto [vcount, icount] = geometry::PieSize(solid, ang0, ang1, facets);

		// Generate the geometry
		Cache cache{vcount, icount, 0, sizeof(uint16_t)};
		auto vptr = cache.m_vcont.data();
		auto iptr = cache.m_icont.data<uint16_t>();
		auto props = geometry::Pie(dimx, dimy, ang0, ang1, radius0, radius1, solid, facets, colour,
			[&](v4_cref<> p, Colour32 c, v4_cref<> n, v2_cref<> t) { SetPCNT(*vptr++, p, Colour(c), n, t); },
			[&](int idx) { *iptr++ = s_cast<uint16_t>(idx); });

		// Create a nugget
		cache.AddNugget(solid ? ETopo::TriStrip : ETopo::LineStrip, props.m_geom, props.m_has_alpha, false, mat);
		cache.m_bbox = props.m_bbox;

		// Create the model
		CreateOptions opts = {.m_bake = o2w};
		return Create(rdr, cache, opts);
	}
	ModelPtr ModelGenerator::RoundedRectangle(Renderer& rdr, float dimx, float dimy, float corner_radius, bool solid, int facets, Colour32 colour, m4x4 const* o2w, NuggetData const* mat)
	{
		// Calculate the required buffer sizes
		auto [vcount, icount] = geometry::RoundedRectangleSize(solid, corner_radius, facets);

		// Generate the geometry
		Cache cache{vcount, icount, 0, sizeof(uint16_t)};
		auto vptr = cache.m_vcont.data();
		auto iptr = cache.m_icont.data<uint16_t>();
		auto props = geometry::RoundedRectangle(dimx, dimy, solid, corner_radius, facets, colour,
			[&](v4_cref<> p, Colour32 c, v4_cref<> n, v2_cref<> t) { SetPCNT(*vptr++, p, Colour(c), n, t); },
			[&](int idx) { *iptr++ = s_cast<uint16_t>(idx); });

		// Create a nugget
		cache.AddNugget(solid ? ETopo::TriStrip : ETopo::LineStrip, props.m_geom, props.m_has_alpha, false, mat);
		cache.m_bbox = props.m_bbox;

		// Create the model
		CreateOptions opts{.m_bake = o2w};
		return Create(rdr, cache, opts);
	}
	ModelPtr ModelGenerator::Polygon(Renderer& rdr, int num_points, v2 const* points, bool solid, int num_colours, Colour32 const* colours, m4x4 const* o2w, NuggetData const* mat)
	{
		// Calculate the required buffer sizes
		auto [vcount, icount] = geometry::PolygonSize(num_points, solid);

		// Generate the geometry
		Cache cache{vcount, icount, 0, sizeof(uint16_t)};
		auto vptr = cache.m_vcont.data();
		auto iptr = cache.m_icont.data<uint16_t>();
		auto props = geometry::Polygon(num_points, points, solid, num_colours, colours,
			[&](v4_cref<> p, Colour32 c, v4_cref<> n, v2_cref<> t) { SetPCNT(*vptr++, p, Colour(c), n, t); },
			[&](int idx) { *iptr++ = s_cast<uint16_t>(idx); });

		// Create a nugget
		cache.AddNugget(solid ? ETopo::TriList : ETopo::LineStrip, props.m_geom, props.m_has_alpha, false, mat);
		cache.m_bbox = props.m_bbox;

		// Create the model
		CreateOptions opts = {.m_bake = o2w};
		return Create(rdr, cache, opts);
	}

	// Boxes ******************************************************************************
	ModelPtr ModelGenerator::Boxes(Renderer& rdr, int num_boxes, v4 const* points, int num_colours, Colour32 const* colours, NuggetData const* mat)
	{
		// Calculate the required buffer sizes
		auto [vcount, icount] = geometry::BoxSize(num_boxes);

		// Generate the geometry
		Cache cache{vcount, icount, 0, sizeof(uint16_t)};
		auto vptr = cache.m_vcont.data();
		auto iptr = cache.m_icont.data<uint16_t>();
		auto props = geometry::Boxes(num_boxes, points, num_colours, colours,
			[&](v4_cref<> p, Colour32 c, v4_cref<> n, v2_cref<> t) { SetPCNT(*vptr++, p, Colour(c), n, t); },
			[&](int idx) { *iptr++ = s_cast<uint16_t>(idx); });

		// Create a nugget
		cache.AddNugget(ETopo::TriList, props.m_geom, props.m_has_alpha, false, mat);
		cache.m_bbox = props.m_bbox;

		// Create the model
		return Create(rdr, cache);
	}
	ModelPtr ModelGenerator::Boxes(Renderer& rdr, int num_boxes, v4 const* points, m4x4 const& o2w, int num_colours, Colour32 const* colours, NuggetData const* mat)
	{
		// Calculate the required buffer sizes
		auto [vcount, icount] = geometry::BoxSize(num_boxes);

		// Generate the geometry
		Cache cache(vcount, icount, 0, 2);
		auto vptr = cache.m_vcont.data();
		auto iptr = cache.m_icont.data<uint16_t>();
		auto props = geometry::Boxes(num_boxes, points, o2w, num_colours, colours,
			[&](v4_cref<> p, Colour32 c, v4_cref<> n, v2_cref<> t) { SetPCNT(*vptr++, p, Colour(c), n, t); },
			[&](int idx) { *iptr++ = s_cast<uint16_t>(idx); });

		// Create a nugget
		cache.AddNugget(ETopo::TriList, props.m_geom, props.m_has_alpha, false, mat);
		cache.m_bbox = props.m_bbox;

		// Create the model
		return Create(rdr, cache);
	}
	ModelPtr ModelGenerator::Box(Renderer& rdr, v4 const& rad, m4x4 const& o2w, Colour32 colour, NuggetData const* mat)
	{
		// Calculate the required buffer sizes
		auto [vcount, icount] = geometry::BoxSize(1);

		// Generate the geometry
		Cache cache{vcount, icount, 0, sizeof(uint16_t)};
		auto vptr = cache.m_vcont.data();
		auto iptr = cache.m_icont.data<uint16_t>();
		auto props = geometry::Box(rad, o2w, colour,
			[&](v4_cref<> p, Colour32 c, v4_cref<> n, v2_cref<> t) { SetPCNT(*vptr++, p, Colour(c), n, t); },
			[&](int idx) { *iptr++ = s_cast<uint16_t>(idx); });

		// Create a nugget
		cache.AddNugget(ETopo::TriList, props.m_geom, props.m_has_alpha, false, mat);
		cache.m_bbox = props.m_bbox;

		// Create the model
		return Create(rdr, cache);
	}
	ModelPtr ModelGenerator::Box(Renderer& rdr, float rad, m4x4 const& o2w, Colour32 colour, NuggetData const* mat)
	{
		return Box(rdr, v4(rad), o2w, colour, mat);
	}
	ModelPtr ModelGenerator::BoxList(Renderer& rdr, int num_boxes, v4 const* positions, v4 const& rad, int num_colours, Colour32 const* colours, NuggetData const* mat)
	{
		// Calculate the required buffer sizes
		auto [vcount, icount] = geometry::BoxSize(num_boxes);

		// Generate the geometry
		Cache cache(vcount, icount, 0, 2);
		auto vptr = cache.m_vcont.data();
		auto iptr = cache.m_icont.data<uint16_t>();
		auto props = geometry::BoxList(num_boxes, positions, rad, num_colours, colours,
			[&](v4_cref<> p, Colour32 c, v4_cref<> n, v2_cref<> t) { SetPCNT(*vptr++, p, Colour(c), n, t); },
			[&](int idx) { *iptr++ = s_cast<uint16_t>(idx); });

		// Create a nugget
		cache.AddNugget(ETopo::TriList, props.m_geom, props.m_has_alpha, false, mat);
		cache.m_bbox = props.m_bbox;

		// Create the model
		return Create(rdr, cache);
	}

	// Sphere *****************************************************************************
	ModelPtr ModelGenerator::Geosphere(Renderer& rdr, v4 const& radius, int divisions , Colour32 colour, NuggetData const* mat)
	{
		// Calculate the required buffer sizes
		auto [vcount, icount] = geometry::GeosphereSize(divisions);

		// Generate the geometry
		Cache cache{vcount, icount, 0, sizeof(uint16_t)};
		auto vptr = cache.m_vcont.data();
		auto iptr = cache.m_icont.data<uint16_t>();
		auto props = geometry::Geosphere(radius, divisions, colour,
			[&](v4_cref<> p, Colour32 c, v4_cref<> n, v2_cref<> t) { SetPCNT(*vptr++, p, Colour(c), n, t); },
			[&](int idx) { *iptr++ = s_cast<uint16_t>(idx); });

		// Create a nugget
		cache.AddNugget(ETopo::TriList, props.m_geom, props.m_has_alpha, false, mat);
		cache.m_bbox = props.m_bbox;

		// Create the model
		return Create(rdr, cache);
	}
	ModelPtr ModelGenerator::Geosphere(Renderer& rdr, float radius, int divisions, Colour32 colour, NuggetData const* mat)
	{
		return Geosphere(rdr, v4(radius, radius, radius, 0.0f), divisions, colour, mat);
	}
	ModelPtr ModelGenerator::Sphere(Renderer& rdr, v4 const& radius, int wedges, int layers, Colour32 colour, NuggetData const* mat)
	{
		// Calculate the required buffer sizes
		auto [vcount, icount] = geometry::SphereSize(wedges, layers);

		// Generate the geometry
		Cache cache(vcount, icount, 0, 2);
		auto vptr = cache.m_vcont.data();
		auto iptr = cache.m_icont.data<uint16_t>();
		auto props = geometry::Sphere(radius, wedges, layers, colour,
			[&](v4_cref<> p, Colour32 c, v4_cref<> n, v2_cref<> t) { SetPCNT(*vptr++, p, Colour(c), n, t); },
			[&](size_t idx) { *iptr++ = s_cast<uint16_t>(idx); });

		// Create a nugget
		cache.AddNugget(ETopo::TriList, props.m_geom, props.m_has_alpha, false, mat);
		cache.m_bbox = props.m_bbox;

		// Create the model
		return Create(rdr, cache);
	}
	ModelPtr ModelGenerator::Sphere(Renderer& rdr, float radius, int wedges, int layers, Colour32 colour, NuggetData const* mat)
	{
		return Sphere(rdr, v4{ radius, radius, radius, 0 }, wedges, layers, colour, mat);
	}

	// Cylinder ***************************************************************************
	ModelPtr ModelGenerator::Cylinder(Renderer& rdr, float radius0, float radius1, float height, float xscale, float yscale, int wedges, int layers, int num_colours, Colour32 const* colours, m4x4 const* o2w, NuggetData const* mat)
	{
		// Calculate the required buffer sizes
		auto [vcount, icount] = geometry::CylinderSize(wedges, layers);

		// Generate the geometry
		Cache cache{vcount, icount, 0, sizeof(uint16_t)};
		auto vptr = cache.m_vcont.data();
		auto iptr = cache.m_icont.data<uint16_t>();
		auto props = geometry::Cylinder(radius0, radius1, height, xscale, yscale, wedges, layers, num_colours, colours,
			[&](v4_cref<> p, Colour32 c, v4_cref<> n, v2_cref<> t) { SetPCNT(*vptr++, p, Colour(c), n, t); },
			[&](int idx) { *iptr++ = s_cast<uint16_t>(idx); });

		// Create a nugget
		cache.AddNugget(ETopo::TriList, props.m_geom, props.m_has_alpha, false, mat);
		cache.m_bbox = props.m_bbox;

		// Create the model
		CreateOptions opts = {.m_bake = o2w};
		return Create(rdr, cache, opts);
	}

	// Extrude ****************************************************************************
	ModelPtr ModelGenerator::Extrude(Renderer& rdr, int cs_count, v2 const* cs, int path_count, v4 const* path, bool closed, bool smooth_cs, int num_colours, Colour32 const* colours, m4x4 const* o2w, NuggetData const* mat)
	{
		assert(path_count >= 2);

		// Calculate the required buffer sizes
		auto [vcount, icount] = geometry::ExtrudeSize(cs_count, path_count, closed, smooth_cs);

		// Convert a stream of points into a stream of transforms
		// At each vertex, ori.z should be the tangent to the extrusion path.
		int p = -1;
		auto ori = m4x4Identity;
		auto yaxis = Perpendicular(path[1] - path[0], v4YAxis);
		auto make_path = [&]
		{
			++p;
			if (p == 0)
			{
				auto tang = path[1] - path[0];
				if (!FEql(tang, v4Zero))
				{
					yaxis = Perpendicular(tang, yaxis);
					ori.rot = OriFromDir(tang, AxisId::PosZ, yaxis);
				}
			}
			else if (p == path_count - 1)
			{
				auto tang = path[p] - path[p - 1];
				if (!FEql(tang, v4Zero))
				{
					yaxis = Perpendicular(tang, yaxis);
					ori.rot = OriFromDir(tang, AxisId::PosZ, yaxis);
				}
			}
			else
			{
				auto a = Normalise(path[p] - path[p - 1], v4Zero);
				auto b = Normalise(path[p + 1] - path[p], v4Zero);
				auto tang = a + b;
				if (!FEql(tang, v4Zero))
				{
					yaxis = Perpendicular(tang, yaxis);
					ori.rot = OriFromDir(tang, AxisId::PosZ, yaxis);
				}
			}
			ori.pos = path[p];
			return ori;
		};

		// Generate the geometry
		Cache cache{vcount, icount, 0, sizeof(uint16_t)};
		auto vptr = cache.m_vcont.data();
		auto iptr = cache.m_icont.data<uint16_t>();
		auto props = geometry::Extrude(cs_count, cs, path_count, make_path, closed, smooth_cs, num_colours, colours,
			[&](v4_cref<> p, Colour32 c, v4_cref<> n, v2_cref<> t) { SetPCNT(*vptr++, p, Colour(c), n, t); },
			[&](int idx) { *iptr++ = s_cast<uint16_t>(idx); });

		// Create a nugget
		cache.AddNugget(ETopo::TriList, props.m_geom, props.m_has_alpha, false, mat);
		cache.m_bbox = props.m_bbox;

		// Create the model
		CreateOptions opts = {.m_bake = o2w};
		return Create(rdr, cache, opts);
	}
	ModelPtr ModelGenerator::Extrude(Renderer& rdr, int cs_count, v2 const* cs, int path_count, m4x4 const* path, bool closed, bool smooth_cs, int num_colours, Colour32 const* colours, m4x4 const* o2w, NuggetData const* mat)
	{
		// Calculate the required buffer sizes
		auto [vcount, icount] = geometry::ExtrudeSize(cs_count, path_count, closed, smooth_cs);

		// Path transform stream source
		auto p = path;
		auto make_path = [&] { return *p++; };

		// Generate the geometry
		Cache cache{vcount, icount, 0, sizeof(uint16_t)};
		auto vptr = cache.m_vcont.data();
		auto iptr = cache.m_icont.data<uint16_t>();
		auto props = geometry::Extrude(cs_count, cs, path_count, make_path, closed, smooth_cs, num_colours, colours,
			[&](v4_cref<> p, Colour32 c, v4_cref<> n, v2_cref<> t) { SetPCNT(*vptr++, p, Colour(c), n, t); },
			[&](size_t idx) { *iptr++ = s_cast<uint16_t>(idx); });

		// Create a nugget
		cache.AddNugget(ETopo::TriList, props.m_geom, props.m_has_alpha, false, mat);
		cache.m_bbox = props.m_bbox;

		// Create the model
		CreateOptions opts = {.m_bake = o2w};
		return Create(rdr, cache, opts);
	}

	// Mesh *******************************************************************************
	ModelPtr ModelGenerator::Mesh(Renderer& rdr, MeshCreationData const& cdata)
	{
		// Calculate the required buffer sizes
		auto [vcount, icount] = geometry::MeshSize(cdata.m_vcount, cdata.m_icount);

		// Generate the geometry
		Cache cache{vcount, icount, 0, sizeof(uint16_t)};
		auto vptr = cache.m_vcont.data();
		auto iptr = cache.m_icont.data<uint16_t>();
		auto props = geometry::Mesh(
			cdata.m_vcount, cdata.m_icount,
			cdata.m_verts, cdata.m_indices,
			cdata.m_ccount, cdata.m_colours,
			cdata.m_ncount, cdata.m_normals,
			cdata.m_tex_coords,
			[&](v4_cref<> p, Colour32 c, v4_cref<> n, v2_cref<> t) { SetPCNT(*vptr++, p, Colour(c), n, t); },
			[&](int idx) { *iptr++ = s_cast<uint16_t>(idx); });

		// Create the nuggets
		cache.m_ncont.insert(cache.m_ncont.begin(), cdata.m_nuggets, cdata.m_nuggets + cdata.m_gcount);
		cache.m_bbox = props.m_bbox;

		// Create the model
		return Create(rdr, cache);
	}

	// SkyBox *****************************************************************************
	ModelPtr ModelGenerator::SkyboxGeosphere(Renderer& rdr, Texture2DPtr sky_texture, float radius, int divisions, Colour32 colour)
	{
		// Calculate the required buffer sizes
		auto [vcount, icount] = geometry::SkyboxGeosphereSize(divisions);

		// Generate the geometry
		Cache cache{vcount, icount, 0, sizeof(uint16_t)};
		auto vptr = cache.m_vcont.data();
		auto iptr = cache.m_icont.data<uint16_t>();
		auto props = geometry::SkyboxGeosphere(radius, divisions, colour,
			[&](v4_cref<> p, Colour32 c, v4_cref<> n, v2_cref<> t) { SetPCNT(*vptr++, p, Colour(c), n, t); },
			[&](size_t idx) { *iptr++ = s_cast<uint16_t>(idx); });

		// Model nugget properties for the sky box
		NuggetData mat;
		mat.m_tex_diffuse = sky_texture;
		mat.m_pso.Set<EPipeState::CullMode>(D3D12_CULL_MODE_FRONT);
		cache.AddNugget(ETopo::TriList, props.m_geom, props.m_has_alpha, false, &mat);
		cache.m_bbox = props.m_bbox;

		// Create the model
		return Create(rdr, cache);
	}
	ModelPtr ModelGenerator::SkyboxGeosphere(Renderer& rdr, wchar_t const* texture_path, float radius, int divisions, Colour32 colour)
	{
		// One texture per nugget
		TextureDesc desc(AutoId, ResDesc(), false, 0, "skybox");
		auto tex = rdr.res_mgr().CreateTexture2D(texture_path, desc);
		return SkyboxGeosphere(rdr, tex, radius, divisions, colour);
	}
	ModelPtr ModelGenerator::SkyboxFiveSidedCube(Renderer& rdr, Texture2DPtr sky_texture, float radius, Colour32 colour)
	{
		// Calculate the required buffer sizes
		auto [vcount, icount] = geometry::SkyboxFiveSidedCubicDomeSize();

		// Generate the geometry
		Cache cache(vcount, icount, 0, sizeof(uint16_t));
		auto vptr = cache.m_vcont.data();
		auto iptr = cache.m_icont.data<uint16_t>();
		auto props = geometry::SkyboxFiveSidedCubicDome(radius, colour,
			[&](v4_cref<> p, Colour32 c, v4_cref<> n, v2_cref<> t) { SetPCNT(*vptr++, p, Colour(c), n, t); },
			[&](size_t idx) { *iptr++ = s_cast<uint16_t>(idx); });

		// Model nugget properties for the sky box
		NuggetData mat;
		mat.m_tex_diffuse = sky_texture;
		mat.m_pso.Set<EPipeState::CullMode>(D3D12_CULL_MODE_FRONT);
		cache.AddNugget(ETopo::TriList, props.m_geom, props.m_has_alpha, false, &mat);
		cache.m_bbox = props.m_bbox;

		// Create the model
		return Create(rdr, cache);
	}
	ModelPtr ModelGenerator::SkyboxFiveSidedCube(Renderer& rdr, wchar_t const* texture_path, float radius, Colour32 colour)
	{
		// One texture per nugget
		TextureDesc desc(AutoId, ResDesc(), false, 0, "skybox");
		auto tex = rdr.res_mgr().CreateTexture2D(texture_path, desc);
		return SkyboxFiveSidedCube(rdr, tex, radius, colour);
	}
	ModelPtr ModelGenerator::SkyboxSixSidedCube(Renderer& rdr, Texture2DPtr (&sky_texture)[6], float radius, Colour32 colour)
	{
		// Calculate the required buffer sizes
		auto [vcount, icount] = geometry::SkyboxSixSidedCubeSize();

		// Generate the geometry
		Cache cache(vcount, icount, 0, sizeof(uint16_t));
		auto vptr = cache.m_vcont.data();
		auto iptr = cache.m_icont.data<uint16_t>();
		auto props = geometry::SkyboxSixSidedCube(radius, colour,
			[&](v4_cref<> p, Colour32 c, v4_cref<> n, v2_cref<> t) { SetPCNT(*vptr++, p, Colour(c), n, t); },
			[&](size_t idx) { *iptr++ = s_cast<uint16_t>(idx); });

		// Create the nuggets, one per face. Expected order: +X, -X, +Y, -Y, +Z, -Z
		for (int i = 0; i != 6; ++i)
		{
			// Create the render nugget for this face of the sky box
			NuggetData mat = {};
			mat.m_tex_diffuse = sky_texture[i];
			mat.m_pso.Set<EPipeState::CullMode>(D3D12_CULL_MODE_FRONT);
			mat.m_vrange = Range(i * 4, (i + 1) * 4);
			mat.m_irange = Range(i * 6, (i + 1) * 6);
			cache.AddNugget(ETopo::TriList, props.m_geom, props.m_has_alpha, &mat);
		}
		cache.m_bbox = props.m_bbox;

		// Create the model
		return Create(rdr, cache);
	}
	ModelPtr ModelGenerator::SkyboxSixSidedCube(Renderer& rdr, wchar_t const* texture_path_pattern, float radius, Colour32 colour)
	{
		wstring256 tpath = texture_path_pattern;
		auto ofs = tpath.find(L"??");
		if (ofs == std::string::npos)
			throw std::runtime_error(FmtS("Skybox texture path '%S' does not include '??' characters", texture_path_pattern));

		Texture2DPtr tex[6] = {}; int i = 0;
		for (auto face : { L"+X", L"-X", L"+Y", L"-Y", L"+Z", L"-Z" })
		{
			// Load the texture for this face of the sky box
			tpath[ofs + 0] = face[0];
			tpath[ofs + 1] = face[1];
			TextureDesc desc(AutoId, ResDesc(), false, 0, "skybox");
			tex[i++] = rdr.res_mgr().CreateTexture2D(tpath.c_str(), desc);
		}

		return SkyboxSixSidedCube(rdr, tex, radius, colour);
	}

	// ModelFile **************************************************************************
	// Load a P3D model from a stream, emitting models for each mesh via 'out'.
	// bool out(span<ModelTreeNode> tree) - return true to stop loading, false to get the next model
	void ModelGenerator::LoadP3DModel(Renderer& rdr, std::istream& src, ModelOutFunc out, CreateOptions const& opts)
	{
		using namespace geometry;

		// Model output helpers
		struct Mat :p3d::Material
		{
			Renderer& m_rdr;
			mutable Texture2DPtr m_tex_diffuse;

			Mat(Renderer& rdr, p3d::Material&& m)
				:p3d::Material(std::forward<p3d::Material>(m))
				,m_rdr(rdr)
				,m_tex_diffuse()
			{}

			// The material base colour
			Colour32 Tint() const
			{
				return this->m_diffuse.argb();
			}

			// The diffuse texture resolved to a renderer texture resource
			Texture2DPtr TexDiffuse() const
			{
				// Lazy load the texture
				if (m_tex_diffuse == nullptr)
				{
					for (auto& tex : m_textures)
					{
						if (tex.m_type != p3d::Texture::EType::Diffuse)
							continue;
						
						TextureDesc desc(AutoId, ResDesc(), AllSet(tex.m_flags, p3d::Texture::EFlags::Alpha), 0, tex.m_filepath.c_str());
						m_tex_diffuse = m_rdr.res_mgr().CreateTexture2D(tex.m_filepath.c_str(), desc);
						break;
					}
				}
				return m_tex_diffuse;
			}
		};
		struct ModelOut
		{
			// Notes:
			//  - Each 'mesh' can contain nested child meshes.
			//    Create models for each and emit the tree structure of models
			using MatCont = std::vector<Mat>;

			Renderer& m_rdr;
			CreateOptions const& m_opts;
			ModelOutFunc m_out;
			Cache<> m_cache;
			MatCont m_mats;

			ModelOut(Renderer& rdr, CreateOptions const& opts, ModelOutFunc out)
				:m_rdr(rdr)
				,m_opts(opts)
				,m_out(out)
				,m_cache(0, 0, 0, sizeof(uint32_t))
				,m_mats()
			{}

			// Functor called from 'ExtractMaterials'
			bool operator ()(p3d::Material&& mat)
			{
				m_mats.push_back(Mat(m_rdr, std::forward<p3d::Material>(mat)));
				return false;
			}

			// Functor called from 'ExtractMeshes'
			bool operator()(p3d::Mesh&& mesh)
			{
				ModelTree tree;
				BuildModelTree(tree, mesh, 0);
				return m_out(tree);
			}

			// Recursive function for populating a model tree
			void BuildModelTree(ModelTree& tree, p3d::Mesh const& mesh, int level)
			{
				tree.push_back({MeshToModel(mesh), mesh.m_o2p, level});
				for (auto& child : mesh.m_children)
					BuildModelTree(tree, child, level + 1);
			}

			// Convert a p3d::Mesh into a rdr::Model
			ModelPtr MeshToModel(p3d::Mesh const& mesh)
			{
				m_cache.Reset();

				// Name/Bounding box
				m_cache.m_name = mesh.m_name;
				m_cache.m_bbox = mesh.m_bbox;

				// Copy the verts
				m_cache.m_vcont.resize(mesh.vcount());
				auto vptr = m_cache.m_vcont.data();
				for (auto mvert : mesh.fat_verts())
				{
					SetPCNT(*vptr, GetP(mvert), GetC(mvert), GetN(mvert), GetT(mvert));
					++vptr;
				}

				// Copy nuggets
				m_cache.m_icont.m_stride = sizeof(uint32_t);
				m_cache.m_icont.resize(mesh.icount() * m_cache.m_icont.stride());
				m_cache.m_ncont.reserve(mesh.ncount());
				auto iptr = m_cache.m_icont.data<uint32_t>();
				auto vrange = Range::Zero();
				auto irange = Range::Zero();
				for (auto& nug : mesh.nuggets())
				{
					// Copy the indices
					vrange = Range::Reset();
					for (auto i : nug.indices())
					{
						vrange.grow(i);
						*iptr++ = i;
					}

					// The index range in the model buffer
					irange.m_beg = irange.m_end;
					irange.m_end = irange.m_beg + nug.icount();

					// The basic nugget
					NuggetData nugget(nug.m_topo, nug.m_geom, vrange, irange);

					// Resolve the material
					for (auto& m : m_mats)
					{
						if (nug.m_mat != m.m_id) continue;
						nugget.m_tex_diffuse = m.TexDiffuse();
						nugget.m_tint = m.Tint();
						nugget.m_nflags = SetBits(nugget.m_nflags, ENuggetFlag::TintHasAlpha, nugget.m_tint.a != 0xff);
						break;
					}

					// Add a nugget
					m_cache.m_ncont.emplace_back(std::move(nugget));
				}

				// Return the renderer model
				return Create(m_rdr, m_cache, m_opts);
			}
		};
		ModelOut model_out(rdr, opts, out);

		// Material read from the p3d model, extended with associated renderer resources.
		p3d::ExtractMaterials<std::istream, ModelOut&>(src, model_out);

		// Load each mesh in the P3D stream and emit it as a model
		p3d::ExtractMeshes<std::istream, ModelOut&>(src, model_out);
	}
	void ModelGenerator::Load3DSModel(Renderer& rdr, std::istream& src, ModelOutFunc out, CreateOptions const& opts)
	{
		using namespace geometry;

		// 3DS Material read from the 3ds model, extended with associated renderer resources
		struct Mat : max_3ds::Material
		{
			mutable Texture2DPtr m_tex_diffuse;

			Mat() {}
			Mat(max_3ds::Material&& m)
				:max_3ds::Material(std::forward<max_3ds::Material>(m))
				,m_tex_diffuse()
			{}

			// The material base colour
			Colour32 Tint() const
			{
				return this->m_diffuse.argb();
			}

			// The diffuse texture resolved to a renderer texture resource
			Texture2DPtr TexDiffuse(Renderer& rdr) const
			{
				if (m_tex_diffuse == nullptr && !m_textures.empty())
				{
					auto& tex = m_textures[0];
					TextureDesc desc(AutoId, ResDesc(), false, 0, tex.m_filepath.c_str());
					m_tex_diffuse = rdr.res_mgr().CreateTexture2D(tex.m_filepath.c_str(), desc);

					// todo
					//SamplerDesc sam_desc{
					//	tex.m_tiling == 0 ? D3D11_TEXTURE_ADDRESS_CLAMP :
					//	tex.m_tiling == 1 ? D3D11_TEXTURE_ADDRESS_WRAP :
					//	D3D11_TEXTURE_ADDRESS_BORDER,
					//	D3D11_FILTER_ANISOTROPIC};
				}
				return m_tex_diffuse;
			}
		};
		std::vector<Mat> mats;
		max_3ds::ReadMaterials(src, [&](max_3ds::Material&& mat)
		{
			mats.emplace_back(std::forward<max_3ds::Material>(mat));
			return false; 
		});

		// Load each mesh in the stream and emit it as a model
		Cache cache{0, 0, 0, sizeof(uint16_t)};
		max_3ds::ReadObjects(src, [&](max_3ds::Object&& obj)
			{
				cache.Reset();

				// Name/Bounding box
				cache.m_name = obj.m_name;
				cache.m_bbox = BBox::Reset();

				// Populate 'cache' from the 3DS data
				auto vout = [&](v4 const& p, Colour const& c, v4 const& n, v2 const& t)
				{
					Vert vert;
					SetPCNT(vert, cache.m_bbox.Grow(p), c, n, t);
					cache.m_vcont.push_back(vert);
				};
				auto iout = [&](uint16_t i0, uint16_t i1, uint16_t i2)
				{
					cache.m_icont.push_back<uint16_t>(i0);
					cache.m_icont.push_back<uint16_t>(i1);
					cache.m_icont.push_back<uint16_t>(i2);
				};
				auto nout = [&](ETopo topo, EGeom geom, Mat const& mat, Range vrange, Range irange)
				{
					NuggetData nugget(topo, geom, vrange, irange);
					nugget.m_tex_diffuse = mat.TexDiffuse(rdr);
					nugget.m_tint = mat.Tint();
					nugget.m_nflags = SetBits(nugget.m_nflags, ENuggetFlag::TintHasAlpha, nugget.m_tint.a != 0xff);
					cache.m_ncont.push_back(nugget);
				};
				auto matlookup = [&](std::string const& name)
				{
					for (auto& mat : mats)
					{
						if (mat.m_name != name) continue;
						return mat;
					}
					return Mat{};
				};
				max_3ds::CreateModel(obj, matlookup, vout, iout, nout);

				// Emit the model. 'out' returns true to stop searching
				// 3DS models cannot nest, so each 'Model Tree' is one root node only
				auto model = Create(rdr, cache, opts);
				auto tree = ModelTree{{model, obj.m_mesh.m_o2p, 0}};
				return out(tree);
			});
	}
	void ModelGenerator::LoadSTLModel(Renderer& rdr, std::istream& src, ModelOutFunc out, CreateOptions const& opts)
	{
		using namespace geometry;

		// Parse the model file in the STL stream
		Cache cache{0, 0, 0, sizeof(uint16_t)};
		stl::Read(src, stl::Options{}, [&](stl::Model&& mesh)
			{
				cache.Reset();

				// Name/Bounding box
				cache.m_name = mesh.m_header;
				cache.m_bbox = BBox::Reset();

				// Copy the verts
				cache.m_vcont.resize(mesh.m_verts.size());
				auto vptr = cache.m_vcont.data();
				for (int i = 0, iend = int(mesh.m_verts.size()); i != iend; ++i)
				{
					SetPCNT(*vptr, cache.m_bbox.Grow(mesh.m_verts[i]), ColourWhite, mesh.m_norms[i/3], v2Zero);
					++vptr;
				}

				// Copy nuggets
				auto vcount = int(cache.m_vcont.size());
				if (vcount < 0x10000)
				{
					// Use 16bit indices
					cache.m_icont.m_stride = sizeof(uint16_t);
					cache.m_icont.resize<uint16_t>(vcount);
					auto ibuf = cache.m_icont.data<uint16_t>();
					for (uint16_t i = 0; vcount-- != 0;)
						*ibuf++ = i++;
				}
				else
				{
					// Use 32bit indices
					cache.m_icont.m_stride = sizeof(uint32_t);
					cache.m_icont.resize<uint32_t>(vcount);
					auto ibuf = cache.m_icont.data<uint32_t>();
					for (uint32_t i = 0; vcount-- != 0;)
						*ibuf++ = i++;
				}
				cache.AddNugget(ETopo::TriList, EGeom::Vert|EGeom::Norm, false, false);

				// Emit the model. 'out' returns true to stop searching
				// 3DS models cannot nest, so each 'Model Tree' is one root node only
				auto model = Create(rdr, cache, opts);
				auto tree = ModelTree{{model, m4x4Identity, 0}};
				return out(tree);
			});
	}
	void ModelGenerator::LoadModel(geometry::EModelFileFormat format, Renderer& rdr, std::istream& src, ModelOutFunc mout, CreateOptions const& opts)
	{
		using namespace geometry;
		switch (format)
		{
			case EModelFileFormat::P3D:    LoadP3DModel(rdr, src, mout, opts); break;
			case EModelFileFormat::Max3DS: Load3DSModel(rdr, src, mout, opts); break;
			case EModelFileFormat::STL:    LoadSTLModel(rdr, src, mout, opts); break;
			default: throw std::runtime_error("Unsupported model file format");
		}
	}

	// Text *******************************************************************************
	// Create a quad containing text.
	// 'text' is the complete text to render into the quad.
	// 'formatting' defines regions in the text to apply formatting to.
	// 'formatting_count' is the length of the 'formatting' array.
	// 'layout' is global text layout information.
	ModelPtr ModelGenerator::Text(Renderer& rdr, wstring256 const& text, TextFormat const* formatting, int formatting_count, TextLayout const& layout, AxisId axis_id, v4& dim_out, m4x4 const* bake)
	{
		// Texture sizes are in physical pixels, but D2D operates in DIP so we need to determine
		// the size in physical pixels on this device that correspond to the returned metrics.
		// From: https://msdn.microsoft.com/en-us/library/windows/desktop/ff684173%28v=vs.85%29.aspx?f=255&MSPPError=-2147217396
		// "Direct2D automatically performs scaling to match the DPI setting.
		//  In Direct2D, coordinates are measured in units called device-independent pixels (DIPs).
		//  A DIP is defined as 1/96th of a logical inch. In Direct2D, all drawing operations are
		//  specified in DIPs and then scaled to the current DPI setting."
		D3DPtr<IDWriteFactory> dwrite;
		Throw(DWriteCreateFactory(DWRITE_FACTORY_TYPE_SHARED, __uuidof(IDWriteFactory), (IUnknown**)&dwrite.m_ptr));

		// Get the default format
		auto def = formatting_count != 0 && formatting[0].empty() ? formatting[0] : TextFormat();

		// Determine of the model requires alpha blending.
		// Consider alpha = 0 as not requiring blending, Alpha clip will be used instead
		auto has_alpha = HasAlpha(layout.m_bk_colour) || HasAlpha(def.m_font.m_colour);

		// Create the default font
		D3DPtr<IDWriteTextFormat> text_format;
		Throw(dwrite->CreateTextFormat(def.m_font.m_name.c_str(), nullptr, def.m_font.m_weight, def.m_font.m_style, def.m_font.m_stretch, def.m_font.m_size, L"en-US", &text_format.m_ptr));

		// Create a text layout interface
		D3DPtr<IDWriteTextLayout> text_layout;
		Throw(dwrite->CreateTextLayout(text.data(), UINT32(text.size()), text_format.get(), layout.m_dim.x, layout.m_dim.y, &text_layout.m_ptr));
		text_layout->SetTextAlignment(layout.m_align_h);
		text_layout->SetParagraphAlignment(layout.m_align_v);
		text_layout->SetWordWrapping(layout.m_word_wrapping);

		// Apply the formatting
		auto fmtting = std::initializer_list<TextFormat>(formatting, formatting + formatting_count);
		for (auto& fmt : fmtting)
		{
			// A null range can be used to set the default font/style for the whole string
			if (fmt.empty())
				continue;

			// Font changes
			if (fmt.m_font.m_name      != def.m_font.m_name      ) text_layout->SetFontFamilyName(fmt.m_font.m_name.c_str() , fmt.m_range);
			if (fmt.m_font.m_size      != def.m_font.m_size      ) text_layout->SetFontSize      (fmt.m_font.m_size         , fmt.m_range);
			if (fmt.m_font.m_weight    != def.m_font.m_weight    ) text_layout->SetFontWeight    (fmt.m_font.m_weight       , fmt.m_range);
			if (fmt.m_font.m_style     != def.m_font.m_style     ) text_layout->SetFontStyle     (fmt.m_font.m_style        , fmt.m_range);
			if (fmt.m_font.m_stretch   != def.m_font.m_stretch   ) text_layout->SetFontStretch   (fmt.m_font.m_stretch      , fmt.m_range);
			if (fmt.m_font.m_underline != def.m_font.m_underline ) text_layout->SetUnderline     (fmt.m_font.m_underline    , fmt.m_range);
			if (fmt.m_font.m_strikeout != def.m_font.m_strikeout ) text_layout->SetStrikethrough (fmt.m_font.m_strikeout    , fmt.m_range);

			// Record if any of the text has alpha
			has_alpha |= HasAlpha(fmt.m_font.m_colour);
		}

		// Measure the formatted text
		DWRITE_TEXT_METRICS metrics;
		Throw(text_layout->GetMetrics(&metrics));

		// The size of the text in device independent pixels, including padding
		auto dip_size = v2(
			metrics.widthIncludingTrailingWhitespace + layout.m_padding.left + layout.m_padding.right,
			metrics.height + layout.m_padding.top + layout.m_padding.bottom);

		// Determine the required texture size
		auto text_size = dip_size;
		auto texture_size = Ceil(text_size) * 2;

		// Create a texture large enough to contain the text, and render the text into it
		Image img(static_cast<int>(texture_size.x), static_cast<int>(texture_size.y), nullptr, DXGI_FORMAT_B8G8R8A8_UNORM);
		auto tdesc = ResDesc::Tex2D(img, 1, EUsage::RenderTarget);// | EUsage::SimultaneousAccess);
		//tdesc.HeapFlags = D3D12_HEAP_FLAG_SHARED;
		TextureDesc desc(AutoId, tdesc, has_alpha, 0, "text_quad");
		auto tex = rdr.res_mgr().CreateTexture2D(desc);
		
		//todo SamDesc sdesc(D3D11_TEXTURE_ADDRESS_CLAMP, D3D11_FILTER_MIN_MAG_MIP_LINEAR);

		{// Get a D2D device context to draw on the texture
			auto dc = tex->GetD2DeviceContext();
			auto fr = To<D3DCOLORVALUE>(def.m_font.m_colour);
			auto bk = To<D3DCOLORVALUE>(layout.m_bk_colour);

			// Apply different colours to text ranges
			for (auto& fmt : fmtting)
			{
				if (fmt.empty()) continue;
				if (fmt.m_font.m_colour != def.m_font.m_colour)
				{
					D3DPtr<ID2D1SolidColorBrush> brush;
					Throw(dc->CreateSolidColorBrush(To<D3DCOLORVALUE>(fmt.m_font.m_colour), &brush.m_ptr));
					brush->SetOpacity(fmt.m_font.m_colour.a);

					// Apply the colour
					text_layout->SetDrawingEffect(brush.get(), fmt.m_range);
				}
			}

			// Create the default text colour brush
			D3DPtr<ID2D1SolidColorBrush> brush;
			Throw(dc->CreateSolidColorBrush(fr, &brush.m_ptr));
			brush->SetOpacity(def.m_font.m_colour.a);

			// Draw the string
			dc->BeginDraw();
			dc->Clear(&bk);
			dc->SetTextAntialiasMode(D2D1_TEXT_ANTIALIAS_MODE_ALIASED);
			dc->DrawTextLayout({layout.m_padding.left, layout.m_padding.top}, text_layout.get(), brush.get(), D2D1_DRAW_TEXT_OPTIONS_NONE);
			Throw(dc->EndDraw());
		}

		// Create a quad using this texture
		auto [vcount, icount] = geometry::QuadSize(1);

		// Return the size of the quad and the texture
		dim_out = v4(text_size, texture_size);

		// Set the texture coordinates to match the text metrics and the quad size
		auto t2q = m4x4::Scale(text_size.x/texture_size.x, text_size.y/texture_size.y, 1.0f, v4Origin) * m4x4(v4XAxis, -v4YAxis, v4ZAxis, v4(0, 1, 0, 1));

		// Generate the geometry
		Cache cache{vcount, icount, 0, sizeof(uint16_t)};
		auto vptr = cache.m_vcont.data();
		auto iptr = cache.m_icont.data<uint16_t>();
		auto props = geometry::Quad(axis_id, layout.m_anchor, text_size.x, text_size.y, iv2Zero, Colour32White, t2q,
			[&](v4_cref<> p, Colour32 c, v4_cref<> n, v2_cref<> t) { SetPCNT(*vptr++, p, Colour(c), n, t); },
			[&](int idx) { *iptr++ = s_cast<uint16_t>(idx); });

		// Create a nugget
		NuggetData mat(ETopo::TriList);
		mat.m_tex_diffuse = tex;
		cache.AddNugget(ETopo::TriList, props.m_geom & ~EGeom::Norm, props.m_has_alpha, false, &mat);
		cache.m_bbox = props.m_bbox;

		// Create the model
		CreateOptions opts = {.m_bake = bake};
		return Create(rdr, cache, opts);
	}
	ModelPtr ModelGenerator::Text(Renderer& rdr, wstring256 const& text, TextFormat const* formatting, int formatting_count, TextLayout const& layout, AxisId axis_id)
	{
		v4 dim_out;
		return Text(rdr, text, formatting, formatting_count, layout, axis_id, dim_out);
	}
	ModelPtr ModelGenerator::Text(Renderer& rdr, wstring256 const& text, TextFormat const& formatting, TextLayout const& layout, AxisId axis_id, v4& dim_out)
	{
		return Text(rdr, text, &formatting, 1, layout, axis_id, dim_out);
	}
	ModelPtr ModelGenerator::Text(Renderer& rdr, wstring256 const& text, TextFormat const& formatting, TextLayout const& layout, AxisId axis_id)
	{
		v4 dim_out;
		return Text(rdr, text, &formatting, 1, layout, axis_id, dim_out);
	}

}
