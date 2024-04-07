#!/usr/bin/env python3
# -*- coding: utf-8 -*- 

import sys, os, glob

proj_root = os.path.abspath(os.path.join(os.path.dirname(__file__), "."))
repo_root = os.path.abspath(os.path.join(os.path.dirname(__file__), "../../.."))

sys.path += [os.path.join(repo_root, "script")]
from Make import Builder, CompileTask, LinkTask, Join

class Project(Builder):
	def __init__(self):
		super().__init__(__file__)
		self.project_name = "basic"
		self.target_filename = "basic.exe"
		self.outdir = Join(proj_root, "obj")
		return
	
	def Setup(self):
		super().Setup()

		# Gather the source files
		self.src += glob.glob(Join(proj_root, "src", "*.cpp"))

		# Set the include paths
		self.inc += [
			Join(proj_root, "src"),
		]

		# Project level defines
		self.defines += [
			"_DEBUG",
			"_CRT_SECURE_NO_WARNINGS",
			"_SCL_SECURE_NO_WARNINGS",
			"_ENABLE_EXTENDED_ALIGNED_STORAGE",
			"_SILENCE_CXX17_CODECVT_HEADER_DEPRECATION_WARNING",
			"_WIN32_WINNT=_WIN32_WINNT_WIN10",
			"NOMINMAX",
			"WIN32_LEAN_AND_MEAN",
			"GDIPVER=0x0110",
		]

		# Add tasks
		self.tasks += [
			CompileTask(),
			LinkTask(),
		]
		return


# Run the project
if __name__ == "__main__":
	Project().Build(sys.argv)