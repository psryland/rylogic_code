# Lost at Sea

## Vision

A sailing game set in a procedurally generated archipelago. The core game loop is a physics simulation of a rigid body (ship) floating on a moving ocean. The player builds sailing vessels and tries to efficiently sail them through increasingly challenging conditions. Art style is low-poly/Phong (WoW-esque), not AAA.

### Inspiration
- Environment, Scenic Setting => Sid Meier's Pirates - age of pirates, wooden sailing ships
- Ship building => basic components, lego brick construction => Valheim base building, KSP rocket building
- Graphic styling => World of Warcraft => low-ish detail models, simple Phong shading

## Key Game Features
- Reasonably physically realistic sailing model. Sails catch air flow, apply forces to vessel
- Realistic buoyancy simulation. Ships displace water based on shape. Centre of buoyancy vs centre of mass affects righting moment
- Ocean water simulation (around the player). Wave amplitudes, wind direction and strength, gusts
- Varying weather causing calm and rough conditions

## Desired Gameplay Scenarios
- Build ship in "shipyard" (i.e. editor), click "Launch" to sail
- Capsize/Sink shipwreck => Ship disassembles into parts (with physics) => Restart back at last home island

---

## Design Decisions

### Platform & Rendering
- **Platform**: Win64, DirectX 12
- **Renderer**: view3d-12 as a **static library** (not DLL). No LdrObjects/ldraw script.
- **Asset pipeline**: Models loaded from FBX/glTF files or generated procedurally. Using existing `pr/geometry/gltf.h` and `pr/geometry/fbx.h` support.

### Scale
- **Real-world scale**: 1 unit = 1 metre. A human male ~1.8m tall.

### Coordinate System
- **Right-handed**: Z = up, X = forward, Y = right

