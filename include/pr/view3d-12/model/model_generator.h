﻿//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/model/nugget.h"
#include "pr/view3d-12/model/model.h"
#include "pr/view3d-12/model/model_tree.h"
#include "pr/view3d-12/model/model_desc.h"
#include "pr/view3d-12/main/renderer.h"

namespace pr::rdr12
{
	// Parameters structure for creating mesh models
	struct MeshCreationData
	{
		std::span<v4 const> m_verts;           // The vertex data for the model
		geometry::index_cspan m_idxbuf;       // The index data for the model
		std::span<NuggetDesc const> m_nuggets; // The nugget data for the model
		std::span<Colour32 const> m_colours;   // The colour data for the model. Typically 0, 1, or 'vcount' colours. Not a requirement though because of interpolation.
		std::span<v4 const> m_normals;         // The normal data for the model. Typically 0, 1, or 'vcount' normals. Not a requirement though because of interpolation.
		std::span<v2 const> m_tex_coords;      // The texture coordinates data for the model. 0, or 'vcount' texture coords
		int m_idx_stride;

		MeshCreationData()
			: m_verts()
			, m_idxbuf()
			, m_nuggets()
			, m_colours()
			, m_normals()
			, m_tex_coords()
			, m_idx_stride()
		{}
		MeshCreationData& verts(std::span<v4 const> vbuf)
		{
			assert(maths::is_aligned(vbuf.data()));
			m_verts = vbuf;
			return *this;
		}
		MeshCreationData& indices(geometry::index_cspan ibuf)
		{
			m_idxbuf = ibuf;
			return *this;
		}
		MeshCreationData& nuggets(std::span<NuggetDesc const> gbuf)
		{
			m_nuggets = gbuf;
			return *this;
		}
		MeshCreationData& colours(std::span<Colour32 const> cbuf)
		{
			// Count doesn't have to be 0, 1, or 'vcount' because interpolation is used
			m_colours = cbuf;
			return *this;
		}
		MeshCreationData& normals(std::span<v4 const> nbuf)
		{
			// Count doesn't have to be 0, 1, or 'vcount' because interpolation is used
			assert(maths::is_aligned(nbuf.data()));
			m_normals = nbuf;
			return *this;
		}
		MeshCreationData& tex(std::span<v2 const> tbuf)
		{
			// Count doesn't have to be 0, 1, or 'vcount' because interpolation is used
			m_tex_coords = tbuf;
			return *this;
		}
	};

	// Create model geometry
	struct ModelGenerator
	{
		// Additional options for model creation
		struct CreateOptions
		{
			enum class EOptions
			{
				None = 0,
				BakeTransform = 1 << 0, // Bake the model transform into the vertices
				Colours = 1 << 1,
				DiffuseTexture = 1 << 2,
				NormalGeneration = 1 << 3,
				TextureToSurface = 1 << 4,
				_flags_enum = 0,
			};

			// Transform the model verts by the given transform.
			m4x4 m_bake = {};

			// Per-vertex or per-object colour
			std::span<Colour32 const> m_colours = {};

			// Diffuse texture
			Texture2DPtr m_tex_diffuse = {};

			// Diffuse texture sampler
			SamplerPtr m_sam_diffuse = {};

			// Texture to surface transform
			m4x4 m_t2s = {};

			// Algorithmically generate surface normals. Value is the smoothing angle.
			float m_gen_normals = {};

			// Flags for set options
			EOptions m_options = EOptions::None;
			bool has(EOptions opt) const
			{
				return (m_options & opt) == opt;
			}

			// Fluent API
			CreateOptions& colours(std::span<Colour32 const> colours)
			{
				m_colours = colours;
				m_options |= EOptions::Colours;
				return *this;
			}
			CreateOptions& bake(m4x4 const& m)
			{
				m_bake = m;
				m_options |= EOptions::BakeTransform;
				return *this;
			}
			CreateOptions& bake(m4x4 const* m)
			{
				if (m) bake(*m);
				return *this;
			}
			CreateOptions& tex_diffuse(Texture2DPtr tex, SamplerPtr sam)
			{
				m_tex_diffuse = tex;
				m_sam_diffuse = sam;
				m_options |= EOptions::DiffuseTexture;
				return *this;
			}
			CreateOptions& tex2surf(m4x4 const& t2s)
			{
				m_t2s = t2s;
				m_options |= EOptions::TextureToSurface;
				return *this;
			}
			CreateOptions& tex2surf(m4x4 const* t2s)
			{
				if (t2s) tex2surf(*t2s);
				return *this;
			}
			CreateOptions& gen_normals(float angle)
			{
				m_gen_normals = angle;
				m_options |= EOptions::NormalGeneration;
				return *this;
			}
		};

