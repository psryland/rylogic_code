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
		struct Skybox :pr::events::IRecv<pr::rdr::Evt_SceneRender>
		{
			// Skybox styles - implies texture organisation as well
			enum Style
			{
				// This is a cubic dome, the texture should be a '+' shape with
				// the top portion from 0.25->0.75, and sides from 0->0.25, 0.75->1.0
				FiveSidedCube,
				
				// The is a full 6-sided cube, 'texpath' should be a filepath with
				// the format 'path\filename??.extn' where '??' will be replaced by
				// +X,-X,+Y,-Y,+Z,-Z to generate the six texture filepaths
				SixSidedCube,
			};
			
			// A renderer instance type for the skybox
			PR_RDR_DECLARE_INSTANCE_TYPE3
			(
				Instance
				,pr::rdr::ModelPtr           ,m_model ,pr::rdr::instance::EComp::ModelPtr
				,pr::m4x4                    ,m_i2w   ,pr::rdr::instance::EComp::I2WTransform
				,pr::rdr::sort_key::Override ,m_sko   ,pr::rdr::instance::EComp::SortkeyOverride
			);
			typedef pr::Array<pr::rdr::Texture2DPtr> TexCont;
			
			Instance m_inst;  // The skybox instance
			TexCont  m_tex;   // The textures used in the skybox
			float    m_scale; // Model scaler
			
			// Constructs a skybox model and instance.
			// 'texpath' should be an unrolled cube texture
			Skybox(pr::Renderer& rdr, wstring const& texpath, Style tex_style)
			:m_inst()
			,m_tex()
			,m_scale(1000.0f)
			{
				switch (tex_style)
				{
				default: PR_ASSERT(PR_DBG, false, "Unsupported texture style");
				case FiveSidedCube: InitFiveSidedCube(rdr, texpath); break;
				case SixSidedCube:  InitSixSidedCube(rdr, texpath); break;
				}
			}
			
			// Add the skybox to a viewport
			void Skybox::OnEvent(pr::rdr::Evt_SceneRender const& e)
			{
				m_inst.m_i2w = pr::Scale4x4(m_scale, e.m_scene->View().m_c2w.pos);
				e.m_scene->AddInstance(m_inst);
			}
		
		private:
			// Returns a material to use for the skybox, with render states set appropriately
			pr::rdr::Material InitMaterial(pr::Renderer& rdr)
			{
				using namespace pr::rdr::shader;
				
				// From the word of Al:
				//  Always try to maintain the z buffer (i.e. writeenable) even for huds etc
				//  Stereoscope rendering requires everything to have correct depth
				//  Render the skybox after all opaques to reduce overdraw
				pr::rdr::Material mat;
				
				// Create a material for rendering the skybox
				mat.m_shader = rdr.m_mat_mgr.FindShaderFor<pr::rdr::VertPT>();
				//mat.m_shader = rdr.m_mat_mgr.CreateShader(pr::rdr::AutoId, &vs_basic, &ps_basic);
				
				//pr::rdr::Material mat = rdr.m_mat_mgr.GetMaterial(pr::geom::EVT);
				//mat.m_rsb.SetRenderState(D3DRS_ZENABLE, D3DZB_TRUE);     // the type of z buffering to use (read/test against the z buffer)
				//mat.m_rsb.SetRenderState(D3DRS_ALPHABLENDENABLE, FALSE); // no alphablend
				return mat;
			}
			
			// Create a model for a 5-sided cubic dome
			void InitFiveSidedCube(pr::Renderer& rdr, wstring const& texpath)
			{
				float const s = 0.5f;
				pr::rdr::VertPT const verts[] = 
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
					0,  1,  2,
					0,  2,  3,
					0,  4,  5,
					0,  5,  1,
					1,  6,  7,
					1,  7,  2,
					2,  8,  9,
					2,  9,  3,
					3, 10, 11,
					3, 11,  0,
				};
				m_inst.m_model = rdr.m_mdl_mgr.CreateModel(pr::rdr::MdlSettings(verts, indices));
				
				// Load the skybox texture
				pr::rdr::TextureDesc desc;
				m_tex.push_back(rdr.m_mat_mgr.CreateTexture2D(pr::rdr::AutoId, desc, texpath.c_str()));
				//m_tex[0]->m_addr_mode.m_addrU = D3DTADDRESS_CLAMP;
				//m_tex[0]->m_addr_mode.m_addrV = D3DTADDRESS_CLAMP;
	
				// Add a render nugget
				pr::rdr::Material mat = InitMaterial(rdr);
				mat.m_tex_diffuse = m_tex[0];
				m_inst.m_model->CreateNugget(mat, D3D11_PRIMITIVE_TOPOLOGY_TRIANGLELIST);
				
				// Set the sortkey so that the skybox draws last
				m_inst.m_sko.Group(pr::rdr::sort_key::SortGroup_Skybox);
			}
			
			// Create a model for a 6-sided cube
			void InitSixSidedCube(pr::Renderer& rdr, wstring const& texpath)
			{
				float const s = 0.5f, t0 = 0.0f, t1 = 1.0f;
				pr::rdr::VertPT const verts[] = 
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
					 0, 1, 2,  0, 2, 3,
					 4, 5, 6,  4, 6, 7,
					 8, 9,10,  8,10,11,
					12,13,14, 12,14,15,
					16,17,18, 16,18,19,
					20,21,22, 20,22,23,
				};
				
				m_inst.m_model = rdr.m_mdl_mgr.CreateModel(pr::rdr::MdlSettings(verts, indices));
				
				wstring tpath = texpath;
				size_t ofs   = tpath.find(L"??", 0, 2);
				PR_ASSERT(PR_DBG, ofs != string::npos, "Provided path does not include '??' characters");
				
				// Add a render nugget
				pr::rdr::Material mat = InitMaterial(rdr);
				
				// Load the skybox textures and add render nuggets
				wchar_t const axes[6][3] = {{L"+X"},{L"-X"},{L"+Y"},{L"-Y"},{L"+Z"},{L"-Z"}};
				for (int i = 0; i != 6; ++i)
				{
					tpath[ofs+0] = axes[i][0];
					tpath[ofs+1] = axes[i][1];
					pr::rdr::TextureDesc desc;
					m_tex.push_back(rdr.m_mat_mgr.CreateTexture2D(pr::rdr::AutoId, desc, tpath.c_str()));
					//m_tex.back()->m_addr_mode.m_addrU = D3DTADDRESS_CLAMP;
					//m_tex.back()->m_addr_mode.m_addrV = D3DTADDRESS_CLAMP;
					
					pr::rdr::Range vrange = pr::rdr::Range::make(i*4, (i+1)*4);
					pr::rdr::Range irange = pr::rdr::Range::make(i*6, (i+1)*6);
					
					// Add a render nugget
					mat.m_tex_diffuse = m_tex.back();
					m_inst.m_model->CreateNugget(mat, D3D11_PRIMITIVE_TOPOLOGY_TRIANGLELIST, &vrange, &irange);
				}
			}
		};
	}
}

#endif
