//*********************************************
// Physics Terrain
//  Copyright (C) Rylogic Ltd 2026
//*********************************************
#include "pr/physics/terrain/landscape/baseline_surface.h"
#include "pr/physics/terrain/landscape/baseline_surface.hlsli"
#include "pr/physics/materials/material.h"

namespace pr::physics::terrain::landscape
{
	namespace
	{
		// Return a deterministic decorrelated field seed derived from the user-visible seed.
		uint32_t MixSeed(uint32_t seed, uint32_t salt)
		{
			auto hash = uint64_t{seed} + 0x9E3779B97F4A7C15ull + (uint64_t{salt} << 1);
			hash ^= hash >> 30;
			hash *= 0xBF58476D1CE4E5B9ull;
			hash ^= hash >> 27;
			hash *= 0x94D049BB133111EBull;
			hash ^= hash >> 31;
			return static_cast<uint32_t>(hash);
		}

		// Require one scalar configuration value to be finite.
		void RequireFinite(double value, char const* name)
		{
			if (!std::isfinite(value))
				throw std::invalid_argument(std::format("Terrain config '{}' must be finite", name));
		}

		// Require one positive scalar configuration value to be finite and strictly positive.
		void RequirePositive(double value, char const* name)
		{
			RequireFinite(value, name);
			if (value <= 0.0)
				throw std::invalid_argument(std::format("Terrain config '{}' must be greater than zero", name));
		}

		// Require one bounded octave count to stay within the supported contract.
		void RequireOctaves(int octave_count, char const* name)
		{
			if (octave_count < 1 || octave_count > BaselineSurface::MaxOctaveCount)
				throw std::invalid_argument(std::format("Terrain config '{}' must be in the range [1, {}]", name, BaselineSurface::MaxOctaveCount));
		}

		// Validate one ordinary fractal band.
		void Validate(FractalConfig const& config, char const* prefix)
		{
			RequireFinite(config.m_amplitude, std::format("{}.amplitude", prefix).c_str());
			RequirePositive(config.m_wavelength_m, std::format("{}.wavelength_m", prefix).c_str());
			RequireOctaves(config.m_octave_count, std::format("{}.octave_count", prefix).c_str());
			RequirePositive(config.m_lacunarity, std::format("{}.lacunarity", prefix).c_str());
			RequireFinite(config.m_persistence, std::format("{}.persistence", prefix).c_str());
		}

		// Validate one domain-warp band.
		void Validate(DomainWarpConfig const& config, char const* prefix)
		{
			RequireFinite(config.m_amplitude_m, std::format("{}.amplitude_m", prefix).c_str());
			RequirePositive(config.m_wavelength_m, std::format("{}.wavelength_m", prefix).c_str());
			RequireOctaves(config.m_octave_count, std::format("{}.octave_count", prefix).c_str());
			RequirePositive(config.m_lacunarity, std::format("{}.lacunarity", prefix).c_str());
			RequireFinite(config.m_persistence, std::format("{}.persistence", prefix).c_str());
		}

		// Validate one rounded-ridge fractal band.
		void Validate(RidgedFractalConfig const& config, char const* prefix)
		{
			RequireFinite(config.m_amplitude, std::format("{}.amplitude", prefix).c_str());
			RequirePositive(config.m_wavelength_m, std::format("{}.wavelength_m", prefix).c_str());
			RequireOctaves(config.m_octave_count, std::format("{}.octave_count", prefix).c_str());
			RequirePositive(config.m_lacunarity, std::format("{}.lacunarity", prefix).c_str());
			RequireFinite(config.m_persistence, std::format("{}.persistence", prefix).c_str());
			RequirePositive(config.m_roundness, std::format("{}.roundness", prefix).c_str());
			RequirePositive(config.m_weight_gain, std::format("{}.weight_gain", prefix).c_str());
		}

		// Validate the supported public baseline-surface contract before any sampling occurs.
		void Validate(BaselineSurfaceConfig const& config)
		{
			if (config.m_material_id < 0 || config.m_material_id >= Material::MaxMaterialId)
				throw std::invalid_argument(std::format("Terrain config 'material_id' must be in the range [0, {})", Material::MaxMaterialId));

			RequirePositive(config.m_supported_coordinate_abs_m, "supported_coordinate_abs_m");
			RequireFinite(config.m_sea_level_bias_m, "sea_level_bias_m");
			RequireFinite(config.m_uplift_height_m, "uplift_height_m");
			RequireFinite(config.m_mountain_base_height_m, "mountain_base_height_m");
			RequireFinite(config.m_basin_depth_m, "basin_depth_m");
			RequireFinite(config.m_basin_threshold, "basin_threshold");
			Validate(config.m_regional_base, "regional_base");
			Validate(config.m_region_selector, "region_selector");
			Validate(config.m_region_uplift, "region_uplift");
			Validate(config.m_domain_warp, "domain_warp");
			Validate(config.m_plains, "plains");
			Validate(config.m_hills, "hills");
			Validate(config.m_mountains, "mountains");
			Validate(config.m_basin_selector, "basin_selector");
		}

