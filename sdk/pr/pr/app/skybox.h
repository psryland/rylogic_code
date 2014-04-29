//*****************************************************************************************
// Application Framework
//  Copyright © Rylogic Ltd 2012
//*****************************************************************************************
#pragma once
#ifndef PR_APP_SKYBOX_H
#define PR_APP_SKYBOX_H

#include "pr/app/forward.h"

namespace pr
{
	namespace app
	{
		// A base class for a skybox
		struct Skybox :pr::events::IRecv<pr::rdr::Evt_UpdateScene>
		{
			// Skybox styles - implies texture organisation as well
			enum Style
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

			// A renderer instance type for the skybox
			#define PR_RDR_INST(x)\
				x(pr::m4x4            ,m_i2w   ,pr::rdr::EInstComp::I2WTransform   )\
				x(pr::rdr::ModelPtr   ,m_model ,pr::rdr::EInstComp::ModelPtr       )\
				x(pr::rdr::SKOverride ,m_sko   ,pr::rdr::EInstComp::SortkeyOverride)
			PR_RDR_DEFINE_INSTANCE(Instance, PR_RDR_INST);
			#undef PR_RDR_INST

			typedef pr::Array<pr::rdr::Texture2DPtr> TexCont;

			Instance m_inst;  // The skybox instance
			TexCont  m_tex;   // The textures used in the skybox
			float    m_scale; // Model scaler
			m4x4     m_i2w;   // The base orientation transform for the skybox (updated with camera position in OnEvent)

			// Constructs a skybox model and instance.
			// 'texpath' should be an unrolled cube texture
			Skybox(pr::Renderer& rdr, wstring const& texpath, Style tex_style, float scale = 1000.0f)
				:m_inst()
				,m_tex()
				,m_scale(scale)
				,m_i2w(pr::Scale4x4(scale, pr::v4Origin))
			{
				switch (tex_style)
				{
				default: PR_ASSERT(PR_DBG, false, "Unsupported texture style");
				case Geosphere:     InitGeosphere(rdr, texpath); break;
				case FiveSidedCube: InitFiveSidedCube(rdr, texpath); break;
				case SixSidedCube:  InitSixSidedCube(rdr, texpath); break;
				}

				// Set the sortkey so that the skybox draws last
				m_inst.m_sko.Group(pr::rdr::ESortGroup::Skybox);
				m_inst.m_model->m_name = "skybox";
			}

			// Add the skybox to a viewport
			void OnEvent(pr::rdr::Evt_UpdateScene const& e) override
			{
				auto& view = e.m_scene.m_view;
				m_inst.m_i2w = m_i2w;
				m_inst.m_i2w.pos = view.m_c2w.pos;
				e.m_scene.AddInstance(m_inst);
			}

		private:

			// Create a model for a geosphere skybox
			void InitGeosphere(pr::Renderer& rdr, wstring const& texpath)
			{
				using namespace pr::rdr;

				// Model nugget properties for the skybox
				NuggetProps ddata;
				ddata.m_tex_diffuse = rdr.m_tex_mgr.CreateTexture2D(AutoId, SamplerDesc::WrapSampler(), texpath.c_str());
				ddata.m_rsb = RSBlock::SolidCullFront();

				// Create the skybox model
				m_inst.m_model = ModelGenerator<VertPT>::Geosphere(rdr, 1.0f, 3, Colour32White, &ddata);
			}

			// Create a model for a 5-sided cubic dome
			void InitFiveSidedCube(pr::Renderer& rdr, wstring const& texpath)
			{
				using namespace pr::rdr;

				float const s = 0.5f;
				VertPT const verts[] =
				{
					{{-s,  s,  s}, { 0.25f, 0.25f}}, //0
					{{-s,  s, -s}, { 0.25f, 0.75f}}, //1
					{{ s,  s, -s}, { 0.75f, 0.75f}}, //2
					{{ s,  s,  s}, { 0.75f, 0.25f}}, //3
					{{-s, -s,  s}, {-0.25f, 0.25f}}, //4
					{{-s, -s, -s}, {-0.25f, 0.75f}}, //5
					{{-s, -s, -s}, { 0.25f, 1.25f}}, //6
					{{ s, -s, -s}, { 0.75f, 1.25f}}, //7
					{{ s, -s, -s}, { 1.25f, 0.75f}}, //8
					{{ s, -s,  s}, { 1.25f, 0.25f}}, //9
					{{ s, -s,  s}, { 0.75f,-0.25f}}, //10
					{{-s, -s,  s}, { 0.25f,-0.25f}}, //11
				};
				pr::uint16 const indices[] =
				{
					0,  1,  2,  0,  2,  3,
					0,  4,  5,  0,  5,  1,
					1,  6,  7,  1,  7,  2,
					2,  8,  9,  2,  9,  3,
					3, 10, 11,  3, 11,  0,
				};

				// Create the skybox model
				m_inst.m_model = rdr.m_mdl_mgr.CreateModel(MdlSettings(verts, indices, pr::BBoxReset, "skybox"));

				// Create a model nugget for the skybox
				NuggetProps ddata(EPrim::TriList, VertPT::GeomMask, rdr.m_shdr_mgr.FindShaderFor(VertPT::GeomMask).m_ptr);
				ddata.m_tex_diffuse = rdr.m_tex_mgr.CreateTexture2D(AutoId, SamplerDesc::ClampSampler(), texpath.c_str());
				m_inst.m_model->CreateNugget(ddata);
			}

