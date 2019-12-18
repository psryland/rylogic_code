//*********************************************
// Renderer
//  Copyright (c) Rylogic Ltd 2012
//*********************************************
#pragma once

#include "pr/view3d/forward.h"
#include "pr/view3d/util/wrappers.h"

namespace pr::rdr
{
	// Notes:
	//  - These functions create ID3D11Resources.
	//    Use this to get the texture interface:
	//       D3DPtr<ID3D11Texture2D> tex;
	//       Throw(res->QueryInterface(__uuidof(ID3D11Texture2D), (void**)&tex.m_ptr));
	//  - tdesc is an output for these functions
	//  - DDS textures can contain arrays of images, where as WIC images are simple 2D bitmaps.
	//    WIC functions support arrays by filepath pattern or by array of raw data. Array textures
	//    all have the same dimensions.
	using ImageBytes = struct
	{
		union { void const* data; uint8_t const* bytes; };
		size_t size;
	};

	// Create a DX texture from a DDS image file, either in memory or on disk
	void CreateDDSTextureFromMemory(ID3D11Device* d3d_device, ImageBytes img, int mips, bool is_cube_map, TextureDesc& tdesc, D3DPtr<ID3D11Resource>& res, D3DPtr<ID3D11ShaderResourceView>& srv, size_t max_dimension = 0);
	void CreateDDSTextureFromFile(ID3D11Device* d3d_device, std::filesystem::path const& filepath, int mips, bool is_cube_map, TextureDesc& tdesc, D3DPtr<ID3D11Resource>& res, D3DPtr<ID3D11ShaderResourceView>& srv, size_t max_dimension = 0);

	// Create a DX texture from an image file, either in memory or on disk
	void CreateWICTextureFromMemory(ID3D11Device* d3d_device, std::span<ImageBytes> images, int mips, bool is_cube_map, TextureDesc& tdesc, D3DPtr<ID3D11Resource>& res, D3DPtr<ID3D11ShaderResourceView>& srv, size_t max_dimension = 0);
	void CreateWICTextureFromFiles(ID3D11Device* d3d_device, vector<std::filesystem::path> const& filepaths, int mips, bool is_cube_map, TextureDesc& tdesc, D3DPtr<ID3D11Resource>& res, D3DPtr<ID3D11ShaderResourceView>& srv, size_t max_dimension = 0);

