//************************************
// Lost at Sea
//  Copyright (c) Rylogic Ltd 2024
//************************************
// Gerstner wave ocean simulation.
// CPU-side wave displacement for physics queries + mesh generation for rendering.
#pragma once
#include "src/forward.h"

namespace las
{
// Parameters for a single Gerstner wave component
struct GerstnerWave
{
v4 m_direction;     // Normalised wave travel direction (XY plane, Z=0, w=0)
float m_amplitude;  // Wave height (peak to mean), in metres
float m_wavelength; // Distance between crests, in metres
float m_speed;      // Phase speed, in m/s
float m_steepness;  // Gerstner steepness Q [0..1], controls sharpness of peaks

float Frequency() const;
float WaveNumber() const;
};

// Ocean simulation and rendering
struct Ocean
{
// Grid resolution and extent. 128x128 = 16k verts, fits in uint16 indices.
static constexpr int GridDim = 128;
static constexpr float GridExtent = 500.0f; // Half-extent in metres
static constexpr float WaterDensity = 1025.0f; // kg/m^3 (seawater)

struct Instance
{
#define PR_RDR_INST(x)\
x(m4x4     , m_i2w  , EInstComp::I2WTransform)\
x(ModelPtr , m_model, EInstComp::ModelPtr)
PR_RDR12_INSTANCE_MEMBERS(Instance, PR_RDR_INST);
#undef PR_RDR_INST
};

std::vector<GerstnerWave> m_waves;
Instance m_inst;
ResourceFactory m_factory;

// CPU-side vertex data (simulation writes here, render reads from here)
std::vector<Vert> m_cpu_verts;
std::vector<uint16_t> m_indices;
bool m_dirty; // True when CPU verts have been updated but not yet uploaded to GPU

explicit Ocean(Renderer& rdr);

// Physics queries (read-only, no rendering side effects)
float HeightAt(float world_x, float world_y, float time) const;
v4 DisplacedPosition(float world_x, float world_y, float time) const;
v4 NormalAt(float world_x, float world_y, float time) const;

// Simulation step: recompute CPU vertex positions for the current time and camera position.
// This only modifies simulation state (m_cpu_verts), not GPU resources.
void Update(float time, v4 camera_world_pos);

// Rendering: upload dirty verts to GPU and add to the scene.
void AddToScene(Scene& scene);

private:

void InitDefaultWaves();
void BuildMesh();
};
}