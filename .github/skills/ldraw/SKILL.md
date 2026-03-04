---
name: ldraw
description: >
  Knowledge about LDraw (View3D-12), a 3D scene viewer built on DirectX 12
  with LDraw script format for defining geometry, models, animations, and montages.
  Use this when working with .ldr files, the LDraw Builder C# API, FBX model display,
  animation montages, or coordinate system conversions between UE/FBX/LDraw.
  Use this tool to demonstrate geometric relationships between data, or to graph
  2D data.
---

# LDraw — View3D-12 3D Scene Viewer

LDraw is a 3D scene viewer and scripting system built on DirectX 12 (Rylogic View3D-12).
It renders `.ldr` script files that define geometry, model instances, animations, montages, and chart data.

**Source locations:**
- Native C++ engine: `E:\Rylogic\Code\projects\rylogic\view3d-12\`
- C# WPF app: `E:\Rylogic\Code\projects\apps\LDraw\`
- C# Builder API: `E:\Rylogic\Code\projects\rylogic\Rylogic.Gfx\` (`Rylogic.Gfx` / `Rylogic.LDraw` namespace)
- LDraw parsing: `view3d-12\src\ldraw\ldraw_parsing.cpp` and `ldraw_templates.cpp`
- C++ Builder API: `E:\Rylogic\Code\include\pr\common\ldraw.h`
---

## Coordinate System

- **LDraw uses right-handed coordinates, Z-up preferred but up direction is configurable**
- Axis colours: **Red = X**, **Green = Y**, **Blue = Z** (up)
- This is the same up-axis as Unreal Engine (Z-up), but UE is left-handed

---

## LDraw Script Format

LDraw files (`.ldr`) use a keyword-based format:
```
*Keyword Name Colour { ...content... }
```
- Script grammer is defined in `E:\Rylogic\Code\projects\rylogic\view3d-12\src\ldraw\ldraw_templates.cpp`
- Script example in `E:\Rylogic\Code\projects\rylogic\view3d-12\src\ldraw\ldraw_demo_scene.ldr`
- 'Name' and 'Colour' are optional
- Colours are 32-bit AARRGGBB hex (e.g., `FFFF0000` = red)
- Objects can be nested within other objects and nested objects inherit the transform of the parent (e.g. `*Box outer { *Box inner {...} *o2w {*pos {1 2 3}} }`)
- Groups are the typical way to nest objects: `*Group MyGroup FFFFFFFF { ... }`
- `*Hidden {true}` hides an object (useful for reference models)

### Common Primitives
```
*Box name FFFF0000 { 1 2 3 }                    // width height depth
*Sphere name FF00FF00 { 1 }                      // radius
*Line name FFFFFF00 { *Style {LineStrip} {x y z} {x y z} ... }
*Line arrow FFFFFFFF { *Arrow {Fwd} {x y z} {dx dy dz} }
```

### Transforms on Objects
```
*O2W { *M4x4 { m11 m12 m13 m14  m21 m22 m23 m24  m31 m32 m33 m34  m41 m42 m43 m44 } }
*Euler { pitch_deg yaw_deg roll_deg }
*Pos { x y z }
```
Euler operations compose left-to-right in Builder: `.euler(90,0,0).euler(0,0,90)` = Rx(90) then Rz(90).

---

## Models (e.g. FBX, glTF, glb, P3D, Max3DS, STL)

Model files are loaded via `*Model`
- Instances of model can be created using `*Instance`. This prevents loading the model multiple times.
- P3D is a rylogic custom model format
```
*Model model_name FFFFFFFF {
    *FilePath {"path\to\model.fbx"}
    *Animation {}
    *NoMaterials {}
}
*Instance inst_name FFFFFFFF {
    *Data {model_name}
    *Euler { 90 0 0 }
}
```

### FBX Coordinate Conversion (FBX → LDraw)

FBX uses **Y-up** convention. LDraw uses **Z-up**. To display FBX models upright in LDraw:
```csharp
.euler(90, 0, 0)    // Rx(90): rotates Y-up to Z-up
.euler(0, 0, 90)    // Rz(90): rotates character to face +X in LDraw
```

The combined euler matrix `M = Rx(90) * Rz(90)` maps:
- **FBX X → LDraw Y** (green)
- **FBX Y → LDraw Z** (blue, up)
- **FBX Z → LDraw X** (red, forward)

```
M = | 0  1  0 |    M_inv = | 0  0  1 |
    | 0  0  1 |            | 1  0  0 |
    | 1  0  0 |            | 0  1  0 |
```

---

## Animation Montages

Montages combine frames from different animations into a single playback sequence.
Each frame references a source animation, a frame number, and an optional per-frame transform.


### C# Builder API
```csharp
var monti = grp.Instance("Montage")
    .inst("RefModel.animation_name")
    .euler(90, 0, 0).euler(0, 0, 90)
    .montage();

// Add additional animation sources (source index 1, 2, ...)
monti.anim_source("path/to/other.fbx");

// Add frames: source_index, frame_number, duration, optional o2w
monti.frame(0, 486, 0.1f, (m4x4)some_matrix);

