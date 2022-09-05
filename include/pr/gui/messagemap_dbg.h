#pragma once
#include <Windows.h>
#include <commctrl.h>
#include "pr/common/fmt.h"
#include "pr/macros/enum.h"
#include "pr/win32/win32.h"

namespace pr::gui
{
	#pragma region WM_MSGS
	#define PR_ENUM(x)\
	x(wm_NULL                           ,"WM_NULL"                           , = 0x0000)\
	x(wm_CREATE                         ,"WM_CREATE"                         , = 0x0001)\
	x(wm_DESTROY                        ,"WM_DESTROY"                        , = 0x0002)\
	x(wm_MOVE                           ,"WM_MOVE"                           , = 0x0003)\
	x(wm_SIZEWAIT                       ,"WM_SIZEWAIT"                       , = 0x0004)\
	x(wm_SIZE                           ,"WM_SIZE"                           , = 0x0005)\
	x(wm_ACTIVATE                       ,"WM_ACTIVATE"                       , = 0x0006)\
	x(wm_SETFOCUS                       ,"WM_SETFOCUS"                       , = 0x0007)\
	x(wm_KILLFOCUS                      ,"WM_KILLFOCUS"                      , = 0x0008)\
	x(wm_SETVISIBLE                     ,"WM_SETVISIBLE"                     , = 0x0009)\
	x(wm_ENABLE                         ,"WM_ENABLE"                         , = 0x000a)\
	x(wm_SETREDRAW                      ,"WM_SETREDRAW"                      , = 0x000b)\
	x(wm_SETTEXT                        ,"WM_SETTEXT"                        , = 0x000c)\
	x(wm_GETTEXT                        ,"WM_GETTEXT"                        , = 0x000d)\
	x(wm_GETTEXTLENGTH                  ,"WM_GETTEXTLENGTH"                  , = 0x000e)\
	x(wm_PAINT                          ,"WM_PAINT"                          , = 0x000f)\
	x(wm_CLOSE                          ,"WM_CLOSE"                          , = 0x0010)\
	x(wm_QUERYENDSESSION                ,"WM_QUERYENDSESSION"                , = 0x0011)\
	x(wm_QUIT                           ,"WM_QUIT"                           , = 0x0012)\
	x(wm_QUERYOPEN                      ,"WM_QUERYOPEN"                      , = 0x0013)\
	x(wm_ERASEBKGND                     ,"WM_ERASEBKGND"                     , = 0x0014)\
	x(wm_SYSCOLORCHANGE                 ,"WM_SYSCOLORCHANGE"                 , = 0x0015)\
	x(wm_ENDSESSION                     ,"WM_ENDSESSION"                     , = 0x0016)\
	x(wm_SYSTEMERROR                    ,"WM_SYSTEMERROR"                    , = 0x0017)\
	x(wm_SHOWWINDOW                     ,"WM_SHOWWINDOW"                     , = 0x0018)\
	x(wm_CTLCOLOR                       ,"WM_CTLCOLOR"                       , = 0x0019)\
	x(wm_WININICHANGE                   ,"WM_WININICHANGE"                   , = 0x001a)\
	x(wm_DEVMODECHANGE                  ,"WM_DEVMODECHANGE"                  , = 0x001b)\
	x(wm_ACTIVATEAPP                    ,"WM_ACTIVATEAPP"                    , = 0x001c)\
	x(wm_FONTCHANGE                     ,"WM_FONTCHANGE"                     , = 0x001d)\
	x(wm_TIMECHANGE                     ,"WM_TIMECHANGE"                     , = 0x001e)\
	x(wm_CANCELMODE                     ,"WM_CANCELMODE"                     , = 0x001f)\
	x(wm_SETCURSOR                      ,"WM_SETCURSOR"                      , = 0x0020)\
	x(wm_MOUSEACTIVATE                  ,"WM_MOUSEACTIVATE"                  , = 0x0021)\
	x(wm_CHILDACTIVATE                  ,"WM_CHILDACTIVATE"                  , = 0x0022)\
	x(wm_QUEUESYNC                      ,"WM_QUEUESYNC"                      , = 0x0023)\
	x(wm_GETMINMAXINFO                  ,"WM_GETMINMAXINFO"                  , = 0x0024)\
	x(wm_LOGOFF                         ,"WM_LOGOFF"                         , = 0x0025)\
	x(wm_PAINTICON                      ,"WM_PAINTICON"                      , = 0x0026)\
	x(wm_ICONERASEBKGND                 ,"WM_ICONERASEBKGND"                 , = 0x0027)\
	x(wm_NEXTDLGCTL                     ,"WM_NEXTDLGCTL"                     , = 0x0028)\
	x(wm_ALTTABACTIVE                   ,"WM_ALTTABACTIVE"                   , = 0x0029)\
	x(wm_SPOOLERSTATUS                  ,"WM_SPOOLERSTATUS"                  , = 0x002a)\
	x(wm_DRAWITEM                       ,"WM_DRAWITEM"                       , = 0x002b)\
	x(wm_MEASUREITEM                    ,"WM_MEASUREITEM"                    , = 0x002c)\
	x(wm_DELETEITEM                     ,"WM_DELETEITEM"                     , = 0x002d)\
	x(wm_VKEYTOITEM                     ,"WM_VKEYTOITEM"                     , = 0x002e)\
	x(wm_CHARTOITEM                     ,"WM_CHARTOITEM"                     , = 0x002f)\
	x(wm_SETFONT                        ,"WM_SETFONT"                        , = 0x0030)\
	x(wm_GETFONT                        ,"WM_GETFONT"                        , = 0x0031)\
	x(wm_SETHOTKEY                      ,"WM_SETHOTKEY"                      , = 0x0032)\
	x(wm_GETHOTKEY                      ,"WM_GETHOTKEY"                      , = 0x0033)\
	x(wm_SHELLNOTIFY                    ,"WM_SHELLNOTIFY"                    , = 0x0034)\
	x(wm_ISACTIVEICON                   ,"WM_ISACTIVEICON"                   , = 0x0035)\
	x(wm_QUERYPARKICON                  ,"WM_QUERYPARKICON"                  , = 0x0036)\
	x(wm_QUERYDRAGICON                  ,"WM_QUERYDRAGICON"                  , = 0x0037)\
	x(wm_WINHELP                        ,"WM_WINHELP"                        , = 0x0038)\
	x(wm_COMPAREITEM                    ,"WM_COMPAREITEM"                    , = 0x0039)\
	x(wm_FULLSCREEN                     ,"WM_FULLSCREEN"                     , = 0x003a)\
	x(wm_CLIENTSHUTDOWN                 ,"WM_CLIENTSHUTDOWN"                 , = 0x003b)\
	x(wm_DDEMLEVENT                     ,"WM_DDEMLEVENT"                     , = 0x003c)\
	x(wm_GETOBJECT                      ,"WM_GETOBJECT"                      , = 0x003d)\
	x(wm_1                              ,"undefined_1"                       , = 0x003e)\
	x(wm_2                              ,"undefined_2"                       , = 0x003f)\
	x(wm_TESTING                        ,"WM_TESTING"                        , = 0x0040)\
	x(wm_COMPACTING                     ,"WM_COMPACTING"                     , = 0x0041)\
	x(wm_OTHERWINDOWCREATED             ,"WM_OTHERWINDOWCREATED"             , = 0x0042)\
	x(wm_OTHERWINDOWDESTROYED           ,"WM_OTHERWINDOWDESTROYED"           , = 0x0043)\
	x(wm_COMMNOTIFY                     ,"WM_COMMNOTIFY"                     , = 0x0044)\
	x(wm_3                              ,"undefined_3"                       , = 0x0045)\
	x(wm_WINDOWPOSCHANGING              ,"WM_WINDOWPOSCHANGING"              , = 0x0046)\
	x(wm_WINDOWPOSCHANGED               ,"WM_WINDOWPOSCHANGED"               , = 0x0047)\
	x(wm_POWER                          ,"WM_POWER"                          , = 0x0048)\
	x(wm_COPYGLOBALDATA                 ,"WM_COPYGLOBALDATA"                 , = 0x0049)\
	x(wm_COPYDATA                       ,"WM_COPYDATA"                       , = 0x004a)\
	x(wm_CANCELJOURNAL                  ,"WM_CANCELJOURNAL"                  , = 0x004b)\
	x(wm_4                              ,"undefined_4"                       , = 0x004c)\
	x(wm_KEYF1                          ,"WM_KEYF1"                          , = 0x004d)\
	x(wm_NOTIFY                         ,"WM_NOTIFY"                         , = 0x004e)\
	x(wm_ACCESS_WINDOW                  ,"WM_ACCESS_WINDOW"                  , = 0x004f)\
	x(wm_INPUTLANGCHANGEREQUEST         ,"WM_INPUTLANGCHANGEREQUEST"         , = 0x0050)\
	x(wm_INPUTLANGCHANGE                ,"WM_INPUTLANGCHANGE"                , = 0x0051)\
	x(wm_TCARD                          ,"WM_TCARD"                          , = 0x0052)\
	x(wm_HELP                           ,"WM_HELP"                           , = 0x0053)\
	x(wm_USERCHANGED                    ,"WM_USERCHANGED"                    , = 0x0054)\
	x(wm_NOTIFYFORMAT                   ,"WM_NOTIFYFORMAT"                   , = 0x0055)\
	x(wm_5                              ,"undefined_5"                       , = 0x0056)\
	x(wm_6                              ,"undefined_6"                       , = 0x0057)\
	x(wm_7                              ,"undefined_7"                       , = 0x0058)\
	x(wm_8                              ,"undefined_8"                       , = 0x0059)\
	x(wm_9                              ,"undefined_9"                       , = 0x005a)\
	x(wm_10                             ,"undefined_10"                      , = 0x005b)\
	x(wm_11                             ,"undefined_11"                      , = 0x005c)\
	x(wm_12                             ,"undefined_12"                      , = 0x005d)\
	x(wm_13                             ,"undefined_13"                      , = 0x005e)\
	x(wm_14                             ,"undefined_14"                      , = 0x005f)\
	x(wm_15                             ,"undefined_15"                      , = 0x0060)\
	x(wm_16                             ,"undefined_16"                      , = 0x0061)\
	x(wm_17                             ,"undefined_17"                      , = 0x0062)\
	x(wm_18                             ,"undefined_18"                      , = 0x0063)\
	x(wm_19                             ,"undefined_19"                      , = 0x0064)\
	x(wm_20                             ,"undefined_20"                      , = 0x0065)\
	x(wm_21                             ,"undefined_21"                      , = 0x0066)\
	x(wm_22                             ,"undefined_22"                      , = 0x0067)\
	x(wm_23                             ,"undefined_23"                      , = 0x0068)\
	x(wm_24                             ,"undefined_24"                      , = 0x0069)\
	x(wm_25                             ,"undefined_25"                      , = 0x006a)\
	x(wm_26                             ,"undefined_26"                      , = 0x006b)\
	x(wm_27                             ,"undefined_27"                      , = 0x006c)\
	x(wm_28                             ,"undefined_28"                      , = 0x006d)\
	x(wm_29                             ,"undefined_29"                      , = 0x006e)\
	x(wm_30                             ,"undefined_30"                      , = 0x006f)\
	x(wm_FINALDESTROY                   ,"WM_FINALDESTROY"                   , = 0x0070)\
	x(wm_MEASUREITEM_CLIENTDATA         ,"WM_MEASUREITEM_CLIENTDATA"         , = 0x0071)\
	x(wm_31                             ,"undefined_31"                      , = 0x0072)\
	x(wm_32                             ,"undefined_32"                      , = 0x0073)\
	x(wm_33                             ,"undefined_33"                      , = 0x0074)\
	x(wm_34                             ,"undefined_34"                      , = 0x0075)\
	x(wm_35                             ,"undefined_35"                      , = 0x0076)\
	x(wm_36                             ,"undefined_36"                      , = 0x0077)\
	x(wm_37                             ,"undefined_37"                      , = 0x0078)\
	x(wm_38                             ,"undefined_38"                      , = 0x0079)\
	x(wm_39                             ,"undefined_39"                      , = 0x007a)\
	x(wm_CONTEXTMENU                    ,"WM_CONTEXTMENU"                    , = 0x007b)\
	x(wm_STYLECHANGING                  ,"WM_STYLECHANGING"                  , = 0x007c)\
	x(wm_STYLECHANGED                   ,"WM_STYLECHANGED"                   , = 0x007d)\
	x(wm_DISPLAYCHANGE                  ,"WM_DISPLAYCHANGE"                  , = 0x007e)\
	x(wm_GETICON                        ,"WM_GETICON"                        , = 0x007f)\
	x(wm_SETICON                        ,"WM_SETICON"                        , = 0x0080)\
	x(wm_NCCREATE                       ,"WM_NCCREATE"                       , = 0x0081)\
	x(wm_NCDESTROY                      ,"WM_NCDESTROY"                      , = 0x0082)\
	x(wm_NCCALCSIZE                     ,"WM_NCCALCSIZE"                     , = 0x0083)\
	x(wm_NCHITTEST                      ,"WM_NCHITTEST"                      , = 0x0084)\
	x(wm_NCPAINT                        ,"WM_NCPAINT"                        , = 0x0085)\
	x(wm_NCACTIVATE                     ,"WM_NCACTIVATE"                     , = 0x0086)\
	x(wm_GETDLGCODE                     ,"WM_GETDLGCODE"                     , = 0x0087)\
	x(wm_SYNCPAINT                      ,"WM_SYNCPAINT"                      , = 0x0088)\
	x(wm_SYNCTASK                       ,"WM_SYNCTASK"                       , = 0x0089)\
	x(wm_40                             ,"undefined_40"                      , = 0x008a)\
	x(wm_KLUDGEMINRECT                  ,"WM_KLUDGEMINRECT"                  , = 0x008b)\
	x(wm_LPKDRAWSWITCHWND               ,"WM_LPKDRAWSWITCHWND"               , = 0x008c)\
	x(wm_41                             ,"undefined_41"                      , = 0x008d)\
	x(wm_42                             ,"undefined_42"                      , = 0x008e)\
	x(wm_43                             ,"undefined_43"                      , = 0x008f)\
	x(wm_UAHDESTROYWINDOW               ,"WM_UAHDESTROYWINDOW"               , = 0x0090)\
	x(wm_UAHDRAWMENU                    ,"WM_UAHDRAWMENU"                    , = 0x0091)\
	x(wm_UAHDRAWMENUITEM                ,"WM_UAHDRAWMENUITEM"                , = 0x0092)\
	x(wm_UAHINITMENU                    ,"WM_UAHINITMENU"                    , = 0x0093)\
	x(wm_UAHMEASUREMENUITEM             ,"WM_UAHMEASUREMENUITEM"             , = 0x0094)\
	x(wm_UAHNCPAINTMENUPOPUP            ,"WM_UAHNCPAINTMENUPOPUP"            , = 0x0095)\
	x(wm_UAHUPDATE                      ,"WM_UAHUPDATE"                      , = 0x0096)\
	x(wm_44                             ,"undefined_44"                      , = 0x0097)\
	x(wm_45                             ,"undefined_45"                      , = 0x0098)\
	x(wm_46                             ,"undefined_46"                      , = 0x0099)\
	x(wm_47                             ,"undefined_47"                      , = 0x009a)\
	x(wm_48                             ,"undefined_48"                      , = 0x009b)\
	x(wm_49                             ,"undefined_49"                      , = 0x009c)\
	x(wm_50                             ,"undefined_50"                      , = 0x009d)\
	x(wm_51                             ,"undefined_51"                      , = 0x009e)\
	x(wm_52                             ,"undefined_52"                      , = 0x009f)\
	x(wm_NCMOUSEMOVE                    ,"WM_NCMOUSEMOVE"                    , = 0x00a0)\
	x(wm_NCLBUTTONDOWN                  ,"WM_NCLBUTTONDOWN"                  , = 0x00a1)\
	x(wm_NCLBUTTONUP                    ,"WM_NCLBUTTONUP"                    , = 0x00a2)\
	x(wm_NCLBUTTONDBLCLK                ,"WM_NCLBUTTONDBLCLK"                , = 0x00a3)\
	x(wm_NCRBUTTONDOWN                  ,"WM_NCRBUTTONDOWN"                  , = 0x00a4)\
	x(wm_NCRBUTTONUP                    ,"WM_NCRBUTTONUP"                    , = 0x00a5)\
	x(wm_NCRBUTTONDBLCLK                ,"WM_NCRBUTTONDBLCLK"                , = 0x00a6)\
	x(wm_NCMBUTTONDOWN                  ,"WM_NCMBUTTONDOWN"                  , = 0x00a7)\
	x(wm_NCMBUTTONUP                    ,"WM_NCMBUTTONUP"                    , = 0x00a8)\
	x(wm_NCMBUTTONDBLCLK                ,"WM_NCMBUTTONDBLCLK"                , = 0x00a9)\
	x(wm_53                             ,"undefined_53"                      , = 0x00aa)\
	x(wm_NCXBUTTONDOWN                  ,"WM_NCXBUTTONDOWN"                  , = 0x00ab)\
	x(wm_NCXBUTTONUP                    ,"WM_NCXBUTTONUP"                    , = 0x00ac)\
	x(wm_NCXBUTTONDBLCLK                ,"WM_NCXBUTTONDBLCLK"                , = 0x00ad)\
	x(wm_NCUAHDRAWCAPTION               ,"WM_NCUAHDRAWCAPTION"               , = 0x00ae)\
	x(wm_NCUAHDRAWFRAME                 ,"WM_NCUAHDRAWFRAME"                 , = 0x00af)\
	x(wm_EM_GETSEL                      ,"EM_GETSEL"                         , = 0x00b0)\
	x(wm_EM_SETSEL                      ,"EM_SETSEL"                         , = 0x00b1)\
	x(wm_EM_GETRECT                     ,"EM_GETRECT"                        , = 0x00b2)\
	x(wm_EM_SETRECT                     ,"EM_SETRECT"                        , = 0x00b3)\
	x(wm_EM_SETRECTNP                   ,"EM_SETRECTNP"                      , = 0x00b4)\
	x(wm_EM_SCROLL                      ,"EM_SCROLL"                         , = 0x00b5)\
	x(wm_EM_LINESCROLL                  ,"EM_LINESCROLL"                     , = 0x00b6)\
	x(wm_EM_SCROLLCARET                 ,"EM_SCROLLCARET"                    , = 0x00b7)\
	x(wm_EM_GETMODIFY                   ,"EM_GETMODIFY"                      , = 0x00b8)\
	x(wm_EM_SETMODIFY                   ,"EM_SETMODIFY"                      , = 0x00b9)\
	x(wm_EM_GETLINECOUNT                ,"EM_GETLINECOUNT"                   , = 0x00ba)\
	x(wm_EM_LINEINDEX                   ,"EM_LINEINDEX"                      , = 0x00bb)\
	x(wm_EM_SETHANDLE                   ,"EM_SETHANDLE"                      , = 0x00bc)\
	x(wm_EM_GETHANDLE                   ,"EM_GETHANDLE"                      , = 0x00bd)\
	x(wm_EM_GETTHUMB                    ,"EM_GETTHUMB"                       , = 0x00be)\
	x(wm_54                             ,"undefined_54"                      , = 0x00bf)\
	x(wm_55                             ,"undefined_55"                      , = 0x00c0)\
	x(wm_EM_LINELENGTH                  ,"EM_LINELENGTH"                     , = 0x00c1)\
	x(wm_EM_REPLACESEL                  ,"EM_REPLACESEL"                     , = 0x00c2)\
	x(wm_EM_SETFONT                     ,"EM_SETFONT"                        , = 0x00c3)\
	x(wm_EM_GETLINE                     ,"EM_GETLINE"                        , = 0x00c4)\
	x(wm_EM_LIMITTEXT                   ,"EM_LIMITTEXT"                      , = 0x00c5)\
	x(wm_EM_CANUNDO                     ,"EM_CANUNDO"                        , = 0x00c6)\
	x(wm_EM_UNDO                        ,"EM_UNDO"                           , = 0x00c7)\
	x(wm_EM_FMTLINES                    ,"EM_FMTLINES"                       , = 0x00c8)\
	x(wm_EM_LINEFROMCHAR                ,"EM_LINEFROMCHAR"                   , = 0x00c9)\
	x(wm_EM_SETWORDBREAK                ,"EM_SETWORDBREAK"                   , = 0x00ca)\
	x(wm_EM_SETTABSTOPS                 ,"EM_SETTABSTOPS"                    , = 0x00cb)\
	x(wm_EM_SETPASSWORDCHAR             ,"EM_SETPASSWORDCHAR"                , = 0x00cc)\
	x(wm_EM_EMPTYUNDOBUFFER             ,"EM_EMPTYUNDOBUFFER"                , = 0x00cd)\
	x(wm_EM_GETFIRSTVISIBLELINE         ,"EM_GETFIRSTVISIBLELINE"            , = 0x00ce)\
	x(wm_EM_SETREADONLY                 ,"EM_SETREADONLY"                    , = 0x00cf)\
	x(wm_EM_SETWORDBREAKPROC            ,"EM_SETWORDBREAKPROC"               , = 0x00d0)\
	x(wm_EM_GETWORDBREAKPROC            ,"EM_GETWORDBREAKPROC"               , = 0x00d1)\
	x(wm_EM_GETPASSWORDCHAR             ,"EM_GETPASSWORDCHAR"                , = 0x00d2)\
	x(wm_EM_SETMARGINS                  ,"EM_SETMARGINS"                     , = 0x00d3)\
	x(wm_EM_GETMARGINS                  ,"EM_GETMARGINS"                     , = 0x00d4)\
	x(wm_EM_GETLIMITTEXT                ,"EM_GETLIMITTEXT"                   , = 0x00d5)\
	x(wm_EM_POSFROMCHAR                 ,"EM_POSFROMCHAR"                    , = 0x00d6)\
	x(wm_EM_CHARFROMPOS                 ,"EM_CHARFROMPOS"                    , = 0x00d7)\
	x(wm_EM_SETIMESTATUS                ,"EM_SETIMESTATUS"                   , = 0x00d8)\
	x(wm_EM_GETIMESTATUS                ,"EM_GETIMESTATUS"                   , = 0x00d9)\
	x(wm_EM_MSGMAX                      ,"EM_MSGMAX"                         , = 0x00da)\
	x(wm_56                             ,"undefined_56"                      , = 0x00db)\
	x(wm_57                             ,"undefined_57"                      , = 0x00dc)\
	x(wm_58                             ,"undefined_58"                      , = 0x00dd)\
	x(wm_59                             ,"undefined_59"                      , = 0x00de)\
	x(wm_60                             ,"undefined_60"                      , = 0x00df)\
	x(wm_61                             ,"undefined_61"                      , = 0x00e0)\
	x(wm_62                             ,"undefined_62"                      , = 0x00e1)\
	x(wm_63                             ,"undefined_63"                      , = 0x00e2)\
	x(wm_64                             ,"undefined_64"                      , = 0x00e3)\
	x(wm_65                             ,"undefined_65"                      , = 0x00e4)\
	x(wm_66                             ,"undefined_66"                      , = 0x00e5)\
	x(wm_67                             ,"undefined_67"                      , = 0x00e6)\
	x(wm_68                             ,"undefined_68"                      , = 0x00e7)\
	x(wm_69                             ,"undefined_69"                      , = 0x00e8)\
	x(wm_70                             ,"undefined_70"                      , = 0x00e9)\
	x(wm_71                             ,"undefined_71"                      , = 0x00ea)\
	x(wm_72                             ,"undefined_72"                      , = 0x00eb)\
	x(wm_73                             ,"undefined_73"                      , = 0x00ec)\
	x(wm_74                             ,"undefined_74"                      , = 0x00ed)\
	x(wm_75                             ,"undefined_75"                      , = 0x00ee)\
	x(wm_76                             ,"undefined_76"                      , = 0x00ef)\
	x(wm_77                             ,"undefined_77"                      , = 0x00f0)\
	x(wm_78                             ,"undefined_78"                      , = 0x00f1)\
	x(wm_79                             ,"undefined_79"                      , = 0x00f2)\
	x(wm_80                             ,"undefined_80"                      , = 0x00f3)\
	x(wm_81                             ,"undefined_81"                      , = 0x00f4)\
	x(wm_82                             ,"undefined_82"                      , = 0x00f5)\
	x(wm_83                             ,"undefined_83"                      , = 0x00f6)\
	x(wm_84                             ,"undefined_84"                      , = 0x00f7)\
	x(wm_85                             ,"undefined_85"                      , = 0x00f8)\
	x(wm_86                             ,"undefined_86"                      , = 0x00f9)\
	x(wm_87                             ,"undefined_87"                      , = 0x00fa)\
	x(wm_88                             ,"undefined_88"                      , = 0x00fb)\
	x(wm_89                             ,"undefined_89"                      , = 0x00fc)\
	x(wm_90                             ,"undefined_90"                      , = 0x00fd)\
	x(wm_INPUT_DEVICE_CHANGE            ,"WM_INPUT_DEVICE_CHANGE"            , = 0x00fe)\
	x(wm_INPUT                          ,"WM_INPUT"                          , = 0x00ff)\
	x(wm_KEYDOWN                        ,"WM_KEYDOWN"                        , = 0x0100)\
	x(wm_KEYUP                          ,"WM_KEYUP"                          , = 0x0101)\
	x(wm_CHAR                           ,"WM_CHAR"                           , = 0x0102)\
	x(wm_DEADCHAR                       ,"WM_DEADCHAR"                       , = 0x0103)\
	x(wm_SYSKEYDOWN                     ,"WM_SYSKEYDOWN"                     , = 0x0104)\
	x(wm_SYSKEYUP                       ,"WM_SYSKEYUP"                       , = 0x0105)\
	x(wm_SYSCHAR                        ,"WM_SYSCHAR"                        , = 0x0106)\
	x(wm_SYSDEADCHAR                    ,"WM_SYSDEADCHAR"                    , = 0x0107)\
	x(wm_YOMICHAR                       ,"WM_YOMICHAR"                       , = 0x0108)\
	x(wm_UNICHAR                        ,"WM_UNICHAR"                        , = 0x0109)\
	x(wm_CONVERTREQUEST                 ,"WM_CONVERTREQUEST"                 , = 0x010a)\
	x(wm_CONVERTRESULT                  ,"WM_CONVERTRESULT"                  , = 0x010b)\
	x(wm_INTERIM                        ,"WM_INTERIM"                        , = 0x010c)\
	x(wm_IME_STARTCOMPOSITION           ,"WM_IME_STARTCOMPOSITION"           , = 0x010d)\
	x(wm_IME_ENDCOMPOSITION             ,"WM_IME_ENDCOMPOSITION"             , = 0x010e)\
	x(wm_IME_COMPOSITION                ,"WM_IME_COMPOSITION"                , = 0x010f)\
	x(wm_INITDIALOG                     ,"WM_INITDIALOG"                     , = 0x0110)\
	x(wm_COMMAND                        ,"WM_COMMAND"                        , = 0x0111)\
	x(wm_SYSCOMMAND                     ,"WM_SYSCOMMAND"                     , = 0x0112)\
	x(wm_TIMER                          ,"WM_TIMER"                          , = 0x0113)\
	x(wm_HSCROLL                        ,"WM_HSCROLL"                        , = 0x0114)\
	x(wm_VSCROLL                        ,"WM_VSCROLL"                        , = 0x0115)\
	x(wm_INITMENU                       ,"WM_INITMENU"                       , = 0x0116)\
	x(wm_INITMENUPOPUP                  ,"WM_INITMENUPOPUP"                  , = 0x0117)\
	x(wm_SYSTIMER                       ,"WM_SYSTIMER"                       , = 0x0118)\
	x(wm_GESTURE                        ,"WM_GESTURE"                        , = 0x0119)\
	x(wm_GESTURENOTIFY                  ,"WM_GESTURENOTIFY"                  , = 0x011a)\
	x(wm_GESTUREINPUT                   ,"WM_GESTUREINPUT"                   , = 0x011b)\
	x(wm_GESTURENOTIFIED                ,"WM_GESTURENOTIFIED"                , = 0x011c)\
	x(wm_91                             ,"undefined_91"                      , = 0x011d)\
	x(wm_92                             ,"undefined_92"                      , = 0x011e)\
	x(wm_MENUSELECT                     ,"WM_MENUSELECT"                     , = 0x011f)\
	x(wm_MENUCHAR                       ,"WM_MENUCHAR"                       , = 0x0120)\
	x(wm_ENTERIDLE                      ,"WM_ENTERIDLE"                      , = 0x0121)\
	x(wm_MENURBUTTONUP                  ,"WM_MENURBUTTONUP"                  , = 0x0122)\
	x(wm_MENUDRAG                       ,"WM_MENUDRAG"                       , = 0x0123)\
	x(wm_MENUGETOBJECT                  ,"WM_MENUGETOBJECT"                  , = 0x0124)\
	x(wm_UNINITMENUPOPUP                ,"WM_UNINITMENUPOPUP"                , = 0x0125)\
	x(wm_MENUCOMMAND                    ,"WM_MENUCOMMAND"                    , = 0x0126)\
	x(wm_CHANGEUISTATE                  ,"WM_CHANGEUISTATE"                  , = 0x0127)\
	x(wm_UPDATEUISTATE                  ,"WM_UPDATEUISTATE"                  , = 0x0128)\
	x(wm_QUERYUISTATE                   ,"WM_QUERYUISTATE"                   , = 0x0129)\
	x(wm_93                             ,"undefined_93"                      , = 0x012a)\
	x(wm_94                             ,"undefined_94"                      , = 0x012b)\
	x(wm_95                             ,"undefined_95"                      , = 0x012c)\
	x(wm_96                             ,"undefined_96"                      , = 0x012d)\
	x(wm_97                             ,"undefined_97"                      , = 0x012e)\
	x(wm_98                             ,"undefined_98"                      , = 0x012f)\
	x(wm_99                             ,"undefined_99"                      , = 0x0130)\
	x(wm_LBTRACKPOINT                   ,"WM_LBTRACKPOINT"                   , = 0x0131)\
	x(wm_CTLCOLORMSGBOX                 ,"WM_CTLCOLORMSGBOX"                 , = 0x0132)\
	x(wm_CTLCOLOREDIT                   ,"WM_CTLCOLOREDIT"                   , = 0x0133)\
	x(wm_CTLCOLORLISTBOX                ,"WM_CTLCOLORLISTBOX"                , = 0x0134)\
	x(wm_CTLCOLORBTN                    ,"WM_CTLCOLORBTN"                    , = 0x0135)\
	x(wm_CTLCOLORDLG                    ,"WM_CTLCOLORDLG"                    , = 0x0136)\
	x(wm_CTLCOLORSCROLLBAR              ,"WM_CTLCOLORSCROLLBAR"              , = 0x0137)\
	x(wm_CTLCOLORSTATIC                 ,"WM_CTLCOLORSTATIC"                 , = 0x0138)\
	x(wm_100                            ,"undefined_100"                     , = 0x0139)\
	x(wm_101                            ,"undefined_101"                     , = 0x013a)\
	x(wm_102                            ,"undefined_102"                     , = 0x013b)\
	x(wm_103                            ,"undefined_103"                     , = 0x013c)\
	x(wm_104                            ,"undefined_104"                     , = 0x013d)\
	x(wm_105                            ,"undefined_105"                     , = 0x013e)\
	x(wm_106                            ,"undefined_106"                     , = 0x013f)\
	x(wm_CB_GETEDITSEL                  ,"CB_GETEDITSEL"                     , = 0x0140)\
	x(wm_CB_LIMITTEXT                   ,"CB_LIMITTEXT"                      , = 0x0141)\
	x(wm_CB_SETEDITSEL                  ,"CB_SETEDITSEL"                     , = 0x0142)\
	x(wm_CB_ADDSTRING                   ,"CB_ADDSTRING"                      , = 0x0143)\
	x(wm_CB_DELETESTRING                ,"CB_DELETESTRING"                   , = 0x0144)\
	x(wm_CB_DIR                         ,"CB_DIR"                            , = 0x0145)\
	x(wm_CB_GETCOUNT                    ,"CB_GETCOUNT"                       , = 0x0146)\
	x(wm_CB_GETCURSEL                   ,"CB_GETCURSEL"                      , = 0x0147)\
	x(wm_CB_GETLBTEXT                   ,"CB_GETLBTEXT"                      , = 0x0148)\
	x(wm_CB_GETLBTEXTLEN                ,"CB_GETLBTEXTLEN"                   , = 0x0149)\
	x(wm_CB_INSERTSTRING                ,"CB_INSERTSTRING"                   , = 0x014a)\
	x(wm_CB_RESETCONTENT                ,"CB_RESETCONTENT"                   , = 0x014b)\
	x(wm_CB_FINDSTRING                  ,"CB_FINDSTRING"                     , = 0x014c)\
	x(wm_CB_SELECTSTRING                ,"CB_SELECTSTRING"                   , = 0x014d)\
	x(wm_CB_SETCURSEL                   ,"CB_SETCURSEL"                      , = 0x014e)\
	x(wm_CB_SHOWDROPDOWN                ,"CB_SHOWDROPDOWN"                   , = 0x014f)\
	x(wm_CB_GETITEMDATA                 ,"CB_GETITEMDATA"                    , = 0x0150)\
	x(wm_CB_SETITEMDATA                 ,"CB_SETITEMDATA"                    , = 0x0151)\
	x(wm_CB_GETDROPPEDCONTROLRECT       ,"CB_GETDROPPEDCONTROLRECT"          , = 0x0152)\
	x(wm_CB_SETITEMHEIGHT               ,"CB_SETITEMHEIGHT"                  , = 0x0153)\
	x(wm_CB_GETITEMHEIGHT               ,"CB_GETITEMHEIGHT"                  , = 0x0154)\
	x(wm_CB_SETEXTENDEDUI               ,"CB_SETEXTENDEDUI"                  , = 0x0155)\
	x(wm_CB_GETEXTENDEDUI               ,"CB_GETEXTENDEDUI"                  , = 0x0156)\
	x(wm_CB_GETDROPPEDSTATE             ,"CB_GETDROPPEDSTATE"                , = 0x0157)\
	x(wm_CB_FINDSTRINGEXACT             ,"CB_FINDSTRINGEXACT"                , = 0x0158)\
	x(wm_CB_SETLOCALE                   ,"CB_SETLOCALE"                      , = 0x0159)\
	x(wm_CB_GETLOCALE                   ,"CB_GETLOCALE"                      , = 0x015a)\
	x(wm_CB_GETTOPINDEX                 ,"CB_GETTOPINDEX"                    , = 0x015b)\
	x(wm_CB_SETTOPINDEX                 ,"CB_SETTOPINDEX"                    , = 0x015c)\
	x(wm_CB_GETHORIZONTALEXTENT         ,"CB_GETHORIZONTALEXTENT"            , = 0x015d)\
	x(wm_CB_SETHORIZONTALEXTENT         ,"CB_SETHORIZONTALEXTENT"            , = 0x015e)\
	x(wm_CB_GETDROPPEDWIDTH             ,"CB_GETDROPPEDWIDTH"                , = 0x015f)\
	x(wm_CB_SETDROPPEDWIDTH             ,"CB_SETDROPPEDWIDTH"                , = 0x0160)\
	x(wm_CB_INITSTORAGE                 ,"CB_INITSTORAGE"                    , = 0x0161)\
	x(wm_CB_MSGMAX_OLD                  ,"CB_MSGMAX_OLD"                     , = 0x0162)\
	x(wm_CB_MULTIPLEADDSTRING           ,"CB_MULTIPLEADDSTRING"              , = 0x0163)\
	x(wm_CB_GETCOMBOBOXINFO             ,"CB_GETCOMBOBOXINFO"                , = 0x0164)\
	x(wm_CB_MSGMAX                      ,"CB_MSGMAX"                         , = 0x0165)\
	x(wm_107                            ,"undefined_107"                     , = 0x0166)\
	x(wm_108                            ,"undefined_108"                     , = 0x0167)\
	x(wm_109                            ,"undefined_109"                     , = 0x0168)\
	x(wm_110                            ,"undefined_110"                     , = 0x0169)\
	x(wm_111                            ,"undefined_111"                     , = 0x016a)\
	x(wm_112                            ,"undefined_112"                     , = 0x016b)\
	x(wm_113                            ,"undefined_113"                     , = 0x016c)\
	x(wm_114                            ,"undefined_114"                     , = 0x016d)\
	x(wm_115                            ,"undefined_115"                     , = 0x016e)\
	x(wm_116                            ,"undefined_116"                     , = 0x016f)\
	x(wm_117                            ,"undefined_117"                     , = 0x0170)\
	x(wm_118                            ,"undefined_118"                     , = 0x0171)\
	x(wm_119                            ,"undefined_119"                     , = 0x0172)\
	x(wm_120                            ,"undefined_120"                     , = 0x0173)\
	x(wm_121                            ,"undefined_121"                     , = 0x0174)\
	x(wm_122                            ,"undefined_122"                     , = 0x0175)\
	x(wm_123                            ,"undefined_123"                     , = 0x0176)\
	x(wm_124                            ,"undefined_124"                     , = 0x0177)\
	x(wm_125                            ,"undefined_125"                     , = 0x0178)\
	x(wm_126                            ,"undefined_126"                     , = 0x0179)\
	x(wm_127                            ,"undefined_127"                     , = 0x017a)\
	x(wm_128                            ,"undefined_128"                     , = 0x017b)\
	x(wm_129                            ,"undefined_129"                     , = 0x017c)\
	x(wm_130                            ,"undefined_130"                     , = 0x017d)\
	x(wm_131                            ,"undefined_131"                     , = 0x017e)\
	x(wm_132                            ,"undefined_132"                     , = 0x017f)\
	x(wm_LB_ADDSTRING                   ,"LB_ADDSTRING"                      , = 0x0180)\
	x(wm_LB_INSERTSTRING                ,"LB_INSERTSTRING"                   , = 0x0181)\
	x(wm_LB_DELETESTRING                ,"LB_DELETESTRING"                   , = 0x0182)\
	x(wm_LB_SELITEMRANGEEX              ,"LB_SELITEMRANGEEX"                 , = 0x0183)\
	x(wm_LB_RESETCONTENT                ,"LB_RESETCONTENT"                   , = 0x0184)\
	x(wm_LB_SETSEL                      ,"LB_SETSEL"                         , = 0x0185)\
	x(wm_LB_SETCURSEL                   ,"LB_SETCURSEL"                      , = 0x0186)\
	x(wm_LB_GETSEL                      ,"LB_GETSEL"                         , = 0x0187)\
	x(wm_LB_GETCURSEL                   ,"LB_GETCURSEL"                      , = 0x0188)\
	x(wm_LB_GETTEXT                     ,"LB_GETTEXT"                        , = 0x0189)\
	x(wm_LB_GETTEXTLEN                  ,"LB_GETTEXTLEN"                     , = 0x018a)\
	x(wm_LB_GETCOUNT                    ,"LB_GETCOUNT"                       , = 0x018b)\
	x(wm_LB_SELECTSTRING                ,"LB_SELECTSTRING"                   , = 0x018c)\
	x(wm_LB_DIR                         ,"LB_DIR"                            , = 0x018d)\
	x(wm_LB_GETTOPINDEX                 ,"LB_GETTOPINDEX"                    , = 0x018e)\
	x(wm_LB_FINDSTRING                  ,"LB_FINDSTRING"                     , = 0x018f)\
	x(wm_LB_GETSELCOUNT                 ,"LB_GETSELCOUNT"                    , = 0x0190)\
	x(wm_LB_GETSELITEMS                 ,"LB_GETSELITEMS"                    , = 0x0191)\
	x(wm_LB_SETTABSTOPS                 ,"LB_SETTABSTOPS"                    , = 0x0192)\
	x(wm_LB_GETHORIZONTALEXTENT         ,"LB_GETHORIZONTALEXTENT"            , = 0x0193)\
	x(wm_LB_SETHORIZONTALEXTENT         ,"LB_SETHORIZONTALEXTENT"            , = 0x0194)\
	x(wm_LB_SETCOLUMNWIDTH              ,"LB_SETCOLUMNWIDTH"                 , = 0x0195)\
	x(wm_LB_ADDFILE                     ,"LB_ADDFILE"                        , = 0x0196)\
	x(wm_LB_SETTOPINDEX                 ,"LB_SETTOPINDEX"                    , = 0x0197)\
	x(wm_LB_GETITEMRECT                 ,"LB_GETITEMRECT"                    , = 0x0198)\
	x(wm_LB_GETITEMDATA                 ,"LB_GETITEMDATA"                    , = 0x0199)\
	x(wm_LB_SETITEMDATA                 ,"LB_SETITEMDATA"                    , = 0x019a)\
	x(wm_LB_SELITEMRANGE                ,"LB_SELITEMRANGE"                   , = 0x019b)\
	x(wm_LB_SETANCHORINDEX              ,"LB_SETANCHORINDEX"                 , = 0x019c)\
	x(wm_LB_GETANCHORINDEX              ,"LB_GETANCHORINDEX"                 , = 0x019d)\
	x(wm_LB_SETCARETINDEX               ,"LB_SETCARETINDEX"                  , = 0x019e)\
	x(wm_LB_GETCARETINDEX               ,"LB_GETCARETINDEX"                  , = 0x019f)\
	x(wm_LB_SETITEMHEIGHT               ,"LB_SETITEMHEIGHT"                  , = 0x01a0)\
	x(wm_LB_GETITEMHEIGHT               ,"LB_GETITEMHEIGHT"                  , = 0x01a1)\
	x(wm_LB_FINDSTRINGEXACT             ,"LB_FINDSTRINGEXACT"                , = 0x01a2)\
	x(wm_LBCB_CARETON                   ,"LBCB_CARETON"                      , = 0x01a3)\
	x(wm_LBCB_CARETOFF                  ,"LBCB_CARETOFF"                     , = 0x01a4)\
	x(wm_LB_SETLOCALE                   ,"LB_SETLOCALE"                      , = 0x01a5)\
	x(wm_LB_GETLOCALE                   ,"LB_GETLOCALE"                      , = 0x01a6)\
	x(wm_LB_SETCOUNT                    ,"LB_SETCOUNT"                       , = 0x01a7)\
	x(wm_LB_INITSTORAGE                 ,"LB_INITSTORAGE"                    , = 0x01a8)\
	x(wm_LB_ITEMFROMPOINT               ,"LB_ITEMFROMPOINT"                  , = 0x01a9)\
	x(wm_LB_INSERTSTRINGUPPER           ,"LB_INSERTSTRINGUPPER"              , = 0x01aa)\
	x(wm_LB_INSERTSTRINGLOWER           ,"LB_INSERTSTRINGLOWER"              , = 0x01ab)\
	x(wm_LB_ADDSTRINGUPPER              ,"LB_ADDSTRINGUPPER"                 , = 0x01ac)\
	x(wm_LB_ADDSTRINGLOWER              ,"LB_ADDSTRINGLOWER"                 , = 0x01ad)\
	x(wm_LBCB_STARTTRACK                ,"LBCB_STARTTRACK"                   , = 0x01ae)\
	x(wm_LBCB_ENDTRACK                  ,"LBCB_ENDTRACK"                     , = 0x01af)\
	x(wm_LB_MSGMAX_OLD                  ,"LB_MSGMAX_OLD"                     , = 0x01b0)\
	x(wm_LB_MULTIPLEADDSTRING           ,"LB_MULTIPLEADDSTRING"              , = 0x01b1)\
	x(wm_LB_GETLISTBOXINFO              ,"LB_GETLISTBOXINFO"                 , = 0x01b2)\
	x(wm_LB_MSGMAX                      ,"LB_MSGMAX"                         , = 0x01b3)\
	x(wm_133                            ,"undefined_133"                     , = 0x01b4)\
	x(wm_134                            ,"undefined_134"                     , = 0x01b5)\
	x(wm_135                            ,"undefined_135"                     , = 0x01b6)\
	x(wm_136                            ,"undefined_136"                     , = 0x01b7)\
	x(wm_137                            ,"undefined_137"                     , = 0x01b8)\
	x(wm_138                            ,"undefined_138"                     , = 0x01b9)\
	x(wm_139                            ,"undefined_139"                     , = 0x01ba)\
	x(wm_140                            ,"undefined_140"                     , = 0x01bb)\
	x(wm_141                            ,"undefined_141"                     , = 0x01bc)\
	x(wm_142                            ,"undefined_142"                     , = 0x01bd)\
	x(wm_143                            ,"undefined_143"                     , = 0x01be)\
	x(wm_144                            ,"undefined_144"                     , = 0x01bf)\
	x(wm_145                            ,"undefined_145"                     , = 0x01c0)\
	x(wm_146                            ,"undefined_146"                     , = 0x01c1)\
	x(wm_147                            ,"undefined_147"                     , = 0x01c2)\
	x(wm_148                            ,"undefined_148"                     , = 0x01c3)\
	x(wm_149                            ,"undefined_149"                     , = 0x01c4)\
	x(wm_150                            ,"undefined_150"                     , = 0x01c5)\
	x(wm_151                            ,"undefined_151"                     , = 0x01c6)\
	x(wm_152                            ,"undefined_152"                     , = 0x01c7)\
	x(wm_153                            ,"undefined_153"                     , = 0x01c8)\
	x(wm_154                            ,"undefined_154"                     , = 0x01c9)\
	x(wm_155                            ,"undefined_155"                     , = 0x01ca)\
	x(wm_156                            ,"undefined_156"                     , = 0x01cb)\
	x(wm_157                            ,"undefined_157"                     , = 0x01cc)\
	x(wm_158                            ,"undefined_158"                     , = 0x01cd)\
	x(wm_159                            ,"undefined_159"                     , = 0x01ce)\
	x(wm_160                            ,"undefined_160"                     , = 0x01cf)\
	x(wm_161                            ,"undefined_161"                     , = 0x01d0)\
	x(wm_162                            ,"undefined_162"                     , = 0x01d1)\
	x(wm_163                            ,"undefined_163"                     , = 0x01d2)\
	x(wm_164                            ,"undefined_164"                     , = 0x01d3)\
	x(wm_165                            ,"undefined_165"                     , = 0x01d4)\
	x(wm_166                            ,"undefined_166"                     , = 0x01d5)\
	x(wm_167                            ,"undefined_167"                     , = 0x01d6)\
	x(wm_168                            ,"undefined_168"                     , = 0x01d7)\
	x(wm_169                            ,"undefined_169"                     , = 0x01d8)\
	x(wm_170                            ,"undefined_170"                     , = 0x01d9)\
	x(wm_171                            ,"undefined_171"                     , = 0x01da)\
	x(wm_172                            ,"undefined_172"                     , = 0x01db)\
	x(wm_173                            ,"undefined_173"                     , = 0x01dc)\
	x(wm_174                            ,"undefined_174"                     , = 0x01dd)\
	x(wm_175                            ,"undefined_175"                     , = 0x01de)\
	x(wm_176                            ,"undefined_176"                     , = 0x01df)\
	x(wm_MN_FIRST                       ,"MN_FIRST"                          , = 0x01e0)\
	x(wm_MN_GETHMENU                    ,"MN_GETHMENU"                       , = 0x01e1)\
	x(wm_177                            ,"undefined_177"                     , = 0x01e2)\
	x(wm_178                            ,"undefined_178"                     , = 0x01e3)\
	x(wm_179                            ,"undefined_179"                     , = 0x01e4)\
	x(wm_180                            ,"undefined_180"                     , = 0x01e5)\
	x(wm_181                            ,"undefined_181"                     , = 0x01e6)\
	x(wm_182                            ,"undefined_182"                     , = 0x01e7)\
	x(wm_183                            ,"undefined_183"                     , = 0x01e8)\
	x(wm_184                            ,"undefined_184"                     , = 0x01e9)\
	x(wm_185                            ,"undefined_185"                     , = 0x01ea)\
	x(wm_186                            ,"undefined_186"                     , = 0x01eb)\
	x(wm_187                            ,"undefined_187"                     , = 0x01ec)\
	x(wm_188                            ,"undefined_188"                     , = 0x01ed)\
	x(wm_189                            ,"undefined_189"                     , = 0x01ee)\
	x(wm_190                            ,"undefined_190"                     , = 0x01ef)\
	x(wm_191                            ,"undefined_191"                     , = 0x01f0)\
	x(wm_192                            ,"undefined_192"                     , = 0x01f1)\
	x(wm_193                            ,"undefined_193"                     , = 0x01f2)\
	x(wm_194                            ,"undefined_194"                     , = 0x01f3)\
	x(wm_195                            ,"undefined_195"                     , = 0x01f4)\
	x(wm_196                            ,"undefined_196"                     , = 0x01f5)\
	x(wm_197                            ,"undefined_197"                     , = 0x01f6)\
	x(wm_198                            ,"undefined_198"                     , = 0x01f7)\
	x(wm_199                            ,"undefined_199"                     , = 0x01f8)\
	x(wm_200                            ,"undefined_200"                     , = 0x01f9)\
	x(wm_201                            ,"undefined_201"                     , = 0x01fa)\
	x(wm_202                            ,"undefined_202"                     , = 0x01fb)\
	x(wm_203                            ,"undefined_203"                     , = 0x01fc)\
	x(wm_204                            ,"undefined_204"                     , = 0x01fd)\
	x(wm_205                            ,"undefined_205"                     , = 0x01fe)\
	x(wm_206                            ,"undefined_206"                     , = 0x01ff)\
	x(wm_MOUSEMOVE                      ,"WM_MOUSEMOVE"                      , = 0x0200)\
	x(wm_LBUTTONDOWN                    ,"WM_LBUTTONDOWN"                    , = 0x0201)\
	x(wm_LBUTTONUP                      ,"WM_LBUTTONUP"                      , = 0x0202)\
	x(wm_LBUTTONDBLCLK                  ,"WM_LBUTTONDBLCLK"                  , = 0x0203)\
	x(wm_RBUTTONDOWN                    ,"WM_RBUTTONDOWN"                    , = 0x0204)\
	x(wm_RBUTTONUP                      ,"WM_RBUTTONUP"                      , = 0x0205)\
	x(wm_RBUTTONDBLCLK                  ,"WM_RBUTTONDBLCLK"                  , = 0x0206)\
	x(wm_MBUTTONDOWN                    ,"WM_MBUTTONDOWN"                    , = 0x0207)\
	x(wm_MBUTTONUP                      ,"WM_MBUTTONUP"                      , = 0x0208)\
	x(wm_MBUTTONDBLCLK                  ,"WM_MBUTTONDBLCLK"                  , = 0x0209)\
	x(wm_MOUSEWHEEL                     ,"WM_MOUSEWHEEL"                     , = 0x020a)\
	x(wm_XBUTTONDOWN                    ,"WM_XBUTTONDOWN"                    , = 0x020b)\
	x(wm_XBUTTONUP                      ,"WM_XBUTTONUP"                      , = 0x020c)\
	x(wm_XBUTTONDBLCLK                  ,"WM_XBUTTONDBLCLK"                  , = 0x020d)\
	x(wm_MOUSEHWHEEL                    ,"WM_MOUSEHWHEEL"                    , = 0x020e)\
	x(wm_207                            ,"undefined_207"                     , = 0x020f)\
	x(wm_PARENTNOTIFY                   ,"WM_PARENTNOTIFY"                   , = 0x0210)\
	x(wm_ENTERMENULOOP                  ,"WM_ENTERMENULOOP"                  , = 0x0211)\
	x(wm_EXITMENULOOP                   ,"WM_EXITMENULOOP"                   , = 0x0212)\
	x(wm_NEXTMENU                       ,"WM_NEXTMENU"                       , = 0x0213)\
	x(wm_SIZING                         ,"WM_SIZING"                         , = 0x0214)\
	x(wm_CAPTURECHANGED                 ,"WM_CAPTURECHANGED"                 , = 0x0215)\
	x(wm_MOVING                         ,"WM_MOVING"                         , = 0x0216)\
	x(wm_208                            ,"undefined_208"                     , = 0x0217)\
	x(wm_POWERBROADCAST                 ,"WM_POWERBROADCAST"                 , = 0x0218)\
	x(wm_DEVICECHANGE                   ,"WM_DEVICECHANGE"                   , = 0x0219)\
	x(wm_209                            ,"undefined_209"                     , = 0x021a)\
	x(wm_210                            ,"undefined_210"                     , = 0x021b)\
	x(wm_211                            ,"undefined_211"                     , = 0x021c)\
	x(wm_212                            ,"undefined_212"                     , = 0x021d)\
	x(wm_213                            ,"undefined_213"                     , = 0x021e)\
	x(wm_214                            ,"undefined_214"                     , = 0x021f)\
	x(wm_MDICREATE                      ,"WM_MDICREATE"                      , = 0x0220)\
	x(wm_MDIDESTROY                     ,"WM_MDIDESTROY"                     , = 0x0221)\
	x(wm_MDIACTIVATE                    ,"WM_MDIACTIVATE"                    , = 0x0222)\
	x(wm_MDIRESTORE                     ,"WM_MDIRESTORE"                     , = 0x0223)\
	x(wm_MDINEXT                        ,"WM_MDINEXT"                        , = 0x0224)\
	x(wm_MDIMAXIMIZE                    ,"WM_MDIMAXIMIZE"                    , = 0x0225)\
	x(wm_MDITILE                        ,"WM_MDITILE"                        , = 0x0226)\
	x(wm_MDICASCADE                     ,"WM_MDICASCADE"                     , = 0x0227)\
	x(wm_MDIICONARRANGE                 ,"WM_MDIICONARRANGE"                 , = 0x0228)\
	x(wm_MDIGETACTIVE                   ,"WM_MDIGETACTIVE"                   , = 0x0229)\
	x(wm_DROPOBJECT                     ,"WM_DROPOBJECT"                     , = 0x022a)\
	x(wm_QUERYDROPOBJECT                ,"WM_QUERYDROPOBJECT"                , = 0x022b)\
	x(wm_BEGINDRAG                      ,"WM_BEGINDRAG"                      , = 0x022c)\
	x(wm_DRAGLOOP                       ,"WM_DRAGLOOP"                       , = 0x022d)\
	x(wm_DRAGSELECT                     ,"WM_DRAGSELECT"                     , = 0x022e)\
	x(wm_DRAGMOVE                       ,"WM_DRAGMOVE"                       , = 0x022f)\
	x(wm_MDISETMENU                     ,"WM_MDISETMENU"                     , = 0x0230)\
	x(wm_ENTERSIZEMOVE                  ,"WM_ENTERSIZEMOVE"                  , = 0x0231)\
	x(wm_EXITSIZEMOVE                   ,"WM_EXITSIZEMOVE"                   , = 0x0232)\
	x(wm_DROPFILES                      ,"WM_DROPFILES"                      , = 0x0233)\
	x(wm_MDIREFRESHMENU                 ,"WM_MDIREFRESHMENU"                 , = 0x0234)\
	x(wm_215                            ,"undefined_215"                     , = 0x0235)\
	x(wm_216                            ,"undefined_216"                     , = 0x0236)\
	x(wm_217                            ,"undefined_217"                     , = 0x0237)\
	x(wm_POINTERDEVICECHANGE            ,"WM_POINTERDEVICECHANGE"            , = 0x0238)\
	x(wm_POINTERDEVICEINRANGE           ,"WM_POINTERDEVICEINRANGE"           , = 0x0239)\
	x(wm_POINTERDEVICEOUTOFRANGE        ,"WM_POINTERDEVICEOUTOFRANGE"        , = 0x023a)\
	x(wm_STOPINERTIA                    ,"WM_STOPINERTIA"                    , = 0x023b)\
	x(wm_ENDINERTIA                     ,"WM_ENDINERTIA"                     , = 0x023c)\
	x(wm_EDGYINERTIA                    ,"WM_EDGYINERTIA"                    , = 0x023d)\
	x(wm_218                            ,"undefined_218"                     , = 0x023e)\
	x(wm_219                            ,"undefined_219"                     , = 0x023f)\
	x(wm_TOUCH                          ,"WM_TOUCH"                          , = 0x0240)\
	x(wm_NCPOINTERUPDATE                ,"WM_NCPOINTERUPDATE"                , = 0x0241)\
	x(wm_NCPOINTERDOWN                  ,"WM_NCPOINTERDOWN"                  , = 0x0242)\
	x(wm_NCPOINTERUP                    ,"WM_NCPOINTERUP"                    , = 0x0243)\
	x(wm_NCPOINTERLAST                  ,"WM_NCPOINTERLAST"                  , = 0x0244)\
	x(wm_POINTERUPDATE                  ,"WM_POINTERUPDATE"                  , = 0x0245)\
	x(wm_POINTERDOWN                    ,"WM_POINTERDOWN"                    , = 0x0246)\
	x(wm_POINTERUP                      ,"WM_POINTERUP"                      , = 0x0247)\
	x(wm_POINTER_reserved_248           ,"WM_POINTER_reserved_248"           , = 0x0248)\
	x(wm_POINTERENTER                   ,"WM_POINTERENTER"                   , = 0x0249)\
	x(wm_POINTERLEAVE                   ,"WM_POINTERLEAVE"                   , = 0x024a)\
	x(wm_POINTERACTIVATE                ,"WM_POINTERACTIVATE"                , = 0x024b)\
	x(wm_POINTERCAPTURECHANGED          ,"WM_POINTERCAPTURECHANGED"          , = 0x024c)\
	x(wm_TOUCHHITTESTING                ,"WM_TOUCHHITTESTING"                , = 0x024d)\
	x(wm_POINTERWHEEL                   ,"WM_POINTERWHEEL"                   , = 0x024e)\
	x(wm_POINTERHWHEEL                  ,"WM_POINTERHWHEEL"                  , = 0x024f)\
	x(wm_POINTER_reserved_250           ,"WM_POINTER_reserved_250"           , = 0x0250)\
	x(wm_POINTER_reserved_251           ,"WM_POINTER_reserved_251"           , = 0x0251)\
	x(wm_POINTER_reserved_252           ,"WM_POINTER_reserved_252"           , = 0x0252)\
	x(wm_POINTER_reserved_253           ,"WM_POINTER_reserved_253"           , = 0x0253)\
	x(wm_POINTER_reserved_254           ,"WM_POINTER_reserved_254"           , = 0x0254)\
	x(wm_POINTER_reserved_255           ,"WM_POINTER_reserved_255"           , = 0x0255)\
	x(wm_POINTER_reserved_256           ,"WM_POINTER_reserved_256"           , = 0x0256)\
	x(wm_POINTERLAST                    ,"WM_POINTERLAST"                    , = 0x0257)\
	x(wm_220                            ,"undefined_220"                     , = 0x0258)\
	x(wm_221                            ,"undefined_221"                     , = 0x0259)\
	x(wm_222                            ,"undefined_222"                     , = 0x025a)\
	x(wm_223                            ,"undefined_223"                     , = 0x025b)\
	x(wm_224                            ,"undefined_224"                     , = 0x025c)\
	x(wm_225                            ,"undefined_225"                     , = 0x025d)\
	x(wm_226                            ,"undefined_226"                     , = 0x025e)\
	x(wm_227                            ,"undefined_227"                     , = 0x025f)\
	x(wm_228                            ,"undefined_228"                     , = 0x0260)\
	x(wm_229                            ,"undefined_229"                     , = 0x0261)\
	x(wm_230                            ,"undefined_230"                     , = 0x0262)\
	x(wm_231                            ,"undefined_231"                     , = 0x0263)\
	x(wm_232                            ,"undefined_232"                     , = 0x0264)\
	x(wm_233                            ,"undefined_233"                     , = 0x0265)\
	x(wm_234                            ,"undefined_234"                     , = 0x0266)\
	x(wm_235                            ,"undefined_235"                     , = 0x0267)\
	x(wm_236                            ,"undefined_236"                     , = 0x0268)\
	x(wm_237                            ,"undefined_237"                     , = 0x0269)\
	x(wm_238                            ,"undefined_238"                     , = 0x026a)\
	x(wm_239                            ,"undefined_239"                     , = 0x026b)\
	x(wm_240                            ,"undefined_240"                     , = 0x026c)\
	x(wm_241                            ,"undefined_241"                     , = 0x026d)\
	x(wm_242                            ,"undefined_242"                     , = 0x026e)\
	x(wm_243                            ,"undefined_243"                     , = 0x026f)\
	x(wm_VISIBILITYCHANGED              ,"WM_VISIBILITYCHANGED"              , = 0x0270)\
	x(wm_VIEWSTATECHANGED               ,"WM_VIEWSTATECHANGED"               , = 0x0271)\
	x(wm_UNREGISTER_WINDOW_SERVICES     ,"WM_UNREGISTER_WINDOW_SERVICES"     , = 0x0272)\
	x(wm_CONSOLIDATED                   ,"WM_CONSOLIDATED"                   , = 0x0273)\
	x(wm_244                            ,"undefined_244"                     , = 0x0274)\
	x(wm_245                            ,"undefined_245"                     , = 0x0275)\
	x(wm_246                            ,"undefined_246"                     , = 0x0276)\
	x(wm_247                            ,"undefined_247"                     , = 0x0277)\
	x(wm_248                            ,"undefined_248"                     , = 0x0278)\
	x(wm_249                            ,"undefined_249"                     , = 0x0279)\
	x(wm_250                            ,"undefined_250"                     , = 0x027a)\
	x(wm_251                            ,"undefined_251"                     , = 0x027b)\
	x(wm_252                            ,"undefined_252"                     , = 0x027c)\
	x(wm_253                            ,"undefined_253"                     , = 0x027d)\
	x(wm_254                            ,"undefined_254"                     , = 0x027e)\
	x(wm_255                            ,"undefined_255"                     , = 0x027f)\
	x(wm_IME_REPORT                     ,"WM_IME_REPORT"                     , = 0x0280)\
	x(wm_IME_SETCONTEXT                 ,"WM_IME_SETCONTEXT"                 , = 0x0281)\
	x(wm_IME_NOTIFY                     ,"WM_IME_NOTIFY"                     , = 0x0282)\
	x(wm_IME_CONTROL                    ,"WM_IME_CONTROL"                    , = 0x0283)\
	x(wm_IME_COMPOSITIONFULL            ,"WM_IME_COMPOSITIONFULL"            , = 0x0284)\
	x(wm_IME_SELECT                     ,"WM_IME_SELECT"                     , = 0x0285)\
	x(wm_IME_CHAR                       ,"WM_IME_CHAR"                       , = 0x0286)\
	x(wm_IME_SYSTEM                     ,"WM_IME_SYSTEM"                     , = 0x0287)\
	x(wm_IME_REQUEST                    ,"WM_IME_REQUEST"                    , = 0x0288)\
	x(wm_KANJI_reserved_289             ,"WM_KANJI_reserved_289"             , = 0x0289)\
	x(wm_KANJI_reserved_28a             ,"WM_KANJI_reserved_28a"             , = 0x028a)\
	x(wm_KANJI_reserved_28b             ,"WM_KANJI_reserved_28b"             , = 0x028b)\
	x(wm_KANJI_reserved_28c             ,"WM_KANJI_reserved_28c"             , = 0x028c)\
	x(wm_KANJI_reserved_28d             ,"WM_KANJI_reserved_28d"             , = 0x028d)\
	x(wm_KANJI_reserved_28e             ,"WM_KANJI_reserved_28e"             , = 0x028e)\
	x(wm_KANJI_reserved_28f             ,"WM_KANJI_reserved_28f"             , = 0x028f)\
	x(wm_IME_KEYDOWN                    ,"WM_IME_KEYDOWN"                    , = 0x0290)\
	x(wm_IME_KEYUP                      ,"WM_IME_KEYUP"                      , = 0x0291)\
	x(wm_KANJI_reserved_292             ,"WM_KANJI_reserved_292"             , = 0x0292)\
	x(wm_KANJI_reserved_293             ,"WM_KANJI_reserved_293"             , = 0x0293)\
	x(wm_KANJI_reserved_294             ,"WM_KANJI_reserved_294"             , = 0x0294)\
	x(wm_KANJI_reserved_295             ,"WM_KANJI_reserved_295"             , = 0x0295)\
	x(wm_KANJI_reserved_296             ,"WM_KANJI_reserved_296"             , = 0x0296)\
	x(wm_KANJI_reserved_297             ,"WM_KANJI_reserved_297"             , = 0x0297)\
	x(wm_KANJI_reserved_298             ,"WM_KANJI_reserved_298"             , = 0x0298)\
	x(wm_KANJI_reserved_299             ,"WM_KANJI_reserved_299"             , = 0x0299)\
	x(wm_KANJI_reserved_29a             ,"WM_KANJI_reserved_29a"             , = 0x029a)\
	x(wm_KANJI_reserved_29b             ,"WM_KANJI_reserved_29b"             , = 0x029b)\
	x(wm_KANJI_reserved_29c             ,"WM_KANJI_reserved_29c"             , = 0x029c)\
	x(wm_KANJI_reserved_29d             ,"WM_KANJI_reserved_29d"             , = 0x029d)\
	x(wm_KANJI_reserved_29e             ,"WM_KANJI_reserved_29e"             , = 0x029e)\
	x(wm_KANJILAST                      ,"WM_KANJILAST"                      , = 0x029f)\
	x(wm_NCMOUSEHOVER                   ,"WM_NCMOUSEHOVER"                   , = 0x02a0)\
	x(wm_MOUSEHOVER                     ,"WM_MOUSEHOVER"                     , = 0x02a1)\
	x(wm_NCMOUSELEAVE                   ,"WM_NCMOUSELEAVE"                   , = 0x02a2)\
	x(wm_MOUSELEAVE                     ,"WM_MOUSELEAVE"                     , = 0x02a3)\
	x(wm_TRACKMOUSEEVENT__reserved_2a4  ,"WM_TRACKMOUSEEVENT__reserved_2a4"  , = 0x02a4)\
	x(wm_TRACKMOUSEEVENT__reserved_2a5  ,"WM_TRACKMOUSEEVENT__reserved_2a5"  , = 0x02a5)\
	x(wm_TRACKMOUSEEVENT__reserved_2a6  ,"WM_TRACKMOUSEEVENT__reserved_2a6"  , = 0x02a6)\
	x(wm_TRACKMOUSEEVENT__reserved_2a7  ,"WM_TRACKMOUSEEVENT__reserved_2a7"  , = 0x02a7)\
	x(wm_TRACKMOUSEEVENT__reserved_2a8  ,"WM_TRACKMOUSEEVENT__reserved_2a8"  , = 0x02a8)\
	x(wm_TRACKMOUSEEVENT__reserved_2a9  ,"WM_TRACKMOUSEEVENT__reserved_2a9"  , = 0x02a9)\
	x(wm_TRACKMOUSEEVENT__reserved_2aa  ,"WM_TRACKMOUSEEVENT__reserved_2aa"  , = 0x02aa)\
	x(wm_TRACKMOUSEEVENT__reserved_2ab  ,"WM_TRACKMOUSEEVENT__reserved_2ab"  , = 0x02ab)\
	x(wm_TRACKMOUSEEVENT__reserved_2ac  ,"WM_TRACKMOUSEEVENT__reserved_2ac"  , = 0x02ac)\
	x(wm_TRACKMOUSEEVENT__reserved_2ad  ,"WM_TRACKMOUSEEVENT__reserved_2ad"  , = 0x02ad)\
	x(wm_TRACKMOUSEEVENT__reserved_2ae  ,"WM_TRACKMOUSEEVENT__reserved_2ae"  , = 0x02ae)\
	x(wm_TRACKMOUSEEVENT_LAST           ,"WM_TRACKMOUSEEVENT_LAST"           , = 0x02af)\
	x(wm_256                            ,"undefined_256"                     , = 0x02b0)\
	x(wm_WTSSESSION_CHANGE              ,"WM_WTSSESSION_CHANGE"              , = 0x02b1)\
	x(wm_257                            ,"undefined_257"                     , = 0x02b2)\
	x(wm_258                            ,"undefined_258"                     , = 0x02b3)\
	x(wm_259                            ,"undefined_259"                     , = 0x02b4)\
	x(wm_260                            ,"undefined_260"                     , = 0x02b5)\
	x(wm_261                            ,"undefined_261"                     , = 0x02b6)\
	x(wm_262                            ,"undefined_262"                     , = 0x02b7)\
	x(wm_263                            ,"undefined_263"                     , = 0x02b8)\
	x(wm_264                            ,"undefined_264"                     , = 0x02b9)\
	x(wm_265                            ,"undefined_265"                     , = 0x02ba)\
	x(wm_266                            ,"undefined_266"                     , = 0x02bb)\
	x(wm_267                            ,"undefined_267"                     , = 0x02bc)\
	x(wm_268                            ,"undefined_268"                     , = 0x02bd)\
	x(wm_269                            ,"undefined_269"                     , = 0x02be)\
	x(wm_270                            ,"undefined_270"                     , = 0x02bf)\
	x(wm_TABLET_FIRST                   ,"WM_TABLET_FIRST"                   , = 0x02c0)\
	x(wm_TABLET__reserved_2c1           ,"WM_TABLET__reserved_2c1"           , = 0x02c1)\
	x(wm_TABLET__reserved_2c2           ,"WM_TABLET__reserved_2c2"           , = 0x02c2)\
	x(wm_TABLET__reserved_2c3           ,"WM_TABLET__reserved_2c3"           , = 0x02c3)\
	x(wm_TABLET__reserved_2c4           ,"WM_TABLET__reserved_2c4"           , = 0x02c4)\
	x(wm_TABLET__reserved_2c5           ,"WM_TABLET__reserved_2c5"           , = 0x02c5)\
	x(wm_TABLET__reserved_2c6           ,"WM_TABLET__reserved_2c6"           , = 0x02c6)\
	x(wm_TABLET__reserved_2c7           ,"WM_TABLET__reserved_2c7"           , = 0x02c7)\
	x(wm_POINTERDEVICEADDED             ,"WM_POINTERDEVICEADDED"             , = 0x02c8)\
	x(wm_POINTERDEVICEDELETED           ,"WM_POINTERDEVICEDELETED"           , = 0x02c9)\
	x(wm_TABLET__reserved_2ca           ,"WM_TABLET__reserved_2ca"           , = 0x02ca)\
	x(wm_FLICK                          ,"WM_FLICK"                          , = 0x02cb)\
	x(wm_TABLET__reserved_2cc           ,"WM_TABLET__reserved_2cc"           , = 0x02cc)\
	x(wm_FLICKINTERNAL                  ,"WM_FLICKINTERNAL"                  , = 0x02cd)\
	x(wm_BRIGHTNESSCHANGED              ,"WM_BRIGHTNESSCHANGED"              , = 0x02ce)\
	x(wm_TABLET__reserved_2cf           ,"WM_TABLET__reserved_2cf"           , = 0x02cf)\
	x(wm_TABLET__reserved_2d0           ,"WM_TABLET__reserved_2d0"           , = 0x02d0)\
	x(wm_TABLET__reserved_2d1           ,"WM_TABLET__reserved_2d1"           , = 0x02d1)\
	x(wm_TABLET__reserved_2d2           ,"WM_TABLET__reserved_2d2"           , = 0x02d2)\
	x(wm_TABLET__reserved_2d3           ,"WM_TABLET__reserved_2d3"           , = 0x02d3)\
	x(wm_TABLET__reserved_2d4           ,"WM_TABLET__reserved_2d4"           , = 0x02d4)\
	x(wm_TABLET__reserved_2d5           ,"WM_TABLET__reserved_2d5"           , = 0x02d5)\
	x(wm_TABLET__reserved_2d6           ,"WM_TABLET__reserved_2d6"           , = 0x02d6)\
	x(wm_TABLET__reserved_2d7           ,"WM_TABLET__reserved_2d7"           , = 0x02d7)\
	x(wm_TABLET__reserved_2d8           ,"WM_TABLET__reserved_2d8"           , = 0x02d8)\
	x(wm_TABLET__reserved_2d9           ,"WM_TABLET__reserved_2d9"           , = 0x02d9)\
	x(wm_TABLET__reserved_2da           ,"WM_TABLET__reserved_2da"           , = 0x02da)\
	x(wm_TABLET__reserved_2db           ,"WM_TABLET__reserved_2db"           , = 0x02db)\
	x(wm_TABLET__reserved_2dc           ,"WM_TABLET__reserved_2dc"           , = 0x02dc)\
	x(wm_TABLET__reserved_2dd           ,"WM_TABLET__reserved_2dd"           , = 0x02dd)\
	x(wm_TABLET__reserved_2de           ,"WM_TABLET__reserved_2de"           , = 0x02de)\
	x(wm_TABLET_LAST                    ,"WM_TABLET_LAST"                    , = 0x02df)\
	x(wm_DPICHANGED                     ,"WM_DPICHANGED"                     , = 0x02e0)\
	x(wm_271                            ,"undefined_271"                     , = 0x02e1)\
	x(wm_272                            ,"undefined_272"                     , = 0x02e2)\
	x(wm_273                            ,"undefined_273"                     , = 0x02e3)\
	x(wm_274                            ,"undefined_274"                     , = 0x02e4)\
	x(wm_275                            ,"undefined_275"                     , = 0x02e5)\
	x(wm_276                            ,"undefined_276"                     , = 0x02e6)\
	x(wm_277                            ,"undefined_277"                     , = 0x02e7)\
	x(wm_278                            ,"undefined_278"                     , = 0x02e8)\
	x(wm_279                            ,"undefined_279"                     , = 0x02e9)\
	x(wm_280                            ,"undefined_280"                     , = 0x02ea)\
	x(wm_281                            ,"undefined_281"                     , = 0x02eb)\
	x(wm_282                            ,"undefined_282"                     , = 0x02ec)\
	x(wm_283                            ,"undefined_283"                     , = 0x02ed)\
	x(wm_284                            ,"undefined_284"                     , = 0x02ee)\
	x(wm_285                            ,"undefined_285"                     , = 0x02ef)\
	x(wm_286                            ,"undefined_286"                     , = 0x02f0)\
	x(wm_287                            ,"undefined_287"                     , = 0x02f1)\
	x(wm_288                            ,"undefined_288"                     , = 0x02f2)\
	x(wm_289                            ,"undefined_289"                     , = 0x02f3)\
	x(wm_290                            ,"undefined_290"                     , = 0x02f4)\
	x(wm_291                            ,"undefined_291"                     , = 0x02f5)\
	x(wm_292                            ,"undefined_292"                     , = 0x02f6)\
	x(wm_293                            ,"undefined_293"                     , = 0x02f7)\
	x(wm_294                            ,"undefined_294"                     , = 0x02f8)\
	x(wm_295                            ,"undefined_295"                     , = 0x02f9)\
	x(wm_296                            ,"undefined_296"                     , = 0x02fa)\
	x(wm_297                            ,"undefined_297"                     , = 0x02fb)\
	x(wm_298                            ,"undefined_298"                     , = 0x02fc)\
	x(wm_299                            ,"undefined_299"                     , = 0x02fd)\
	x(wm_300                            ,"undefined_300"                     , = 0x02fe)\
	x(wm_301                            ,"undefined_301"                     , = 0x02ff)\
	x(wm_CUT                            ,"WM_CUT"                            , = 0x0300)\
	x(wm_COPY                           ,"WM_COPY"                           , = 0x0301)\
	x(wm_PASTE                          ,"WM_PASTE"                          , = 0x0302)\
	x(wm_CLEAR                          ,"WM_CLEAR"                          , = 0x0303)\
	x(wm_UNDO                           ,"WM_UNDO"                           , = 0x0304)\
	x(wm_RENDERFORMAT                   ,"WM_RENDERFORMAT"                   , = 0x0305)\
	x(wm_RENDERALLFORMATS               ,"WM_RENDERALLFORMATS"               , = 0x0306)\
	x(wm_DESTROYCLIPBOARD               ,"WM_DESTROYCLIPBOARD"               , = 0x0307)\
	x(wm_DRAWCLIPBOARD                  ,"WM_DRAWCLIPBOARD"                  , = 0x0308)\
	x(wm_PAINTCLIPBOARD                 ,"WM_PAINTCLIPBOARD"                 , = 0x0309)\
	x(wm_VSCROLLCLIPBOARD               ,"WM_VSCROLLCLIPBOARD"               , = 0x030a)\
	x(wm_SIZECLIPBOARD                  ,"WM_SIZECLIPBOARD"                  , = 0x030b)\
	x(wm_ASKCBFORMATNAME                ,"WM_ASKCBFORMATNAME"                , = 0x030c)\
	x(wm_CHANGECBCHAIN                  ,"WM_CHANGECBCHAIN"                  , = 0x030d)\
	x(wm_HSCROLLCLIPBOARD               ,"WM_HSCROLLCLIPBOARD"               , = 0x030e)\
	x(wm_QUERYNEWPALETTE                ,"WM_QUERYNEWPALETTE"                , = 0x030f)\
	x(wm_PALETTEISCHANGING              ,"WM_PALETTEISCHANGING"              , = 0x0310)\
	x(wm_PALETTECHANGED                 ,"WM_PALETTECHANGED"                 , = 0x0311)\
	x(wm_HOTKEY                         ,"WM_HOTKEY"                         , = 0x0312)\
	x(wm_SYSMENU                        ,"WM_SYSMENU"                        , = 0x0313)\
	x(wm_HOOKMSG                        ,"WM_HOOKMSG"                        , = 0x0314)\
	x(wm_EXITPROCESS                    ,"WM_EXITPROCESS"                    , = 0x0315)\
	x(wm_WAKETHREAD                     ,"WM_WAKETHREAD"                     , = 0x0316)\
	x(wm_PRINT                          ,"WM_PRINT"                          , = 0x0317)\
	x(wm_PRINTCLIENT                    ,"WM_PRINTCLIENT"                    , = 0x0318)\
	x(wm_APPCOMMAND                     ,"WM_APPCOMMAND"                     , = 0x0319)\
	x(wm_THEMECHANGED                   ,"WM_THEMECHANGED"                   , = 0x031a)\
	x(wm_UAHINIT                        ,"WM_UAHINIT"                        , = 0x031b)\
	x(wm_DESKTOPNOTIFY                  ,"WM_DESKTOPNOTIFY"                  , = 0x031c)\
	x(wm_CLIPBOARDUPDATE                ,"WM_CLIPBOARDUPDATE"                , = 0x031d)\
	x(wm_DWMCOMPOSITIONCHANGED          ,"WM_DWMCOMPOSITIONCHANGED"          , = 0x031e)\
	x(wm_DWMNCRENDERINGCHANGED          ,"WM_DWMNCRENDERINGCHANGED"          , = 0x031f)\
	x(wm_DWMCOLORIZATIONCOLORCHANGED    ,"WM_DWMCOLORIZATIONCOLORCHANGED"    , = 0x0320)\
	x(wm_DWMWINDOWMAXIMIZEDCHANGE       ,"WM_DWMWINDOWMAXIMIZEDCHANGE"       , = 0x0321)\
	x(wm_DWMEXILEFRAME                  ,"WM_DWMEXILEFRAME"                  , = 0x0322)\
	x(wm_DWMSENDICONICTHUMBNAIL         ,"WM_DWMSENDICONICTHUMBNAIL"         , = 0x0323)\
	x(wm_MAGNIFICATION_STARTED          ,"WM_MAGNIFICATION_STARTED"          , = 0x0324)\
	x(wm_MAGNIFICATION_ENDED            ,"WM_MAGNIFICATION_ENDED"            , = 0x0325)\
	x(wm_DWMSENDICONICLIVEPREVIEWBITMAP ,"WM_DWMSENDICONICLIVEPREVIEWBITMAP" , = 0x0326)\
	x(wm_DWMTHUMBNAILSIZECHANGED        ,"WM_DWMTHUMBNAILSIZECHANGED"        , = 0x0327)\
	x(wm_MAGNIFICATION_OUTPUT           ,"WM_MAGNIFICATION_OUTPUT"           , = 0x0328)\
	x(wm_BSDRDATA                       ,"WM_BSDRDATA"                       , = 0x0329)\
	x(wm_DWMTRANSITIONSTATECHANGED      ,"WM_DWMTRANSITIONSTATECHANGED"      , = 0x032a)\
	x(wm_302                            ,"undefined_302"                     , = 0x032b)\
	x(wm_KEYBOARDCORRECTIONCALLOUT      ,"WM_KEYBOARDCORRECTIONCALLOUT"      , = 0x032c)\
	x(wm_KEYBOARDCORRECTIONACTION       ,"WM_KEYBOARDCORRECTIONACTION"       , = 0x032d)\
	x(wm_UIACTION                       ,"WM_UIACTION"                       , = 0x032e)\
	x(wm_ROUTED_UI_EVENT                ,"WM_ROUTED_UI_EVENT"                , = 0x032f)\
	x(wm_MEASURECONTROL                 ,"WM_MEASURECONTROL"                 , = 0x0330)\
	x(wm_GETACTIONTEXT                  ,"WM_GETACTIONTEXT"                  , = 0x0331)\
	x(wm_CE_ONLY__reserved_332          ,"WM_CE_ONLY__reserved_332"          , = 0x0332)\
	x(wm_FORWARDKEYDOWN                 ,"WM_FORWARDKEYDOWN"                 , = 0x0333)\
	x(wm_FORWARDKEYUP                   ,"WM_FORWARDKEYUP"                   , = 0x0334)\
	x(wm_CE_ONLY__reserved_335          ,"WM_CE_ONLY__reserved_335"          , = 0x0335)\
	x(wm_CE_ONLY__reserved_336          ,"WM_CE_ONLY__reserved_336"          , = 0x0336)\
	x(wm_CE_ONLY__reserved_337          ,"WM_CE_ONLY__reserved_337"          , = 0x0337)\
	x(wm_CE_ONLY__reserved_338          ,"WM_CE_ONLY__reserved_338"          , = 0x0338)\
	x(wm_CE_ONLY__reserved_339          ,"WM_CE_ONLY__reserved_339"          , = 0x0339)\
	x(wm_CE_ONLY__reserved_33a          ,"WM_CE_ONLY__reserved_33a"          , = 0x033a)\
	x(wm_CE_ONLY__reserved_33b          ,"WM_CE_ONLY__reserved_33b"          , = 0x033b)\
	x(wm_CE_ONLY__reserved_33c          ,"WM_CE_ONLY__reserved_33c"          , = 0x033c)\
	x(wm_CE_ONLY__reserved_33d          ,"WM_CE_ONLY__reserved_33d"          , = 0x033d)\
	x(wm_CE_ONLY_LAST                   ,"WM_CE_ONLY_LAST"                   , = 0x033e)\
	x(wm_GETTITLEBARINFOEX              ,"WM_GETTITLEBARINFOEX"              , = 0x033f)\
	x(wm_NOTIFYWOW                      ,"WM_NOTIFYWOW"                      , = 0x0340)\
	x(wm_303                            ,"undefined_303"                     , = 0x0341)\
	x(wm_304                            ,"undefined_304"                     , = 0x0342)\
	x(wm_305                            ,"undefined_305"                     , = 0x0343)\
	x(wm_306                            ,"undefined_306"                     , = 0x0344)\
	x(wm_307                            ,"undefined_307"                     , = 0x0345)\
	x(wm_308                            ,"undefined_308"                     , = 0x0346)\
	x(wm_309                            ,"undefined_309"                     , = 0x0347)\
	x(wm_310                            ,"undefined_310"                     , = 0x0348)\
	x(wm_311                            ,"undefined_311"                     , = 0x0349)\
	x(wm_312                            ,"undefined_312"                     , = 0x034a)\
	x(wm_313                            ,"undefined_313"                     , = 0x034b)\
	x(wm_314                            ,"undefined_314"                     , = 0x034c)\
	x(wm_315                            ,"undefined_315"                     , = 0x034d)\
	x(wm_316                            ,"undefined_316"                     , = 0x034e)\
	x(wm_317                            ,"undefined_317"                     , = 0x034f)\
	x(wm_318                            ,"undefined_318"                     , = 0x0350)\
	x(wm_319                            ,"undefined_319"                     , = 0x0351)\
	x(wm_320                            ,"undefined_320"                     , = 0x0352)\
	x(wm_321                            ,"undefined_321"                     , = 0x0353)\
	x(wm_322                            ,"undefined_322"                     , = 0x0354)\
	x(wm_323                            ,"undefined_323"                     , = 0x0355)\
	x(wm_324                            ,"undefined_324"                     , = 0x0356)\
	x(wm_325                            ,"undefined_325"                     , = 0x0357)\
	x(wm_HANDHELDFIRST                  ,"WM_HANDHELDFIRST"                  , = 0x0358)\
	x(wm_HANDHELD_reserved_359          ,"WM_HANDHELD_reserved_359"          , = 0x0359)\
	x(wm_HANDHELD_reserved_35a          ,"WM_HANDHELD_reserved_35a"          , = 0x035a)\
	x(wm_HANDHELD_reserved_35b          ,"WM_HANDHELD_reserved_35b"          , = 0x035b)\
	x(wm_HANDHELD_reserved_35c          ,"WM_HANDHELD_reserved_35c"          , = 0x035c)\
	x(wm_HANDHELD_reserved_35d          ,"WM_HANDHELD_reserved_35d"          , = 0x035d)\
	x(wm_HANDHELD_reserved_35e          ,"WM_HANDHELD_reserved_35e"          , = 0x035e)\
	x(wm_HANDHELDLAST                   ,"WM_HANDHELDLAST"                   , = 0x035f)\
	x(wm_AFXFIRST                       ,"WM_AFXFIRST"                       , = 0x0360)\
	x(wm_AFX_reserved_361               ,"WM_AFX_reserved_361"               , = 0x0361)\
	x(wm_AFX_reserved_362               ,"WM_AFX_reserved_362"               , = 0x0362)\
	x(wm_AFX_reserved_363               ,"WM_AFX_reserved_363"               , = 0x0363)\
	x(wm_AFX_reserved_364               ,"WM_AFX_reserved_364"               , = 0x0364)\
	x(wm_AFX_reserved_365               ,"WM_AFX_reserved_365"               , = 0x0365)\
	x(wm_AFX_reserved_366               ,"WM_AFX_reserved_366"               , = 0x0366)\
	x(wm_AFX_reserved_367               ,"WM_AFX_reserved_367"               , = 0x0367)\
	x(wm_AFX_reserved_368               ,"WM_AFX_reserved_368"               , = 0x0368)\
	x(wm_AFX_reserved_369               ,"WM_AFX_reserved_369"               , = 0x0369)\
	x(wm_AFX_reserved_36a               ,"WM_AFX_reserved_36a"               , = 0x036a)\
	x(wm_AFX_reserved_36b               ,"WM_AFX_reserved_36b"               , = 0x036b)\
	x(wm_AFX_reserved_36c               ,"WM_AFX_reserved_36c"               , = 0x036c)\
	x(wm_AFX_reserved_36d               ,"WM_AFX_reserved_36d"               , = 0x036d)\
	x(wm_AFX_reserved_36e               ,"WM_AFX_reserved_36e"               , = 0x036e)\
	x(wm_AFX_reserved_36f               ,"WM_AFX_reserved_36f"               , = 0x036f)\
	x(wm_AFX_reserved_370               ,"WM_AFX_reserved_370"               , = 0x0370)\
	x(wm_AFX_reserved_371               ,"WM_AFX_reserved_371"               , = 0x0371)\
	x(wm_AFX_reserved_372               ,"WM_AFX_reserved_372"               , = 0x0372)\
	x(wm_AFX_reserved_373               ,"WM_AFX_reserved_373"               , = 0x0373)\
	x(wm_AFX_reserved_374               ,"WM_AFX_reserved_374"               , = 0x0374)\
	x(wm_AFX_reserved_375               ,"WM_AFX_reserved_375"               , = 0x0375)\
	x(wm_AFX_reserved_376               ,"WM_AFX_reserved_376"               , = 0x0376)\
	x(wm_AFX_reserved_377               ,"WM_AFX_reserved_377"               , = 0x0377)\
	x(wm_AFX_reserved_378               ,"WM_AFX_reserved_378"               , = 0x0378)\
	x(wm_AFX_reserved_379               ,"WM_AFX_reserved_379"               , = 0x0379)\
	x(wm_AFX_reserved_37a               ,"WM_AFX_reserved_37a"               , = 0x037a)\
	x(wm_AFX_reserved_37b               ,"WM_AFX_reserved_37b"               , = 0x037b)\
	x(wm_AFX_reserved_37c               ,"WM_AFX_reserved_37c"               , = 0x037c)\
	x(wm_AFX_reserved_37d               ,"WM_AFX_reserved_37d"               , = 0x037d)\
	x(wm_AFX_reserved_37e               ,"WM_AFX_reserved_37e"               , = 0x037e)\
	x(wm_AFXLAST                        ,"WM_AFXLAST"                        , = 0x037f)\
	x(wm_PENWINFIRST                    ,"WM_PENWINFIRST"                    , = 0x0380)\
	x(wm_PENWIN_reserved_381            ,"WM_PENWIN_reserved_381"            , = 0x0381)\
	x(wm_PENWIN_reserved_382            ,"WM_PENWIN_reserved_382"            , = 0x0382)\
	x(wm_PENWIN_reserved_383            ,"WM_PENWIN_reserved_383"            , = 0x0383)\
	x(wm_PENWIN_reserved_384            ,"WM_PENWIN_reserved_384"            , = 0x0384)\
	x(wm_PENWIN_reserved_385            ,"WM_PENWIN_reserved_385"            , = 0x0385)\
	x(wm_PENWIN_reserved_386            ,"WM_PENWIN_reserved_386"            , = 0x0386)\
	x(wm_PENWIN_reserved_387            ,"WM_PENWIN_reserved_387"            , = 0x0387)\
	x(wm_PENWIN_reserved_388            ,"WM_PENWIN_reserved_388"            , = 0x0388)\
	x(wm_PENWIN_reserved_389            ,"WM_PENWIN_reserved_389"            , = 0x0389)\
	x(wm_PENWIN_reserved_38a            ,"WM_PENWIN_reserved_38a"            , = 0x038a)\
	x(wm_PENWIN_reserved_38b            ,"WM_PENWIN_reserved_38b"            , = 0x038b)\
	x(wm_PENWIN_reserved_38c            ,"WM_PENWIN_reserved_38c"            , = 0x038c)\
	x(wm_PENWIN_reserved_38d            ,"WM_PENWIN_reserved_38d"            , = 0x038d)\
	x(wm_PENWIN_reserved_38e            ,"WM_PENWIN_reserved_38e"            , = 0x038e)\
	x(wm_PENWINLAST                     ,"WM_PENWINLAST"                     , = 0x038f)\
	x(wm_COALESCE_FIRST                 ,"WM_COALESCE_FIRST"                 , = 0x0390)\
	x(wm_COALESCE__reserved_391         ,"WM_COALESCE__reserved_391"         , = 0x0391)\
	x(wm_COALESCE__reserved_392         ,"WM_COALESCE__reserved_392"         , = 0x0392)\
	x(wm_COALESCE__reserved_393         ,"WM_COALESCE__reserved_393"         , = 0x0393)\
	x(wm_COALESCE__reserved_394         ,"WM_COALESCE__reserved_394"         , = 0x0394)\
	x(wm_COALESCE__reserved_395         ,"WM_COALESCE__reserved_395"         , = 0x0395)\
	x(wm_COALESCE__reserved_396         ,"WM_COALESCE__reserved_396"         , = 0x0396)\
	x(wm_COALESCE__reserved_397         ,"WM_COALESCE__reserved_397"         , = 0x0397)\
	x(wm_COALESCE__reserved_398         ,"WM_COALESCE__reserved_398"         , = 0x0398)\
	x(wm_COALESCE__reserved_399         ,"WM_COALESCE__reserved_399"         , = 0x0399)\
	x(wm_COALESCE__reserved_39a         ,"WM_COALESCE__reserved_39a"         , = 0x039a)\
	x(wm_COALESCE__reserved_39b         ,"WM_COALESCE__reserved_39b"         , = 0x039b)\
	x(wm_COALESCE__reserved_39c         ,"WM_COALESCE__reserved_39c"         , = 0x039c)\
	x(wm_COALESCE__reserved_39d         ,"WM_COALESCE__reserved_39d"         , = 0x039d)\
	x(wm_COALESCE__reserved_39e         ,"WM_COALESCE__reserved_39e"         , = 0x039e)\
	x(wm_COALESCE_LAST                  ,"WM_COALESCE_LAST"                  , = 0x039f)\
	x(wm_MM_RESERVED_FIRST              ,"WM_MM_RESERVED_FIRST"              , = 0x03a0)\
	x(wm_MM_RESERVED__reserved_3a1      ,"WM_MM_RESERVED__reserved_3a1"      , = 0x03a1)\
	x(wm_MM_RESERVED__reserved_3a2      ,"WM_MM_RESERVED__reserved_3a2"      , = 0x03a2)\
	x(wm_MM_RESERVED__reserved_3a3      ,"WM_MM_RESERVED__reserved_3a3"      , = 0x03a3)\
	x(wm_MM_RESERVED__reserved_3a4      ,"WM_MM_RESERVED__reserved_3a4"      , = 0x03a4)\
	x(wm_MM_RESERVED__reserved_3a5      ,"WM_MM_RESERVED__reserved_3a5"      , = 0x03a5)\
	x(wm_MM_RESERVED__reserved_3a6      ,"WM_MM_RESERVED__reserved_3a6"      , = 0x03a6)\
	x(wm_MM_RESERVED__reserved_3a7      ,"WM_MM_RESERVED__reserved_3a7"      , = 0x03a7)\
	x(wm_MM_RESERVED__reserved_3a8      ,"WM_MM_RESERVED__reserved_3a8"      , = 0x03a8)\
	x(wm_MM_RESERVED__reserved_3a9      ,"WM_MM_RESERVED__reserved_3a9"      , = 0x03a9)\
	x(wm_MM_RESERVED__reserved_3aa      ,"WM_MM_RESERVED__reserved_3aa"      , = 0x03aa)\
	x(wm_MM_RESERVED__reserved_3ab      ,"WM_MM_RESERVED__reserved_3ab"      , = 0x03ab)\
	x(wm_MM_RESERVED__reserved_3ac      ,"WM_MM_RESERVED__reserved_3ac"      , = 0x03ac)\
	x(wm_MM_RESERVED__reserved_3ad      ,"WM_MM_RESERVED__reserved_3ad"      , = 0x03ad)\
	x(wm_MM_RESERVED__reserved_3ae      ,"WM_MM_RESERVED__reserved_3ae"      , = 0x03ae)\
	x(wm_MM_RESERVED__reserved_3af      ,"WM_MM_RESERVED__reserved_3af"      , = 0x03af)\
	x(wm_MM_RESERVED__reserved_3b0      ,"WM_MM_RESERVED__reserved_3b0"      , = 0x03b0)\
	x(wm_MM_RESERVED__reserved_3b1      ,"WM_MM_RESERVED__reserved_3b1"      , = 0x03b1)\
	x(wm_MM_RESERVED__reserved_3b2      ,"WM_MM_RESERVED__reserved_3b2"      , = 0x03b2)\
	x(wm_MM_RESERVED__reserved_3b3      ,"WM_MM_RESERVED__reserved_3b3"      , = 0x03b3)\
	x(wm_MM_RESERVED__reserved_3b4      ,"WM_MM_RESERVED__reserved_3b4"      , = 0x03b4)\
	x(wm_MM_RESERVED__reserved_3b5      ,"WM_MM_RESERVED__reserved_3b5"      , = 0x03b5)\
	x(wm_MM_RESERVED__reserved_3b6      ,"WM_MM_RESERVED__reserved_3b6"      , = 0x03b6)\
	x(wm_MM_RESERVED__reserved_3b7      ,"WM_MM_RESERVED__reserved_3b7"      , = 0x03b7)\
	x(wm_MM_RESERVED__reserved_3b8      ,"WM_MM_RESERVED__reserved_3b8"      , = 0x03b8)\
	x(wm_MM_RESERVED__reserved_3b9      ,"WM_MM_RESERVED__reserved_3b9"      , = 0x03b9)\
	x(wm_MM_RESERVED__reserved_3ba      ,"WM_MM_RESERVED__reserved_3ba"      , = 0x03ba)\
	x(wm_MM_RESERVED__reserved_3bb      ,"WM_MM_RESERVED__reserved_3bb"      , = 0x03bb)\
	x(wm_MM_RESERVED__reserved_3bc      ,"WM_MM_RESERVED__reserved_3bc"      , = 0x03bc)\
	x(wm_MM_RESERVED__reserved_3bd      ,"WM_MM_RESERVED__reserved_3bd"      , = 0x03bd)\
	x(wm_MM_RESERVED__reserved_3be      ,"WM_MM_RESERVED__reserved_3be"      , = 0x03be)\
	x(wm_MM_RESERVED__reserved_3bf      ,"WM_MM_RESERVED__reserved_3bf"      , = 0x03bf)\
	x(wm_MM_RESERVED__reserved_3c0      ,"WM_MM_RESERVED__reserved_3c0"      , = 0x03c0)\
	x(wm_MM_RESERVED__reserved_3c1      ,"WM_MM_RESERVED__reserved_3c1"      , = 0x03c1)\
	x(wm_MM_RESERVED__reserved_3c2      ,"WM_MM_RESERVED__reserved_3c2"      , = 0x03c2)\
	x(wm_MM_RESERVED__reserved_3c3      ,"WM_MM_RESERVED__reserved_3c3"      , = 0x03c3)\
	x(wm_MM_RESERVED__reserved_3c4      ,"WM_MM_RESERVED__reserved_3c4"      , = 0x03c4)\
	x(wm_MM_RESERVED__reserved_3c5      ,"WM_MM_RESERVED__reserved_3c5"      , = 0x03c5)\
	x(wm_MM_RESERVED__reserved_3c6      ,"WM_MM_RESERVED__reserved_3c6"      , = 0x03c6)\
	x(wm_MM_RESERVED__reserved_3c7      ,"WM_MM_RESERVED__reserved_3c7"      , = 0x03c7)\
	x(wm_MM_RESERVED__reserved_3c8      ,"WM_MM_RESERVED__reserved_3c8"      , = 0x03c8)\
	x(wm_MM_RESERVED__reserved_3c9      ,"WM_MM_RESERVED__reserved_3c9"      , = 0x03c9)\
	x(wm_MM_RESERVED__reserved_3ca      ,"WM_MM_RESERVED__reserved_3ca"      , = 0x03ca)\
	x(wm_MM_RESERVED__reserved_3cb      ,"WM_MM_RESERVED__reserved_3cb"      , = 0x03cb)\
	x(wm_MM_RESERVED__reserved_3cc      ,"WM_MM_RESERVED__reserved_3cc"      , = 0x03cc)\
	x(wm_MM_RESERVED__reserved_3cd      ,"WM_MM_RESERVED__reserved_3cd"      , = 0x03cd)\
	x(wm_MM_RESERVED__reserved_3ce      ,"WM_MM_RESERVED__reserved_3ce"      , = 0x03ce)\
	x(wm_MM_RESERVED__reserved_3cf      ,"WM_MM_RESERVED__reserved_3cf"      , = 0x03cf)\
	x(wm_MM_RESERVED__reserved_3d0      ,"WM_MM_RESERVED__reserved_3d0"      , = 0x03d0)\
	x(wm_MM_RESERVED__reserved_3d1      ,"WM_MM_RESERVED__reserved_3d1"      , = 0x03d1)\
	x(wm_MM_RESERVED__reserved_3d2      ,"WM_MM_RESERVED__reserved_3d2"      , = 0x03d2)\
	x(wm_MM_RESERVED__reserved_3d3      ,"WM_MM_RESERVED__reserved_3d3"      , = 0x03d3)\
	x(wm_MM_RESERVED__reserved_3d4      ,"WM_MM_RESERVED__reserved_3d4"      , = 0x03d4)\
	x(wm_MM_RESERVED__reserved_3d5      ,"WM_MM_RESERVED__reserved_3d5"      , = 0x03d5)\
	x(wm_MM_RESERVED__reserved_3d6      ,"WM_MM_RESERVED__reserved_3d6"      , = 0x03d6)\
	x(wm_MM_RESERVED__reserved_3d7      ,"WM_MM_RESERVED__reserved_3d7"      , = 0x03d7)\
	x(wm_MM_RESERVED__reserved_3d8      ,"WM_MM_RESERVED__reserved_3d8"      , = 0x03d8)\
	x(wm_MM_RESERVED__reserved_3d9      ,"WM_MM_RESERVED__reserved_3d9"      , = 0x03d9)\
	x(wm_MM_RESERVED__reserved_3da      ,"WM_MM_RESERVED__reserved_3da"      , = 0x03da)\
	x(wm_MM_RESERVED__reserved_3db      ,"WM_MM_RESERVED__reserved_3db"      , = 0x03db)\
	x(wm_MM_RESERVED__reserved_3dc      ,"WM_MM_RESERVED__reserved_3dc"      , = 0x03dc)\
	x(wm_MM_RESERVED__reserved_3dd      ,"WM_MM_RESERVED__reserved_3dd"      , = 0x03dd)\
	x(wm_MM_RESERVED__reserved_3de      ,"WM_MM_RESERVED__reserved_3de"      , = 0x03de)\
	x(wm_MM_RESERVED_LAST               ,"WM_MM_RESERVED_LAST"               , = 0x03df)\
	x(wm_INTERNAL_DDE_FIRST             ,"WM_INTERNAL_DDE_FIRST"             , = 0x03e0)\
	x(wm_INTERNAL_DDE__reserved_3e1     ,"WM_INTERNAL_DDE__reserved_3e1"     , = 0x03e1)\
	x(wm_INTERNAL_DDE__reserved_3e2     ,"WM_INTERNAL_DDE__reserved_3e2"     , = 0x03e2)\
	x(wm_INTERNAL_DDE__reserved_3e3     ,"WM_INTERNAL_DDE__reserved_3e3"     , = 0x03e3)\
	x(wm_INTERNAL_DDE__reserved_3e4     ,"WM_INTERNAL_DDE__reserved_3e4"     , = 0x03e4)\
	x(wm_INTERNAL_DDE__reserved_3e5     ,"WM_INTERNAL_DDE__reserved_3e5"     , = 0x03e5)\
	x(wm_INTERNAL_DDE__reserved_3e6     ,"WM_INTERNAL_DDE__reserved_3e6"     , = 0x03e6)\
	x(wm_INTERNAL_DDE__reserved_3e7     ,"WM_INTERNAL_DDE__reserved_3e7"     , = 0x03e7)\
	x(wm_INTERNAL_DDE__reserved_3e8     ,"WM_INTERNAL_DDE__reserved_3e8"     , = 0x03e8)\
	x(wm_INTERNAL_DDE__reserved_3e9     ,"WM_INTERNAL_DDE__reserved_3e9"     , = 0x03e9)\
	x(wm_INTERNAL_DDE__reserved_3ea     ,"WM_INTERNAL_DDE__reserved_3ea"     , = 0x03ea)\
	x(wm_INTERNAL_DDE__reserved_3eb     ,"WM_INTERNAL_DDE__reserved_3eb"     , = 0x03eb)\
	x(wm_INTERNAL_DDE__reserved_3ec     ,"WM_INTERNAL_DDE__reserved_3ec"     , = 0x03ec)\
	x(wm_INTERNAL_DDE__reserved_3ed     ,"WM_INTERNAL_DDE__reserved_3ed"     , = 0x03ed)\
	x(wm_INTERNAL_DDE__reserved_3ee     ,"WM_INTERNAL_DDE__reserved_3ee"     , = 0x03ee)\
	x(wm_INTERNAL_DDE_LAST              ,"WM_INTERNAL_DDE_LAST"              , = 0x03ef)\
	x(wm_CBT_RESERVED_FIRST             ,"WM_CBT_RESERVED_FIRST"             , = 0x03f0)\
	x(wm_CBT_RESERVED__reserved_3f1     ,"WM_CBT_RESERVED__reserved_3f1"     , = 0x03f1)\
	x(wm_CBT_RESERVED__reserved_3f2     ,"WM_CBT_RESERVED__reserved_3f2"     , = 0x03f2)\
	x(wm_CBT_RESERVED__reserved_3f3     ,"WM_CBT_RESERVED__reserved_3f3"     , = 0x03f3)\
	x(wm_CBT_RESERVED__reserved_3f4     ,"WM_CBT_RESERVED__reserved_3f4"     , = 0x03f4)\
	x(wm_CBT_RESERVED__reserved_3f5     ,"WM_CBT_RESERVED__reserved_3f5"     , = 0x03f5)\
	x(wm_CBT_RESERVED__reserved_3f6     ,"WM_CBT_RESERVED__reserved_3f6"     , = 0x03f6)\
	x(wm_CBT_RESERVED__reserved_3f7     ,"WM_CBT_RESERVED__reserved_3f7"     , = 0x03f7)\
	x(wm_CBT_RESERVED__reserved_3f8     ,"WM_CBT_RESERVED__reserved_3f8"     , = 0x03f8)\
	x(wm_CBT_RESERVED__reserved_3f9     ,"WM_CBT_RESERVED__reserved_3f9"     , = 0x03f9)\
	x(wm_CBT_RESERVED__reserved_3fa     ,"WM_CBT_RESERVED__reserved_3fa"     , = 0x03fa)\
	x(wm_CBT_RESERVED__reserved_3fb     ,"WM_CBT_RESERVED__reserved_3fb"     , = 0x03fb)\
	x(wm_CBT_RESERVED__reserved_3fc     ,"WM_CBT_RESERVED__reserved_3fc"     , = 0x03fc)\
	x(wm_CBT_RESERVED__reserved_3fd     ,"WM_CBT_RESERVED__reserved_3fd"     , = 0x03fd)\
	x(wm_CBT_RESERVED__reserved_3fe     ,"WM_CBT_RESERVED__reserved_3fe"     , = 0x03fe)\
	x(wm_CBT_RESERVED_LAST              ,"WM_CBT_RESERVED_LAST"              , = 0x03ff)\
	x(wm_USER                           ,"WM_USER"                           , = 0x0400)
	//x(wm_CBEM_INSERTITEMA               ,"CBEM_INSERTITEMA"                  , = 0x0401)\
	//x(wm_DDM_DRAW                       ,"DDM_DRAW"                          , = 0x0401)\
	//x(wm_DM_SETDEFID                    ,"DM_SETDEFID"                       , = 0x0401)\
	//x(wm_HKM_SETHOTKEY                  ,"HKM_SETHOTKEY"                     , = 0x0401)\
	//x(wm_PBM_SETRANGE                   ,"PBM_SETRANGE"                      , = 0x0401)\
	//x(wm_RB_INSERTBANDA                 ,"RB_INSERTBANDA"                    , = 0x0401)\
	//x(wm_SB_SETTEXTA                    ,"SB_SETTEXTA"                       , = 0x0401)\
	//x(wm_TB_ENABLEBUTTON                ,"TB_ENABLEBUTTON"                   , = 0x0401)\
	//x(wm_TBM_GETRANGEMIN                ,"TBM_GETRANGEMIN"                   , = 0x0401)\
	//x(wm_TTM_ACTIVATE                   ,"TTM_ACTIVATE"                      , = 0x0401)\
	//x(wm_WM_CHOOSEFONT_GETLOGFONT       ,"WM_CHOOSEFONT_GETLOGFONT"          , = 0x0401)\
	//x(wm_WM_PSD_FULLPAGERECT            ,"WM_PSD_FULLPAGERECT"               , = 0x0401)\
	//x(wm_CBEM_SETIMAGELIST              ,"CBEM_SETIMAGELIST"                 , = 0x0402)\
	//x(wm_DDM_CLOSE                      ,"DDM_CLOSE"                         , = 0x0402)\
	//x(wm_DM_REPOSITION                  ,"DM_REPOSITION"                     , = 0x0402)\
	//x(wm_HKM_GETHOTKEY                  ,"HKM_GETHOTKEY"                     , = 0x0402)\
	//x(wm_PBM_SETPOS                     ,"PBM_SETPOS"                        , = 0x0402)\
	//x(wm_RB_DELETEBAND                  ,"RB_DELETEBAND"                     , = 0x0402)\
	//x(wm_SB_GETTEXTA                    ,"SB_GETTEXTA"                       , = 0x0402)\
	//x(wm_TB_CHECKBUTTON                 ,"TB_CHECKBUTTON"                    , = 0x0402)\
	//x(wm_TBM_GETRANGEMAX                ,"TBM_GETRANGEMAX"                   , = 0x0402)\
	//x(wm_WM_PSD_MINMARGINRECT           ,"WM_PSD_MINMARGINRECT"              , = 0x0402)\
	//x(wm_CBEM_GETIMAGELIST              ,"CBEM_GETIMAGELIST"                 , = 0x0403)\
	//x(wm_DDM_BEGIN                      ,"DDM_BEGIN"                         , = 0x0403)\
	//x(wm_HKM_SETRULES                   ,"HKM_SETRULES"                      , = 0x0403)\
	//x(wm_PBM_DELTAPOS                   ,"PBM_DELTAPOS"                      , = 0x0403)\
	//x(wm_RB_GETBARINFO                  ,"RB_GETBARINFO"                     , = 0x0403)\
	//x(wm_SB_GETTEXTLENGTHA              ,"SB_GETTEXTLENGTHA"                 , = 0x0403)\
	//x(wm_TBM_GETTIC                     ,"TBM_GETTIC"                        , = 0x0403)\
	//x(wm_TB_PRESSBUTTON                 ,"TB_PRESSBUTTON"                    , = 0x0403)\
	//x(wm_TTM_SETDELAYTIME               ,"TTM_SETDELAYTIME"                  , = 0x0403)\
	//x(wm_WM_PSD_MARGINRECT              ,"WM_PSD_MARGINRECT"                 , = 0x0403)\
	//x(wm_CBEM_GETITEMA                  ,"CBEM_GETITEMA"                     , = 0x0404)\
	//x(wm_DDM_END                        ,"DDM_END"                           , = 0x0404)\
	//x(wm_PBM_SETSTEP                    ,"PBM_SETSTEP"                       , = 0x0404)\
	//x(wm_RB_SETBARINFO                  ,"RB_SETBARINFO"                     , = 0x0404)\
	//x(wm_SB_SETPARTS                    ,"SB_SETPARTS"                       , = 0x0404)\
	//x(wm_TB_HIDEBUTTON                  ,"TB_HIDEBUTTON"                     , = 0x0404)\
	//x(wm_TBM_SETTIC                     ,"TBM_SETTIC"                        , = 0x0404)\
	//x(wm_TTM_ADDTOOLA                   ,"TTM_ADDTOOLA"                      , = 0x0404)\
	//x(wm_WM_PSD_GREEKTEXTRECT           ,"WM_PSD_GREEKTEXTRECT"              , = 0x0404)\
	//x(wm_CBEM_SETITEMA                  ,"CBEM_SETITEMA"                     , = 0x0405)\
	//x(wm_PBM_STEPIT                     ,"PBM_STEPIT"                        , = 0x0405)\
	//x(wm_TB_INDETERMINATE               ,"TB_INDETERMINATE"                  , = 0x0405)\
	//x(wm_TBM_SETPOS                     ,"TBM_SETPOS"                        , = 0x0405)\
	//x(wm_TTM_DELTOOLA                   ,"TTM_DELTOOLA"                      , = 0x0405)\
	//x(wm_WM_PSD_ENVSTAMPRECT            ,"WM_PSD_ENVSTAMPRECT"               , = 0x0405)\
	//x(wm_CBEM_GETCOMBOCONTROL           ,"CBEM_GETCOMBOCONTROL"              , = 0x0406)\
	//x(wm_PBM_SETRANGE32                 ,"PBM_SETRANGE32"                    , = 0x0406)\
	//x(wm_RB_SETBANDINFOA                ,"RB_SETBANDINFOA"                   , = 0x0406)\
	//x(wm_SB_GETPARTS                    ,"SB_GETPARTS"                       , = 0x0406)\
	//x(wm_TB_MARKBUTTON                  ,"TB_MARKBUTTON"                     , = 0x0406)\
	//x(wm_TBM_SETRANGE                   ,"TBM_SETRANGE"                      , = 0x0406)\
	//x(wm_TTM_NEWTOOLRECTA               ,"TTM_NEWTOOLRECTA"                  , = 0x0406)\
	//x(wm_WM_PSD_YAFULLPAGERECT          ,"WM_PSD_YAFULLPAGERECT"             , = 0x0406)\
	//x(wm_CBEM_GETEDITCONTROL            ,"CBEM_GETEDITCONTROL"               , = 0x0407)\
	//x(wm_PBM_GETRANGE                   ,"PBM_GETRANGE"                      , = 0x0407)\
	//x(wm_RB_SETPARENT                   ,"RB_SETPARENT"                      , = 0x0407)\
	//x(wm_SB_GETBORDERS                  ,"SB_GETBORDERS"                     , = 0x0407)\
	//x(wm_TBM_SETRANGEMIN                ,"TBM_SETRANGEMIN"                   , = 0x0407)\
	//x(wm_TTM_RELAYEVENT                 ,"TTM_RELAYEVENT"                    , = 0x0407)\
	//x(wm_CBEM_SETEXSTYLE                ,"CBEM_SETEXSTYLE"                   , = 0x0408)\
	//x(wm_PBM_GETPOS                     ,"PBM_GETPOS"                        , = 0x0408)\
	//x(wm_RB_HITTEST                     ,"RB_HITTEST"                        , = 0x0408)\
	//x(wm_SB_SETMINHEIGHT                ,"SB_SETMINHEIGHT"                   , = 0x0408)\
	//x(wm_TBM_SETRANGEMAX                ,"TBM_SETRANGEMAX"                   , = 0x0408)\
	//x(wm_TTM_GETTOOLINFOA               ,"TTM_GETTOOLINFOA"                  , = 0x0408)\
	//x(wm_CBEM_GETEXSTYLE                ,"CBEM_GETEXSTYLE"                   , = 0x0409)\
	//x(wm_CBEM_GETEXTENDEDSTYLE          ,"CBEM_GETEXTENDEDSTYLE"             , = 0x0409)\
	//x(wm_PBM_SETBARCOLOR                ,"PBM_SETBARCOLOR"                   , = 0x0409)\
	//x(wm_RB_GETRECT                     ,"RB_GETRECT"                        , = 0x0409)\
	//x(wm_SB_SIMPLE                      ,"SB_SIMPLE"                         , = 0x0409)\
	//x(wm_TB_ISBUTTONENABLED             ,"TB_ISBUTTONENABLED"                , = 0x0409)\
	//x(wm_TBM_CLEARTICS                  ,"TBM_CLEARTICS"                     , = 0x0409)\
	//x(wm_TTM_SETTOOLINFOA               ,"TTM_SETTOOLINFOA"                  , = 0x0409)\
	//x(wm_CBEM_HASEDITCHANGED            ,"CBEM_HASEDITCHANGED"               , = 0x040a)\
	//x(wm_RB_INSERTBANDW                 ,"RB_INSERTBANDW"                    , = 0x040a)\
	//x(wm_SB_GETRECT                     ,"SB_GETRECT"                        , = 0x040a)\
	//x(wm_TB_ISBUTTONCHECKED             ,"TB_ISBUTTONCHECKED"                , = 0x040a)\
	//x(wm_TBM_SETSEL                     ,"TBM_SETSEL"                        , = 0x040a)\
	//x(wm_TTM_HITTESTA                   ,"TTM_HITTESTA"                      , = 0x040a)\
	//x(wm_WIZ_QUERYNUMPAGES              ,"WIZ_QUERYNUMPAGES"                 , = 0x040a)\
	//x(wm_CBEM_INSERTITEMW               ,"CBEM_INSERTITEMW"                  , = 0x040b)\
	//x(wm_RB_SETBANDINFOW                ,"RB_SETBANDINFOW"                   , = 0x040b)\
	//x(wm_SB_SETTEXTW                    ,"SB_SETTEXTW"                       , = 0x040b)\
	//x(wm_TB_ISBUTTONPRESSED             ,"TB_ISBUTTONPRESSED"                , = 0x040b)\
	//x(wm_TBM_SETSELSTART                ,"TBM_SETSELSTART"                   , = 0x040b)\
	//x(wm_TTM_GETTEXTA                   ,"TTM_GETTEXTA"                      , = 0x040b)\
	//x(wm_WIZ_NEXT                       ,"WIZ_NEXT"                          , = 0x040b)\
	//x(wm_CBEM_SETITEMW                  ,"CBEM_SETITEMW"                     , = 0x040c)\
	//x(wm_RB_GETBANDCOUNT                ,"RB_GETBANDCOUNT"                   , = 0x040c)\
	//x(wm_SB_GETTEXTLENGTHW              ,"SB_GETTEXTLENGTHW"                 , = 0x040c)\
	//x(wm_TB_ISBUTTONHIDDEN              ,"TB_ISBUTTONHIDDEN"                 , = 0x040c)\
	//x(wm_TBM_SETSELEND                  ,"TBM_SETSELEND"                     , = 0x040c)\
	//x(wm_TTM_UPDATETIPTEXTA             ,"TTM_UPDATETIPTEXTA"                , = 0x040c)\
	//x(wm_WIZ_PREV                       ,"WIZ_PREV"                          , = 0x040c)\
	//x(wm_CBEM_GETITEMW                  ,"CBEM_GETITEMW"                     , = 0x040d)\
	//x(wm_RB_GETROWCOUNT                 ,"RB_GETROWCOUNT"                    , = 0x040d)\
	//x(wm_SB_GETTEXTW                    ,"SB_GETTEXTW"                       , = 0x040d)\
	//x(wm_TB_ISBUTTONINDETERMINATE       ,"TB_ISBUTTONINDETERMINATE"          , = 0x040d)\
	//x(wm_TTM_GETTOOLCOUNT               ,"TTM_GETTOOLCOUNT"                  , = 0x040d)\
	//x(wm_CBEM_SETEXTENDEDSTYLE          ,"CBEM_SETEXTENDEDSTYLE"             , = 0x040e)\
	//x(wm_RB_GETROWHEIGHT                ,"RB_GETROWHEIGHT"                   , = 0x040e)\
	//x(wm_SB_ISSIMPLE                    ,"SB_ISSIMPLE"                       , = 0x040e)\
	//x(wm_TB_ISBUTTONHIGHLIGHTED         ,"TB_ISBUTTONHIGHLIGHTED"            , = 0x040e)\
	//x(wm_TBM_GETPTICS                   ,"TBM_GETPTICS"                      , = 0x040e)\
	//x(wm_TTM_ENUMTOOLSA                 ,"TTM_ENUMTOOLSA"                    , = 0x040e)\
	//x(wm_SB_SETICON                     ,"SB_SETICON"                        , = 0x040f)\
	//x(wm_TBM_GETTICPOS                  ,"TBM_GETTICPOS"                     , = 0x040f)\
	//x(wm_TTM_GETCURRENTTOOLA            ,"TTM_GETCURRENTTOOLA"               , = 0x040f)\
	//x(wm_RB_IDTOINDEX                   ,"RB_IDTOINDEX"                      , = 0x0410)\
	//x(wm_SB_SETTIPTEXTA                 ,"SB_SETTIPTEXTA"                    , = 0x0410)\
	//x(wm_TBM_GETNUMTICS                 ,"TBM_GETNUMTICS"                    , = 0x0410)\
	//x(wm_TTM_WINDOWFROMPOINT            ,"TTM_WINDOWFROMPOINT"               , = 0x0410)\
	//x(wm_RB_GETTOOLTIPS                 ,"RB_GETTOOLTIPS"                    , = 0x0411)\
	//x(wm_SB_SETTIPTEXTW                 ,"SB_SETTIPTEXTW"                    , = 0x0411)\
	//x(wm_TBM_GETSELSTART                ,"TBM_GETSELSTART"                   , = 0x0411)\
	//x(wm_TB_SETSTATE                    ,"TB_SETSTATE"                       , = 0x0411)\
	//x(wm_TTM_TRACKACTIVATE              ,"TTM_TRACKACTIVATE"                 , = 0x0411)\
	//x(wm_RB_SETTOOLTIPS                 ,"RB_SETTOOLTIPS"                    , = 0x0412)\
	//x(wm_SB_GETTIPTEXTA                 ,"SB_GETTIPTEXTA"                    , = 0x0412)\
	//x(wm_TB_GETSTATE                    ,"TB_GETSTATE"                       , = 0x0412)\
	//x(wm_TBM_GETSELEND                  ,"TBM_GETSELEND"                     , = 0x0412)\
	//x(wm_TTM_TRACKPOSITION              ,"TTM_TRACKPOSITION"                 , = 0x0412)\
	//x(wm_RB_SETBKCOLOR                  ,"RB_SETBKCOLOR"                     , = 0x0413)\
	//x(wm_SB_GETTIPTEXTW                 ,"SB_GETTIPTEXTW"                    , = 0x0413)\
	//x(wm_TB_ADDBITMAP                   ,"TB_ADDBITMAP"                      , = 0x0413)\
	//x(wm_TBM_CLEARSEL                   ,"TBM_CLEARSEL"                      , = 0x0413)\
	//x(wm_TTM_SETTIPBKCOLOR              ,"TTM_SETTIPBKCOLOR"                 , = 0x0413)\
	//x(wm_RB_GETBKCOLOR                  ,"RB_GETBKCOLOR"                     , = 0x0414)\
	//x(wm_SB_GETICON                     ,"SB_GETICON"                        , = 0x0414)\
	//x(wm_TB_ADDBUTTONSA                 ,"TB_ADDBUTTONSA"                    , = 0x0414)\
	//x(wm_TBM_SETTICFREQ                 ,"TBM_SETTICFREQ"                    , = 0x0414)\
	//x(wm_TTM_SETTIPTEXTCOLOR            ,"TTM_SETTIPTEXTCOLOR"               , = 0x0414)\
	//x(wm_RB_SETTEXTCOLOR                ,"RB_SETTEXTCOLOR"                   , = 0x0415)\
	//x(wm_TB_INSERTBUTTONA               ,"TB_INSERTBUTTONA"                  , = 0x0415)\
	//x(wm_TBM_SETPAGESIZE                ,"TBM_SETPAGESIZE"                   , = 0x0415)\
	//x(wm_TTM_GETDELAYTIME               ,"TTM_GETDELAYTIME"                  , = 0x0415)\
	//x(wm_RB_GETTEXTCOLOR                ,"RB_GETTEXTCOLOR"                   , = 0x0416)\
	//x(wm_TB_DELETEBUTTON                ,"TB_DELETEBUTTON"                   , = 0x0416)\
	//x(wm_TBM_GETPAGESIZE                ,"TBM_GETPAGESIZE"                   , = 0x0416)\
	//x(wm_TTM_GETTIPBKCOLOR              ,"TTM_GETTIPBKCOLOR"                 , = 0x0416)\
	//x(wm_RB_SIZETORECT                  ,"RB_SIZETORECT"                     , = 0x0417)\
	//x(wm_TB_GETBUTTON                   ,"TB_GETBUTTON"                      , = 0x0417)\
	//x(wm_TBM_SETLINESIZE                ,"TBM_SETLINESIZE"                   , = 0x0417)\
	//x(wm_TTM_GETTIPTEXTCOLOR            ,"TTM_GETTIPTEXTCOLOR"               , = 0x0417)\
	//x(wm_RB_BEGINDRAG                   ,"RB_BEGINDRAG"                      , = 0x0418)\
	//x(wm_TB_BUTTONCOUNT                 ,"TB_BUTTONCOUNT"                    , = 0x0418)\
	//x(wm_TBM_GETLINESIZE                ,"TBM_GETLINESIZE"                   , = 0x0418)\
	//x(wm_TTM_SETMAXTIPWIDTH             ,"TTM_SETMAXTIPWIDTH"                , = 0x0418)\
	//x(wm_RB_ENDDRAG                     ,"RB_ENDDRAG"                        , = 0x0419)\
	//x(wm_TB_COMMANDTOINDEX              ,"TB_COMMANDTOINDEX"                 , = 0x0419)\
	//x(wm_TBM_GETTHUMBRECT               ,"TBM_GETTHUMBRECT"                  , = 0x0419)\
	//x(wm_TTM_GETMAXTIPWIDTH             ,"TTM_GETMAXTIPWIDTH"                , = 0x0419)\
	//x(wm_RB_DRAGMOVE                    ,"RB_DRAGMOVE"                       , = 0x041a)\
	//x(wm_TBM_GETCHANNELRECT             ,"TBM_GETCHANNELRECT"                , = 0x041a)\
	//x(wm_TB_SAVERESTOREA                ,"TB_SAVERESTOREA"                   , = 0x041a)\
	//x(wm_TTM_SETMARGIN                  ,"TTM_SETMARGIN"                     , = 0x041a)\
	//x(wm_RB_GETBARHEIGHT                ,"RB_GETBARHEIGHT"                   , = 0x041b)\
	//x(wm_TB_CUSTOMIZE                   ,"TB_CUSTOMIZE"                      , = 0x041b)\
	//x(wm_TBM_SETTHUMBLENGTH             ,"TBM_SETTHUMBLENGTH"                , = 0x041b)\
	//x(wm_TTM_GETMARGIN                  ,"TTM_GETMARGIN"                     , = 0x041b)\
	//x(wm_RB_GETBANDINFOW                ,"RB_GETBANDINFOW"                   , = 0x041c)\
	//x(wm_TB_ADDSTRINGA                  ,"TB_ADDSTRINGA"                     , = 0x041c)\
	//x(wm_TBM_GETTHUMBLENGTH             ,"TBM_GETTHUMBLENGTH"                , = 0x041c)\
	//x(wm_TTM_POP                        ,"TTM_POP"                           , = 0x041c)\
	//x(wm_RB_GETBANDINFOA                ,"RB_GETBANDINFOA"                   , = 0x041d)\
	//x(wm_TB_GETITEMRECT                 ,"TB_GETITEMRECT"                    , = 0x041d)\
	//x(wm_TBM_SETTOOLTIPS                ,"TBM_SETTOOLTIPS"                   , = 0x041d)\
	//x(wm_TTM_UPDATE                     ,"TTM_UPDATE"                        , = 0x041d)\
	//x(wm_RB_MINIMIZEBAND                ,"RB_MINIMIZEBAND"                   , = 0x041e)\
	//x(wm_TB_BUTTONSTRUCTSIZE            ,"TB_BUTTONSTRUCTSIZE"               , = 0x041e)\
	//x(wm_TBM_GETTOOLTIPS                ,"TBM_GETTOOLTIPS"                   , = 0x041e)\
	//x(wm_TTM_GETBUBBLESIZE              ,"TTM_GETBUBBLESIZE"                 , = 0x041e)\
	//x(wm_RB_MAXIMIZEBAND                ,"RB_MAXIMIZEBAND"                   , = 0x041f)\
	//x(wm_TBM_SETTIPSIDE                 ,"TBM_SETTIPSIDE"                    , = 0x041f)\
	//x(wm_TB_SETBUTTONSIZE               ,"TB_SETBUTTONSIZE"                  , = 0x041f)\
	//x(wm_TTM_ADJUSTRECT                 ,"TTM_ADJUSTRECT"                    , = 0x041f)\
	//x(wm_TBM_SETBUDDY                   ,"TBM_SETBUDDY"                      , = 0x0420)\
	//x(wm_TB_SETBITMAPSIZE               ,"TB_SETBITMAPSIZE"                  , = 0x0420)\
	//x(wm_TTM_SETTITLEA                  ,"TTM_SETTITLEA"                     , = 0x0420)\
	//x(wm_MSG_FTS_JUMP_VA                ,"MSG_FTS_JUMP_VA"                   , = 0x0421)\
	//x(wm_TB_AUTOSIZE                    ,"TB_AUTOSIZE"                       , = 0x0421)\
	//x(wm_TBM_GETBUDDY                   ,"TBM_GETBUDDY"                      , = 0x0421)\
	//x(wm_TTM_SETTITLEW                  ,"TTM_SETTITLEW"                     , = 0x0421)\
	//x(wm_RB_GETBANDBORDERS              ,"RB_GETBANDBORDERS"                 , = 0x0422)\
	//x(wm_MSG_FTS_JUMP_QWORD             ,"MSG_FTS_JUMP_QWORD"                , = 0x0423)\
	//x(wm_RB_SHOWBAND                    ,"RB_SHOWBAND"                       , = 0x0423)\
	//x(wm_TB_GETTOOLTIPS                 ,"TB_GETTOOLTIPS"                    , = 0x0423)\
	//x(wm_MSG_REINDEX_REQUEST            ,"MSG_REINDEX_REQUEST"               , = 0x0424)\
	//x(wm_TB_SETTOOLTIPS                 ,"TB_SETTOOLTIPS"                    , = 0x0424)\
	//x(wm_MSG_FTS_WHERE_IS_IT            ,"MSG_FTS_WHERE_IS_IT"               , = 0x0425)\
	//x(wm_RB_SETPALETTE                  ,"RB_SETPALETTE"                     , = 0x0425)\
	//x(wm_TB_SETPARENT                   ,"TB_SETPARENT"                      , = 0x0425)\
	//x(wm_RB_GETPALETTE                  ,"RB_GETPALETTE"                     , = 0x0426)\
	//x(wm_RB_MOVEBAND                    ,"RB_MOVEBAND"                       , = 0x0427)\
	//x(wm_TB_SETROWS                     ,"TB_SETROWS"                        , = 0x0427)\
	//x(wm_TB_GETROWS                     ,"TB_GETROWS"                        , = 0x0428)\
	//x(wm_TB_GETBITMAPFLAGS              ,"TB_GETBITMAPFLAGS"                 , = 0x0429)\
	//x(wm_TB_SETCMDID                    ,"TB_SETCMDID"                       , = 0x042a)\
	//x(wm_RB_PUSHCHEVRON                 ,"RB_PUSHCHEVRON"                    , = 0x042b)\
	//x(wm_TB_CHANGEBITMAP                ,"TB_CHANGEBITMAP"                   , = 0x042b)\
	//x(wm_TB_GETBITMAP                   ,"TB_GETBITMAP"                      , = 0x042c)\
	//x(wm_MSG_GET_DEFFONT                ,"MSG_GET_DEFFONT"                   , = 0x042d)\
	//x(wm_TB_GETBUTTONTEXTA              ,"TB_GETBUTTONTEXTA"                 , = 0x042d)\
	//x(wm_TB_REPLACEBITMAP               ,"TB_REPLACEBITMAP"                  , = 0x042e)\
	//x(wm_TB_SETINDENT                   ,"TB_SETINDENT"                      , = 0x042f)\
	//x(wm_TB_SETIMAGELIST                ,"TB_SETIMAGELIST"                   , = 0x0430)\
	//x(wm_TB_GETIMAGELIST                ,"TB_GETIMAGELIST"                   , = 0x0431)\
	//x(wm_TB_LOADIMAGES                  ,"TB_LOADIMAGES"                     , = 0x0432)\
	//x(wm_TTM_ADDTOOLW                   ,"TTM_ADDTOOLW"                      , = 0x0432)\
	//x(wm_TB_GETRECT                     ,"TB_GETRECT"                        , = 0x0433)\
	//x(wm_TTM_DELTOOLW                   ,"TTM_DELTOOLW"                      , = 0x0433)\
	//x(wm_TB_SETHOTIMAGELIST             ,"TB_SETHOTIMAGELIST"                , = 0x0434)\
	//x(wm_TTM_NEWTOOLRECTW               ,"TTM_NEWTOOLRECTW"                  , = 0x0434)\
	//x(wm_TB_GETHOTIMAGELIST             ,"TB_GETHOTIMAGELIST"                , = 0x0435)\
	//x(wm_TTM_GETTOOLINFOW               ,"TTM_GETTOOLINFOW"                  , = 0x0435)\
	//x(wm_TB_SETDISABLEDIMAGELIST        ,"TB_SETDISABLEDIMAGELIST"           , = 0x0436)\
	//x(wm_TTM_SETTOOLINFOW               ,"TTM_SETTOOLINFOW"                  , = 0x0436)\
	//x(wm_TB_GETDISABLEDIMAGELIST        ,"TB_GETDISABLEDIMAGELIST"           , = 0x0437)\
	//x(wm_TTM_HITTESTW                   ,"TTM_HITTESTW"                      , = 0x0437)\
	//x(wm_TB_SETSTYLE                    ,"TB_SETSTYLE"                       , = 0x0438)\
	//x(wm_TTM_GETTEXTW                   ,"TTM_GETTEXTW"                      , = 0x0438)\
	//x(wm_TB_GETSTYLE                    ,"TB_GETSTYLE"                       , = 0x0439)\
	//x(wm_TTM_UPDATETIPTEXTW             ,"TTM_UPDATETIPTEXTW"                , = 0x0439)\
	//x(wm_TB_GETBUTTONSIZE               ,"TB_GETBUTTONSIZE"                  , = 0x043a)\
	//x(wm_TTM_ENUMTOOLSW                 ,"TTM_ENUMTOOLSW"                    , = 0x043a)\
	//x(wm_TB_SETBUTTONWIDTH              ,"TB_SETBUTTONWIDTH"                 , = 0x043b)\
	//x(wm_TTM_GETCURRENTTOOLW            ,"TTM_GETCURRENTTOOLW"               , = 0x043b)\
	//x(wm_TB_SETMAXTEXTROWS              ,"TB_SETMAXTEXTROWS"                 , = 0x043c)\
	//x(wm_TB_GETTEXTROWS                 ,"TB_GETTEXTROWS"                    , = 0x043d)\
	//x(wm_TB_GETOBJECT                   ,"TB_GETOBJECT"                      , = 0x043e)\
	//x(wm_TB_GETBUTTONINFOW              ,"TB_GETBUTTONINFOW"                 , = 0x043f)\
	//x(wm_TB_SETBUTTONINFOW              ,"TB_SETBUTTONINFOW"                 , = 0x0440)\
	//x(wm_TB_GETBUTTONINFOA              ,"TB_GETBUTTONINFOA"                 , = 0x0441)\
	//x(wm_TB_SETBUTTONINFOA              ,"TB_SETBUTTONINFOA"                 , = 0x0442)\
	//x(wm_TB_INSERTBUTTONW               ,"TB_INSERTBUTTONW"                  , = 0x0443)\
	//x(wm_TB_ADDBUTTONSW                 ,"TB_ADDBUTTONSW"                    , = 0x0444)\
	//x(wm_TB_HITTEST                     ,"TB_HITTEST"                        , = 0x0445)\
	//x(wm_TB_SETDRAWTEXTFLAGS            ,"TB_SETDRAWTEXTFLAGS"               , = 0x0446)\
	//x(wm_TB_GETHOTITEM                  ,"TB_GETHOTITEM"                     , = 0x0447)\
	//x(wm_TB_SETHOTITEM                  ,"TB_SETHOTITEM"                     , = 0x0448)\
	//x(wm_TB_SETANCHORHIGHLIGHT          ,"TB_SETANCHORHIGHLIGHT"             , = 0x0449)\
	//x(wm_TB_GETANCHORHIGHLIGHT          ,"TB_GETANCHORHIGHLIGHT"             , = 0x044a)\
	//x(wm_TB_GETBUTTONTEXTW              ,"TB_GETBUTTONTEXTW"                 , = 0x044b)\
	//x(wm_TB_SAVERESTOREW                ,"TB_SAVERESTOREW"                   , = 0x044c)\
	//x(wm_TB_ADDSTRINGW                  ,"TB_ADDSTRINGW"                     , = 0x044d)\
	//x(wm_TB_MAPACCELERATORA             ,"TB_MAPACCELERATORA"                , = 0x044e)\
	//x(wm_TB_GETINSERTMARK               ,"TB_GETINSERTMARK"                  , = 0x044f)\
	//x(wm_TB_SETINSERTMARK               ,"TB_SETINSERTMARK"                  , = 0x0450)\
	//x(wm_TB_INSERTMARKHITTEST           ,"TB_INSERTMARKHITTEST"              , = 0x0451)\
	//x(wm_TB_MOVEBUTTON                  ,"TB_MOVEBUTTON"                     , = 0x0452)\
	//x(wm_TB_GETMAXSIZE                  ,"TB_GETMAXSIZE"                     , = 0x0453)\
	//x(wm_TB_SETEXTENDEDSTYLE            ,"TB_SETEXTENDEDSTYLE"               , = 0x0454)\
	//x(wm_TB_GETEXTENDEDSTYLE            ,"TB_GETEXTENDEDSTYLE"               , = 0x0455)\
	//x(wm_TB_GETPADDING                  ,"TB_GETPADDING"                     , = 0x0456)\
	//x(wm_TB_SETPADDING                  ,"TB_SETPADDING"                     , = 0x0457)\
	//x(wm_TB_SETINSERTMARKCOLOR          ,"TB_SETINSERTMARKCOLOR"             , = 0x0458)\
	//x(wm_TB_GETINSERTMARKCOLOR          ,"TB_GETINSERTMARKCOLOR"             , = 0x0459)\
	//x(wm_TB_MAPACCELERATORW             ,"TB_MAPACCELERATORW"                , = 0x045a)\
	//x(wm_TB_GETSTRINGW                  ,"TB_GETSTRINGW"                     , = 0x045b)\
	//x(wm_TB_GETSTRINGA                  ,"TB_GETSTRINGA"                     , = 0x045c)\
	//x(wm_TAPI_REPLY                     ,"TAPI_REPLY"                        , = 0x0463)\
	//x(wm_ACM_OPENA                      ,"ACM_OPENA"                         , = 0x0464)\
	//x(wm_BFFM_SETSTATUSTEXTA            ,"BFFM_SETSTATUSTEXTA"               , = 0x0464)\
	//x(wm_CDM_FIRST                      ,"CDM_FIRST"                         , = 0x0464)\
	//x(wm_CDM_GETSPEC                    ,"CDM_GETSPEC"                       , = 0x0464)\
	//x(wm_IPM_CLEARADDRESS               ,"IPM_CLEARADDRESS"                  , = 0x0464)\
	//x(wm_WM_CAP_UNICODE_START           ,"WM_CAP_UNICODE_START"              , = 0x0464)\
	//x(wm_ACM_PLAY                       ,"ACM_PLAY"                          , = 0x0465)\
	//x(wm_BFFM_ENABLEOK                  ,"BFFM_ENABLEOK"                     , = 0x0465)\
	//x(wm_CDM_GETFILEPATH                ,"CDM_GETFILEPATH"                   , = 0x0465)\
	//x(wm_IPM_SETADDRESS                 ,"IPM_SETADDRESS"                    , = 0x0465)\
	//x(wm_PSM_SETCURSEL                  ,"PSM_SETCURSEL"                     , = 0x0465)\
	//x(wm_UDM_SETRANGE                   ,"UDM_SETRANGE"                      , = 0x0465)\
	//x(wm_WM_CHOOSEFONT_SETLOGFONT       ,"WM_CHOOSEFONT_SETLOGFONT"          , = 0x0465)\
	//x(wm_ACM_STOP                       ,"ACM_STOP"                          , = 0x0466)\
	//x(wm_BFFM_SETSELECTIONA             ,"BFFM_SETSELECTIONA"                , = 0x0466)\
	//x(wm_CDM_GETFOLDERPATH              ,"CDM_GETFOLDERPATH"                 , = 0x0466)\
	//x(wm_IPM_GETADDRESS                 ,"IPM_GETADDRESS"                    , = 0x0466)\
	//x(wm_PSM_REMOVEPAGE                 ,"PSM_REMOVEPAGE"                    , = 0x0466)\
	//x(wm_UDM_GETRANGE                   ,"UDM_GETRANGE"                      , = 0x0466)\
	//x(wm_WM_CAP_SET_CALLBACK_ERRORW     ,"WM_CAP_SET_CALLBACK_ERRORW"        , = 0x0466)\
	//x(wm_WM_CHOOSEFONT_SETFLAGS         ,"WM_CHOOSEFONT_SETFLAGS"            , = 0x0466)\
	//x(wm_ACM_OPENW                      ,"ACM_OPENW"                         , = 0x0467)\
	//x(wm_BFFM_SETSELECTIONW             ,"BFFM_SETSELECTIONW"                , = 0x0467)\
	//x(wm_CDM_GETFOLDERIDLIST            ,"CDM_GETFOLDERIDLIST"               , = 0x0467)\
	//x(wm_IPM_SETRANGE                   ,"IPM_SETRANGE"                      , = 0x0467)\
	//x(wm_PSM_ADDPAGE                    ,"PSM_ADDPAGE"                       , = 0x0467)\
	//x(wm_UDM_SETPOS                     ,"UDM_SETPOS"                        , = 0x0467)\
	//x(wm_WM_CAP_SET_CALLBACK_STATUSW    ,"WM_CAP_SET_CALLBACK_STATUSW"       , = 0x0467)\
	//x(wm_BFFM_SETSTATUSTEXTW            ,"BFFM_SETSTATUSTEXTW"               , = 0x0468)\
	//x(wm_CDM_SETCONTROLTEXT             ,"CDM_SETCONTROLTEXT"                , = 0x0468)\
	//x(wm_IPM_SETFOCUS                   ,"IPM_SETFOCUS"                      , = 0x0468)\
	//x(wm_PSM_CHANGED                    ,"PSM_CHANGED"                       , = 0x0468)\
	//x(wm_UDM_GETPOS                     ,"UDM_GETPOS"                        , = 0x0468)\
	//x(wm_CDM_HIDECONTROL                ,"CDM_HIDECONTROL"                   , = 0x0469)\
	//x(wm_IPM_ISBLANK                    ,"IPM_ISBLANK"                       , = 0x0469)\
	//x(wm_PSM_RESTARTWINDOWS             ,"PSM_RESTARTWINDOWS"                , = 0x0469)\
	//x(wm_UDM_SETBUDDY                   ,"UDM_SETBUDDY"                      , = 0x0469)\
	//x(wm_CDM_SETDEFEXT                  ,"CDM_SETDEFEXT"                     , = 0x046a)\
	//x(wm_PSM_REBOOTSYSTEM               ,"PSM_REBOOTSYSTEM"                  , = 0x046a)\
	//x(wm_UDM_GETBUDDY                   ,"UDM_GETBUDDY"                      , = 0x046a)\
	//x(wm_PSM_CANCELTOCLOSE              ,"PSM_CANCELTOCLOSE"                 , = 0x046b)\
	//x(wm_UDM_SETACCEL                   ,"UDM_SETACCEL"                      , = 0x046b)\
	//x(wm_EM_CONVPOSITION                ,"EM_CONVPOSITION"                   , = 0x046c)\
	//x(wm_PSM_QUERYSIBLINGS              ,"PSM_QUERYSIBLINGS"                 , = 0x046c)\
	//x(wm_UDM_GETACCEL                   ,"UDM_GETACCEL"                      , = 0x046c)\
	//x(wm_MCIWNDM_GETZOOM                ,"MCIWNDM_GETZOOM"                   , = 0x046d)\
	//x(wm_PSM_UNCHANGED                  ,"PSM_UNCHANGED"                     , = 0x046d)\
	//x(wm_UDM_SETBASE                    ,"UDM_SETBASE"                       , = 0x046d)\
	//x(wm_PSM_APPLY                      ,"PSM_APPLY"                         , = 0x046e)\
	//x(wm_UDM_GETBASE                    ,"UDM_GETBASE"                       , = 0x046e)\
	//x(wm_PSM_SETTITLEA                  ,"PSM_SETTITLEA"                     , = 0x046f)\
	//x(wm_UDM_SETRANGE32                 ,"UDM_SETRANGE32"                    , = 0x046f)\
	//x(wm_PSM_SETWIZBUTTONS              ,"PSM_SETWIZBUTTONS"                 , = 0x0470)\
	//x(wm_UDM_GETRANGE32                 ,"UDM_GETRANGE32"                    , = 0x0470)\
	//x(wm_WM_CAP_DRIVER_GET_NAMEW        ,"WM_CAP_DRIVER_GET_NAMEW"           , = 0x0470)\
	//x(wm_PSM_PRESSBUTTON                ,"PSM_PRESSBUTTON"                   , = 0x0471)\
	//x(wm_UDM_SETPOS32                   ,"UDM_SETPOS32"                      , = 0x0471)\
	//x(wm_WM_CAP_DRIVER_GET_VERSIONW     ,"WM_CAP_DRIVER_GET_VERSIONW"        , = 0x0471)\
	//x(wm_PSM_SETCURSELID                ,"PSM_SETCURSELID"                   , = 0x0472)\
	//x(wm_UDM_GETPOS32                   ,"UDM_GETPOS32"                      , = 0x0472)\
	//x(wm_PSM_SETFINISHTEXTA             ,"PSM_SETFINISHTEXTA"                , = 0x0473)\
	//x(wm_PSM_GETTABCONTROL              ,"PSM_GETTABCONTROL"                 , = 0x0474)\
	//x(wm_PSM_ISDIALOGMESSAGE            ,"PSM_ISDIALOGMESSAGE"               , = 0x0475)\
	//x(wm_MCIWNDM_REALIZE                ,"MCIWNDM_REALIZE"                   , = 0x0476)\
	//x(wm_PSM_GETCURRENTPAGEHWND         ,"PSM_GETCURRENTPAGEHWND"            , = 0x0476)\
	//x(wm_MCIWNDM_SETTIMEFORMATA         ,"MCIWNDM_SETTIMEFORMATA"            , = 0x0477)\
	//x(wm_PSM_INSERTPAGE                 ,"PSM_INSERTPAGE"                    , = 0x0477)\
	//x(wm_MCIWNDM_GETTIMEFORMATA         ,"MCIWNDM_GETTIMEFORMATA"            , = 0x0478)\
	//x(wm_PSM_SETTITLEW                  ,"PSM_SETTITLEW"                     , = 0x0478)\
	//x(wm_WM_CAP_FILE_SET_CAPTURE_FILEW  ,"WM_CAP_FILE_SET_CAPTURE_FILEW"     , = 0x0478)\
	//x(wm_MCIWNDM_VALIDATEMEDIA          ,"MCIWNDM_VALIDATEMEDIA"             , = 0x0479)\
	//x(wm_PSM_SETFINISHTEXTW             ,"PSM_SETFINISHTEXTW"                , = 0x0479)\
	//x(wm_WM_CAP_FILE_GET_CAPTURE_FILEW  ,"WM_CAP_FILE_GET_CAPTURE_FILEW"     , = 0x0479)\
	//x(wm_MCIWNDM_PLAYTO                 ,"MCIWNDM_PLAYTO"                    , = 0x047b)\
	//x(wm_WM_CAP_FILE_SAVEASW            ,"WM_CAP_FILE_SAVEASW"               , = 0x047b)\
	//x(wm_MCIWNDM_GETFILENAMEA           ,"MCIWNDM_GETFILENAMEA"              , = 0x047c)\
	//x(wm_MCIWNDM_GETDEVICEA             ,"MCIWNDM_GETDEVICEA"                , = 0x047d)\
	//x(wm_PSM_SETHEADERTITLEA            ,"PSM_SETHEADERTITLEA"               , = 0x047d)\
	//x(wm_WM_CAP_FILE_SAVEDIBW           ,"WM_CAP_FILE_SAVEDIBW"              , = 0x047d)\
	//x(wm_MCIWNDM_GETPALETTE             ,"MCIWNDM_GETPALETTE"                , = 0x047e)\
	//x(wm_PSM_SETHEADERTITLEW            ,"PSM_SETHEADERTITLEW"               , = 0x047e)\
	//x(wm_MCIWNDM_SETPALETTE             ,"MCIWNDM_SETPALETTE"                , = 0x047f)\
	//x(wm_PSM_SETHEADERSUBTITLEA         ,"PSM_SETHEADERSUBTITLEA"            , = 0x047f)\
	//x(wm_MCIWNDM_GETERRORA              ,"MCIWNDM_GETERRORA"                 , = 0x0480)\
	//x(wm_PSM_SETHEADERSUBTITLEW         ,"PSM_SETHEADERSUBTITLEW"            , = 0x0480)\
	//x(wm_PSM_HWNDTOINDEX                ,"PSM_HWNDTOINDEX"                   , = 0x0481)\
	//x(wm_PSM_INDEXTOHWND                ,"PSM_INDEXTOHWND"                   , = 0x0482)\
	//x(wm_MCIWNDM_SETINACTIVETIMER       ,"MCIWNDM_SETINACTIVETIMER"          , = 0x0483)\
	//x(wm_PSM_PAGETOINDEX                ,"PSM_PAGETOINDEX"                   , = 0x0483)\
	//x(wm_PSM_INDEXTOPAGE                ,"PSM_INDEXTOPAGE"                   , = 0x0484)\
	//x(wm_DL_BEGINDRAG                   ,"DL_BEGINDRAG"                      , = 0x0485)\
	//x(wm_MCIWNDM_GETINACTIVETIMER       ,"MCIWNDM_GETINACTIVETIMER"          , = 0x0485)\
	//x(wm_PSM_IDTOINDEX                  ,"PSM_IDTOINDEX"                     , = 0x0485)\
	//x(wm_DL_DRAGGING                    ,"DL_DRAGGING"                       , = 0x0486)\
	//x(wm_PSM_INDEXTOID                  ,"PSM_INDEXTOID"                     , = 0x0486)\
	//x(wm_DL_DROPPED                     ,"DL_DROPPED"                        , = 0x0487)\
	//x(wm_PSM_GETRESULT                  ,"PSM_GETRESULT"                     , = 0x0487)\
	//x(wm_DL_CANCELDRAG                  ,"DL_CANCELDRAG"                     , = 0x0488)\
	//x(wm_PSM_RECALCPAGESIZES            ,"PSM_RECALCPAGESIZES"               , = 0x0488)\
	//x(wm_MCIWNDM_GET_SOURCE             ,"MCIWNDM_GET_SOURCE"                , = 0x048c)\
	//x(wm_MCIWNDM_PUT_SOURCE             ,"MCIWNDM_PUT_SOURCE"                , = 0x048d)\
	//x(wm_MCIWNDM_GET_DEST               ,"MCIWNDM_GET_DEST"                  , = 0x048e)\
	//x(wm_MCIWNDM_PUT_DEST               ,"MCIWNDM_PUT_DEST"                  , = 0x048f)\
	//x(wm_MCIWNDM_CAN_PLAY               ,"MCIWNDM_CAN_PLAY"                  , = 0x0490)\
	//x(wm_MCIWNDM_CAN_WINDOW             ,"MCIWNDM_CAN_WINDOW"                , = 0x0491)\
	//x(wm_MCIWNDM_CAN_RECORD             ,"MCIWNDM_CAN_RECORD"                , = 0x0492)\
	//x(wm_MCIWNDM_CAN_SAVE               ,"MCIWNDM_CAN_SAVE"                  , = 0x0493)\
	//x(wm_MCIWNDM_CAN_EJECT              ,"MCIWNDM_CAN_EJECT"                 , = 0x0494)\
	//x(wm_MCIWNDM_CAN_CONFIG             ,"MCIWNDM_CAN_CONFIG"                , = 0x0495)\
	//x(wm_IE_GETINK                      ,"IE_GETINK"                         , = 0x0496)\
	//x(wm_IE_MSGFIRST                    ,"IE_MSGFIRST"                       , = 0x0496)\
	//x(wm_MCIWNDM_PALETTEKICK            ,"MCIWNDM_PALETTEKICK"               , = 0x0496)\
	//x(wm_IE_SETINK                      ,"IE_SETINK"                         , = 0x0497)\
	//x(wm_IE_GETPENTIP                   ,"IE_GETPENTIP"                      , = 0x0498)\
	//x(wm_IE_SETPENTIP                   ,"IE_SETPENTIP"                      , = 0x0499)\
	//x(wm_IE_GETERASERTIP                ,"IE_GETERASERTIP"                   , = 0x049a)\
	//x(wm_IE_SETERASERTIP                ,"IE_SETERASERTIP"                   , = 0x049b)\
	//x(wm_IE_GETBKGND                    ,"IE_GETBKGND"                       , = 0x049c)\
	//x(wm_IE_SETBKGND                    ,"IE_SETBKGND"                       , = 0x049d)\
	//x(wm_IE_GETGRIDORIGIN               ,"IE_GETGRIDORIGIN"                  , = 0x049e)\
	//x(wm_IE_SETGRIDORIGIN               ,"IE_SETGRIDORIGIN"                  , = 0x049f)\
	//x(wm_IE_GETGRIDPEN                  ,"IE_GETGRIDPEN"                     , = 0x04a0)\
	//x(wm_IE_SETGRIDPEN                  ,"IE_SETGRIDPEN"                     , = 0x04a1)\
	//x(wm_IE_GETGRIDSIZE                 ,"IE_GETGRIDSIZE"                    , = 0x04a2)\
	//x(wm_IE_SETGRIDSIZE                 ,"IE_SETGRIDSIZE"                    , = 0x04a3)\
	//x(wm_IE_GETMODE                     ,"IE_GETMODE"                        , = 0x04a4)\
	//x(wm_IE_SETMODE                     ,"IE_SETMODE"                        , = 0x04a5)\
	//x(wm_IE_GETINKRECT                  ,"IE_GETINKRECT"                     , = 0x04a6)\
	//x(wm_WM_CAP_SET_MCI_DEVICEW         ,"WM_CAP_SET_MCI_DEVICEW"            , = 0x04a6)\
	//x(wm_WM_CAP_GET_MCI_DEVICEW         ,"WM_CAP_GET_MCI_DEVICEW"            , = 0x04a7)\
	//x(wm_WM_CAP_PAL_OPENW               ,"WM_CAP_PAL_OPENW"                  , = 0x04b4)\
	//x(wm_WM_CAP_PAL_SAVEW               ,"WM_CAP_PAL_SAVEW"                  , = 0x04b5)\
	//x(wm_IE_GETAPPDATA                  ,"IE_GETAPPDATA"                     , = 0x04b8)\
	//x(wm_IE_SETAPPDATA                  ,"IE_SETAPPDATA"                     , = 0x04b9)\
	//x(wm_IE_GETDRAWOPTS                 ,"IE_GETDRAWOPTS"                    , = 0x04ba)\
	//x(wm_IE_SETDRAWOPTS                 ,"IE_SETDRAWOPTS"                    , = 0x04bb)\
	//x(wm_IE_GETFORMAT                   ,"IE_GETFORMAT"                      , = 0x04bc)\
	//x(wm_IE_SETFORMAT                   ,"IE_SETFORMAT"                      , = 0x04bd)\
	//x(wm_IE_GETINKINPUT                 ,"IE_GETINKINPUT"                    , = 0x04be)\
	//x(wm_IE_SETINKINPUT                 ,"IE_SETINKINPUT"                    , = 0x04bf)\
	//x(wm_IE_GETNOTIFY                   ,"IE_GETNOTIFY"                      , = 0x04c0)\
	//x(wm_IE_SETNOTIFY                   ,"IE_SETNOTIFY"                      , = 0x04c1)\
	//x(wm_IE_GETRECOG                    ,"IE_GETRECOG"                       , = 0x04c2)\
	//x(wm_IE_SETRECOG                    ,"IE_SETRECOG"                       , = 0x04c3)\
	//x(wm_IE_GETSECURITY                 ,"IE_GETSECURITY"                    , = 0x04c4)\
	//x(wm_IE_SETSECURITY                 ,"IE_SETSECURITY"                    , = 0x04c5)\
	//x(wm_IE_GETSEL                      ,"IE_GETSEL"                         , = 0x04c6)\
	//x(wm_IE_SETSEL                      ,"IE_SETSEL"                         , = 0x04c7)\
	//x(wm_CDM_LAST                       ,"CDM_LAST"                          , = 0x04c8)\
	//x(wm_IE_DOCOMMAND                   ,"IE_DOCOMMAND"                      , = 0x04c8)\
	//x(wm_MCIWNDM_NOTIFYMODE             ,"MCIWNDM_NOTIFYMODE"                , = 0x04c8)\
	//x(wm_IE_GETCOMMAND                  ,"IE_GETCOMMAND"                     , = 0x04c9)\
	//x(wm_IE_GETCOUNT                    ,"IE_GETCOUNT"                       , = 0x04ca)\
	//x(wm_IE_GETGESTURE                  ,"IE_GETGESTURE"                     , = 0x04cb)\
	//x(wm_MCIWNDM_NOTIFYMEDIA            ,"MCIWNDM_NOTIFYMEDIA"               , = 0x04cb)\
	//x(wm_IE_GETMENU                     ,"IE_GETMENU"                        , = 0x04cc)\
	//x(wm_IE_GETPAINTDC                  ,"IE_GETPAINTDC"                     , = 0x04cd)\
	//x(wm_MCIWNDM_NOTIFYERROR            ,"MCIWNDM_NOTIFYERROR"               , = 0x04cd)\
	//x(wm_IE_GETPDEVENT                  ,"IE_GETPDEVENT"                     , = 0x04ce)\
	//x(wm_IE_GETSELCOUNT                 ,"IE_GETSELCOUNT"                    , = 0x04cf)\
	//x(wm_IE_GETSELITEMS                 ,"IE_GETSELITEMS"                    , = 0x04d0)\
	//x(wm_IE_GETSTYLE                    ,"IE_GETSTYLE"                       , = 0x04d1)\
	//x(wm_MCIWNDM_SETTIMEFORMATW         ,"MCIWNDM_SETTIMEFORMATW"            , = 0x04db)\
	//x(wm_EM_OUTLINE                     ,"EM_OUTLINE"                        , = 0x04dc)\
	//x(wm_MCIWNDM_GETTIMEFORMATW         ,"MCIWNDM_GETTIMEFORMATW"            , = 0x04dc)\
	//x(wm_EM_GETSCROLLPOS                ,"EM_GETSCROLLPOS"                   , = 0x04dd)\
	//x(wm_EM_SETSCROLLPOS                ,"EM_SETSCROLLPOS"                   , = 0x04de)\
	//x(wm_EM_SETFONTSIZE                 ,"EM_SETFONTSIZE"                    , = 0x04df)\
	//x(wm_MCIWNDM_GETFILENAMEW           ,"MCIWNDM_GETFILENAMEW"              , = 0x04e0)\
	//x(wm_MCIWNDM_GETDEVICEW             ,"MCIWNDM_GETDEVICEW"                , = 0x04e1)\
	//x(wm_MCIWNDM_GETERRORW              ,"MCIWNDM_GETERRORW"                 , = 0x04e4)\
	//x(wm_FM_GETFOCUS                    ,"FM_GETFOCUS"                       , = 0x0600)\
	//x(wm_FM_GETDRIVEINFOA               ,"FM_GETDRIVEINFOA"                  , = 0x0601)\
	//x(wm_FM_GETSELCOUNT                 ,"FM_GETSELCOUNT"                    , = 0x0602)\
	//x(wm_FM_GETSELCOUNTLFN              ,"FM_GETSELCOUNTLFN"                 , = 0x0603)\
	//x(wm_FM_GETFILESELA                 ,"FM_GETFILESELA"                    , = 0x0604)\
	//x(wm_FM_GETFILESELLFNA              ,"FM_GETFILESELLFNA"                 , = 0x0605)\
	//x(wm_FM_REFRESH_WINDOWS             ,"FM_REFRESH_WINDOWS"                , = 0x0606)\
	//x(wm_FM_RELOAD_EXTENSIONS           ,"FM_RELOAD_EXTENSIONS"              , = 0x0607)\
	//x(wm_FM_GETDRIVEINFOW               ,"FM_GETDRIVEINFOW"                  , = 0x0611)\
	//x(wm_FM_GETFILESELW                 ,"FM_GETFILESELW"                    , = 0x0614)\
	//x(wm_FM_GETFILESELLFNW              ,"FM_GETFILESELLFNW"                 , = 0x0615)\
	//x(wm_WLX_WM_SAS                     ,"WLX_WM_SAS"                        , = 0x0659)\
	//x(wm_SM_GETSELCOUNT                 ,"SM_GETSELCOUNT"                    , = 0x07e8)\
	//x(wm_UM_GETSELCOUNT                 ,"UM_GETSELCOUNT"                    , = 0x07e8)\
	//x(wm_WM_CPL_LAUNCH                  ,"WM_CPL_LAUNCH"                     , = 0x07e8)\
	//x(wm_SM_GETSERVERSELA               ,"SM_GETSERVERSELA"                  , = 0x07e9)\
	//x(wm_UM_GETUSERSELA                 ,"UM_GETUSERSELA"                    , = 0x07e9)\
	//x(wm_WM_CPL_LAUNCHED                ,"WM_CPL_LAUNCHED"                   , = 0x07e9)\
	//x(wm_SM_GETSERVERSELW               ,"SM_GETSERVERSELW"                  , = 0x07ea)\
	//x(wm_UM_GETUSERSELW                 ,"UM_GETUSERSELW"                    , = 0x07ea)\
	//x(wm_SM_GETCURFOCUSA                ,"SM_GETCURFOCUSA"                   , = 0x07eb)\
	//x(wm_UM_GETGROUPSELA                ,"UM_GETGROUPSELA"                   , = 0x07eb)\
	//x(wm_SM_GETCURFOCUSW                ,"SM_GETCURFOCUSW"                   , = 0x07ec)\
	//x(wm_UM_GETGROUPSELW                ,"UM_GETGROUPSELW"                   , = 0x07ec)\
	//x(wm_SM_GETOPTIONS                  ,"SM_GETOPTIONS"                     , = 0x07ed)\
	//x(wm_UM_GETCURFOCUSA                ,"UM_GETCURFOCUSA"                   , = 0x07ed)\
	//x(wm_UM_GETCURFOCUSW                ,"UM_GETCURFOCUSW"                   , = 0x07ee)\
	//x(wm_UM_GETOPTIONS                  ,"UM_GETOPTIONS"                     , = 0x07ef)\
	//x(wm_UM_GETOPTIONS2                 ,"UM_GETOPTIONS2"                    , = 0x07f0)\
	//x(wm_OCMBASE                        ,"OCMBASE"                           , = 0x2000)\
	//x(wm_OCM_CTLCOLOR                   ,"OCM_CTLCOLOR"                      , = 0x2019)\
	//x(wm_OCM_DRAWITEM                   ,"OCM_DRAWITEM"                      , = 0x202b)\
	//x(wm_OCM_MEASUREITEM                ,"OCM_MEASUREITEM"                   , = 0x202c)\
	//x(wm_OCM_DELETEITEM                 ,"OCM_DELETEITEM"                    , = 0x202d)\
	//x(wm_OCM_VKEYTOITEM                 ,"OCM_VKEYTOITEM"                    , = 0x202e)\
	//x(wm_OCM_CHARTOITEM                 ,"OCM_CHARTOITEM"                    , = 0x202f)\
	//x(wm_OCM_COMPAREITEM                ,"OCM_COMPAREITEM"                   , = 0x2039)\
	//x(wm_OCM_NOTIFY                     ,"OCM_NOTIFY"                        , = 0x204e)\
	//x(wm_OCM_COMMAND                    ,"OCM_COMMAND"                       , = 0x2111)\
	//x(wm_OCM_HSCROLL                    ,"OCM_HSCROLL"                       , = 0x2114)\
	//x(wm_OCM_VSCROLL                    ,"OCM_VSCROLL"                       , = 0x2115)\
	//x(wm_OCM_CTLCOLORMSGBOX             ,"OCM_CTLCOLORMSGBOX"                , = 0x2132)\
	//x(wm_OCM_CTLCOLOREDIT               ,"OCM_CTLCOLOREDIT"                  , = 0x2133)\
	//x(wm_OCM_CTLCOLORLISTBOX            ,"OCM_CTLCOLORLISTBOX"               , = 0x2134)\
	//x(wm_OCM_CTLCOLORBTN                ,"OCM_CTLCOLORBTN"                   , = 0x2135)\
	//x(wm_OCM_CTLCOLORDLG                ,"OCM_CTLCOLORDLG"                   , = 0x2136)\
	//x(wm_OCM_CTLCOLORSCROLLBAR          ,"OCM_CTLCOLORSCROLLBAR"             , = 0x2137)\
	//x(wm_OCM_CTLCOLORSTATIC             ,"OCM_CTLCOLORSTATIC"                , = 0x2138)\
	//x(wm_OCM_PARENTNOTIFY               ,"OCM_PARENTNOTIFY"                  , = 0x2210)\
	//x(wm_WM_APP                         ,"WM_APP"                            , = 0x8000)\
	//x(wm_WM_RASDIALEVENT                ,"WM_RASDIALEVENT"                   , = 0xcccd)

