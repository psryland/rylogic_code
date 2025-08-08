//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#include "view3d-12/src/render/render_raycast.h"
#include "pr/view3d-12/main/renderer.h"
#include "pr/view3d-12/scene/scene.h"
#include "pr/view3d-12/instance/instance.h"
#include "pr/view3d-12/model/vertex_layout.h"
#include "pr/view3d-12/shaders/shader.h"
#include "pr/view3d-12/shaders/shader_ray_cast.h"
#include "pr/view3d-12/shaders/shader_registers.h"
#include "pr/view3d-12/resource/resource_factory.h"
#include "pr/view3d-12/utility/barrier_batch.h"
#include "pr/view3d-12/utility/pipe_state.h"
#include "pr/view3d-12/utility/wrappers.h"
#include "view3d-12/src/shaders/common.h"

namespace pr::rdr12
{
	namespace shaders
	{
		#include "view3d-12/src/shaders/hlsl/types.hlsli"

		namespace ray_cast
		{
			#include "view3d-12/src/shaders/hlsl/utility/ray_cast_cbuf.hlsli"
			static_assert((sizeof(CBufFrame) % 16) == 0);
			static_assert((sizeof(CBufNugget) % 16) == 0);
		}
	}

	using Intercept = shaders::ray_cast::Intercept;
	constexpr int MaxRays = shaders::ray_cast::MaxRays;
	constexpr int MaxIntercepts = shaders::ray_cast::MaxIntercepts;

	// Stream output stage buffer format
	static const StreamOutputDesc so_desc = StreamOutputDesc{}
		.add_buffer(sizeof(Intercept))
		.add_entry({ 0, "WSIntercept", 0, 0, 4, 0 })
		.add_entry({ 0, "SnapType", 0, 0, 1, 0 })
		.add_entry({ 0, "RayIndex", 0, 0, 1, 0 })
		.add_entry({ 0, "InstPtr", 0, 0, 2, 0 })
		.no_raster();

	RenderRayCast::RenderRayCast(Scene& scene, bool continuous)
		: RenderStep(Id, scene)
		, m_rays()
		, m_snap_distance(1.0f)
		, m_snap_mode(ESnapMode::NoSnap)
		, m_flags(EHitTestFlags::Verts | EHitTestFlags::Edges | EHitTestFlags::Faces)
		, m_include([](auto){ return true; })
		, m_cmd_list(scene.d3d(), nullptr, "RenderRayCast", EColours::BlanchedAlmond)
		, m_gsync()
		, m_shader_vert(scene.d3d())
		, m_shader_edge(scene.d3d())
		, m_shader_face(scene.d3d())
		, m_shader(&m_shader_vert)
		, m_results()
		, m_results_uav()
		, m_readback(m_gsync, MaxRays * MaxIntercepts * sizeof(Intercept)) // Todo: checkme
		, m_output()
		, m_continuous(continuous)
	{
		// Create a default PSO description
		m_default_pipe_state = D3D12_GRAPHICS_PIPELINE_STATE_DESC{
			.pRootSignature = m_shader_vert.m_signature.get(),
			.VS = m_shader->m_code.VS,
			.PS = {}, // No pixel shader when using stream output
			.DS = m_shader->m_code.DS,
			.HS = m_shader->m_code.HS,
			.GS = m_shader->m_code.GS,
			.StreamOutput = so_desc,
			.BlendState = BlendStateDesc{},
			.SampleMask = UINT_MAX,
			.RasterizerState = RasterStateDesc{},
			.DepthStencilState = DepthStateDesc{},
			.InputLayout = Vert::LayoutDesc(),
			.IBStripCutValue = D3D12_INDEX_BUFFER_STRIP_CUT_VALUE_DISABLED,
			.PrimitiveTopologyType = D3D12_PRIMITIVE_TOPOLOGY_TYPE_TRIANGLE,
			.NumRenderTargets = 0U, // No render targets for stream output
			.RTVFormats = {}, // Empty for stream output
			.DSVFormat = DXGI_FORMAT_UNKNOWN,// No depth for stream output
			.SampleDesc = {1, 0},
			.NodeMask = 0U,
			.CachedPSO = {},
			.Flags = D3D12_PIPELINE_STATE_FLAG_NONE,
		};

		// Set up the output buffers
		ResourceFactory factory(rdr());
		ResDesc rdesc = ResDesc::Buf<Intercept>(MaxIntercepts, {}).usage(EUsage::UnorderedAccess).def_state(D3D12_RESOURCE_STATE_STREAM_OUT);
		m_results = factory.CreateResource(rdesc, "RayCastIntercepts");

		// Create a descriptor for the result buffer
		ResourceStore::Access store(rdr());
		D3D12_UNORDERED_ACCESS_VIEW_DESC uav_desc = {
			.Format = DXGI_FORMAT_R32_TYPELESS,
			.ViewDimension = D3D12_UAV_DIMENSION::D3D12_UAV_DIMENSION_BUFFER,
			.Buffer = D3D12_BUFFER_UAV{
				.FirstElement = 0,
				.NumElements = s_cast<UINT>(MaxIntercepts),
				.StructureByteStride = sizeof(Intercept),
				.CounterOffsetInBytes = 0,
				.Flags = D3D12_BUFFER_UAV_FLAGS::D3D12_BUFFER_UAV_FLAG_NONE,
			},
		};
		m_results_uav = store.Descriptors().Create(m_results.get(), uav_desc);
	}

