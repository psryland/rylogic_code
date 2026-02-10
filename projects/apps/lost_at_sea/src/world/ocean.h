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

float Frequency() const { return maths::tauf / m_wavelength; }
float WaveNumber() const { return maths::tauf / m_wavelength; }
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

// Persistent buffers to avoid per-frame allocation
std::vector<v4> m_verts;
std::vector<v4> m_norms;
std::vector<Colour32> m_colours;
std::vector<uint16_t> m_indices;

explicit Ocean(Renderer& rdr)
:m_waves()
,m_inst()
,m_factory(rdr)
,m_verts(GridDim * GridDim)
,m_norms(GridDim * GridDim)
,m_colours(GridDim * GridDim, Colour32(0xFF804010)) // Default ocean blue (ABGR: dark teal)
,m_indices()
{
InitDefaultWaves();
BuildIndexBuffer();
}

// Query the ocean surface height at a world position and time
float HeightAt(float world_x, float world_y, float time) const
{
auto h = 0.0f;
for (auto& w : m_waves)
{
auto k = w.WaveNumber();
auto phase = k * (w.m_direction.x * world_x + w.m_direction.y * world_y) - w.Frequency() * time;
h += w.m_amplitude * std::sin(phase);
}
return h;
}

// Query the full Gerstner-displaced position
v4 DisplacedPosition(float world_x, float world_y, float time) const
{
auto dx = 0.0f, dy = 0.0f, dz = 0.0f;
for (auto& w : m_waves)
{
auto k = w.WaveNumber();
auto phase = k * (w.m_direction.x * world_x + w.m_direction.y * world_y) - w.Frequency() * time;
auto c = std::cos(phase);
auto s = std::sin(phase);

dx -= w.m_steepness * w.m_amplitude * w.m_direction.x * c;
dy -= w.m_steepness * w.m_amplitude * w.m_direction.y * c;
dz += w.m_amplitude * s;
}
return v4(world_x + dx, world_y + dy, dz, 1.0f);
}

// Compute the surface normal
v4 NormalAt(float world_x, float world_y, float time) const
{
auto nx = 0.0f, ny = 0.0f, nz = 1.0f;
for (auto& w : m_waves)
{
auto k = w.WaveNumber();
auto phase = k * (w.m_direction.x * world_x + w.m_direction.y * world_y) - w.Frequency() * time;
auto c = std::cos(phase);
auto s = std::sin(phase);

nx -= w.m_direction.x * k * w.m_amplitude * c;
ny -= w.m_direction.y * k * w.m_amplitude * c;
nz -= w.m_steepness * k * w.m_amplitude * s;
}
return Normalise(v4(nx, ny, nz, 0.0f));
}

// Rebuild the mesh for the current time and camera world position
void Update(float time, v4 camera_world_pos)
{
auto cell_size = 2.0f * GridExtent / (GridDim - 1);

for (int iy = 0; iy < GridDim; ++iy)
{
for (int ix = 0; ix < GridDim; ++ix)
{
auto idx = iy * GridDim + ix;

// Grid position in world space
auto wx = camera_world_pos.x + (ix - GridDim / 2) * cell_size;
auto wy = camera_world_pos.y + (iy - GridDim / 2) * cell_size;

// Displaced position, converted to render space (camera at origin)
auto displaced = DisplacedPosition(wx, wy, time);
m_verts[idx] = displaced - camera_world_pos;
m_verts[idx].w = 1.0f;

// Normal
m_norms[idx] = NormalAt(wx, wy, time);
}
}

// Rebuild model each frame (CPU displacement approach)
auto vcount = isize(m_verts);
NuggetDesc nugget(ETopo::TriList, EGeom::Vert | EGeom::Colr | EGeom::Norm);
nugget.m_vrange = rdr12::Range(0, vcount);
nugget.m_irange = rdr12::Range(0, isize(m_indices));
auto nug_span = std::span<NuggetDesc const>(&nugget, 1);

MeshCreationData cdata;
cdata.verts(m_verts).indices(std::span<uint16_t const>(m_indices)).colours(m_colours).normals(m_norms).nuggets(nug_span);
m_inst.m_model = ModelGenerator::Mesh(m_factory, cdata);
m_inst.m_i2w = m4x4::Identity();
}

// Add the ocean to the scene
void AddToScene(Scene& scene)
{
if (m_inst.m_model)
scene.AddInstance(m_inst);
}

private:

void InitDefaultWaves()
{
m_waves = {
{ Normalise(v4(1.0f, 0.3f, 0, 0)), 1.2f, 60.0f, 8.0f, 0.5f },  // Primary swell
{ Normalise(v4(0.8f, -0.6f, 0, 0)), 0.6f, 30.0f, 5.5f, 0.4f },  // Secondary
{ Normalise(v4(-0.3f, 1.0f, 0, 0)), 0.3f, 15.0f, 3.8f, 0.3f },  // Cross chop
{ Normalise(v4(0.5f, 0.5f, 0, 0)), 0.15f, 8.0f, 2.8f, 0.2f },   // Small ripple
};
}

void BuildIndexBuffer()
{
m_indices.reserve((GridDim - 1) * (GridDim - 1) * 6);
for (int iy = 0; iy < GridDim - 1; ++iy)
{
for (int ix = 0; ix < GridDim - 1; ++ix)
{
auto i0 = static_cast<uint16_t>(iy * GridDim + ix);
auto i1 = static_cast<uint16_t>(i0 + 1);
auto i2 = static_cast<uint16_t>(i0 + GridDim);
auto i3 = static_cast<uint16_t>(i2 + 1);
m_indices.push_back(i0); m_indices.push_back(i2); m_indices.push_back(i1);
m_indices.push_back(i1); m_indices.push_back(i2); m_indices.push_back(i3);
}
}
}
};
}