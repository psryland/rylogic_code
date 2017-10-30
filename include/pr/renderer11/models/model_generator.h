//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2007
//*********************************************
#pragma once

#include "pr/renderer11/forward.h"
#include "pr/renderer11/models/model.h"

namespace pr
{
	namespace rdr
	{
		// Parameters structure for creating mesh models
		struct MeshCreationData
		{
			int m_vcount; // The length of the 'verts' array
			int m_icount; // The length of the 'indices' array
			int m_gcount; // The length of the 'nuggets' array
			int m_ccount; // The length of the 'colours' array. 0, 1, or 'vcount'
			int m_ncount; // The length of the 'normals' array. 0, 1, or 'vcount'
			pr::v4               const* m_verts;      // The vertex data for the model
			pr::uint16           const* m_indices;    // The index data for the model
			pr::rdr::NuggetProps const* m_nuggets;    // The nugget data for the model
			pr::Colour32         const* m_colours;    // The colour data for the model. Typically nullptr, 1, or 'vcount' colours
			pr::v4               const* m_normals;    // The normal data for the model. Typically nullptr or a pointer to 'vcount' normals
			pr::v2               const* m_tex_coords; // The texture coordinates data for the model. nullptr or a pointer to 'vcount' texture coords

			MeshCreationData() :m_vcount() ,m_icount() ,m_gcount() ,m_ccount() ,m_ncount() ,m_verts() ,m_indices() ,m_nuggets() ,m_colours() ,m_normals() ,m_tex_coords() {}
			MeshCreationData& verts(pr::v4 const* vbuf, int count)
			{
				assert(count == 0 || vbuf != nullptr);
				assert(pr::maths::is_aligned(vbuf));
				m_vcount = count;
				m_verts = vbuf;
				return *this;
			}
			MeshCreationData& indices(pr::uint16 const* ibuf, int count)
			{
				assert(count == 0 || ibuf != nullptr);
				m_icount = count;
				m_indices = ibuf;
				return *this;
			}
			MeshCreationData& nuggets(pr::rdr::NuggetProps const* gbuf, int count)
			{
				assert(count == 0 || gbuf != nullptr);
				m_gcount = count;
				m_nuggets = gbuf;
				return *this;
			}
			MeshCreationData& colours(pr::Colour32 const* cbuf, int count)
			{
				// Count doesn't have to be 0, 1, or 'vcount' because interpolation is used
				assert(count == 0 || cbuf != nullptr);
				m_ccount = count;
				m_colours = cbuf;
				return *this;
			}
			MeshCreationData& normals(pr::v4 const* nbuf, int count)
			{
				// Count doesn't have to be 0, 1, or 'vcount' because interpolation is used
				assert(count == 0 || nbuf != nullptr);
				assert(pr::maths::is_aligned(nbuf));
				m_ncount = count;
				m_normals = nbuf;
				return *this;
			}
			MeshCreationData& tex(pr::v2 const* tbuf, int count)
			{
				// Count doesn't have to be 0, 1, or 'vcount' because interpolation is used
				assert(count == 0 || tbuf != nullptr);
				m_tex_coords = tbuf;
				(void)count;
				return *this;
			}

			MeshCreationData& verts(std::initializer_list<pr::v4> vbuf)
			{
				assert(pr::maths::is_aligned(vbuf.begin()));
				m_vcount = int(vbuf.size());
				m_verts = vbuf.begin();
				return *this;
			}
			MeshCreationData& indices(std::initializer_list<pr::uint16> ibuf)
			{
				m_icount = int(ibuf.size());
				m_indices = ibuf.begin();
				return *this;
			}
			MeshCreationData& nuggets(std::initializer_list<pr::rdr::NuggetProps> gbuf)
			{
				m_gcount = int(gbuf.size());
				m_nuggets = gbuf.begin();
				return *this;
			}
			MeshCreationData& colours(std::initializer_list<pr::Colour32> cbuf)
			{
				m_ccount = int(cbuf.size());
				m_colours = cbuf.begin();
				return *this;
			}
			MeshCreationData& normals(std::initializer_list<pr::v4> nbuf)
			{
				assert(int(nbuf.size()) == m_vcount);
				assert(pr::maths::is_aligned(nbuf.begin()));
				m_ncount = int(nbuf.size());
				m_normals = nbuf.begin();
				return *this;
			}
			MeshCreationData& tex(std::initializer_list<pr::v2> tbuf)
			{
				assert(int(tbuf.size()) == m_vcount);
				m_tex_coords = tbuf.begin();
				return *this;
			}

			template <int N> MeshCreationData& verts(pr::v4 const (&vbuf)[N])
			{
				return verts(&vbuf[0], N);
			}
			template <int N> MeshCreationData& indices(pr::uint16 const (&ibuf)[N])
			{
				return indices(&ibuf[0], N);
			}
			template <int N> MeshCreationData& nuggets(pr::rdr::NuggetProps const (&nbuf)[N])
			{
				return nuggets(&nbuf[0], N);
			}
			template <int N> MeshCreationData& colours(pr::Colour32 const (&cbuf)[N])
			{
				return colours(&cbuf[0], N);
			}
			template <int N> MeshCreationData& normals(pr::v4 const (&nbuf)[N])
			{
				return normals(&nbuf[0], N);
			}
			template <int N> MeshCreationData& tex(pr::v2 const (&tbuf)[N])
			{
				return tex(&tbuf[0], N);
			}
		};

		template <typename VType = Vert, typename IType = pr::uint16>
		struct ModelGenerator
		{
			using VCont = pr::vector<VType>;
			using ICont = pr::vector<IType>;
			using NCont = pr::vector<NuggetProps>;

			using VIter = typename VCont::iterator;
			using IIter = typename ICont::iterator;
			using NIter = typename NCont::iterator;

