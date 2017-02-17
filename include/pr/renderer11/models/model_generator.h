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
			// A container for the model data
			struct Cont :AlignTo<16>
			{
				typedef VType VType;
				typedef IType IType;

				using VCont = pr::vector<VType>;
				using ICont = pr::vector<IType>;
				using NCont = pr::vector<NuggetProps>;

				using VIter = typename VCont::iterator;
				using IIter = typename ICont::iterator;
				using NIter = typename NCont::iterator;

				static EGeom const GeomMask = VType::GeomMask;

				std::string m_name; // Model name
				VCont m_vcont; // Model verts
				ICont m_icont; // Model faces/lines/points/etc
				NCont m_ncont; // Model nuggets
				BBox  m_bbox;  // Model bounding box

				Cont(int vcount = 0, int icount = 0, int ncount = 0)
					:m_name()
					,m_vcont(vcount)
					,m_icont(icount)
					,m_ncont(ncount)
					,m_bbox(BBoxReset)
				{}
				void AddNugget(EPrim topo, EGeom geom, bool has_alpha, NuggetProps const* mat = nullptr)
				{
					NuggetProps nug;
					if (mat) nug = *mat;
					nug.m_topo = topo;
					nug.m_geom = geom;
					nug.m_has_alpha = has_alpha;
					m_ncont.push_back(nug);
				}
			};

			// Return the thread local static instance of a 'Cont'
			static Cont& CacheCont(int vcount = 0, int icount = 0, int ncount = 0)
			{
				assert(vcount >= 0 && icount >= 0 && ncount >= 0);
				thread_local static Cont* cont;
				if (!cont) cont = new Cont();
				cont->m_name.resize(0);
				cont->m_vcont.resize(vcount);
				cont->m_icont.resize(icount);
				cont->m_ncont.resize(ncount);
				cont->m_bbox = BBoxReset;
				return *cont;
			}

			// Create a model from 'cont'
			// 'alpha' if true, the default nugget will have alpha enabled. Ignored if 'cont' contains nuggets
			// 'bake' is a transform to bake into the model
			// 'gen_normals' generates normals for the model if >= 0f. Value is the threshold for smoothing (in rad)
			static ModelPtr Create(Renderer& rdr, Cont& cont, m4x4 const* bake = nullptr, float gen_normals = -1.0f)
			{
				// Sanity check 'cont'
				#if PR_DBG_RDR
				PR_ASSERT(1, !cont.m_ncont.empty(), "No nuggets given");
				for (auto& nug : cont.m_ncont)
				{
					PR_ASSERT(1, nug.m_vrange.begin() < cont.m_vcont.size(), "Nugget range invalid");
					PR_ASSERT(1, nug.m_irange.begin() < cont.m_icont.size(), "Nugget range invalid");
					PR_ASSERT(1, nug.m_vrange.end()  <= cont.m_vcont.size(), "Nugget range invalid");
					PR_ASSERT(1, nug.m_irange.end()  <= cont.m_icont.size(), "Nugget range invalid");
				}
				#endif

				// Bake a transform into the model
				#pragma region Bake transform
				if (bake)
				{
					// Apply the 'bake' transform to every vertex
					cont.m_bbox = *bake * cont.m_bbox;
					for (auto& v : cont.m_vcont)
					{
						v.m_vert = *bake * v.m_vert;
						v.m_norm = *bake * v.m_norm;
					}

					// If the transform is left handed, flip the faces
					if (Determinant3(*bake) < 0)
					{
						// Check each nugget for faces
						for (auto& nug : cont.m_ncont)
						{
							switch (nug.m_topo)
							{
							case EPrim::TriList:
								{
									assert((nug.m_irange.size() % 3) == 0);
									for (size_t i = nug.m_irange.begin(), iend = nug.m_irange.end(); i != iend; i += 3)
										std::swap(cont.m_icont[i+1], cont.m_icont[i+2]);
									break;
								}
							case EPrim::TriStrip:
								{
									assert((nug.m_irange.size() % 2) == 0);
									for (size_t i = nug.m_irange.begin(), iend = nug.m_irange.end(); i != iend; i += 2)
										std::swap(cont.m_icont[i+0], cont.m_icont[i+1]);
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
					for (auto& nug : cont.m_ncont)
					{
						switch (nug.m_topo)
						{
						case EPrim::TriList:
							{
								// Generate normals
								auto iout = std::begin(cont.m_icont) + nug.m_irange.begin();
								pr::geometry::GenerateNormals(
									nug.m_irange.size(), cont.m_icont.data() + nug.m_irange.begin(), gen_normals,
									[&](Cont::IType idx)
									{
										return GetP(cont.m_vcont[idx]);
									},
									cont.m_vcont.size(),
									[&](Cont::IType idx, Cont::IType orig, v4 const& norm)
									{
										if (idx >= cont.m_vcont.size()) cont.m_vcont.resize(idx + 1, cont.m_vcont[orig]);
										SetN(cont.m_vcont[idx], norm);
									},
									[&](Cont::IType i0, Cont::IType i1, Cont::IType i2)
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
				VBufferDesc vb(cont.m_vcont.size(), cont.m_vcont.data());
				IBufferDesc ib(cont.m_icont.size(), cont.m_icont.data());
				auto model = rdr.m_mdl_mgr.CreateModel(MdlSettings(vb, ib, cont.m_bbox));
				model->m_name = cont.m_name;

				// Create the render nuggets
				for (auto& nug : cont.m_ncont)
				{
					// If the model geom has valid texture data but no texture, use white
					if (AllSet(nug.m_geom, EGeom::Tex0) && nug.m_tex_diffuse == nullptr)
						nug.m_tex_diffuse = rdr.m_tex_mgr.FindTexture(EStockTexture::White);

					// Create the nugget
					model->CreateNugget(nug);
				}

				return model;
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
				auto& cont = CacheCont(vcount, icount);
				auto props = pr::geometry::Lines(num_lines, points, num_colours, colours, std::begin(cont.m_vcont), std::begin(cont.m_icont));
				cont.m_bbox = props.m_bbox;
				cont.AddNugget(EPrim::LineList, props.m_geom, props.m_has_alpha, mat);

				// Create the model
				return Create(rdr, cont);
			}
			static ModelPtr LinesD(Renderer& rdr, int num_lines, v4 const* points, v4 const* directions, int num_colours = 0, Colour32 const* colours = nullptr, NuggetProps const* mat = nullptr)
			{
				// Calculate the required buffer sizes
				int vrange, irange;
				pr::geometry::LineSize(num_lines, vcount, icount);

				// Generate the geometry
				auto& cont = CacheCont(vcount, icount);
				auto props = pr::geometry::LinesD(num_lines, points, directions, num_colours, colours, std::begin(cont.m_vcont), std::begin(cont.m_icont));
				cont.m_bbox = props.m_bbox;
				cont.AddNugget(EPrim::LineList, props.m_geom, props.m_has_alpha, mat);

				// Create the model
				return Create(rdr, cont);
			}
			static ModelPtr LineStrip(Renderer& rdr, int num_lines, v4 const* points, int num_colours = 0, Colour32 const* colour = nullptr, NuggetProps const* mat = nullptr)
			{
				// Calculate the required buffer sizes
				int vcount, icount;
				pr::geometry::LineStripSize(num_lines, vcount, icount);

				// Generate the geometry
				auto& cont = CacheCont(vcount, icount);
				auto props = pr::geometry::LinesStrip(num_lines, points, num_colours, colour, std::begin(cont.m_vcont), std::begin(cont.m_icont));
				cont.m_bbox = props.m_bbox;
				cont.AddNugget(EPrim::LineStrip, props.m_geom, props.m_has_alpha, mat);

				// Create the model
				return Create(rdr, cont);
			}

			// Quad *******************************************************************************
			static ModelPtr Quad(Renderer& rdr, int num_quads, v4 const* verts, int num_colours = 0, Colour32 const* colours = nullptr, m4x4 const& t2q = m4x4Identity, NuggetProps const* mat = nullptr)
			{
				// Calculate the required buffer sizes
				int vcount, icount;
				pr::geometry::QuadSize(num_quads, vcount, icount);

				// Generate the geometry
				auto& cont = CacheCont(vcount, icount);
				auto props = pr::geometry::Quad(num_quads, verts, num_colours, colours, t2q, std::begin(cont.m_vcont), std::begin(cont.m_icont));
				cont.m_bbox = props.m_bbox;
				cont.AddNugget(EPrim::TriList, props.m_geom, props.m_has_alpha, mat);

				// Create the model
				return Create(rdr, cont);
			}
			static ModelPtr Quad(Renderer& rdr, v4 const& origin, v4 const& patch_x, v4 const& patch_y, iv2 const& divisions = iv2Zero, Colour32 colour = Colour32White, m4x4 const& t2q = m4x4Identity, NuggetProps const* mat = nullptr)
			{
				// Calculate the required buffer sizes
				int vcount, icount;
				pr::geometry::QuadSize(divisions, vcount, icount);

				// Generate the geometry
				auto& cont = CacheCont(vcount, icount);
				auto props = pr::geometry::Quad(origin, quad_x, quad_z, divisions, colour, t2q, std::begin(cont.m_vcont), std::begin(cont.m_icont));
				cont.m_bbox = props.m_bbox;
				cont.AddNugget(EPrim::TriList, props.m_geom, props.m_has_alpha, mat);

				// Create the model
				return Create(rdr, cont);
			}
			static ModelPtr Quad(Renderer& rdr, float width, float height, iv2 const& divisions = iv2Zero, Colour32 colour = Colour32White, NuggetProps const* mat = nullptr)
			{
				// Calculate the required buffer sizes
				int vcount, icount;
				pr::geometry::QuadSize(divisions, vcount, icount);

				// Generate the geometry
				auto& cont = CacheCont(vcount, icount);
				auto props = pr::geometry::Quad(width, height, divisions, colour, std::begin(cont.m_vcont), std::begin(cont.m_icont));
				cont.m_bbox = props.m_bbox;
				cont.AddNugget(EPrim::TriList, props.m_geom, props.m_has_alpha, mat);

				// Create the model
				return Create(rdr, cont);
			}
			static ModelPtr Quad(Renderer& rdr, v4 const& centre, v4 const& forward, v4 const& top, float width, float height, iv2 const& divisions = iv2Zero, Colour32 colour = Colour32White, v2 const& tex_origin = v2Zero, v2 const& tex_dim = v2One, NuggetProps const* mat = nullptr)
			{
				// Calculate the required buffer sizes
				int vcount, icount;
				pr::geometry::QuadSize(divisions, vcount, icount);

				// Generate the geometry
				auto& cont = CacheCont(vcount, icount);
				auto props = pr::geometry::Quad(centre, forward, top, width, height, divisions, colour, tex_origin, tex_dim, std::begin(cont.m_vcont), std::begin(cont.m_icont));
				cont.m_bbox = props.m_bbox;
				cont.AddNugget(EPrim::TriList, props.m_geom, props.m_has_alpha, mat);

				// Create the model
				return Create(rdr, cont);
			}
			static ModelPtr QuadStrip(Renderer& rdr, int num_quads, v4 const* verts, float width, int num_normals = 0, v4 const* normals = nullptr, int num_colours = 0, Colour32 const* colours = nullptr, NuggetProps const* mat = nullptr)
			{
				// Calculate the required buffer sizes
				int vcount, icount;
				pr::geometry::QuadStripSize(num_quads, vcount, icount);

				// Generate the geometry
				auto& cont = CacheCont(vcount, icount);
				auto props = pr::geometry::QuadStrip(num_quads, verts, width, num_normals, normals, num_colours, colours, std::begin(cont.m_vcont), std::begin(cont.m_icont));
				cont.m_bbox = props.m_bbox;
				cont.AddNugget(EPrim::TriStrip, props.m_geom, props.m_has_alpha, mat);

				// Create the model
				return Create(rdr, cont);
			}

			// Shape2d ****************************************************************************
			static ModelPtr Ellipse(Renderer& rdr, float dimx, float dimy, bool solid, int facets = 40, Colour32 colour = Colour32White, m4x4 const* o2w = nullptr, NuggetProps const* mat = nullptr)
			{
				// Calculate the required buffer sizes
				int vcount, icount;
				pr::geometry::EllipseSize(solid, facets, vcount, icount);

				// Generate the geometry
				auto& cont = CacheCont(vcount, icount);
				auto props = pr::geometry::Ellipse(dimx, dimy, solid, facets, colour, std::begin(cont.m_vcont), std::begin(cont.m_icont));
				cont.m_bbox = props.m_bbox;
				cont.AddNugget(solid ? EPrim::TriStrip : EPrim::LineStrip, props.m_geom, props.m_has_alpha, mat);

				// Create the model
				return Create(rdr, cont, o2w);
			}
			static ModelPtr Pie(Renderer& rdr, float dimx, float dimy, float ang0, float ang1, float radius0, float radius1, bool solid, int facets = 40, Colour32 colour = Colour32White, m4x4 const* o2w = nullptr, NuggetProps const* mat = nullptr)
			{
				// Calculate the required buffer sizes
				int vcount, icount;
				pr::geometry::PieSize(solid, ang0, ang1, facets, vcount, icount);

				// Generate the geometry
				auto& cont = CacheCont(vcount, icount);
				auto props = pr::geometry::Pie(dimx, dimy, ang0, ang1, radius0, radius1, solid, facets, colour, std::begin(cont.m_vcont), std::begin(cont.m_icont));
				cont.m_bbox = props.m_bbox;
				cont.AddNugget(solid ? EPrim::TriStrip : EPrim::LineStrip, props.m_geom, props.m_has_alpha, mat);

				// Create the model
				return Create(rdr, cont, o2w);
			}
			static ModelPtr RoundedRectangle(Renderer& rdr, float dimx, float dimy, float corner_radius, bool solid, int facets = 10, Colour32 colour = Colour32White, m4x4 const* o2w = nullptr, NuggetProps const* mat = nullptr)
			{
				// Calculate the required buffer sizes
				int vcount, icount;
				pr::geometry::RoundedRectangleSize(solid, corner_radius, facets, vcount, icount);

				// Generate the geometry
				auto& cont = CacheCont(vcount, icount);
				auto props = pr::geometry::RoundedRectangle(dimx, dimy, solid, corner_radius, facets, colour, std::begin(cont.m_vcont), std::begin(cont.m_icont));
				cont.m_bbox = props.m_bbox;
				cont.AddNugget(solid ? EPrim::TriStrip : EPrim::LineStrip, props.m_geom, props.m_has_alpha, mat);

				// Create the model
				return Create(rdr, cont, o2w);
			}
			static ModelPtr Polygon(Renderer& rdr, int num_points, v2 const* points, bool solid, int num_colours = 0, Colour32 const* colours = nullptr, m4x4 const* o2w = nullptr, NuggetProps const* mat = nullptr)
			{
				// Calculate the required buffer sizes
				int vcount, icount;
				pr::geometry::PolygonSize(num_points, solid, vcount, icount);

				// Generate the geometry
				auto& cont = CacheCont(vcount, icount);
				auto props = pr::geometry::Polygon(num_points, points, solid, num_colours, colours, std::begin(cont.m_vcont), std::begin(cont.m_icont));
				cont.m_bbox = props.m_bbox;
				cont.AddNugget(solid ? EPrim::TriList : EPrim::LineStrip, props.m_geom, props.m_has_alpha, mat);

				// Create the model
				return Create(rdr, cont, o2w);
			}
			
			// Boxes ******************************************************************************
			static ModelPtr Boxes(Renderer& rdr, int num_boxes, v4 const* points, int num_colours = 0, Colour32 const* colours = nullptr, NuggetProps const* mat = nullptr)
			{
				// Calculate the required buffer sizes
				int vcount, icount;
				pr::geometry::BoxSize(num_boxes, vcount, icount);

				// Generate the geometry
				auto& cont = CacheCont(vcount, icount);
				auto props = pr::geometry::Boxes(num_boxes, points, num_colours, colours, std::begin(cont.m_vcont), std::begin(cont.m_icont));
				cont.m_bbox = props.m_bbox;
				cont.AddNugget(EPrim::TriList, props.m_geom, props.m_has_alpha, mat);

				// Create the model
				return Create(rdr, cont);
			}
			static ModelPtr Boxes(Renderer& rdr, int num_boxes, v4 const* points, m4x4 const& o2w, int num_colours = 0, Colour32 const* colours = nullptr, NuggetProps const* mat = nullptr)
			{
				// Calculate the required buffer sizes
				int vcount, icount;
				pr::geometry::BoxSize(num_boxes, vcount, icount);

				// Generate the geometry
				auto& cont = CacheCont(vcount, icount);
				auto props = pr::geometry::Boxes(num_boxes, points, o2w, num_colours, colours, std::begin(cont.m_vcont), std::begin(cont.m_icont));
				cont.m_bbox = props.m_bbox;
				cont.AddNugget(EPrim::TriList, props.m_geom, props.m_has_alpha, mat);

				// Create the model
				return Create(rdr, cont);
			}
			static ModelPtr Box(Renderer& rdr, v4 const& rad, m4x4 const& o2w = m4x4Identity, Colour32 colour = Colour32White, NuggetProps const* mat = nullptr)
			{
				// Calculate the required buffer sizes
				int vcount, icount;
				pr::geometry::BoxSize(1, vcount, icount);

				// Generate the geometry
				auto& cont = CacheCont(vcount, icount);
				auto props = pr::geometry::Box(rad, o2w, colour, std::begin(cont.m_vcont), std::begin(cont.m_icont));
				cont.m_bbox = props.m_bbox;
				cont.AddNugget(EPrim::TriList, props.m_geom, props.m_has_alpha, mat);

				// Create the model
				return Create(rdr, cont);
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
				auto& cont = CacheCont(vcount, icount);
				auto props = pr::geometry::BoxList(num_boxes, positions, rad, num_colours, colours, std::begin(cont.m_vcont), std::begin(cont.m_icont));
				cont.m_bbox = props.m_bbox;
				cont.AddNugget(EPrim::TriList, props.m_geom, props.m_has_alpha, mat);

				// Create the model
				return Create(rdr, cont);
			}

			// Sphere *****************************************************************************
			static ModelPtr Geosphere(Renderer& rdr, v4 const& radius, int divisions = 3, Colour32 colour = Colour32White, NuggetProps const* mat = nullptr)
			{
				// Calculate the required buffer sizes
				int vcount, icount;
				pr::geometry::GeosphereSize(divisions, vcount, icount);

				// Generate the geometry
				auto& cont = CacheCont(vcount, icount);
				auto props = pr::geometry::Geosphere(radius, divisions, colour, std::begin(cont.m_vcont), std::begin(cont.m_icont));
				cont.m_bbox = props.m_bbox;
				cont.AddNugget(EPrim::TriList, props.m_geom, props.m_has_alpha, mat);

				// Create the model
				return Create(rdr, cont);
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
				auto& cont = CacheCont(vcount, icount);
				auto props = pr::geometry::Sphere(radius, wedges, layers, colour, std::begin(cont.m_vcont), std::begin(cont.m_icont));
				cont.m_bbox = props.m_bbox;
				cont.AddNugget(EPrim::TriList, props.m_geom, props.m_has_alpha, mat);

				// Create the model
				return Create(rdr, cont);
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
				auto& cont = CacheCont(vcount, icount);
				auto props = pr::geometry::Cylinder(radius0, radius1, height, xscale, yscale, wedges, layers, num_colours, colours, std::begin(cont.m_vcont), std::begin(cont.m_icont));
				cont.m_bbox = props.m_bbox;
				cont.AddNugget(EPrim::TriList, props.m_geom, props.m_has_alpha, mat);

				// Create the model
				return Create(rdr, cont, o2w);
			}

			// Capsule ****************************************************************************
			//void        CapsuleSize(Range& vrange, Range& irange, int divisions);
			//Settings    CapsuleModelSettings(int divisions);
			//ModelPtr    CapsuleHRxy(MLock& mlock  ,MaterialManager& matmgr ,float height ,float xradius ,float yradius ,m4x4 const& o2w = pr::m4x4Identity ,int divisions = 3 ,Colour32 colour = Colour32White ,rdr::Material const* mat = 0 ,Range* vrange = 0 ,Range* irange = 0);
			//ModelPtr    CapsuleHRxy(Renderer& rdr                          ,float height ,float xradius ,float yradius ,m4x4 const& o2w = pr::m4x4Identity ,int divisions = 3 ,Colour32 colour = Colour32White ,rdr::Material const* mat = 0 ,Range* vrange = 0 ,Range* irange = 0);

			// Extrude *******************************************************************************
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
				auto& cont = CacheCont(vcount, icount);
				auto props = pr::geometry::Extrude(cs_count, cs, path_count, make_path, closed, smooth_cs, num_colours, colours, std::begin(cont.m_vcont), std::begin(cont.m_icont));
				cont.m_bbox = props.m_bbox;
				cont.AddNugget(EPrim::TriList, props.m_geom, props.m_has_alpha, mat);

				// Create the model
				return Create(rdr, cont, o2w);
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
				auto& cont = CacheCont(vcount, icount);
				auto props = pr::geometry::Extrude(cs_count, cs, path_count, make_path, closed, smooth_cs, num_colours, colours, std::begin(cont.m_vcont), std::begin(cont.m_icont));
				cont.m_bbox = props.m_bbox;
				cont.AddNugget(EPrim::TriList, props.m_geom, props.m_has_alpha, mat);

				// Create the model
				return Create(rdr, cont, o2w);
			}

			// Mesh *******************************************************************************
			static ModelPtr Mesh(Renderer& rdr, MeshCreationData const& cdata)
			{
				// Calculate the required buffer sizes
				int vcount, icount;
				pr::geometry::MeshSize(cdata.m_vcount, cdata.m_icount, vcount, icount);

				// Generate the geometry
				auto& cont = CacheCont(vcount, icount);
				auto props = pr::geometry::Mesh(
					cdata.m_vcount, cdata.m_icount,
					cdata.m_verts, cdata.m_indices,
					cdata.m_ccount, cdata.m_colours,
					cdata.m_ncount, cdata.m_normals,
					cdata.m_tex_coords,
					std::begin(cont.m_vcont), std::begin(cont.m_icont));
				cont.m_bbox = props.m_bbox;

				// Create the nuggets
				cont.m_ncont.insert(std::begin(cont.m_ncont), cdata.m_nuggets, cdata.m_nuggets + cdata.m_gcount);

				// Create the model
				return Create(rdr, cont);
			}

			// ModelFile **************************************************************************
			// Populates 'cont' from 'src' which is expected to be a p3d model file stream.
			// 'P3D' models can contain more than one mesh. If 'mesh_name' is nullptr, then the
			// first mesh in the scene is loaded. If not null, then the first mesh that matches
			// 'mesh_name' is loaded. If 'mesh_name' is non-null and 'src' does not contain a matching
			// mesh, an exception is thrown.
			static void LoadP3DModel(Renderer& rdr, std::istream& src, char const* mesh_name, Cont& cont)
			{
				using namespace pr::geometry::p3d;
				static_assert(sizeof(Cont::VType) == sizeof(Vert), "P3D vert is a different size");
				static_assert(sizeof(Cont::IType) == sizeof(u16), "P3D index is a different size");

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
					cont.m_name = mesh.m_name;
					cont.m_bbox = mesh.m_bbox;

					// Copy the verts
					cont.m_vcont.resize(mesh.m_verts.size());
					memcpy(cont.m_vcont.data(), mesh.m_verts.data(), sizeof(Vert) * mesh.m_verts.size());

					// Copy the indices
					cont.m_icont.resize(mesh.m_idx16.size());
					memcpy(cont.m_icont.data(), mesh.m_idx16.data(), sizeof(u16) * mesh.m_idx16.size());

					// Copy the nuggets
					cont.m_ncont.reserve(mesh.m_nugget.size());
					for (auto& nug : mesh.m_nugget)
					{
						cont.m_ncont.emplace_back(
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
			static void Load3DSModel(Renderer& rdr, std::istream& src, char const* mesh_name, Cont& cont)
			{
				using namespace pr::geometry::max_3ds;

				// Bounding box
				cont.m_bbox = BBoxReset;
				auto bb = [&](v4 const& v){ Encompass(cont.m_bbox, v); return v; };

				// Output callback functions
				auto vout = [&](v4 const& p, Colour const& c, v4 const& n, v2 const& t)
				{
					VType vert;
					SetPCNT(vert, bb(p), c, n, t);
					cont.m_vcont.push_back(vert);
				};
				auto iout = [&](pr::uint16 i0, pr::uint16 i1, pr::uint16 i2)
				{
					cont.m_icont.push_back(i0);
					cont.m_icont.push_back(i1);
					cont.m_icont.push_back(i2);
				};
				auto nout = [&](Material const& mat, EGeom geom, Range vrange, Range irange)
				{
					NuggetProps ddata(EPrim::TriList, geom, nullptr, vrange, irange);
					ddata.m_has_alpha = !FEql(mat.m_diffuse.a, 1.0f);

					// Register any materials with the renderer
					if (!mat.m_textures.empty())
					{
						// This is tricky, textures are likely to be jpg's, or pngs
						// but the renderer only supports dds at the moment.
						// Also, we only support one diffuse texture per nugget currently

						(void)rdr;
						//ddata.m_tex_diffuse = rdr.m_tex_mgr.CreateTexture2D();
					}

					cont.m_ncont.push_back(ddata);
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
				auto& cont = CacheCont();
				LoadP3DModel(rdr, src, mesh_name, cont);
				return Create(rdr, cont, bake, gen_normals);
			}
			static ModelPtr Load3DSModel(Renderer& rdr, std::istream& src, char const* mesh_name = nullptr, m4x4 const* bake = nullptr, float gen_normals = -1.0f)
			{
				auto& cont = CacheCont();
				Load3DSModel(rdr, src, mesh_name, cont);
				return Create(rdr, cont, bake, gen_normals);
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
		};
	}
}

			/*

			//// Utility ****************************************************************************
			//// Generate normals for this model
			//// Assumes the locked region of the model contains a triangle list
			//// Note: you're probably better off generating the normals before creating the model.
			//// That way you don't need to create a buffer that has CPU read/write access
			//template <typename TVert> void GenerateNormals(MLock& mlock, Range const* vrange = 0, Range const* irange = 0)
			//{
			//	if (!vrange) vrange = &mlock.m_vrange;
			//	if (!irange) irange = &mlock.m_irange;
			//	PR_ASSERT(PR_DBG_RDR, IsWithin(mlock.m_vlock.m_range, *vrange), "The provided vertex range is not within the locked range");
			//	PR_ASSERT(PR_DBG_RDR, IsWithin(mlock.m_ilock.m_range, *irange), "The provided index range is not within the locked range");
			//	PR_ASSERT(PR_DBG_RDR, (irange->size() % 3) == 0, "This function assumes the index range refers to a triangle list");

			//	TVert* verts = mlock.m_vlock.ptr<TVert>()
			//	pr::geometry::GenerateNormals();
			//}
			//template <typename TVert> void GenerateNormals(ModelPtr& model, Range const* vrange, Range const* irange)
			//{
			//	MLock mlock(model);
			//	GenerateNormals<TVert>(mlock, vrange, irange);
			//}
			//void SetVertexColours(MLock& mlock, Colour32 colour, Range const* vrange = 0)
			//{
			//	if (!vrange) vrange = &mlock.m_vrange;
			//	PR_ASSERT(PR_DBG_RDR, IsWithin(mlock.m_vlock.m_range, *vrange), "The provided vertex range is not within the locked range");

			//	TVert* vb = mlock.m_vlock.ptr + v_range->m_begin;
			//	for (std::size_t v = 0; v != v_range->size(); ++v, ++vb)
			//		vb->colour() = colour;
			//}


			// Helper function for creating a model
			template <typename GenFunc>
			static ModelPtr Create(Renderer& rdr, std::size_t vcount, std::size_t icount, EPrim topo, NuggetProps const* ddata_, GenFunc& GenerateFunc, m4x4 const* bake = nullptr)
			{
				// Generate the model in local buffers
				Cont cont(vcount, icount);
				pr::geometry::Props props = GenerateFunc(begin(cont.m_vcont), begin(cont.m_icont));

				// Bake a transform into the model
				if (bake)
				{
					props.m_bbox = *bake * props.m_bbox;
					for (auto& v : cont.m_vcont)
					{
						v.m_vert = *bake * v.m_vert;
						v.m_norm = *bake * v.m_norm;
					}
					if (Determinant3(*bake) < 0) // flip faces
					{
						switch (topo) {
						case EPrim::TriList:
							for (size_t i = 0, iend = cont.m_icont.size(); i != iend; i += 3)
								std::swap(cont.m_icont[i+1], cont.m_icont[i+2]);
							break;
						case EPrim::TriStrip:
							if (!cont.m_icont.empty())
								cont.m_icont.insert(begin(cont.m_icont), *begin(cont.m_icont));
							break;
						}
					}
				}

				// Create the model
				VBufferDesc vb(cont.m_vcont.size(), &cont.m_vcont[0]);
				IBufferDesc ib(cont.m_icont.size(), &cont.m_icont[0]);
				ModelPtr model = rdr.m_mdl_mgr.CreateModel(MdlSettings(vb, ib, props.m_bbox));

				// Default nugget creation for the model
				NuggetProps ddata;

				// If draw data is provided, use it
				if (ddata_ != nullptr)
					ddata = *ddata_;

				// Set primitive type, this is non-negotiable
				ddata.m_topo = topo;

				// Default the geometry type from the generate function
				if (ddata.m_geom == EGeom::Invalid)
					ddata.m_geom = props.m_geom;

				// If the model geom has valid texture data but no texture, use white
				if (AllSet(ddata.m_geom, EGeom::Tex0) && ddata.m_tex_diffuse == nullptr)
					ddata.m_tex_diffuse = rdr.m_tex_mgr.FindTexture(EStockTexture::White);

				// If the model has alpha, set the alpha blending state
				if (props.m_has_alpha)
					SetAlphaBlending(ddata, true);

				// Create the render nugget
				model->CreateNugget(ddata);
				return model;
			}
			*/