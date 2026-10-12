//*********************************************
// Compute
//  Copyright (c) Rylogic Ltd 2026
//*********************************************
#pragma once
#include "pr/compute/shaders/shader_cache.h"

namespace pr::compute::shader_cache
{
	// Filesystem-backed compiled shader cache.
	struct ShaderCacheFS : IShaderCache
	{
		struct Config
		{
			std::filesystem::path m_cache_dir;
			std::string m_namespace;
			std::chrono::milliseconds m_lock_timeout = std::chrono::seconds(30);
			std::chrono::milliseconds m_lock_poll_interval = std::chrono::milliseconds(10);
		};

		Config m_config;
		mutable std::mutex m_mutex;
		CacheStats m_stats;

		// Driver-compiled pipelines persisted in one file per cache namespace. The library reads from 'm_library_blob' for its whole
		// lifetime, so the blob is declared first to be destroyed last. Only pipelines created on 'm_library_device' use the library.
		std::mutex m_library_mutex;
		std::vector<uint8_t> m_library_blob;
		D3DPtr<ID3D12Device> m_library_device;
		D3DPtr<ID3D12PipelineLibrary> m_library;
		bool m_library_dirty;

		explicit ShaderCacheFS(std::filesystem::path cache_dir, std::string cache_namespace = {});
		explicit ShaderCacheFS(Config config);
		ShaderCacheFS(ShaderCacheFS const&) = delete;
		ShaderCacheFS& operator = (ShaderCacheFS const&) = delete;

		// Save any newly created pipelines.
		~ShaderCacheFS() override;

		// Return cached output or compile and publish a new cache entry.
		CompileOutput GetOrCompile(CompileRequest const& request, IShaderCache::CompileFunc compile) override;

		// Return a compute pipeline state, loading it from the persisted pipeline library when possible.
		D3DPtr<ID3D12PipelineState> GetOrCreatePipeline(ID3D12Device* device, D3D12_COMPUTE_PIPELINE_STATE_DESC const& desc, std::string_view name) override;

		// Write pipelines created since the library was loaded to the cache directory. Does nothing if there are none.
		void SavePipelines();

		// Delete entries owned by this cache namespace.
		void Clear();

		// Return diagnostic counters for this cache instance.
		CacheStats Stats() const override;

	private:

		// Add 'value' to a named diagnostic counter.
		void Count(uint64_t CacheStats::*field, uint64_t value = 1);

		// Return the pipeline library for 'device', or null if pipelines on 'device' cannot be cached. Requires 'm_library_mutex'.
		ID3D12PipelineLibrary* Library(ID3D12Device* device);
	};
}

