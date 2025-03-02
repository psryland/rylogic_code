#!/usr/bin/env python3
# -*- coding: utf-8 -*- 
import os, subprocess
root = os.path.dirname(__file__)

publish_dir = os.path.join(root, "publish")
if not os.path.exists(publish_dir): os.makedirs(publish_dir)

subprocess.run(["npx", "vsce", "package", "--out", "publish/ldraw-script.vsix"], shell=True, cwd=root)

