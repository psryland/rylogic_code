//*****************************************
//*****************************************
#include "test.h"
#include "pr/maths/maths.h"
#include "pr/renderer/renderer.h"
#include "pr/camera/trackball_3d.h"
#include "pr/storage/xfile/xfile.h"

namespace TestRenderer
{
	using namespace pr;
	using namespace pr::rdr;
	
	PR_RDR_DECLARE_INSTANCE_TYPE3
	(
		Instance,
		pr::rdr::Model* ,m_model   ,pr::rdr::instance::ECpt_ModelPtr,
		pr::m4x4        ,m_i2w     ,pr::rdr::instance::ECpt_I2WTransform,
		pr::Colour32    ,m_colour0 ,pr::rdr::instance::ECpt_TintColour32
	);

	// Test copying viewports
	VPSettings ViewportSettings(Renderer& renderer)
	{
		static ViewportId vpid = 0;
		VPSettings vp_settings;
		vp_settings.m_renderer   = &renderer;
		vp_settings.m_identifier = vpid++;
		return vp_settings;
	}
	void Run()
	{
		rdr::Allocator allocator;

		// Get the configuration to use for the renderer
		HWND hwnd = GetConsoleWindow();
		RECT r; GetClientRect(hwnd, &r);
		IRect rect = IRect::make(0, 0, r.right - r.left, r.bottom - r.top);;
		try
		{
			//DeviceConfig config = GetDefaultDeviceConfigFullScreen(640, 480);
			DeviceConfig config = GetDefaultDeviceConfigWindowed(/*D3DDEVTYPE_HAL, true*/);
			
			// Renderer
			RdrSettings rdr_settings;
			rdr_settings.m_window_handle      = hwnd;
			rdr_settings.m_device_config      = config;
			rdr_settings.m_allocator          = &allocator;
			rdr_settings.m_client_area        = rect;
			rdr_settings.m_background_colour  = 0xFF0000A0;
			rdr_settings.m_max_shader_version = "v9_9";
			Renderer renderer(rdr_settings);
	
			// Viewport
			Viewport viewport(ViewportSettings(renderer));
			//viewport.RenderState(D3DRS_CULLMODE, D3DCULL_CW);
			//viewport.ViewRect(pr::FRect::make(0,0,0.5f,1.0f));

			//Viewport dbgview(ViewportSettings(renderer));
			//dbgview .ViewRect(pr::FRect::make(0.5f,0,1.0f,1.0f));
			
			ViewportGroup viewgrp;
			viewgrp.Add(viewport);
			//viewgrp.Add(dbgview);
			
			{// Lights
			rdr::Light& light = renderer.m_lighting_manager.m_light[0];
			light.m_type           = pr::rdr::ELight::Point;
			light.m_on             = true;
			light.m_direction      = pr::v4::normal3(-1,0,0,0);//pr::v4::normal3(-0.831970f,-0.521140f,0.190425f,0);
			light.m_position       = pr::v4::make(2.0f, 2.0f, 1.0f, 1.0f);
			light.m_ambient        = Colour::make(0.01f, 0.1f, 0.01f, 0.0f);
			light.m_diffuse        = Colour::make(0.5f, 0.5f, 0.5f, 1.0f);
			light.m_specular       = Colour::make(0.1f, 0.1f, 0.1f, 0.0f);
			light.m_specular_power = 1000.0f;
			light.m_cast_shadows   = true;
			}
			{
			rdr::Light& light = renderer.m_lighting_manager.m_light[1];
			light.m_type           = pr::rdr::ELight::Point;
			light.m_on             = true;
			light.m_direction      = pr::v4::make(0.0f, 0.0f, 1.0f, 0.0f);
			light.m_position       = pr::v4::make(0.0f, 0.0f, 5.0f, 1.0f);
			light.m_ambient        = Colour::make(0.01f, 0.1f, 0.01f, 0.0f);
			light.m_diffuse        = Colour::make(0.5f, 0.5f, 0.5f, 1.0f);
			light.m_specular       = Colour::make(0.1f, 0.1f, 0.1f, 0.0f);
			light.m_specular_power = 1000.0f;
			//light.m_cast_shadows   = true;
			}

			// Register effects
			rdr::Effect2 const* effect = 0;
			{
				using namespace pr::rdr::effect;
				using namespace pr::rdr::effect::frag;
				Desc desc(renderer.D3DDevice());
				desc.Add(Txfm()                                    );
				//desc.Add(Tinting(0, Tinting::EStyle_Tint)          );
				desc.Add(Lighting(1, 1, true)                      );
				//desc.Add(EnvMap()                                  );
				desc.Add(Terminator()                              );
				renderer.m_material_manager.CreateEffect(desc, effect);
			}
			//{
			//	using namespace pr::rdr::effect::frag;
			//	Buffer buf;
			//	Add(buf, Txfm()                                    );
			//	Add(buf, Tinting(0, Tinting::EStyle_Tint)          );
			//	Add(buf, Lighting(1, 1, true)                      );
			//	Add(buf, EnvMap()                                  );
			//	Add(buf ,Terminator()                              );
			//	renderer.m_material_manager.CreateEffect(buf, effect);
			//}

			// Load a Texture
			//pr::rdr::Texture const* texture = 0;
			//renderer.m_material_manager.LoadTexture("q:/sdk/pr/res/ship_01.tga", texture);

			// Create a material
			pr::rdr::Material material = pr::rdr::Material::make(effect, 0);
			
			// Create models
			Model* plane = pr::rdr::model::Quad(renderer, pr::v4Origin, pr::v4XAxis, 2.0f, 2.0f);
			//Model* plane = pr::rdr::model::Quad(renderer, pr::v4Origin, pr::v4::normal3( 0.662170f,  0.000000f,  0.749353f, 0.000000f), 40.034516f, 12.4264078f);
			Model* model = pr::rdr::model::Box(renderer, pr::v4::make(1,1,1,0));
			std::vector<pr::v4> points;
			for (float k = -1.0f; k <= 1.0f; k += 0.5f)
			for (float j = -1.0f; j <= 1.0f; j += 0.5f)
			for (float i = -1.0f; i <= 1.0f; i += 0.5f) points.push_back(pr::v4::make(0,0,0,1) + pr::v4::make(i,j,k,0));
			Model* boxlist = pr::rdr::model::BoxList(renderer, pr::v4::make(0.02f), &points[0], points.size()); 

			//pr::Geometry geometry; Verify(pr::xfile::Load("q:/sdk/pr/res/ship_01.x", geometry));
			//Model* model = 0; Verify(pr::rdr::LoadMesh(renderer, geometry.m_frame.front().m_mesh, model));
			plane->SetMaterial(material, pr::rdr::model::EPrimitive::TriangleList, true);
			model->SetMaterial(material, pr::rdr::model::EPrimitive::TriangleList, true);
			boxlist->SetMaterial(material, pr::rdr::model::EPrimitive::TriangleList, true);
			
			// Create instances
			Instance inst[3];
			inst[0].m_model = plane;
			inst[0].m_i2w = pr::Translation(-1.0f, 0.0f, 0.0f);
			inst[0].m_colour0.set(0xA0, 0xFF, 0xA0, 0xFF);
			viewgrp.AddInstance(inst[0]);
			
			inst[1].m_model = model;
			inst[1].m_i2w = pr::Translation(0.0f, 0.0f, 0.0f);
			inst[1].m_colour0.set(0xFF, 0x80, 0x80, 0xFF);
			viewgrp.AddInstance(inst[1]);
			
			inst[2].m_model = boxlist;
			inst[2].m_i2w = pr::Translation(0.0f, 0.0f, 0.0f);
			inst[2].m_colour0.set(0xFF, 0x80, 0x80, 0xFF);
			viewgrp.AddInstance(inst[2]);

			// Camera
			pr::camera::Trackball3D cam(maths::pi_by_4, rect.Aspect());
			pr::BoundingBox bbox = BBoxReset;
			pr::Encompase(bbox, inst[0].m_i2w * inst[0].m_model->m_bbox);
			pr::Encompase(bbox, inst[1].m_i2w * inst[1].m_model->m_bbox);
			pr::Encompase(bbox, inst[2].m_i2w * inst[2].m_model->m_bbox);
			//cam.View(bbox, pr::v4::normal3(-1,0,0,0), pr::v4YAxis, true);
			cam.LookAt(pr::v4::make(5.0f,0,0,1), pr::v4Origin, pr::v4YAxis, true);

			//cam.CameraToWorld(pr::m4x4::make(
			//	pr::v4::make(-0.051171f, -0.031261f, -0.998201f, 0.000000),
			//	pr::v4::make(0.521329f, -0.853356f, -0.000000f, 0.000000 ),
			//	pr::v4::make(-0.851820f, -0.520391f, 0.059964f, 0.000000 ),
			//	pr::v4::make(-4.289804f, -2.620712f, -0.299100f, 1.000000)
			//	), true);

			bool light_is_cam_relative = true;
			rdr::Light& light = renderer.m_lighting_manager.m_light[0];
			for (float t = 0.0f;; t = pr::Fmod(t + 0.002f, maths::two_pi))
			{
				// Rotate the instance
				//inst[0].m_i2w = Rotation4x4(0.0f,  t, t, inst[0].m_i2w.pos);
				//inst[1].m_i2w = Rotation4x4(0.0f, -t, t, inst[1].m_i2w.pos);

				// Navigate
				if (GetForegroundWindow() == GetConsoleWindow())
				{
					cam.KBNav(0.2f, 0.03f);
					if (cam.KeyPress('Q')) break;
					if (cam.KeyPress('L')) light_is_cam_relative = !light_is_cam_relative;

					//rdr::Light& light = renderer.m_lighting_manager.m_light[0];
					//if (cam.KeyDown(VK_LEFT )) light.m_direction = pr::Rotation4x4(0, 0.02f,0,pr::v4Origin) * light.m_direction;
					//if (cam.KeyDown(VK_RIGHT)) light.m_direction = pr::Rotation4x4(0,-0.02f,0,pr::v4Origin) * light.m_direction;
					//if (cam.KeyDown(VK_UP   )) light.m_direction = pr::Rotation4x4( 0.02f,0,0,pr::v4Origin) * light.m_direction;
					//if (cam.KeyDown(VK_DOWN )) light.m_direction = pr::Rotation4x4(-0.02f,0,0,pr::v4Origin) * light.m_direction;
				}
				viewgrp.SetView(cam);

				// Create a camera to world transform
				//v4 pos = v4::make(4*pr::Sin(-t), 0.0f, 4*pr::Cos(-t), 1.0f);
				//viewgrp.CameraToWorld(pr::LookAt(pos, v4Origin, v4YAxis));

				pr::v4 ltdir = light.m_direction;
				pr::v4 ltpos = light.m_position;
				if (light_is_cam_relative)
				{
					light.m_direction = cam.CameraToWorld() * light.m_direction;
					light.m_position  = cam.CameraToWorld() * light.m_position;
				}
				
				if (Succeeded(renderer.RenderStart()))
				{
					viewgrp.Render();
					renderer.RenderEnd();
					renderer.Present();
				}

				light.m_direction = ltdir;
				light.m_position  = ltpos;
				
				Sleep(10);
			}
		}
		catch (rdr::Exception const& e)
		{
			MessageBox(hwnd, Fmt("Test failed: (%d) %s\n", e.m_value, e.m_message.c_str()).c_str(), "Bollox", MB_OK);
			_getch();
		}
	}
}

