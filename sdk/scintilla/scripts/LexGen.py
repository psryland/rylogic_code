#!/usr/bin/env python3
# LexGen.py - implemented 2002 by Neil Hodgson neilh@scintilla.org
# Released to the public domain.

# Regenerate the Scintilla source files that list all the lexers.
# Should be run whenever a new lexer is added or removed.
# Requires Python 2.5 or later
# Files are regenerated in place with templates stored in comments.
# The format of generation comments is documented in FileGenerator.py.

import os
from FileGenerator import Regenerate, UpdateLineInFile, ReplaceREInFile
import ScintillaData
import HFacer

def UpdateVersionNumbers(sci, root):
    UpdateLineInFile(os.path.join(root, "src", "win32", "ScintRes.rc"),
		f"#define VERSION_SCINTILLA",
        f"#define VERSION_SCINTILLA \"{sci.versionDotted}\"")
    UpdateLineInFile(os.path.join(root, "src", "win32", "ScintRes.rc"),
		f"#define VERSION_WORDS",
        f"#define VERSION_WORDS {sci.versionCommad}")
    #UpdateLineInFile(root + "qt/ScintillaEditBase/ScintillaEditBase.pro",
    #    "VERSION =",
    #    "VERSION = " + sci.versionDotted)
    #UpdateLineInFile(root + "qt/ScintillaEdit/ScintillaEdit.pro",
    #    "VERSION =",
    #    "VERSION = " + sci.versionDotted)
    UpdateLineInFile(os.path.join(root, "doc", "ScintillaDownload.html"),
        f"       Release",
        f"       Release {sci.versionDotted}")
    ReplaceREInFile(os.path.join(root, "doc", "ScintillaDownload.html"),
        f"/scintilla/([a-zA-Z]+)\\d\\d\\d",
        f"/scintilla/\\g<1>{sci.version}")
    UpdateLineInFile(os.path.join(root, "doc", "index.html"),
        f"          <font color=""#FFCC99"" size=""3""> Release version",
        f"          <font color=""#FFCC99"" size=""3""> Release version {sci.versionDotted}<br />")
    UpdateLineInFile(os.path.join(root, "doc", "index.html"),
        f"           Site last modified",
        f"           Site last modified {sci.mdyModified}</font>")
    UpdateLineInFile(os.path.join(root, "doc", "ScintillaHistory.html"),
        f"	Released ",
        f"	Released {sci.dmyModified}.")

def RegenerateAll(root):
    
    sci = ScintillaData.ScintillaData(root)

    Regenerate(os.path.join(root, "src", "Catalogue.cxx"), "//", sci.lexerModules)
    Regenerate(os.path.join(root, "src", "win32", "scintilla.mak"), "#", sci.lexFiles)

    UpdateVersionNumbers(sci, root)
    
    HFacer.RegenerateAll(root, False)

if __name__=="__main__":
   root = os.path.abspath(os.path.join(os.path.dirname(__file__), ".."))
   RegenerateAll(root)
