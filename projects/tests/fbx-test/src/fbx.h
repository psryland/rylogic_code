//*************************************************************************************************
// Fbx
// Rylogic (C) 2025
//*************************************************************************************************
#pragma once
#include <vector>
#include <string>
#include <filesystem>

namespace fbxsdk
{
	class FbxManager;
	class FbxIOSettings;
	class FbxScene;
	class FbxExporter;
	class FbxImporter;
	struct FbxVersion { int Major = 0; int Minor = 0; int Revs = 0; };
};

namespace pr::fbx
{
	struct Manager
	{
		fbxsdk::FbxManager* m_manager;
		char const* m_version;

		Manager();
		~Manager();
		operator fbxsdk::FbxManager* () const { return m_manager; }
	};

	struct Settings
	{
		// This is used for import and export.
		fbxsdk::FbxIOSettings* m_settings;

		explicit Settings(Manager& manager);
		~Settings();
		operator fbxsdk::FbxIOSettings* () const { return m_settings; }

		// Get/set the password
		std::string Password() const;
		void Password(std::string_view password);
	};

	struct Scene
	{
		// This object holds most objects imported/exported from/to files.
		fbxsdk::FbxScene* m_scene;

		explicit Scene(Manager& manager);
		Scene(Scene&& rhs);
		Scene(Scene const&) = delete;
		Scene& operator=(Scene&& rhs);
		Scene& operator=(Scene const&) = delete;
		~Scene();
		operator fbxsdk::FbxScene* () const { return m_scene; }
	};

	struct Exporter
	{
		Manager& m_manager;
		fbxsdk::FbxExporter* m_exporter;

		Exporter(Manager& manager, Settings const& settings);
		Exporter(Exporter&&) = delete;
		Exporter(Exporter const&) = delete;
		Exporter& operator=(Exporter&&) = delete;
		Exporter& operator=(Exporter const&) = delete;
		~Exporter();
		operator fbxsdk::FbxExporter* () const { return m_exporter; }

		// Export a fbx scene
		void Export(Scene& scene, std::filesystem::path const& filepath, int format = -1);
	};

	struct Importer
	{
		Manager& m_manager;
		fbxsdk::FbxImporter* m_importer;

		Importer(Manager& manager, Settings const& settings);
		Importer(Importer&&) = delete;
		Importer(Importer const&) = delete;
		Importer& operator=(Importer&&) = delete;
		Importer& operator=(Importer const&) = delete;
		~Importer();
		operator fbxsdk::FbxImporter* () const { return m_importer; }

		
		// Load an fbx scene
		Scene Import(std::filesystem::path const& filepath, std::vector<std::string>* errors = nullptr);
	};

	// Dump the contents of a fbx into 'out'
	void WriteContent(Scene const& scene, std::ostream& out);
}