//// Billboard instance
//const uint num_billboards = 2;
//Instance bb_instance;
//{
//	rdr::QuadBuffer bb_quads(renderer, num_billboards);
//	bb_quads.Begin();
//	bb_quads.AddBillboard(0, v4::make(0.5f, 0.5f, 0.0f, 1.0f), 1.0f, 1.0f, Colour32Blue);
//	bb_quads.AddBillboard(1, v4::make(1.0f, 1.0f, 0.0f, 1.0f), 1.0f, 1.0f, Colour32Green);
//	bb_quads.End();
//	bb_quads.m_model->SetMaterial(test_material, model::EPrimitive::TriangleList);
//	
//	bb_instance.m_model = bb_quads.m_model;
//}
//bb_instance.m_colour			= Colour32White;
//bb_instance.m_i2w.identity();
//viewport.AddInstance(bb_instance);

//// Sprite instance
//const uint num_sprites = 2;
//Instance sprite_instance;
//{
//	rdr::QuadBuffer sprite_quads(renderer, num_sprites);
//	bb_quads.Begin();
//	bb_quads.AddSprite(0, v4::make(0.5f, 0.5f, 0.0f, 1.0f), 1.0f, 1.0f, Colour32Blue);
//	bb_quads.AddSprite(1, v4::make(1.0f, 1.0f, 0.0f, 1.0f), 1.0f, 1.0f, Colour32Green);
//	bb_quads.End();
//	bb_quads.m_model->SetMaterial(test_material);
//	
//	bb_instance.m_model = bb_quads.m_model;
//}
//sprite_instance.m_colour			= Coloure32White;
//sprite_instance.m_i2w.Identity();
