//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/model/nugget.h"
#include "pr/view3d-12/model/model.h"
#include "pr/view3d-12/model/model_desc.h"
#include "pr/view3d-12/main/renderer.h"

namespace pr::rdr12
{
	// Parameters structure for creating mesh models
	struct MeshCreationData
	{
		int m_vcount;                // The length of the 'verts' array
		int m_icount;                // The length of the 'indices' array
		int m_gcount;                // The length of the 'nuggets' array
		int m_ccount;                // The length of the 'colours' array. 0, 1, or 'vcount'
		int m_ncount;                // The length of the 'normals' array. 0, 1, or 'vcount'
		v4 const* m_verts;           // The vertex data for the model
		uint16_t const* m_indices;   // The index data for the model
		NuggetData const* m_nuggets; // The nugget data for the model
		Colour32 const* m_colours;   // The colour data for the model. Typically nullptr, 1, or 'vcount' colours
		v4 const* m_normals;         // The normal data for the model. Typically nullptr or a pointer to 'vcount' normals
		v2 const* m_tex_coords;      // The texture coordinates data for the model. nullptr or a pointer to 'vcount' texture coords

		MeshCreationData()
			:m_vcount()
			,m_icount()
			,m_gcount()
			,m_ccount()
			,m_ncount()
			,m_verts()
			,m_indices()
			,m_nuggets()
			,m_colours()
			,m_normals()
			,m_tex_coords()
		{}
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
		MeshCreationData& nuggets(NuggetData const* gbuf, int count)
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
		MeshCreationData& nuggets(std::initializer_list<NuggetData> gbuf)
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
		template <int N> MeshCreationData& nuggets(NuggetData const (&nbuf)[N])
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
	struct ModelGenerator
	{
		// Additional options for model creation
		struct CreateOptions
		{
			// Transform the model verts by the given transform.
			m4x4 const* m_bake;

			// Algorithmically generate surface normals. Value is the smoothing angle.
			float const* m_gen_normals;
		};

		// Points/Sprites *********************************************************************
		// Generate a cloud of points from an array of points
		static ModelPtr Points(Renderer& rdr, int num_points, v4 const* points, int num_colours = 0, Colour32 const* colours = nullptr, NuggetData const* mat = nullptr);

		// Lines ******************************************************************************
		// Generate lines from an array of start point, end point pairs.
		// 'num_lines' is the number of start/end point pairs in the following arrays
		// 'points' is the input array of start and end points for lines.
		// 'num_colours' should be either, 0, 1, or num_lines * 2
		// 'colours' is an input array of colour values or a pointer to a single colour.
		// 'mat' is an optional material to use for the lines
		static ModelPtr Lines(Renderer& rdr, int num_lines, v4 const* points, int num_colours = 0, Colour32 const* colours = nullptr, NuggetData const* mat = nullptr);
		static ModelPtr LinesD(Renderer& rdr, int num_lines, v4 const* points, v4 const* directions, int num_colours = 0, Colour32 const* colours = nullptr, NuggetData const* mat = nullptr);
		static ModelPtr LineStrip(Renderer& rdr, int num_lines, v4 const* points, int num_colours = 0, Colour32 const* colour = nullptr, NuggetData const* mat = nullptr);

		// Quad *******************************************************************************
		static ModelPtr Quad(Renderer& rdr, NuggetData const* mat = nullptr);
		static ModelPtr Quad(Renderer& rdr, int num_quads, v4 const* verts, int num_colours = 0, Colour32 const* colours = nullptr, m4x4 const& t2q = m4x4Identity, NuggetData const* mat = nullptr);
		static ModelPtr Quad(Renderer& rdr, v2 const& anchor, v4 const& quad_w, v4 const& quad_h, iv2 const& divisions = iv2Zero, Colour32 colour = Colour32White, m4x4 const& t2q = m4x4Identity, NuggetData const* mat = nullptr);
		static ModelPtr Quad(Renderer& rdr, AxisId axis_id, v2 const& anchor, float width, float height, iv2 const& divisions = iv2Zero, Colour32 colour = Colour32White, m4x4 const& t2q = m4x4Identity, NuggetData const* mat = nullptr);
		static ModelPtr QuadStrip(Renderer& rdr, int num_quads, v4 const* verts, float width, int num_normals = 0, v4 const* normals = nullptr, int num_colours = 0, Colour32 const* colours = nullptr, NuggetData const* mat = nullptr);
		static ModelPtr QuadPatch(Renderer& rdr, int dimx, int dimy, NuggetData const* mat = nullptr);

