//*****************************************************************************************
// Application Framework
//  Copyright (c) Rylogic Ltd 2012
//*****************************************************************************************
#pragma once
#include "pr/app/forward.h"

namespace pr::app
{
	// A base class for a sky box
	struct Skybox
	{
		// Sky box styles - implies texture organisation as well
		enum class EStyle
		{
			// This is a geosphere with inward facing normals
			Geosphere,

			// This is a cubic dome, the texture should be a '+' shape with
			// the top portion from 0.25->0.75, and sides from 0->0.25, 0.75->1.0
			FiveSidedCube,

			// The is a full 6-sided cube, 'texpath' should be a filepath with
			// the format 'path\filename??.extn' where '??' will be replaced by
			// +X,-X,+Y,-Y,+Z,-Z to generate the six texture filepaths
			SixSidedCube,
		};

		// A renderer instance type for the sky box
		#define PR_RDR_INST(x)\
			x(m4x4            ,m_i2w   ,rdr::EInstComp::I2WTransform   )\
			x(rdr::ModelPtr   ,m_model ,rdr::EInstComp::ModelPtr       )\
			x(rdr::SKOverride ,m_sko   ,rdr::EInstComp::SortkeyOverride)
		PR_RDR_DEFINE_INSTANCE(Instance, PR_RDR_INST);
		#undef PR_RDR_INST

		using TexCont = pr::vector<rdr::Texture2DPtr>;

		Instance m_inst;  // The sky box instance
		TexCont  m_tex;   // The textures used in the sky box
		float    m_scale; // Model scaler
		m4x4     m_i2w;   // The base orientation transform for the sky box (updated with camera position in OnEvent)

		// Constructs a sky box model and instance.
		// 'texpath' should be an unrolled cube texture
		Skybox(Renderer& rdr, std::filesystem::path const& texpath, EStyle tex_style, float scale = 100.0f)
			:m_inst()
			,m_tex()
			,m_scale(scale)
			,m_i2w(m4x4::Scale(scale, pr::v4Origin))
		{
			switch (tex_style)
			{
			default: PR_ASSERT(PR_DBG, false, "Unsupported texture style");
			case EStyle::Geosphere:     InitGeosphere(rdr, texpath); break;
			case EStyle::FiveSidedCube: InitFiveSidedCube(rdr, texpath); break;
			case EStyle::SixSidedCube:  InitSixSidedCube(rdr, texpath); break;
			}

			// Set the sort key so that the sky box draws last
			m_inst.m_sko.Group(pr::rdr::ESortGroup::Skybox);
			m_inst.m_model->m_name = "sky box";
		}

		// Add the sky box to a viewport
		void AddToScene(rdr::Scene& scene)
		{
			auto& view = scene.m_view;
			m_inst.m_i2w = m_i2w;
			m_inst.m_i2w.pos = view.m_c2w.pos;
			scene.AddInstance(m_inst);
		}

	private:

		// Create a model for a geosphere sky box
		void InitGeosphere(pr::Renderer& rdr, std::filesystem::path const& texpath)
		{
			using namespace pr::rdr;

			// Model nugget properties for the sky box
			NuggetProps ddata;
			ddata.m_tex_diffuse = rdr.m_tex_mgr.CreateTexture2D(AutoId, texpath.c_str(), SamplerDesc::LinearWrap(), false, "skybox");
			ddata.m_geom = EGeom::Vert | EGeom::Tex0;
			ddata.m_rsb = RSBlock::SolidCullFront();

			// Create the sky box model
			m_inst.m_model = ModelGenerator<>::Geosphere(rdr, 1.0f, 3, Colour32White, &ddata);
		}

		// Create a model for a 5-sided cubic dome
		void InitFiveSidedCube(pr::Renderer& rdr, std::filesystem::path const& texpath)
		{
			using namespace pr::rdr;

			float const s = 0.5f;
			static Vert const verts[] =
			{
				{{-s,  s,  s, 1}, ColourWhite, v4Zero, { 0.25f, 0.25f}}, //0
				{{-s,  s, -s, 1}, ColourWhite, v4Zero, { 0.25f, 0.75f}}, //1
				{{ s,  s, -s, 1}, ColourWhite, v4Zero, { 0.75f, 0.75f}}, //2
				{{ s,  s,  s, 1}, ColourWhite, v4Zero, { 0.75f, 0.25f}}, //3
				{{-s, -s,  s, 1}, ColourWhite, v4Zero, {-0.25f, 0.25f}}, //4
				{{-s, -s, -s, 1}, ColourWhite, v4Zero, {-0.25f, 0.75f}}, //5
				{{-s, -s, -s, 1}, ColourWhite, v4Zero, { 0.25f, 1.25f}}, //6
				{{ s, -s, -s, 1}, ColourWhite, v4Zero, { 0.75f, 1.25f}}, //7
				{{ s, -s, -s, 1}, ColourWhite, v4Zero, { 1.25f, 0.75f}}, //8
				{{ s, -s,  s, 1}, ColourWhite, v4Zero, { 1.25f, 0.25f}}, //9
				{{ s, -s,  s, 1}, ColourWhite, v4Zero, { 0.75f,-0.25f}}, //10
				{{-s, -s,  s, 1}, ColourWhite, v4Zero, { 0.25f,-0.25f}}, //11
			};
			static uint16_t const indices[] =
			{
				0,  1,  2,  0,  2,  3,
				0,  4,  5,  0,  5,  1,
				1,  6,  7,  1,  7,  2,
				2,  8,  9,  2,  9,  3,
				3, 10, 11,  3, 11,  0,
			};

			// Create the sky box model
			m_inst.m_model = rdr.m_mdl_mgr.CreateModel(MdlSettings(verts, indices, pr::BBoxReset, "sky box"));

			// Create a model nugget for the sky box
			NuggetProps ddata;
			ddata.m_topo = EPrim::TriList;
			ddata.m_geom = EGeom::Vert|EGeom::Tex0;
			ddata.m_tex_diffuse = rdr.m_tex_mgr.CreateTexture2D(AutoId, texpath.c_str(), SamplerDesc::LinearClamp(), false, "skybox");
			m_inst.m_model->CreateNugget(ddata);
		}