	PR_DEFINE_ENUM3(EWinMsg, PR_ENUM);
	#undef PR_ENUM
	#pragma endregion
	static_assert(is_reflected_enum_v<EWinMsg>, "");

	#pragma region SysCommands
	#define PR_ENUM(x)\
	x(sc_CLOSE        , "SC_CLOSE"        , = SC_CLOSE        )\
	x(sc_CONTEXTHELP  , "SC_CONTEXTHELP"  , = SC_CONTEXTHELP  )\
	x(sc_DEFAULT      , "SC_DEFAULT"      , = SC_DEFAULT      )\
	x(sc_HOTKEY       , "SC_HOTKEY"       , = SC_HOTKEY       )\
	x(sc_HSCROLL      , "SC_HSCROLL"      , = SC_HSCROLL      )\
	x(scF_ISSECURE    , "SCF_ISSECURE"    , = SCF_ISSECURE    )\
	x(sc_KEYMENU      , "SC_KEYMENU"      , = SC_KEYMENU      )\
	x(sc_MAXIMIZE     , "SC_MAXIMIZE"     , = SC_MAXIMIZE     )\
	x(sc_MINIMIZE     , "SC_MINIMIZE"     , = SC_MINIMIZE     )\
	x(sc_MONITORPOWER , "SC_MONITORPOWER" , = SC_MONITORPOWER )\
	x(sc_MOUSEMENU    , "SC_MOUSEMENU"    , = SC_MOUSEMENU    )\
	x(sc_MOVE         , "SC_MOVE"         , = SC_MOVE         )\
	x(sc_NEXTWINDOW   , "SC_NEXTWINDOW"   , = SC_NEXTWINDOW   )\
	x(sc_PREVWINDOW   , "SC_PREVWINDOW"   , = SC_PREVWINDOW   )\
	x(sc_RESTORE      , "SC_RESTORE"      , = SC_RESTORE      )\
	x(sc_SCREENSAVE   , "SC_SCREENSAVE"   , = SC_SCREENSAVE   )\
	x(sc_SIZE         , "SC_SIZE"         , = SC_SIZE         )\
	x(sc_TASKLIST     , "SC_TASKLIST"     , = SC_TASKLIST     )\
	x(sc_VSCROLL      , "SC_VSCROLL"      , = SC_VSCROLL      )

