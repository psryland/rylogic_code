//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
// Usage:
//  Create a long-lived instance of the GpuRadixSort.
//  Resize it to the size of the data to be sorted.

#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/shaders/shader.h"
#include "pr/view3d-12/utility/root_signature.h"
#include "pr/view3d-12/utility/barrier_batch.h"
#include "pr/view3d-12/compute/compute_pso.h"
#include "pr/view3d-12/main/renderer.h"

namespace pr::rdr12
{
	// Types that can be sorted on the GPU
	template <typename T>
	concept GpuSortableKey = std::is_same_v<T, int> || std::is_same_v<T, uint32_t> || std::is_same_v<T, float>;
	template <typename T>
	concept GpuSortableValue = std::is_same_v<T, int> || std::is_same_v<T, uint32_t> || std::is_same_v<T, float> || std::is_same_v<T, void>;

	// Radix sort on the GPU
	template <GpuSortableKey Key, GpuSortableValue Value, bool Ascending = true>
	struct GpuRadixSort
	{
		// Notes:
		//  - This type is intended to be used repeatedly to sort large numbers of elements.
		//    It's not suited for transient sorts.
		//  - Use 'Value = void' if no payload is required, i.e., you just want to sort key values.

		static constexpr int KeyBits = sizeof(Key)*8; // 32-bit keys atm
		static constexpr int RadixBits = 8;
		static constexpr int Radix = 1 << RadixBits; // The number of digit bins
		static constexpr int RadixPasses = KeyBits / RadixBits;
		static constexpr int MaxReadBack = 1 << 13;
		static constexpr int MaxDispatchDimension = 65535;
		static constexpr bool HasPayload = !std::is_same_v<Value, void>;
		static constexpr bool SortAscending = Ascending;

		enum class EParam {};
		enum class ESamp {};

		struct ComputeStep
		{
			D3DPtr<ID3D12RootSignature> m_sig;
			D3DPtr<ID3D12PipelineState> m_pso;
		};
		struct TuningParams
		{
			std::wstring shader_model = L"cs_6_6";
			int partition_size = 7680;
			int keys_per_thread = 15;
			int part_size = 7680;
			bool use_16bit = true;
		};

		Renderer* m_rdr;            // The renderer instance to use to run the compute shader
		GpuSync m_gsync;            // The GPU fence
		ComCmdAllocPool m_cmd_pool; // Command allocator pool for the compute shader
		ComCmdList m_cmd_list;      // Command list for the compute shader

		ComputeStep m_init;
		ComputeStep m_sweep_up;
		ComputeStep m_scan;
		ComputeStep m_sweep_down;

		mutable GpuUploadBuffer m_upload;     // Upload buffer for the compute shader
		mutable GpuReadbackBuffer m_readback; // Readback buffer for the compute shader
		D3DPtr<ID3D12Resource> m_sort[2];
		D3DPtr<ID3D12Resource> m_payload[2];
		D3DPtr<ID3D12Resource> m_pass_histogram;
		D3DPtr<ID3D12Resource> m_global_histogram;
		D3DPtr<ID3D12Resource> m_error_count;

		TuningParams m_tuning; // Tuning parameters
		int64_t m_size; // The number of elements to be sorted

		struct Result
		{
			GpuReadbackBuffer::Allocation keys;
			GpuReadbackBuffer::Allocation values;
		};