		// Points/Sprites *********************************************************************
		// Generate a cloud of points from an array of points
		// Supports optional colours (opts->m_colours), either, 0, 1, or num_points
		static ModelPtr Points(ResourceFactory& factory, std::span<v4 const> points, CreateOptions const* opts = nullptr);

		// Lines ******************************************************************************
		// Generate a batch of lines.
		// 'num_lines' is the number of line segments to create.
		// 'points' is the input array of start and end points for lines.
		// 'directions' is the vector from each point to the next.
		// Supports optional colours (opts->m_colours), either, 0, 1, or num_lines * 2
		static ModelPtr Lines(ResourceFactory& factory, int num_lines, std::span<v4 const> points, CreateOptions const* opts = nullptr);
		static ModelPtr LinesD(ResourceFactory& factory, int num_lines, std::span<v4 const> points, std::span<v4 const> directions, CreateOptions const* opts = nullptr);
		static ModelPtr LineStrip(ResourceFactory& factory, int num_lines, std::span<v4 const> points, CreateOptions const* opts = nullptr);

		// Quad *******************************************************************************
		// Create a quad.
		// Supports optional colours (opts->m_colours), either, 0, 1, or num_quads
		static ModelPtr Quad(ResourceFactory& factory, CreateOptions const* opts = nullptr);
		static ModelPtr Quad(ResourceFactory& factory, int num_quads, std::span<v4 const> verts, CreateOptions const* opts = nullptr);
		static ModelPtr Quad(ResourceFactory& factory, v2 const& anchor, v4 const& quad_w, v4 const& quad_h, iv2 const& divisions = iv2::Zero(), CreateOptions const* opts = nullptr);
		static ModelPtr Quad(ResourceFactory& factory, AxisId axis_id, v2 const& anchor, float width, float height, iv2 const& divisions = iv2::Zero(), CreateOptions const* opts = nullptr);
		static ModelPtr QuadStrip(ResourceFactory& factory, int num_quads, std::span<v4 const> verts, float width, std::span<v4 const> normals = {}, CreateOptions const* opts = nullptr);
		static ModelPtr QuadPatch(ResourceFactory& factory, int dimx, int dimy, CreateOptions const* opts = nullptr);

		// Shape2d ****************************************************************************
		static ModelPtr Ellipse(ResourceFactory& factory, float dimx, float dimy, bool solid, int facets = 40, CreateOptions const* opts = nullptr);		
		static ModelPtr Pie(ResourceFactory& factory, float dimx, float dimy, float ang0, float ang1, float radius0, float radius1, bool solid, int facets = 40, CreateOptions const* opts = nullptr);
		static ModelPtr RoundedRectangle(ResourceFactory& factory, float dimx, float dimy, float corner_radius, bool solid, int facets = 10, CreateOptions const* opts = nullptr);
		static ModelPtr Polygon(ResourceFactory& factory, std::span<v2 const> points, bool solid, CreateOptions const* opts = nullptr);

		// Boxes ******************************************************************************
		static ModelPtr Box(ResourceFactory& factory, float rad, CreateOptions const* opts = nullptr);
		static ModelPtr Box(ResourceFactory& factory, v4_cref rad, CreateOptions const* opts = nullptr);
		static ModelPtr Boxes(ResourceFactory& factory, int num_boxes, std::span<v4 const> points, CreateOptions const* opts = nullptr);
		static ModelPtr BoxList(ResourceFactory& factory, int num_boxes, std::span<v4 const> positions, v4_cref rad, CreateOptions const* opts = nullptr);

		// Sphere *****************************************************************************
		static ModelPtr Geosphere(ResourceFactory& factory, float radius, int divisions = 3, CreateOptions const* opts = nullptr);
		static ModelPtr Geosphere(ResourceFactory& factory, v4_cref radius, int divisions = 3, CreateOptions const* opts = nullptr);
		static ModelPtr Sphere(ResourceFactory& factory, float radius, int wedges = 20, int layers = 5, CreateOptions const* opts = nullptr);
		static ModelPtr Sphere(ResourceFactory& factory, v4 const& radius, int wedges = 20, int layers = 5, CreateOptions const* opts = nullptr);

