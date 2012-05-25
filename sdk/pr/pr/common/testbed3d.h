//******************************************************************************
//******************************************************************************
// Command line 3d test app base
// Example Usage:
//	namespace pr
//	{
//		PR_RDR_DECLARE_INSTANCE_TYPE2
//		(
//			TestInstance, 
//				rdr::ModelPtr,					m_model,				EComponent_ModelPtr,
//				m4x4,							m_instance_to_world,	EComponent_I2WTransform
//		);
//	}//namespace pr
//
//	TestInstance inst;
//	{
//		using namespace pr::geometry;
//		inst.m_model = CreateModel(unit_cube::num_vertices, unit_cube::vertices, unit_cube::num_indices, unit_cube::indices, m4x4Identity);
//		inst.m_instance_to_world = m4x4Identity;
//	}
//
//	const char scene_script[] =
//	"										\
//	*Window									\
//	{										\
//		*Bounds 0 0 900 900					\
//		*ClientArea 0 0 900 900				\
//		*BackColour FF3000A0				\
//	}										\
//	*Viewport								\
//	{										\
//		*Rect 0.0 0.0 1.0 1.0				\
//	}										\
//	*Camera									\
//	{										\
//		*Position 0 0 10					\
//		*LookAt 0 0 0						\
//		*Up 0 1 0							\
//		*NearPlane 0.1						\
//		*FarPlane 100.0						\
//		*FOV 0.785398						\
//		*Aspect 1							\
//	}										\
//	*CameraController						\
//	{										\
//		*Keyboard							\
//		*LinAccel 0.2						\
//		*MaxLinVel 1000.0					\
//		*RotAccel 0.03						\
//		*MaxRotVel 20.0						\
//		*Scale 1							\
//	}										\
//	*Light									\
//	{										\
//		*Ambient 0.1 0.1 0.1 1.0			\
//		*Diffuse 1.0 1.0 1.0 1.0			\
//		*Specular 0.2 0.2 0.2 1.0			\
//		*SpecularPower 100.0				\
//		*Direction -1.0 -2.0 -2.0			\
//	}										\
//	*Light									\
//	{										\
//		*Ambient 0.1 0.1 0.1 1.0			\
//		*Diffuse 1.0 0.0 0.0 1.0			\
//		*Specular 0.2 0.2 0.2 1.0			\
//		*SpecularPower 100.0				\
//		*Direction 1.0 -2.0 2.0				\
//	}										\
//	";
//
//	void main()
//	{
//		TestBed3d tb(scene_script, sizeof(scene_script));
//		TestInstance inst1;
//		inst1.m_instance_to_world.Identity();
//		inst1.m_model = tb.CreateModel(
//			geometry::unit_cube::num_vertices,
//			geometry::unit_cube::vertices,
//			geometry::unit_cube::num_indices,
//			geometry::unit_cube::indices,
//			m4x4Identity);
//		tb.AddInstance(inst1);
//		while( GetAsyncKeyState(VK_ESCAPE) == 0 )
//		{
//			tb.ReadInput();
//			tb.Present();
//		}
//		tb.RemoveInstance(inst1);
//	}

#ifndef PR_TESTBED_3D_H
#define PR_TESTBED_3D_H

// Defines
// ;NOMINMAX ;_WIN32_WINNT=0x0500

#include <signal.h>
#include <windows.h>
#include "pr/maths/maths.h"
#include "pr/common/scriptreader.h"
#include "pr/str/prstring.h"
#include "pr/renderer/renderer.h"
#include "pr/geometry/primitive.h"
#include "pr/storage/xfile/xfile.h"
#include "pr/geometry/mesh_tools.h"
#include "pr/geometry/optimise_mesh.h"
#include "pr/geometry/geometry.h"
//#include "pr/camera/camera.h"
//#include "pr/camera/icameracontroller.h"
//#include "pr/camera/ccmousefreecamera.h"
//#include "pr/camera/cckeyboardfreecamera2.h"

namespace pr
{
	namespace testbed3d
	{
		enum EFlags
		{
			EFlags_GenerateNormals	= 1 << 0,
			EFlags_OptimiseMesh		= 1 << 1,
			EFlags_IgnoreTexture	= 1 << 2,
			EFlags_IgnoreColour		= 1 << 3,
			EFlags_IgnoreMaterials	= 1 << 4
		};

	}//namespace testbed3d

