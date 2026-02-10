//************************************
// Lost at Sea
//  Copyright (c) Rylogic Ltd 2024
//************************************
#pragma once
#include "src/forward.h"

namespace las
{
	struct Settings
	{
		// Display
		bool m_fullscreen = false;
		int m_res_x = 1920;
		int m_res_y = 1080;

		// Version tracking
		std::string m_version;

		Settings()
			:m_version("0.00.01")
		{
		}
		explicit Settings(int)
			:Settings()
		{
		}

		// Load settings from a JSON file
		bool Load(std::filesystem::path const& filepath)
		{
			if (!std::filesystem::exists(filepath))
				return false;

			try
			{
				auto doc = pr::json::Read(filepath);
				auto& root = doc.to_object();
				if (root.find("version"))    m_version = root["version"].to<std::string>();
				if (root.find("fullscreen")) m_fullscreen = root["fullscreen"].to<bool>();
				if (root.find("res_x"))      m_res_x = root["res_x"].to<int>();
				if (root.find("res_y"))      m_res_y = root["res_y"].to<int>();
				return true;
			}
			catch (...)
			{
				return false;
			}
		}

		// Save settings to a JSON file
		bool Save(std::filesystem::path const& filepath) const
		{
			try
			{
				auto dir = filepath.parent_path();
				if (!dir.empty() && !std::filesystem::exists(dir))
					std::filesystem::create_directories(dir);

				pr::json::Document doc;
				doc.root()["version"] = m_version;
				doc.root()["fullscreen"] = m_fullscreen;
				doc.root()["res_x"] = m_res_x;
				doc.root()["res_y"] = m_res_y;

				auto str = pr::json::Write(doc, pr::json::Options{ .Indent = true });
				std::ofstream file(filepath, std::ios::out);
				if (!file) return false;
				file << str;
				return true;
			}
			catch (...)
			{
				return false;
			}
		}
	};
}