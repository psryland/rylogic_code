# Copilot Instructions for Rylogic Codebase

## Build Commands

### Full Solution Build
```powershell
# Using dotnet-script (requires: dotnet tool install -g dotnet-script)
dotnet-script ./script/Build.csx

# Or open Rylogic.sln in Visual Studio
```

### Building Specific Projects
```powershell
# Build specific project(s)
dotnet-script ./script/Build.csx -project <ProjectName> -build

# Available projects: Sqlite3, Scintilla, Audio, Fbx, View3d, P3d, RylogicCore, RylogicDB,
#   RylogicDirectShow, RylogicGfx, RylogicGuiWPF, RylogicNet, RylogicScintilla, RylogicWindows,
#   Csex, LDraw, RyLogViewer, RylogicTextAligner, AllNative, AllManaged, AllRylogic, All

# Build with specific platform and configuration
dotnet-script ./script/Build.csx -project View3d -platform x64 -config Release -build

# Rebuild (clean + build)
dotnet-script ./script/Build.csx -project RylogicCore -rebuild
```

### Deploy and Publish
```powershell
# Build and deploy to /lib folder
dotnet-script ./script/Build.csx -project AllNative -build -deploy

# Publish to NuGet
dotnet-script ./script/Build.csx -project RylogicCore -deploy -publish
```

## Testing

### C# Unit Tests
Tests are embedded within source files using `[TestMethod]` attributes. Run tests via:
```powershell
# Run unit tests on a compiled assembly
dotnet-script ./script/RunUnitTests.csx <TargetPath> <is_managed> [dependencies...]

# Example:
dotnet-script ./script/RunUnitTests.csx "E:/Rylogic/Code/projects/rylogic/Rylogic.Core/bin/Debug/net9.0-windows/Rylogic.Core.dll" true
```

### Python Tests (py-rylogic)
```powershell
cd projects/rylogic/py-rylogic
pip install .[dev]
pytest                    # Run all tests
pytest tests/test_file.py # Run single test file
```

### TypeScript Tests (VS Code Extensions)
```powershell
cd typescript/projects/Rylogic.TextAligner
npm install
npm run compile
```

## Architecture

### Language Mix
- **C++**: Native libraries in `/include/pr/` (header-heavy, template-based)
- **C#/.NET**: Managed assemblies in `/projects/rylogic/` targeting `net9.0-windows` and `net481`
- **TypeScript**: VS Code extensions in `/typescript/projects/`
- **Python**: Python bindings in `/projects/rylogic/py-rylogic/`

### Core Libraries Hierarchy
```
Rylogic.Core      → Base library (extensions, math, containers, utilities)
Rylogic.Windows   → Windows-specific helpers (depends on Core)
Rylogic.Gui.WPF   → WPF UI components (depends on Core, Windows)
Rylogic.Gfx       → Graphics/View3d wrapper (depends on Core, Native)
Rylogic.Scintilla → Scintilla editor wrapper (depends on Core, Gui.WPF, Windows)
```

### Native DLLs
- `view3d-12.dll` - DirectX 12 3D graphics engine
- `audio.dll` - Audio processing library
- Native libraries are built via MSBuild and deployed to `/lib/{platform}/{config}/`

### Key Directories
- `/include/pr/` - C++ public headers (all includes are relative to `/include/`)
- `/projects/rylogic/` - Core .NET assemblies
- `/projects/apps/` - Applications (LDraw, RyLogViewer, etc.)
- `/projects/tests/` - Test projects
- `/script/` - Build automation scripts (dotnet-script .csx files)

## Conventions

### C# Naming
- Private/internal fields: `m_field_name` (lowercase with underscores, `m_` prefix)
- Interfaces: `I` prefix (e.g., `IMyInterface`)
- File-scoped namespaces preferred

### C++ Includes
All includes relative to `/include/`:
```cpp
#include "pr/common/..."
#include "pr/maths/..."
#include "pr/gfx/..."
```

### Nullable References
C# projects have nullable enabled: `<Nullable>Enable</Nullable>`

### Target Frameworks
Primary: `net9.0-windows`
Secondary: `net481` (for legacy support)

## Build System Details

### Prerequisites
- Visual Studio 2022 with C++ workload
- .NET 9 SDK
- `dotnet-script` tool: `dotnet tool install -g dotnet-script`

### Configuration Files
- `Directory.Build.props` - Shared MSBuild properties for all projects
- `Directory.Packages.props` - Centralized NuGet package versions
- `.editorconfig` - Code style and formatting rules

### UserVars
Customize build paths via `/script/UserVars.csx` or `/script/UserVars.json`
