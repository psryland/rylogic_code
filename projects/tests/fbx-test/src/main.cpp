#include <fstream>
#include "pr/geometry/fbx.h"
#include "pr/geometry/p3d.h"
#include "pr/maths/bbox.h"

using namespace pr;
using namespace pr::geometry;

#if 0
// Helper for setting the DLL search path before main()
extern "C" void __cdecl InitDllDirectory()
{
	// Platform based on pointer size
	constexpr wchar_t const* platform =
		sizeof(void*) == 8 ? L"x64" :
		sizeof(void*) == 4 ? L"x86" :
		L"";

	// NDEBUG is unreliable. Seems it's not always defined in release
	#if defined(_DEBUG) || defined(DEBUG)
	constexpr wchar_t const* config = L"debug";
	#else
	constexpr wchar_t const* config = L"release";
	#endif

	OutputDebugStringA("InitDll Called"); // Sends it to the debugger output window

	__debugbreak();

	// This function runs very, very early � before global/static initializers
	if (!SetDllDirectoryW(std::wstring(L"lib\\").append(platform).append(L"\\").append(config).c_str()))
	{
		DWORD err = GetLastError();

		// Log the error somewhere safe
		// You can't use fancy logging here yet � CRT is not fully ready
		char buf[256];
		snprintf(buf, sizeof(buf), "SetDllDirectoryW failed with error %lu\n", err);

		OutputDebugStringA(buf); // Sends it to the debugger output window
		// (Optional) printf to console if available
		// printf("%s", buf);
	}
}

// Force the linker to keep the symbol, even if it thinks it's unused
#ifdef _M_X64
#pragma comment(linker, "/include:InitDllDirectory")
#else
#pragma comment(linker, "/include:_InitDllDirectory")
#endif

// Mark the function to run during CRT init
#pragma section(".CRT$XCT", read)
__declspec(allocate(".CRT$XCT")) void (__cdecl* pInitDllDirectory)(void) = &InitDllDirectory;
#endif

int main()
{
	//std::filesystem::path ifilepath = "E:\\Dump\\biplane.fbx";
	//std::filesystem::path ifilepath = "E:\\Dump\\Hyperpose\\AJ-99.fbx";
	std::filesystem::path ifilepath = "E:\\Rylogic\\Code\\art\\models\\pendulum\\pendulum.fbx";
	//std::filesystem::path ofilepath = "E:\\Dump\\Hyperpose\\fbx-round-trip.fbx";
	//std::filesystem::path dfilepath = "E:\\Dump\\Hyperpose\\fbx-dump.txt";
	std::filesystem::path p3doutpath = "E:\\Dump\\model.p3d";

	std::ifstream ifile(ifilepath, std::ios::binary);
	//std::ofstream ofile(ofilepath, std::ios::binary);
	//std::ofstream dfile(dfilepath);

	//dll.Fbx_RoundTripTest(ifile, ofile);
	//dll.Fbx_DumpStream(ifile, dfile);
	fbx::Scene fbxscene(ifile, { .m_parts = fbx::EParts::ModelOnly });

	// Convert the models the p3d
	p3d::File file = {};
	{
		// Materials
		for (auto const& [unique_id, mat] : fbxscene.m_materials)
		{
			p3d::Material material;
			material.m_id = std::format("mat-{}", unique_id);
			material.m_diffuse = mat.m_diffuse;
			file.m_scene.m_materials.push_back(material);
		}

		// Models
		for (fbx::Mesh const& mesh : fbxscene.m_meshes)
		{
			// Ignoring 'mesh.m_level'
			p3d::Mesh m;
			m.m_name = mesh.m_name;
			m.m_bbox = mesh.m_bbox;
			m.m_o2p = mesh.m_o2p;
			for (auto& v : mesh.m_vbuf)
			{
				m.add_vert({ v.m_vert, v.m_colr, v.m_norm, v.m_tex0 });
			}
			for (auto& n : mesh.m_nbuf)
			{
				p3d::Nugget nugget{ n.m_topo, n.m_geom, std::format("mat-{}", n.m_mat_id) };
				nugget.m_vidx.append<int>(std::span{ mesh.m_ibuf }.subspan(n.m_irange.begin(), n.m_irange.size()));
				m.add_nugget(nugget);
			}
			file.m_scene.m_meshes.push_back(std::move(m));
		}
	}

	if (std::ofstream ofile(p3doutpath, std::ios::binary); ofile)
		p3d::Write(ofile, file);

	return 0;
}