			// Create a model for a 6-sided cube
			void InitSixSidedCube(pr::Renderer& rdr, wstring const& texpath)
			{
				using namespace pr::rdr;

				float const s = 0.5f, t0 = 0.0f, t1 = 1.0f;
				VertPT const verts[] =
				{
					{{+s, +s, -s}, {t0, t0}}, //  0 // +X
					{{+s, -s, -s}, {t0, t1}}, //  1
					{{+s, -s, +s}, {t1, t1}}, //  2
					{{+s, +s, +s}, {t1, t0}}, //  3
					{{-s, +s, +s}, {t0, t0}}, //  4 // -X
					{{-s, -s, +s}, {t0, t1}}, //  5
					{{-s, -s, -s}, {t1, t1}}, //  6
					{{-s, +s, -s}, {t1, t0}}, //  7
					{{+s, +s, +s}, {t0, t0}}, //  8 // +Y
					{{-s, +s, +s}, {t0, t1}}, //  9
					{{-s, +s, -s}, {t1, t1}}, // 10
					{{+s, +s, -s}, {t1, t0}}, // 11
					{{+s, -s, -s}, {t0, t0}}, // 12 // -Y
					{{-s, -s, -s}, {t0, t1}}, // 13
					{{-s, -s, +s}, {t1, t1}}, // 14
					{{+s, -s, +s}, {t1, t0}}, // 15
					{{+s, +s, +s}, {t0, t0}}, // 16 // +Z
					{{+s, -s, +s}, {t0, t1}}, // 17
					{{-s, -s, +s}, {t1, t1}}, // 18
					{{-s, +s, +s}, {t1, t0}}, // 19
					{{-s, +s, -s}, {t0, t0}}, // 20 // -Z
					{{-s, -s, -s}, {t0, t1}}, // 21
					{{+s, -s, -s}, {t1, t1}}, // 22
					{{+s, +s, -s}, {t1, t0}}, // 23
				};
				pr::uint16 const indices[] =
				{
					0, 1, 2,  0, 2, 3, // 0 - 6
					4, 5, 6,  4, 6, 7, // 6 - 12
					8, 9,10,  8,10,11, // 12 - 18
					12,13,14, 12,14,15, // 18 - 24
					16,17,18, 16,18,19, // 24 - 30
					20,21,22, 20,22,23, // 30 - 36
				};

				// Create the skybox model
				m_inst.m_model = rdr.m_mdl_mgr.CreateModel(MdlSettings(verts, indices, pr::BBoxReset, "skybox"));

				// Create the model nuggets for the skybox
				NuggetProps ddata(EPrim::TriList, VertPT::GeomMask);

				// One texture per nugget
				wstring tpath = texpath;
				size_t ofs    = tpath.find(L"??", 0, 2);
				PR_ASSERT(PR_DBG, ofs != string::npos, "Provided path does not include '??' characters");
				wchar_t const axes[6][3] = {{L"+X"},{L"-X"},{L"+Y"},{L"-Y"},{L"+Z"},{L"-Z"}};
				for (int i = 0; i != 6; ++i)
				{
					// Load the texture for this face of the skybox
					tpath[ofs+0] = axes[i][0];
					tpath[ofs+1] = axes[i][1];
					ddata.m_tex_diffuse = rdr.m_tex_mgr.CreateTexture2D(AutoId, SamplerDesc::ClampSampler(), tpath.c_str());

					// Create the render nugget for this face of the skybox
					rdr::Range vrange = rdr::Range::make(i*4, (i+1)*4);
					rdr::Range irange = rdr::Range::make(i*6, (i+1)*6);
					m_inst.m_model->CreateNugget(ddata, &vrange, &irange);
				}
			}
		};
	}
}

#endif