		// Cylinder ***************************************************************************
		static ModelPtr Cylinder(ResourceFactory& factory, float radius0, float radius1, float height, float xscale = 1.0f, float yscale = 1.0f, int wedges = 20, int layers = 1, CreateOptions const* opts = nullptr);

		// Extrude ****************************************************************************
		static ModelPtr Extrude(ResourceFactory& factory, std::span<v2 const> cs, std::span<v4 const> path, bool closed, bool smooth_cs, CreateOptions const* opts = nullptr);
		static ModelPtr Extrude(ResourceFactory& factory, std::span<v2 const> cs, std::span<m4x4 const> path, bool closed, bool smooth_cs, CreateOptions const* opts = nullptr);

		// Mesh *******************************************************************************
		static ModelPtr Mesh(ResourceFactory& factory, MeshCreationData const& cdata, CreateOptions const* opts = nullptr);

		// SkyBox *****************************************************************************
		static ModelPtr SkyboxGeosphere(ResourceFactory& factory, Texture2DPtr sky_texture, float radius = 1.0f, int divisions = 3, CreateOptions const* opts = nullptr);
		static ModelPtr SkyboxGeosphere(ResourceFactory& factory, std::filesystem::path const& texture_path, float radius = 1.0f, int divisions = 3, CreateOptions const* opts = nullptr);
		static ModelPtr SkyboxFiveSidedCube(ResourceFactory& factory, Texture2DPtr sky_texture, float radius = 1.0f, CreateOptions const* opts = nullptr);
		static ModelPtr SkyboxFiveSidedCube(ResourceFactory& factory, std::filesystem::path const& texture_path, float radius = 1.0f, CreateOptions const* opts = nullptr);
		static ModelPtr SkyboxSixSidedCube(ResourceFactory& factory, Texture2DPtr (&sky_texture)[6], float radius = 1.0f, CreateOptions const* opts = nullptr);
		static ModelPtr SkyboxSixSidedCube(ResourceFactory& factory, std::filesystem::path const& texture_path_pattern, float radius = 1.0f, CreateOptions const* opts = nullptr);

		// ModelFile **************************************************************************
		// Load a P3D model from a stream, emitting models for each mesh via 'out'.
		struct IModelOut
		{
			enum EResult { Continue, Stop };

			virtual ~IModelOut() = default;
			virtual geometry::ESceneParts Parts() const
			{
				return geometry::ESceneParts::All;
			}
			virtual bool ModelFilter(std::string_view model_name) const
			{
				(void)model_name;
				return true; // True means include the model in the output
			}
			virtual bool SkeletonFilter(std::string_view skeleton_name) const
			{
				(void)skeleton_name;
				return true; // True means include the skeleton in the output
			}
			virtual rdr12::FrameRange FrameRange() const
			{
				// The frame range of animation data to return
				return { 0, std::numeric_limits<int>::max() };
			}
			virtual EResult Model(ModelTree&&)
			{
				// Output model receiver
				return EResult::Stop; // Read more models or stop
			}
			virtual EResult Animation(vector<SkeletonPtr>&&, vector<KeyFrameAnimationPtr>&&)
			{
				// Output animation receiver
				return EResult::Stop; // Read more animations or stop
			}
			virtual bool Progress(int64_t step, int64_t total, char const* message, int nest)
			{
				//OutputDebugStringA(std::format("{}{} ({} of {}){}", Indent(nest), message, step, total, nest == 0 ? '\n' : '\r').c_str());
				(void)step, total, message, nest;
				return true;
			}
		};
		static void LoadP3DModel(ResourceFactory& factory, std::istream& src, IModelOut& out, CreateOptions const* opts = nullptr);
		static void Load3DSModel(ResourceFactory& factory, std::istream& src, IModelOut& out, CreateOptions const* opts = nullptr);
		static void LoadSTLModel(ResourceFactory& factory, std::istream& src, IModelOut& out, CreateOptions const* opts = nullptr);
		static void LoadFBXModel(ResourceFactory& factory, std::istream& src, IModelOut& out, CreateOptions const* opts = nullptr);
		static void LoadModel(geometry::EModelFileFormat format, ResourceFactory& factory, std::istream& src, IModelOut& mout, CreateOptions const* opts = nullptr);