		// Require one query position to stay finite and within the documented supported range.
		void Validate(BaselineSurfaceConfig const& config, BaselineSurface::Position position_xy)
		{
			if (!std::isfinite(position_xy.x) || !std::isfinite(position_xy.y))
				throw std::invalid_argument("Terrain query coordinates must be finite");
			if (std::abs(position_xy.x) > config.m_supported_coordinate_abs_m || std::abs(position_xy.y) > config.m_supported_coordinate_abs_m)
				throw std::out_of_range("Terrain query coordinates exceed the supported absolute range");
		}

		// Copy a validated fractal band without quantization or a change to its seed.
		shared::BaselineBand Prepare(FractalConfig const& config, uint32_t seed)
		{
			return {
				.m_amplitude = config.m_amplitude, .m_wavelength_m = config.m_wavelength_m,
				.m_lacunarity = config.m_lacunarity, .m_persistence = config.m_persistence,
				.m_roundness = 0, .m_weight_gain = 0, .m_seed = seed, .m_octave_count = config.m_octave_count,
			};
		}
	}

	// Construct one immutable surface after validating its bounded configuration.
	BaselineSurface::BaselineSurface(BaselineSurfaceConfig config)
		: m_config(std::move(config))
		, m_recipe()
	{
		Validate(m_config);

		// Expand the public seed in the original field order and preserve every double-valued recipe parameter.
		m_recipe.m_fields[0] = Prepare(config.m_regional_base, MixSeed(config.m_seed, 1));
		m_recipe.m_fields[1] = Prepare(config.m_region_selector, MixSeed(config.m_seed, 2));
		m_recipe.m_fields[2] = Prepare(config.m_region_uplift, MixSeed(config.m_seed, 3));
		m_recipe.m_fields[3] = Prepare(config.m_plains, MixSeed(config.m_seed, 4));
		m_recipe.m_fields[4] = Prepare(config.m_hills, MixSeed(config.m_seed, 5));
		auto const& mountain = config.m_mountains;
		m_recipe.m_fields[5] = Prepare({mountain.m_amplitude, mountain.m_wavelength_m, mountain.m_octave_count, mountain.m_lacunarity, mountain.m_persistence}, MixSeed(config.m_seed, 6));
		m_recipe.m_fields[5].m_roundness = mountain.m_roundness;
		m_recipe.m_fields[5].m_weight_gain = mountain.m_weight_gain;
		auto const& warp = config.m_domain_warp;
		m_recipe.m_fields[6] = Prepare({warp.m_amplitude_m, warp.m_wavelength_m, warp.m_octave_count, warp.m_lacunarity, warp.m_persistence}, MixSeed(config.m_seed, 7));
		m_recipe.m_fields[7] = m_recipe.m_fields[6];
		m_recipe.m_fields[7].m_seed = MixSeed(config.m_seed, 8);
		m_recipe.m_fields[8] = Prepare(config.m_basin_selector, MixSeed(config.m_seed, 9));
		m_recipe.m_supported_coordinate_abs_m = config.m_supported_coordinate_abs_m;
		m_recipe.m_sea_level_bias_m = config.m_sea_level_bias_m;
		m_recipe.m_uplift_height_m = config.m_uplift_height_m;
		m_recipe.m_mountain_base_height_m = config.m_mountain_base_height_m;
		m_recipe.m_basin_depth_m = config.m_basin_depth_m;
		m_recipe.m_basin_threshold = config.m_basin_threshold;
		m_recipe.m_material_id = config.m_material_id;
	}

	// Return the validated immutable configuration that defines this surface.
	BaselineSurfaceConfig const& BaselineSurface::Config() const noexcept
	{
		return m_config;
	}

	// Return the immutable upload representation without allocating or creating a GPU context.
	shared::BaselineRecipe const& BaselineSurface::Recipe() const noexcept
	{
		return m_recipe;
	}