		explicit GpuRadixSort(Renderer& rdr, TuningParams const& tuning = {})
			: m_rdr(&rdr)
			, m_gsync(rdr.D3DDevice())
			, m_cmd_pool(m_gsync)
			, m_cmd_list(rdr.D3DDevice(), m_cmd_pool.Get(), nullptr, "GpuRadixSort", 0xFF78A79d)
			, m_init()
			, m_sweep_up()
			, m_scan()
			, m_sweep_down()
			, m_upload(m_gsync, 0)
			, m_readback(m_gsync, 0)
			, m_sort()
			, m_payload()
			, m_pass_histogram()
			, m_global_histogram()
			, m_error_count()
			, m_tuning(tuning)
			, m_size()
		{
			auto device = rdr.D3DDevice();
			auto shader_model = L"-T" + m_tuning.shader_model;
			auto source = resource::Read<char>(L"GPU_RADIX_SORT_HLSL", L"TEXT");
			auto args = std::vector<wchar_t const*>{ L"-E<entry_point_placeholder>", shader_model.c_str(), L"-O3", L"-Zi" };
			if constexpr (std::is_same_v<Key, int>)
				args.push_back(L"-DKEY_TYPE=int");
			if constexpr (std::is_same_v<Key, uint32_t>)
				args.push_back(L"-DKEY_TYPE=uint");
			if constexpr (std::is_same_v<Key, float>)
				args.push_back(L"-DKEY_TYPE=float");
			if constexpr (std::is_same_v<Value, int>)
				args.push_back(L"-DPAYLOAD_TYPE=int");
			if constexpr (std::is_same_v<Value, uint32_t>)
				args.push_back(L"-DPAYLOAD_TYPE=uint");
			if constexpr (std::is_same_v<Value, float>)
				args.push_back(L"-DPAYLOAD_TYPE=float");
			if constexpr (Ascending)
				args.push_back(L"-DSHOULD_ASCEND");
			if constexpr (HasPayload)
				args.push_back(L"-DSORT_PAIRS=1");
			auto keys_per_thread = std::format(L"-DKEYS_PER_THREAD={}", m_tuning.keys_per_thread);
			args.push_back(keys_per_thread.c_str());
			auto part_size = std::format(L"-DPART_SIZE={}", m_tuning.part_size);
			args.push_back(part_size.c_str());
			auto use_16bit = std::format(L"-DUSE_16_BIT={}", m_tuning.use_16bit ? 1 : 0);
			args.push_back(use_16bit.c_str());
			if (m_tuning.use_16bit)
				args.push_back(L"-enable-16bit-types");

			// InitRadixSort
			{
				RootSig<EParam, ESamp> sig;
				sig.Flags = {
					D3D12_ROOT_SIGNATURE_FLAG_DENY_VERTEX_SHADER_ROOT_ACCESS |
					D3D12_ROOT_SIGNATURE_FLAG_DENY_HULL_SHADER_ROOT_ACCESS |
					D3D12_ROOT_SIGNATURE_FLAG_DENY_DOMAIN_SHADER_ROOT_ACCESS |
					D3D12_ROOT_SIGNATURE_FLAG_DENY_GEOMETRY_SHADER_ROOT_ACCESS |
					D3D12_ROOT_SIGNATURE_FLAG_DENY_PIXEL_SHADER_ROOT_ACCESS |
					D3D12_ROOT_SIGNATURE_FLAG_DENY_AMPLIFICATION_SHADER_ROOT_ACCESS |
					D3D12_ROOT_SIGNATURE_FLAG_DENY_MESH_SHADER_ROOT_ACCESS |
					D3D12_ROOT_SIGNATURE_FLAG_NONE };
				sig.Uav(EParam(0), EUAVReg::u4);
				m_init.m_sig = sig.Create(device);

				args[0] = L"-EInitRadixSort";
				auto bytecode = CompileShader(source, args);
				ComputePSO pso(m_init.m_sig.get(), bytecode);
				m_init.m_pso = pso.Create(device, "GpuRadixSort:Init");
			}

			// Sweep Up
			{
				RootSig<EParam, ESamp> sig;
				sig.Flags = {
					D3D12_ROOT_SIGNATURE_FLAG_DENY_VERTEX_SHADER_ROOT_ACCESS |
					D3D12_ROOT_SIGNATURE_FLAG_DENY_HULL_SHADER_ROOT_ACCESS |
					D3D12_ROOT_SIGNATURE_FLAG_DENY_DOMAIN_SHADER_ROOT_ACCESS |
					D3D12_ROOT_SIGNATURE_FLAG_DENY_GEOMETRY_SHADER_ROOT_ACCESS |
					D3D12_ROOT_SIGNATURE_FLAG_DENY_PIXEL_SHADER_ROOT_ACCESS |
					D3D12_ROOT_SIGNATURE_FLAG_DENY_AMPLIFICATION_SHADER_ROOT_ACCESS |
					D3D12_ROOT_SIGNATURE_FLAG_DENY_MESH_SHADER_ROOT_ACCESS |
					D3D12_ROOT_SIGNATURE_FLAG_NONE };
				sig.U32(EParam(0), ECBufReg::b0, 4);
				sig.Uav(EParam(1), EUAVReg::u0); // m_sort0
				sig.Uav(EParam(2), EUAVReg::u4); // m_global_histogram
				sig.Uav(EParam(3), EUAVReg::u5); // m_pass_histogram
				m_sweep_up.m_sig = sig.Create(device);

				args[0] = L"-ESweepUp";
				auto bytecode = CompileShader(source, args);
				ComputePSO pso(m_sweep_up.m_sig.get(), bytecode);
				m_sweep_up.m_pso = pso.Create(device, "GpuRadixSort:SweepUp");
			}

			// Scan
			{
				RootSig<EParam, ESamp> sig;
				sig.Flags = {
					D3D12_ROOT_SIGNATURE_FLAG_DENY_VERTEX_SHADER_ROOT_ACCESS |
					D3D12_ROOT_SIGNATURE_FLAG_DENY_HULL_SHADER_ROOT_ACCESS |
					D3D12_ROOT_SIGNATURE_FLAG_DENY_DOMAIN_SHADER_ROOT_ACCESS |
					D3D12_ROOT_SIGNATURE_FLAG_DENY_GEOMETRY_SHADER_ROOT_ACCESS |
					D3D12_ROOT_SIGNATURE_FLAG_DENY_PIXEL_SHADER_ROOT_ACCESS |
					D3D12_ROOT_SIGNATURE_FLAG_DENY_AMPLIFICATION_SHADER_ROOT_ACCESS |
					D3D12_ROOT_SIGNATURE_FLAG_DENY_MESH_SHADER_ROOT_ACCESS |
					D3D12_ROOT_SIGNATURE_FLAG_NONE };
				sig.U32(EParam(0), ECBufReg::b0, 4);
				sig.Uav(EParam(1), EUAVReg::u5); // m_pass_histogram
				m_scan.m_sig = sig.Create(device);

				args[0] = L"-EScan";
				auto bytecode = CompileShader(source, args);
				ComputePSO pso(m_scan.m_sig.get(), bytecode);
				m_scan.m_pso = pso.Create(device, "GpuRadixSort:Scan");
			}

			// Sweep Down
			{
				RootSig<EParam, ESamp> sig;
				sig.Flags = {
					D3D12_ROOT_SIGNATURE_FLAG_DENY_VERTEX_SHADER_ROOT_ACCESS |
					D3D12_ROOT_SIGNATURE_FLAG_DENY_HULL_SHADER_ROOT_ACCESS |
					D3D12_ROOT_SIGNATURE_FLAG_DENY_DOMAIN_SHADER_ROOT_ACCESS |
					D3D12_ROOT_SIGNATURE_FLAG_DENY_GEOMETRY_SHADER_ROOT_ACCESS |
					D3D12_ROOT_SIGNATURE_FLAG_DENY_PIXEL_SHADER_ROOT_ACCESS |
					D3D12_ROOT_SIGNATURE_FLAG_DENY_AMPLIFICATION_SHADER_ROOT_ACCESS |
					D3D12_ROOT_SIGNATURE_FLAG_DENY_MESH_SHADER_ROOT_ACCESS |
					D3D12_ROOT_SIGNATURE_FLAG_NONE };
				sig.U32(EParam(0), ECBufReg::b0, 4);
				sig.Uav(EParam(1), EUAVReg::u0); // m_sort0
				sig.Uav(EParam(2), EUAVReg::u1); // m_sort1
				sig.Uav(EParam(3), EUAVReg::u2); // m_payload0
				sig.Uav(EParam(4), EUAVReg::u3); // m_payload1
				sig.Uav(EParam(5), EUAVReg::u4); // m_global_histogram
				sig.Uav(EParam(6), EUAVReg::u5); // m_pass_histogram
				m_sweep_down.m_sig = sig.Create(device);

				args[0] = L"-ESweepDown";
				auto bytecode = CompileShader(source, args);
				ComputePSO pso(m_sweep_down.m_sig.get(), bytecode);
				m_sweep_down.m_pso = pso.Create(device, "GpuRadixSort:SweepDown");
			}

			// Create sort-size independent buffers
			{
				auto& desc = ResDesc::Buf(Radix * RadixPasses, sizeof(Key), nullptr, alignof(Key)).usage(EUsage::UnorderedAccess);
				m_global_histogram = rdr.res().CreateResource(desc, "RadixSort:histogram");
			}
			{
				auto& desc = ResDesc::Buf(1, sizeof(Key), nullptr, alignof(Key)).usage(EUsage::UnorderedAccess);
				m_error_count = rdr.res().CreateResource(desc, "RadixSort:error_count");
			}
		}