	class TestBed3d
	{
		rdr::Allocator				m_allocator;
		Renderer					m_renderer;
		rdr::Viewport				m_viewport;
		//Camera						m_camera;
		//ICameraController*			m_camera_controller;
		std::size_t					m_last_tick;

	public:
		TestBed3d(const char* scene_script = 0)
		:m_allocator()
		,m_renderer(GetRendererSettings(scene_script, m_allocator))
		,m_viewport(GetViewportSettings(scene_script, m_renderer))
		//,m_camera(GetCameraSettings(scene_script))
		//,m_camera_controller(0)
		,m_last_tick(GetTickCount())
		{
			CreateCameraController();
			SetConsoleCtrlHandler(CtrlCHandler, TRUE);
			//m_viewport.SetWorldToCamera(m_camera.GetWorldToCamera());
			CreateLight();
		}
		~TestBed3d()
		{
			//delete m_camera_controller;
			SetConsoleCtrlHandler(CtrlCHandler, FALSE);
		}

		// Read the keys/mouse.
		void ReadInput()
		{
			std::size_t now = static_cast<std::size_t>(GetTickCount());
			//float elapsed_seconds = Clamp<float>((now - m_last_tick) / 1000.0f, 0.0f, 1.0f);
			m_last_tick = now;
			//m_camera_controller->Step(elapsed_seconds);
		}
		
		// Draw the scene
		void Present()
		{
			if( Succeeded(m_renderer.RenderStart()) )
			{
				//m_viewport.SetWorldToCamera (m_camera.GetWorldToCamera());
				//m_viewport.SetCameraToScreen(m_camera.GetCameraToScreen());
				//m_viewport.SetViewFrustum	(m_camera.GetViewFrustum());

				m_viewport.Render();
				m_renderer.RenderEnd();
				m_renderer.Present();
			}
		}

		// Initialise and add an instance to the scene
		void AddInstance(pr::rdr::instance::Base& inst_base)
		{
			m_viewport.AddInstance(inst_base);
		}
		template <typename InstType> void AddInstance(InstType& inst)
		{
			AddInstance(inst.m_base);
		}

		// Remove an instance to the scene
		void RemoveInstance(pr::rdr::instance::Base& inst_base)
		{
			m_viewport.RemoveInstance(inst_base);
		}
		template <typename InstType> void RemoveInstance(InstType& inst)
		{
			RemoveInstance(inst.m_base);
		}

		// Create a model
		pr::rdr::ModelPtr CreateModel(std::size_t num_vertices, pr::Vert const* vertices, std::size_t num_indices, const rdr::Index* indices, const m4x4& transform)
		{
			using namespace rdr;
			
			model::Settings settings;
			settings.m_vertex_type = vf::EType_PosNormDiffTex;
			settings.m_Vcount      = (uint)num_vertices;
			settings.m_Icount      = (uint)num_indices;
			ModelPtr model = m_renderer.m_model_manager.CreateModel(settings);

			model::VLock vlock;
			vf::iterator vb = model->LockVBuffer(vlock);
			for (std::size_t i = 0; i != num_vertices; ++i, ++vb) 
				vb->set(transform * vertices[i]);

			model::ILock ilock;
			rdr::Index* ib = model->LockIBuffer(ilock);
			for (std::size_t i = 0; i != num_indices; ++i, ++ib)
				*ib = indices[i];

			rdr::Material mat = m_renderer.m_material_manager.GetMaterial(pr::geom::EVNCT);
			model->SetMaterial(mat, model::EPrimitive::TriangleList, false);

			return model;
		}

		// Create a model from an xfile
		rdr::ModelPtr CreateModel(const char* xfile_filename, int frame_number = 0, std::size_t flags = 0) // testbed3d::EFlags
		{
			Geometry xfile_geometry;
			if( Failed(pr::xfile::Load(xfile_filename, xfile_geometry)) ) { printf("Failed to load X File: %s\n", xfile_filename); return 0; }
			Frame& frame = xfile_geometry.m_frame[frame_number];
			
			// If the first normal is Zero then generate normals for the mesh
			if ((flags & testbed3d::EFlags_GenerateNormals) || ((frame.m_mesh.m_geom_type & geom::ENormal) && IsZero3(frame.m_mesh.m_vertex[0].m_normal)))
			{
				geometry::GenerateNormals(frame.m_mesh);
			}
			if (flags & testbed3d::EFlags_OptimiseMesh    ) { pr::geometry::OptimiseMesh(frame.m_mesh); }
			if (flags & testbed3d::EFlags_IgnoreColour    ) { frame.m_mesh.m_geom_type &= ~geom::EColour; }
			if (flags & testbed3d::EFlags_IgnoreTexture   ) { frame.m_mesh.m_geom_type &= ~geom::ETexture; }
			if (flags & testbed3d::EFlags_IgnoreMaterials ) { frame.m_mesh.m_material.clear(); }

			// Load the model
			rdr::ModelPtr model = rdr::LoadMesh(m_renderer, frame.m_mesh);
			rdr::Material mat = m_renderer.m_material_manager.GetMaterial(frame.m_mesh.m_geom_type);
			model->SetMaterial(mat, pr::rdr::model::EPrimitive::TriangleList, false);

			return model;
		}

