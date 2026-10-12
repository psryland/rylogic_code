//*********************************************
// Physics Terrain Water
//  Copyright (C) Rylogic Ltd 2026
//*********************************************
#pragma once
#include "pr/physics/terrain/forward.h"
#include "pr/physics/terrain/water/wind_sea.hlsli"

namespace pr::physics::terrain::water
{
	using WindSeaGrid = shared::WindSeaGrid;
	using WindSeaTableRow = shared::WindSeaTableRow;
	using WindSeaBand = shared::WindSeaBand;

	// An immutable wind sea: a grid of winds and the table that turns a band's wind into wave amplitudes. See wind_sea.hlsli.
	// The CPU evaluation here matches the stage-neutral HLSL, so CPU and GPU consumers see the same waves.
	class WindSea
	{
		WindSeaGrid m_grid;
		std::vector<WindSeaTableRow> m_table;
		std::vector<v2> m_winds;
		std::vector<float> m_peak_bounds;
		std::vector<float> m_spread_bounds;
		std::vector<float> m_square_bounds;

	public:

		// Construct a validated wind sea. 'table' holds grid.band_count * grid.speed_count rows and 'winds' holds
		// grid.dims.x * grid.dims.y * grid.level_count wind vectors (m/s), laid out as described in wind_sea.hlsli.
		// Throws invalid_argument for non-finite values, a non-positive cell size or speed step, fewer than two nodes or rows on an axis,
		// a level count outside [1, band_count], negative table values, or crest sharpness outside [0, WaterFieldMaxCrestSharpness].
		WindSea(WindSeaGrid const& grid, std::span<WindSeaTableRow const> table, std::span<v2 const> winds);

		// The grid layout.
		WindSeaGrid const& Grid() const noexcept;

		// The table rows, band by band.
		std::span<WindSeaTableRow const> Table() const noexcept;

		// The node winds, node by node with grid.level_count winds per node.
		std::span<v2 const> Winds() const noexcept;

		// Upper bounds for 'band' over every position and time. 'PeakBound' bounds the crest height, amplitude * (1 + sharpness), of one element.
		// 'SpreadBound' bounds the root sum of squares of the crest heights of the band's elements, for elements whose directions are a subset
		// of directions spread evenly around the circle with the table's direction count. 'SquareBound' estimates the sum of the squared
		// amplitudes of the band's elements, which is twice the band's variance.
		float PeakBound(int band) const;
		float SpreadBound(int band) const;
		float SquareBound(int band) const;

		// Write the state of every band at 'xy' into 'bands' (grid.band_count values).
		void Bands(v2 xy, std::span<WindSeaBand> bands) const;
	};
}
