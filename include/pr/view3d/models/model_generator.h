//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2007
//*********************************************
#pragma once
#include "pr/view3d/forward.h"
#include "pr/view3d/models/model.h"
#include "pr/view3d/models/model_tree.h"
#include "pr/view3d/render/renderer.h"
#include "pr/view3d/shaders/input_layout.h"

namespace pr::rdr
{
	// Parameters structure for creating mesh models
	struct MeshCreationData
	{
		int m_vcount; // The length of the 'verts' array
		int m_icount; // The length of the 'indices' array
		int m_gcount; // The length of the 'nuggets' array
		int m_ccount; // The length of the 'colours' array. 0, 1, or 'vcount'
		int m_ncount; // The length of the 'normals' array. 0, 1, or 'vcount'
		v4          const* m_verts;      // The vertex data for the model
		uint16_t    const* m_indices;    // The index data for the model
		NuggetProps const* m_nuggets;    // The nugget data for the model
		Colour32    const* m_colours;    // The colour data for the model. Typically nullptr, 1, or 'vcount' colours
		v4          const* m_normals;    // The normal data for the model. Typically nullptr or a pointer to 'vcount' normals
		v2          const* m_tex_coords; // The texture coordinates data for the model. nullptr or a pointer to 'vcount' texture coords

		MeshCreationData() :m_vcount() ,m_icount() ,m_gcount() ,m_ccount() ,m_ncount() ,m_verts() ,m_indices() ,m_nuggets() ,m_colours() ,m_normals() ,m_tex_coords() {}
		MeshCreationData& verts(v4 const* vbuf, int count)
		{
			assert(count == 0 || vbuf != nullptr);
			assert(maths::is_aligned(vbuf));
			m_vcount = count;
			m_verts = vbuf;
			return *this;
		}
		MeshCreationData& indices(uint16_t const* ibuf, int count)
		{
			assert(count == 0 || ibuf != nullptr);
			m_icount = count;
			m_indices = ibuf;
			return *this;
		}
		MeshCreationData& nuggets(NuggetProps const* gbuf, int count)
		{
			assert(count == 0 || gbuf != nullptr);
			m_gcount = count;
			m_nuggets = gbuf;
			return *this;
		}
		MeshCreationData& colours(Colour32 const* cbuf, int count)
		{
			// Count doesn't have to be 0, 1, or 'vcount' because interpolation is used
			assert(count == 0 || cbuf != nullptr);
			m_ccount = count;
			m_colours = cbuf;
			return *this;
		}
		MeshCreationData& normals(v4 const* nbuf, int count)
		{
			// Count doesn't have to be 0, 1, or 'vcount' because interpolation is used
			assert(count == 0 || nbuf != nullptr);
			assert(maths::is_aligned(nbuf));
			m_ncount = count;
			m_normals = nbuf;
			return *this;
		}
		MeshCreationData& tex(v2 const* tbuf, int count)
		{
			// Count doesn't have to be 0, 1, or 'vcount' because interpolation is used
			assert(count == 0 || tbuf != nullptr);
			m_tex_coords = tbuf;
			(void)count;
			return *this;
		}

		MeshCreationData& verts(std::initializer_list<v4> vbuf)
		{
			assert(maths::is_aligned(vbuf.begin()));
			m_vcount = int(vbuf.size());
			m_verts = vbuf.begin();
			return *this;
		}
		MeshCreationData& indices(std::initializer_list<uint16_t> ibuf)
		{
			m_icount = int(ibuf.size());
			m_indices = ibuf.begin();
			return *this;
		}
		MeshCreationData& nuggets(std::initializer_list<rdr::NuggetProps> gbuf)
		{
			m_gcount = int(gbuf.size());
			m_nuggets = gbuf.begin();
			return *this;
		}
		MeshCreationData& colours(std::initializer_list<Colour32> cbuf)
		{
			m_ccount = int(cbuf.size());
			m_colours = cbuf.begin();
			return *this;
		}
		MeshCreationData& normals(std::initializer_list<v4> nbuf)
		{
			assert(int(nbuf.size()) == m_vcount);
			assert(maths::is_aligned(nbuf.begin()));
			m_ncount = int(nbuf.size());
			m_normals = nbuf.begin();
			return *this;
		}
		MeshCreationData& tex(std::initializer_list<v2> tbuf)
		{
			assert(int(tbuf.size()) == m_vcount);
			m_tex_coords = tbuf.begin();
			return *this;
		}

		template <int N> MeshCreationData& verts(v4 const (&vbuf)[N])
		{
			return verts(&vbuf[0], N);
		}
		template <int N> MeshCreationData& indices(uint16_t const (&ibuf)[N])
		{
			return indices(&ibuf[0], N);
		}
		template <int N> MeshCreationData& nuggets(rdr::NuggetProps const (&nbuf)[N])
		{
			return nuggets(&nbuf[0], N);
		}
		template <int N> MeshCreationData& colours(Colour32 const (&cbuf)[N])
		{
			return colours(&cbuf[0], N);
		}
		template <int N> MeshCreationData& normals(v4 const (&nbuf)[N])
		{
			return normals(&nbuf[0], N);
		}
		template <int N> MeshCreationData& tex(v2 const (&tbuf)[N])
		{
			return tex(&tbuf[0], N);
		}
	};