		//// Load a model package
		//void LoadPackage(const char* package_filename)
		//{
		//	nugget::Nugget package;
		//	//nugget::SingleNuggetReceiver nug_receiver(&package);
		//	nugget::BackInserter<nugget::Nugget*> nug_receiver(&package);
		//	if( pr::Failed(pr::nugget::Load(package_filename, nugget::ECopyFlag_Reference, nug_receiver)) ) { printf("Failed to load package nugget file"); return; }
		//	if( pr::Failed(pr::rdr::LoadPackage(m_renderer, package)) ) { printf("Failed to register package with renderer"); return; }
		//}

		//// Allow playing with the camera
		//Camera&				GetCamera()				{ return m_camera; }
		//ICameraController&	GetCameraController()	{ return *m_camera_controller; }

	private:
		TestBed3d(const TestBed3d&);
		TestBed3d& operator =(const TestBed3d&);

		// Get the configuration to use for the renderer
		static rdr::RdrSettings GetRendererSettings(char const* scene_script, rdr::Allocator& allocator)
		{
			rdr::RdrSettings settings;
			settings.m_window_handle      = GetConsoleWindow();
			settings.m_device_config      = rdr::GetDefaultDeviceConfigWindowed();
			settings.m_allocator          = &allocator;
			settings.m_client_area        = IRect::make(0, 0, 800, 800);
			settings.m_background_colour  = 0xFF0000A0;

			pr::script::Reader rdr;
			rdr.AddString(scene_script);
			if (rdr.FindKeyword("Window"))
			{
				std::string kw;
				rdr.SectionStart();
				while (rdr.GetKeyword(kw))
				{
					if (pr::str::EqualI(kw, "ClientArea")) { rdr.ExtractIntArray((int*)&settings.m_client_area, 4, 10); continue; }
					if (pr::str::EqualI(kw, "BackColour")) { rdr.ExtractInt(settings.m_background_colour.m_aarrggbb, 16); continue; }
				}
				rdr.SectionEnd();
			}

			return settings;
		}

		// Get the configuration to use for the viewport
		static rdr::VPSettings GetViewportSettings(char const* /*scene_script*/, Renderer& renderer)
		{
			rdr::VPSettings settings;
			settings.m_renderer   = &renderer;
			settings.m_identifier = 0;

			//ScriptLoader section; uint start = 0;
			//if (Succeeded(scene_script.GetSection("Viewport", &start, section)))
			//{
			//	uint begin = 0;
			//	if( section.FindKeyword("Rect", &begin) )		{ section.ExtractFloatArray((float*)&settings.m_viewport_rect, 4); }
			//}

			return settings;
		}

		//// Get the configuration to use for the camera
		//static CameraSettings GetCameraSettings(ScriptLoader& scene_script)
		//{
		//	CameraSettings settings;
		//	settings.m_3dcamera					= true;
		//	settings.m_righthanded				= true;
		//	settings.m_use_FOV_for_perspective	= true;
		//	settings.m_position					.set(0.0f, 0.0f, 10.0f, 1.0f);
		//	settings.m_orientation				.identity();
		//	settings.m_near						= 0.01f;
		//	settings.m_far						= 100.0f;
		//	settings.m_fov						= maths::pi / 4.0f;
		//	settings.m_aspect					= 1.0f;

