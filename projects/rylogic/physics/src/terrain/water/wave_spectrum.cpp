//*********************************************
// Physics Terrain Water
//  Copyright (C) Rylogic Ltd 2026
//*********************************************
#include "pr/physics/terrain/water/wave_spectrum.h"

namespace pr::physics::terrain::water
{
	namespace
	{
		// Fractional part of the golden ratio. Successive multiples of it spread evenly over [0,1), so successive bands get different offsets.
		constexpr double GoldenFraction = 0.61803398874989485;

		// Full circle in radians.
		constexpr double Tau = math::constants<double>::tau;

		// Directional spreading exponent at the spectrum peak. Larger values concentrate the wave energy closer to the wind direction.
		constexpr double PeakSpreading = 12.0;

		// Wind speed (m/s) above which every band is held to a minimum spreading exponent, and the growth of that minimum per m/s.
		constexpr double GaleSpreadingOnset = 8.0;
		constexpr double GaleSpreadingRate = 0.4;

		// Return a well-mixed 32-bit hash of 'value'.
		uint32_t Hash(uint32_t value)
		{
			// Integer finaliser from MurmurHash3; every input bit affects every output bit.
			value ^= value >> 16;
			value *= 0x85EBCA6Bu;
			value ^= value >> 13;
			value *= 0xC2B2AE35u;
			value ^= value >> 16;
			return value;
		}
	}

	// Build the fixed component table.
	WaveSpectrum::WaveSpectrum(WaveSpectrumLayout const& layout)
		: m_layout(layout)
		, m_components()
	{
		if (!std::isfinite(layout.m_gravity) || !(layout.m_gravity > 0.0f))
			throw std::invalid_argument("Wave spectrum gravity must be finite and positive");
		if (!std::isfinite(layout.m_repeat_period) || !(layout.m_repeat_period > 0.0f))
			throw std::invalid_argument("Wave spectrum repeat period must be finite and positive");
		if (!std::isfinite(layout.m_min_wavelength) || !std::isfinite(layout.m_max_wavelength) || !(layout.m_min_wavelength > 0.0f) || !(layout.m_max_wavelength > layout.m_min_wavelength))
			throw std::invalid_argument("Wave spectrum wavelengths must be finite with 0 < min < max");
		if (layout.m_bands < 1 || layout.m_directions < 1 || static_cast<int64_t>(layout.m_bands) * layout.m_directions > MaxElementCount)
			throw std::invalid_argument(std::format("Wave spectrum bands and directions must be positive with at most {} components", MaxElementCount));

		// Deep-water dispersion gives omega = sqrt(g*k). Rounding omega to a whole multiple of the base frequency makes the surface periodic;
		// the change in speed is under one percent for every component in the supported range.
		auto const gravity = static_cast<double>(layout.m_gravity);
		auto const base_frequency = Tau / static_cast<double>(layout.m_repeat_period);
		auto const ratio = static_cast<double>(layout.m_max_wavelength) / layout.m_min_wavelength;
		auto const bands = layout.m_bands;
		auto const directions = layout.m_directions;
		m_components.resize(static_cast<size_t>(bands) * directions);
		for (int b = 0; b != bands; ++b)
		{
			// Each band spreads its directions evenly around the full circle, so waves can follow a wind from any direction.
			// The directions are turned by a different fraction of their spacing in each band so that neighbouring bands do not travel in the same directions.
			auto const jitter = std::fmod(b * GoldenFraction, 1.0);
			for (int d = 0; d != directions; ++d)
			{
				// Give each direction its own wavelength within the band.
				auto const i = b * directions + d;
				auto const wavelength = layout.m_min_wavelength * std::pow(ratio, (b + (d + 0.5) / directions) / bands);
				auto const k = Tau / wavelength;
				auto const harmonic = std::max(std::round(std::sqrt(gravity * k) / base_frequency), 1.0);
				auto const angle = (d + jitter) * Tau / directions;
				m_components[i] = WaveComponent{
					.m_direction = v2{static_cast<float>(std::cos(angle)), static_cast<float>(std::sin(angle))},
					.m_wavelength = static_cast<float>(wavelength),
					.m_angular_frequency = static_cast<float>(harmonic * base_frequency),
					.m_phase = static_cast<float>(Tau * (Hash(static_cast<uint32_t>(i) + 0x9E3779B9u) / 4294967296.0)),
				};
			}
		}
	}

	// The layout used to build the components.
	WaveSpectrumLayout const& WaveSpectrum::Layout() const noexcept
	{
		return m_layout;
	}

	// The number of components.
	int WaveSpectrum::ComponentCount() const noexcept
	{
		return static_cast<int>(m_components.size());
	}

