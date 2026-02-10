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
# Build specific project(s) via the build script
dotnet-script ./script/Build.csx -project <ProjectName> -build

# Available projects: Sqlite3, Scintilla, Audio, Fbx, View3d, P3d, RylogicCore, RylogicDB,
#   RylogicDirectShow, RylogicGfx, RylogicGuiWPF, RylogicNet, RylogicScintilla, RylogicWindows,
#   Csex, LDraw, RyLogViewer, RylogicTextAligner, AllNative, AllManaged, AllRylogic, All

# Build with specific platform and configuration
dotnet-script ./script/Build.csx -project View3d -platform x64 -config Release -build

# Rebuild (clean + build)
dotnet-script ./script/Build.csx -project RylogicCore -rebuild

# Build a single C# project directly with dotnet CLI
dotnet build projects/rylogic/Rylogic.Core/Rylogic.Core.csproj
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
Tests are **embedded inline in source files** (not in separate test files) using NUnit attributes:
```csharp
// At the bottom of a source file, e.g. Rylogic.Core/src/Common/Base64.cs:
[TestFixture] public class TestBase64
{
    [Test] public void Base64()
    {
        // assertions...
    }
}
```
Run tests via:
```powershell
# Run unit tests on a compiled assembly
dotnet-script ./script/RunUnitTests.csx <TargetPath> <is_managed> [dependencies...]

# Example:
dotnet-script ./script/RunUnitTests.csx "projects/rylogic/Rylogic.Core/bin/Debug/net9.0-windows/Rylogic.Core.dll" true
```

### C++ Unit Tests
Tests use a lightweight custom framework (`include/pr/common/unittests.h`), also inline:
```cpp
#if PR_UNITTESTS
#include "pr/common/unittests.h"
namespace pr::unittests
{
    PRUnitTest(TestName)
    {
        PR_EXPECT(1 + 1 == 2);
        PR_THROWS(bad_call(), std::exception);
    }
}
#endif
```
Compile with `/DPR_UNITTESTS=1`. The `projects/tests/unittests/` project includes all test headers.

### Python Tests (py-rylogic)
```powershell
cd projects/rylogic/py-rylogic
pip install .[dev]
pytest                    # Run all tests
pytest tests/test_file.py # Run single test file
```

### TypeScript (VS Code Extensions)
```powershell
cd typescript/projects/Rylogic.TextAligner
npm install && npm run compile
```

## Architecture

### Language Mix
- **C++**: Native libraries in `/include/pr/` (header-heavy, template-based, C++20)
- **C#/.NET**: Managed assemblies in `/projects/rylogic/` targeting `net9.0-windows` and `net481`
- **TypeScript**: VS Code extensions in `/typescript/projects/`
- **Python**: Python bindings in `/projects/rylogic/py-rylogic/`

### Core .NET Libraries Hierarchy
```
Rylogic.Core      → Base library (extensions, math, containers, utilities)
Rylogic.Windows   → Windows-specific helpers (depends on Core)
Rylogic.Gui.WPF   → WPF UI components (depends on Core, Windows)
Rylogic.Gfx       → Graphics/View3d wrapper (depends on Core, Native DLLs)
Rylogic.Scintilla → Scintilla editor wrapper (depends on Core, Gui.WPF, Windows)
```

### Native DLLs
- `view3d-12.dll` - DirectX 12 3D graphics engine (the main native library)
- `audio.dll`, `fbx.dll`, `gltf.dll` - Supporting native libraries
- Built via MSBuild (`.vcxproj`), deployed to `/lib/{platform}/{config}/`

### C++ Header Library
All C++ code lives under `/include/pr/` as a header-heavy library. All `#include` paths are relative to `/include/`:
```cpp
#include "pr/common/assert.h"
#include "pr/maths/maths.h"
#include "pr/view3d-12/view3d-dll.h"
```

## Conventions

### Naming (applies to both C++ and C#)
- **Classes, methods, properties, enums**: `PascalCase`
- **Private/internal fields**: `m_field_name` (lowercase with underscores, `m_` prefix)
- **Local variables**: `snake_case`
- **Interfaces**: `I` prefix (e.g., `IMyInterface`)
- **No camelCase** — this is a strong preference throughout the codebase
- C++ namespaces: `pr::maths`, `pr::ldraw`, `pr::collision`, etc.

### Coding Style
- **Tabs** for indentation in both C++ and C#
- **Allman braces** (opening brace on its own line) in C#
- Prefer `var` in C# and `auto` in C++ unless the type change is deliberate
- File-scoped namespaces in C#
- Comments should explain "why", not "what" — add a blank line before comment blocks
- C# nullable references enabled (`<Nullable>Enable</Nullable>`)

### P/Invoke Pattern
Win32 interop in `Rylogic.Core/src/Win32/` uses a trailing-underscore convention:
```csharp
public static bool AllocConsole() => AllocConsole_();
[DllImport("kernel32.dll", EntryPoint = "AllocConsole", SetLastError = true)]
private static extern bool AllocConsole_();
```
The public method wraps the private `_`-suffixed extern.

### `@copilot` Comments
Any comment containing `@copilot` is an instruction, question, or context directed at Copilot. Treat these as actionable input during sessions.

### Target Frameworks
- Primary: `net9.0-windows`
- Legacy: `net481`
- C++ toolset: `v145` (VS 2022), Windows SDK 10.0

## Build System Details

### Prerequisites
- Windows (this project is Windows-only)
- Visual Studio 2022 with C++ workload (for native projects)
- .NET 9 SDK
- `dotnet-script` tool: `dotnet tool install -g dotnet-script`

### Configuration Files
- `Directory.Build.props` — Shared MSBuild properties for all C# and C++ projects
- `Directory.Packages.props` — Centralized NuGet package versions
- `.editorconfig` — Code style and formatting rules
- `/script/UserVars.csx` or `/script/UserVars.json` — Local build path customization