			class Cache
			{
				// The cached buffers
				struct Buffers :AlignTo<16>
				{
					std::string m_name;  // Model name
					VCont       m_vcont; // Model verts
					ICont       m_icont; // Model faces/lines/points/etc
					NCont       m_ncont; // Model nuggets
					BBox        m_bbox;  // Model bounding box
				};
				static Buffers& this_thread_instance()
				{
					// A static instance for this thread
					thread_local static Buffers* buffers;
					if (!buffers) buffers = new Buffers();
					return *buffers;
				}
				static bool& this_thread_cache_in_use()
				{
					thread_local static bool in_use;
					if (in_use) throw std::exception("Reentrant use of the model generator cache for this thread");
					return in_use;
				}
				Buffers& m_buffers;
				bool& m_in_use;

			public:

				std::string& m_name;  // Model name
				VCont&       m_vcont; // Model verts
				ICont&       m_icont; // Model faces/lines/points/etc
				NCont&       m_ncont; // Model nuggets
				BBox&        m_bbox;  // Model bounding box

				Cache(int vcount = 0, int icount = 0, int ncount = 0)
					:m_buffers(this_thread_instance())
					,m_in_use(this_thread_cache_in_use())
					,m_name(m_buffers.m_name)
					,m_vcont(m_buffers.m_vcont)
					,m_icont(m_buffers.m_icont)
					,m_ncont(m_buffers.m_ncont)
					,m_bbox(m_buffers.m_bbox)
				{
					assert(vcount >= 0 && icount >= 0 && ncount >= 0);
					m_vcont.resize(vcount);
					m_icont.resize(icount);
					m_ncont.resize(ncount);
					m_in_use = true;
				}
				~Cache()
				{
					Reset();
					m_in_use = false;
				}
				Cache(Cache const& rhs) = delete;
				Cache operator =(Cache const& rhs) = delete;
	
				// Resize all buffers to 0
				void Reset()
				{
					m_name.resize(0);
					m_vcont.resize(0);
					m_icont.resize(0);
					m_ncont.resize(0);
					m_bbox = BBoxReset;
				}

				// Add a nugget to 'm_ncont' (helper)
				void AddNugget(EPrim topo, EGeom geom, bool geometry_has_alpha, bool tint_has_alpha, NuggetProps const* mat = nullptr)
				{
					NuggetProps nug;
					if (mat) nug = *mat;
					nug.m_topo = topo;
					nug.m_geom = geom;
					nug.m_geometry_has_alpha |= geometry_has_alpha;
					nug.m_tint_has_alpha |= tint_has_alpha;
					m_ncont.push_back(nug);
				}
			};

			// Create a model from 'cont'
			// 'alpha' if true, the default nugget will have alpha enabled. Ignored if 'cont' contains nuggets
			// 'bake' is a transform to bake into the model
			// 'gen_normals' generates normals for the model if >= 0f. Value is the threshold for smoothing (in rad)
			static ModelPtr Create(Renderer& rdr, Cache& cache, m4x4 const* bake = nullptr, float gen_normals = -1.0f)
			{
				// Sanity check 'cache'
				#if PR_DBG_RDR
				PR_ASSERT(1, !cache.m_ncont.empty(), "No nuggets given");
				for (auto& nug : cache.m_ncont)
				{
					PR_ASSERT(1, nug.m_vrange.begin() < cache.m_vcont.size(), "Nugget range invalid");
					PR_ASSERT(1, nug.m_irange.begin() < cache.m_icont.size(), "Nugget range invalid");
					PR_ASSERT(1, nug.m_vrange.end()  <= cache.m_vcont.size(), "Nugget range invalid");
					PR_ASSERT(1, nug.m_irange.end()  <= cache.m_icont.size(), "Nugget range invalid");
				}
				#endif

				// Bake a transform into the model
				#pragma region Bake transform
				if (bake)
				{
					// Apply the 'bake' transform to every vertex
					cache.m_bbox = *bake * cache.m_bbox;
					for (auto& v : cache.m_vcont)
					{
						v.m_vert = *bake * v.m_vert;
						v.m_norm = *bake * v.m_norm;
					}

					// If the transform is left handed, flip the faces
					if (Determinant3(*bake) < 0)
					{
						// Check each nugget for faces
						for (auto& nug : cache.m_ncont)
						{
							switch (nug.m_topo)
							{
							case EPrim::TriList:
								{
									assert((nug.m_irange.size() % 3) == 0);
									for (size_t i = nug.m_irange.begin(), iend = nug.m_irange.end(); i != iend; i += 3)
										std::swap(cache.m_icont[i+1], cache.m_icont[i+2]);
									break;
								}
							case EPrim::TriStrip:
								{
									assert((nug.m_irange.size() % 2) == 0);
									for (size_t i = nug.m_irange.begin(), iend = nug.m_irange.end(); i != iend; i += 2)
										std::swap(cache.m_icont[i+0], cache.m_icont[i+1]);
									break;
								}
							}
						}
					}
				}
				#pragma endregion

				// Generate normals
				#pragma region Generate Normals
				if (gen_normals >= 0.0f)
				{
					// Check each nugget for faces
					for (auto& nug : cache.m_ncont)
					{
						switch (nug.m_topo)
						{
						case EPrim::TriList:
							{
								// Generate normals
								auto iout = std::begin(cache.m_icont) + nug.m_irange.begin();
								pr::geometry::GenerateNormals(
									nug.m_irange.size(), cache.m_icont.data() + nug.m_irange.begin(), gen_normals,
									[&](IType idx)
									{
										return GetP(cache.m_vcont[idx]);
									},
									cache.m_vcont.size(),
									[&](IType idx, IType orig, v4 const& norm)
									{
										if (idx >= cache.m_vcont.size()) cache.m_vcont.resize(idx + 1, cache.m_vcont[orig]);
										SetN(cache.m_vcont[idx], norm);
									},
									[&](IType i0, IType i1, IType i2)
									{
										*iout++ = i0;
										*iout++ = i1;
										*iout++ = i2;
									});
								break;
							}
						case EPrim::TriStrip:
							{
								throw std::exception("Generate normals isn't supported for TriStrip");
							}
						}
					}
				}
				#pragma endregion

				// Create the model
				VBufferDesc vb(cache.m_vcont.size(), cache.m_vcont.data());
				IBufferDesc ib(cache.m_icont.size(), cache.m_icont.data());
				auto model = rdr.m_mdl_mgr.CreateModel(MdlSettings(vb, ib, cache.m_bbox));
				model->m_name = cache.m_name;

				// Create the render nuggets
				for (auto& nug : cache.m_ncont)
				{
					// If the model geom has valid texture data but no texture, use white
					if (AllSet(nug.m_geom, EGeom::Tex0) && nug.m_tex_diffuse == nullptr)
						nug.m_tex_diffuse = rdr.m_tex_mgr.FindTexture(EStockTexture::White);

					// Create the nugget
					model->CreateNugget(nug);
				}

				// Return the freshly minted model.
				return model;
			}

