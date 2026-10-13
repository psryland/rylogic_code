//*********************************************
// Physics Terrain
//  Copyright (C) Rylogic Ltd 2026
//*********************************************
#ifndef PR_TERRAIN_BASELINE_TYPES_HLSLI
#define PR_TERRAIN_BASELINE_TYPES_HLSLI
#ifdef __cplusplus
#include "pr/physics/terrain/forward.h"
namespace pr::physics::terrain::landscape::shared
{
	using uint = uint32_t;
#endif

// Bound all per-field iteration in both language front ends.
static const int BaselineMaxOctaveCount = 8;

// One field in a prepared recipe; scalar layout is identical in C++ and StructuredBuffer HLSL.
struct BaselineBand
{
	double m_amplitude;
	double m_wavelength_m;
	double m_lacunarity;
	double m_persistence;
	double m_roundness;
	double m_weight_gain;
	uint m_seed;
	int m_octave_count;
};

// Immutable CPU-prepared recipe. Field order: regional base, selector, uplift, plains, hills, mountains, warp X, warp Y, basin selector.
// Upload as one StructuredBuffer element, NOT as a legacy cbuffer (whose array packing differs).
struct BaselineRecipe
{
	BaselineBand m_fields[9];
	double m_supported_coordinate_abs_m;
	double m_sea_level_bias_m;
	double m_uplift_height_m;
	double m_mountain_base_height_m;
	double m_basin_depth_m;
	double m_basin_threshold;
	int m_material_id;
	uint m_reserved;
};

// Status is 0 on success, 1 for invalid/out-of-range coordinates, 2 for unsupported arithmetic.
// Failure returns zero height/derivatives and material -1, which must not be treated as a terrain plane.
struct BaselineResult
{
	double m_height;
	double m_dx;
	double m_dy;
	int m_material_id;
	uint m_status;
};

// One octave of a local recipe. A local recipe is prepared in double precision around an origin, so a single-precision evaluator can sample positions
// relative to that origin. The lattice coordinate of origin + local is (m_origin_cell + m_origin_frac) + local * m_frequency, split so the large integer
// part stays exact. The z lattice coordinate (the octave's slice) is constant, so it is stored already split.
struct BaselineLocalOctave
{
	float m_frequency;   // Lattice cells per metre.
	float m_amplitude;   // Octave amplitude in the band's output units.
	int m_origin_cell_x; // floor(origin.x * frequency).
	int m_origin_cell_y; // floor(origin.y * frequency).
	float m_origin_frac_x; // origin.x * frequency - m_origin_cell_x, in [0, 1).
	float m_origin_frac_y; // origin.y * frequency - m_origin_cell_y, in [0, 1).
	int m_cell_z;        // floor of the octave's z lattice coordinate.
	float m_frac_z;      // Fractional part of the octave's z lattice coordinate.
};

// One field of a local recipe. Octaves beyond 'm_octave_count' are unused.
struct BaselineLocalBand
{
	float m_roundness;
	float m_weight_gain;
	uint m_seed;
	int m_octave_count;
	BaselineLocalOctave m_octaves[BaselineMaxOctaveCount];
};

// Immutable single-precision recipe prepared around an origin by BaselineSurface::LocalRecipe(). Field order matches BaselineRecipe.
// It is read from a ByteAddressBuffer by baseline_surface_local.hlsli, so every member is a 32-bit scalar.
struct BaselineLocalRecipe
{
	BaselineLocalBand m_fields[9];
	float m_sea_level_bias_m;
	float m_uplift_height_m;
	float m_mountain_base_height_m;
	float m_basin_depth_m;
	float m_basin_threshold;
	int m_material_id;
	uint m_reserved0;
	uint m_reserved1;
};

// Byte layout of BaselineLocalRecipe, used by the ByteAddressBuffer reader.
static const uint BaselineLocalOctaveSize = 32;
static const uint BaselineLocalBandSize = 16 + BaselineMaxOctaveCount * BaselineLocalOctaveSize;
static const uint BaselineLocalScalarsOffset = 9 * BaselineLocalBandSize;
static const uint BaselineLocalRecipeSize = BaselineLocalScalarsOffset + 32;

#ifdef __cplusplus
	static_assert(sizeof(BaselineBand) == 56 && alignof(BaselineBand) == 8);
	static_assert(offsetof(BaselineBand, m_amplitude) == 0 && offsetof(BaselineBand, m_wavelength_m) == 8);
	static_assert(offsetof(BaselineBand, m_lacunarity) == 16 && offsetof(BaselineBand, m_persistence) == 24);
	static_assert(offsetof(BaselineBand, m_roundness) == 32 && offsetof(BaselineBand, m_weight_gain) == 40);
	static_assert(offsetof(BaselineBand, m_seed) == 48 && offsetof(BaselineBand, m_octave_count) == 52);
	static_assert(sizeof(BaselineRecipe) == 560 && alignof(BaselineRecipe) == 8);
	static_assert(offsetof(BaselineRecipe, m_supported_coordinate_abs_m) == 504);
	static_assert(offsetof(BaselineRecipe, m_sea_level_bias_m) == 512 && offsetof(BaselineRecipe, m_uplift_height_m) == 520);
	static_assert(offsetof(BaselineRecipe, m_mountain_base_height_m) == 528 && offsetof(BaselineRecipe, m_basin_depth_m) == 536);
	static_assert(offsetof(BaselineRecipe, m_basin_threshold) == 544 && offsetof(BaselineRecipe, m_material_id) == 552);
	static_assert(offsetof(BaselineRecipe, m_reserved) == 556);
	static_assert(sizeof(BaselineResult) == 32 && offsetof(BaselineResult, m_status) == 28);
	static_assert(offsetof(BaselineResult, m_height) == 0 && offsetof(BaselineResult, m_dx) == 8 && offsetof(BaselineResult, m_dy) == 16);
	static_assert(offsetof(BaselineResult, m_material_id) == 24 && sizeof(v2d) == 16);
	static_assert(std::is_trivially_copyable_v<BaselineRecipe> && std::is_standard_layout_v<BaselineRecipe>);
	static_assert(sizeof(BaselineLocalOctave) == BaselineLocalOctaveSize && offsetof(BaselineLocalOctave, m_origin_frac_x) == 16 && offsetof(BaselineLocalOctave, m_frac_z) == 28);
	static_assert(sizeof(BaselineLocalBand) == BaselineLocalBandSize && offsetof(BaselineLocalBand, m_octaves) == 16);
	static_assert(sizeof(BaselineLocalRecipe) == BaselineLocalRecipeSize && offsetof(BaselineLocalRecipe, m_sea_level_bias_m) == BaselineLocalScalarsOffset);
	static_assert(offsetof(BaselineLocalRecipe, m_material_id) == BaselineLocalScalarsOffset + 20);
	static_assert(std::is_trivially_copyable_v<BaselineLocalRecipe> && std::is_standard_layout_v<BaselineLocalRecipe>);
}
#endif
#endif
