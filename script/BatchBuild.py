#!/usr/bin/env python3
# -*- coding: utf-8 -*- 
import sys, os, shutil, re
import Rylogic as Tools
import UserVars

try:
	print(
		"*************************************************************************\n"
		"Batch Build\n"
		"Copyright (c) Rylogic 2013\n"
		"*************************************************************************")

	Tools.AssertVersion(1)
	Tools.AssertPathsExist([UserVars.root])

	sln = os.path.join(UserVars.root, "build", "Rylogic.sln")

	# e.g: "\"folder\proj_name:Rebuild\""
	native_libs = [
		"Rylogic\\renderer11",
		"Rylogic\\audio",
		"Rylogic\\physics",
		"Rylogic\\physics2",
		]
	native_dlls = [
		"SDK\\scintilla",
		"SDK\\sqlite3",
		"Rylogic\\view3d",
		"Rylogic\\audio.dll",
		"VSExtensions\\prautoexp",
		]
	native_apps = [
		"Tests\\gui",
		"Tools\\cex",
		"Tools\\p3d",
		"Tools\\elevate",
		"Tools\\KeySpy",
		]
	managed_dlls = [
		"Rylogic\\Rylogic.Core",
		"Rylogic\\Rylogic.Core.Windows",
		"Rylogic\\Rylogic.Gui.WinForms",
		"Rylogic\\Rylogic.Gui.WPF",
		"Rylogic\\Rylogic.Scintilla",
		"Rylogic\\Rylogic.View3d",
		"Rylogic\\Rylogic.DirectShow",
		]
	rylogviewer = [
		"RylogViewer\\RylogViewer",
		"RylogViewer\\RylogViewer.ExamplePlugin",
		"RylogViewer\\RylogViewer.Extensions",
		"RylogViewer\\RylogViewer2",
		]
	ldraw = [
		"LDraw\\LDraw",
		]
	coinflip = [
		"CoinFlip\\Bot.Arbitrage",
		"CoinFlip\\Bot.HeikinAshiChaser",
		"CoinFlip\\Bot.Rebalance",
		"CoinFlip\\ExchApi.Common",
		"CoinFlip\\ExchApi.Binance",
		"CoinFlip\\ExchApi.Bittrex",
		"CoinFlip\\ExchApi.Poloniex",
		"CoinFlip\\CoinFlip.Model",
		"CoinFlip\\CoinFlip.UI",
		]
	tools = [
		"Tools\\Csex",
		"VSExtensions\\Rylogic.TextAligner",
		]
	tests = [
		"Tests\\unittests",
		"Tests\\TestConsoleCS",
		"Tests\\TestCS",
		"Tests\\TestWPF",
		]

	projects = (
		native_libs +
		native_dlls +
		native_apps +
		managed_dlls +
		tests +
		tools +
		ldraw +
		coinflip +
		rylogviewer +
		[])
	platforms = [
		"x86",
		"x64"
		]
	configs = [
		"debug",
		"release"
		]

	if not Tools.MSBuild(sln, projects, platforms, configs, parallel = True, same_window = True):
		Tools.OnError("Errors occurred")
	else:
		Tools.OnSuccess(pause_time_seconds=0)

except Exception as ex:
	Tools.OnException(ex)