			// Points/Sprites *********************************************************************
			static ModelPtr Points(Renderer& rdr, int num_points, v4 const* points, int num_colours = 0, Colour32 const* colours = nullptr, NuggetProps const* mat = nullptr)
			{
				// Calculate the required buffer sizes
				auto vcount = num_points;
				auto icount = num_points;

				// Generate the geometry
				Cache cache(vcount, icount);
				auto props = pr::geometry::Points(num_points, points, num_colours, colours, std::begin(cache.m_vcont), std::begin(cache.m_icont));
				cache.m_bbox = props.m_bbox;
				cache.AddNugget(EPrim::PointList, props.m_geom, props.m_has_alpha, false, mat);

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
			static ModelPtr Lines(Renderer& rdr, int num_lines, v4 const* points, int num_colours = 0, Colour32 const* colours = nullptr, NuggetProps const* mat = nullptr)
			{
				// Calculate the required buffer sizes
				int vcount, icount;
				pr::geometry::LineSize(num_lines, vcount, icount);

				// Generate the geometry
				Cache cache(vcount, icount);
				auto props = pr::geometry::Lines(num_lines, points, num_colours, colours, std::begin(cache.m_vcont), std::begin(cache.m_icont));
				cache.m_bbox = props.m_bbox;
				cache.AddNugget(EPrim::LineList, props.m_geom, props.m_has_alpha, false, mat);

				// Create the model
				return Create(rdr, cache);
			}
			static ModelPtr LinesD(Renderer& rdr, int num_lines, v4 const* points, v4 const* directions, int num_colours = 0, Colour32 const* colours = nullptr, NuggetProps const* mat = nullptr)
			{
				// Calculate the required buffer sizes
				int vrange, irange;
				pr::geometry::LineSize(num_lines, vcount, icount);

				// Generate the geometry
				Cache cache(vcount, icount);
				auto props = pr::geometry::LinesD(num_lines, points, directions, num_colours, colours, std::begin(cache.m_vcont), std::begin(cache.m_icont));
				cache.m_bbox = props.m_bbox;
				cache.AddNugget(EPrim::LineList, props.m_geom, props.m_has_alpha, false, mat);

				// Create the model
				return Create(rdr, cache);
			}
			static ModelPtr LineStrip(Renderer& rdr, int num_lines, v4 const* points, int num_colours = 0, Colour32 const* colour = nullptr, NuggetProps const* mat = nullptr)
			{
				// Calculate the required buffer sizes
				int vcount, icount;
				pr::geometry::LineStripSize(num_lines, vcount, icount);

				// Generate the geometry
				Cache cache(vcount, icount);
				auto props = pr::geometry::LinesStrip(num_lines, points, num_colours, colour, std::begin(cache.m_vcont), std::begin(cache.m_icont));
				cache.m_bbox = props.m_bbox;
				cache.AddNugget(EPrim::LineStrip, props.m_geom, props.m_has_alpha, false, mat);

				// Create the model
				return Create(rdr, cache);
			}

			// Quad *******************************************************************************
			static ModelPtr Quad(Renderer& rdr, int num_quads, v4 const* verts, int num_colours = 0, Colour32 const* colours = nullptr, m4x4 const& t2q = m4x4Identity, NuggetProps const* mat = nullptr)
			{
				// Calculate the required buffer sizes
				int vcount, icount;
				pr::geometry::QuadSize(num_quads, vcount, icount);

				// Generate the geometry
				Cache cache(vcount, icount);
				auto props = pr::geometry::Quad(num_quads, verts, num_colours, colours, t2q, std::begin(cache.m_vcont), std::begin(cache.m_icont));
				cache.m_bbox = props.m_bbox;
				cache.AddNugget(EPrim::TriList, props.m_geom, props.m_has_alpha, false, mat);

				// Create the model
				return Create(rdr, cache);
			}
			static ModelPtr Quad(Renderer& rdr, v2 const& anchor, v4 const& quad_w, v4 const& quad_h, iv2 const& divisions = iv2Zero, Colour32 colour = Colour32White, m4x4 const& t2q = m4x4Identity, NuggetProps const* mat = nullptr)
			{
				// Calculate the required buffer sizes
				int vcount, icount;
				pr::geometry::QuadSize(divisions, vcount, icount);

				// Generate the geometry
				Cache cache(vcount, icount);
				auto props = pr::geometry::Quad(anchor, quad_w, quad_h, divisions, colour, t2q, std::begin(cache.m_vcont), std::begin(cache.m_icont));
				cache.m_bbox = props.m_bbox;
				cache.AddNugget(EPrim::TriList, props.m_geom, props.m_has_alpha, false, mat);

				// Create the model
				return Create(rdr, cache);
			}
			static ModelPtr Quad(Renderer& rdr, AxisId axis_id, v2 const& anchor, float width, float height, iv2 const& divisions = iv2Zero, Colour32 colour = Colour32White, m4x4 const& t2q = m4x4Identity, m4x4 const& o2w = m4x4Identity, NuggetProps const* mat = nullptr)
			{
				// Calculate the required buffer sizes
				int vcount, icount;
				pr::geometry::QuadSize(divisions, vcount, icount);

				// Generate the geometry
				Cache cache(vcount, icount);
				auto props = pr::geometry::Quad(axis_id, anchor, width, height, divisions, colour, t2q, o2w, std::begin(cache.m_vcont), std::begin(cache.m_icont));
				cache.m_bbox = props.m_bbox;
				cache.AddNugget(EPrim::TriList, props.m_geom, props.m_has_alpha, false, mat);

				// Create the model
				return Create(rdr, cache);
			}
			static ModelPtr Quad(Renderer& rdr, v4 const& centre, v4 const& forward, v4 const& top, float width, float height, iv2 const& divisions = iv2Zero, Colour32 colour = Colour32White, v2 const& tex_origin = v2Zero, v2 const& tex_dim = v2One, NuggetProps const* mat = nullptr)
			{
				// Calculate the required buffer sizes
				int vcount, icount;
				pr::geometry::QuadSize(divisions, vcount, icount);

				// Generate the geometry
				Cache cache(vcount, icount);
				auto props = pr::geometry::Quad(centre, forward, top, width, height, divisions, colour, tex_origin, tex_dim, std::begin(cache.m_vcont), std::begin(cache.m_icont));
				cache.m_bbox = props.m_bbox;
				cache.AddNugget(EPrim::TriList, props.m_geom, props.m_has_alpha, false, mat);

				// Create the model
				return Create(rdr, cache);
			}
			static ModelPtr QuadStrip(Renderer& rdr, int num_quads, v4 const* verts, float width, int num_normals = 0, v4 const* normals = nullptr, int num_colours = 0, Colour32 const* colours = nullptr, NuggetProps const* mat = nullptr)
			{
				// Calculate the required buffer sizes
				int vcount, icount;
				pr::geometry::QuadStripSize(num_quads, vcount, icount);

				// Generate the geometry
				Cache cache(vcount, icount);
				auto props = pr::geometry::QuadStrip(num_quads, verts, width, num_normals, normals, num_colours, colours, std::begin(cache.m_vcont), std::begin(cache.m_icont));
				cache.m_bbox = props.m_bbox;
				cache.AddNugget(EPrim::TriStrip, props.m_geom, props.m_has_alpha, false, mat);

				// Create the model
				return Create(rdr, cache);
			}

			// Shape2d ****************************************************************************
			static ModelPtr Ellipse(Renderer& rdr, float dimx, float dimy, bool solid, int facets = 40, Colour32 colour = Colour32White, m4x4 const* o2w = nullptr, NuggetProps const* mat = nullptr)
			{
				// Calculate the required buffer sizes
				int vcount, icount;
				pr::geometry::EllipseSize(solid, facets, vcount, icount);

				// Generate the geometry
				Cache cache(vcount, icount);
				auto props = pr::geometry::Ellipse(dimx, dimy, solid, facets, colour, std::begin(cache.m_vcont), std::begin(cache.m_icont));
				cache.m_bbox = props.m_bbox;
				cache.AddNugget(solid ? EPrim::TriStrip : EPrim::LineStrip, props.m_geom, props.m_has_alpha, false, mat);

				// Create the model
				return Create(rdr, cache, o2w);
			}
			static ModelPtr Pie(Renderer& rdr, float dimx, float dimy, float ang0, float ang1, float radius0, float radius1, bool solid, int facets = 40, Colour32 colour = Colour32White, m4x4 const* o2w = nullptr, NuggetProps const* mat = nullptr)
			{
				// Calculate the required buffer sizes
				int vcount, icount;
				pr::geometry::PieSize(solid, ang0, ang1, facets, vcount, icount);

				// Generate the geometry
				Cache cache(vcount, icount);
				auto props = pr::geometry::Pie(dimx, dimy, ang0, ang1, radius0, radius1, solid, facets, colour, std::begin(cache.m_vcont), std::begin(cache.m_icont));
				cache.m_bbox = props.m_bbox;
				cache.AddNugget(solid ? EPrim::TriStrip : EPrim::LineStrip, props.m_geom, props.m_has_alpha, false, mat);

				// Create the model
				return Create(rdr, cache, o2w);
			}
			static ModelPtr RoundedRectangle(Renderer& rdr, float dimx, float dimy, float corner_radius, bool solid, int facets = 10, Colour32 colour = Colour32White, m4x4 const* o2w = nullptr, NuggetProps const* mat = nullptr)
			{
				// Calculate the required buffer sizes
				int vcount, icount;
				pr::geometry::RoundedRectangleSize(solid, corner_radius, facets, vcount, icount);

				// Generate the geometry
				Cache cache(vcount, icount);
				auto props = pr::geometry::RoundedRectangle(dimx, dimy, solid, corner_radius, facets, colour, std::begin(cache.m_vcont), std::begin(cache.m_icont));
				cache.m_bbox = props.m_bbox;
				cache.AddNugget(solid ? EPrim::TriStrip : EPrim::LineStrip, props.m_geom, props.m_has_alpha, false, mat);

				// Create the model
				return Create(rdr, cache, o2w);
			}
			static ModelPtr Polygon(Renderer& rdr, int num_points, v2 const* points, bool solid, int num_colours = 0, Colour32 const* colours = nullptr, m4x4 const* o2w = nullptr, NuggetProps const* mat = nullptr)
			{
				// Calculate the required buffer sizes
				int vcount, icount;
				pr::geometry::PolygonSize(num_points, solid, vcount, icount);

				// Generate the geometry
				Cache cache(vcount, icount);
				auto props = pr::geometry::Polygon(num_points, points, solid, num_colours, colours, std::begin(cache.m_vcont), std::begin(cache.m_icont));
				cache.m_bbox = props.m_bbox;
				cache.AddNugget(solid ? EPrim::TriList : EPrim::LineStrip, props.m_geom, props.m_has_alpha, false, mat);

				// Create the model
				return Create(rdr, cache, o2w);
			}
			
			// Boxes ******************************************************************************
			static ModelPtr Boxes(Renderer& rdr, int num_boxes, v4 const* points, int num_colours = 0, Colour32 const* colours = nullptr, NuggetProps const* mat = nullptr)
			{
				// Calculate the required buffer sizes
				int vcount, icount;
				pr::geometry::BoxSize(num_boxes, vcount, icount);

				// Generate the geometry
				Cache cache(vcount, icount);
				auto props = pr::geometry::Boxes(num_boxes, points, num_colours, colours, std::begin(cache.m_vcont), std::begin(cache.m_icont));
				cache.m_bbox = props.m_bbox;
				cache.AddNugget(EPrim::TriList, props.m_geom, props.m_has_alpha, false, mat);

				// Create the model
				return Create(rdr, cache);
			}
			static ModelPtr Boxes(Renderer& rdr, int num_boxes, v4 const* points, m4x4 const& o2w, int num_colours = 0, Colour32 const* colours = nullptr, NuggetProps const* mat = nullptr)
			{
				// Calculate the required buffer sizes
				int vcount, icount;
				pr::geometry::BoxSize(num_boxes, vcount, icount);

				// Generate the geometry
				Cache cache(vcount, icount);
				auto props = pr::geometry::Boxes(num_boxes, points, o2w, num_colours, colours, std::begin(cache.m_vcont), std::begin(cache.m_icont));
				cache.m_bbox = props.m_bbox;
				cache.AddNugget(EPrim::TriList, props.m_geom, props.m_has_alpha, false, mat);

				// Create the model
				return Create(rdr, cache);
			}
			static ModelPtr Box(Renderer& rdr, v4 const& rad, m4x4 const& o2w = m4x4Identity, Colour32 colour = Colour32White, NuggetProps const* mat = nullptr)
			{
				// Calculate the required buffer sizes
				int vcount, icount;
				pr::geometry::BoxSize(1, vcount, icount);

				// Generate the geometry
				Cache cache(vcount, icount);
				auto props = pr::geometry::Box(rad, o2w, colour, std::begin(cache.m_vcont), std::begin(cache.m_icont));
				cache.m_bbox = props.m_bbox;
				cache.AddNugget(EPrim::TriList, props.m_geom, props.m_has_alpha, false, mat);

				// Create the model
				return Create(rdr, cache);
			}
			static ModelPtr Box(Renderer& rdr, float rad, m4x4 const& o2w = m4x4Identity, Colour32 colour = Colour32White, NuggetProps const* mat = nullptr)
			{
				return Box(rdr, v4(rad), o2w, colour, mat);
			}
			static ModelPtr BoxList(Renderer& rdr, int num_boxes, v4 const* positions, v4 const& rad, int num_colours = 0, Colour32 const* colours = nullptr, NuggetProps const* mat = nullptr)
			{
				// Calculate the required buffer sizes
				int vcount, icount;
				pr::geometry::BoxSize(num_boxes, vcount, icount);

				// Generate the geometry
				Cache cache(vcount, icount);
				auto props = pr::geometry::BoxList(num_boxes, positions, rad, num_colours, colours, std::begin(cache.m_vcont), std::begin(cache.m_icont));
				cache.m_bbox = props.m_bbox;
				cache.AddNugget(EPrim::TriList, props.m_geom, props.m_has_alpha, false, mat);

				// Create the model
				return Create(rdr, cache);
			}

			// Sphere *****************************************************************************
			static ModelPtr Geosphere(Renderer& rdr, v4 const& radius, int divisions = 3, Colour32 colour = Colour32White, NuggetProps const* mat = nullptr)
			{
				// Calculate the required buffer sizes
				int vcount, icount;
				pr::geometry::GeosphereSize(divisions, vcount, icount);

				// Generate the geometry
				Cache cache(vcount, icount);
				auto props = pr::geometry::Geosphere(radius, divisions, colour, std::begin(cache.m_vcont), std::begin(cache.m_icont));
				cache.m_bbox = props.m_bbox;
				cache.AddNugget(EPrim::TriList, props.m_geom, props.m_has_alpha, false, mat);

				// Create the model
				return Create(rdr, cache);
			}
			static ModelPtr Geosphere(Renderer& rdr, float radius, int divisions = 3, Colour32 colour = Colour32White, NuggetProps const* mat = nullptr)
			{
				return Geosphere(rdr, v4(radius, radius, radius, 0.0f), divisions, colour, mat);
			}
			static ModelPtr Sphere(Renderer& rdr, v4 const& radius, int wedges = 20, int layers = 5, Colour32 colour = Colour32White, NuggetProps const* mat = nullptr)
			{
				// Calculate the required buffer sizes
				int vcount, icount;
				pr::geometry::SphereSize(wedges, layers, vcount, icount);

				// Generate the geometry
				Cache cache(vcount, icount);
				auto props = pr::geometry::Sphere(radius, wedges, layers, colour, std::begin(cache.m_vcont), std::begin(cache.m_icont));
				cache.m_bbox = props.m_bbox;
				cache.AddNugget(EPrim::TriList, props.m_geom, props.m_has_alpha, false, mat);

				// Create the model
				return Create(rdr, cache);
			}
			static ModelPtr Sphere(Renderer& rdr, float radius, int wedges = 20, int layers = 5, Colour32 colour = Colour32White, NuggetProps const* mat = nullptr)
			{
				return Sphere(rdr, v4(radius, 0.0f), wedges, layers, colour, mat);
			}

			// Cylinder ***************************************************************************
			static ModelPtr Cylinder(Renderer& rdr, float radius0, float radius1, float height, float xscale = 1.0f, float yscale = 1.0f, int wedges = 20, int layers = 1, int num_colours = 0, Colour32 const* colours = nullptr, m4x4 const* o2w = nullptr, NuggetProps const* mat = nullptr)
			{
				// Calculate the required buffer sizes
				int vcount, icount;
				pr::geometry::CylinderSize(wedges, layers, vcount, icount);

				// Generate the geometry
				Cache cache(vcount, icount);
				auto props = pr::geometry::Cylinder(radius0, radius1, height, xscale, yscale, wedges, layers, num_colours, colours, std::begin(cache.m_vcont), std::begin(cache.m_icont));
				cache.m_bbox = props.m_bbox;
				cache.AddNugget(EPrim::TriList, props.m_geom, props.m_has_alpha, false, mat);

				// Create the model
				return Create(rdr, cache, o2w);
			}

			// Capsule ****************************************************************************
			//void        CapsuleSize(Range& vrange, Range& irange, int divisions);
			//Settings    CapsuleModelSettings(int divisions);
			//ModelPtr    CapsuleHRxy(MLock& mlock  ,MaterialManager& matmgr ,float height ,float xradius ,float yradius ,m4x4 const& o2w = pr::m4x4Identity ,int divisions = 3 ,Colour32 colour = Colour32White ,rdr::Material const* mat = 0 ,Range* vrange = 0 ,Range* irange = 0);
			//ModelPtr    CapsuleHRxy(Renderer& rdr                          ,float height ,float xradius ,float yradius ,m4x4 const& o2w = pr::m4x4Identity ,int divisions = 3 ,Colour32 colour = Colour32White ,rdr::Material const* mat = 0 ,Range* vrange = 0 ,Range* irange = 0);

			// Extrude ****************************************************************************
			static ModelPtr Extrude(Renderer& rdr, int cs_count, v2 const* cs, int path_count, v4 const* path, bool closed, bool smooth_cs, int num_colours = 0, Colour32 const* colours = nullptr, m4x4 const* o2w = nullptr, NuggetProps const* mat = nullptr)
			{
				// Calculate the required buffer sizes
				int vcount, icount;
				pr::geometry::ExtrudeSize(cs_count, path_count, closed, smooth_cs, vcount, icount);

				// Convert a stream of points into a stream of transforms
				m4x4 ori; auto make_path = [&](int p)
				{
					if (p == 0)
					{
						ori.rot = OriFromDir(path[1] - path[0], AxisId::PosZ, v4YAxis);
					}
					else if (p == path_count - 1)
					{
						ori.rot = OriFromDir(path[p] - path[p-1], AxisId::PosZ, v4YAxis);
					}
					else
					{
						auto a = Normalise3(path[p  ] - path[p-1], v4Zero);
						auto b = Normalise3(path[p+1] - path[p  ], v4Zero);
						ori.rot = OriFromDir(a + b, AxisId::PosZ, v4YAxis);
					}
					ori.pos = path[p];
					return ori;
				};

				// Generate the geometry
				Cache cache(vcount, icount);
				auto props = pr::geometry::Extrude(cs_count, cs, path_count, make_path, closed, smooth_cs, num_colours, colours, std::begin(cache.m_vcont), std::begin(cache.m_icont));
				cache.m_bbox = props.m_bbox;
				cache.AddNugget(EPrim::TriList, props.m_geom, props.m_has_alpha, false, mat);

				// Create the model
				return Create(rdr, cache, o2w);
			}
			static ModelPtr Extrude(Renderer& rdr, int cs_count, v2 const* cs, int path_count, m4x4 const* path, bool closed, bool smooth_cs, int num_colours = 0, Colour32 const* colours = nullptr, m4x4 const* o2w = nullptr, NuggetProps const* mat = nullptr)
			{
				// Calculate the required buffer sizes
				int vcount, icount;
				pr::geometry::ExtrudeSize(cs_count, path_count, closed, smooth_cs, vcount, icount);

				// Path transform stream source
				auto p = path;
				auto make_path = [&]{ return *p++; };

				// Generate the geometry
				Cache cache(vcount, icount);
				auto props = pr::geometry::Extrude(cs_count, cs, path_count, make_path, closed, smooth_cs, num_colours, colours, std::begin(cache.m_vcont), std::begin(cache.m_icont));
				cache.m_bbox = props.m_bbox;
				cache.AddNugget(EPrim::TriList, props.m_geom, props.m_has_alpha, false, mat);

				// Create the model
				return Create(rdr, cache, o2w);
			}

			// Mesh *******************************************************************************
			static ModelPtr Mesh(Renderer& rdr, MeshCreationData const& cdata)
			{
				// Calculate the required buffer sizes
				int vcount, icount;
				pr::geometry::MeshSize(cdata.m_vcount, cdata.m_icount, vcount, icount);

				// Generate the geometry
				Cache cache(vcount, icount);
				auto props = pr::geometry::Mesh(
					cdata.m_vcount, cdata.m_icount,
					cdata.m_verts, cdata.m_indices,
					cdata.m_ccount, cdata.m_colours,
					cdata.m_ncount, cdata.m_normals,
					cdata.m_tex_coords,
					std::begin(cache.m_vcont), std::begin(cache.m_icont));
				cache.m_bbox = props.m_bbox;

				// Create the nuggets
				cache.m_ncont.insert(std::begin(cache.m_ncont), cdata.m_nuggets, cdata.m_nuggets + cdata.m_gcount);

				// Create the model
				return Create(rdr, cache);
			}

			// ModelFile **************************************************************************
			// Populates 'cache' from 'src' which is expected to be a p3d model file stream.
			// 'P3D' models can contain more than one mesh. If 'mesh_name' is nullptr, then the
			// first mesh in the scene is loaded. If not null, then the first mesh that matches
			// 'mesh_name' is loaded. If 'mesh_name' is non-null and 'src' does not contain a matching
			// mesh, an exception is thrown.
			static void LoadP3DModel(Renderer& rdr, std::istream& src, char const* mesh_name, Cache& cache)
			{
				using namespace pr::geometry::p3d;
				static_assert(sizeof(VType) == sizeof(Vert), "P3D vert is a different size");
				static_assert(sizeof(IType) == sizeof(u16), "P3D index is a different size");

				std::vector<std::string> mats;

				// Parse the meshes in the stream
				// Todo, if you're not reading the first mesh in the file, the earlier
				// meshes all get loaded into memory for no good reason... need a nice
				// way to seek to the mesh we're after without loading the entire mesh into memory
				ReadMeshes(src, [&](pr::geometry::p3d::Mesh const& mesh)
				{
					if (mesh_name != nullptr && mesh.m_name != mesh_name)
						return false;

					// Name/Bounding box
					cache.m_name = mesh.m_name;
					cache.m_bbox = mesh.m_bbox;

					// Copy the verts
					cache.m_vcont.resize(mesh.m_verts.size());
					memcpy(cache.m_vcont.data(), mesh.m_verts.data(), sizeof(Vert) * mesh.m_verts.size());

					// Copy the indices
					cache.m_icont.resize(mesh.m_idx16.size());
					memcpy(cache.m_icont.data(), mesh.m_idx16.data(), sizeof(u16) * mesh.m_idx16.size());

					// Copy the nuggets
					cache.m_ncont.reserve(mesh.m_nugget.size());
					for (auto& nug : mesh.m_nugget)
					{
						cache.m_ncont.emplace_back(
							static_cast<EPrim>(nug.m_topo),
							static_cast<EGeom>(nug.m_geom),
							nullptr,
							nug.m_vrange,
							nug.m_irange);

						// Record the material as used
						pr::insert_unique(mats, nug.m_mat.str);
					}

					// Stop searching
					return true;
				});

				// Load the used materials into the renderer
				for (auto& mat : mats)
				{
					// todo
					(void)rdr,mat;
				}
			}
			static void Load3DSModel(Renderer& rdr, std::istream& src, char const* mesh_name, Cache& cache)
			{
				using namespace pr::geometry::max_3ds;

				// Bounding box
				cache.m_bbox = BBoxReset;
				auto bb = [&](v4 const& v){ Encompass(cache.m_bbox, v); return v; };

				// Output callback functions
				auto vout = [&](v4 const& p, Colour const& c, v4 const& n, v2 const& t)
				{
					VType vert;
					SetPCNT(vert, bb(p), c, n, t);
					cache.m_vcont.push_back(vert);
				};
				auto iout = [&](pr::uint16 i0, pr::uint16 i1, pr::uint16 i2)
				{
					cache.m_icont.push_back(i0);
					cache.m_icont.push_back(i1);
					cache.m_icont.push_back(i2);
				};
				auto nout = [&](Material const& mat, EGeom geom, Range vrange, Range irange)
				{
					NuggetProps ddata(EPrim::TriList, geom, nullptr, vrange, irange);
					ddata.m_geometry_has_alpha = !FEql(mat.m_diffuse.a, 1.0f);

					// Register any materials with the renderer
					if (!mat.m_textures.empty())
					{
						// This is tricky, textures are likely to be jpg's, or pngs
						// but the renderer only supports dds at the moment.
						// Also, we only support one diffuse texture per nugget currently

						(void)rdr;
						//ddata.m_tex_diffuse = rdr.m_tex_mgr.CreateTexture2D();
					}

					cache.m_ncont.push_back(ddata);
				};

				// Parse the materials in the 3ds stream
				std::unordered_map<std::string, Material> mats;
				ReadMaterials(src, [&](Material&& mat){ mats[mat.m_name] = mat; return false; });
				auto matlookup = [&](std::string const& name) { return mats.at(name); };

				// Parse the model objects in the 3ds stream
				ReadObjects(src, [&](Object&& obj)
					{
						// Wrong name, keep searching
						if (mesh_name != nullptr && obj.m_name != mesh_name)
							return false;

						CreateModel(obj, matlookup, nout, vout, iout);
						return true; // done, stop searching
					});
			}
			static ModelPtr LoadP3DModel(Renderer& rdr, std::istream& src, char const* mesh_name = nullptr, m4x4 const* bake = nullptr, float gen_normals = -1.0f)
			{
				Cache cache;
				LoadP3DModel(rdr, src, mesh_name, cache);
				return Create(rdr, cache, bake, gen_normals);
			}
			static ModelPtr Load3DSModel(Renderer& rdr, std::istream& src, char const* mesh_name = nullptr, m4x4 const* bake = nullptr, float gen_normals = -1.0f)
			{
				Cache cache;
				Load3DSModel(rdr, src, mesh_name, cache);
				return Create(rdr, cache, bake, gen_normals);
			}
			static ModelPtr LoadModel(Renderer& rdr, pr::geometry::EModelFileFormat format, std::istream& src, char const* mesh_name = nullptr, m4x4 const* bake = nullptr, float gen_normals = -1.0f)
			{
				using namespace pr::geometry;
				switch (format)
				{
				default: throw std::exception("Unsupported model file format");
				case EModelFileFormat::P3D:    return LoadP3DModel(rdr, src, mesh_name, bake, gen_normals);
				case EModelFileFormat::Max3DS: return Load3DSModel(rdr, src, mesh_name, bake, gen_normals);
				}
			}

			// Text *******************************************************************************
			// Create a quad containing text
			// 'text' is the formatting interface for the text
			// 'axis_id' is the basis axis of the quad normal
			// 'fr_colour' and 'bk_colour' are the text colour and background colours
			// 'anchor' controls the position of the origin of the quad, anchor.x = -1,0,+1 = left,centre_h,right. anchor.y = -1,0,+1 = top,centre_v,bottom
			// 'dim_out.xy' is the dimensions of the text texture. (measured in pixels)
			// 'dim_out.zw' is the dimensions of the quad that contains the text (measured in pixels)
			static ModelPtr Text(Renderer& rdr, IDWriteTextLayout* text, AxisId axis_id, Colour32 fr_colour, Colour32 bk_colour, v2 const& anchor, v4& dim_out, m4x4 const* bake = nullptr)
			{
				// Get the text layout to measure the string.
				DWRITE_TEXT_METRICS metrics;
				pr::Throw(text->GetMetrics(&metrics));

				// Texture sizes are in physical pixels, but D2D operates in DIP so we need to determine
				// the size in physical pixels on this device that correspond to the returned metrics.
				// From: https://msdn.microsoft.com/en-us/library/windows/desktop/ff684173%28v=vs.85%29.aspx?f=255&MSPPError=-2147217396
				// "Direct2D automatically performs scaling to match the DPI setting.
				//  In Direct2D, coordinates are measured in units called device-independent pixels (DIPs).
				//  A DIP is defined as 1/96th of a logical inch. In Direct2D, all drawing operations are
				//  specified in DIPs and then scaled to the current DPI setting."
				auto dpi = rdr.DpiScale();
				auto text_size = v2(metrics.width * dpi.x, metrics.height * dpi.y);
				auto texture_size = Ceil(text_size);

				// Create a texture large enough to contain the text, and render the text into it
				SamplerDesc sdesc(D3D11_TEXTURE_ADDRESS_CLAMP, D3D11_FILTER_MIN_MAG_MIP_POINT);
				TextureDesc tdesc(size_t(texture_size.x), size_t(texture_size.y), 1, DXGI_FORMAT_R8G8B8A8_UNORM);
				tdesc.BindFlags = D3D11_BIND_RENDER_TARGET | D3D11_BIND_SHADER_RESOURCE;
				auto has_alpha = bk_colour.a != 0xff || fr_colour.a != 0xff;
				auto tex = rdr.m_tex_mgr.CreateTexture2D(AutoId, Image(), tdesc, sdesc, has_alpha, "text_quad");

				// Get a D2D device context to draw on
				auto dc = tex->GetD2DeviceContext();
				auto fr = pr::To<D3DCOLORVALUE>(fr_colour);
				auto bk = pr::To<D3DCOLORVALUE>(bk_colour);

				// Create a solid brush
				D3DPtr<ID2D1SolidColorBrush> brush;
				pr::Throw(dc->CreateSolidColorBrush(fr, &brush.m_ptr));
				brush->SetOpacity(fr_colour.a);

				// Draw the string
				dc->BeginDraw();
				dc->Clear(&bk);
				dc->DrawTextLayout(D2D1::Point2F(0, 0), text, brush.get(), D2D1_DRAW_TEXT_OPTIONS_NONE);
				pr::Throw(dc->EndDraw());

				// Create a quad using this texture
				int vcount, icount;
				pr::geometry::QuadSize(1, vcount, icount);

				// Return the size of the quad and the texture
				dim_out = v4(text_size, texture_size);

				// Set the texture coordinates to match the text metrics and the quad size
				auto t2q = m4x4::Scale(text_size.x/texture_size.x, text_size.y/texture_size.y, 1.0f, v4Origin) * m4x4(v4XAxis, -v4YAxis, v4ZAxis, v4(0, 1, 0, 1));

				// Create a quad with this size
				NuggetProps mat(EPrim::TriList);
				mat.m_tex_diffuse = tex;

				// Generate the geometry
				Cache cache(vcount, icount);
				auto props = pr::geometry::Quad(axis_id, anchor, text_size.x, text_size.y, iv2Zero, Colour32White, t2q, std::begin(cache.m_vcont), std::begin(cache.m_icont));
				cache.m_bbox = props.m_bbox;
				cache.AddNugget(EPrim::TriList, props.m_geom, props.m_has_alpha, false, &mat);

				// Create the model
				return Create(rdr, cache, bake);
			}
			static ModelPtr Text(Renderer& rdr, IDWriteTextLayout* text, AxisId axis_id, Colour32 fr_colour, Colour32 bk_colour, v2 const& anchor)
			{
				v4 dim_out;
				return Text(rdr, text, axis_id, fr_colour, bk_colour, anchor, dim_out);
			}
			static ModelPtr Text(Renderer& rdr, wchar_t const* text, int length, AxisId axis_id, Colour32 fr_colour, Colour32 bk_colour, float max_width, float max_height, v2 const& anchor, v4& dim_out)
			{
				using namespace D2D1;
				auto dwrite = rdr.DWrite();

				// Create the "font", a.k.a text format
				D3DPtr<IDWriteTextFormat> text_format;
				pr::Throw(dwrite->CreateTextFormat(L"tahoma", nullptr, DWRITE_FONT_WEIGHT_LIGHT, DWRITE_FONT_STYLE_NORMAL, DWRITE_FONT_STRETCH_NORMAL, 20.0f, L"en-GB", &text_format.m_ptr));

				// Create the 'format" a.k.a text layout
				D3DPtr<IDWriteTextLayout> text_layout;
				pr::Throw(dwrite->CreateTextLayout(text, UINT32(length), text_format.get(), max_width, max_height, &text_layout.m_ptr));

				// Create the text model
				return Text(rdr, text_layout.get(), axis_id, fr_colour, bk_colour, anchor, dim_out);
			}
			static ModelPtr Text(Renderer& rdr, wchar_t const* text, int length, AxisId axis_id, Colour32 fr_colour, Colour32 bk_colour, float max_width, float max_height, v2 const& anchor)
			{
				v4 dim_out;
				return Text(rdr, text, length, axis_id, fr_colour, bk_colour, max_width, max_height, anchor, dim_out);
			}
		};
	}
}
