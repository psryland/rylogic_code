//************************************
// Lost at Sea
//  Copyright (c) Rylogic Ltd 2024
//************************************
// Perlin noise height field for world terrain generation.
// Height > 0 = land, height < 0 = ocean floor, ocean surface at z = 0.
#pragma once
#include "src/forward.h"

namespace las
{
	// Multi-octave Perlin noise height field
	struct HeightField
	{
		// Noise parameters
		int m_octaves;           // Number of noise octaves
		float m_base_frequency;  // Base frequency (lower = larger features)
		float m_persistence;     // Amplitude falloff per octave [0..1]
		float m_amplitude;       // Maximum height amplitude in metres
		float m_sea_level_bias;  // Bias to control land-to-water ratio (negative = more water)

		pr::PerlinNoiseGenerator<std::default_random_engine> m_noise;
		std::default_random_engine m_rng;

		explicit HeightField(uint32_t seed = 42)
			:m_octaves(6)
			,m_base_frequency(0.001f)  // ~1000m feature scale
			,m_persistence(0.5f)
			,m_amplitude(80.0f)        // Max mountain height ~80m
			,m_sea_level_bias(-0.3f)   // Ensures ~60-70% water coverage
			,m_rng(seed)
			,m_noise(m_rng)
		{}

		// Query the terrain height at a world position.
		// Returns height in metres. Positive = land, negative = underwater.
		float HeightAt(float world_x, float world_y) const
		{
			auto value = 0.0f;
			auto freq = m_base_frequency;
			auto amp = 1.0f;
			auto max_amp = 0.0f;

			for (int i = 0; i < m_octaves; ++i)
			{
				value += m_noise.Noise(world_x * freq, world_y * freq, 0.0f) * amp;
				max_amp += amp;
				amp *= m_persistence;
				freq *= 2.0f;
			}

			// Normalise to [-1, 1] and apply amplitude + bias
			value = value / max_amp;
			return (value + m_sea_level_bias) * m_amplitude;
		}

		// Query the terrain normal at a world position via central differences
		v4 NormalAt(float world_x, float world_y) const
		{
			auto eps = 1.0f; // 1m sample spacing
			auto hL = HeightAt(world_x - eps, world_y);
			auto hR = HeightAt(world_x + eps, world_y);
			auto hD = HeightAt(world_x, world_y - eps);
			auto hU = HeightAt(world_x, world_y + eps);
			return Normalise(v4(hL - hR, hD - hU, 2.0f * eps, 0.0f));
		}

		// Returns true if the given world position is above sea level
		bool IsLand(float world_x, float world_y) const
		{
			return HeightAt(world_x, world_y) > 0.0f;
		}
	};
}
