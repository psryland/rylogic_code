#pragma once

#include "clicket/forward.h"

struct UserData
{
	enum { WindowTitleLen = MAX_PATH, ButtonTextLen = 64, ControlTypeLen = 128 };
	
	// WARNING: POD data only
	wchar_t m_window_title[WindowTitleLen];     // window title text
	wchar_t m_control_type[ControlTypeLen];     // the control type to look for
	wchar_t m_button_text[ButtonTextLen];       // button text
	DWORD   m_pol_freq;                         // value of polling frequency
	int     m_pol_freq_unit;                    // selection for unit
	
	UserData()
	{
		_snwprintf_s(m_window_title, WindowTitleLen, L"<title of window to look for>");
		_snwprintf_s(m_control_type, ControlTypeLen, L"<type of control to look for>");
		_snwprintf_s(m_button_text,  ButtonTextLen,  L"<control text>");
		m_pol_freq = 1;
		m_pol_freq_unit = 1;
	}
	void Validate()
	{
		m_window_title[WindowTitleLen - 1] = 0;
		m_control_type[ControlTypeLen - 1] = 0;
		m_button_text[ButtonTextLen - 1] = 0;
		m_pol_freq      = (m_pol_freq       >= 1 && m_pol_freq      <= 100000)         ? m_pol_freq : 1;
		m_pol_freq_unit = (m_pol_freq_unit  >= 0 && m_pol_freq_unit < EFreq::NumberOf) ? m_pol_freq_unit : 1;
	}
	void Load()
	{
		std::wstring path(MAX_PATH, 0);
		GetModuleFileNameW(0, &path[0], static_cast<DWORD>(path.size()));
		path = path.c_str();
		path += L".user_data";

		UserData copy;
		pr::Handle file = pr::FileOpen(path.c_str(), pr::EFileOpen::Reading);
		if (!pr::FileRead(file, &copy, sizeof(copy))) return;

		copy.Validate();
		*this = copy;
	}
	void Save()
	{
		std::wstring path(MAX_PATH, 0);
		GetModuleFileNameW(0, &path[0], static_cast<DWORD>(path.size()));
		path = path.c_str();
		path += L".user_data";

		pr::Handle file = pr::FileOpen(path.c_str(), pr::EFileOpen::Writing);
		pr::FileWrite(file, this, sizeof(*this));
	}
};

