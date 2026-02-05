#!/usr/bin/env python3
# -*- coding: utf-8 -*- 
import os, subprocess
root = os.path.dirname(__file__)

publish_dir = os.path.join(root, "publish")
if not os.path.exists(publish_dir): os.makedirs(publish_dir)

# Build the extension (parses templates and compiles TypeScript)
subprocess.run(["npm", "run", "build"], shell=True, cwd=root, check=True)

# Package the extension
subprocess.run(["npx", "vsce", "package", "--out", "publish/ldraw-script.vsix"], shell=True, cwd=root)