		// Create a model for a 6-sided cube
		void InitSixSidedCube(pr::Renderer& rdr, std::filesystem::path  const& texpath)
		{
			using namespace pr::rdr;

			constexpr float s = 0.5f, t0 = 0.0f, t1 = 1.0f;
			static Vert const verts[] =
			{
				{{+s, +s, -s, 1}, ColourWhite, v4Zero, {t0, t0}}, //  0 // +X
				{{+s, -s, -s, 1}, ColourWhite, v4Zero, {t0, t1}}, //  1
				{{+s, -s, +s, 1}, ColourWhite, v4Zero, {t1, t1}}, //  2
				{{+s, +s, +s, 1}, ColourWhite, v4Zero, {t1, t0}}, //  3
				{{-s, +s, +s, 1}, ColourWhite, v4Zero, {t0, t0}}, //  4 // -X
				{{-s, -s, +s, 1}, ColourWhite, v4Zero, {t0, t1}}, //  5
				{{-s, -s, -s, 1}, ColourWhite, v4Zero, {t1, t1}}, //  6
				{{-s, +s, -s, 1}, ColourWhite, v4Zero, {t1, t0}}, //  7
				{{+s, +s, +s, 1}, ColourWhite, v4Zero, {t0, t0}}, //  8 // +Y
				{{-s, +s, +s, 1}, ColourWhite, v4Zero, {t0, t1}}, //  9
				{{-s, +s, -s, 1}, ColourWhite, v4Zero, {t1, t1}}, // 10
				{{+s, +s, -s, 1}, ColourWhite, v4Zero, {t1, t0}}, // 11
				{{+s, -s, -s, 1}, ColourWhite, v4Zero, {t0, t0}}, // 12 // -Y
				{{-s, -s, -s, 1}, ColourWhite, v4Zero, {t0, t1}}, // 13
				{{-s, -s, +s, 1}, ColourWhite, v4Zero, {t1, t1}}, // 14
				{{+s, -s, +s, 1}, ColourWhite, v4Zero, {t1, t0}}, // 15
				{{+s, +s, +s, 1}, ColourWhite, v4Zero, {t0, t0}}, // 16 // +Z
				{{+s, -s, +s, 1}, ColourWhite, v4Zero, {t0, t1}}, // 17
				{{-s, -s, +s, 1}, ColourWhite, v4Zero, {t1, t1}}, // 18
				{{-s, +s, +s, 1}, ColourWhite, v4Zero, {t1, t0}}, // 19
				{{-s, +s, -s, 1}, ColourWhite, v4Zero, {t0, t0}}, // 20 // -Z
				{{-s, -s, -s, 1}, ColourWhite, v4Zero, {t0, t1}}, // 21
				{{+s, -s, -s, 1}, ColourWhite, v4Zero, {t1, t1}}, // 22
				{{+s, +s, -s, 1}, ColourWhite, v4Zero, {t1, t0}}, // 23
			};
			static uint16_t const indices[] =
			{
				0, 1, 2,  0, 2, 3, // 0 - 6
				4, 5, 6,  4, 6, 7, // 6 - 12
				8, 9,10,  8,10,11, // 12 - 18
				12,13,14, 12,14,15, // 18 - 24
				16,17,18, 16,18,19, // 24 - 30
				20,21,22, 20,22,23, // 30 - 36
			};

			// Create the sky box model
			m_inst.m_model = rdr.m_mdl_mgr.CreateModel(MdlSettings(verts, indices, pr::BBoxReset, "sky box"));

			// Create the model nuggets for the sky box
			NuggetProps ddata(EPrim::TriList, EGeom::Vert|EGeom::Tex0);

			// One texture per nugget
			auto tpath = texpath.wstring();
			size_t ofs = tpath.find(L"??", 0, 2);
			if (ofs == std::wstring::npos)
				throw std::runtime_error("Provided path does not include '??' characters");

			wchar_t const axes[6][3] = {{L"+X"},{L"-X"},{L"+Y"},{L"-Y"},{L"+Z"},{L"-Z"}};
			for (int i = 0; i != 6; ++i)
			{
				// Load the texture for this face of the sky box
				tpath[ofs+0] = axes[i][0];
				tpath[ofs+1] = axes[i][1];
				ddata.m_tex_diffuse = rdr.m_tex_mgr.CreateTexture2D(AutoId, tpath.c_str(), SamplerDesc::LinearClamp(), false, "skybox");

				// Create the render nugget for this face of the sky box
				ddata.m_vrange = rdr::Range(i*4, (i+1)*4);
				ddata.m_irange = rdr::Range(i*6, (i+1)*6);
				m_inst.m_model->CreateNugget(ddata);
			}
		}
	};
}