	PR_DEFINE_ENUM3(ESysCmd, PR_ENUM);
	#undef PR_ENUM
	#pragma endregion

	// Convert a windows message to a string
	inline char const* WMtoString(UINT uMsg)
	{
		return Enum<EWinMsg>::IsValue(uMsg) ? Enum<EWinMsg>::ToStringA(uMsg) : "";
	}

	// Return the Window Text for a window
	inline char const* WndText(HWND hwnd)
	{
		static char wndtext[64] = {};
		wndtext[::DefWindowProcA(hwnd, WM_GETTEXT, _countof(wndtext) - 1, LPARAM(&wndtext[0]))] = 0;
		if (wndtext[0] == 0) sprintf(wndtext, "%p", hwnd);
		return wndtext;
	};

	// Convert a VK_* define into a string
	inline char const* VKtoString(int vk)
	{
		switch (vk)
		{
		default:
			{
				static char str[2] = {'\0','\0'};
				if ((vk >= '0' && vk <= '9') || (vk >= 'A' && vk <= 'Z'))
					str[0] = (char)vk;
				return str;
			}
		case VK_LBUTTON: return "VK_LBUTTON";
		case VK_RBUTTON: return "VK_RBUTTON";
		case VK_CANCEL : return "VK_CANCEL";
		case VK_MBUTTON: return "VK_MBUTTON";

		#if(_WIN32_WINNT >= 0x0500)
		case VK_XBUTTON1: return "VK_XBUTTON1";
		case VK_XBUTTON2: return "VK_XBUTTON2";
		#endif /* _WIN32_WINNT >= 0x0500 */

		case VK_BACK: return "VK_BACK";
		case VK_TAB:  return "VK_TAB";
		case VK_CLEAR: return "VK_CLEAR";
		case VK_RETURN: return "VK_RETURN";
		case VK_SHIFT: return "VK_SHIFT";
		case VK_CONTROL: return "VK_CONTROL";
		case VK_MENU: return "VK_MENU";
		case VK_PAUSE: return "VK_PAUSE";
		case VK_CAPITAL: return "VK_CAPITAL";
		case VK_KANA: return "VK_KANA";
		case VK_JUNJA: return "VK_JUNJA";
		case VK_FINAL: return "VK_FINAL";
		case VK_HANJA: return "VK_HANJA";
		case VK_ESCAPE: return "VK_ESCAPE";
		case VK_CONVERT: return "VK_CONVERT";
		case VK_NONCONVERT: return "VK_NONCONVERT";
		case VK_ACCEPT: return "VK_ACCEPT";
		case VK_MODECHANGE: return "VK_MODECHANGE";
		case VK_SPACE: return "VK_SPACE";
		case VK_PRIOR: return "VK_PRIOR";
		case VK_NEXT: return "VK_NEXT";
		case VK_END: return "VK_END";
		case VK_HOME: return "VK_HOME";
		case VK_LEFT: return "VK_LEFT";
		case VK_UP: return "VK_UP";
		case VK_RIGHT: return "VK_RIGHT";
		case VK_DOWN: return "VK_DOWN";
		case VK_SELECT: return "VK_SELECT";
		case VK_PRINT: return "VK_PRINT";
		case VK_EXECUTE: return "VK_EXECUTE";
		case VK_SNAPSHOT: return "VK_SNAPSHOT";
		case VK_INSERT: return "VK_INSERT";
		case VK_DELETE: return "VK_DELETE";
		case VK_HELP: return "VK_HELP";
		case VK_LWIN: return "VK_LWIN";
		case VK_RWIN: return "VK_RWIN";
		case VK_APPS: return "VK_APPS";
		case VK_SLEEP: return "VK_SLEEP";
		case VK_NUMPAD0: return "VK_NUMPAD0";
		case VK_NUMPAD1: return "VK_NUMPAD1";
		case VK_NUMPAD2: return "VK_NUMPAD2";
		case VK_NUMPAD3: return "VK_NUMPAD3";
		case VK_NUMPAD4: return "VK_NUMPAD4";
		case VK_NUMPAD5: return "VK_NUMPAD5";
		case VK_NUMPAD6: return "VK_NUMPAD6";
		case VK_NUMPAD7: return "VK_NUMPAD7";
		case VK_NUMPAD8: return "VK_NUMPAD8";
		case VK_NUMPAD9: return "VK_NUMPAD9";
		case VK_MULTIPLY: return "VK_MULTIPLY";
		case VK_ADD: return "VK_ADD";
		case VK_SEPARATOR: return "VK_SEPARATOR";
		case VK_SUBTRACT: return "VK_SUBTRACT";
		case VK_DECIMAL: return "VK_DECIMAL";
		case VK_DIVIDE: return "VK_DIVIDE";
		case VK_F1: return "VK_F1";
		case VK_F2: return "VK_F2";
		case VK_F3: return "VK_F3";
		case VK_F4: return "VK_F4";
		case VK_F5: return "VK_F5";
		case VK_F6: return "VK_F6";
		case VK_F7: return "VK_F7";
		case VK_F8: return "VK_F8";
		case VK_F9: return "VK_F9";
		case VK_F10: return "VK_F10";
		case VK_F11: return "VK_F11";
		case VK_F12: return "VK_F12";
		case VK_F13: return "VK_F13";
		case VK_F14: return "VK_F14";
		case VK_F15: return "VK_F15";
		case VK_F16: return "VK_F16";
		case VK_F17: return "VK_F17";
		case VK_F18: return "VK_F18";
		case VK_F19: return "VK_F19";
		case VK_F20: return "VK_F20";
		case VK_F21: return "VK_F21";
		case VK_F22: return "VK_F22";
		case VK_F23: return "VK_F23";
		case VK_F24: return "VK_F24";
		case VK_NUMLOCK: return "VK_NUMLOCK";
		case VK_SCROLL: return "VK_SCROLL";
		case VK_OEM_NEC_EQUAL: return "VK_OEM_NEC_EQUAL";
		case VK_OEM_FJ_MASSHOU: return "VK_OEM_FJ_MASSHOU";
		case VK_OEM_FJ_TOUROKU: return "VK_OEM_FJ_TOUROKU";
		case VK_OEM_FJ_LOYA: return "VK_OEM_FJ_LOYA";
		case VK_OEM_FJ_ROYA: return "VK_OEM_FJ_ROYA";
		case VK_LSHIFT: return "VK_LSHIFT";
		case VK_RSHIFT: return "VK_RSHIFT";
		case VK_LCONTROL: return "VK_LCONTROL";
		case VK_RCONTROL: return "VK_RCONTROL";
		case VK_LMENU: return "VK_LMENU";
		case VK_RMENU: return "VK_RMENU";

		#if(_WIN32_WINNT >= 0x0500)
		case VK_BROWSER_BACK: return "VK_BROWSER_BACK";
		case VK_BROWSER_FORWARD: return "VK_BROWSER_FORWARD";
		case VK_BROWSER_REFRESH: return "VK_BROWSER_REFRESH";
		case VK_BROWSER_STOP: return "VK_BROWSER_STOP";
		case VK_BROWSER_SEARCH: return "VK_BROWSER_SEARCH";
		case VK_BROWSER_FAVORITES: return "VK_BROWSER_FAVORITES";
		case VK_BROWSER_HOME: return "VK_BROWSER_HOME";
		case VK_VOLUME_MUTE: return "VK_VOLUME_MUTE";
		case VK_VOLUME_DOWN: return "VK_VOLUME_DOWN";
		case VK_VOLUME_UP: return "VK_VOLUME_UP";
		case VK_MEDIA_NEXT_TRACK: return "VK_MEDIA_NEXT_TRACK";
		case VK_MEDIA_PREV_TRACK: return "VK_MEDIA_PREV_TRACK";
		case VK_MEDIA_STOP: return "VK_MEDIA_STOP";
		case VK_MEDIA_PLAY_PAUSE: return "VK_MEDIA_PLAY_PAUSE";
		case VK_LAUNCH_MAIL: return "VK_LAUNCH_MAIL";
		case VK_LAUNCH_MEDIA_SELECT: return "VK_LAUNCH_MEDIA_SELECT";
		case VK_LAUNCH_APP1: return "VK_LAUNCH_APP1";
		case VK_LAUNCH_APP2: return "VK_LAUNCH_APP2";
		#endif /* _WIN32_WINNT >= 0x0500 */

		case VK_OEM_1: return "VK_OEM_1";
		case VK_OEM_PLUS: return "VK_OEM_PLUS";
		case VK_OEM_COMMA: return "VK_OEM_COMMA";
		case VK_OEM_MINUS: return "VK_OEM_MINUS";
		case VK_OEM_PERIOD: return "VK_OEM_PERIOD";
		case VK_OEM_2: return "VK_OEM_2";
		case VK_OEM_3: return "VK_OEM_3";
		case VK_OEM_4: return "VK_OEM_4";
		case VK_OEM_5: return "VK_OEM_5";
		case VK_OEM_6: return "VK_OEM_6";
		case VK_OEM_7: return "VK_OEM_7";
		case VK_OEM_8: return "VK_OEM_8";
		case VK_OEM_AX: return "VK_OEM_AX";
		case VK_OEM_102: return "VK_OEM_102";
		case VK_ICO_HELP: return "VK_ICO_HELP";
		case VK_ICO_00: return "VK_ICO_00";

		#if(WINVER >= 0x0400)
		case VK_PROCESSKEY: return "VK_PROCESSKEY";
		#endif /* WINVER >= 0x0400 */

		case VK_ICO_CLEAR: return "VK_ICO_CLEAR";

		#if(_WIN32_WINNT >= 0x0500)
		case VK_PACKET: return "VK_PACKET";
		#endif /* _WIN32_WINNT >= 0x0500 */

		case VK_OEM_RESET: return "VK_OEM_RESET";
		case VK_OEM_JUMP: return "VK_OEM_JUMP";
		case VK_OEM_PA1: return "VK_OEM_PA1";
		case VK_OEM_PA2: return "VK_OEM_PA2";
		case VK_OEM_PA3: return "VK_OEM_PA3";
		case VK_OEM_WSCTRL: return "VK_OEM_WSCTRL";
		case VK_OEM_CUSEL: return "VK_OEM_CUSEL";
		case VK_OEM_ATTN: return "VK_OEM_ATTN";
		case VK_OEM_FINISH: return "VK_OEM_FINISH";
		case VK_OEM_COPY: return "VK_OEM_COPY";
		case VK_OEM_AUTO: return "VK_OEM_AUTO";
		case VK_OEM_ENLW: return "VK_OEM_ENLW";
		case VK_OEM_BACKTAB: return "VK_OEM_BACKTAB";
		case VK_ATTN: return "VK_ATTN";
		case VK_CRSEL: return "VK_CRSEL";
		case VK_EXSEL: return "VK_EXSEL";
		case VK_EREOF: return "VK_EREOF";
		case VK_PLAY: return "VK_PLAY";
		case VK_ZOOM: return "VK_ZOOM";
		case VK_NONAME: return "VK_NONAME";
		case VK_PA1: return "VK_PA1";
		case VK_OEM_CLEAR: return "VK_OEM_CLEAR";
		}
	}