	// Create model geometry
	template <typename = void>
	struct ModelGenerator
	{
		// Memory pooling for model buffers
		template <typename VertexType = Vert>
		struct Cache
		{
			using VType = VertexType;
			using VCont = vector<VType>;
			using ICont = pr::geometry::IdxBuf;
			using NCont = vector<NuggetProps>;

		private:

			// The cached buffers
			struct alignas(16) Buffers
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

			std::string& m_name; // Model name
			VCont& m_vcont;      // Model verts
			ICont& m_icont;      // Model faces/lines/points/etc
			NCont& m_ncont;      // Model nuggets
			BBox& m_bbox;        // Model bounding box

			Cache() = delete;
			Cache(int vcount, int icount, int ncount, int idx_stride)
				:m_buffers(this_thread_instance())
				,m_in_use(this_thread_cache_in_use())
				,m_name(m_buffers.m_name)
				,m_vcont(m_buffers.m_vcont)
				,m_icont(m_buffers.m_icont)
				,m_ncont(m_buffers.m_ncont)
				,m_bbox(m_buffers.m_bbox)
			{
				assert(vcount >= 0 && icount >= 0 && ncount >= 0 && idx_stride >= 1);
				m_vcont.resize(vcount);
				m_icont.resize(icount * idx_stride);
				m_ncont.resize(ncount);
				m_icont.m_stride = idx_stride;
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
				m_bbox = BBox::Reset();
			}

			// Container item counts
			size_t VCount() const { return m_vcont.size(); }
			size_t ICount() const { return m_icont.count(); }
			size_t NCount() const { return m_ncont.size(); }

			// Return the buffer format associated with the index stride
			DXGI_FORMAT IdxFormat() const
			{
				auto stride = m_icont.stride();
				switch (stride)
				{
				case 4: return dx_format_v<uint32_t>;
				case 2: return dx_format_v<uint16_t>;
				case 1: return dx_format_v<uint8_t>;
				default: throw std::runtime_error(Fmt("Unsupported index stride: %d", stride));
				}
			}

			// Add a nugget to 'm_ncont' (helper)
			void AddNugget(ETopo topo, EGeom geom, bool geometry_has_alpha, bool tint_has_alpha, NuggetProps const* mat = nullptr)
			{
				// Notes:
				// - Don't change the 'geom' flags here based on whether the material has a texture
				//   or not. The texture may be set in the material after here and before the model
				//   is rendered.
				auto nug = mat ? *mat : NuggetProps{};
				nug.m_topo = topo;
				nug.m_geom = geom;
				nug.m_nflags = SetBits(nug.m_nflags, ENuggetFlag::GeometryHasAlpha, geometry_has_alpha);
				nug.m_nflags = SetBits(nug.m_nflags, ENuggetFlag::TintHasAlpha, tint_has_alpha);
				m_ncont.push_back(nug);
			}
		};

