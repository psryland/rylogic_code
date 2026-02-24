//**********************************************
// Console Extensions
//  Copyright (c) Rylogic Ltd 2004
//**********************************************
// Rtfm: Output complete markdown documentation of all cex commands.
#include "src/forward.h"
#include "src/commands/commands.h"

namespace cex
{
	struct Cmd_Rtfm
	{
		void ShowHelp() const
		{
			std::cout <<
				"Rtfm: Output complete documentation for all cex commands\n"
				" Syntax: Cex -rtfm\n"
				"\n"
				"  Outputs markdown-formatted reference documentation to stdout.\n";
		}

		int Run(pr::CmdLine const& args)
		{
			if (args.count("help") != 0)
				return ShowHelp(), 0;

			std::cout << R"md(# CEX - Console EXtensions

A multi-purpose command-line utility for Windows. Each command is invoked as:

```
cex -<command> [parameters]
```

Use `cex -<command> -help` for built-in help on any command.

---

## Commands

### automate

Execute a script of mouse, keyboard, and drawing commands against a target window.

```
cex -automate -p <process-name> [-w <window-name>] [-f <script-file>]
```

| Parameter | Required | Description |
|-----------|----------|-------------|
| `-p`      | Yes      | Name (or partial name) of the target process |
| `-w`      | No       | Title (or partial title) of the target window. Default: largest window |
| `-f`      | No       | Path to a script file. Default: read from stdin |

**Script commands** (one per line, `#` for comments):

| Command | Syntax | Description |
|---------|--------|-------------|
| move    | `move x,y` | Move mouse to client-area coordinates |
| click   | `click x,y [button]` | Click at position. Button: left (default), right, middle |
| down    | `down x,y [button]` | Press button down at position |
| up      | `up [button]` | Release button |
| drag    | `drag x1,y1 x2,y2 [N]` | Drag from one point to another in N steps |
| line    | `line x1,y1 x2,y2` | Draw a line between two points |
| circle  | `circle cx,cy r [N]` | Draw a circle with N segments |
| arc     | `arc cx,cy r a0 a1 [N]` | Draw an arc from angle a0 to a1 |
| fill_circle | `fill_circle cx,cy r [N]` | Draw a filled circle |
| type    | `type text...` | Type text using keyboard simulation |
| key     | `key combo` | Press a key combo (e.g. `ctrl+a`, `shift+delete`, `enter`, `f5`) |
| delay   | `delay ms` | Pause for milliseconds |

All coordinates are relative to the window's client area.

---

### clip

Copy text to the clipboard with optional transformations, or paste clipboard contents to stdout.

```
cex -clip "text" [-lwr] [-upr] [-fwdslash] [-bkslash] [-cstr] [-crlf|-cr|-lf]
cex -clip -paste
```

| Parameter    | Description |
|--------------|-------------|
| `-lwr`       | Convert to lower case |
| `-upr`       | Convert to upper case |
| `-fwdslash`  | Convert path separators to forward slashes |
| `-bkslash`   | Convert path separators to back slashes |
| `-cstr`      | Convert to C/C++ escaped string |
| `-crlf`      | Convert newlines to DOS (CR+LF) |
| `-cr`        | Convert newlines to Mac (CR) |
| `-lf`        | Convert newlines to Linux (LF) |
| `-paste`     | Paste clipboard contents to stdout |

---

### dirpath

Open a folder selection dialog and store the chosen path in an environment variable.

```
cex -dirpath <env_var_name> [-msg "Message"]
```

| Parameter | Required | Description |
|-----------|----------|-------------|
| `env_var_name` | Yes | Environment variable to set with the selected path |
| `-msg`    | No       | Message to display in the dialog |

---

### exec

Execute another process, optionally asynchronously.

```
cex -exec [-async] [-cwd <working_dir>] -p <exe_path> [args...]
```

| Parameter | Required | Description |
|-----------|----------|-------------|
| `-p`      | Yes      | Executable path followed by its arguments |
| `-async`  | No       | Return immediately without waiting for completion |
| `-cwd`    | No       | Working directory for the process |

**Returns:** Process exit code (or 0 if async). -1 on error.

---

### find_element

Find a UI element by name using the Windows UI Automation API.

```
cex -find_element -name <text> -p <process-name> [-w <window-name>] [-depth N]
```

| Parameter | Required | Description |
|-----------|----------|-------------|
| `-name`   | Yes      | Text to search for (case-insensitive substring match) |
| `-p`      | Yes      | Name (or partial name) of the target process |
| `-w`      | No       | Title (or partial title) of the target window |
| `-depth`  | No       | Maximum tree depth to traverse. Default: 8 |

**Output:** For each matching element: control type, name, bounding rectangle (screen and client-area coordinates), and center point.

---

### guid

Generate and output a new GUID.

```
cex -guid
```

No parameters. Outputs a GUID string.

---

### hash

Compute a hash of the given input data.

```
cex -hash <data_to_hash>
```

**Output:** 8-character hexadecimal hash value.

---

### hdata

Convert a file into a C/C++ compatible header containing the file's data.

```
cex -hdata -f <src_file> -o <output_header> [-t] [-v]
```

| Parameter | Required | Description |
|-----------|----------|-------------|
| `-f`      | Yes      | Input file path |
| `-o`      | Yes      | Output header file path |
| `-t`      | No       | Output as text (escaped C-string). Default: binary hex bytes |
| `-v`      | No       | Verbose output |

---

### list_windows

List windows belonging to a process, or all windows on the system.

```
cex -list_windows [-p <process-name>] [-all]
```

| Parameter | Required | Description |
|-----------|----------|-------------|
| `-p`      | No       | Name (or partial name) of the target process. If omitted, lists all windows |
| `-all`    | No       | Include hidden and minimised windows |

**Output format:** One line per window:
```
HWND=0x...  WxH (client WxH)  [visible|hidden|minimised]  [process.exe]  'title'
```
The process name is shown when `-p` is omitted.

---

### lwr

Convert a string to lower case.

```
cex -lwr "Text To Lower"
```

**Output:** The input text in lower case.

---

### msgbox

Display a Windows message box.

```
cex -msgbox -title "title" -body "body text" -style <style_id>
```

| Parameter | Required | Description |
|-----------|----------|-------------|
| `-title`  | No       | Dialog title. Default: "Message" |
| `-body`   | No       | Dialog body text |
| `-style`  | No       | Windows MB_* style constant (integer) |

**Returns:** The user's button selection (MessageBox return value).

---

### newlines

Normalise newlines and control consecutive blank lines in a text file.

```
cex -newlines -f <file> [-o <output>] [-limit <min> <max>] [-lineends <style>]
```

| Parameter    | Required | Description |
|--------------|----------|-------------|
| `-f`         | Yes      | Input file path |
| `-o`         | No       | Output file path. Default: overwrite input file |
| `-limit`     | No       | Min and max consecutive newlines allowed |
| `-lineends`  | No       | Line ending style: `CR`, `LF`, `CRLF`, or `LFCR` |

---

### read_dpi

Report the DPI scaling for a monitor.

```
cex -read_dpi [-monitor <index>]
```

| Parameter  | Required | Description |
|------------|----------|-------------|
| `-monitor` | No       | Zero-based monitor index. Default: primary monitor |

**Output format:** `dpi_x dpi_y scale_percent` (e.g. `144 144 150`)

---

### read_text

Read text content from a window's UI elements using the Windows UI Automation API.

```
cex -read_text -p <process-name> [-w <window-name>] [-depth N]
```

| Parameter | Required | Description |
|-----------|----------|-------------|
| `-p`      | Yes      | Name (or partial name) of the target process |
| `-w`      | No       | Title (or partial title) of the target window |
| `-depth`  | No       | Maximum tree depth to traverse. Default: 5 |

**Output:** Indented element tree showing control type, name, and text content.

---

### screenshot

Capture windows of a process to PNG files.

```
cex -screenshot -p <process-name> -o <output-dir> [-all] [-bitblt] [-scale N]
```

| Parameter | Required | Description |
|-----------|----------|-------------|
| `-p`      | Yes      | Name (or partial name) of the process to capture |
| `-o`      | Yes      | Output directory for PNG files |
| `-all`    | No       | Also capture hidden/minimised windows |
| `-bitblt` | No       | Capture from screen DC (works for GPU-rendered apps like Electron/Chromium; window must be visible and in foreground) |
| `-scale`  | No       | Scale factor (e.g. `0.25` for quarter size) |

**Output:** PNG files named `<process>.<window-title>.png`. Duplicates are suffixed `-1`, `-2`, etc.

---

### send_keys

Send text as keyboard input to a window.

```
cex -send_keys "text" -p <process-name> [-w <window-name>] [-rate <keys_per_sec>]
```

| Parameter | Required | Description |
|-----------|----------|-------------|
| text      | Yes      | Text to type (positional argument) |
| `-p`      | Yes      | Name (or partial name) of the target process |
| `-w`      | No       | Title (or partial title) of the target window |
| `-rate`   | No       | Key press rate in keys/second. Default: 10 |

Uses SendInput with KEYEVENTF_UNICODE. Brings the window to the foreground automatically.

---

### send_mouse

Send a mouse event to a window at client-area coordinates.

```
cex -send_mouse x,y -b <action> -p <process-name> [-w <window-name>]
```

| Parameter | Required | Description |
|-----------|----------|-------------|
| `x,y`     | Yes      | Client-area coordinates (comma-separated, positional) |
| `-b`      | Yes      | Button action (see below) |
| `-p`      | Yes      | Name (or partial name) of the target process |
| `-w`      | No       | Title (or partial title) of the target window |

**Button actions:** `LeftDown`, `LeftUp`, `LeftClick`, `RightDown`, `RightUp`, `RightClick`, `MiddleDown`, `MiddleUp`, `MiddleClick`, `Move`

Click actions send both down and up events.

---

### shutdown_process

Gracefully shut down a process by sending WM_CLOSE to its windows.

```
cex -shutdown_process -p <process-name> [-w <window-name>] [-timeout <ms>]
```

| Parameter  | Required | Description |
|------------|----------|-------------|
| `-p`       | Yes      | Name (or partial name) of the target process |
| `-w`       | No       | Title (or partial title) of a specific window. Default: all windows |
| `-timeout` | No       | Milliseconds to wait for exit. Default: 5000. Use 0 to skip waiting |

**Returns:** 0 on success, 1 on timeout, -1 on error.

Equivalent to clicking the window's close button -- allows the app to save state and clean up.

---

### wait

Pause execution for a specified duration.

```
cex -wait <seconds> [-msg "message"]
```

| Parameter | Required | Description |
|-----------|----------|-------------|
| seconds   | Yes      | Time to wait in seconds (positional) |
| `-msg`    | No       | Message to display while waiting |

---

### wait_window

Wait for a window to appear, polling until found or timeout.

```
cex -wait_window -p <process-name> [-w <window-name>] [-timeout <ms>]
```

| Parameter  | Required | Description |
|------------|----------|-------------|
| `-p`       | Yes      | Name (or partial name) of the target process |
| `-w`       | No       | Title (or partial title) to wait for. Default: any window |
| `-timeout` | No       | Maximum wait in milliseconds. Default: 30000 |

**Returns:** 0 when window is found, 1 on timeout.
**Output:** Window title, dimensions, and elapsed time on success.

---

### rtfm

Output this documentation in markdown format.

```
cex -rtfm
```

No parameters. Outputs complete command reference to stdout.

---

## Notes

- cex is a Windows subsystem application (no console window by default). When run from a console (cmd/PowerShell), it attaches to the parent console for stdout/stderr.
- For programmatic use, redirect stdout: `Start-Process cex.exe '-command args' -RedirectStandardOutput out.txt -Wait`
- Process name matching (`-p`) is case-insensitive substring matching.
- Window name matching (`-w`) is case-insensitive substring matching.
- All coordinate parameters use the window's client-area coordinate system unless otherwise noted.
- cex is DPI-aware (Per-Monitor V2). All coordinates and captures use physical pixels.
)md";
			return 0;
		}
	};

	int Rtfm(pr::CmdLine const& args)
	{
		Cmd_Rtfm cmd;
		return cmd.Run(args);
	}
}
