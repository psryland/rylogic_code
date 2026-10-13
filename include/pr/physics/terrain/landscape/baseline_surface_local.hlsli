//*********************************************
// Physics Terrain
//  Copyright (C) Rylogic Ltd 2026
//*********************************************
#ifndef PR_TERRAIN_BASELINE_SURFACE_LOCAL_HLSLI
#define PR_TERRAIN_BASELINE_SURFACE_LOCAL_HLSLI
#include "pr/physics/terrain/landscape/baseline_surface.hlsli"
#ifdef __cplusplus
#error "baseline_surface_local.hlsli is HLSL only. Prepare its recipe with BaselineSurface::LocalRecipe()"
#endif
#if !PR_TERRAIN_FP32
#error "The local baseline evaluator requires PR_TERRAIN_FP32=1"
#endif

// Single-precision baseline evaluator for positions relative to a prepared origin. It uses no double-precision arithmetic.
// The recipe comes from BaselineSurface::LocalRecipe(origin, radius) and is read from a ByteAddressBuffer or RWByteAddressBuffer at a caller-chosen
// byte offset. Positions are 'local' = world - origin, and the caller must keep them within the prepared radius. The lattice phase of the origin is prepared
// in double precision, so the position error is about one float rounding of 'local' (about 0.25 mm at 4 km), independent of the origin.
// It shares the noise and blend arithmetic with BaselineEvaluate, so its results agree with the CPU surface within single-precision rounding.
// The evaluator is stage-neutral: no registers, barriers or dispatch intrinsics. Compile with HLSL 2021 and -Gis, like baseline_surface.hlsli.

// One local evaluator sample. Height includes the sea-level datum. Status is 0 on success, or 2 for unsupported arithmetic (a malformed recipe).
struct BaselineLocalResult
{
	float m_height;
	float m_dx;
	float m_dy;
	int m_material_id;
	uint m_status;
};

// Evaluate one fractal band of a local recipe at 'local'. 'band_offset' is the byte offset of the band within 'recipe'.
template <typename RecipeBuffer>
BaselineField BaselineLocalFractal(RecipeBuffer recipe, uint band_offset, float2 local, bool ridged)
{
	// Read the band header. A malformed recipe cannot cause an unbounded shader loop.
	uint4 header = recipe.Load4(band_offset);
	BaselineReal roundness = asfloat(header.x);
	BaselineReal weight_gain = asfloat(header.y);
	uint seed = header.z;
	int octave_count = (int)header.w;
	BaselineField sample = BaselineConstant(0);
	BaselineField weight = BaselineConstant(1);
	if (octave_count < 1 || octave_count > BaselineMaxOctaveCount)
		return BaselineField(asfloat(0x7fc00000u), 0, 0);

	for (int octave = 0; octave != octave_count; ++octave)
	{
		// Resolve the lattice cell from the prepared origin phase. 'floor' and the subtraction are exact, so the cell fraction keeps full precision.
		uint offset = band_offset + 16 + (uint)octave * BaselineLocalOctaveSize;
		uint4 a = recipe.Load4(offset);
		uint4 b = recipe.Load4(offset + 16);
		BaselineReal frequency = asfloat(a.x);
		BaselineReal amplitude = asfloat(a.y);
		float2 lattice = float2(asfloat(b.x), asfloat(b.y)) + local * frequency;
		float2 cell = floor(lattice);
		int64_t cell_x = (int64_t)((int)a.z + (int)cell.x);
		int64_t cell_y = (int64_t)((int)a.w + (int)cell.y);
		int64_t cell_z = (int64_t)(int)b.z;
		NoiseSample noise = NoiseCellWithDerivatives(seed, cell_x, cell_y, cell_z, cell_x + 1, cell_y + 1, cell_z + 1, lattice.x - cell.x, lattice.y - cell.y, asfloat(b.w));

		// Accumulate the octave exactly as BaselineFractal does, with lattice derivatives scaled to metres.
		if (ridged)
		{
			BaselineField ridge = BaselineRidge(BaselineField(noise.m_value, frequency * noise.m_dx, frequency * noise.m_dy), roundness);
			sample = sample + amplitude * BaselineProduct(weight, ridge);
			weight = BaselineClamp(weight_gain * ridge);
		}
		else
		{
			sample.x += amplitude * noise.m_value;
			sample.y += amplitude * frequency * noise.m_dx;
			sample.z += amplitude * frequency * noise.m_dy;
		}
	}
	return sample;
}