		// Implementation functions
		struct Impl
		{
			// Bake a transform into 'cache'
			template <typename VType>
			static void BakeTransform(Cache<VType>& cache, m4x4 const& a2b)
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
								case sizeof(uint32_t): FlipTriListFaces<VType, uint32_t>(cache, nug.m_irange); break;
								case sizeof(uint16_t): FlipTriListFaces<VType, uint16_t>(cache, nug.m_irange); break;
								default: throw std::runtime_error("Unsupported index stride");
								}
								break;
							}
						case ETopo::TriStrip:
							{
								switch (cache.m_icont.stride())
								{
								case sizeof(uint32_t): FlipTriStripFaces<VType, uint32_t>(cache, nug.m_irange); break;
								case sizeof(uint16_t): FlipTriStripFaces<VType, uint16_t>(cache, nug.m_irange); break;
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
			static void FlipTriListFaces(Cache<VType>& cache, Range irange)
			{
				assert((irange.size() % 3) == 0);
				auto ibuf = cache.m_icont.data<IType>();
				for (size_t i = irange.begin(), iend = irange.end(); i != iend; i += 3)
					std::swap(ibuf[i + 1], ibuf[i + 2]);
			}

			// Flip the winding order of faces in a triangle strip
			template <typename VType, typename IType>
			static void FlipTriStripFaces(Cache<VType>& cache, Range irange)
			{
				assert((irange.size() % 2) == 0);
				auto ibuf = cache.m_icont.data<IType>();
				for (size_t i = irange.begin(), iend = irange.end(); i != iend; i += 2)
					std::swap(ibuf[i + 0], ibuf[i + 1]);
			}

			// Generate normals for the triangle list nuggets in 'cache'
			template <typename VType>
			static void GenerateNormals(Cache<VType>& cache, float gen_normals)
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
							case sizeof(uint32_t): GenerateNormals<VType, uint32_t>(cache, nug.m_irange, gen_normals); break;
							case sizeof(uint16_t): GenerateNormals<VType, uint16_t>(cache, nug.m_irange, gen_normals); break;
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
			static void GenerateNormals(Cache<VType>& cache, Range irange, float gen_normals)
			{
				auto ibuf = cache.m_icont.data<IType>() + irange.begin();
				geometry::GenerateNormals(
					irange.size(), ibuf, gen_normals, cache.m_vcont.size(),
					[&](int idx)
					{
						return GetP(cache.m_vcont[idx]);
					},
					[&](int idx, int orig, v4 const& norm)
					{
						assert(idx <= s_cast<int>(cache.m_vcont.size()));
						if (idx == s_cast<int>(cache.m_vcont.size()))
							cache.m_vcont.push_back(cache.m_vcont[orig]);
						//if (idx >= cache.m_vcont.size())
						//	cache.m_vcont.resize(idx + 1, cache.m_vcont[orig]);
						SetN(cache.m_vcont[idx], norm);
					},
					[&](int i0, int i1, int i2)
					{
						*ibuf++ = s_cast<IType>(i0);
						*ibuf++ = s_cast<IType>(i1);
						*ibuf++ = s_cast<IType>(i2);
					});
			}
		};

		// Addition options for model creation
		struct CreateOptions
		{
			// Transform the model verts by the given transform
			// Non-null if a transform is to be applied
			m4x4 m_bake;

			// Algorithmically generate surface normals.
			// Value is the smoothing angle.
			float m_gen_normals;

			explicit CreateOptions(m4x4 const& bake = m4x4Identity, float gen_normals = -1.0f)
				:m_bake(bake)
				,m_gen_normals(gen_normals)
			{}
		};

		// Create a model from 'cache'
		// 'bake' is a transform to bake into the model
		// 'gen_normals' generates normals for the model if >= 0f. Value is the threshold for smoothing (in rad)
		template <typename VType>
		static ModelPtr Create(Renderer& rdr, Cache<VType>& cache, CreateOptions const& opts = CreateOptions{})
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
			if (opts.m_bake != m4x4Identity)
				Impl::BakeTransform(cache, opts.m_bake);

			// Generate normals
			if (opts.m_gen_normals >= 0.0f)
				Impl::GenerateNormals(cache, opts.m_gen_normals);

			// Create the model
			VBufferDesc vb(cache.VCount(), cache.m_vcont.data());
			IBufferDesc ib(cache.ICount(), cache.m_icont.data(), cache.IdxFormat());
			auto model = rdr.m_mdl_mgr.CreateModel(MdlSettings(vb, ib, cache.m_bbox));
			model->m_name = cache.m_name;

			// Create the render nuggets
			for (auto& nug : cache.m_ncont)
			{
				// If the model geom has valid texture data but no texture, use white
				if (AllSet(nug.m_geom, EGeom::Tex0) && nug.m_tex_diffuse == nullptr)
					nug.m_tex_diffuse = rdr.m_tex_mgr.FindTexture<Texture2D>(RdrId(EStockTexture::White));

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
		static ModelPtr Lines(Renderer& rdr, int num_lines, v4 const* points, int num_colours = 0, Colour32 const* colours = nullptr, NuggetProps const* mat = nullptr)
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
		static ModelPtr LinesD(Renderer& rdr, int num_lines, v4 const* points, v4 const* directions, int num_colours = 0, Colour32 const* colours = nullptr, NuggetProps const* mat = nullptr)
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
		static ModelPtr LineStrip(Renderer& rdr, int num_lines, v4 const* points, int num_colours = 0, Colour32 const* colour = nullptr, NuggetProps const* mat = nullptr)
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
		static ModelPtr Quad(Renderer& rdr, NuggetProps const* mat = nullptr)
		{
			v4 const verts[] = { v4{-1,-1,0,1}, v4{+1,-1,0,1}, v4{-1,+1,0,1}, v4{+1,+1,0,1} };
			return Quad(rdr, 1, &verts[0], 0, nullptr, m4x4Identity, mat);
		}
		static ModelPtr Quad(Renderer& rdr, int num_quads, v4 const* verts, int num_colours = 0, Colour32 const* colours = nullptr, m4x4 const& t2q = m4x4Identity, NuggetProps const* mat = nullptr)
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
		static ModelPtr Quad(Renderer& rdr, v2 const& anchor, v4 const& quad_w, v4 const& quad_h, iv2 const& divisions = iv2Zero, Colour32 colour = Colour32White, m4x4 const& t2q = m4x4Identity, NuggetProps const* mat = nullptr)
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
		static ModelPtr Quad(Renderer& rdr, AxisId axis_id, v2 const& anchor, float width, float height, iv2 const& divisions = iv2Zero, Colour32 colour = Colour32White, m4x4 const& t2q = m4x4Identity, NuggetProps const* mat = nullptr)
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
		static ModelPtr QuadStrip(Renderer& rdr, int num_quads, v4 const* verts, float width, int num_normals = 0, v4 const* normals = nullptr, int num_colours = 0, Colour32 const* colours = nullptr, NuggetProps const* mat = nullptr)
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
		static ModelPtr QuadPatch(Renderer& rdr, int dimx, int dimy, NuggetProps const* mat = nullptr)
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
		static ModelPtr Ellipse(Renderer& rdr, float dimx, float dimy, bool solid, int facets = 40, Colour32 colour = Colour32White, m4x4 const* o2w = nullptr, NuggetProps const* mat = nullptr)
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
			CreateOptions opts;
			if (o2w != nullptr) opts.m_bake = *o2w;
			return Create(rdr, cache, opts);
		}
		static ModelPtr Pie(Renderer& rdr, float dimx, float dimy, float ang0, float ang1, float radius0, float radius1, bool solid, int facets = 40, Colour32 colour = Colour32White, m4x4 const* o2w = nullptr, NuggetProps const* mat = nullptr)
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
			CreateOptions opts;
			if (o2w != nullptr) opts.m_bake = *o2w;
			return Create(rdr, cache, opts);
		}
		static ModelPtr RoundedRectangle(Renderer& rdr, float dimx, float dimy, float corner_radius, bool solid, int facets = 10, Colour32 colour = Colour32White, m4x4 const* o2w = nullptr, NuggetProps const* mat = nullptr)
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
			CreateOptions opts;
			if (o2w != nullptr) opts.m_bake = *o2w;
			return Create(rdr, cache, opts);
		}
		static ModelPtr Polygon(Renderer& rdr, int num_points, v2 const* points, bool solid, int num_colours = 0, Colour32 const* colours = nullptr, m4x4 const* o2w = nullptr, NuggetProps const* mat = nullptr)
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
			CreateOptions opts;
			if (o2w != nullptr) opts.m_bake = *o2w;
			return Create(rdr, cache, opts);
		}

		// Boxes ******************************************************************************
		static ModelPtr Boxes(Renderer& rdr, int num_boxes, v4 const* points, int num_colours = 0, Colour32 const* colours = nullptr, NuggetProps const* mat = nullptr)
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
		static ModelPtr Boxes(Renderer& rdr, int num_boxes, v4 const* points, m4x4 const& o2w, int num_colours = 0, Colour32 const* colours = nullptr, NuggetProps const* mat = nullptr)
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
		static ModelPtr Box(Renderer& rdr, v4 const& rad, m4x4 const& o2w = m4x4Identity, Colour32 colour = Colour32White, NuggetProps const* mat = nullptr)
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
		static ModelPtr Box(Renderer& rdr, float rad, m4x4 const& o2w = m4x4Identity, Colour32 colour = Colour32White, NuggetProps const* mat = nullptr)
		{
			return Box(rdr, v4(rad), o2w, colour, mat);
		}
		static ModelPtr BoxList(Renderer& rdr, int num_boxes, v4 const* positions, v4 const& rad, int num_colours = 0, Colour32 const* colours = nullptr, NuggetProps const* mat = nullptr)
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
		static ModelPtr Geosphere(Renderer& rdr, v4 const& radius, int divisions = 3, Colour32 colour = Colour32White, NuggetProps const* mat = nullptr)
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
		static ModelPtr Geosphere(Renderer& rdr, float radius, int divisions = 3, Colour32 colour = Colour32White, NuggetProps const* mat = nullptr)
		{
			return Geosphere(rdr, v4(radius, radius, radius, 0.0f), divisions, colour, mat);
		}
		static ModelPtr Sphere(Renderer& rdr, v4 const& radius, int wedges = 20, int layers = 5, Colour32 colour = Colour32White, NuggetProps const* mat = nullptr)
		{
			// Calculate the required buffer sizes
			auto [vcount, icount] = geometry::SphereSize(wedges, layers);

			// Generate the geometry
			Cache cache(vcount, icount, 0, 2);
			auto vptr = cache.m_vcont.data();
			auto iptr = cache.m_icont.data<uint16_t>();
			auto props = geometry::Sphere(radius, wedges, layers, colour,
				[&](v4_cref<> p, Colour32 c, v4_cref<> n, v2_cref<> t) { SetPCNT(*vptr++, p, Colour(c), n, t); },
				[&](int idx) { *iptr++ = s_cast<uint16_t>(idx); });

			// Create a nugget
			cache.AddNugget(ETopo::TriList, props.m_geom, props.m_has_alpha, false, mat);
			cache.m_bbox = props.m_bbox;

			// Create the model
			return Create(rdr, cache);
		}
		static ModelPtr Sphere(Renderer& rdr, float radius, int wedges = 20, int layers = 5, Colour32 colour = Colour32White, NuggetProps const* mat = nullptr)
		{
			return Sphere(rdr, v4{ radius, radius, radius, 0 }, wedges, layers, colour, mat);
		}

		// Cylinder ***************************************************************************
		static ModelPtr Cylinder(Renderer& rdr, float radius0, float radius1, float height, float xscale = 1.0f, float yscale = 1.0f, int wedges = 20, int layers = 1, int num_colours = 0, Colour32 const* colours = nullptr, m4x4 const* o2w = nullptr, NuggetProps const* mat = nullptr)
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
			CreateOptions opts;
			if (o2w != nullptr) opts.m_bake = *o2w;
			return Create(rdr, cache, opts);
		}

		// Capsule ****************************************************************************
		//void        CapsuleSize(Range& vrange, Range& irange, int divisions);
		//Settings    CapsuleModelSettings(int divisions);
		//ModelPtr    CapsuleHRxy(MLock& mlock  ,MaterialManager& matmgr ,float height ,float xradius ,float yradius ,m4x4 const& o2w = m4x4Identity ,int divisions = 3 ,Colour32 colour = Colour32White ,rdr::Material const* mat = 0 ,Range* vrange = 0 ,Range* irange = 0);
		//ModelPtr    CapsuleHRxy(Renderer& rdr                          ,float height ,float xradius ,float yradius ,m4x4 const& o2w = m4x4Identity ,int divisions = 3 ,Colour32 colour = Colour32White ,rdr::Material const* mat = 0 ,Range* vrange = 0 ,Range* irange = 0);

		// Extrude ****************************************************************************
		static ModelPtr Extrude(Renderer& rdr, int cs_count, v2 const* cs, int path_count, v4 const* path, bool closed, bool smooth_cs, int num_colours = 0, Colour32 const* colours = nullptr, m4x4 const* o2w = nullptr, NuggetProps const* mat = nullptr)
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
			CreateOptions opts;
			if (o2w != nullptr) opts.m_bake = *o2w;
			return Create(rdr, cache, opts);
		}
		static ModelPtr Extrude(Renderer& rdr, int cs_count, v2 const* cs, int path_count, m4x4 const* path, bool closed, bool smooth_cs, int num_colours = 0, Colour32 const* colours = nullptr, m4x4 const* o2w = nullptr, NuggetProps const* mat = nullptr)
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
				[&](int idx) { *iptr++ = s_cast<uint16_t>(idx); });

			// Create a nugget
			cache.AddNugget(ETopo::TriList, props.m_geom, props.m_has_alpha, false, mat);
			cache.m_bbox = props.m_bbox;

			// Create the model
			return Create(rdr, cache, o2w);
		}

		// Mesh *******************************************************************************
		static ModelPtr Mesh(Renderer& rdr, MeshCreationData const& cdata)
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

		// Skybox *****************************************************************************

		// Create a model for a geosphere sky box
		static ModelPtr SkyboxGeosphere(Renderer& rdr, Texture2DPtr sky_texture, float radius = 1.0f, int divisions = 3, Colour32 colour = Colour32White)
		{
			// Calculate the required buffer sizes
			int vcount, icount;
			geometry::SkyboxGeosphereSize(divisions, vcount, icount);

			// Generate the geometry
			Cache cache{vcount, icount, 0, sizeof(uint16_t)};
			auto props = geometry::SkyboxGeosphere(radius, divisions, colour, cache.m_vcont.data(), cache.m_icont.data<uint16_t>());
			cache.m_bbox = props.m_bbox;

			// Model nugget properties for the sky box
			NuggetProps mat;
			mat.m_tex_diffuse = sky_texture;
			mat.m_rsb.Set(ERS::CullMode, D3D11_CULL_MODE::D3D11_CULL_FRONT);
			cache.AddNugget(ETopo::TriList, props.m_geom, props.m_has_alpha, false, &mat);

			// Create the model
			return Create(rdr, cache);
		}
		static ModelPtr SkyboxGeosphere(Renderer& rdr, wchar_t const* texture_path, float radius = 1.0f, int divisions = 3, Colour32 colour = Colour32White)
		{
			// One texture per nugget
			auto tex = rdr.m_tex_mgr.CreateTexture2D(AutoId, SamplerDesc::LinearClamp(), texture_path, false, "skybox");
			return SkyboxGeosphere(rdr, tex, radius, colour);
		}
		static ModelPtr SkyboxFiveSidedCube(Renderer& rdr, Texture2DPtr sky_texture, float radius = 1.0f, Colour32 colour = Colour32White)
		{
			// Calculate the required buffer sizes
			int vcount, icount;
			geometry::SkyboxFiveSidedCubicDomeSize(vcount, icount);

			// Generate the geometry
			Cache cache(vcount, icount, 0, 2);
			auto props = geometry::SkyboxFiveSidedCubicDome(radius, colour, cache.m_vcont.data(), cache.m_icont.data<uint16_t>());
			cache.m_bbox = props.m_bbox;

			// Model nugget properties for the sky box
			NuggetProps mat;
			mat.m_tex_diffuse = sky_texture;
			mat.m_rsb.Set(ERS::CullMode, D3D11_CULL_MODE::D3D11_CULL_FRONT);
			cache.AddNugget(ETopo::TriList, props.m_geom, props.m_has_alpha, false, &mat);

			// Create the model
			return Create(rdr, cache);
		}
		static ModelPtr SkyboxFiveSidedCube(Renderer& rdr, wchar_t const* texture_path, float radius = 1.0f, Colour32 colour = Colour32White)
		{
			// One texture per nugget
			auto tex = rdr.m_tex_mgr.CreateTexture2D(AutoId, SamplerDesc::LinearClamp(), texture_path, false, "skybox");
			return SkyboxFiveSidedCube(rdr, tex, radius, colour);
		}
		static ModelPtr SkyboxSixSidedCube(Renderer& rdr, Texture2DPtr (&sky_texture)[6], float radius = 1.0f, Colour32 colour = Colour32White)
		{
			// Calculate the required buffer sizes
			int vcount, icount;
			geometry::SkyboxSixSidedCubeSize(vcount, icount);

			// Generate the geometry
			Cache cache{vcount, icount, 0, sizeof(uint16_t)};
			auto props = geometry::SkyboxSixSidedCube(radius, colour, cache.m_vcont.data(), cache.m_icont.data<uint16_t>());
			cache.m_bbox = props.m_bbox;

			// Create the nuggets, one per face. Expected order: +X, -X, +Y, -Y, +Z, -Z
			for (int i = 0; i != 6; ++i)
			{
				// Create the render nugget for this face of the sky box
				NuggetProps mat = {};
				mat.m_tex_diffuse = sky_texture[i];
				mat.m_rsb.Set(ERS::CullMode, D3D11_CULL_MODE::D3D11_CULL_FRONT);
				mat.m_vrange = rdr::Range(i * 4, (i + 1) * 4);
				mat.m_irange = rdr::Range(i * 6, (i + 1) * 6);
				cache.AddNugget(ETopo::TriList, props.m_geom, props.m_has_alpha, &mat);
			}

			// Create the model
			return Create(rdr, cache);
		}
		static ModelPtr SkyboxSixSidedCube(Renderer& rdr, wchar_t const* texture_path_pattern, float radius = 1.0f, Colour32 colour = Colour32White)
		{
			wstring256 tpath = texture_path_pattern;
			auto ofs = tpath.find(L"??");
			PR_ASSERT(PR_DBG, ofs != string::npos, "Provided path does not include '??' characters");

			Texture2DPtr tex[6] = {}; int i = 0;
			for (auto face : { L"+X", L"-X", L"+Y", L"-Y", L"+Z", L"-Z" })
			{
				// Load the texture for this face of the sky box
				tpath[ofs + 0] = face[0];
				tpath[ofs + 1] = face[1];
				tex[i++] = rdr.m_tex_mgr.CreateTexture2D(AutoId, SamplerDesc::LinearClamp(), tpath.c_str(), false, "skybox");
			}

			return SkyboxSixSidedCube(rdr, tex, radius, colour);
		}

		// ModelFile **************************************************************************

		// Load a P3D model from a stream, emitting models for each mesh via 'out'.
		// bool out(span<ModelTreeNode> tree) - return true to stop loading, false to get the next model
		template <typename TOut> static void LoadP3DModel(Renderer& rdr, std::istream& src, TOut out, CreateOptions const& opts = CreateOptions{})
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
							if (tex.m_type != p3d::Texture::EType::Diffuse) continue;
							auto tex_id = MakeId(tex.m_filepath.c_str());
							auto sam_desc = SamplerDesc{s_cast<D3D11_TEXTURE_ADDRESS_MODE>(tex.m_addr_mode), D3D11_FILTER_ANISOTROPIC};
							auto has_alpha = AllSet(tex.m_flags, p3d::Texture::EFlags::Alpha);
							m_tex_diffuse = m_rdr.m_tex_mgr.CreateTexture2D(tex_id, tex.m_filepath.c_str(), sam_desc, has_alpha, tex.m_filepath.c_str());
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
				TOut m_out;
				Cache<> m_cache;
				MatCont m_mats;

				explicit ModelOut(Renderer& rdr, CreateOptions const& opts, TOut out)
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
						NuggetProps nugget(
							static_cast<ETopo>(nug.m_topo),
							static_cast<EGeom>(nug.m_geom),
							nullptr,
							vrange,
							irange);

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
		template <typename TOut> static void Load3DSModel(Renderer& rdr, std::istream& src, TOut out, CreateOptions const& opts = CreateOptions{})
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
						SamplerDesc sam_desc{
							tex.m_tiling == 0 ? D3D11_TEXTURE_ADDRESS_CLAMP :
							tex.m_tiling == 1 ? D3D11_TEXTURE_ADDRESS_WRAP :
							D3D11_TEXTURE_ADDRESS_BORDER,
							D3D11_FILTER_ANISOTROPIC};
						m_tex_diffuse = rdr.m_tex_mgr.CreateTexture2D(rdr::AutoId, tex.m_filepath.c_str(), sam_desc, false, tex.m_filepath.c_str());
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
						NuggetProps nugget(topo, geom, nullptr, vrange, irange);
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
		template <typename TOut> static void LoadSTLModel(Renderer& rdr, std::istream& src, TOut out, CreateOptions const& opts = CreateOptions{})
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
		template <typename MOut> static void LoadModel(geometry::EModelFileFormat format, Renderer& rdr, std::istream& src, MOut mout, CreateOptions const& opts = CreateOptions{})
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

		// A Direct2D font description
		struct Font
		{
			wstring32           m_name;   // Font family name
			float               m_size;   // in points (1 pt = 1/72.272 inches = 0.35145mm)
			Colour32            m_colour; // Fore colour for the text
			DWRITE_FONT_WEIGHT  m_weight; // boldness
			DWRITE_FONT_STRETCH m_stretch;
			DWRITE_FONT_STYLE   m_style;
			bool                m_underline;
			bool                m_strikeout;

			Font()
				:m_name(L"tahoma")
				,m_size(12.0f)
				,m_colour(0xFF000000)
				,m_weight(DWRITE_FONT_WEIGHT_NORMAL)
				,m_stretch(DWRITE_FONT_STRETCH_NORMAL)
				,m_style(DWRITE_FONT_STYLE_NORMAL)
				,m_underline(false)
				,m_strikeout(false)
			{}
			bool operator == (Font const& rhs) const
			{
				return
					m_name      == rhs.m_name      &&
					m_size      == rhs.m_size      &&
					m_colour    == rhs.m_colour    &&
					m_weight    == rhs.m_weight    &&
					m_stretch   == rhs.m_stretch   &&
					m_style     == rhs.m_style     &&
					m_underline == rhs.m_underline &&
					m_strikeout == rhs.m_strikeout;
			}
			bool operator != (Font const& rhs) const
			{
				return !(*this == rhs);
			}
		};

		// Text formatting description
		struct TextFormat
		{
			// The range of characters that the format applies to
			DWRITE_TEXT_RANGE m_range;

			// Font/Style for the text range
			Font m_font;

			TextFormat()
				:m_range()
				,m_font()
			{}
			TextFormat(int beg, int count, Font const& font)
				:m_range({UINT32(beg), UINT32(count)})
				,m_font(font)
			{}
			bool empty() const
			{
				return m_range.length == 0;
			}
		};

		// Layout options for a collection of text fragments
		struct TextLayout
		{
			struct Padding { float left, top, right, bottom; };

			v2 m_dim; // The box to constrain the text to
			v2 m_anchor; // The anchor point for the text
			Padding m_padding; // Padding around the text
			Colour32 m_bk_colour; // Background colour for the text
			DWRITE_TEXT_ALIGNMENT m_align_h;
			DWRITE_PARAGRAPH_ALIGNMENT m_align_v;
			DWRITE_WORD_WRAPPING m_word_wrapping;

			TextLayout()
				:m_dim(maths::float_inf, maths::float_inf)
				,m_anchor(0,0)
				,m_padding()
				,m_bk_colour(0x00000000)
				,m_align_h(DWRITE_TEXT_ALIGNMENT_LEADING)
				,m_align_v(DWRITE_PARAGRAPH_ALIGNMENT_NEAR)
				,m_word_wrapping(DWRITE_WORD_WRAPPING_WRAP)
			{}
		};

		// Create a quad containing text.
		// 'text' is the complete text to render into the quad.
		// 'formatting' defines regions in the text to apply formatting to.
		// 'layout' is global text layout information.
		// 'scale' controls the size of the output quad. Scale of 1 => 100pt = 1m
		// 'axis_id' is the forward direction of the quad
		// 'dim_out' is 'xy' = size of text in pixels, 'zw' = size of quad in pixels
		template <typename = void>
		static ModelPtr Text(Renderer& rdr, std::wstring_view text, std::span<TextFormat const> formatting, TextLayout const& layout, float scale, AxisId axis_id, v4& dim_out, m4x4 const* bake = nullptr)
		{
			// Texture sizes are in physical pixels, but D2D operates in DIP so we need to determine
			// the size in physical pixels on this device that correspond to the returned metrics.
			// From: https://msdn.microsoft.com/en-us/library/windows/desktop/ff684173%28v=vs.85%29.aspx?f=255&MSPPError=-2147217396
			// "Direct2D automatically performs scaling to match the DPI setting.
			//  In Direct2D, coordinates are measured in units called device-independent pixels (DIPs).
			//  A DIP is defined as 1/96th of a logical inch. In Direct2D, all drawing operations are
			//  specified in DIPs and then scaled to the current DPI setting."
			Renderer::Lock lock(rdr);
			auto dwrite = lock.DWrite();

			// Get the default format
			auto def = !formatting.empty() && !formatting[0].empty() ? formatting[0] : TextFormat();

			// Determine if the model requires alpha blending.
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
			for (auto& fmt : formatting)
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

			// The size of the text in device independent pixels, including padding.
			auto dip_size = v2(
				metrics.widthIncludingTrailingWhitespace + layout.m_padding.left + layout.m_padding.right,
				metrics.height + layout.m_padding.top + layout.m_padding.bottom);

			// DIP is defined as 1/96th of a logical inch (= 0.2645833 mm/px)
			// Font size 12pt is 16px high = 4.233mm (1pt = 1/72th of an inch)
			// Can choose the quat size arbitrarily so defaulting to 1pt = 1cm. 'scale' can be used to adjust this.
			constexpr float pt_to_px = 96.0f / 72.0f;  // This is used to find the required texture size.
			const float pt_to_m = 0.00828491f * scale; // This is used to create the quad as a multiple of the text size.

			// Determine the required texture size. This is controlled by the font size only.
			// DWrite draws in absolute pixels so there is no point in trying to scale the texture.
			auto text_size = dip_size;
			auto texture_size = Ceil(text_size * pt_to_px);

			// Create a texture large enough to contain the text, and render the text into it
			SamplerDesc sdesc(D3D11_TEXTURE_ADDRESS_CLAMP, D3D11_FILTER_MIN_MAG_MIP_LINEAR);
			Texture2DDesc tdesc(size_t(texture_size.x), size_t(texture_size.y), 1, DXGI_FORMAT_R8G8B8A8_UNORM);
			tdesc.BindFlags = D3D11_BIND_RENDER_TARGET | D3D11_BIND_SHADER_RESOURCE;
			auto tex = rdr.m_tex_mgr.CreateTexture2D(AutoId, Image(), tdesc, sdesc, has_alpha, "text_quad");

			// Get a D2D device context to draw on the texture
			auto dc = tex->GetD2DeviceContext();
			auto fr = To<D3DCOLORVALUE>(def.m_font.m_colour);
			auto bk = To<D3DCOLORVALUE>(layout.m_bk_colour);

			// Apply different colours to text ranges
			for (auto& fmt : formatting)
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

			// Create a quad using this texture
			auto [vcount, icount] = geometry::QuadSize(1);

			// Return the text metrics size and the texture size
			dim_out = v4(text_size, texture_size);

			// Set the texture coordinates to match the text metrics and the quad size
			auto t2q = m4x4::Scale(text_size.x / texture_size.x, text_size.y / texture_size.y, 1.0f, v4Origin) * m4x4(v4XAxis, -v4YAxis, v4ZAxis, v4(0, 1, 0, 1));

			// Generate the geometry
			Cache cache{vcount, icount, 0, sizeof(uint16_t)};
			auto vptr = cache.m_vcont.data();
			auto iptr = cache.m_icont.data<uint16_t>();
			auto props = geometry::Quad(axis_id, layout.m_anchor, text_size.x * pt_to_m, text_size.y * pt_to_m, iv2Zero, Colour32White, t2q,
				[&](v4_cref<> p, Colour32 c, v4_cref<> n, v2_cref<> t) { SetPCNT(*vptr++, p, Colour(c), n, t); },
				[&](int idx) { *iptr++ = s_cast<uint16_t>(idx); });

			// Create a nugget
			NuggetProps mat(ETopo::TriList);
			mat.m_tex_diffuse = tex;
			cache.AddNugget(ETopo::TriList, props.m_geom & ~EGeom::Norm, props.m_has_alpha, false, &mat);
			cache.m_bbox = props.m_bbox;

			// Create the model
			CreateOptions opts;
			if (bake != nullptr) opts.m_bake = *bake;
			return Create(rdr, cache, opts);
		}
		static ModelPtr Text(Renderer& rdr, std::wstring_view text, std::span<TextFormat const> formatting, TextLayout const& layout, float scale, AxisId axis_id)
		{
			v4 dim_out;
			return Text(rdr, text, formatting, layout, scale, axis_id, dim_out);
		}
		static ModelPtr Text(Renderer& rdr, std::wstring_view text, TextFormat const& formatting, TextLayout const& layout, float scale, AxisId axis_id, v4& dim_out)
		{
			return Text(rdr, text, std::span(&formatting, 1), layout, scale, axis_id, dim_out);
		}
		static ModelPtr Text(Renderer& rdr, std::wstring_view text, TextFormat const& formatting, TextLayout const& layout, float scale, AxisId axis_id)
		{
			v4 dim_out;
			return Text(rdr, text, std::span(&formatting, 1), layout, scale, axis_id, dim_out);
		}
	};
}