		// Shape2d ****************************************************************************
		static ModelPtr Ellipse(Renderer& rdr, float dimx, float dimy, bool solid, int facets = 40, Colour32 colour = Colour32White, m4x4 const* o2w = nullptr, NuggetData const* mat = nullptr);		
		static ModelPtr Pie(Renderer& rdr, float dimx, float dimy, float ang0, float ang1, float radius0, float radius1, bool solid, int facets = 40, Colour32 colour = Colour32White, m4x4 const* o2w = nullptr, NuggetData const* mat = nullptr);
		static ModelPtr RoundedRectangle(Renderer& rdr, float dimx, float dimy, float corner_radius, bool solid, int facets = 10, Colour32 colour = Colour32White, m4x4 const* o2w = nullptr, NuggetData const* mat = nullptr);
		static ModelPtr Polygon(Renderer& rdr, int num_points, v2 const* points, bool solid, int num_colours = 0, Colour32 const* colours = nullptr, m4x4 const* o2w = nullptr, NuggetData const* mat = nullptr);

		// Boxes ******************************************************************************
		static ModelPtr Boxes(Renderer& rdr, int num_boxes, v4 const* points, int num_colours = 0, Colour32 const* colours = nullptr, NuggetData const* mat = nullptr);
		static ModelPtr Boxes(Renderer& rdr, int num_boxes, v4 const* points, m4x4 const& o2w, int num_colours = 0, Colour32 const* colours = nullptr, NuggetData const* mat = nullptr);
		static ModelPtr Box(Renderer& rdr, v4 const& rad, m4x4 const& o2w = m4x4Identity, Colour32 colour = Colour32White, NuggetData const* mat = nullptr);
		static ModelPtr Box(Renderer& rdr, float rad, m4x4 const& o2w = m4x4Identity, Colour32 colour = Colour32White, NuggetData const* mat = nullptr);
		static ModelPtr BoxList(Renderer& rdr, int num_boxes, v4 const* positions, v4 const& rad, int num_colours = 0, Colour32 const* colours = nullptr, NuggetData const* mat = nullptr);

		// Sphere *****************************************************************************
		static ModelPtr Geosphere(Renderer& rdr, v4 const& radius, int divisions = 3, Colour32 colour = Colour32White, NuggetData const* mat = nullptr);
		static ModelPtr Geosphere(Renderer& rdr, float radius, int divisions = 3, Colour32 colour = Colour32White, NuggetData const* mat = nullptr);
		static ModelPtr Sphere(Renderer& rdr, v4 const& radius, int wedges = 20, int layers = 5, Colour32 colour = Colour32White, NuggetData const* mat = nullptr);
		static ModelPtr Sphere(Renderer& rdr, float radius, int wedges = 20, int layers = 5, Colour32 colour = Colour32White, NuggetData const* mat = nullptr);

		// Cylinder ***************************************************************************
		static ModelPtr Cylinder(Renderer& rdr, float radius0, float radius1, float height, float xscale = 1.0f, float yscale = 1.0f, int wedges = 20, int layers = 1, int num_colours = 0, Colour32 const* colours = nullptr, m4x4 const* o2w = nullptr, NuggetData const* mat = nullptr);

		// Extrude ****************************************************************************
		static ModelPtr Extrude(Renderer& rdr, int cs_count, v2 const* cs, int path_count, v4 const* path, bool closed, bool smooth_cs, int num_colours = 0, Colour32 const* colours = nullptr, m4x4 const* o2w = nullptr, NuggetData const* mat = nullptr);
		static ModelPtr Extrude(Renderer& rdr, int cs_count, v2 const* cs, int path_count, m4x4 const* path, bool closed, bool smooth_cs, int num_colours = 0, Colour32 const* colours = nullptr, m4x4 const* o2w = nullptr, NuggetData const* mat = nullptr);

		// Mesh *******************************************************************************
		static ModelPtr Mesh(Renderer& rdr, MeshCreationData const& cdata);

		// SkyBox *****************************************************************************
		static ModelPtr SkyboxGeosphere(Renderer& rdr, Texture2DPtr sky_texture, float radius = 1.0f, int divisions = 3, Colour32 colour = Colour32White);
		static ModelPtr SkyboxGeosphere(Renderer& rdr, wchar_t const* texture_path, float radius = 1.0f, int divisions = 3, Colour32 colour = Colour32White);
		static ModelPtr SkyboxFiveSidedCube(Renderer& rdr, Texture2DPtr sky_texture, float radius = 1.0f, Colour32 colour = Colour32White);
		static ModelPtr SkyboxFiveSidedCube(Renderer& rdr, wchar_t const* texture_path, float radius = 1.0f, Colour32 colour = Colour32White);
		static ModelPtr SkyboxSixSidedCube(Renderer& rdr, Texture2DPtr (&sky_texture)[6], float radius = 1.0f, Colour32 colour = Colour32White);
		static ModelPtr SkyboxSixSidedCube(Renderer& rdr, wchar_t const* texture_path_pattern, float radius = 1.0f, Colour32 colour = Colour32White);