		//	ScriptLoader section; uint start = 0;
		//	if( Succeeded(scene_script.GetSection("Camera", &start, section)) )
		//	{
		//		uint begin = 0;
		//		v4 up = v4YAxis;
		//		v4 target = v4Origin;
		//		if( section.FindKeyword("Position", &begin) )	{ section.ExtractVector3(settings.m_position, 1.0f); }
		//		if( section.FindKeyword("FOV", &begin) )		{ section.ExtractFloat(settings.m_fov); }
		//		if( section.FindKeyword("NearPlane", &begin) )	{ section.ExtractFloat(settings.m_near); }
		//		if( section.FindKeyword("FarPlane", &begin) )	{ section.ExtractFloat(settings.m_far); }
		//		if( section.FindKeyword("Aspect", &begin) )		{ section.ExtractFloat(settings.m_aspect); }
		//		if( section.FindKeyword("Up", &begin) )			{ section.ExtractVector3(up, 0.0f); }
		//		if( section.FindKeyword("LookAt", &begin) )		{ section.ExtractVector3(target, 0.0f); settings.m_orientation.set(LookAt(settings.m_position, target, up)); }
		//	}

		//	return settings;
		//}

		// Create an object to drive the camera
		void CreateCameraController()
		{
			//CameraControllerSettings settings;
			//settings.m_camera					= &m_camera;
			//settings.m_window_handle			= GetConsoleWindow();
			//settings.m_app_instance				= 0;
			//settings.m_linear_acceleration		= 0.2f;
			//settings.m_max_linear_velocity		= 1000.0f;
			//settings.m_rotational_acceleration	= 0.03f;
			//settings.m_max_rotational_velocity	= 20.0f;
			//settings.m_scale					= 1.0f;

			//bool mouse_driven = false;
			//ScriptLoader section; uint start = 0;
			//if( Succeeded(m_scene_script.GetSection("CameraController", &start, section)) )
			//{
			//	uint begin = 0;
			//	if( section.FindKeyword("Mouse", &begin) )		{ mouse_driven = true; }
			//	if( section.FindKeyword("LinAccel", &begin) )	{ section.ExtractFloat(settings.m_linear_acceleration); }
			//	if( section.FindKeyword("MaxLinVel", &begin) )	{ section.ExtractFloat(settings.m_max_linear_velocity); }
			//	if( section.FindKeyword("RotAccel", &begin) )	{ section.ExtractFloat(settings.m_rotational_acceleration); }
			//	if( section.FindKeyword("MaxRotVel", &begin) )	{ section.ExtractFloat(settings.m_max_rotational_velocity); }
			//	if( section.FindKeyword("Scale", &begin) )		{ section.ExtractFloat(settings.m_scale); }
			//}
			//
			//if( mouse_driven )	{ m_camera_controller = new camera::MouseFreeCamera(settings); }
			//else				{ m_camera_controller = new camera::KeyboardFreeCamera_2(settings); }
		}

		// Initialise a light for the scene
		void CreateLight()
		{
			//bool make_one_light = !m_scene_script.IsLoaded();

			//int light_count = 0;
			//ScriptLoader section; std::size_t from = 0;
			//while( Succeeded(m_scene_script.GetSection("Light", &from, section)) || make_one_light )
			//{
			//	rdr::Light& light		= m_renderer.m_lighting_manager.m_light[light_count++];
			//	light.m_ambient			= Colour::make(0.1f, 0.1f, 0.1f, 1.0f);
			//	light.m_diffuse			= Colour::make(1.0f, 1.0f, 1.0f, 1.0f);
			//	light.m_specular		= Colour::make(0.2f, 0.2f, 0.2f, 1.0f);
			//	light.m_specular_power	= 100.0f;		
			//	light.m_direction		= -GetNormal3(v4XAxis + v4YAxis + v4ZAxis);
			//	light.m_type           = pr::rdr::ELight::Directional;
			//	light.m_on             = true;

			//	uint begin = 0;
			//	if( section.FindKeyword("Ambient", &begin) )		{ section.ExtractFloatArray((float*)&light.m_ambient, 4); }
			//	if( section.FindKeyword("Diffuse", &begin) )		{ section.ExtractFloatArray((float*)&light.m_diffuse, 4); }
			//	if( section.FindKeyword("Specular", &begin) )		{ section.ExtractFloatArray((float*)&light.m_specular, 4); }
			//	if( section.FindKeyword("SpecularPower", &begin) )	{ section.ExtractFloat(light.m_specular_power); }
			//	if( section.FindKeyword("Direction", &begin) )		{ section.ExtractVector3(light.m_direction, 0.0f); Normalise3(light.m_direction); }

			//	make_one_light = false;
			//}
		}

		// Ctrl-C signal handler
		static BOOL WINAPI CtrlCHandler(DWORD) { return TRUE; }
	};
}//namespace pr

#endif//PR_TESTBED_3D_H
