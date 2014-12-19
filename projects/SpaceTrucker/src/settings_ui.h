#pragma once

#include "SpaceTrucker/src/forward.h"
#include "SpaceTrucker/src/settings.h"

namespace st
{
	struct SettingsUI :Form<SettingsUI>
	{
		pr::rdr::SystemConfig m_config;
		Settings&             m_settings;
		ComboBox              m_cb_adaptor;

		SettingsUI(Settings& settings)
			:Form(IDD_SETTINGS)
			,m_config()
			,m_settings(settings)
			,m_cb_adaptor(IDC_CB_ADAPTER, this)
		{
			//for (auto& a : m_config.m_adapters)
			//	m_cb_adaptor.AddItem(Narrow(a.m_desc.Description).c_str());
		}
	};
}
