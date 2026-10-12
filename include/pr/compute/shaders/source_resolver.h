//*********************************************
// Compute
//  Copyright (c) Rylogic Ltd 2026
//*********************************************
#pragma once
#include "pr/compute/forward.h"
#include "pr/compute/shaders/dependency.h"

namespace pr::compute::shader_cache
{
	// Source text returned by a cache-aware source resolver.
	struct Source
	{
		std::string m_identity;
		std::string m_code;
		Dependency m_dependency;
	};

	// An interface for resolving root shader sources and includes while exposing cache validation metadata.
	struct ISourceResolver
	{
		virtual ~ISourceResolver() = default;

		// Return a stable key for the resolver configuration.
		virtual std::string CacheKey() const = 0;

		// Resolve 'source_name' to source text and dependency metadata.
		virtual Source Resolve(std::string_view source_name) const = 0;
	};

	// A source resolver for files on disk.
	struct FileSourceResolver : ISourceResolver
	{
		std::vector<std::filesystem::path> m_include_dirs;

		FileSourceResolver() = default;
		explicit FileSourceResolver(std::filesystem::path include_dir);
		explicit FileSourceResolver(std::vector<std::filesystem::path> include_dirs);

		// Return a stable key for the include search paths.
		std::string CacheKey() const override;

		// Resolve a file path or include name to source text and file timestamp metadata.
		Source Resolve(std::string_view source_name) const override;
	};

	// A source resolver for embedded TEXT resources.
	struct ResourceSourceResolver : ISourceResolver
	{
		HMODULE m_module;
		std::wstring m_type;

		explicit ResourceSourceResolver(HMODULE module = nullptr, std::wstring type = L"TEXT");

		// Return a stable key for the resource module and resource type.
		std::string CacheKey() const override;

		// Resolve a resource name to source text and resource content metadata.
		Source Resolve(std::string_view source_name) const override;
	};

	// Return all bytes from 'path'.
	std::vector<uint8_t> ReadBytes(std::filesystem::path const& path);
}

#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::compute::shader_cache::tests
{
	PRUnitTestClass(ShaderDependencyTests)
	{
		// An embedded shader is identified by its content, so the cache entry survives a rebuild of its module that does not change the shader.
		PRUnitTestMethod(ResourceDependencyUsesContent, Quick)
		{
			// Resolve a shader embedded in this module and check that no module timestamp is recorded.
			auto source = ResourceSourceResolver{}.Resolve("src/compute/radix_sort/radix_sort.hlsl");
			auto const& dep = source.m_dependency;
			PR_EXPECT(dep.m_kind == Dependency::EKind::Resource);
			PR_EXPECT(dep.m_last_write == 0);
			PR_EXPECT(dep.m_size == source.m_code.size());
			PR_EXPECT(dep.m_hash == static_cast<uint64_t>(hash::HashBytes64(source.m_code.data(), source.m_code.data() + source.m_code.size())));

			// Revalidating against the loaded module finds the same content.
			auto current = CurrentDependency(dep);
			PR_EXPECT(current.has_value() && dep.SameContentVersion(*current));

			// A dependency on a module that is not loaded cannot be validated.
			auto missing = dep;
			missing.m_path = std::filesystem::temp_directory_path() / L"pr_not_a_loaded_module.dll";
			PR_EXPECT(!CurrentDependency(missing).has_value());
		}
	};
}
#endif