	// Prepare a single-precision recipe that samples positions relative to 'origin_xy'.
	shared::BaselineLocalRecipe BaselineSurface::LocalRecipe(Position origin_xy, double radius_m) const
	{
		// The sampled square must lie in the range that the CPU surface supports.
		if (!std::isfinite(origin_xy.x) || !std::isfinite(origin_xy.y))
			throw std::invalid_argument("Terrain local recipe origin must be finite");

		RequirePositive(radius_m, "local recipe radius_m");
		if (std::abs(origin_xy.x) + radius_m > m_config.m_supported_coordinate_abs_m || std::abs(origin_xy.y) + radius_m > m_config.m_supported_coordinate_abs_m)
			throw std::out_of_range("Terrain local recipe exceeds the supported absolute range");

		// Bound the domain warp offset. Each noise octave is within [-2, 2], so this bound is conservative.
		auto const& warp = m_recipe.m_fields[6];
		auto warp_reach_m = 0.0;
		auto warp_amplitude = warp.m_amplitude;
		for (int octave = 0; octave != warp.m_octave_count; ++octave)
		{
			warp_reach_m += 2 * std::abs(warp_amplitude);
			warp_amplitude *= warp.m_persistence;
		}
		auto const reach_m = std::max(std::abs(origin_xy.x), std::abs(origin_xy.y)) + radius_m + warp_reach_m;

		// Repeat BaselineFractal's double-precision frequency, amplitude and slice sequences, and split the origin's lattice phase at each octave.
		auto local = shared::BaselineLocalRecipe{};
		for (int field = 0; field != _countof(m_recipe.m_fields); ++field)
		{
			// Only the mountain band is ridged; see BaselineEvaluate.
			auto const& band = m_recipe.m_fields[field];
			auto& out = local.m_fields[field];
			auto const ridged = field == 5;
			out.m_roundness = static_cast<float>(band.m_roundness);
			out.m_weight_gain = static_cast<float>(band.m_weight_gain);
			out.m_seed = band.m_seed;
			out.m_octave_count = band.m_octave_count;
			auto frequency = double(1) / band.m_wavelength_m;
			auto amplitude = band.m_amplitude;
			auto slice = ridged
				? 0.38196601125010515 + static_cast<double>(band.m_seed) * (double(1) / double(65539))
				: 0.17320508075688773 + static_cast<double>(band.m_seed) * (double(1) / double(65537));
			for (int octave = 0; octave != band.m_octave_count; ++octave)
			{
				// Every lattice cell the shader can reach must fit in a 32-bit integer.
				if (!(reach_m * frequency < 1073741824.0))
					throw std::out_of_range("Terrain local recipe lattice coordinates exceed 32 bits");

				// Split the origin and slice into exact integer cells and single-precision fractions. A fraction that rounds up to one moves to the next cell.
				auto const z = slice + octave * (ridged ? 0.4142135623730950 : 0.6180339887498949);
				auto const cell_x = std::floor(origin_xy.x * frequency);
				auto const cell_y = std::floor(origin_xy.y * frequency);
				auto cell_z = std::floor(z);
				auto frac_z = static_cast<float>(z - cell_z);
				if (frac_z == 1.0f)
				{
					cell_z += 1;
					frac_z = 0.0f;
				}
				out.m_octaves[octave] = {
					.m_frequency = static_cast<float>(frequency),
					.m_amplitude = static_cast<float>(amplitude),
					.m_origin_cell_x = static_cast<int>(cell_x),
					.m_origin_cell_y = static_cast<int>(cell_y),
					.m_origin_frac_x = static_cast<float>(origin_xy.x * frequency - cell_x),
					.m_origin_frac_y = static_cast<float>(origin_xy.y * frequency - cell_y),
					.m_cell_z = static_cast<int>(cell_z),
					.m_frac_z = frac_z,
				};
				frequency *= band.m_lacunarity;
				amplitude *= band.m_persistence;
			}
		}

		// The height datum and blend constants are small, so single precision keeps them within the shader's rounding.
		local.m_sea_level_bias_m = static_cast<float>(m_recipe.m_sea_level_bias_m);
		local.m_uplift_height_m = static_cast<float>(m_recipe.m_uplift_height_m);
		local.m_mountain_base_height_m = static_cast<float>(m_recipe.m_mountain_base_height_m);
		local.m_basin_depth_m = static_cast<float>(m_recipe.m_basin_depth_m);
		local.m_basin_threshold = static_cast<float>(m_recipe.m_basin_threshold);
		local.m_material_id = m_recipe.m_material_id;
		return local;
	}

	// Sample one world-space XY position and return its double-precision terrain result.
	SurfaceSample BaselineSurface::Sample(Position position_xy) const
	{
		Validate(m_config, position_xy);
		auto const sample = shared::BaselineEvaluate(m_recipe, position_xy);
		if (sample.m_status != 0)
			throw std::runtime_error("Terrain evaluation exceeds supported double-precision arithmetic");

		return {.m_height = sample.m_height, .m_gradient_xy = v2d{sample.m_dx, sample.m_dy}, .m_material_id = sample.m_material_id};
	}

	// Sample a caller-owned batch in stable order without retaining caller buffers.
	void BaselineSurface::Sample(std::span<Position const> positions_xy, std::span<SurfaceSample> samples) const
	{
		if (positions_xy.size() != samples.size())
			throw std::invalid_argument("Terrain batch sampling requires equal input and output sizes");

		// Reuse the scalar evaluator so the batch boundary preserves exact scalar behaviour.
		for (auto index = size_t{}; index != positions_xy.size(); ++index)
			samples[index] = Sample(positions_xy[index]);
	}
}
