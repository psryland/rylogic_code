//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/openxr/openxr.h"
#include "openxr/openxr.h"

namespace pr
{
	template <> struct Convert<XrViewConfigurationType, rdr12::openxr::EViewType>
	{
		static XrViewConfigurationType Func(rdr12::openxr::EViewType from)
		{
			using namespace rdr12::openxr;
			switch (from)
			{
				case EViewType::Mono: return XR_VIEW_CONFIGURATION_TYPE_PRIMARY_MONO;
				case EViewType::Stereo: return XR_VIEW_CONFIGURATION_TYPE_PRIMARY_STEREO;
				//case EViewType:: return XR_VIEW_CONFIGURATION_TYPE_PRIMARY_STEREO_WITH_FOVEATED_INSET = 1000037000;
				//case EViewType:: return XR_VIEW_CONFIGURATION_TYPE_SECONDARY_MONO_FIRST_PERSON_OBSERVER_MSFT = 1000054000;
				//case EViewType:: return XR_VIEW_CONFIGURATION_TYPE_PRIMARY_QUAD_VARJO = XR_VIEW_CONFIGURATION_TYPE_PRIMARY_STEREO_WITH_FOVEATED_INSET;
				//case EViewType:: return XR_VIEW_CONFIGURATION_TYPE_MAX_ENUM = 0x7FFFFFFF;
				default: throw std::runtime_error("Unknown rdr12::openxr::EViewType");
			}
		}
	};
	template <> struct Convert<rdr12::openxr::EViewType, XrViewConfigurationType>
	{
		static rdr12::openxr::EViewType Func(XrViewConfigurationType from)
		{
			using namespace rdr12::openxr;
			switch (from)
			{
				case XR_VIEW_CONFIGURATION_TYPE_PRIMARY_MONO: return EViewType::Mono;
				case XR_VIEW_CONFIGURATION_TYPE_PRIMARY_STEREO: return EViewType::Stereo;
				//case XR_VIEW_CONFIGURATION_TYPE_PRIMARY_STEREO_WITH_FOVEATED_INSET = 1000037000;
				//case XR_VIEW_CONFIGURATION_TYPE_SECONDARY_MONO_FIRST_PERSON_OBSERVER_MSFT = 1000054000;
				//case XR_VIEW_CONFIGURATION_TYPE_PRIMARY_QUAD_VARJO = XR_VIEW_CONFIGURATION_TYPE_PRIMARY_STEREO_WITH_FOVEATED_INSET;
				//case XR_VIEW_CONFIGURATION_TYPE_MAX_ENUM = 0x7FFFFFFF;
				default: throw std::runtime_error("Unknown XrViewConfigurationType");
			}
		}
	};

	template <> struct Convert<XrViewConfigurationView, rdr12::openxr::ViewSpec>
	{
		static XrViewConfigurationView Func(rdr12::openxr::ViewSpec from)
		{
			using namespace rdr12::openxr;
			return XrViewConfigurationView{
				.type                            = XR_TYPE_VIEW_CONFIGURATION_VIEW,
				.next                            = nullptr,
				.recommendedImageRectWidth       = static_cast<uint32_t>(from.m_image_size_rec.x),
				.maxImageRectWidth               = static_cast<uint32_t>(from.m_image_size_max.x),
				.recommendedImageRectHeight      = static_cast<uint32_t>(from.m_image_size_rec.y),
				.maxImageRectHeight              = static_cast<uint32_t>(from.m_image_size_max.y),
				.recommendedSwapchainSampleCount = static_cast<uint32_t>(from.m_samples_rec),
				.maxSwapchainSampleCount         = static_cast<uint32_t>(from.m_samples_max),
			};
		}
	};
	template <> struct Convert<rdr12::openxr::ViewSpec, XrViewConfigurationView>
	{
		static rdr12::openxr::ViewSpec Func(XrViewConfigurationView from)
		{
			using namespace rdr12::openxr;
			return ViewSpec{
				.m_image_size_rec = iv2{
					static_cast<int>(from.recommendedImageRectWidth),
					static_cast<int>(from.recommendedImageRectHeight)},
				.m_image_size_max = iv2{
					static_cast<int>(from.maxImageRectWidth),
					static_cast<int>(from.maxImageRectHeight)},
				.m_samples_rec = static_cast<int>(from.recommendedSwapchainSampleCount),
				.m_samples_max = static_cast<int>(from.maxSwapchainSampleCount),
			};
		}
	};
}