		// Resize the GPU buffers in preparation for sorting 'size' elements
		void Resize(int64_t size)
		{
			if (size == m_size)
				return;

			{
				auto& desc = ResDesc::Buf(size, sizeof(Key), nullptr, alignof(Key))
					.usage(EUsage::UnorderedAccess)
					.def_state(D3D12_RESOURCE_STATE_UNORDERED_ACCESS);

				m_sort[0] = m_rdr->res().CreateResource(desc, "RadixSort:sort0");
				m_sort[1] = m_rdr->res().CreateResource(desc, "RadixSort:sort1");
			}
			{
				using T = std::conditional_t<HasPayload, Value, int>;
				auto& desc = ResDesc::Buf(HasPayload ? size : 1, sizeof(T), nullptr, alignof(T))
					.usage(EUsage::UnorderedAccess)
					.def_state(D3D12_RESOURCE_STATE_UNORDERED_ACCESS);

				m_payload[0] = m_rdr->res().CreateResource(desc, "RadixSort:payload0");
				m_payload[1] = m_rdr->res().CreateResource(desc, "RadixSort:payload1");
			}
			{
				auto partitions = ThreadBlocks(size);
				auto& desc = ResDesc::Buf(Radix * partitions, sizeof(Key), nullptr, alignof(Key))
					.usage(EUsage::UnorderedAccess)
					.def_state(D3D12_RESOURCE_STATE_UNORDERED_ACCESS);

				m_pass_histogram = m_rdr->res().CreateResource(desc, "RadixSort:passHistBuffer");
			}

			m_size = size;
		}