### Camera Model
- Camera stays at (0, 0, cam_height) in render space
- All world objects render at `render_pos = true_pos - camera_world_pos`
- Physics runs in true world space (player doesn't travel astronomical distances)
- This keeps all rendered geometry near the origin for floating point precision

### World Model
- Infinite flat plane with Perlin noise height field
- `height(x, y)` = multi-octave Perlin/Simplex noise, seed-based for reproducibility
- Height > 0 = land, height < 0 = ocean floor, ocean surface at z = 0
- Rendered with curvature falloff to simulate a horizon - mountains peek over before full islands visible

### Ocean Surface
- Flat plane at z = 0 with animated vertex displacement (Gerstner/sine waves)
- Grid mesh centred on camera, tessellated more densely nearby
- CPU mirrors the same wave math for physics queries (`OceanHeightAt`)

### Physics
- Built on `include/pr/physics2` (header-only, spatial algebra based)
- Buoyancy as a new forces module within physics2
- Uses existing collision shapes for hull geometry

### Technologies
- **Renderer**: `view3d-12` static lib (`projects/rylogic/view3d-12/`)
- **Collision**: `include/pr/collision/` - shapes, GJK, raycasting
- **Physics**: `include/pr/physics2/` - rigid body, integrator, engine (spatial algebra)
- **Audio**: `projects/rylogic/audio/` - DirectSound, WAV, OGG streaming
- **Asset loading**: `include/pr/geometry/gltf.h`, `include/pr/geometry/fbx.h`

### Settings Format
- **JSON** (not ldraw script). Uses `pr/storage/json.h` for read/write.
- Simple struct with explicit `Load(path)` / `Save(path)` methods.
- Drop the old `SettingsBase<>` macro system and `pr::script::Reader` dependency.

### Header Inclusion Convention
- Libraries do **not** include files from outside the library, except via `forward.h`
- Each library has a `forward.h` that acts as a pseudo-precompiled header and includes all "foreign" dependencies
- All other headers within a library only include sibling headers from the same library
- This is a repo-wide convention and must be followed in all new code

---

## Phases

### Phase 0: Project Modernisation & Foundation *(Start Here)*
Get the project compiling cleanly with current libs before adding features.
Use AceInspaders as the reference for the modern app framework pattern.

- **0.1 Modernise `forward.h`**: Strip old includes (stdafx.h, d3d11.h, old event system, old script headers).
  Use modern view3d-12 headers. Follow the repo convention: forward.h is the only file that includes "foreign" headers.
- **0.2 Modernise `settings.h`**: Replace the old ldraw-script-based settings (`settings.cpp`) with JSON-based
  settings using `pr/storage/json.h`. Simple struct with `Load(path)`/`Save(path)` using JSON read/write.
  Drop `SettingsBase<>` and `pr::script::Reader` dependency.
- **0.3 Modernise `main.h`/`main.cpp`**: Follow AceInspaders pattern:
  - Use `pr::app::DefaultSetup` (or minimal custom Setup)
  - Use `pr::app::Main<Main, MainUI, Settings>` and `pr::app::MainUI<MainUI, Main, SimMsgLoop>`
  - Remove old commented-out audio/DirectSound experiments
  - Remove `skybox.h/cpp` from `src/world/` (old D3D9-era code). Use `pr::app::Skybox` from `pr/app/skybox.h` (already partially done in main.cpp)
- **0.4 Clean up stubs**: Remove old `cam.h/cpp`, `ship.h/cpp`, `terrain.h/cpp` stubs (will be rewritten for Phase 1+).
  Or keep empty placeholder files if preferred.
- **0.5 Remove menu.ldr**: Delete `data/menu/menu.ldr` (ldraw script). Menu system will be rebuilt later (or deferred).
- **0.6 Update vcxproj**: Remove references to old files (stdafx, ogg, vorbis, old camera/dinput).
  Ensure `view3d-12-static.lib` and `audio.lib` are linked.
- **0.7 Compile and run**: Verify the app launches, shows a skybox, and can be closed cleanly.

### Phase 1: Ocean & Terrain Rendering *(Current Focus)*
Render a basic world the player can look at.

- **1.1 Ocean mesh**: Grid mesh centred on camera (e.g. 256x256 vertices, ~500m x 500m). Vertex shader applies animated Gerstner wave displacement based on wind direction/speed.
- **1.2 Ocean shader**: Water material with environment map reflection (from skybox), Fresnel, foam at wave peaks. Depth-based colour (turquoise shallow, dark blue deep).
- **1.3 Height field**: Multi-octave Perlin/Simplex noise. Input = (world_x, world_y), output = height. Seed-based for reproducibility.
- **1.4 Terrain mesh**: Terrain mesh for visible land (height > 0). LOD by distance from camera. Simple height/slope-based texturing (sand, grass, rock).
- **1.5 Horizon curvature**: Vertex shader curves geometry downward with distance from camera. Mountains appear on horizon before full islands are visible.
- **1.6 Skybox integration**: Skybox matches ocean horizon line. Architecture extensible for future day/night cycle.

### Phase 2: Ocean Physics (Buoyancy)
The core technical challenge - make things float realistically.

- **2.1 Wave height query**: `float OceanHeightAt(float x, float y, float time)` - CPU-side function matching GPU Gerstner displacement exactly.
- **2.2 Buoyancy force**: For a convex shape partially submerged, calculate submerged volume and centre of buoyancy. Force = rho_water x g x V_submerged, applied at centroid. Start with plane approximation (sample ocean height at hull centre, clip hull against that plane).
- **2.3 Physics2 integration**: New header `include/pr/physics2/forces/buoyancy.h` with `Ocean` struct and `BuoyancyForce()` function using spatial algebra types (`v8force`).
- **2.4 Water drag**: Linear + quadratic drag opposing motion through water. Separate drag for submerged vs above-water portions.
- **2.5 Test with primitives**: Drop box/sphere into ocean. Verify correct waterline, rocking on waves, settling to equilibrium, capsizing.

### Phase 3: Sailing Model
Make a pre-built ship respond to wind.

- **3.1 Ship rigid body**: Simple hull shape (convex polytope or shape array). Derive mass/inertia. Register with physics engine.
- **3.2 Wind model**: Global wind direction + speed, with Perlin-based gust variation. `v4 WindAt(v4 pos, float time)` query.
- **3.3 Sail forces**: Apparent wind (true wind - ship velocity), sail angle to lift/drag coefficients (simplified aerofoil model), force at sail attachment point.
- **3.4 Rudder & steering**: Player input to rudder angle to lateral force at stern to turning moment. Keel provides lateral resistance.
- **3.5 Basic HUD**: Wind direction compass, ship speed, heading. Controls: A/D = rudder, W/S or mouse = sail trim.

### Phase 4: Player Experience & Polish
Turn the simulation into something playable.

- **4.1 Camera modes**: Chase cam (behind ship), deck cam (first person), free cam (debug). Smooth interpolation, wave-following.
- **4.2 Audio**: Ocean ambience (waves, wind), sailing sounds (creaking, splashing, flapping). Vary with speed/conditions. Use audio.dll.
- **4.3 Ship capsize/sink**: Roll past critical angle, break apart, physics debris, fade/sink, respawn at last island.
- **4.4 Island interaction**: Approach island, anchor/dock. Save point / home base.
- **4.5 Procedural island placement**: Use height field to identify islands. Place points of interest, docks.

### Phase 5: Weather & World
Environmental variety and challenge.

- **5.1 Weather system**: Time-varying wind strength and direction. Calm to breeze to gale cycle.
- **5.2 Wave conditions**: Amplitude/frequency driven by wind. Calm near islands (sheltered), rough in open sea.
- **5.3 Visual weather**: Rain particles, fog (reduced draw distance), storm lighting.
- **5.4 Difficulty gradient**: Further from start = stronger winds, bigger waves, more storms.

### Future Phases (Deferred)
- Ship building editor (Valheim/KSP style construction)
- Multiplayer
- NPC/rival ships (possibly Copilot SDK for AI reasoning)
- Day/night cycle & dynamic lighting
- Cannons/combat
- Trading/economy
- Save/load system
- Full fluid simulation (Navier-Stokes)

---

## Technical Notes

### Buoyancy Algorithm (Phase 2 Detail)
Computing submerged volume of a convex hull against a non-planar surface (waves). Approaches in order of complexity:

1. **Plane approximation** *(start here)*: Sample ocean height at hull centre, treat water surface as a plane, clip hull, compute volume/centroid. Fast, good enough for prototyping.
2. **Multi-sample**: Sample ocean at several points under hull, piecewise planar water surface. Better for large ships on short-wavelength waves.
3. **Slice method**: Slice hull into horizontal layers, compute submerged fraction per slice. Expensive but very accurate.

### Ocean Mesh Strategy
- Grid mesh centred on camera, e.g. 256x256 verts covering ~500m x 500m
- Vertex shader displaces using sum of Gerstner waves
- CPU mirrors the same maths for physics queries
- Near camera: high tessellation. Far: low (LOD via mesh density or tessellation shader)
- Beyond mesh edge: skybox/fog hides boundary

### Height Field for Terrain
- `height(x, y) = Sum( amplitude_i * noise(frequency_i * x, frequency_i * y) )`
- Terrain generated in chunks around camera, LOD by distance
- Same height field used for collision detection (ship grounding)

---

## Other Crazy Ideas
- Integrate Copilot SDK to drive logic and reasoning for rival vessels