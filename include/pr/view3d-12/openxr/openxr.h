//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"

namespace pr::rdr12::openxr
{
	// Construct an OpenXR version number
	static constexpr uint64_t Version(uint16_t major, uint16_t minor, uint32_t patch)
	{
		return
			((static_cast<uint64_t>(major) & 0xffffULL) << 48) |
			((static_cast<uint64_t>(minor) & 0xffffULL) << 32) |
			((static_cast<uint64_t>(patch) & 0xffffffffULL) << 0);
	}

	// View types
	enum class EViewType
	{
		Unknown = 0,
		Mono, // XR_VIEW_CONFIGURATION_TYPE_PRIMARY_MONO
		Stereo, // XR_VIEW_CONFIGURATION_TYPE_PRIMARY_STEREO
	};

	// Specifications for a view configuration
	struct ViewSpec
	{
		EViewType m_view_type;
		iv2 m_image_size_rec; // Recommended image size
		iv2 m_image_size_max; // The maximum image size
		int m_samples_rec; // Recommented number of multisamples
		int m_samples_max; // The maximum number of multisamples
	};

	// Requirements for a Dx device to support OpenXR
	struct DeviceRequirementsData
	{
		D3D_FEATURE_LEVEL m_feature_level = D3D_FEATURE_LEVEL_11_0;
		LUID m_adapter_luid = {};
	};

	// Configuration for OpenXR initialisation
	struct Config
	{
		// XR API version to use
		uint64_t m_xr_version = Version(1, 0, 0);

		// Dx12 instances to bind to the XR session
		ID3D12Device* m_device = {};
		ID3D12CommandQueue* m_queue = {};

		// Application name
		std::string m_app_name = "Rylogic App";
		int m_app_version = 1;

		EViewType m_view_type = EViewType::Stereo;

		// Fluent interface
		Config& app_name(std::string_view name)
		{
			m_app_name = name;
			return *this;
		}
		Config& app_version(int version)
		{
			m_app_version = version;
			return *this;
		}
		Config& xr_version(uint64_t version)
		{
			m_xr_version = version;
			return *this;
		}
		Config& device(ID3D12Device* dev)
		{
			m_device = dev;
			return *this;
		}
		Config& queue(ID3D12CommandQueue* queue)
		{
			m_queue = queue;
			return *this;
		}
	};


	struct OpenXR
	{
		virtual ~OpenXR() = default;

		// Return the XR device requirements
		virtual DeviceRequirementsData DeviceRequirements() const = 0;

		// Get the list of supported view specs
		virtual std::vector<ViewSpec> GetViewSpecs() const = 0;
		
		// Attempt to initialize the OpenXR runtime
		virtual void CreateSession(ViewSpec const& view) = 0;
	};

	// Create an OpenXR instasnce
	std::unique_ptr<OpenXR> CreateInstance(Config const& config);
}