		// Sort 'values' by 'keys' in-place
		void Sort(std::span<Key> keys, std::span<Value> values)
		{
			// Upload 'keys' and 'values' to the GPU and then sort them
			auto result = Sort(m_cmd_list, keys, values);

			// Job complete
			m_cmd_list.Close();

			// Run the sort job
			m_rdr->ExecuteComCommandLists({ m_cmd_list.get() });

			// Record the sync point for when the command will be finished
			auto sync_point = m_gsync.AddSyncPoint(m_rdr->ComQueue());
			m_cmd_list.SyncPoint(sync_point);

			// Reset for the next job
			m_cmd_list.Reset(m_cmd_pool.Get());

			// Wait for the GPU to finish
			m_gsync.Wait();

			// Readback the results and update the input arrays
			{
				memcpy(keys.data(), result.keys.ptr<Key>(), result.keys.m_size);
			}
			if constexpr (HasPayload)
			{
				memcpy(values.data(), result.values.ptr<Value>(), result.values.m_size);
			}
		}

		// Sort 'values' by 'keys' using the provided command list
		// Returns Readback buffer allocations that will contain the sorted result once the command list has been executed.
		Result Sort(ComCmdList& cmd_list, std::span<Key const> keys, std::span<Value const> values) const
		{
			if (ssize(keys) > m_size)
			{
				throw std::runtime_error("GpuRadixSort::Sort: sort buffer is not large enough. Use 'Resize' first.");
			}
			if constexpr (HasPayload)
			{
				if (keys.size() != values.size())
					throw std::runtime_error("GpuRadixSort::Sort: keys and values must be the same size");
			}
			else
			{
				if (!values.empty())
					throw std::runtime_error("GpuRadixSort::Sort: values provided to keys-only sorter");
			}

			BarrierBatch barriers(cmd_list);
			barriers.Transition(m_sort[0].get(), D3D12_RESOURCE_STATE_COPY_DEST);
			barriers.Transition(m_payload[0].get(), D3D12_RESOURCE_STATE_COPY_DEST);
			barriers.Commit();

			// Copy the keys and values to the GPU. If 'keys' is smaller than 'm_size', pad with 0xFF
			{
				auto upload = m_upload.Alloc(m_size * sizeof(Key), alignof(Key));
				memcpy(upload.ptr<Key>(), keys.data(), keys.size() * sizeof(Key));
				memset(upload.ptr<Key>() + keys.size(), 0xFF, (m_size - keys.size()) * sizeof(Key));
				cmd_list.CopyBufferRegion(m_sort[0].get(), 0, upload.m_buf, upload.m_ofs, upload.m_size);
			}
			if constexpr (HasPayload)
			{
				auto upload = m_upload.Alloc(m_size * sizeof(Value), alignof(Value));
				memcpy(upload.ptr<Value>(), values.data(), values.size() * sizeof(Value));
				memset(upload.ptr<Key>() + values.size(), 0xFF, (m_size - values.size()) * sizeof(Value));
				cmd_list.CopyBufferRegion(m_payload[0].get(), 0, upload.m_buf, upload.m_ofs, upload.m_size);
			}

			barriers.Transition(m_sort[0].get(), D3D12_RESOURCE_STATE_NON_PIXEL_SHADER_RESOURCE);
			barriers.Transition(m_payload[0].get(), D3D12_RESOURCE_STATE_NON_PIXEL_SHADER_RESOURCE);
			barriers.Commit();

			// Sort the buffers on the GPU
			Sort(cmd_list);

			barriers.Transition(m_sort[0].get(), D3D12_RESOURCE_STATE_COPY_SOURCE);
			barriers.Transition(m_payload[0].get(), D3D12_RESOURCE_STATE_COPY_SOURCE);
			barriers.Commit();

			Result result = {};

			// Copy the results back to the CPU
			{
				auto readback = m_readback.Alloc(ssize(keys) * sizeof(Key), alignof(Key));
				cmd_list.CopyBufferRegion(readback.m_buf, readback.m_ofs, m_sort[0].get(), 0, readback.m_size);
				result.keys = readback;
			}
			if constexpr (HasPayload)
			{
				auto readback = m_readback.Alloc(ssize(values) * sizeof(Value), alignof(Value));
				cmd_list.CopyBufferRegion(readback.m_buf, readback.m_ofs, m_payload[0].get(), 0, readback.m_size);
				result.values = readback;
			}
			
			barriers.Transition(m_sort[0].get(), D3D12_RESOURCE_STATE_NON_PIXEL_SHADER_RESOURCE);
			barriers.Transition(m_payload[0].get(), D3D12_RESOURCE_STATE_NON_PIXEL_SHADER_RESOURCE);
			barriers.Commit();

			return result;
		}