	// Set the rays to cast.
	void RenderRayCast::SetRays(std::span<HitTestRay const> rays, float snap_distance, EHitTestFlags flags, RayCastFilter include)
	{
		// Save the rays so we can match ray indices to the actual ray.
		m_rays = rays.subspan(0, std::min<size_t>(rays.size(), MaxRays));
		m_snap_distance = snap_distance;
		m_flags = flags;
		m_include = include;
	}

	// Add model nuggets to the draw list for this render step
	void RenderRayCast::AddNuggets(BaseInstance const& inst, TNuggetChain const& nuggets, drawlist_t& drawlist)
	{
		// Ignore instances that are filtered out
		if (!m_include(&inst))
			return;

		drawlist.reserve(drawlist.size() + nuggets.size());
		for (auto& nug : nuggets)
		{
			// Ignore if flagged as not visible
			if (AllSet(nug.m_nflags, ENuggetFlag::Hidden))
				continue;

			// Add an element to the draw list
			DrawListElement dle = {
				.m_sort_key = nug.m_sort_key,
				.m_nugget = &nug,
				.m_instance = &inst,
			};
			drawlist.push_back(dle);
			m_sort_needed = true;

			// Recursively add dependent nuggets
			if (!nug.m_nuggets.empty())
				AddNuggets(inst, nug.m_nuggets, drawlist);
		}
	}

