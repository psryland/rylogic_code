//*****************************************
//*****************************************
#include "test.h"
#include <string>
#include <vector>
#include <sstream>
#include <fstream>
#include <iostream>
#include <d3dx9.h>

using namespace pr;

namespace TestMisc
{
	inline void ZDiv(pr::v4& vec, float s) { vec = (s == 0.0f) ? pr::v4Origin : vec / s; }

	void Run()
	{
		float aspect = 1.0f;
		float fovY = pr::maths::pi_by_2;
		float Znear = 0.0f;
		float Zfar = 500.0f;

		pr::m4x4 c2w = pr::LookAt(pr::v4::make(2,0,2,1), pr::v4Origin, pr::v4YAxis);
		pr::m4x4 f2w = c2w * pr::Translation(0,0,-Zfar);
		//pr::m4x4 w2c = pr::GetInverseFast(c2w);
		pr::m4x4 w2f = pr::GetInverseFast(f2w);

		bool g_light_directional = true;
		pr::v4 ws_light_direction = pr::v4::normal3(-1.0f, 0.0f, 0.0f, 0.0f);
		pr::v4 ws_light_position  = pr::v4::make(-1,0,0,1);

		std::vector<pr::v4> points;
		//int const count = 50; float s = 0.0f;
		//points.push_back(pr::v4::make(0.2f, 2.6f, 6, 1));
		//for (int i = 0; i <= count; ++i, s = i/float(count))
		//	points.push_back(pr::v4::make(0, 0, -5.0f*(1.0f-s) + 10.0f*(s), 1.0f));
		//for (int i = 0; i <= count; ++i, s = i/float(count))
		//	points.push_back(pr::v4Random3(pr::v4::make(0.0f, 0.0f, 3.0f, 1.0f), 15.0f, 1.0f));
		//float k = 8.0f;
		for (float k = -1.0f; k <= 1.0f; k += 0.3f)
		for (float j = -1.0f; j <= 1.0f; j += 0.3f)
		for (float i = -1.0f; i <= 1.0f; i += 0.3f)
			points.push_back(ws_light_position + pr::v4::make(i,j,k,0));

		// world space view
		std::string str;
		pr::ldr::FrustumFA("view_volume", 0xFFFF0000,-3, fovY, aspect, Znear, Zfar, c2w, str);
		pr::ldr::BoxList("box", 0xFF00FF00, pr::v4::make(0.1f), &points[0], (int)points.size(), str);
		if (!g_light_directional) pr::ldr::Sphere("Light", 0xFFFFFFFF, ws_light_position, 0.2f, str);

		std::string smap_str;
		pr::ldr::Box("Smap", 0xFF0000FF, pr::v4Origin, pr::v4::make(2,2,0.001f,0), smap_str);
		pr::ldr::Box("Smap", 0xFF0000FF, pr::v4Origin, pr::v4::make(2/3.0f,2/3.0f,0.001f,0), smap_str);

		pr::Frustum frust = pr::Frustum::makeFA(fovY, aspect, Zfar);
		pr::v4 frust_dim = pr::v4::make(frust.Width(), frust.Height(), frust.ZDist(), 0);
		for (int p = 0; p != (int)points.size(); ++p)
		{
			pr::v4 const& pt = points[p];

			//VS*****************

			// find a ray in cs from the light_source passing through 'cs_pos'
			pr::v4 fs_pos0 = w2f * pt;
			pr::v4 fs_ray  = g_light_directional ? w2f * ws_light_direction : fs_pos0 - w2f * ws_light_position;
			pr::v4 fs_pos1 = fs_pos0 + fs_ray;
			if (Length3Sq(fs_ray) < pr::maths::tiny) continue;

			float t0 = 0.0f, t1 = 100000.0f;
			if (!Intersect(frust, fs_pos0, fs_pos1, t0, t1, true)) continue;

			fs_pos1 = fs_pos0 + t1 * fs_ray;
			pr::ldr::Line("ray", 0xFFFFFF00, f2w*(fs_pos0), f2w*(fs_pos1), str);
			
			// Output the distance from the intersection with the frustum to the nearest occluder
			// The depth test will be: "if this number is greater than mine then I'm in shadow"
			float dist = Length3(fs_pos1 - fs_pos0); (void)dist;

			// Map fs_pos1 to a smap texture coordinate
			const float zf_area_ratio = 1.0f / 3.0f;
			pr::v4 uv = fs_pos1;
			uv.x *= 2*zf_area_ratio / frust_dim.x;
			uv.y *= 2*zf_area_ratio / frust_dim.y;
			uv.z *=   zf_area_ratio / frust_dim.z;
			if (pr::Abs(uv.z) > 0.0005f)
			{
				float z = uv.z / (zf_area_ratio + 0.000001f);
				float a = (z/zf_area_ratio + 1 - z) / (1 - z);
				float b = (z + zf_area_ratio - z*zf_area_ratio);
				if (pr::Abs(uv.x) > pr::Abs(uv.y))
				{
					uv.x = pr::Sign(uv.x) * b;
					uv.y *= a;
				}
				else
				{
					uv.x *= a;
					uv.y = pr::Sign(uv.y) * b;
				}
				uv.z = 0.0f;
			}
			uv = uv.w1();
			PR_ASSERT(1, pr::Abs(uv.x) < 1.001f && pr::Abs(uv.y) < 1.001f);
			pr::ldr::Box("pt", 0xFFFFFF00, uv, 0.04f, smap_str);

			//VS*****************
		}
		pr::ldr::Write(str, "d:/deleteme/smap_test.ldr");
		pr::ldr::Write(smap_str, "d:/deleteme/smap_test_output.ldr");
	}
}