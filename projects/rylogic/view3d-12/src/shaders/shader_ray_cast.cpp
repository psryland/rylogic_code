//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#include "pr/view3d-12/shaders/shader_ray_cast.h"
#include "pr/view3d-12/render/drawlist_element.h"
#include "pr/view3d-12/utility/root_signature.h"
#include "view3d-12/src/shaders/common.h"

namespace pr::rdr12::shaders
{
	using namespace ray_cast;

	struct EReg
	{
		inline static constexpr auto CBufFrame = ECBufReg::b0;
		inline static constexpr auto CBufNugget = ECBufReg::b1;

		inline static constexpr auto Pose = ESRVReg::t4;
		inline static constexpr auto Skin = ESRVReg::t5;
	};
	
	RayCast::RayCast(ID3D12Device* device)
		:Shader()
	{
		// Create the root signature
		m_signature = RootSig(ERootSigFlags::VertGeomPixelOnly)
			.CBuf(EReg::CBufFrame)
			.CBuf(EReg::CBufNugget)
			.SRV(EReg::Pose, 1)
			.SRV(EReg::Skin, 1)
			.Create(device, "RayCastVertSig");
	}

	// Config the shader
	void RayCast::SetupFrame(ID3D12GraphicsCommandList* cmd_list, GpuUploadBuffer& upload, std::span<HitTestRay const> rays, ESnapMode snap_mode, float snap_distance)
	{
		if (rays.size() > _countof(CBufFrame::m_rays))
			throw std::runtime_error("Too many rays provided");

		CBufFrame cb0 = {};
		for (auto const& r : rays)
		{
			auto& ray = cb0.m_rays[&r - rays.data()];
			ray.ws_direction = r.m_ws_direction;
			ray.ws_origin = r.m_ws_origin;
		}
		cb0.m_ray_count = s_cast<int>(rays.size());
		cb0.m_snap_mode = s_cast<int>(snap_mode);
		cb0.m_snap_distance = snap_distance;
		auto gpu_address = upload.Add(cb0, D3D12_CONSTANT_BUFFER_DATA_PLACEMENT_ALIGNMENT, false);
		cmd_list->SetGraphicsRootConstantBufferView((UINT)ERootParam::CBufFrame, gpu_address);
	}
	void RayCast::SetupElement(ID3D12GraphicsCommandList* cmd_list, GpuUploadBuffer& upload, DrawListElement const* dle)
	{
		auto& inst = *dle->m_instance;
		auto& nug = *dle->m_nugget;

		CBufNugget cb1 = {};
		SetFlags(cb1, inst, nug, false);
		SetTxfm(cb1, inst, nug.m_model);

		cb1.m_inst_ptr = &inst;
		auto gpu_address = upload.Add(cb1, D3D12_CONSTANT_BUFFER_DATA_PLACEMENT_ALIGNMENT, false);
		cmd_list->SetGraphicsRootConstantBufferView((UINT)ERootParam::CBufNugget, gpu_address);
	}
}
