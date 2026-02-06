# Copilot Instructions for PictureFrame

A Python tkinter application that displays images and videos in a slideshow format, designed to run on Raspberry Pi as a digital picture frame.

## Running the Application

```bash
# Scan for images/videos (builds image_list.txt)
python picture.py --scan

# Run the slideshow
python picture.py

# Compile to standalone exe (Windows)
pip install pyinstaller
pyinstaller --onefile --noconsole picture.py

# Profile performance
python -m cProfile -o profile.out picture.py
pip install snakeviz && snakeviz profile.out
```

## Dependencies

```bash
# Linux/Raspberry Pi
sudo apt install mpv python3-mpv python3-pathspec

# Windows (pip)
pip install python-mpv pathspec
```

## Architecture

### Core Components

- **`PictureFrame` class**: Main application - handles UI, media playback, and slideshow logic
- **MPV player**: Embedded video/image renderer (python-mpv wrapper)
- **tkinter**: UI framework with fullscreen support

### Configuration Files

| File | Purpose |
|------|---------|
| `config.json` | Runtime settings (display period, fullscreen, paths) |
| `image_list.txt` | Generated list of media file paths (created by `--scan`) |
| `ignore_patterns.txt` | Gitignore-style patterns for files to skip |
| `displayed_image_log.txt` | Log of recently displayed media |

### Platform Paths

Config uses platform-specific image roots:
- `ImageRoot-Windows`: Windows path (e.g., `Z:\Pictures\Albums`)
- `ImageRoot-Linux`: Linux path (e.g., `/mnt/ZDrive/Pictures/Albums`)

## Key Conventions

### Memory Leak Prevention

`window.after()` callbacks must be tracked and cancelled to prevent memory leaks:
- Use `_ScheduleAfter()` instead of direct `window.after()` calls
- Call `_CancelPendingAfterCallbacks()` before changing images or shutdown
- See `pending_after_ids` list for callback tracking

### Issue Number Pattern

The `issue_number` counter prevents stale callbacks from affecting new images:
```python
self.issue_number += 1
# Pass issue_number to callbacks, check it matches before executing
if issue_number != self.issue_number: return
```

### Raspberry Pi Setup

- Requires X11 (not Wayland): `sudo raspi-config` → Advanced → Wayland → X11
- MPV config in `~/.config/mpv/mpv.conf`:
  ```
  vo=gpu
  hwdec=v4l2m2m
  x11-bypass-compositor=yes
  ```
