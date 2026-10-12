//*********************************************
// Physics Terrain Water
//  Copyright (C) Rylogic Ltd 2026
//*********************************************
#include "pr/physics/terrain/water/wind_sea.h"

namespace pr::physics::terrain::water
{
	// Construct a validated wind sea.
	WindSea::WindSea(WindSeaGrid const& grid, std::span<WindSeaTableRow const> table, std::span<v2 const> winds)
		: m_grid(grid)
		, m_table(table.begin(), table.end())
		, m_winds(winds.begin(), winds.end())
		, m_peak_bounds()
		, m_spread_bounds()
		, m_square_bounds()
	{
		// Validate the layout before sizing anything from it.
		if (!std::isfinite(grid.origin.x) || !std::isfinite(grid.origin.y))
			throw std::invalid_argument("Wind-sea grid origin must be finite");
		if (!std::isfinite(grid.cell_size) || !(grid.cell_size > 0.0f))
			throw std::invalid_argument("Wind-sea cell size must be finite and positive");
		if (!std::isfinite(grid.speed_step) || !(grid.speed_step > 0.0f))
			throw std::invalid_argument("Wind-sea speed step must be finite and positive");
		if (grid.dims.x < 2 || grid.dims.y < 2 || grid.band_count < 1 || grid.speed_count < 2)
			throw std::invalid_argument("Wind-sea grids need at least two nodes on each axis, one band, and two table rows");
		if (grid.band_count > shared::WaterFieldMaxElementCount)
			throw std::invalid_argument(std::format("Wind seas support at most {} bands", shared::WaterFieldMaxElementCount));
		if (grid.level_count < 1 || grid.level_count > grid.band_count)
			throw std::invalid_argument("Wind-sea level count must be in [1, band_count]");
		if (std::ssize(table) != static_cast<int64_t>(grid.band_count) * grid.speed_count)
			throw std::invalid_argument(std::format("Wind-sea table must hold {} rows", static_cast<int64_t>(grid.band_count) * grid.speed_count));
		if (std::ssize(winds) != static_cast<int64_t>(grid.dims.x) * grid.dims.y * grid.level_count)
			throw std::invalid_argument(std::format("Wind-sea winds must hold {} vectors", static_cast<int64_t>(grid.dims.x) * grid.dims.y * grid.level_count));

		// Table values must be usable by every evaluator.
		for (auto const& row : table)
		{
			if (!std::isfinite(row.x) || row.x < 0.0f || !std::isfinite(row.y) || row.y < 0.0f || !std::isfinite(row.w) || row.w < 0.0f)
				throw std::invalid_argument("Wind-sea table rows must be finite and non-negative");
			if (!(row.z >= 0.0f && row.z <= shared::WaterFieldMaxCrestSharpness))
				throw std::invalid_argument(std::format("Wind-sea crest sharpness must be in [0, {}]", shared::WaterFieldMaxCrestSharpness));
		}

		// Find the strongest wind of each level. A blend of node winds is never stronger than the strongest node.
		auto level_speed = std::vector<float>(grid.level_count, 0.0f);
		for (auto i = 0LL; i != std::ssize(winds); ++i)
		{
			auto const& wind = winds[i];
			if (!std::isfinite(wind.x) || !std::isfinite(wind.y))
				throw std::invalid_argument("Wind-sea winds must be finite");

			auto& speed = level_speed[i % grid.level_count];
			speed = std::max(speed, Length(wind));
		}

		// A band's wind blends its two levels, so it is never stronger than the stronger of them.
		auto max_speed = std::vector<float>(grid.band_count, 0.0f);
		for (int b = 0; b != grid.band_count; ++b)
		{
			auto l1 = 0;
			auto frac = 0.0f;
			auto const l0 = shared::WindSeaLevelIndex(grid, b, l1, frac);
			max_speed[b] = std::max(level_speed[l0], level_speed[l1]);
		}

		// Each band's bounds are the largest over the table rows that its winds can reach. Local values interpolate between two rows, so the
		// row after the strongest wind's row is included.
		m_peak_bounds.resize(grid.band_count);
		m_spread_bounds.resize(grid.band_count);
		m_square_bounds.resize(grid.band_count);
		for (int b = 0; b != grid.band_count; ++b)
		{
			auto const last = std::min(static_cast<int>(max_speed[b] / grid.speed_step) + 1, grid.speed_count - 1);
			for (int r = 0; r <= last; ++r)
			{
				// The summed squares over evenly spread directions exceed 'w' by at most the largest term, which is one, because the spread
				// has a single peak. See WindSeaTableRow for the meaning of each row value.
				auto const& row = m_table[b * grid.speed_count + r];
				auto const crest = row.x * (1.0f + row.z);
				m_peak_bounds[b] = std::max(m_peak_bounds[b], crest);
				m_spread_bounds[b] = std::max(m_spread_bounds[b], crest * std::sqrt(1.0f + row.w));
				m_square_bounds[b] = std::max(m_square_bounds[b], row.x * row.x * row.w);
			}
		}
	}

	// The grid layout.
	WindSeaGrid const& WindSea::Grid() const noexcept
	{
		return m_grid;
	}

	// The table rows, band by band.
	std::span<WindSeaTableRow const> WindSea::Table() const noexcept
	{
		return m_table;
	}

	// The node winds.
	std::span<v2 const> WindSea::Winds() const noexcept
	{
		return m_winds;
	}

	// Per-band bounds over every position.
	float WindSea::PeakBound(int band) const
	{
		return m_peak_bounds.at(band);
	}
	float WindSea::SpreadBound(int band) const
	{
		return m_spread_bounds.at(band);
	}
	float WindSea::SquareBound(int band) const
	{
		return m_square_bounds.at(band);
	}

	// Write the state of every band at 'xy'.
	void WindSea::Bands(v2 xy, std::span<WindSeaBand> bands) const
	{
		assert(std::ssize(bands) == m_grid.band_count);

		// Blend the node winds of each level.
		auto const cell = shared::WindSeaLocate(m_grid, xy);
		auto const n = m_grid.level_count;
		auto levels = std::array<v2, shared::WaterFieldMaxElementCount>{};
		for (int l = 0; l != n; ++l)
			levels[l] = shared::WindSeaBlendWind(cell, m_winds[cell.index.x * n + l], m_winds[cell.index.y * n + l], m_winds[cell.index.z * n + l], m_winds[cell.index.w * n + l]);

		// Interpolate each band's wind between its levels, then interpolate its table rows at that wind's speed.
		for (int b = 0; b != m_grid.band_count; ++b)
		{
			auto l1 = 0;
			auto level_frac = 0.0f;
			auto const l0 = shared::WindSeaLevelIndex(m_grid, b, l1, level_frac);
			auto const wind = levels[l0] + (levels[l1] - levels[l0]) * level_frac;
			auto frac = 0.0f;
			auto const row = shared::WindSeaTableRowIndex(m_grid, b, Length(wind), frac);
			bands[b] = shared::WindSeaBandState(wind, m_table[row], m_table[row + 1], frac);
		}
	}
}
