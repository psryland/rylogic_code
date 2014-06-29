import sys, os
import UserVars
import Rylogic as Tools

missing = [];

if not os.path.exists(UserVars.dumpdir):          missing = missing + [UserVars.dumpdir         ]
if not os.path.exists(UserVars.textedit):         missing = missing + [UserVars.textedit        ]
if not os.path.exists(UserVars.ziptool):          missing = missing + [UserVars.ziptool         ]
if not os.path.exists(UserVars.mergetool):        missing = missing + [UserVars.mergetool       ]
if not os.path.exists(UserVars.msbuild):          missing = missing + [UserVars.msbuild         ]
if not os.path.exists(UserVars.winsdk):           missing = missing + [UserVars.winsdk          ]
if not os.path.exists(UserVars.vs_dir):           missing = missing + [UserVars.vs_dir          ]
if not os.path.exists(UserVars.vc_env):           missing = missing + [UserVars.vc_env          ]
if not os.path.exists(UserVars.devenv):           missing = missing + [UserVars.devenv          ]
if not os.path.exists(UserVars.silverlight_root): missing = missing + [UserVars.silverlight_root]
if not os.path.exists(UserVars.java_sdkdir):      missing = missing + [UserVars.java_sdkdir     ]
if not os.path.exists(UserVars.android_sdkdir):   missing = missing + [UserVars.android_sdkdir  ]
if not os.path.exists(UserVars.adb):              missing = missing + [UserVars.adb             ]
if not os.path.exists(UserVars.fxc):              missing = missing + [UserVars.fxc             ]
if not os.path.exists(UserVars.dmdroot):          missing = missing + [UserVars.dmdroot         ]
if not os.path.exists(UserVars.dmd):              missing = missing + [UserVars.dmd             ]
if not os.path.exists(UserVars.rdmd):             missing = missing + [UserVars.rdmd            ]
if not os.path.exists(UserVars.csex):             missing = missing + [UserVars.csex            ]
if not os.path.exists(UserVars.wwwroot):          missing = missing + [UserVars.wwwroot         ]
if not os.path.exists(UserVars.elevate):          missing = missing + [UserVars.elevate         ]
if not os.path.exists(UserVars.ttbuild):          missing = missing + [UserVars.ttbuild         ]
if not os.path.exists(UserVars.linqpad):          missing = missing + [UserVars.linqpad         ]

if missing != []:
	print("Missing paths:")
	for p in missing:
		print(p)
	Tools.OnError("")
else:
	print("All valid")
	Tools.OnSuccess()