	// Perform the ray cast and read the results
	std::future<void> RenderRayCast::ExecuteImmediate(RayCastResultsOut cb)
	{
		m_cmd_list.Reset(wnd().m_cmd_alloc_pool.Get());
		auto output = ExecuteCore();
		m_cmd_list.Close();

		pix::BeginEvent(rdr().GfxQueue(), s_cast<uint32_t>(EColours::LightGreen), "Immediate Ray Cast");

		// Execute the command list
		rdr().ExecuteGfxCommandLists({ m_cmd_list });

		// Add a sync point and wait for it
		auto sync_point = m_gsync.AddSyncPoint(rdr().GfxQueue());
		m_cmd_list.SyncPoint(sync_point);

		pix::EndEvent(rdr().GfxQueue());

		// Return a future that will process the results after GPU completion
		return std::async(std::launch::deferred, [this, output = std::move(output), sync_point, cb]() mutable
		{
			// Wait for GPU to complete
			m_gsync.Wait(sync_point);

			// Read the values out of the buffer
			// Look for the sentinel to indicate the length
			auto intercepts = output.span<Intercept>();
			intercepts = intercepts.subspan(0, index_if(intercepts, [](auto& i) { return i.inst_ptr == nullptr; }));

			// Returns the squared distance from the ray
			auto DistSqFromRay = [](HitTestRay const& ray, Intercept const& intercept)
			{
				return DistanceSq_PointToInfiniteLine(intercept.ws_intercept.w1(), ray.m_ws_origin, ray.m_ws_direction);
			};
			constexpr auto Eql = [](Intercept const& l, Intercept const& r)
			{
				return
					l.ws_intercept == r.ws_intercept &&
					l.inst_ptr == r.inst_ptr &&
					l.ray_index == r.ray_index;
			};

			// Sort the intercepts from nearest to furtherest.
			// This is a bit of a fuzzy ordering because of snapping.
			sort(intercepts, [&](Intercept const& l, Intercept const& r)
			{
				// Cases:
				//  - If either intercept is a face snap, then sort by distance
				//    because faces should occlude any intercepts behind them.
				//  - Otherwise, sort by distance if the difference is greater
				//    than the snap distance.
				//  - If two intercepts are within the snap distance:
				//    - Sort by closest to the ray.

				// If one of the intercepts is a face snap
				if ((ESnapType)l.snap_type == ESnapType::Face ||
					(ESnapType)r.snap_type == ESnapType::Face)
				{
					// At least one of the intercepts is a face, sort by distance
					if (Abs(l.ws_intercept.w - r.ws_intercept.w) > maths::tinyf)
						return l.ws_intercept.w < r.ws_intercept.w;

					// If the intercepts are at the same distance, prioritise by snap type
					// (Remember face snap have zero distance from the ray)
					return l.snap_type < r.snap_type;
				}

				// Neither intercept is a face snap. Sort roughly by distance first
				if (Abs(l.ws_intercept.w - r.ws_intercept.w) > m_snap_distance)
					return l.ws_intercept.w < r.ws_intercept.w;

				// If one of the intercepts is an edge snap
				if ((ESnapType)l.snap_type == ESnapType::Edge ||
					(ESnapType)r.snap_type == ESnapType::Edge)
				{
					// If one of the intercepts is a point snap, it has priority
					if (l.snap_type != r.snap_type)
						return l.snap_type < r.snap_type;

					// Sort by distance of the intercepts from the ray
					auto dist_l = DistSqFromRay(m_rays[l.ray_index], l);
					auto dist_r = DistSqFromRay(m_rays[r.ray_index], r);
					return dist_l < dist_r;
				}

				// The intercepts are point snaps. Sort by distance from the ray
				auto dist_l = DistSqFromRay(m_rays[l.ray_index], l);
				auto dist_r = DistSqFromRay(m_rays[r.ray_index], r);
				return dist_l < dist_r;
			});

			// Forward each unique intercept to the callback
			for (int i = 0, iend = isize(intercepts); i != iend; )
			{
				auto& intercept = intercepts[i];

				// Forward the hits to the callback
				HitTestResult result = {
					.m_ws_origin = m_rays[intercept.ray_index].m_ws_origin,
					.m_ws_direction = m_rays[intercept.ray_index].m_ws_direction,
					.m_ws_intercept = intercept.ws_intercept.w1(),
					.m_instance = type_ptr<BaseInstance>(intercept.inst_ptr),
					.m_distance = intercept.ws_intercept.w,
					.m_ray_index = intercept.ray_index,
					.m_snap_type = static_cast<ESnapType>(intercept.snap_type),
				};
				if (!cb(result))
				{
					return;
				}

				// Skip duplicates
				for (++i; i != iend && Eql(intercepts[i], intercept); ++i) {}
			}
		});
	}

	// Perform the render step
	void RenderRayCast::Execute(Frame& frame)
	{
		m_cmd_list.Reset(frame.m_cmd_alloc_pool.Get());

		// Add the command lists we're using to the frame.
		frame.m_main.push_back(m_cmd_list);

		m_output = ExecuteCore();

		// Commands complete
		m_cmd_list.Close();

		// need to process the 'output' like in ExecuteImmediate
		throw std::runtime_error("not implemented");
	}

	// Step up the GPU call for the ray cast
	GpuTransferAllocation RenderRayCast::ExecuteCore()
	{
		// Assumes the cmd list is ready to go
		GpuTransferAllocation output = {};

		// Sort the draw list if needed
		SortIfNeeded();

		// Set stream output targets
		D3D12_STREAM_OUTPUT_BUFFER_VIEW so_view = {
			.BufferLocation = m_results->GetGPUVirtualAddress(),
			.SizeInBytes = MaxIntercepts * sizeof(Intercept),
			.BufferFilledSizeLocation = 0, // For tracking how much was written
		};
		m_cmd_list.SOSetTargets(0, { &so_view, 1 });

		// Set the signature for the shader used for this nugget
		m_cmd_list.SetGraphicsRootSignature(m_shader->m_signature.get());

		// Configure the shader constants
		m_shader->SetupFrame(m_cmd_list.get(), m_upload_buffer, m_rays, m_snap_mode, m_snap_distance);

		BarrierBatch barriers(m_cmd_list);

		// Zero the results buffer
		{
			barriers.Transition(m_results.get(), D3D12_RESOURCE_STATE_UNORDERED_ACCESS);
			barriers.Commit();

			auto gpu = wnd().m_heap_view.Add(m_results_uav);
			m_cmd_list.ClearUnorderedAccessViewFloat(gpu, m_results_uav.m_cpu, m_results.get(), v4::Zero().arr);

			barriers.Transition(m_results.get(), D3D12_RESOURCE_STATE_STREAM_OUT);
			barriers.Commit();
		}

		// Apply ray cast to each object
		Lock lock(*this);
		for (auto& dle : lock.drawlist())
		{
			auto const& nugget = *dle.m_nugget;
			auto const& instance = *dle.m_instance;
			auto desc = m_default_pipe_state;
			(void)instance; // todo: Skinned instances?

			// Configure the shader for this element
			m_shader->SetupElement(m_cmd_list.get(), m_upload_buffer, &dle);

			// Draw the nugget **** 
			DrawNugget(nugget, desc);
		}

		// Copy results back to the CPU
		{
			barriers.Transition(m_results.get(), D3D12_RESOURCE_STATE_COPY_SOURCE);
			barriers.Commit();

			output = m_readback.Alloc<Intercept>(MaxIntercepts);
			m_cmd_list.CopyBufferRegion(output, m_results.get(), 0);

			barriers.Transition(m_results.get(), D3D12_RESOURCE_STATE_STREAM_OUT);
			barriers.Commit();
		}

		return output;
	}