// Optionally strip root motion
monti.no_translation();
monti.no_rotation();
```

### Source Indices
- **Source 0** = the base model referenced by `.inst("RefModel.name")`
- **Source 1, 2, ...** = additional Model files added via `.anim_source(path)`

### NoRootTranslation / NoRootRotation

These flags **zero out the root bone** position/rotation during playback:
- They override BOTH the original root motion AND any per-frame o2w
- Use them when you want the character pinned at the instance's static position

---

## Keyboard Shortcuts

| Key | Action |
|-----|--------|
| **F7** | Zoom to fit **visible** objects |
| **Ctrl+F7** | Zoom to fit **all** objects (including hidden) |
| **Shift+F7** | Zoom to fit **selected** objects |
| **Space** | Open Object Manager UI |
| **Ctrl+W** | Cycle fill modes (Solid → Wireframe → SolidWire) |
| **.** (period) | Center camera on object under cursor |

---

## Animation Controls Window

The Animation Controls window provides playback control with these elements:
- **Clock field**: editable text box showing current time in seconds
- **Frame field**: current frame number
- **FPS field**: playback rate
- **Progress slider**: Frame 0–100 through the animation. Upper limit (100) can be changed
- **Transport buttons**: rewind, step back, play reverse, pause, play forward, step forward

To reset animation to the start via automation, set the Clock field to `0`:
```
# Using conx automate targeting the "Animation Controls" window:
click <clock_field_x>,<clock_field_y>
key ctrl+a
type 0
key enter
```

---

## Streaming / Remote Control

LDraw supports TCP streaming for remote command injection:
- Enable via `View3D_StreamingEnable(TRUE, port)`
- Accepts raw LDraw script text over TCP (no framing needed beyond `*Keyword { }` blocks)
- Can switch to binary mode mid-stream with `*BinaryStream` command
- Streaming is **not enabled by default** — must be toggled in the app

---

## Auto-Refresh (File Watching)

LDraw watches loaded `.ldr` files for changes and auto-reloads them.
Scripts can overwrite the output `.ldr` file and LDraw will pick up changes automatically.
The animation state (clock position) persists across reloads.

---

## C# Builder API Reference

```csharp
using Rylogic.LDraw;

var builder = new Builder();

// Groups
var grp = builder.Group("MyGroup");
grp.hidden(true);

// Primitives
grp.Box("MyBox", 0xFFFF0000).dim(1, 2, 3);
grp.Sphere("MySphere", 0xFF00FF00).dim(1);
grp.Line("MyLine", 0xFFFFFF00).style(ELineStyle.LineStrip);
grp.Arrow("MyArrow", 0xFFFFFFFF).style(EArrowStyle.Fwd);

// Models and instances
grp.Model("name", 0xFFFFFFFF).filepath("model.fbx").anim().no_materials();
grp.Instance("name", 0xFFFFFFFF).inst("RefModel.model_name").euler(90, 0, 0);

// Transforms
grp.Box("b", 0xFFFF0000).pos(x, y, z).euler(rx, ry, rz).o2w(matrix);

// Save
builder.Save("output.ldr", ESaveFlags.Pretty);
```

---

## UE ↔ LDraw Coordinate Conversion

| Property | Unreal Engine | LDraw | FBX |
|----------|--------------|-------|-----|
| Up axis | Z | Z | Y |
| Handedness | Left | Right | Right |
| Forward | +X | +X (convention) | +Z (after euler) |
| Units | Centimetres | Unitless (matches source) | Unitless |

### Velocity / Direction Vectors from UE

When displaying UE velocity data in LDraw:
- Use `Vector3.TransformNormal` (w=0) for directions, NOT `Vector3.Transform` (w=1)
- Negate Y for left-hand → right-hand conversion: `vel.Y = -vel.Y`
- For grid-layout displays, use raw root-space velocity (no transform needed)

### Transform Data from UE (NextToPrev, etc.)

UE-space transforms used for path reconstruction need the **similarity transform** to
convert to FBX space for montage per-frame o2w (see "Converting World-Space Transforms" above).
Simple matrix operations on translation alone work if no rotation is needed.

## Charts and 2D Data

LDraw can display 2D chart data using the `*Chart {...}` object.
- Chart data can be loaded from CSV files or given directly in the script
- `*FilePath` and `*Data` are mutually exclusive
- `*Series` objects are used to visualize the data.
- Within a Series, columns are referred to by `C<n>`, where 'n' is the column number

Example chart:
```
*Chart chart
{
	// Reference an external file containing CSV data
	*FilePath {"my_data.csv"}

	// The dimensions of the data
	*Dim {3} // {columns, [rows]}. Optional if 'FilePath' is used.

	// Or embed the data directly in the script
	*Data
	{
		// Data values. Can be layed out in any way, '*Dim' defines the number of columns
		0.0,  1.0,  0.0,
		0.5,  1.2,  0.3,
		1.0,  0.2,  0.9,
		1.5,  0.8,  1.2,
		2.0,  1.5,  1.1,
		2.5,  1.2,  0.8,
		3.0,  0.9,  0.5,
		3.5,  0.7,  0.6,
		4.0,  0.3,  0.7,
	}

    // Add a series to the chart
	*Series plot1 FF00A0E0 // Colour is auto assigned if not given
	{
		*XAxis {"C0"}            // Expression for the X values
		*YAxis {"abs(C2 - C1)"}  // Expression for the Y values
		*Width {7}               // Optional. A width for the lines
		*Dashed {0.1 0.1}        // Optional. Dashed line pattern
		*DataPoints
		{
			*Size {30}          // Optional. The size of data points on the graph
			*Colour {FFFF00FF}  // Optional. The colour of the data points
			*Style {Circle}     // Optional. Data point marker shape
		}
	}

    // There can be many Series objects
	*Series plot2 FFA000E0 // Colour is auto assigned if not given
	{
		*XAxis {"CI"}            // Use CI or RI for the virtual index column or index row
		*YAxis {"sin(C2) + 0.1*C0"}
		*Width {5}
		*Smooth {} // Optional. Smooth the line
	}
}
```