	// Create a DX texture from a 'DDS,JPG,PNG,TGA,GIF,BMP' image file, either in memory or on disk.
	// 'images' is an array of equal sized images.
	// 'filepaths' is a sorted list of image files that make up the elements in a texture array or cube map.
	// 'pattern' is a single filepath, or a regex expression for multiple images that form and array.
	// DDS images natively support cube maps and array textures so only single DDS images are supported. (See Texassemble.exe for creating DDS textures)
	// Cube maps, created from non-DDS textures, should use the naming convention: <name_??.png>.
	// The first '?' is the sign, the second is the axis, e.g. "my_cube_??.png" finds "my_cube_+x.png" .. "my_cube_-z.png"
	// Use 'img_(\+|\-)(x|y|z)\.png' as the regex pattern
	inline void CreateTextureFromMemory(ID3D11Device* device, std::span<ImageBytes> images, int mips, bool is_cube_map, TextureDesc& tdesc, D3DPtr<ID3D11Resource>& res, D3DPtr<ID3D11ShaderResourceView>& srv, size_t max_dimension = 0)
	{
		if (images.empty())
			throw std::runtime_error("At least one image is required");

		// If the data is a DDS file, use the faster DDS loader
		// This does not support some DDS formats tho, so might be worth trying the 'directxtex' DDS loader
		extern bool IsDDSData(ImageBytes img);
		if (IsDDSData(images[0]))
		{
			if (images.size() != 1)
				throw std::runtime_error("Only single DDS textures are supported since they natively support texture arrays and cube maps");

			CreateDDSTextureFromMemory(device, images[0], mips, is_cube_map, tdesc, res, srv, max_dimension);
		}
		else
		{
			if (is_cube_map && images.size() != 6)
				throw std::runtime_error("Expected 6 images for a cube map");

			CreateWICTextureFromMemory(device, images, mips, is_cube_map, tdesc, res, srv, max_dimension);
		}
	}
	inline void CreateTextureFromMemory(ID3D11Device* device, ImageBytes data, int mips, bool is_cube_map, TextureDesc& tdesc, D3DPtr<ID3D11Resource>& res, D3DPtr<ID3D11ShaderResourceView>& srv, size_t max_dimension = 0)
	{
		CreateTextureFromMemory(device, std::span{ &data, 1 }, mips, is_cube_map, tdesc, res, srv, max_dimension);
	}
	inline void CreateTextureFromFile(ID3D11Device* device, vector<std::filesystem::path> const& filepaths, int mips, bool is_cube_map, TextureDesc& tdesc, D3DPtr<ID3D11Resource>& res, D3DPtr<ID3D11ShaderResourceView>& srv, size_t max_dimension = 0)
	{
		if (filepaths.empty())
			throw std::runtime_error("At least one image is required");

		// If the file is a DDS file, use the faster DDS loader.
		// This does not support some DDS formats tho, so might be worth trying the 'directxtex' DDS loader
		auto extn = filepaths[0].extension();
		auto dds = _wcsicmp(extn.c_str(), L"dds") == 0;

		if (dds)
		{
			if (filepaths.size() != 1)
				throw std::runtime_error("Only single DDS textures are supported since they natively support texture arrays and cube maps");

			CreateDDSTextureFromFile(device, filepaths[0], mips, is_cube_map, tdesc, res, srv, max_dimension);
		}
		else
		{
			if (is_cube_map && filepaths.size() != 6)
				throw std::runtime_error("Expected 6 images for a cube map");

			CreateWICTextureFromFiles(device, filepaths, mips, is_cube_map, tdesc, res, srv, max_dimension);
		}
	}
	inline void CreateTextureFromFile(ID3D11Device* device, std::filesystem::path const& filepath, int mips, bool is_cube_map, TextureDesc& tdesc, D3DPtr<ID3D11Resource>& res, D3DPtr<ID3D11ShaderResourceView>& srv, size_t max_dimension = 0)
	{
		vector<std::filesystem::path> paths;
		if (is_cube_map)
		{
			auto pattern = filepath.wstring();
			auto idx = pattern.find(L"??");
			if (idx == std::wstring::npos)
				throw std::runtime_error("Expected cubemap texture filepath pattern to contain '??'");

			// Create the collection of filepaths in the required order
			for (auto face : { L"+x", L"-x", L"+y", L"-y", L"+z", L"-z" })
			{
				pattern[idx + 0] = face[0];
				pattern[idx + 1] = face[1];
				std::filesystem::path path = pattern;
				if (!std::filesystem::exists(path))
					throw std::runtime_error(FmtS("Cube map face %S does not exist (%S)", face, filepath.c_str()));

				paths.push_back(path);
			}

			// Create the cube map
			CreateTextureFromFile(device, paths, mips, true, tdesc, res, srv, max_dimension);
		}
		else
		{
			paths.push_back(filepath);
			CreateTextureFromFile(device, paths, mips, false, tdesc, res, srv, max_dimension);
		}
	}

	// Return an ordered list of filepaths based on 'pattern'
	inline vector<std::filesystem::path> PatternToPaths(std::filesystem::path const& dir, char const* pattern)
	{
		using namespace std::filesystem;

		vector<path> paths;

		// Assume the pattern is in the filename only
		auto pat = std::regex(pattern, std::regex::flag_type::icase);
		for (auto& entry : directory_iterator(dir))
		{
			if (!std::regex_match(entry.path().filename().u8string(), pat)) continue;
			paths.push_back(entry.path());
		}

		// Sort the paths lexically
		pr::sort(paths);
		return std::move(paths);
	}
}
