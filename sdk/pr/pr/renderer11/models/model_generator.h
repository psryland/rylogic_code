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
		template <typename VType = Vert, typename IType = pr::uint16>
		struct ModelGenerator
		{
			// A container for the model data
			struct Cont
			{
				typedef VType VType;
				typedef IType IType;

				typedef std::vector<VType> VCont;
				typedef std::vector<IType> ICont;
				typedef std::vector<NuggetProps> NCont;

				typedef typename VCont::iterator VIter;
				typedef typename ICont::iterator IIter;
				typedef typename NCont::iterator NIter;

				VCont m_vcont; // Model verts
				ICont m_icont; // Model faces/lines/points/etc
				NCont m_ncont; // Model nuggets
				BBox  m_bbox;  // Model bounding box
				EGeom m_geom;  // Geometry type

				enum { GeomMask = VType::GeomMask };

				Cont(std::size_t vcount = 0, std::size_t icount = 0, std::size_t ncount = 0)
					:m_vcont(vcount)
					,m_icont(icount)
					,m_ncont(ncount)
					,m_bbox(BBoxReset)
					,m_geom(GeomMask)
				{}
			};

			// Create a model from 'cont'
			// 'topo' is the topology of the model data. Ignored if 'cont' contains nuggets
			// 'mat' contains some default settings for the default nugget. Ignored if 'cont' contains nuggets
			// 'alpha' if true, the default nugget will have alpha enabled. Ignored if 'cont' contains nuggets
			// 'bake' is a transform to bake into the model
			// 'gen_normals' generates normals for the model if >= 0f. Value is the threshold for smoothing (in rad)
			static ModelPtr Create(Renderer& rdr, Cont& cont, EPrim topo = EPrim::Invalid, NuggetProps const* mat = nullptr, bool alpha = false, m4x4 const* bake = nullptr, float gen_normals = -1.0f)
			{
				// Bake a transform into the model
				if (bake)
				{
					cont.m_bbox = *bake * cont.m_bbox;
					for (auto& v : cont.m_vcont)
					{
						v.m_vert = *bake * v.m_vert;
						v.m_norm = *bake * v.m_norm;
					}
					if (Determinant3(*bake) < 0) // flip faces
					{
						if (topo == EPrim::TriList)
						{
							for (size_t i = 0, iend = cont.m_icont.size(); i != iend; i += 3)
								std::swap(cont.m_icont[i+1], cont.m_icont[i+2]);
						}
						else if (topo == EPrim::TriStrip)
						{
							if (!cont.m_icont.empty())
								cont.m_icont.insert(begin(cont.m_icont), *begin(cont.m_icont));
						}
					}
				}

				// Generate normals
				if (gen_normals >= 0.0f)
				{
					if (topo == EPrim::TriList)
					{
						auto iout = std::begin(cont.m_icont);
						pr::geometry::GenerateNormals(cont.m_icont.size(), cont.m_icont.data(), gen_normals
							,[&](Cont::IType idx)
							{
								return GetP(cont.m_vcont[idx]);
							}
							,[&](Cont::IType idx, Cont::IType orig, v4 const& norm)
							{
								if (idx >= cont.m_vcont.size()) cont.m_vcont.push_back(cont.m_vcont[orig]);
								SetN(cont.m_vcont[idx], norm);
							}
							,[&](Cont::IType i0, Cont::IType i1, Cont::IType i2)
							{
								*iout++ = i0;
								*iout++ = i1;
								*iout++ = i2;
							});
					}
				}

				// If no nuggets have been provided, create one for the whole model
				if (cont.m_ncont.empty())
				{
					NuggetProps nug;
					if (mat != nullptr) nug = *mat;
					nug.m_topo = topo;
					SetAlphaBlending(nug, alpha);
					cont.m_ncont.push_back(nug);
				}

				// Create the model
				VBufferDesc vb(cont.m_vcont.size(), cont.m_vcont.data());
				IBufferDesc ib(cont.m_icont.size(), cont.m_icont.data());
				auto model = rdr.m_mdl_mgr.CreateModel(MdlSettings(vb, ib, cont.m_bbox));

				// Create the render nuggets
				for (auto& nug : cont.m_ncont)
				{
					// Default the geometry type
					if (nug.m_geom == EGeom::Invalid)
						nug.m_geom = cont.m_geom;

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
			static ModelPtr Lines(Renderer& rdr, std::size_t num_lines, v4 const* points, std::size_t num_colours = 0, Colour32 const* colours = nullptr, NuggetProps const* mat = nullptr)
			{
				std::size_t vcount, icount;
				pr::geometry::LineSize(num_lines, vcount, icount);

				Cont cont(vcount, icount);
				auto props = pr::geometry::Lines(num_lines, points, num_colours, colours, begin(cont.m_vcont), begin(cont.m_icont));
				cont.m_bbox = props.m_bbox;
				cont.m_geom = props.m_geom;
				return Create(rdr, cont, EPrim::LineList, mat, props.m_has_alpha, nullptr, -1.0f);
			}
			static ModelPtr LinesD(Renderer& rdr, std::size_t num_lines, v4 const* points, v4 const* directions, std::size_t num_colours = 0, Colour32 const* colours = nullptr, NuggetProps const* mat = nullptr)
			{
				std::size_t vrange, irange;
				pr::geometry::LineSize(num_lines, vcount, icount);

				Cont cont(vcount, icount);
				auto props = pr::geometry::LinesD(num_lines, points, directions, num_colours, colours, begin(cont.m_vcont), begin(cont.m_icont));
				cont.m_bbox = props.m_bbox;
				cont.m_geom = props.m_geom;
				return Create(rdr, cont, EPrim::LineList, mat, props.m_has_alpha, nullptr, -1.0f);
			}
			static ModelPtr LineStrip(Renderer& rdr, std::size_t num_lines, v4 const* points, std::size_t num_colours = 0, Colour32 const* colour = nullptr, NuggetProps const* mat = nullptr)
			{
				std::size_t vcount, icount;
				pr::geometry::LineStripSize(num_lines, vcount, icount);

				Cont cont(vcount, icount);
				auto props = pr::geometry::LinesStrip(num_lines, points, num_colours, colour, begin(cont.m_vcont), begin(cont.m_icont));
				cont.m_bbox = props.m_bbox;
				cont.m_geom = props.m_geom;
				return Create(rdr, cont, EPrim::LineStrip, mat, props.m_has_alpha, nullptr, -1.0f);
			}

			// Quad *******************************************************************************
			static ModelPtr Quad(Renderer& rdr, size_t num_quads, v4 const* verts, size_t num_colours = 0, Colour32 const* colours = nullptr, m4x4 const& t2q = m4x4Identity, NuggetProps const* mat = nullptr)
			{
				std::size_t vcount, icount;
				pr::geometry::QuadSize(num_quads, vcount, icount);

				Cont cont(vcount, icount);
				auto props = pr::geometry::Quad(num_quads, verts, num_colours, colours, t2q, begin(cont.m_vcont), begin(cont.m_icont));
				cont.m_bbox = props.m_bbox;
				cont.m_geom = props.m_geom;
				return Create(rdr, cont, EPrim::TriList, mat, props.m_has_alpha, nullptr, -1.0f);
			}
			static ModelPtr Quad(Renderer& rdr, v4 const& origin, v4 const& patch_x, v4 const& patch_y, iv2 const& divisions = iv2Zero, Colour32 colour = Colour32White, m4x4 const& t2q = m4x4Identity, NuggetProps const* mat = nullptr)
			{
				std::size_t vcount, icount;
				pr::geometry::QuadSize(divisions, vcount, icount);

				Cont cont(vcount, icount);
				auto props = pr::geometry::Quad(origin, quad_x, quad_z, divisions, colour, t2q, begin(cont.m_vcont), begin(cont.m_icont));
				cont.m_bbox = props.m_bbox;
				cont.m_geom = props.m_geom;
				return Create(rdr, cont, EPrim::TriList, mat, props.m_has_alpha, nullptr, -1.0f);
			}
			static ModelPtr Quad(Renderer& rdr, float width, float height, iv2 const& divisions = iv2Zero, Colour32 colour = Colour32White, NuggetProps const* mat = nullptr)
			{
				std::size_t vcount, icount;
				pr::geometry::QuadSize(divisions, vcount, icount);

				Cont cont(vcount, icount);
				auto props = pr::geometry::Quad(width, height, divisions, colour, begin(cont.m_vcont), begin(cont.m_icont));
				cont.m_bbox = props.m_bbox;
				cont.m_geom = props.m_geom;
				return Create(rdr, cont, EPrim::TriList, mat, props.m_has_alpha, nullptr, -1.0f);
			}
			static ModelPtr Quad(Renderer& rdr, v4 const& centre, v4 const& forward, v4 const& top, float width, float height, iv2 const& divisions = iv2Zero, Colour32 colour = Colour32White, v2 const& tex_origin = v2Zero, v2 const& tex_dim = v2One, NuggetProps const* mat = nullptr)
			{
				std::size_t vcount, icount;
				pr::geometry::QuadSize(divisions, vcount, icount);

				Cont cont(vcount, icount);
				auto props = pr::geometry::Quad(centre, forward, top, width, height, divisions, colour, tex_origin, tex_dim, begin(cont.m_vcont), begin(cont.m_icont));
				cont.m_bbox = props.m_bbox;
				cont.m_geom = props.m_geom;
				return Create(rdr, cont, EPrim::TriList, mat, props.m_has_alpha, nullptr, -1.0f);
			}
			static ModelPtr QuadStrip(Renderer& rdr, size_t num_quads, v4 const* verts, float width, size_t num_normals = 0, v4 const* normals = nullptr, size_t num_colours = 0, Colour32 const* colours = nullptr, NuggetProps const* mat = nullptr)
			{
				std::size_t vcount, icount;
				pr::geometry::QuadStripSize(num_quads, vcount, icount);

				Cont cont(vcount, icount);
				auto props = pr::geometry::QuadStrip(num_quads, verts, width, num_normals, normals, num_colours, colours, begin(cont.m_vcont), begin(cont.m_icont));
				cont.m_bbox = props.m_bbox;
				cont.m_geom = props.m_geom;
				return Create(rdr, cont, EPrim::TriStrip, mat, props.m_has_alpha, nullptr, -1.0f);
			}

			// Shape2d ****************************************************************************
			static ModelPtr Ellipse(Renderer& rdr, float dimx, float dimy, bool solid, int facets = 40, Colour32 colour = Colour32White, m4x4 const* o2w = nullptr, NuggetProps const* mat = nullptr)
			{
				std::size_t vcount, icount;
				pr::geometry::EllipseSize(solid, facets, vcount, icount);

				Cont cont(vcount, icount);
				auto props = pr::geometry::Ellipse(dimx, dimy, solid, facets, colour, begin(cont.m_vcont), begin(cont.m_icont));
				cont.m_bbox = props.m_bbox;
				cont.m_geom = props.m_geom;
				return Create(rdr, cont, solid ? EPrim::TriStrip : EPrim::LineStrip, mat, props.m_has_alpha, o2w, -1.0f);
			}
			static ModelPtr Pie(Renderer& rdr, float dimx, float dimy, float ang0, float ang1, float radius0, float radius1, bool solid, int facets = 40, Colour32 colour = Colour32White, m4x4 const* o2w = nullptr, NuggetProps const* mat = nullptr)
			{
				std::size_t vcount, icount;
				pr::geometry::PieSize(solid, ang0, ang1, facets, vcount, icount);

				Cont cont(vcount, icount);
				auto props = pr::geometry::Pie(dimx, dimy, ang0, ang1, radius0, radius1, solid, facets, colour, begin(cont.m_vcont), begin(cont.m_icont));
				cont.m_bbox = props.m_bbox;
				cont.m_geom = props.m_geom;
				return Create(rdr, cont, solid ? EPrim::TriStrip : EPrim::LineStrip, mat, props.m_has_alpha, o2w, -1.0f);
			}
			static ModelPtr RoundedRectangle(Renderer& rdr, float dimx, float dimy, float corner_radius, bool solid, int facets = 10, Colour32 colour = Colour32White, m4x4 const* o2w = nullptr, NuggetProps const* mat = nullptr)
			{
				std::size_t vcount, icount;
				pr::geometry::RoundedRectangleSize(solid, corner_radius, facets, vcount, icount);

				Cont cont(vcount, icount);
				auto props = pr::geometry::RoundedRectangle(dimx, dimy, solid, corner_radius, facets, colour, begin(cont.m_vcont), begin(cont.m_icont));
				cont.m_bbox = props.m_bbox;
				cont.m_geom = props.m_geom;
				return Create(rdr, cont, solid ? EPrim::TriStrip : EPrim::LineStrip, mat, props.m_has_alpha, o2w, -1.0f);
			}

			// Boxes ******************************************************************************
			static ModelPtr Boxes(Renderer& rdr, std::size_t num_boxes, v4 const* points, std::size_t num_colours = 0, Colour32 const* colours = nullptr, NuggetProps const* mat = nullptr)
			{
				std::size_t vcount, icount;
				pr::geometry::BoxSize(num_boxes, vcount, icount);

				Cont cont(vcount, icount);
				auto props = pr::geometry::Boxes(num_boxes, points, num_colours, colours, begin(cont.m_vcont), begin(cont.m_icont));
				cont.m_bbox = props.m_bbox;
				cont.m_geom = props.m_geom;
				return Create(rdr, cont, EPrim::TriList, mat, props.m_has_alpha, nullptr, -1.0f);
			}
			static ModelPtr Boxes(Renderer& rdr, std::size_t num_boxes, v4 const* points, m4x4 const& o2w, std::size_t num_colours = 0, Colour32 const* colours = nullptr, NuggetProps const* mat = nullptr)
			{
				std::size_t vcount, icount;
				pr::geometry::BoxSize(num_boxes, vcount, icount);

				Cont cont(vcount, icount);
				auto props = pr::geometry::Boxes(num_boxes, points, o2w, num_colours, colours, begin(cont.m_vcont), begin(cont.m_icont));
				cont.m_bbox = props.m_bbox;
				cont.m_geom = props.m_geom;
				return Create(rdr, cont, EPrim::TriList, mat, props.m_has_alpha, nullptr, -1.0f);
			}
			static ModelPtr Box(Renderer& rdr, v4 const& rad, m4x4 const& o2w = m4x4Identity, Colour32 colour = Colour32White, NuggetProps const* mat = nullptr)
			{
				std::size_t vcount, icount;
				pr::geometry::BoxSize(1, vcount, icount);

				Cont cont(vcount, icount);
				auto props = pr::geometry::Box(rad, o2w, colour, begin(cont.m_vcont), begin(cont.m_icont));
				cont.m_bbox = props.m_bbox;
				cont.m_geom = props.m_geom;
				return Create(rdr, cont, EPrim::TriList, mat, props.m_has_alpha, nullptr, -1.0f);
			}
			static ModelPtr Box(Renderer& rdr, float rad, m4x4 const& o2w = m4x4Identity, Colour32 colour = Colour32White, NuggetProps const* mat = nullptr)
			{
				return Box(rdr, v4::make(rad), o2w, colour, mat);
			}
			static ModelPtr BoxList(Renderer& rdr, std::size_t num_boxes, v4 const* positions, v4 const& rad, std::size_t num_colours = 0, Colour32 const* colours = nullptr, NuggetProps const* mat = nullptr)
			{
				std::size_t vcount, icount;
				pr::geometry::BoxSize(num_boxes, vcount, icount);

				Cont cont(vcount, icount);
				auto props = pr::geometry::BoxList(num_boxes, positions, rad, num_colours, colours, begin(cont.m_vcont), begin(cont.m_icont));
				cont.m_bbox = props.m_bbox;
				cont.m_geom = props.m_geom;
				return Create(rdr, cont, EPrim::TriList, mat, props.m_has_alpha, nullptr, -1.0f);
			}

			// Sphere *****************************************************************************
			static ModelPtr Geosphere(Renderer& rdr, v4 const& radius, std::size_t divisions = 3, Colour32 colour = Colour32White, NuggetProps const* mat = nullptr)
			{
				std::size_t vcount, icount;
				pr::geometry::GeosphereSize(divisions, vcount, icount);

				Cont cont(vcount, icount);
				auto props = pr::geometry::Geosphere(radius, divisions, colour, begin(cont.m_vcont), begin(cont.m_icont));
				cont.m_bbox = props.m_bbox;
				cont.m_geom = props.m_geom;
				return Create(rdr, cont, EPrim::TriList, mat, props.m_has_alpha, nullptr, -1.0f);
			}
			static ModelPtr Geosphere(Renderer& rdr, float radius, std::size_t divisions = 3, Colour32 colour = Colour32White, NuggetProps const* mat = nullptr)
			{
				return Geosphere(rdr, v4::make(radius, 0.0f), divisions, colour, mat);
			}
			static ModelPtr Sphere(Renderer& rdr, v4 const& radius, std::size_t wedges = 20, std::size_t layers = 5, Colour32 colour = Colour32White, NuggetProps const* mat = nullptr)
			{
				std::size_t vcount, icount;
				pr::geometry::SphereSize(wedges, layers, vcount, icount);

				Cont cont(vcount, icount);
				auto props = pr::geometry::Sphere(radius, wedges, layers, colour, begin(cont.m_vcont), begin(cont.m_icont));
				cont.m_bbox = props.m_bbox;
				cont.m_geom = props.m_geom;
				return Create(rdr, cont, EPrim::TriList, mat, props.m_has_alpha, nullptr, -1.0f);
			}
			static ModelPtr Sphere(Renderer& rdr, float radius, std::size_t wedges = 20, std::size_t layers = 5, Colour32 colour = Colour32White, NuggetProps const* mat = nullptr)
			{
				return Sphere(rdr, v4::make(radius, 0.0f), wedges, layers, colour, mat);
			}

			// Cylinder ***************************************************************************
			static ModelPtr Cylinder(Renderer& rdr, float radius0, float radius1, float height, float xscale = 1.0f, float yscale = 1.0f, std::size_t wedges = 20, std::size_t layers = 1, std::size_t num_colours = 0, Colour32 const* colours = nullptr, m4x4 const* o2w = nullptr, NuggetProps const* mat = nullptr)
			{
				std::size_t vcount, icount;
				pr::geometry::CylinderSize(wedges, layers, vcount, icount);

				Cont cont(vcount, icount);
				auto props = pr::geometry::Cylinder(radius0, radius1, height, xscale, yscale, wedges, layers, num_colours, colours, begin(cont.m_vcont), begin(cont.m_icont));
				cont.m_bbox = props.m_bbox;
				cont.m_geom = props.m_geom;
				return Create(rdr, cont, EPrim::TriList, mat, props.m_has_alpha, o2w, -1.0f);
			}

			// Capsule ****************************************************************************
			//void        CapsuleSize(Range& vrange, Range& irange, std::size_t divisions);
			//Settings    CapsuleModelSettings(std::size_t divisions);
			//ModelPtr    CapsuleHRxy(MLock& mlock  ,MaterialManager& matmgr ,float height ,float xradius ,float yradius ,m4x4 const& o2w = pr::m4x4Identity ,std::size_t divisions = 3 ,Colour32 colour = Colour32White ,rdr::Material const* mat = 0 ,Range* vrange = 0 ,Range* irange = 0);
			//ModelPtr    CapsuleHRxy(Renderer& rdr                          ,float height ,float xradius ,float yradius ,m4x4 const& o2w = pr::m4x4Identity ,std::size_t divisions = 3 ,Colour32 colour = Colour32White ,rdr::Material const* mat = 0 ,Range* vrange = 0 ,Range* irange = 0);

			// Mesh *******************************************************************************
			static ModelPtr Mesh(Renderer& rdr, EPrim prim_type, std::size_t num_verts, std::size_t num_indices, v4 const* verts, pr::uint16 const* indices, std::size_t num_colours = 0, Colour32 const* colours = nullptr, std::size_t num_normals = 0, v4 const* normals = 0, v2 const* tex_coords = 0, NuggetProps const* mat = nullptr)
			{
				std::size_t vcount, icount;
				pr::geometry::MeshSize(num_verts, num_indices, vcount, icount);

				Cont cont(vcount, icount);
				auto props = pr::geometry::Mesh(num_verts, num_indices, verts, indices, num_colours, colours, num_normals, normals, tex_coords, begin(cont.m_vcont), begin(cont.m_icont));
				cont.m_bbox = props.m_bbox;
				cont.m_geom = props.m_geom;
				return Create(rdr, cont, prim_type, mat, props.m_has_alpha, nullptr, -1.0f);
			}

			// ModelFile **************************************************************************
			static void Load3DSModel(Renderer& rdr, std::istream& src, Cont& cont)
			{
				// Populates 'cont' from 'src'
				using namespace pr::geometry;

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
				auto nout = [&](max_3ds::Material const& mat, EGeom geom, Range vrange, Range irange)
				{
					NuggetProps ddata(EPrim::TriList, geom, nullptr, vrange, irange);

					// Register any materials with the renderer
					if (!mat.m_textures.empty())
					{
						// This is tricky, textures are likely to be jpg's, or pngs
						// but the renderer only supports dds at the moment.
						// Also, we only support one diffuse texture per nugget currently

						(void)rdr;
						//ddata.m_tex_diffuse = rdr.m_tex_mgr.CreateTexture2D();
					}

					// If the material has alpha, set the alpha blending states in the nugget
					if (!FEql(mat.m_diffuse.a, 1.0f))
						SetAlphaBlending(ddata, true);

					cont.m_ncont.push_back(ddata);
				};

				// Material lookup
				std::unordered_map<std::string, max_3ds::Material> mats;
				auto matlookup = [&](std::string const& name) { return mats.at(name); };

				// Parse the materials in the 3ds stream
				max_3ds::ReadMaterials(src, [&](max_3ds::Material&& mat)
					{
						mats[mat.m_name] = mat;
					});

				// Parse the model objects in the 3ds stream
				max_3ds::ReadObjects(src, [&](max_3ds::Object&& obj)
					{
						max_3ds::CreateModel(obj, matlookup, nout, vout, iout);
					});
			}
			static ModelPtr Load3DSModel(Renderer& rdr, std::istream& src, m4x4 const* bake = nullptr, float gen_normals = -1.0f)
			{
				Cont cont(0,0);
				Load3DSModel(rdr, src, cont);
				return Create(rdr, cont, EPrim::TriList, nullptr, false, bake, gen_normals);
			}
			static ModelPtr LoadModel(Renderer& rdr, pr::geometry::EModelFileFormat format, std::istream& src, m4x4 const* bake = nullptr, float gen_normals = -1.0f)
			{
				using namespace pr::geometry;
				switch (format)
				{
				default: throw std::exception("Unsupported model file format");
				case EModelFileFormat::Max3DS: return Load3DSModel(rdr, src, bake, gen_normals);
				}
			}

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
		};
	}
}

			/*
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