		// Text *******************************************************************************

		// A Direct2D font description
		struct Font
		{
			wstring32           m_name;   // Font family name (D2D requires wchar_t)
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
		static ModelPtr Text(ResourceFactory& factory, std::wstring_view text, std::span<TextFormat const> formatting, TextLayout const& layout, float scale, AxisId axis_id, v4& dim_out, CreateOptions const* opts = nullptr);
		static ModelPtr Text(ResourceFactory& factory, std::wstring_view text, std::span<TextFormat const> formatting, TextLayout const& layout, float scale, AxisId axis_id, CreateOptions const* opts = nullptr);
		static ModelPtr Text(ResourceFactory& factory, std::wstring_view text, TextFormat const& formatting, TextLayout const& layout, float scale, AxisId axis_id, v4& dim_out, CreateOptions const* opts = nullptr);
		static ModelPtr Text(ResourceFactory& factory, std::wstring_view text, TextFormat const& formatting, TextLayout const& layout, float scale, AxisId axis_id, CreateOptions const* opts = nullptr);

		// Cache ****************************************************************************************

		// Memory pooling for model buffers
		template <typename VertexType = Vert>
		struct Cache
		{
			using VType = VertexType;
			using VCont = vector<VType>;
			using ICont = pr::geometry::IdxBuf;
			using NCont = vector<NuggetDesc>;

		private:

			// The cached buffers
			struct alignas(16) Buffers
			{
				string32 m_name = {};                 // Model name
				VCont    m_vcont = {};                // Model verts
				ICont    m_icont = {};                // Model faces/lines/points/etc
				NCont    m_ncont = {};                // Model nuggets
				BBox     m_bbox = BBox::Reset();      // Model bounding box
				m4x4     m_m2root = m4x4::Identity(); // Model to root transform
			};
			static Buffers& this_thread_instance()
			{
				// A static instance for this thread
				thread_local static std::unique_ptr<Buffers> buffers;
				if (!buffers) buffers.reset(new Buffers);
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
			m4x4& m_m2root;   // Model to root transform

			Cache() = delete;
			Cache(int vcount, int icount, int ncount, int idx_stride)
				: m_buffers(this_thread_instance())
				, m_in_use(this_thread_cache_in_use())
				, m_name(m_buffers.m_name)
				, m_vcont(m_buffers.m_vcont)
				, m_icont(m_buffers.m_icont)
				, m_ncont(m_buffers.m_ncont)
				, m_bbox(m_buffers.m_bbox)
				, m_m2root(m_buffers.m_m2root)
			{
				assert(vcount >= 0 && icount >= 0 && ncount >= 0 && idx_stride >= 1);
				m_vcont.resize(vcount, {});
				m_icont.resize(icount, idx_stride);
				m_ncont.resize(ncount, {});
				m_in_use = true;
			}
			~Cache()
			{
				Reset();
				m_in_use = false;
			}
			Cache(Cache&& rhs) = delete;
			Cache(Cache const& rhs) = delete;
			Cache operator =(Cache&& rhs) = delete;
			Cache operator =(Cache const& rhs) = delete;

			// Resize all buffers to 0
			void Reset()
			{
				m_name.resize(0);
				m_vcont.resize(0);
				m_icont.resize(0, 1);
				m_ncont.resize(0);
				m_bbox = BBox::Reset();
				m_m2root = m4x4::Identity();
			}

			// Container item counts
			int64_t VCount() const { return isize(m_vcont); }
			int64_t ICount() const { return isize(m_icont); }
			int64_t NCount() const { return isize(m_ncont); }

			// Return the buffer format associated with the index stride
			DXGI_FORMAT IdxFormat() const
			{
				auto stride = m_icont.stride();
				switch (stride)
				{
				case 4: return dx_format_v<uint32_t>.format;
				case 2: return dx_format_v<uint16_t>.format;
				case 1: return dx_format_v<uint8_t>.format;
				default: throw std::runtime_error(std::format("Unsupported index stride: {}", stride));
				}
			}
		};
	};
}