	// Output info about a windows message
	inline char const* DebugMessage(HWND hwnd, UINT uMsg, WPARAM wparam, LPARAM lparam, char const* newline = "")
	{
		INT wParamLo = LOWORD(wparam);
		INT wParamHi = HIWORD(wparam);
		INT lParamLo = LOWORD(lparam);
		INT lParamHi = HIWORD(lparam);

		auto msg = WMtoString(uMsg);
		auto hdr = pr::FmtX<struct a, 128, char>("%s(0x%04x):", msg, uMsg);
		auto wnd = pr::FmtX<struct b, 128, char>(" hwnd=(%s)", WndText(hwnd));
		auto fmt = pr::FmtX<struct X, 1024, char>;

		// Note: there is a C# version of this, copy to/from there is possible
		switch (uMsg)
		{
			default:
			{
				return fmt("%s %s wparam: %x(%x,%x)  lparam: %x(%x,%x)%s"
					, hdr, wnd
					, wparam, wParamHi, wParamLo
					, lparam, lParamHi, lParamLo
					, newline);
			}
			case WM_LBUTTONDOWN:
			{
				return fmt("%s button state = %s%s%s%s%s%s%s  x,y=(%d,%d)%s"
					, hdr
					, (wparam & MK_CONTROL ? "|Ctrl" : "")
					, (wparam & MK_LBUTTON ? "|LBtn" : "")
					, (wparam & MK_MBUTTON ? "|MBtn" : "")
					, (wparam & MK_RBUTTON ? "|RBtn" : "")
					, (wparam & MK_SHIFT ? "|Shift" : "")
					, (wparam & MK_XBUTTON1 ? "|XBtn1" : "")
					, (wparam & MK_XBUTTON2 ? "|XBtn2" : "")
					, lParamLo, lParamHi
					, newline);
			}
			case WM_ACTIVATEAPP:
			{
				return fmt("%s %s Other Thread: %p%s"
					, hdr
					, (wparam ? "ACTIVE" : "INACTIVE")
					, lparam
					, newline);
			}
			case WM_ACTIVATE:
			{
				return fmt("%s %s Other Window=(%s)%s"
					, hdr
					, (LOWORD(wparam) == WA_ACTIVE ? "ACTIVE" : LOWORD(wparam) == WA_INACTIVE ? "INACTIVE" : "Click ACTIVE")
					, WndText((HWND)lparam)
					, newline);
			}
			case WM_NCACTIVATE:
			{
				return fmt("%s %s lparam:%x(%x,%x)%s"
					, hdr
					, (wparam ? "ACTIVE" : "INACTIVE")
					, lparam, lParamHi, lParamLo
					, newline);
			}
			case WM_MOUSEACTIVATE:
			{
				return fmt("%s top-level parent window=(%s)  lparam: %x(%x,%x)%s"
					, hdr
					, WndText((HWND)wparam)
					, lparam, lParamHi, lParamLo
					, newline);
			}
			case WM_SHOWWINDOW:
			{
				return fmt("%s %s %s%s"
					, hdr
					, (wparam ? "VISIBLE" : "HIDDEN")
					, (lparam == SW_OTHERUNZOOM ? "OtherUnzoom" :
						lparam == SW_PARENTCLOSING ? "ParentClosing" :
						lparam == SW_OTHERZOOM ? "OtherZoom" :
						lparam == SW_PARENTOPENING ? "ParentOpening" :
						"ShowWindow called")
					, newline);
			}
			case WM_WINDOWPOSCHANGING:
			case WM_WINDOWPOSCHANGED:
			{
				auto& wp = *reinterpret_cast<WINDOWPOS*>(lparam);
				return fmt("%s x,y=(%d,%d) size=(%d,%d) after=(%s) flags=%s%s%s%s%s%s%s%s%s%s%s%s%s%s"
					, hdr
					, wp.x, wp.y
					, wp.cx, wp.cy
					, WndText(wp.hwndInsertAfter)
					, (wp.flags & SWP_DRAWFRAME ? "|SWP_DRAWFRAME" : "")
					, (wp.flags & SWP_FRAMECHANGED ? "|SWP_FRAMECHANGED" : "")
					, (wp.flags & SWP_HIDEWINDOW ? "|SWP_HIDEWINDOW" : "")
					, (wp.flags & SWP_NOACTIVATE ? "|SWP_NOACTIVATE" : "")
					, (wp.flags & SWP_NOCOPYBITS ? "|SWP_NOCOPYBITS" : "")
					, (wp.flags & SWP_NOMOVE ? "|SWP_NOMOVE" : "")
					, (wp.flags & SWP_NOOWNERZORDER ? "|SWP_NOOWNERZORDER" : "")
					, (wp.flags & SWP_NOREDRAW ? "|SWP_NOREDRAW" : "")
					, (wp.flags & SWP_NOREPOSITION ? "|SWP_NOREPOSITION" : "")
					, (wp.flags & SWP_NOSENDCHANGING ? "|SWP_NOSENDCHANGING" : "")
					, (wp.flags & SWP_NOSIZE ? "|SWP_NOSIZE" : "")
					, (wp.flags & SWP_NOZORDER ? "|SWP_NOZORDER" : "")
					, (wp.flags & SWP_SHOWWINDOW ? "|SWP_SHOWWINDOW" : "")
					, newline);
			}
			case WM_GETMINMAXINFO:
			{
				auto mm = *reinterpret_cast<MINMAXINFO*>(lparam);
				return fmt("%s max size=(%d,%d)  max pos=(%d,%d)  min track=(%d,%d)  max track=(%d,%d)%s"
					, hdr
					, mm.ptMaxSize.x, mm.ptMaxSize.y
					, mm.ptMaxPosition.x, mm.ptMaxPosition.y
					, mm.ptMinTrackSize.x, mm.ptMinTrackSize.y
					, mm.ptMaxTrackSize.x, mm.ptMaxTrackSize.y
					, newline);
			}
			case WM_KILLFOCUS:
			{
				return fmt("%s Focused Window=(%s)%s"
					, hdr
					, WndText((HWND)wparam)
					, newline);
			}
			case WM_CAPTURECHANGED:
			{
				return fmt("%s new owner=(%s)%s"
					, hdr
					, WndText((HWND)lparam)
					, newline);
			}
			case WM_NOTIFY:
			{
				char const* notify_type = "unknown";
				NMHDR* nmhdr = (NMHDR*)lparam;
				if (NM_LAST <= nmhdr->code) notify_type = "NM";
				else if (LVN_LAST <= nmhdr->code) notify_type = "LVN";
				else if (HDN_LAST <= nmhdr->code) notify_type = "HDN";
				else if (TVN_LAST <= nmhdr->code) notify_type = "TVN";
				else if (TTN_LAST <= nmhdr->code) notify_type = "TTN";
				else if (TCN_LAST <= nmhdr->code) notify_type = "TCN";
				else if (CDN_LAST <= nmhdr->code) notify_type = "CDN";
				else if (TBN_LAST <= nmhdr->code) notify_type = "TBN";
				else if (UDN_LAST <= nmhdr->code) notify_type = "UDN";
#if (_WIN32_IE >= 0x0300)
				else if (DTN_LAST <= nmhdr->code) notify_type = "DTN";
				else if (MCN_LAST <= nmhdr->code) notify_type = "MCN";
				else if (DTN_LAST2 <= nmhdr->code) notify_type = "DTN";
				else if (CBEN_LAST <= nmhdr->code) notify_type = "CBEN";
				else if (RBN_LAST <= nmhdr->code) notify_type = "RBN";
#endif
#if (_WIN32_IE >= 0x0400)
				else if (IPN_LAST <= nmhdr->code) notify_type = "IPN";
				else if (SBN_LAST <= nmhdr->code) notify_type = "SBN";
				else if (PGN_LAST <= nmhdr->code) notify_type = "PGN";
#endif
#if (_WIN32_IE >= 0x0500)
#ifndef WMN_FIRST
				else if (WMN_LAST <= nmhdr->code) notify_type = "WMN";
#endif
#endif
#if (_WIN32_WINNT >= 0x0501)
				else if (BCN_LAST <= nmhdr->code) notify_type = "BCN";
#endif
#if (_WIN32_WINNT >= 0x0600)
				else if (TRBN_LAST <= nmhdr->code) notify_type = "TRBN";
#endif

				// Ignore
				if (nmhdr->code == LVN_HOTTRACK)
					return "";

				return fmt("%s SourceCtrlId=(%d)  from_hWnd=(%s)  from_id=(%d)  code=(%d:%s)%s"
					, hdr
					, wparam
					, WndText(nmhdr->hwndFrom)
					, nmhdr->idFrom
					, nmhdr->code
					, notify_type
					, newline);
			}
			case WM_SYSKEYDOWN:
			{
				return fmt("%s vk_key=(%d:%s)  Repeats=(%d)  lparam: %d%s"
					, hdr
					, wparam, VKtoString((int)wparam)
					, lParamLo
					, lparam
					, newline);
			}
			case WM_SYSCOMMAND:
			{
				return fmt("%s cmd=(%s) pos=(%d,%d)%s"
					, hdr
					, Enum<ESysCmd>::ToStringA(wparam & 0xFFF0)
					, lParamLo, lParamHi
					, newline);
			}
			case WM_PAINT:
			{
				RECT r;
				::GetUpdateRect(hwnd, &r, FALSE);
				return fmt("%s update=(%d,%d) size=(%d,%d)  HDC: %p%s"
					, hdr
					, r.left, r.top, r.right - r.left, r.bottom - r.top
					, wparam
					, newline);
			}
			case WM_IME_REQUEST:
			{
				return fmt("%s IME Request %d  lParam: %d"
					, hdr
					, wparam //{ (EIME_Request)wparam }
					, lparam 
					);
			}
			case WM_IME_NOTIFY:
			{
				return fmt("%s IME Notify %d  lParam: %d"
					, hdr
					, wparam//{ (EIME_Notification)wparam }
					, lparam
					);
			}
			case WM_IME_SETCONTEXT:
			{
				auto active = (int)wparam != 0 ? "Active" : "Inactive";
				return fmt("%s IME SetContext  Window: %s Options: %d"
					, hdr
					, active
					, lparam // { (EIME_SetContextFlags)lparam }
					);
			}
			case WM_IME_STARTCOMPOSITION:
			{
				return fmt("%s IME Start Composition"
					, hdr
				);
			}
			case WM_ENTERIDLE:
			case WM_NCHITTEST:
			case WM_SETCURSOR:
			case WM_NCMOUSEMOVE:
			case WM_NCMOUSELEAVE:
			case WM_MOUSEMOVE:
			case WM_GETICON:
			case (UINT)EWinMsg::wm_UAHDRAWMENUITEM:
			case (UINT)EWinMsg::wm_UAHDRAWMENU:
			case (UINT)EWinMsg::wm_UAHINITMENU:
			case (UINT)EWinMsg::wm_DWMCOLORIZATIONCOLORCHANGED:
			case (UINT)EWinMsg::wm_UAHMEASUREMENUITEM:
				//case WM_CTLCOLORDLG:
				//case WM_AFXLAST:
				//case WM_ENTERIDLE:
				//case WM_ERASEBKGND:
				//case WM_PAINT:
			case WM_NULL:
				return "";//ignore
		}
	}

	// Display a text description of a windows message if it passes 'pred'
	template <typename Pred> inline char const* DebugMessage(Pred pred, HWND hwnd, UINT msg, WPARAM wparam, LPARAM lparam, char const* newline = "\r\n")
	{
		if (!pred(hwnd, msg, wparam, lparam)) return "";
		return DebugMessage(hwnd, msg, wparam, lparam, newline);
	}

	// Displays a text description of a windows message. Use this in PreTranslateMessage()
	template <typename Pred> inline char const* DebugMessage(MSG const* msg, Pred pred)
	{
		static bool enable_dbg_mm = true;
		if (!enable_dbg_mm)
			return "";

		static UINT break_on_message = 0;
		if (break_on_message == msg->message)
			_CrtDbgBreak();

		if (!pred(msg->message)) return "";
		return DebugMessage(msg->hwnd, msg->message, msg->wParam, msg->lParam);
			
	}
	inline char const* DebugMessage(MSG const* msg)
	{
		return DebugMessage(msg, [](int){ return true; });
	}
}