// Sample a local recipe at 'local' (= world - origin) and return its normalized (plains, hills, mountains, basins) weights.
// 'offset' is the byte offset of the BaselineLocalRecipe within 'recipe'. Inspect m_status before using either output.
template <typename RecipeBuffer>
BaselineLocalResult BaselineEvaluateLocal(RecipeBuffer recipe, uint offset, float2 local, out float4 region_weights)
{
	// Apply the same smooth domain warp to all regional and detail fields.
	BaselineLocalResult result;
	result.m_height = result.m_dx = result.m_dy = 0;
	result.m_material_id = -1;
	result.m_status = 2;
	region_weights = float4(0, 0, 0, 0);
	BaselineField warp_x = BaselineLocalFractal(recipe, offset + 6 * BaselineLocalBandSize, local, false);
	BaselineField warp_y = BaselineLocalFractal(recipe, offset + 7 * BaselineLocalBandSize, local, false);
	float2 warped = local + float2(warp_x.x, warp_y.x);
	BaselineFields fields;
	fields.m_regional = BaselineApplyWarp(BaselineLocalFractal(recipe, offset + 0 * BaselineLocalBandSize, warped, false), warp_x, warp_y);
	fields.m_uplift = BaselineUnit(BaselineApplyWarp(BaselineLocalFractal(recipe, offset + 2 * BaselineLocalBandSize, warped, false), warp_x, warp_y));
	fields.m_selector = BaselineUnit(BaselineApplyWarp(BaselineLocalFractal(recipe, offset + 1 * BaselineLocalBandSize, warped, false), warp_x, warp_y));
	fields.m_plains = BaselineApplyWarp(BaselineLocalFractal(recipe, offset + 3 * BaselineLocalBandSize, warped, false), warp_x, warp_y);
	fields.m_hills = BaselineApplyWarp(BaselineLocalFractal(recipe, offset + 4 * BaselineLocalBandSize, warped, false), warp_x, warp_y);
	fields.m_mountains = BaselineApplyWarp(BaselineLocalFractal(recipe, offset + 5 * BaselineLocalBandSize, warped, true), warp_x, warp_y);
	fields.m_depression = BaselineUnit(BaselineApplyWarp(BaselineLocalFractal(recipe, offset + 8 * BaselineLocalBandSize, warped, false), warp_x, warp_y));

	// Blend the families with the scalars that follow the bands.
	uint4 scalars = recipe.Load4(offset + BaselineLocalScalarsOffset);
	uint2 tail = recipe.Load2(offset + BaselineLocalScalarsOffset + 16);
	BaselineComposite composite = BaselineCompose(fields, asfloat(scalars.y), asfloat(scalars.z), asfloat(scalars.w), asfloat(tail.x));
	if (!composite.m_valid)
		return result;

	// Add the height datum last, like the CPU evaluator.
	BaselineField height = composite.m_height;
	float absolute_height = asfloat(scalars.x) + height.x;
	if (!isfinite(absolute_height) || !isfinite(height.y) || !isfinite(height.z))
		return result;

	result.m_height = absolute_height;
	result.m_dx = height.y;
	result.m_dy = height.z;
	result.m_material_id = (int)tail.y;
	result.m_status = 0;
	float land_share = 1 - composite.m_basin;
	region_weights = float4(composite.m_plains_weight * land_share, composite.m_hills_weight * land_share, composite.m_mountains_weight * land_share, composite.m_basin);
	return result;
}
#endif