	// Draw a single nugget
	void RenderRayCast::DrawNugget(Nugget const& nugget, PipeStateDesc& desc)
	{
		// Render solid or wireframe nuggets
		auto fill_mode = nugget.FillMode();
		if (fill_mode == EFillMode::Default ||
			fill_mode == EFillMode::Solid ||
			fill_mode == EFillMode::Wireframe ||
			fill_mode == EFillMode::SolidWire)
		{
			m_cmd_list.SetPipelineState(m_pipe_state_pool.Get(desc));
			if (nugget.m_irange.empty())
			{
				m_cmd_list.DrawInstanced(
					s_cast<size_t>(nugget.m_vrange.size()), 1U,
					s_cast<size_t>(nugget.m_vrange.m_beg), 0U);
			}
			else
			{
				m_cmd_list.DrawIndexedInstanced(
					s_cast<size_t>(nugget.m_irange.size()), 1U,
					s_cast<size_t>(nugget.m_irange.m_beg), 0, 0U);
			}
		}

		// Render wire frame over solid for 'SolidWire' mode
		if (!nugget.m_irange.empty() && fill_mode == EFillMode::SolidWire && (
			nugget.m_topo == ETopo::TriList ||
			nugget.m_topo == ETopo::TriListAdj ||
			nugget.m_topo == ETopo::TriStrip ||
			nugget.m_topo == ETopo::TriStripAdj))
		{
			// Change the pipe state to wireframe
			auto prev_fill_mode = desc.Get<EPipeState::FillMode>();
			desc.Apply(PSO<EPipeState::FillMode>(D3D12_FILL_MODE_WIREFRAME));
			desc.Apply(PSO<EPipeState::BlendState0>({FALSE}));
			m_cmd_list.SetPipelineState(m_pipe_state_pool.Get(desc));

			m_cmd_list.DrawIndexedInstanced(
				s_cast<size_t>(nugget.m_irange.size()), 1U,
				s_cast<size_t>(nugget.m_irange.m_beg), 0, 0U);

			// Restore it
			desc.Apply(PSO<EPipeState::FillMode>(prev_fill_mode));
		}

		// Render points for 'Points' mode
		if (fill_mode == EFillMode::Points)
		{
			// Change the pipe state to point list
			desc.Apply(PSO<EPipeState::TopologyType>(To<D3D12_PRIMITIVE_TOPOLOGY_TYPE>(ETopo::PointList)));
			desc.Apply(PSO<EPipeState::GS>(wnd().m_diag.m_gs_fillmode_points->m_code.GS));
			//todo scn().m_diag.m_gs_fillmode_points->Setup();
			m_cmd_list.SetPipelineState(m_pipe_state_pool.Get(desc));

			m_cmd_list.DrawInstanced(
				s_cast<size_t>(nugget.m_vrange.size()), 1U,
				s_cast<size_t>(nugget.m_vrange.m_beg), 0U);
		}
	}


#if 0 // todo
	// Update the provided shader set appropriate for this render step
	void RenderRayCast::ConfigShaders(ShaderSet1& ss, ETopo topo) const
	{
		ss = ShaderSet1{};
		ss.m_vs = m_vs.get();
		switch (topo)
		{
		case ETopo::PointList:
			ss.m_gs = m_gs_vert.get();
			break;
		case ETopo::LineList:
		case ETopo::LineListAdj:
		case ETopo::LineStrip:
		case ETopo::LineStripAdj:
			ss.m_gs = m_gs_edge.get();
			break;
		case ETopo::TriList:
		case ETopo::TriStrip:
			ss.m_gs = m_gs_face.get();
			break;
		default:
			throw std::runtime_error("Unsupported primitive type");
		}
	}

#endif
}