		// ModelFile **************************************************************************
		// Load a P3D model from a stream, emitting models for each mesh via 'out'.
		// bool out(span<ModelTreeNode> tree) - return true to stop loading, false to get the next model
		using ModelOutFunc = std::function<bool(std::span<ModelTreeNode>)>;
		static void LoadP3DModel(Renderer& rdr, std::istream& src, ModelOutFunc out, CreateOptions const& opts = CreateOptions{});		
		static void Load3DSModel(Renderer& rdr, std::istream& src, ModelOutFunc out, CreateOptions const& opts = CreateOptions{});
		static void LoadSTLModel(Renderer& rdr, std::istream& src, ModelOutFunc out, CreateOptions const& opts = CreateOptions{});
		static void LoadModel(geometry::EModelFileFormat format, Renderer& rdr, std::istream& src, ModelOutFunc mout, CreateOptions const& opts = CreateOptions{});

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

			v2                         m_dim;
			v2                         m_anchor;
			Padding                    m_padding;
			Colour32                   m_bk_colour;
			DWRITE_TEXT_ALIGNMENT      m_align_h;
			DWRITE_PARAGRAPH_ALIGNMENT m_align_v;
			DWRITE_WORD_WRAPPING       m_word_wrapping;

			TextLayout()
				:m_dim(512,128)
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
		// 'formatting_count' is the length of the 'formatting' array.
		// 'layout' is global text layout information.
		static ModelPtr Text(Renderer& rdr, std::wstring_view text, std::span<TextFormat const> formatting, TextLayout const& layout, float scale, AxisId axis_id, v4& dim_out, m4x4 const* bake = nullptr);
		static ModelPtr Text(Renderer& rdr, std::wstring_view text, std::span<TextFormat const> formatting, TextLayout const& layout, float scale, AxisId axis_id);
		static ModelPtr Text(Renderer& rdr, std::wstring_view text, TextFormat const& formatting, TextLayout const& layout, float scale, AxisId axis_id, v4& dim_out);
		static ModelPtr Text(Renderer& rdr, std::wstring_view text, TextFormat const& formatting, TextLayout const& layout, float scale, AxisId axis_id);

		// Cache ****************************************************************************************

			// Memory pooling for model buffers
		template <typename VertexType = Vert>
		struct Cache
		{
			using VType = VertexType;
			using VCont = vector<VType>;
			using ICont = pr::geometry::IdxBuf;
			using NCont = vector<NuggetData>;

		private:

			// The cached buffers
			struct alignas(16) Buffers
			{
				string32 m_name;  // Model name
				VCont    m_vcont; // Model verts
				ICont    m_icont; // Model faces/lines/points/etc
				NCont    m_ncont; // Model nuggets
				BBox     m_bbox;  // Model bounding box
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

			string32& m_name; // Model name
			VCont& m_vcont;   // Model verts
			ICont& m_icont;   // Model faces/lines/points/etc
			NCont& m_ncont;   // Model nuggets
			BBox& m_bbox;     // Model bounding box

			Cache() = delete;
			Cache(int vcount, int icount, int ncount, int idx_stride)
				:m_buffers(this_thread_instance())
				, m_in_use(this_thread_cache_in_use())
				, m_name(m_buffers.m_name)
				, m_vcont(m_buffers.m_vcont)
				, m_icont(m_buffers.m_icont)
				, m_ncont(m_buffers.m_ncont)
				, m_bbox(m_buffers.m_bbox)
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
			void AddNugget(ETopo topo, EGeom geom, bool geometry_has_alpha, bool tint_has_alpha, NuggetData const* mat = nullptr)
			{
				// Notes:
				// - Don't change the 'geom' flags here based on whether the material has a texture or not.
				//   The texture may be set in the material after here and before the model is rendered.
				auto nug = mat ? *mat : NuggetData{};
				nug.m_topo = topo;
				nug.m_geom = geom;
				nug.m_nflags = SetBits(nug.m_nflags, ENuggetFlag::GeometryHasAlpha, geometry_has_alpha);
				nug.m_nflags = SetBits(nug.m_nflags, ENuggetFlag::TintHasAlpha, tint_has_alpha);
				m_ncont.push_back(nug);
			}
		};
	};
}
