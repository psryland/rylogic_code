//*********************************************
// View 3d
//  Copyright (c) Rylogic Ltd 2022
//*********************************************
#pragma once
#include "pr/view3d-12/forward.h"
#include "pr/view3d-12/resource/image.h"

namespace pr::rdr12
{
	// Notes:
	//  - These functions convert image files into 'ImageWithData' objects,
	//    *not* into ID3D12Resources because resource initialisation requires command lists.
	//  - LoadWIC no longer automatically generates mip-maps. A function on the ResourceManager will have to do it.
	//  - DDS textures can contain arrays of images, where as WIC images are simple 2D bitmaps.
	//  - WIC functions support arrays by filepath pattern or by array of raw data. Array textures all have the same dimensions.
	
	// Use structured binding. i.e. auto [images, desc] = LoadDDS(...);
	struct LoadedImageResult
	{
		pr::vector<ImageWithData> images;
		D3D12_RESOURCE_DESC desc;
	};

	// True if 'data' points at DDS data (probably)
	bool IsDDSData(std::span<uint8_t const> img);

	// Load an image from a DDS image, either in memory or on disk.
	LoadedImageResult LoadDDS(std::span<uint8_t const> mem, int mips = 0, bool is_cube_map = false, int max_dimension = 0);
	LoadedImageResult LoadDDS(std::filesystem::path const& filepath, int mips = 0, bool is_cube_map = false, int max_dimension = 0);

	// Load an image from a WIC image, either in memory or on disk.
	LoadedImageResult LoadWIC(std::span<std::span<uint8_t const>> const& images, int mips = 0, int max_dimension = 0, FeatureSupport const* features = nullptr);
	LoadedImageResult LoadWIC(std::span<std::filesystem::path> const& filepaths, int mips = 0, int max_dimension = 0, FeatureSupport const* features = nullptr);

	// Load 'DDS, JPG, PNG, TGA, GIF, or BMP' image data, either from memory or disk.
	// 'images' is an array of equal sized images.
	// 'filepaths' is a sorted list of image files that make up the elements in a texture array or cube map.
	// 'pattern' is a single filepath, or a regex expression for multiple images that form and array.
	// DDS images natively support cube maps and array textures so only single DDS images are supported. (See Texassemble.exe for creating DDS textures)
	// Cube maps, created from non-DDS textures, should use the naming convention: <name_??.png>.
	// The first '?' is the sign, the second is the axis, e.g. "my_cube_??.png" finds "my_cube_+x.png" .. "my_cube_-z.png"
	// Use 'img_(\+|\-)(x|y|z)\.png' as the regex pattern
	inline LoadedImageResult LoadImageData(std::span<std::span<uint8_t const>> const& images, int mips = 0, bool is_cube_map = false, int max_dimension = 0, FeatureSupport const* features = nullptr)
	{
		if (images.empty())
			throw std::runtime_error("At least one image is required");

		// If the data is a DDS file, use the faster DDS loader.
		// This does not support some DDS formats tho, so might be worth trying the 'directxtex' DDS loader
		if (IsDDSData(images[0]))
		{
			if (images.size() != 1)
				throw std::runtime_error("Only single DDS textures are supported since they natively support texture arrays and cube maps");

			return std::move(LoadDDS(images[0], mips, is_cube_map, max_dimension));
		}
		else
		{
			if (is_cube_map && images.size() != 6)
				throw std::runtime_error("Expected 6 images for a cube map");

			return std::move(LoadWIC(images, mips, max_dimension, features));
		}
	}
	inline LoadedImageResult LoadImageData(std::span<uint8_t const> data, int mips = 0, bool is_cube_map = false, int max_dimension = 0, FeatureSupport const* features = nullptr)
	{
		return std::move(LoadImageData(std::span{&data, 1}, mips, is_cube_map, max_dimension, features));
	}
	inline LoadedImageResult LoadImageData(std::span<std::filesystem::path> const& filepaths, int mips = 0, bool is_cube_map = false, int max_dimension = 0, FeatureSupport const* features = nullptr)
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

			return std::move(LoadDDS(filepaths[0], mips, is_cube_map, max_dimension));
		}
		else
		{
			if (is_cube_map && filepaths.size() != 6)
				throw std::runtime_error("Expected 6 images for a cube map");

			return std::move(LoadWIC(filepaths, mips, max_dimension, features));
		}
	}
	inline LoadedImageResult LoadImageData(std::filesystem::path const& filepath, int mips = 0, bool is_cube_map = false, int max_dimension = 0, FeatureSupport const* features = nullptr)
	{
		pr::vector<std::filesystem::path> paths;
		if (is_cube_map)
		{
			auto pattern = filepath.wstring();
			auto idx = pattern.find(L"??");
			if (idx == std::wstring::npos)
				throw std::runtime_error("Expected cube-map texture filepath pattern to contain '??'");

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
			return std::move(LoadImageData(paths, mips, true, max_dimension, features));
		}
		else
		{
			paths.push_back(filepath);
			return std::move(LoadImageData(paths, mips, false, max_dimension, features));
		}
	}
}