	// The fixed components, band by band from the shortest to the longest wavelength.
	std::span<WaveComponent const> WaveSpectrum::Components() const noexcept
	{
		return m_components;
	}

	// Write the wind-sea table for 'fetch'.
	void WaveSpectrum::SeaTable(float fetch, float speed_step, std::span<shared::WindSeaTableRow> rows) const
	{
		auto const bands = m_layout.m_bands;
		if (!std::isfinite(fetch) || !(fetch > 0.0f))
			throw std::invalid_argument("Wind fetch must be finite and positive");
		if (!std::isfinite(speed_step) || !(speed_step > 0.0f))
			throw std::invalid_argument("Wind-sea speed step must be finite and positive");
		if (std::ssize(rows) % bands != 0 || std::ssize(rows) / bands < 2)
			throw std::invalid_argument(std::format("Wind-sea table must hold at least two rows for each of {} bands", bands));

		// Each band's frequency range runs from its long end to its short end, because longer wavelengths have lower frequencies.
		auto const g = static_cast<double>(m_layout.m_gravity);
		auto const ratio = static_cast<double>(m_layout.m_max_wavelength) / m_layout.m_min_wavelength;
		auto const directions = static_cast<double>(m_layout.m_directions);
		auto const speed_count = static_cast<int>(std::ssize(rows) / bands);
		for (int b = 0; b != bands; ++b)
		{
			auto const omega_lo = std::sqrt(g * Tau / (m_layout.m_min_wavelength * std::pow(ratio, (b + 1.0) / bands)));
			auto const omega_hi = std::sqrt(g * Tau / (m_layout.m_min_wavelength * std::pow(ratio, static_cast<double>(b) / bands)));
			for (int r = 0; r != speed_count; ++r)
			{
				// Calm air raises no waves.
				auto& row = rows[b * speed_count + r];
				auto const wind = static_cast<double>(r) * speed_step;
				if (wind < 0.1)
				{
					row = shared::WindSeaTableRow{0, 0, 0, 0};
					continue;
				}

				// Fetch-limited wind-sea (JONSWAP) parameters from the dimensionless fetch X = g*F/U². Short fetches give a higher, sharper peak
				// frequency. Long fetches approach the fully developed sea, which bounds both the peak frequency and the energy scale from below.
				auto const x = g * fetch / (wind * wind);
				auto const peak = std::max(22.0 * (g / wind) * std::pow(x, -1.0 / 3.0), 0.855 * g / wind);
				auto const alpha = std::max(0.076 * std::pow(x, -0.22), 0.0081);
				auto const gamma = 3.3;
				auto const density = [=](double omega)
				{
					// Spectral density S(omega) in m²·s.
					auto const sigma = omega <= peak ? 0.07 : 0.09;
					auto const shape = std::exp(-(omega - peak) * (omega - peak) / (2.0 * sigma * sigma * peak * peak));
					return alpha * g * g * std::pow(omega, -5.0) * std::exp(-1.25 * std::pow(peak / omega, 4.0)) * std::pow(gamma, shape);
				};

				// The band carries the variance of its whole frequency range, found by midpoint integration in log frequency. The spectrum peak
				// can be narrower than a band, so the integration samples the band finely.
				constexpr int SamplesPerBand = 32;
				auto const step = std::log(omega_hi / omega_lo) / SamplesPerBand;
				auto variance = 0.0;
				for (int s = 0; s != SamplesPerBand; ++s)
				{
					// d(omega) = omega * d(ln omega).
					auto const omega = omega_lo * std::exp((s + 0.5) * step);
					variance += density(omega) * omega * step;
				}

				// The variance is spread over direction in proportion to cos(delta/2)^(2s) for angles 'delta' from downwind (the Mitsuyasu form).
				// The exponent 's' is largest at the spectrum peak, so waves near the peak are the most closely aligned with the wind; longer swell
				// and shorter ripples spread more widely. In strong winds the peak can be longer than every band, which would leave all of them
				// widely spread, so 's' has a minimum that grows with the wind above a fresh breeze and keeps a clear dominant direction in a gale.
				auto const omega = std::sqrt(omega_lo * omega_hi);
				auto const spreading = PeakSpreading * (omega <= peak ? std::pow(omega / peak, 5.0) : std::pow(omega / peak, -2.5));
				auto const s = std::max(spreading, GaleSpreadingRate * std::max(wind - GaleSpreadingOnset, 0.0));

				// An element's amplitude is the square root of its share of the variance, so it follows cos(delta/2)^s. Summed over directions
				// spaced evenly around the circle, cos(delta/2)^(2s) is close to N * Gamma(s + 1/2) / (sqrt(pi) * Gamma(s + 1)) whatever the wind
				// direction, because the spread is smooth compared with the spacing. A sine wave of amplitude A has variance A²/2.
				auto const spread_sum = directions * std::exp(std::lgamma(s + 0.5) - std::lgamma(s + 1.0)) / std::sqrt(Tau / 2);
				row = shared::WindSeaTableRow{
					static_cast<float>(std::sqrt(2.0 * variance / spread_sum)),
					static_cast<float>(0.5 * s),
					CrestSharpness(static_cast<float>(wind)),
					static_cast<float>(spread_sum),
				};
			}
		}
	}

