//*****************************************
//*****************************************
#include "test.h"
#include "pr/common/testbed3d.h"

namespace pr
{
	PR_RDR_DECLARE_INSTANCE_TYPE3
	(
		TestInstance, 
			rdr::Model*,	m_model,				ECpt_ModelPtr,
			m4x4,			m_instance_to_world,	ECpt_I2WTransform,
			Colour32,		m_colour,				ECpt_TintColour32
	);
}//namespace pr

const char scene_script[] =
"										\
*Window									\
{										\
	*Bounds 0 0 900 900					\
	*ClientArea 0 0 900 900				\
	*BackColour FF3000A0				\
}										\
*Viewport								\
{										\
	*Rect 0.0 0.0 1.0 1.0				\
}										\
*Camera									\
{										\
	*Position 0 0 10					\
	*LookAt 0 0 0						\
	*Up 0 1 0							\
	*NearPlane 0.1						\
	*FarPlane 100.0						\
	*FOV 0.785398						\
	*Aspect 1							\
}										\
*CameraController						\
{										\
	*Keyboard							\
	*LinAccel 0.2						\
	*MaxLinVel 1000.0					\
	*RotAccel 0.03						\
	*MaxRotVel 20.0						\
	*Scale 1							\
}										\
*Light									\
{										\
	*Ambient 0.1 0.1 0.1 1.0			\
	*Diffuse 1.0 1.0 1.0 1.0			\
	*Specular 0.2 0.2 0.2 1.0			\
	*SpecularPower 100.0				\
	*Direction -1.0 -2.0 -2.0			\
}										\
*Light									\
{										\
	*Ambient 0.1 0.1 0.1 1.0			\
	*Diffuse 1.0 0.0 0.0 1.0			\
	*Specular 0.2 0.2 0.2 1.0			\
	*SpecularPower 100.0				\
	*Direction 1.0 -2.0 2.0				\
}										\
";

namespace TestTestBed3d
{
	using namespace pr;

	void Run()
	{
		TestBed3d tb(scene_script);
		TestInstance inst1;
		inst1.m_instance_to_world.identity();
		inst1.m_model = tb.CreateModel(
			geom::unit_cube::num_vertices,
			geom::unit_cube::vertices,
			geom::unit_cube::num_indices,
			geom::unit_cube::indices,
			m4x4Identity);

		tb.AddInstance(inst1.m_base);
		while( GetAsyncKeyState(VK_ESCAPE) == 0 )
		{
			tb.ReadInput();
			tb.Present();

			//v4 cam_pos = tb.GetCamera().GetPosition();
			//float cam_scale = tb.GetCameraController().GetSettings().m_scale;
			//printf("Camera Pos: %f %f %f    Scale %f\r", cam_pos.x, cam_pos.y, cam_pos.z, cam_scale);
		}
		tb.RemoveInstance(inst1.m_base);

		_getch();
	}
}//namespace TestTestBed3d