		// Sort the keys/values in 'm_sort[0]/m_payload[0]' assuming they're uploaded to the GPU already.
		// This overload is indended for use when you want to leave the keys/values on the GPU without reading them back.
		void Sort(ComCmdList& cmd_list) const
		{
			// Reset the histogram
			{
				cmd_list.SetPipelineState(m_init.m_pso.get());
				cmd_list.SetComputeRootSignature(m_init.m_sig.get());
				cmd_list.SetComputeRootUnorderedAccessView(0, m_global_histogram->GetGPUVirtualAddress());
				cmd_list.Dispatch(1, 1, 1);
			}

			BarrierBatch barriers(cmd_list);
			barriers.UAV(m_global_histogram.get());
			barriers.Commit();

			// Do the sort
			int i = 0, j = 1;
			const auto thread_blocks = s_cast<uint32_t>(ThreadBlocks(m_size));
			for (auto radix_shift = 0U; radix_shift != KeyBits; radix_shift += RadixBits)
			{
				// Sweep Up
				{
					const auto full_blocks = thread_blocks / MaxDispatchDimension;
					if (full_blocks)
					{
						cmd_list.SetPipelineState(m_sweep_up.m_pso.get());
						cmd_list.SetComputeRootSignature(m_sweep_up.m_sig.get());

						std::array<uint32_t, 4> t = { s_cast<uint32_t>(m_size), radix_shift, thread_blocks, 0 };
						cmd_list.SetComputeRoot32BitConstants(0, isize(t), t.data(), 0);
						cmd_list.SetComputeRootUnorderedAccessView(1, m_sort[i]->GetGPUVirtualAddress());
						cmd_list.SetComputeRootUnorderedAccessView(2, m_global_histogram->GetGPUVirtualAddress());
						cmd_list.SetComputeRootUnorderedAccessView(3, m_pass_histogram->GetGPUVirtualAddress());
						cmd_list.Dispatch(MaxDispatchDimension, full_blocks, 1);
					}

					const auto partial_blocks = thread_blocks - full_blocks * MaxDispatchDimension;
					if (partial_blocks)
					{
						cmd_list.SetPipelineState(m_sweep_up.m_pso.get());
						cmd_list.SetComputeRootSignature(m_sweep_up.m_sig.get());

						std::array<uint32_t, 4> t = { s_cast<uint32_t>(m_size), radix_shift, thread_blocks, (full_blocks << 1) | 1 };
						cmd_list.SetComputeRoot32BitConstants(0, isize(t), t.data(), 0);
						cmd_list.SetComputeRootUnorderedAccessView(1, m_sort[i]->GetGPUVirtualAddress());
						cmd_list.SetComputeRootUnorderedAccessView(2, m_global_histogram->GetGPUVirtualAddress());
						cmd_list.SetComputeRootUnorderedAccessView(3, m_pass_histogram->GetGPUVirtualAddress());
						cmd_list.Dispatch(partial_blocks, 1, 1);
					}
				}

				barriers.UAV(m_pass_histogram.get());
				barriers.Commit();

				// Scan
				{
					cmd_list.SetPipelineState(m_scan.m_pso.get());
					cmd_list.SetComputeRootSignature(m_scan.m_sig.get());

					std::array<uint32_t, 4> t = { 0, 0, thread_blocks, 0 };
					cmd_list.SetComputeRoot32BitConstants(0, isize(t), t.data(), 0);
					cmd_list.SetComputeRootUnorderedAccessView(1, m_pass_histogram->GetGPUVirtualAddress());
					cmd_list.Dispatch(256, 1, 1);
				}

				barriers.UAV(m_pass_histogram.get());
				barriers.UAV(m_global_histogram.get());
				barriers.Commit();

				// Sweep Down
				{
					const uint32_t full_blocks = thread_blocks / MaxDispatchDimension;
					if (full_blocks)
					{
						cmd_list.SetPipelineState(m_sweep_down.m_pso.get());
						cmd_list.SetComputeRootSignature(m_sweep_down.m_sig.get());

						std::array<uint32_t, 4> t = { s_cast<uint32_t>(m_size), radix_shift, thread_blocks, 0 };
						cmd_list.SetComputeRoot32BitConstants(0, isize(t), t.data(), 0);
						cmd_list.SetComputeRootUnorderedAccessView(1, m_sort[i]->GetGPUVirtualAddress());
						cmd_list.SetComputeRootUnorderedAccessView(2, m_sort[j]->GetGPUVirtualAddress());
						cmd_list.SetComputeRootUnorderedAccessView(3, m_payload[i]->GetGPUVirtualAddress());
						cmd_list.SetComputeRootUnorderedAccessView(4, m_payload[j]->GetGPUVirtualAddress());
						cmd_list.SetComputeRootUnorderedAccessView(5, m_global_histogram->GetGPUVirtualAddress());
						cmd_list.SetComputeRootUnorderedAccessView(6, m_pass_histogram->GetGPUVirtualAddress());
						cmd_list.Dispatch(MaxDispatchDimension, full_blocks, 1);
					}

					const uint32_t partial_blocks = thread_blocks - full_blocks * MaxDispatchDimension;
					if (partial_blocks)
					{
						cmd_list.SetPipelineState(m_sweep_down.m_pso.get());
						cmd_list.SetComputeRootSignature(m_sweep_down.m_sig.get());

						std::array<uint32_t, 4> t = { s_cast<uint32_t>(m_size), radix_shift, thread_blocks, (full_blocks << 1) | 1 };
						cmd_list.SetComputeRoot32BitConstants(0, isize(t), t.data(), 0);
						cmd_list.SetComputeRootUnorderedAccessView(1, m_sort[i]->GetGPUVirtualAddress());
						cmd_list.SetComputeRootUnorderedAccessView(2, m_sort[j]->GetGPUVirtualAddress());
						cmd_list.SetComputeRootUnorderedAccessView(3, m_payload[i]->GetGPUVirtualAddress());
						cmd_list.SetComputeRootUnorderedAccessView(4, m_payload[j]->GetGPUVirtualAddress());
						cmd_list.SetComputeRootUnorderedAccessView(5, m_global_histogram->GetGPUVirtualAddress());
						cmd_list.SetComputeRootUnorderedAccessView(6, m_pass_histogram->GetGPUVirtualAddress());
						cmd_list.Dispatch(partial_blocks, 1, 1);
					}
				}

				barriers.UAV(m_sort[i].get());
				barriers.UAV(m_sort[j].get());
				barriers.UAV(m_payload[i].get());
				barriers.UAV(m_payload[j].get());
				barriers.Commit();

				i = 1 - i;
				j = 1 - j;
			}
		}

	private:

		// How many blocks to partition the job into
		int ThreadBlocks(int64_t size) const
		{
			auto partitions = (size + m_tuning.partition_size - 1) / m_tuning.partition_size;
			return s_cast<int>(partitions);
				
		}

	};
}