	// Write band-tagged Gerstner elements for the selected components.
	int WaveSpectrum::Elements(std::span<shared::WindSeaTableRow const> table, float min_wavelength, std::span<WaterFieldElement> elements) const
	{
		auto const bands = m_layout.m_bands;
		auto const directions = m_layout.m_directions;
		if (std::ssize(table) % bands != 0 || std::ssize(table) / bands < 2)
			throw std::invalid_argument(std::format("Wind-sea table must hold at least two rows for each of {} bands", bands));
		if (!std::isfinite(min_wavelength))
			throw std::invalid_argument("Minimum wavelength must be finite");

		// Skip components that cannot contribute so consumers loop over as few elements as possible.
		auto const speed_count = std::ssize(table) / bands;
		auto count = 0;
		for (int b = 0; b != bands; ++b)
		{
			// The band's largest amplitude and sharpness bound every local value, because local values interpolate between table rows.
			auto amplitude = 0.0f;
			auto sharpness = 0.0f;
			for (auto const& row : table.subspan(b * speed_count, speed_count))
			{
				if (!std::isfinite(row.x) || row.x < 0.0f || !std::isfinite(row.y) || row.y < 0.0f || !(row.z >= 0.0f && row.z <= shared::WaterFieldMaxCrestSharpness) || !std::isfinite(row.w) || row.w < 0.0f)
					throw std::invalid_argument("Wind-sea table rows must be finite and non-negative, with sharpness in range");

				amplitude = std::max(amplitude, row.x);
				sharpness = std::max(sharpness, row.z);
			}

			// Bands that stay below a millimetre in every wind are invisible and do not move bodies.
			constexpr float MinAmplitude = 0.001f;
			if (amplitude < MinAmplitude)
				continue;

			for (int d = 0; d != directions; ++d)
			{
				auto const& component = m_components[b * directions + d];
				if (component.m_wavelength < min_wavelength)
					continue;
				if (count == std::ssize(elements))
					throw std::invalid_argument("Wave element output is too small");

				// The phase speed is omega/k so the element's time term is exactly the quantised frequency.
				auto const phase_speed = component.m_angular_frequency * component.m_wavelength / static_cast<float>(Tau);
				auto element = GerstnerWave(component.m_direction, amplitude, component.m_wavelength, phase_speed, 0.0f, sharpness, component.m_phase);
				element.info.y = b + 1;
				elements[count++] = element;
			}
		}
		return count;
	}

	// Move 'values' towards 'targets'.
	void WaveSpectrum::Relax(std::span<float> values, std::span<float const> targets, float dt, float time_constant)
	{
		if (values.size() != targets.size())
			throw std::invalid_argument("Relaxation values and targets must have the same length");
		if (!std::isfinite(dt) || dt < 0.0f)
			throw std::invalid_argument("Relaxation time step must be finite and non-negative");
		if (!std::isfinite(time_constant) || time_constant < 0.0f)
			throw std::invalid_argument("Relaxation time constant must be finite and non-negative");

		// An exact exponential step is stable for any time step, and a zero time constant jumps straight to the targets.
		auto const blend = time_constant > 0.0f ? 1.0f - std::exp(-dt / time_constant) : 1.0f;
		for (size_t i = 0; i != values.size(); ++i)
			values[i] += (targets[i] - values[i]) * blend;
	}

	// Return the crest sharpness of wind-driven waves.
	float WaveSpectrum::CrestSharpness(float wind_speed)
	{
		if (!std::isfinite(wind_speed) || wind_speed < 0.0f)
			throw std::invalid_argument("Wind speed must be finite and non-negative");

		// Sharpness starts at the calm-sea threshold and approaches its limit with a 6 m/s scale, so moderate winds already add visible chop.
		constexpr auto calm_wind_speed = 4.0f;
		constexpr auto wind_speed_scale = 6.0f;
		if (wind_speed <= calm_wind_speed)
			return 0.0f;

		return MaxCrestSharpness * (1.0f - std::exp(-(wind_speed - calm_wind_speed) / wind_speed_scale));
	}
}