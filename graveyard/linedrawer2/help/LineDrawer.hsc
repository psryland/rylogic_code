HelpScribble project file.
13
...
0
1
LineDrawer Help


Copyright © 2004 - Paul Ryland
FALSE

P:\LINEDR~1\hlp
1
BrowseButtons()
0
TRUE

FALSE
TRUE
16777215
0
16711680
8388736
255
FALSE
FALSE
FALSE
1
FALSE
FALSE
Contents
%s Contents
Index
%s Index
Previous
Next
FALSE

81
10
Scribble10
Overview
Overview;


mainbrowsesequence:000010
Writing



FALSE
13
{\rtf1\ansi\ansicpg1252\deff0{\fonttbl{\f0\fnil\fcharset0 Arial;}{\f1\fnil Arial;}}
{\colortbl ;\red0\green0\blue255;\red0\green0\blue0;\red0\green128\blue0;\red128\green0\blue0;}
\viewkind4\uc1\pard\cf1\lang2057\b\fs32 Overview\lang1033\f1 
\par \pard\li720\b0\fs20 
\par \cf2\lang2057\f0 LineDrawer\cf0\lang1033\'99\cf2\lang2057  is a tool for visualising objects in 3D. Objects are described using a text-based scripting language that LineDrawer\cf0\lang1033\'99\cf2\lang2057  parses. \cf3\strike LineDrawer\lang1033\'99\fs26  \lang2057\fs20 script\cf4\strike0\{linkID=15\}\cf2  can be read from text files, entered directly into the UI, or streamed via a TCP connection.
\par 
\par LineDrawer\cf0\lang1033\'99\fs26  \fs20 provides two styles of navigation; \cf3\strike "trackball" mouse control\cf4\strike0\{linkID=740\}\cf0  used for viewing objects, and \cf3\strike "inertial camera" keyboard control\cf4\strike0\{linkID=750\}\cf0  useful for "flying" through a scene.
\par 
\par \cf2\lang2057 LineDrawer\cf0\lang1033\'99 allows the objects to be switched between solid, wire frame, visible and hidden via the \cf3\strike data window\cf4\strike0\{linkID=760\}\cf0 . This window also displays the hierarchy of nested objects and their properties.
\par 
\par Finally, \cf2\lang2057 LineDrawer\cf0\lang1033\'99 provides various rendering options including \cf3\strike scene lighting\cf4\strike0\{linkID=880\}\cf0 , perspective and orthographic projection, and the ultra cool \cf3\strike stereo view\cf4\strike0\{linkID=870\}\cf0  option.
\par 
\par }
15
Scribble15
LineDrawer Script
LineDrawer script;Script;


mainbrowsesequence:000020
Writing



FALSE
16
{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 Arial;}{\f1\fnil Arial;}{\f2\fnil\fprq1\fcharset0 Courier New;}}
{\colortbl ;\red0\green0\blue255;\red0\green128\blue0;\red128\green0\blue0;}
\viewkind4\uc1\pard\cf1\lang2057\b\fs32 LineDrawer\lang1033\'99\lang2057  Script\cf0\lang1033\b0\f1\fs20 
\par 
\par \pard\li720\f0 LineDrawer\'99 script is a text based language used to describe objects viewed in LineDrawer\'99.
\par \pard 
\par \lang2057\b Comments:
\par \pard\li720\b0 The ' # ' character indicates the start of a comment. All text on a line after the ' # ' character is considered comment text and is ignored by the parser.
\par \pard 
\par \b Delimiting Characters:\b0 
\par \pard\li720 The following characters are considered equivalent to white space characters: \f2 ' ', ',', and ';'.
\par \f0 Data within an \cf2\strike object type\cf3\strike0\{linkID=20\}\cf0  may be separated by any of these characters. The "Name" parameter of an object must not contain these characters.
\par 
\par \pard\b Object Type Definitions:
\par \pard\li720\b0 See \cf2\strike here\cf3\strike0\{linkID=20\}\cf0 .\lang1033\f1 
\par }
20
Scribble20
Objects
Data;Objects;


mainbrowsesequence:000030
Writing



FALSE
67
{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 Arial;}{\f1\fnil Arial;}}
{\colortbl ;\red0\green0\blue255;\red0\green128\blue0;\red128\green0\blue0;}
\viewkind4\uc1\pard\cf1\lang2057\b\fs32 Objects\cf0\lang1033\b0\f1\fs20 
\par 
\par \pard\li700\lang2057\f0 All objects in \cf2\strike LineDrawer\lang1033\'99 s\lang2057 cript\cf3\strike0\{linkID=15\}\cf0  have this format:
\par \pard 
\par \pard\li680 <\cf2\strike Object Type\cf3\strike0\{linkID=25\}\cf0 > <Name> <Colour>
\par \{
\par \pard\li1400 <Object Parameters>
\par <\cf2\strike Optional Object Modifiers\cf3\strike0\{linkID=175\}\cf0 >
\par <Optional nested Object Types>
\par \pard\fi-20\li680\}
\par \pard 
\par \b Description:\b0 
\par \i 
\par \pard\li720 Object Types:\i0 
\par \pard\li1400 A complete list of all object types is given \cf2\strike here\cf3\strike0\{linkID=25\}\cf0 .
\par \pard 
\par \pard\li680\i Name:
\par \pard\li1400\i0 This is an alpha-numeric string without white space.Valid characters are:
\par 'A' - 'Z', 'a' - 'z', '0' - '9', and '_'.
\par \pard 
\par \pard\li700\i Colour:
\par \pard\li1380\i0 This is an 8 digit hexadecimal number of the format:
\par AARRGGBB. A = Alpha, R = red, G = green, B = blue
\par \pard\li1400 e.g.
\par \pard\li2120 Opaque red:\tab\tab\tab FFFF0000
\par Semi transparent green:\tab\tab 8000FF00
\par \pard\i 
\par \pard\li700 Object Parameters:
\par \pard\li1380\i0 These depend on the object type.
\par \pard 
\par \pard\li700\i Optional Object Modifiers:
\par \pard\li1380\i0 A description of the object modifiers is given \cf2\strike here\cf3\strike0\{linkID=175\}\cf0 
\par \pard\i 
\par \pard\li700 Optional nested Object Types:
\par \pard\li1380\i0 All objects can be nested within other objects. Nested objects are relative to the containing object.
\par \pard 
\par \b Remarks:
\par \tab 
\par \pard\li700\b0 All parameters must be separated by white space.
\par Carriage returns are not required.
\par \b 
\par \pard Example:
\par 
\par \pard\li680\b0 BoxWHD MyRedBox FFFF0000
\par \{
\par \pard\li1400 1 1 1
\par \pard\li680\}
\par 
\par or
\par 
\par SphereR MyTransparentBlueSphere 800000FF    \{  1  \}
\par 
\par or
\par 
\par CylinderHR MyGreenCylinder FF00FF00
\par \{
\par \pard\li1400 1 0.5
\par BoxWHD MyNestedBox FFFF00FF
\par \{
\par \pard\li2120 0.5 0.5 0.5
\par \pard\li1400\}
\par \pard\li680\}
\par \pard 
\par \i 
\par }
25
Scribble25
Object Types
Data;Object Types;Types;


mainbrowsesequence:000040
Writing



FALSE
34
{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 Arial;}{\f1\fnil Arial;}{\f2\fnil\fprq1\fcharset0 Courier New;}}
{\colortbl ;\red0\green0\blue255;\red0\green128\blue0;\red128\green0\blue0;}
\viewkind4\uc1\pard\cf1\lang2057\b\fs32 Object Types\cf0\lang1033\b0\f1\fs20 
\par 
\par \pard\li700\lang2057\f0 These are the object types supported by LineDrawer:\lang1033\f1 
\par \pard\lang2057\f0 
\par \pard\li1400\cf2\strike\f2 Point\cf3\strike0\{linkID=30\}\cf0 
\par \cf2\strike Line\cf3\strike0\{linkID=40\}\cf0 
\par \cf2\strike LineD\cf3\strike0\{linkID=40\}\cf0 
\par \cf2\strike LineNL\cf3\strike0\{linkID=40\}\cf0 
\par \cf2\strike RectangleLU\cf3\strike0\{linkID=50\}\cf0 
\par \cf2\strike RectangleWHZ\cf3\strike0\{linkID=50\}\cf0 
\par \cf2\strike Triangle\cf3\strike0\{linkID=60\}\cf0 
\par \cf2\strike Quad\cf3\strike0\{linkID=70\}\cf0 
\par \cf2\strike QuadLU\cf3\strike0\{linkID=70\}\cf0 
\par \cf2\strike QuadWHZ\cf3\strike0\{linkID=70\}\cf0 
\par \cf2\strike CircleR\cf3\strike0\{linkID=80\}\cf0 
\par \cf2\strike CircleRxRyZ\cf3\strike0\{linkID=80\}\cf0 
\par \cf2\strike BoxLU\cf3\strike0\{linkID=90\}\cf0 
\par \cf2\strike BoxWHD\cf3\strike0\{linkID=90\}\cf0 
\par \cf2\strike CylinderHR\cf3\strike0\{linkID=100\}\cf0 
\par \cf2\strike CylinderHRxRy\cf3\strike0\{linkID=100\}\cf0 
\par \cf2\strike SphereR\cf3\strike0\{linkID=110\}\cf0 
\par \cf2\strike SphereRxRyRz\cf3\strike0\{linkID=110\}\cf0 
\par \cf2\strike FrustumWHNF\cf3\strike0\{linkID=120\}\cf0\tab\tab\tab 
\par \cf2\strike FrustumATNF\cf3\strike0\{linkID=120\}\cf0 
\par \cf2\strike GridWHD\cf3\strike0\{linkID=130\}\cf0 
\par \cf2\strike Mesh\cf3\strike0\{linkID=140\}\cf0 
\par \cf2\strike XFile\cf3\strike0\{linkID=150\}\cf0 
\par \cf2\strike Group\cf3\strike0\{linkID=160\}\cf0 
\par \cf2\strike GroupCyclic\cf3\strike0\{linkID=160\}\cf0 
\par \pard\lang1033\f1 
\par \pard\li700\lang2057\b\f0 Note:\b0  Some of these objects are \cf2\strike list-able\cf3\strike0\{linkID=27\}\cf0 .\lang1033\f1 
\par }
27
Scribble27
List-able Object Types
List-able Object Types;Multiple Objects;


mainbrowsesequence:000050
Writing



FALSE
28
{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 Arial;}{\f1\fnil Arial;}{\f2\fnil\fprq1\fcharset0 Courier New;}}
{\colortbl ;\red0\green0\blue255;\red0\green0\blue0;\red0\green128\blue0;\red128\green0\blue0;}
\viewkind4\uc1\pard\cf1\lang2057\b\fs32 List-able Object Types\cf0\lang1033\b0\f1\fs20 
\par 
\par \pard\li720\lang2057\f0 List-able objects are useful when you have thousands of objects of one type to display (e.g. thousands of data points). \cf2 LineDrawer\cf0\lang1033\'99\lang2057 's performance depends on the number of objects it is trying to draw. Listing parameters within an \cf3\strike object type\cf4\strike0\{linkID=20\}\cf0  creates one object from all of the data. However, doing this means that individual control over each datum is lost.
\par \pard 
\par \pard\li700 e.g.
\par \pard\li1380 1000 point objects containing 1 point :- Slower performance, each point can be individually manipulated
\par 1 point object containing 1000 points :- Faster performance, no individual control over each point
\par \pard\lang1033\f1 
\par \pard\li720\lang2057\f0 The following \cf3\strike object types\cf4\strike0\{linkID=20\}\cf0  support lists of parameters:
\par \pard 
\par \pard\li1400\cf3\strike\f2 Point\cf4\strike0\{linkID=30\}\cf0 
\par \cf3\strike Line\cf4\strike0\{linkID=40\}\cf0 
\par \cf3\strike LineD\cf4\strike0\{linkID=40\}\cf0 
\par \cf3\strike LineNL\cf4\strike0\{linkID=40\}\cf0 
\par \cf3\strike RectangleLU\cf4\strike0\{linkID=50\}\cf0 
\par \cf3\strike RectangleWHZ\cf4\strike0\{linkID=50\}\cf0 
\par \cf3\strike Triangle\cf4\strike0\{linkID=60\}\cf0 
\par \cf3\strike Quad\cf4\strike0\{linkID=70\}\cf0 
\par \cf3\strike QuadLU\cf4\strike0\{linkID=70\}\cf0 
\par \cf3\strike QuadWHZ\cf4\strike0\{linkID=70\}\cf0 
\par \cf3\strike CircleR\cf4\strike0\{linkID=80\}\cf0 
\par \cf3\strike CircleRxRyZ\cf4\strike0\{linkID=80\}\cf0 
\par \cf3\strike BoxLU\cf4\strike0\{linkID=90\}\cf0 
\par \cf3\strike BoxWHD\cf4\strike0\{linkID=90\}\cf0 
\par \pard 
\par }
30
Scribble30
Point
Point;


objecttypesequence:000010
Writing



FALSE
31
{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 Arial;}{\f1\fnil Arial;}{\f2\fnil\fprq1\fcharset0 Courier New;}}
{\colortbl ;\red0\green0\blue255;}
\viewkind4\uc1\pard\cf1\lang2057\b\fs32 Point\cf0\lang1033\b0\f1\fs20 
\par 
\par \pard\li700\lang2057\f0 Creates one or more single pixel points.
\par \pard\b 
\par Syntax:
\par \b0\tab 
\par \pard\li720\f2 Point name colour
\par \{
\par \pard\li1400 x y z
\par <x, y, z>
\par <...>
\par \pard\li720\}
\par \pard\f0 
\par \b Parameters:\b0 
\par 
\par \pard\li720\i x, y, z:
\par \pard\li1400\i0 The position of the point.
\par \pard\f2 
\par \b\f0 Example:
\par 
\par \pard\li700\b0\f2 #####
\par # Point
\par # Syntax: Point name colour \{ x y z \}
\par Point MyPoint FFFF0000
\par \{
\par \pard\li1400 1 1 1
\par \pard\li700\}
\par \pard\lang1033\f1 
\par }
40
Scribble40
Line, LineD, LineNL
Line;LineD;LineNL;


objecttypesequence:000020
Writing



FALSE
76
{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 Arial;}{\f1\fnil Arial;}{\f2\fnil\fprq1\fcharset0 Courier New;}}
{\colortbl ;\red0\green0\blue255;}
\viewkind4\uc1\pard\cf1\lang2057\b\fs32 Line, LineD, LineNL\cf0\lang1033\b0\f1\fs20 
\par 
\par \pard\li680\lang2057\f0 Creates one or more lines. There are three variations of the Line. They are:
\par \b\tab 
\par \pard\fi-740\li1400\b0\i Line:\i0\tab Creates a line between two points \f2 
\par \i\f0 LineD:\tab\i0 Creates a line from a point and a direction\b 
\par \b0\i LineNL:\i0\tab Creates a line from a point, a normal, and a length
\par \pard\b 
\par Syntax:
\par \b0\tab 
\par \pard\li680\f2 Line name colour
\par \{
\par \pard\li1400 x1 y1 z1 x2 y2 z2
\par <x1 y1 z1 x2 y2 z2>
\par <...>
\par \pard\li680\}
\par 
\par LineD name colour
\par \{
\par \pard\li1400 x1 y1 z1 dx dy dz
\par <x1 y1 z1 dx dy dz>
\par <...>
\par \pard\li680\}
\par \f0 
\par \f2 LineNL name colour
\par \{
\par \pard\li1400 x1 y1 z1 nx ny nz l
\par <x1 y1 z1 nx ny nz l>
\par <...>
\par \pard\li680\}
\par \pard\f0 
\par \b Parameters:\b0 
\par 
\par \pard\li680\i x1, y1, z1:
\par \pard\li1400\i0 The start vertex of the line
\par \pard\li680 
\par \i x2, y2, z2:
\par \pard\li1400\i0 The end vertex of the line
\par \pard\li680 
\par d\i x, dy, dz:
\par \pard\li1420\i0 The direction vector of the line
\par \pard\li680 
\par \i nx, ny, nz:
\par \pard\li1400\i0 The normal direction vector of the line
\par \pard\li680\i 
\par l:
\par \pard\li1400\i0 The length to multiply the normal direction vector by.
\par \pard 
\par \b Example:
\par 
\par \pard\li680\b0\f2 #####
\par # Line
\par # Syntax: Line name colour \{ x1 y1 z1 x2 y2 z2 \}
\par Line MyLine  FF00FF00
\par \{
\par \pard\li1400 0 0 0 1 1 1
\par \pard\li680\}
\par 
\par #####
\par # LineD
\par # Syntax: LineD name colour \{ x1 y1 z1 dx dy dz \}
\par LineD MyLine  FF00FF00
\par \{
\par \pard\li1380 0 1 0 1 0 1
\par \pard\li680\}
\par 
\par #####
\par # LineNL
\par # Syntax: LineNL name colour \{ x1 y1 z1 nx ny nz l \}
\par LineNL MyLine  FF00FF00
\par \{
\par \pard\li1380 1 1 1 0 -1 0 3
\par \pard\li680\}\lang1033\f1 
\par }
50
Scribble50
RectangleLU, RectangleWHZ
RectangleLU;RectangleWHZ;


objecttypesequence:000030
Writing



FALSE
62
{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 Arial;}{\f1\fnil Arial;}{\f2\fnil\fprq1\fcharset0 Courier New;}}
{\colortbl ;\red0\green0\blue255;}
\viewkind4\uc1\pard\cf1\lang2057\b\fs32 RectangleLU, RectangleWHZ\cf0\lang1033\b0\f1\fs20 
\par 
\par \pard\li720\lang2057\f0 Creates one or more rectangles. There are two variations of the Rectangle. They are:
\par \i 
\par \pard\fi-1400\li2120 RectangleLU:\i0\tab Creates a rectangle between a lower point and an upper point\f2 
\par \i\f0 RectangleWHZ:\tab\i0 Creates a rectangle perpendicular to the Z axis from a width and height.
\par \pard 
\par \b Syntax:
\par \b0\tab 
\par \pard\li720\f2 RectangleLU name colour
\par \{
\par \pard\li1380 lx ly lz ux uy uz
\par <lx ly lz ux uy uz>
\par <...>
\par \pard\li720\}
\par \f0 
\par \f2 RectangleWHZ name colour
\par \{
\par \pard\fi20\li1420 w h z
\par <w h z>
\par <...>
\par \pard\li720\}
\par \pard\tab\f0 
\par \b Parameters:\b0 
\par 
\par \pard\li720\i lx, ly, lz:
\par \pard\li1400\i0 The lower corner of the rectangle
\par \pard\li720 
\par \i ux, uy, uz:
\par \pard\fi20\li1400\i0 The upper corner of the rectangle
\par \pard\li720 
\par \i w:
\par \pard\li1400\i0 The width of the rectangle
\par \pard\li720\i 
\par h:
\par \pard\li1400\i0 The height of the rectangle
\par \pard\li720 
\par \i z:
\par \pard\fi-20\li1400\i0 The position on the Z axis of the rectangle
\par \pard 
\par \b Example:
\par 
\par \pard\li720\b0\f2 #####
\par # RectangleLU
\par # Syntax: RectangleLU name colour \{ lx ly lz ux uy uz \}
\par RectangleLU MyRectangleLU FF0000FF
\par \{
\par \pard\li1380 0 0 0 1 1 1
\par \pard\li720\}
\par 
\par #####
\par # RectangleWHZ
\par # Syntax: RectangleWHZ name colour \{ w h z \}
\par RectangleWHZ MyRectangleWHZ FF00FFFF
\par \{
\par \pard\li1400 2 3 1
\par \pard\li720\}
\par \pard 
\par 
\par }
60
Scribble60
Triangle
Triangle;


objecttypesequence:000040
Writing



FALSE
38
{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 Arial;}{\f1\fnil Arial;}{\f2\fnil\fprq1\fcharset0 Courier New;}}
{\colortbl ;\red0\green0\blue255;}
\viewkind4\uc1\pard\cf1\lang2057\b\fs32 Triangle\cf0\lang1033\b0\f1\fs20 
\par \cf1\lang2057\b\f0 
\par \pard\li700\cf0\b0 Creates one or more triangles.
\par \pard\i 
\par \b\i0 Syntax:
\par \b0\tab 
\par \pard\li720\lang1033\f2 Triangle name colour
\par \{
\par \pard\li1400 x0 y0 z0 x1 y1 z1 x2 y2 z2
\par <x0 y0 z0 x1 y1 z1 x2 y2 z2>
\par <...>
\par \pard\li720\}
\par \pard\lang2057\tab\f0 
\par \b Parameters:\b0 
\par 
\par \pard\li720\i x0, y0, z0:
\par \pard\li1400\i0 The first vertex of the triangle
\par \pard\li720\i 
\par x1, y1, z1:
\par \pard\li1400\i0 The second vertex of the triangle
\par \pard\li720 
\par \i x2, y2, z2:
\par \pard\li1400\i0 The third vertex of the triangle
\par \pard 
\par \b Example:
\par 
\par \pard\li680\lang1033\b0\f2 #####
\par # Triangle
\par # Syntax: Triangle name colour \{ x0 y0 z0 x1 y1 z1 x2 y2 z2 \}
\par Triangle MyTriangle FF00FF00
\par \{
\par \pard\li1400 0 0 0
\par 1 1 1
\par 0 1 0
\par \pard\li680\}\f1 
\par }
70
Scribble70
Quad, QuadLU, QuadWHZ
Quad;QuadLU;


objecttypesequence:000050
Writing



FALSE
92
{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 Arial;}{\f1\fnil Arial;}{\f2\fnil\fprq1\fcharset0 Courier New;}}
{\colortbl ;\red0\green0\blue255;}
\viewkind4\uc1\pard\cf1\lang2057\b\fs32 Quad, QuadLU, QuadWHZ\cf0\lang1033\b0\f1\fs20 
\par 
\par \pard\li720\lang2057\f0 Creates one or more quads. There are three variations of the Quad. They are:
\par \i 
\par \pard\fi-1420\li2140 Quad:\tab\tab\i0 Creates a quad from four points\i 
\par QuadLU:\tab\i0 Creates a quad between a lower point and an upper point\f2 
\par \i\f0 QuadWHZ::\i0\tab Creates a quad perpendicular to the Z axis from a width and height.
\par \pard 
\par \b Syntax:
\par \b0\tab 
\par \pard\li700\lang1033\f2 Quad name colour
\par \{
\par \pard\li1400 x0 y0 z0 x1 y1 z1 x2 y2 z2 x3 y3 z3
\par \lang2057 <\lang1033 x0 y0 z0 x1 y1 z1 x2 y2 z2 x3 y3 z3\lang2057 >
\par <...>
\par \pard\li700\}
\par \f0 
\par \lang1033\f2 QuadLU name colour
\par \{
\par \pard\li1420 lx ly lz ux uy uz
\par \lang2057 <lx\lang1033  ly lz ux uy uz\lang2057 >
\par <...>
\par \pard\li700\}
\par \tab 
\par \lang1033 QuadWHZ name colour
\par \{
\par \pard\li1400 w h z
\par \lang2057 <\lang1033 w h z\lang2057 >
\par <...>
\par \pard\li700\lang1033\}
\par \pard\lang2057\f0 
\par \b Parameters:\b0 
\par 
\par \pard\li700\i x0, y0, z0:
\par \pard\fi-20\li1400\i0 The first vertex of the quad
\par \pard\li700\i 
\par x1, y1, z1:
\par \pard\fi-20\li1400\i0 The second vertex of the quad
\par \pard\li700 
\par \i x2, y2, z2:
\par \pard\li1400\i0 The third vertex of the quad
\par \pard\li700 
\par \i x3, y3, z3:
\par \pard\li1400\i0 The fourth vertex of the quad
\par \pard\li700 
\par \i lx, ly, lz:
\par \pard\fi-60\li1440\i0 The lower corner of the quad
\par \pard\li700\i 
\par ux, uy, uz:
\par \pard\li1400\i0 The upper corner of the quad
\par \pard\li700 
\par \i w:
\par \pard\li1400\i0 The width of the quad
\par \pard\li700 
\par \i h:
\par \pard\fi700\li680\i0 The height of the quad
\par \pard\li700 
\par \i z:
\par \pard\fi60\li1400\i0 The position on the Z axis of the quad
\par \pard 
\par \b Example:
\par 
\par \pard\li720\lang1033\b0\f2 #####
\par # Quad
\par # Syntax: Quad name colour \{ x0 y0 z0 x1 y1 z1 x2 y2 z2 x3 y3 z3 \}
\par Quad MyQuad FFF0F00F
\par \{
\par \pard\fi-20\li1400 0 0 0
\par 1 0 0
\par 1 1 0
\par 0 1 0
\par \pard\li720\}
\par 
\par #####
\par # QuadLU
\par # Syntax: QuadLU name colour \{ lx ly lz ux uy uz \}
\par QuadLU MyQuadLU FF0FF00F
\par \{
\par \pard\li1400 0 0 0 
\par -1 -1 -1
\par \pard\li720\}
\par 
\par #####
\par # QuadWHZ
\par # Syntax: QuadWHZ name colour \{ w h z \}
\par QuadWHZ MyQuadWHZ FF0FF00F
\par \{
\par \pard\li1400 2 1 -5
\par \pard\li720\}\f1 
\par }
80
Scribble80
CircleR, CircleRxRyZ
CircleR;CircleRxRyZ;


objecttypesequence:000060
Writing



FALSE
55
{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 Arial;}{\f1\fnil Arial;}{\f2\fnil\fprq1\fcharset0 Courier New;}}
{\colortbl ;\red0\green0\blue255;}
\viewkind4\uc1\pard\cf1\lang2057\b\fs32 CircleR, CircleRxRyZ\cf0\lang1033\b0\f1\fs20 
\par 
\par \pard\li720\lang2057\f0 Creates one or more circles. There are two variations of the Circle. They are:
\par \i 
\par \pard\fi-1400\li2120 CircleR:\tab\i0\tab Creates a circle from a radius centred at the origin and perpendicular to the z axis\f2 
\par \i\f0 CircleRxRyZ:\tab\i0 Creates an ellipse from an x radius and y radius perpendicular to the z axis
\par \pard\li720 
\par \pard\b Syntax:
\par \b0\tab 
\par \pard\li720\lang1033\f2 CircleR name colour
\par \{
\par \pard\li1400 r
\par \lang2057 <r>
\par <...>
\par \pard\li720\lang1033\}
\par \lang2057 
\par \lang1033 CircleRxRyZ name colour
\par \{
\par \pard\fi-80\li1480 rx ry z
\par <rx ry z>
\par <...>
\par \pard\li720\}
\par \pard\lang2057\f0 
\par \b Parameters:\b0 
\par 
\par \pard\li700\i r:
\par \pard\li1400\i0 The radius of the circle
\par \pard\li700 
\par \i rx, ry:
\par \pard\li1400\i0 The x and y radii of the ellipse
\par \pard\li700\i 
\par z:
\par \pard\li1420\i0 The position on the Z axis of the circle
\par \pard\li700 
\par \pard\b Example:
\par 
\par \pard\li700\lang1033\b0\f2 #####
\par # CircleR
\par # Syntax: CircleR name colour \{ r \}
\par CircleR MyCircleR FFFFFF00
\par \{
\par \pard\li1420 1
\par \pard\li700\}
\par 
\par #####
\par # CircleRxRyZ
\par # Syntax: CircleRxRyZ name colour \{ rx ry z \}
\par CircleRxRyZ MyCircleRxRyZ FF808080
\par \{
\par \pard\li1380 3 2 -1
\par \pard\li700\}
\par \f1 
\par }
90
Scribble90
BoxLU, BoxWHD
BoxLU;BoxWHD;


objecttypesequence:000070
Writing



FALSE
55
{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 Arial;}{\f1\fnil Arial;}{\f2\fnil\fprq1\fcharset0 Courier New;}}
{\colortbl ;\red0\green0\blue255;}
\viewkind4\uc1\pard\cf1\lang2057\b\fs32 BoxLU, BoxWHD\cf0\lang1033\b0\f1\fs20 
\par 
\par \pard\li720\lang2057\f0 Creates one or more boxes. There are two variations of the Box. They are:
\par \i 
\par \pard\fi-1420\li2140 BoxLU:\i0\tab\tab Creates a box between a lower point and an upper point\f2 
\par \i\f0 BoxWHD:\tab\i0 Creates a box from a width, height, and depth.
\par \pard 
\par \b Syntax:
\par \b0\tab 
\par \pard\li700\lang1033\f2 BoxLU name colour
\par \{
\par \pard\li1400 lx ly lz ux uy uz
\par \lang2057 <lx ly lz ux uy uz>
\par <...>
\par \pard\li700\}
\par \f0 
\par \lang1033\f2 BoxWHD name colour
\par \lang2057\{
\par \pard\li1380 w h d
\par <w h d>
\par <...>
\par \pard\li700\}
\par \pard\tab\f0 
\par \b Parameters:\b0 
\par 
\par \pard\li720\i lx, ly, lz:
\par \pard\li1380\i0 The lower corner of the box
\par \pard\li720 
\par \i ux, uy, uz:
\par \pard\fi-20\li1400\i0 The upper corner of the box
\par \pard\li720 
\par w, h, d\i :
\par \pard\li1400\i0 The width, height, and depth of the box respectively
\par \pard\li720 
\par \pard\b Example:
\par \lang1033\b0\f1 
\par \pard\li680\f2 #####
\par # BoxLU
\par # Syntax: BoxLU name colour \{ lx ly lz ux uy uz \}
\par BoxLU MyBoxLU FF008080
\par \{
\par \pard\li1420 -1 -1 -1 1 1 1
\par \pard\li680\}
\par 
\par #####
\par # BoxWHD
\par # Syntax: BoxWHD name colour \{ w h d \}
\par BoxWHD MyBoxWHD FFFFFF00
\par \{
\par \pard\li1400 1 1.5 2
\par \pard\li680\}
\par \f1 
\par }
100
Scribble100
CylinderHR, CylinderHRxRy
CylinderHR;CylinderHRxRy;


objecttypesequence:000080
Writing



FALSE
51
{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 Arial;}{\f1\fnil Arial;}{\f2\fnil\fprq1\fcharset0 Courier New;}}
{\colortbl ;\red0\green0\blue255;}
\viewkind4\uc1\pard\cf1\lang2057\b\fs32 CylinderHR, CylinderHRxRy\cf0\lang1033\b0\f1\fs20 
\par 
\par \pard\li700\lang2057\f0 Creates a cylinder. There are two variations of the Cylinder. They are:
\par \i 
\par \pard\fi-1460\li2140 CylinderHR:\i0\tab Creates a cylinder from a height and radius centred at the origin and parallel to the z axis\f2 
\par \i\f0 CylinderHRxRy:\tab\i0 Creates an ellipsoid from an x radius, y radius, and height and radius centred at the origin and parallel to the z axis\f2 
\par \pard\f0 
\par \b Syntax:
\par \b0\tab 
\par \pard\li700\lang1033\f2 CylinderHR name colour
\par \{
\par \pard\li1380 h r
\par \pard\li700\lang2057\}
\par \f0 
\par \lang1033\f2 CylinderHRxRy name colour
\par \{
\par \pard\li1400 h rx ry
\par \pard\li700\lang2057\}
\par \tab\f0 
\par \pard\b Parameters:\b0 
\par 
\par \pard\li680\i h:
\par \pard\li1380\i0 The height of the cylinder or ellipsoid
\par \pard\li680 
\par \i r:
\par \pard\li1380\i0 The radius of the cylinder
\par \pard\li680 
\par \i rx, ry:
\par \pard\fi-40\li1420\i0 The X and Y radii of the ellipsoid
\par \pard\li680 
\par \pard\b Example:
\par 
\par \pard\fi-20\li720\lang1033\b0\f2 #####
\par # CylinderHR
\par # Syntax: CylinderHR name colour \{ h r \}
\par CylinderHR MyCylinderHR FFFF007F
\par \{
\par \pard\li1400 2 0.5
\par \pard\fi-20\li720\}
\par 
\par #####
\par # CylinderHRxRy
\par # Syntax: CylinderHRxRy name colour \{ h rx ry \}
\par CylinderHRxRy MyCylinderHRxRy FF7F7FFF
\par \{
\par \pard\fi-20\li1420 2 0.5 0.75
\par \pard\fi-20\li720\}
\par \f1 
\par }
110
Scribble110
SphereR, SphereRxRyRz
SphereR;SphereRxRyRz;


objecttypesequence:000090
Writing



FALSE
48
{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 Arial;}{\f1\fnil Arial;}{\f2\fnil\fprq1\fcharset0 Courier New;}}
{\colortbl ;\red0\green0\blue255;}
\viewkind4\uc1\pard\cf1\lang2057\b\fs32 SphereR, SphereRxRyRz\cf0\lang1033\b0\f1\fs20 
\par 
\par \pard\li700\lang2057\f0 Creates a sphere. There are two variations of the Sphere. They are:
\par \i 
\par \pard\fi-1440\li2120 SphereR:\i0\tab Creates a sphere from a radius, centred at the origin\f2 
\par \i\f0 SphereRxRyRz:\tab\i0 Creates a spheroid from x, y, and z radii, centred at the origin\f2 
\par \pard\f0 
\par \b Syntax:
\par \b0\tab 
\par \pard\li720\lang1033\f2 SphereR name colour
\par \{
\par \pard\li1380 r
\par \pard\li720\}
\par \lang2057\f0 
\par \lang1033\f2 SphereRxRyRz name colour
\par \{
\par \pard\li1400 rx ry rz
\par \pard\li720\lang2057\}
\par \pard\tab\f0 
\par \b Parameters:\b0 
\par 
\par \pard\li720\i r:
\par \pard\li1400\i0 The radius of the sphere
\par \pard\li720 
\par \i rx, ry, rz:
\par \pard\li1420\i0 The X, Y, and Z radii of the spheroid
\par \pard\li720 
\par \pard\b Example:
\par 
\par \pard\fi-20\li720\lang1033\b0\f2 #####
\par # SphereR
\par # Syntax: SphereR name colour \{ r \}
\par SphereR MySphereR FFFF7F7F
\par \{
\par \pard\li1400 2
\par \pard\fi-20\li720\}
\par 
\par #####
\par # SphereRxRyRz 
\par # Syntax: SphereRxRyRz name colour \{ rx ry rz \}
\par SphereRxRyRz MySphereRxRyRz FF007FFF
\par \{
\par \pard\li1400 0.25 0.5 0.75
\par \pard\fi-20\li720\}
\par \pard\f1 
\par }
120
Scribble120
FrustumWHNF, FrustumATNF
FrustumATNF;FrustumWHNF;


objecttypesequence:000100
Writing



FALSE
52
{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 Arial;}{\f1\fnil Arial;}{\f2\fnil\fprq1\fcharset0 Courier New;}}
{\colortbl ;\red0\green0\blue255;}
\viewkind4\uc1\pard\cf1\lang2057\b\fs32 FrustumWHNF, FrustumATNF\cf0\lang1033\b0\f1\fs20 
\par 
\par \pard\li700\lang2057\f0 Creates a frustum. There are two variations of the Frustum. They are:
\par \i 
\par \pard\fi-1440\li2120 FrustumWHNF:\i0\tab Creates a frustum from a width, height, near plane, and far plane.\f2 
\par \i\f0 FrustumATNF:\tab\i0 Creates a frustum from a horizontal angle, vertical angle, near plane, and far plane\f2 
\par \pard\li700\f0 
\par Both frustra are created parallel to the z axis.
\par \pard 
\par \b Syntax:
\par \b0\tab 
\par \pard\li720\lang1033\f2 FrustumWHNF name colour
\par \{
\par \pard\fi20\li1380 w h n f
\par \pard\li720\}
\par \tab 
\par FrustumATNF name colour
\par \{
\par \pard\fi20\li1380 a t n f
\par \pard\li720\}
\par \pard\tab\lang2057\tab\f0 
\par \b Parameters:\b0 
\par 
\par \pard\li680\i w, h:
\par \pard\li1420\i0 The width and height of the frustum at the near plane
\par \pard\li680 
\par \i n, f:\tab 
\par \pard\li1380\i0 The distance to the near and far plane respectively
\par \pard\li680 
\par \i a, t:
\par \pard\li1420\i0 The horizontal angle (alpha) and vertical angle (theta) in degrees
\par \pard 
\par \b Example:
\par 
\par \pard\fi-40\li740\lang1033\b0\f2 #####
\par # FrustumWHNF
\par # Syntax: FrustumWHNF name colour \{ w h n f \}
\par FrustumWHNF MyFrustumWHNF 707F00FF
\par \{
\par \pard\li1400 1 1 1 2
\par \pard\fi-40\li740\}
\par 
\par #####
\par # FrustumATNF
\par # Syntax: FrustumATNF name colour \{ a t n f \}
\par FrustumATNF MyFrustumATNF 707FFF00
\par \{
\par \pard\fi-20\li1400 45 45 1 2.5
\par \pard\fi-40\li740\}\f1 
\par }
130
Scribble130
GridWHD
GridWHD;


objecttypesequence:000110
Writing



FALSE
59
{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 Arial;}{\f1\fnil Arial;}{\f2\fnil\fprq1\fcharset0 Courier New;}}
{\colortbl ;\red0\green0\blue255;}
\viewkind4\uc1\pard\cf1\lang2057\b\fs32 GridWHD\cf0\lang1033\b0\f1\fs20 
\par 
\par \pard\li700\lang2057\f0 Creates a grid perpendicular to the z axis.
\par \pard\i 
\par \b\i0 Syntax:
\par \b0\tab 
\par \pard\fi-80\li780\lang1033\f2 GridWHD name colour
\par \{
\par \pard\li1380 w h
\par x y z
\par x y z
\par ...
\par \pard\fi-80\li780\}
\par \pard\lang2057\tab\f0 
\par \b Parameters:\b0 
\par 
\par \pard\li680\i w, h:
\par \pard\li1380\i0 The width and height of the grid
\par \pard\li680\i 
\par x y z:
\par \pard\li1400\i0 The vertices of the grid. The number of vertices given must equal w * h.
\par \pard\li680 
\par \pard\b Example:
\par 
\par \pard\li720\lang1033\b0\f2 #####
\par # GridWHD
\par # Syntax: GridWHD name colour \{ w h point point ... \}
\par GridWHD MyGridWHD FF7F7FFF
\par \{
\par \pard\fi-20\li1400 5\tab 5
\par 0\tab 0\tab 1.000000\tab 
\par 1\tab 0\tab -1.000000\tab 
\par 2\tab 0\tab 1.000000\tab 
\par 3\tab 0\tab -1.000000\tab 
\par 4\tab 0\tab 1.000000\tab 
\par 0\tab 1\tab 0.707100\tab 
\par 1\tab 1\tab -0.707100\tab 
\par 2\tab 1\tab 0.707100\tab 
\par 3\tab 1\tab -0.707100\tab 
\par 4\tab 1\tab 0.707100\tab 
\par 0\tab 2\tab 0.224733\tab 
\par 1\tab 2\tab -0.224733\tab 
\par 2\tab 2\tab 0.224733\tab 
\par 3\tab 2\tab -0.224733\tab 
\par 4\tab 2\tab 0.224733\tab 
\par 0\tab 3\tab -0.317851\tab 
\par 1\tab 3\tab 0.317851\tab 
\par 2\tab 3\tab -0.317851\tab 
\par 3\tab 3\tab 0.317851\tab 
\par 4\tab 3\tab -0.317851\tab 
\par 0\tab 4\tab -0.775267\tab 
\par 1\tab 4\tab 0.775267\tab 
\par 2\tab 4\tab -0.775267\tab 
\par 3\tab 4\tab 0.775267\tab 
\par 4\tab 4\tab -0.775267\tab 
\par \pard\li720\}
\par }
140
Scribble140
Mesh
Mesh;


objecttypesequence:000120
Writing



FALSE
71
{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 Arial;}{\f1\fnil Arial;}{\f2\fnil\fprq1\fcharset0 Courier New;}}
{\colortbl ;\red0\green0\blue255;}
\viewkind4\uc1\pard\cf1\lang2057\b\fs32 Mesh\cf0\lang1033\b0\f1\fs20 
\par \lang2057\f0 
\par \pard\li680 Creates a general mesh. A mesh must have the sections "Vertices" and "Indices". The "Vertices" section contains a list of vertices that define points in the mesh. The "Indices" section contains a multiple of 3 indices that reference into the "Vertices" section defining triangle faces in the mesh. The "Normals" section is optional and allows a particular normal for each vertex. If not given, normals are generated automatically.
\par \i 
\par \pard\b\i0 Syntax:
\par \b0\tab 
\par \pard\li700\lang1033\f2 Mesh name colour
\par \{
\par \pard\li1400 Vertices \{ x y z ... \}
\par Indices  \{ i1 i2 i3 i4 ... \}
\par <Normals \{ nx ny nz ... \}>
\par \pard\li700\}\lang2057\tab\f0 
\par \pard\b 
\par Parameters:\b0 
\par 
\par \pard\li700\i x y z:
\par \pard\li1400\i0 The vertices of the mesh.
\par \pard\li700 
\par \i i1 i2  i3 ...:
\par \pard\li1380\i0 Indices into the vertices defining the triangles of the mesh. Must be a multiple of three.
\par \pard\li700 
\par \i nx ny nz ...:
\par \pard\li1400 Normals for each vertex in the list of vertices\i0 . If given, their must be one normal for every vertex.
\par \pard 
\par \b Example:
\par 
\par \pard\li700\lang1033\b0\f2 #####
\par # Mesh
\par # Syntax:\tab Mesh name colour
\par # \tab\tab\{
\par # \tab\tab\tab Vertices \{ x y z ... \}
\par # \tab\tab\tab Indices \{ i1 i2 i3 i4 ... \}
\par # \tab\tab\tab [Normals \{ nx ny nz ... \}]
\par # \tab\tab\}
\par Mesh MyMesh FFFFFF00
\par \{ 
\par \pard\li1400 Vertices
\par \{
\par \pard\li2100 1.087695 -2.175121 0.600000
\par 1.087695  3.726199 0.600000
\par 2.899199 -2.175121 0.600000
\par 2.899199  3.726199 0.600000
\par 1.087695  3.726199 0.721147
\par 1.087695 -2.175121 0.721147
\par 2.899199 -2.175121 0.721147
\par 2.899199  3.726199 0.721147
\par 1.087695  3.726199 0.721147
\par 1.087695  3.726199 0.600000
\par 1.087695 -2.175121 0.600000
\par 1.087695 -2.175121 0.721147
\par 2.730441  3.725990 0.721148
\par 2.740741 -2.175321 0.721147
\par 2.740741 -2.175321 0.600000
\par 2.730441  3.725990 0.600000
\par \pard\li1400\}
\par Indices
\par \{
\par \pard\li2120 0,1,2;,
\par 3,2,1;,
\par 4,5,6;,
\par 6,7,4;,
\par 8,9,10;,
\par 8,10,11;,
\par 12,13,14;,
\par 14,15,12;;
\par \pard\li1400\}
\par \pard\li700\}
\par \f1 
\par }
150
Scribble150
File
Microsoft DirectX XFile;XFile;


objecttypesequence:000130
Writing



FALSE
39
{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 Arial;}{\f1\fnil Arial;}{\f2\fnil\fprq1\fcharset0 Courier New;}}
{\colortbl ;\red0\green0\blue255;}
\viewkind4\uc1\pard\cf1\lang2057\b\fs32 File\cf0\lang1033\b0\f1\fs20 
\par 
\par \pard\fi-20\li740\lang2057\f0 Loads a file containing mesh geometry. Supported file formats are:
\par \pard\li1400 Microsoft DirectX X Files
\par 3D Studio Max ASE Files
\par \pard\i 
\par \b\i0 Syntax:
\par \b0\tab 
\par \pard\fi-20\li700\lang1033\f2 File name colour
\par \{
\par \pard\li1380 "filename"
\par \pard\fi-20\li700\}
\par \pard\lang2057\b\f0 
\par Parameters:\b0 
\par 
\par \pard\li680\i filename:
\par \pard\fi-40\li1420\i0 The full path and filename of the xfile to load in quotes.
\par \pard 
\par 
\par \b Example:
\par 
\par \pard\li720\lang1033\b0\f2 #####
\par # File
\par # Syntax: File name colour \{ "filename" \}
\par File MyXfile 70FFFFFF
\par \{
\par \pard\li1420 "C:\\XFiles\\MyXFile.x"
\par \pard\li720\}
\par 
\par or
\par 
\par File MyASEfile 70FFFFFF
\par \{
\par \pard\li1420 "C:\\AseFiles\\MyAseFile.ase"
\par \pard\li720\}
\par \f1 
\par }
160
Scribble160
Group, GroupCyclic
Group;GroupCyclic;Start GroupCyclic;


objecttypesequence:000140
Writing



FALSE
75
{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 Arial;}{\f1\fnil Arial;}{\f2\fnil\fprq1\fcharset0 Courier New;}}
{\colortbl ;\red0\green0\blue255;\red0\green128\blue0;\red128\green0\blue0;}
\viewkind4\uc1\pard\cf1\lang2057\b\fs32 Group, GroupCyclic\cf0\lang1033\b0\f1\fs20 
\par 
\par \pard\li680\lang2057\f0 Groups a number of objects together. The group itself contains no object data, it is simply used to encapsulate objects.
\par There are two variations of the group, they are:
\par \i 
\par \pard\fi-1520\li2180 Group:\i0\tab\tab Collects several objects together so that they can be modified as a single object\f2 
\par \i\f0 GroupCyclic:\tab\i0 Collects several objects together but draws one at a time in a cyclic fashion. GroupCyclic objects are started via the "Data\\\cf2\strike Start Cyclic Objects\cf3\strike0\{linkID=550\}\cf0 " menu option\f2 .
\par \pard\f0 
\par \b Syntax:
\par \b0\tab 
\par \pard\li700\lang1033\f2 Group name colour
\par \{
\par \pard\li1400 AnyObject
\par <AnyObject>
\par <...\lang2057 >
\par \pard\li700\}
\par \f0 
\par \lang1033\f2 GroupCyclic name colour
\par \{
\par \pard\li1420 style
\par frames_per_second
\par AnyObject
\par <AnyObject>
\par <...\lang2057 >
\par \pard\li700\}
\par \tab\f0 
\par \pard\b Parameters:\b0 
\par 
\par \pard\li700\i AnyObject:
\par \pard\fi-40\li1420\i0 Any object type
\par \pard\li700 
\par \i style:
\par \pard\li1420\i0 The style in which to play a cyclic group:
\par \pard\li2140\lang1033\f2 0 = start -> end
\par 1 = end -> start
\par 2 = ping pong, i.e. start -> end -> start -> end -> ...
\par \pard\li700\lang2057\f0 
\par \i frames_per_second:
\par \pard\li1380\i0 The rate at which to switch between GroupCyclic objects in seconds
\par \pard 
\par \b Example:
\par \lang1033\b0\f1 
\par \pard\li700\f2 #####
\par # Group
\par # Syntax: Group name colour \{ AnyObject ... \}
\par Group Axes FFFFFFFF
\par \{
\par \pard\li1400 Line X\tab FFFF0000 \{ 0 0 0 1 0 0 \}
\par Line Y\tab FF00FF00 \{ 0 0 0 0 1 0 \}
\par Line Z\tab FF0000FF \{ 0 0 0 0 0 1 \}
\par \pard\li700\}
\par 
\par #####
\par # GroupCyclic
\par # Syntax: GroupCyclic name colour \{ style frames_per_second AnyObject ... \}
\par # style: 0 = start->end
\par #        1 = end->start
\par #        2 = ping_pong
\par GroupCyclic MyGroupCyclic 00000000
\par \{
\par \pard\li1420 0
\par 8
\par Line frame1 FFFF0000 \{ 0 0 0  5  0  0 \}
\par Line frame2 FFFF0000 \{ 0 0 0  4  4  0 \}
\par Line frame3 FFFF0000 \{ 0 0 0  0  5  0 \}
\par Line frame4 FFFF0000 \{ 0 0 0 -4  4  0 \}
\par Line frame5 FFFF0000 \{ 0 0 0 -5  0  0 \}
\par Line frame6 FFFF0000 \{ 0 0 0 -4 -4  0 \}
\par Line frame7 FFFF0000 \{ 0 0 0  0 -5  0 \}
\par Line frame8 FFFF0000 \{ 0 0 0  4 -4  0 \}
\par \pard\li700\}
\par \pard\f1 
\par }
170
Scribble170
Example Summary
Examples;Samples;


objecttypesequence:000150
Writing



FALSE
327
{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 Arial;}{\f1\fnil Arial;}{\f2\fnil\fprq1\fcharset0 Courier New;}}
{\colortbl ;\red0\green0\blue255;}
\viewkind4\uc1\pard\cf1\lang2057\b\fs32 Example Summary\cf0\lang1033\b0\f1\fs20 
\par 
\par \f2 ######################################################################
\par # Example file for Line Drawer
\par ######################################################################
\par #
\par #\tab Separator characters: ' ', ';', ','
\par #\tab Colours: AARRGGBB
\par #\tab Object Types:\tab\tab\tab\tab Listable
\par #\tab\tab Point\tab\tab\tab\tab\tab\tab Y
\par #\tab\tab Line\tab\tab\tab\tab\tab\tab Y
\par #\tab\tab LineD\tab\tab\tab\tab\tab\tab Y
\par #\tab\tab LineNL\tab\tab\tab\tab\tab Y
\par #\tab\tab RectangleLU\tab\tab\tab\tab\tab Y
\par #\tab\tab RectangleWHZ\tab\tab\tab\tab Y
\par #\tab\tab Triangle\tab\tab\tab\tab\tab Y
\par #\tab\tab Quad\tab\tab\tab\tab\tab\tab Y
\par #\tab\tab QuadLU\tab\tab\tab\tab\tab Y
\par #\tab\tab QuadWHZ\tab\tab\tab\tab\tab Y
\par #\tab\tab CircleR\tab\tab\tab\tab\tab Y
\par #\tab\tab CircleRxRyZ\tab\tab\tab\tab\tab Y
\par #\tab\tab BoxLU\tab\tab\tab\tab\tab\tab Y
\par #\tab\tab BoxWHD\tab\tab\tab\tab\tab Y
\par #\tab\tab CylinderHR\tab\tab\tab\tab\tab N
\par #\tab\tab CylinderHRxRy\tab\tab\tab\tab N
\par #\tab\tab SphereR\tab\tab\tab\tab\tab N
\par #\tab\tab SphereRxRyRz\tab\tab\tab\tab N
\par #\tab\tab FrustumWHNF\tab\tab\tab\tab\tab N
\par #\tab\tab FrustumATNF\tab\tab\tab\tab\tab N
\par #\tab\tab GridWHD\tab\tab\tab\tab\tab N
\par #\tab\tab Mesh\tab\tab\tab\tab\tab\tab N
\par #\tab\tab File\tab\tab\tab\tab\tab\tab N
\par #\tab\tab Group\tab\tab\tab\tab\tab\tab Y
\par #\tab\tab GroupCyclic\tab\tab\tab\tab\tab Y
\par #\tab Optional sections:
\par #\tab\tab Transform
\par #\tab\tab Position
\par #\tab\tab Orientation
\par #\tab\tab Animation
\par #\tab\tab Hidden
\par #\tab\tab Wire
\par #
\par ######################################################################
\par 
\par 
\par #####
\par # Point
\par # Syntax: Point name colour \{ x y z \}
\par Point MyPoint FFFF0000 \{ 1 1 1 \}
\par 
\par #####
\par # Line
\par # Syntax: Line name colour \{ x1 y1 z1 x2 y2 z2 \}
\par Line MyLine  FF00FF00
\par \{
\par \tab 0 0 0 1 1 1
\par \tab Point MyChildPoint FF00FF00 \{ 0.5 0.5 0.5 \}
\par \}
\par 
\par #####
\par # LineD
\par # Syntax: LineD name colour \{ x1 y1 z1 dx dy dz \}
\par LineD MyLine  FF00FF00
\par \{
\par \tab 0 1 0 1 0 1
\par \}
\par 
\par #####
\par # LineNL
\par # Syntax: LineNL name colour \{ x1 y1 z1 nx ny nz l \}
\par LineNL MyLine  FF00FF00
\par \{
\par \tab 1 1 1 0 -1 0 3
\par \}
\par 
\par #####
\par # RectangleLU
\par # Syntax: RectangleLU name colour \{ lx ly lz ux uy uz \}
\par RectangleLU MyRectangleLU FF0000FF
\par \{
\par \tab 0 0 0 1 1 1
\par \}
\par 
\par #####
\par # RectangleWHZ
\par # Syntax: RectangleWHZ name colour \{ w h z \}
\par RectangleWHZ MyRectangleWHZ FF00FFFF
\par \{
\par \tab 2 3 1
\par \}
\par 
\par #####
\par # Triangle
\par # Syntax: Triangle name colour \{ x0 y0 z0 x1 y1 z1 x2 y2 z2 \}
\par Triangle MyTriangle FF00FF00
\par \{
\par \tab 0 0 0
\par \tab 1 1 1
\par \tab 0 1 0
\par \}
\par 
\par #####
\par # Quad
\par # Syntax: Quad name colour \{ x0 y0 z0 x1 y1 z1 x2 y2 z2 x3 y3 z3 \}
\par Quad MyQuad FFF0F00F
\par \{
\par \tab 0 0 0
\par \tab 1 0 0
\par \tab 1 1 0
\par \tab 0 1 0
\par \}
\par 
\par #####
\par # QuadLU
\par # Syntax: QuadLU name colour \{ lx ly lz ux uy uz \}
\par QuadLU MyQuadLU FF0FF00F
\par \{
\par \tab 0 0 0 
\par \tab -1 -1 -1
\par \}
\par 
\par #####
\par # QuadWHZ
\par # Syntax: QuadWHZ name colour \{ w h z \}
\par QuadWHZ MyQuadWHZ FF0FF00F
\par \{
\par \tab 2 1 -5
\par \}
\par 
\par #####
\par # CircleR
\par # Syntax: CircleR name colour \{ r \}
\par CircleR MyCircleR FFFFFF00
\par \{
\par \tab 1
\par \}
\par 
\par #####
\par # CircleRxRyZ
\par # Syntax: CircleRxRyZ name colour \{ rx ry z \}
\par CircleRxRyZ MyCircleRxRyZ FF808080
\par \{
\par \tab 3 2 -1
\par \}
\par 
\par #####
\par # BoxLU
\par # Syntax: BoxLU name colour \{ lx ly lz ux uy uz \}
\par BoxLU MyBoxLU FF008080
\par \{
\par \tab -1 -1 -1 1 1 1
\par \tab Animation
\par \tab\{
\par \tab\tab 3\tab\tab # Style: one of: NoAnimation = 0, PlayOnce = 1, PlayReverse = 2, PingPong = 3, PlayContinuous = 4
\par \tab\tab 2.0\tab\tab # Period in seconds
\par \tab\tab 0 1 0\tab\tab # Linear velocity in m/s
\par \tab\tab 1 0 0\tab\tab # Axis of rotation
\par \tab\tab 3.0\tab\tab # Angular speed in rad/s
\par \tab\}
\par \}
\par 
\par #####
\par # BoxWHD
\par # Syntax: BoxWHD name colour \{ w h d \}
\par BoxWHD MyBoxWHD FFFFFF00
\par \{
\par \tab 1 1.5 2
\par \}
\par 
\par #####
\par # CylinderHR
\par # Syntax: CylinderHR name colour \{ h r \}
\par CylinderHR MyCylinderHR FFFF007F
\par \{
\par \tab 2 0.5
\par \}
\par 
\par #####
\par # CylinderHRxRy
\par # Syntax: CylinderHRxRy name colour \{ h rx ry \}
\par CylinderHRxRy MyCylinderHRxRy FF7F7FFF
\par \{
\par \tab 2 0.5 0.75
\par \}
\par 
\par #####
\par # SphereR
\par # Syntax: SphereR name colour \{ r \}
\par SphereR MySphereR FFFF7F7F
\par \{
\par \tab 2
\par \}
\par 
\par #####
\par # SphereRxRyRz 
\par # Syntax: SphereRxRyRz name colour \{ rx ry rz \}
\par SphereRxRyRz MySphereRxRyRz FF007FFF
\par \{
\par \tab 0.25 0.5 0.75
\par \}
\par 
\par #####
\par # FrustumWHNF
\par # Syntax: FrustumWHNF name colour \{ w h n f \}
\par FrustumWHNF MyFrustumWHNF 707F00FF
\par \{
\par \tab 1 1 1 2
\par \}
\par 
\par #####
\par # FrustumATNF
\par # Syntax: FrustumATNF name colour \{ a t n f \}
\par FrustumATNF MyFrustumATNF 707FFF00
\par \{
\par \tab 45 45 1 2.5
\par \}
\par 
\par #####
\par # GridWHD
\par # Syntax: GridWHD name colour \{ w d point point ... \}
\par GridWHD MyGridWHD FF7F7FFF
\par \{
\par \tab 5\tab 5
\par \tab 0\tab 0\tab 1.000000\tab 
\par \tab 1\tab 0\tab -1.000000\tab 
\par \tab 2\tab 0\tab 1.000000\tab 
\par \tab 3\tab 0\tab -1.000000\tab 
\par \tab 4\tab 0\tab 1.000000\tab 
\par \tab 0\tab 1\tab 0.707100\tab 
\par \tab 1\tab 1\tab -0.707100\tab 
\par \tab 2\tab 1\tab 0.707100\tab 
\par \tab 3\tab 1\tab -0.707100\tab 
\par \tab 4\tab 1\tab 0.707100\tab 
\par \tab 0\tab 2\tab 0.224733\tab 
\par \tab 1\tab 2\tab -0.224733\tab 
\par \tab 2\tab 2\tab 0.224733\tab 
\par \tab 3\tab 2\tab -0.224733\tab 
\par \tab 4\tab 2\tab 0.224733\tab 
\par \tab 0\tab 3\tab -0.317851\tab 
\par \tab 1\tab 3\tab 0.317851\tab 
\par \tab 2\tab 3\tab -0.317851\tab 
\par \tab 3\tab 3\tab 0.317851\tab 
\par \tab 4\tab 3\tab -0.317851\tab 
\par \tab 0\tab 4\tab -0.775267\tab 
\par \tab 1\tab 4\tab 0.775267\tab 
\par \tab 2\tab 4\tab -0.775267\tab 
\par \tab 3\tab 4\tab 0.775267\tab 
\par \tab 4\tab 4\tab -0.775267\tab 
\par \}
\par 
\par #####
\par # Mesh
\par # Syntax: Mesh name colour \{ Vertices \{ x y z ... \} Indices \{ i1 i2 i3 i4 ... \} [Normals \{ nx ny nz ... \}] \}
\par Mesh MyMesh FFFFFF00
\par \{ 
\par \tab Vertices
\par \tab\{
\par \tab\tab 1.087695 -2.175121 0.600000
\par \tab\tab 1.087695  3.726199 0.600000
\par \tab\tab 2.899199 -2.175121 0.600000
\par \tab\tab 2.899199  3.726199 0.600000
\par \tab\tab 1.087695  3.726199 0.721147
\par \tab\tab 1.087695 -2.175121 0.721147
\par \tab\tab 2.899199 -2.175121 0.721147
\par \tab\tab 2.899199  3.726199 0.721147
\par \tab\tab 1.087695  3.726199 0.721147
\par \tab\tab 1.087695  3.726199 0.600000
\par \tab\tab 1.087695 -2.175121 0.600000
\par \tab\tab 1.087695 -2.175121 0.721147
\par \tab\tab 2.730441  3.725990 0.721148
\par \tab\tab 2.740741 -2.175321 0.721147
\par \tab\tab 2.740741 -2.175321 0.600000
\par \tab\tab 2.730441  3.725990 0.600000
\par \tab\}
\par \tab Indices
\par \tab\{
\par \tab\tab 0,1,2;,
\par \tab\tab 3,2,1;,
\par \tab\tab 4,5,6;,
\par \tab\tab 6,7,4;,
\par \tab\tab 8,9,10;,
\par \tab\tab 8,10,11;,
\par \tab\tab 12,13,14;,
\par \tab\tab 14,15,12;;
\par \tab\}
\par \}
\par 
\par #####
\par # File
\par # Syntax: File name colour \{ "filename" \}
\par File MyFile 70FFFFFF
\par \{
\par \tab "C:\\Files\\MyXfile.x"
\par \}
\par 
\par #####
\par # Group
\par # Syntax: Group name colour \{ AnyObject ... \}
\par Group Axes FFFFFFFF
\par \{
\par \tab Line X\tab FFFF0000 \{ 0 0 0 1 0 0 \}
\par \tab Line Y\tab FF00FF00 \{ 0 0 0 0 1 0 \}
\par \tab Line Z\tab FF0000FF \{ 0 0 0 0 0 1 \}
\par \}
\par 
\par #####
\par # GroupCyclic
\par # Syntax: GroupCyclic name colour \{ style frames_per_second AnyObject ... \}
\par # style: 0 = start->end
\par #        1 = end->start
\par #        2 = ping_pong
\par GroupCyclic MyGroupCyclic 00000000
\par \{
\par \tab 0
\par \tab 8
\par \tab Line frame1 FFFF0000 \{ 0 0 0  5  0  0 \}
\par \tab Line frame2 FFFF0000 \{ 0 0 0  4  4  0 \}
\par \tab Line frame3 FFFF0000 \{ 0 0 0  0  5  0 \}
\par \tab Line frame4 FFFF0000 \{ 0 0 0 -4  4  0 \}
\par \tab Line frame5 FFFF0000 \{ 0 0 0 -5  0  0 \}
\par \tab Line frame6 FFFF0000 \{ 0 0 0 -4 -4  0 \}
\par \tab Line frame7 FFFF0000 \{ 0 0 0  0 -5  0 \}
\par \tab Line frame8 FFFF0000 \{ 0 0 0  4 -4  0 \}
\par \}\f1 
\par }
175
Scribble175
Object Type Modifiers
Modifiers;Object Type Modifiers;


mainbrowsesequence:000060
Writing



FALSE
14
{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 Arial;}{\f1\fnil Arial;}{\f2\fnil\fprq1\fcharset0 Courier New;}}
{\colortbl ;\red0\green0\blue255;\red0\green128\blue0;\red128\green0\blue0;}
\viewkind4\uc1\pard\cf1\lang2057\b\fs32 Object Type Modifiers\cf0\lang1033\b0\f1\fs20 
\par 
\par \pard\li720\lang2057\f0 Object type modifiers are optional components of an \cf2\strike object type definition\cf3\strike0\{linkID=20\}\cf0 . Supported modifiers are:
\par \pard\lang1033\f1 
\par \pard\li1400\cf2\strike\f2 Transform\cf3\strike0\{linkID=177\}\cf0 
\par \cf2\strike Position\cf3\strike0\{linkID=179\}\cf0 
\par \cf2\strike Orientation\cf3\strike0\{linkID=189\}\cf0 
\par \cf2\strike Animation\cf3\strike0\{linkID=199\}\cf0 
\par \cf2\strike Wire\cf3\strike0\{linkID=209\}\cf0\f1 
\par \cf2\strike\f2 Hidden\cf3\strike0\{linkID=219\}\cf0 
\par 
\par }
177
Scribble177
Transform
Transform;


objecttypemodifiersequence:000010
Writing



FALSE
39
{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 Arial;}{\f1\fnil Arial;}{\f2\fnil\fprq1\fcharset0 Courier New;}}
{\colortbl ;\red0\green0\blue255;\red0\green128\blue0;\red128\green0\blue0;}
\viewkind4\uc1\pard\cf1\lang2057\b\fs32 Transform\cf0\lang1033\b0\f1\fs20 
\par 
\par \pard\li700\cf2\lang2057\strike\f0 Object type modifier\cf3\strike0\{linkID=175\}\cf0  describing the object to parent transform. This 4x4 matrix converts points in object space to points in the space of the object's parent or world space for objects that aren't nested.
\par \pard\i 
\par \b\i0 Syntax:
\par \b0\tab 
\par \pard\li720\lang1033\f2 Transform
\par \{
\par \pard\li1400 xx xy xz xw
\par yx yy yz yw
\par zx zy zz zw
\par wx wy wz ww
\par \pard\li720\}
\par \pard\lang2057\tab\f0 
\par \b Remarks:\b0 
\par 
\par \pard\li680 Vertices of the object have the form: V = \{ x y z w \} where w = 1.
\par \pard 
\par \b Example:
\par 
\par \pard\li680\lang1033\b0\f2 #####
\par # BoxWHD
\par # Syntax: BoxWHD name colour \{ w h d \}
\par BoxWHD MyBoxWHD FFFFFF00
\par \{
\par \pard\li1400 1 1.5 2
\par Transform
\par \{
\par \pard\li2100 1  0  0  0\tab # Rotation of 90 degrees about the X axis
\par 0  0  1  0\tab #
\par 0 -1  0  0\tab #
\par 2 -1  3  1\tab # Positioned at 2,-1,3
\par \pard\li1400\}
\par \pard\li680\}
\par \f1 
\par \pard\f2 
\par }
179
Scribble179
Position
Position;


objecttypemodifiersequence:000020
Writing



FALSE
34
{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 Arial;}{\f1\fnil Arial;}{\f2\fnil\fprq1\fcharset0 Courier New;}}
{\colortbl ;\red0\green0\blue255;\red0\green128\blue0;\red128\green0\blue0;}
\viewkind4\uc1\pard\cf1\lang2057\b\fs32 Position\cf0\lang1033\b0\f1\fs20 
\par 
\par \pard\li700\cf2\lang2057\strike\f0 Object type modifier\cf3\strike0\{linkID=175\}\cf0  describing the position of the object.
\par \pard\i 
\par \b\i0 Syntax:
\par \b0\tab 
\par \pard\li720\lang1033\f2 Position
\par \{
\par \pard\li1400 x y z
\par \pard\li720\}
\par \pard\lang2057\tab\f0 
\par \b Remarks:\b0 
\par \pard\li1380 
\par \pard\li720 Vertices of the object have \{x y z\} added to them
\par \pard 
\par \b Example:
\par 
\par \pard\li680\lang1033\b0\f2 #####
\par # BoxWHD
\par # Syntax: BoxWHD name colour \{ w h d \}
\par BoxWHD MyBoxWHD FFFFFF00
\par \{
\par \pard\li1400 1 1.5 2
\par Position
\par \{
\par \pard\li2100 2 -1  3 # Positioned at 2,-1,3
\par \pard\li1400\}
\par \pard\li680\}
\par \f1 
\par \pard\f2 
\par \f1 
\par }
189
Scribble189
Orientation
Orientation;Rotate;


objecttypemodifiersequence:000030
Writing



FALSE
44
{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 Arial;}{\f1\fnil Arial;}{\f2\fnil\fprq1\fcharset0 Courier New;}}
{\colortbl ;\red0\green0\blue255;\red0\green128\blue0;\red128\green0\blue0;}
\viewkind4\uc1\pard\cf1\lang2057\b\fs32 Orientation\cf0\lang1033\b0\f1\fs20 
\par 
\par \pard\li700\cf2\lang2057\strike\f0 Object type modifier\cf3\strike0\{linkID=175\}\cf0  describing the orientation of the object. Contains four elements of a quaternion.
\par \pard\i 
\par \b\i0 Syntax:
\par \b0\tab 
\par \pard\li720\lang1033\f2 Orientation
\par \{
\par \pard\li1400 x y z w
\par \pard\li720\}
\par \pard\lang2057\tab\f0 
\par \b Remarks:\b0 
\par 
\par \pard\li700 x, y, z are the axis components of the quaternion given by:
\par 
\par \pard\li1380 x = sin( theta / 2 ) * axis.x
\par y = sin( theta / 2 ) * axis.y
\par z = sin( theta / 2 ) * axis.z
\par 
\par \pard\li720 w is the scalar component of the quaternion given by:
\par \pard\li1380 w = cos( theta / 2 )
\par \pard\li680 
\par where 'axis' is a normalised vector describing the axis of rotation and 'theta' is the angle of rotation.
\par \pard 
\par \b Example:
\par 
\par \pard\li680\lang1033\b0\f2 #####
\par # BoxWHD
\par # Syntax: BoxWHD name colour \{ w h d \}
\par BoxWHD MyBoxWHD FFFFFF00
\par \{
\par \pard\li1400 1 1.5 2
\par Orientation
\par \{
\par \pard\li2100 0.544 0.102 0.158 0.817 # A rotation about the axis [0.544 0.102 0.158] by 1.228 radians
\par \pard\li1400\}
\par \pard\li680\}
\par \f1 
\par \pard\f2 
\par \f1 
\par 
\par }
199
Scribble199
Animation
Animation;Start Animations;


objecttypemodifiersequence:000040
Writing



FALSE
57
{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 Arial;}{\f1\fnil Arial;}{\f2\fnil\fprq1\fcharset0 Courier New;}}
{\colortbl ;\red0\green0\blue255;\red0\green128\blue0;\red128\green0\blue0;}
\viewkind4\uc1\pard\cf1\lang2057\b\fs32 Animation\cf0\lang1033\b0\f1\fs20 
\par 
\par \pard\li700\cf2\lang2057\strike\f0 Object type modifier\cf3\strike0\{linkID=175\}\cf0  describing a simple animation for the object. The animation consists of a linear and angular velocity. Animations are activated via the "Data\\\cf2\strike Start Animations\cf3\strike0\{linkID=560\}\cf0 " menu option.
\par \pard\i 
\par \b\i0 Syntax:
\par \b0\tab 
\par \pard\li700\lang1033\f2 Animation
\par \{
\par \pard\li1400 style
\par period
\par linear_velocity
\par axis_of_rotation
\par angular_speed
\par \pard\li700\}\f1 
\par \lang2057\f2\tab\f0 
\par \pard\b Remarks:\b0 
\par 
\par \pard\li700\i style:
\par \pard\li1400\i0 Style of animation:
\par \pard\li2140\i 0 = \lang1033\i0\f2 NoAnimation
\par 1 = PlayOnce
\par 2 = PlayReverse
\par 3 = PingPong
\par 4 = PlayContinuous
\par \pard\li680\lang2057\i\f0 period:
\par \pard\fi20\li1400\i0 The period of the animation in seconds.
\par 
\par \pard\li720\i Linear_velocity:
\par \pard\li1400\i0 A vector describing the velocity in m/s
\par 
\par \pard\li720 Axis_of_rotation:
\par \pard\li1400 The axis to rotate about
\par 
\par \pard\li720\i angular_speed:\i0 
\par \pard\li1400 The angular speed in rad/s
\par \pard 
\par \b Example:
\par \lang1033\b0\f1 
\par \pard\li680\f2\tab #####
\par # BoxWHD
\par # Syntax: BoxWHD name colour \{ w h d \}
\par BoxWHD MyBoxWHD FFFFFF00
\par \{
\par \pard\li1400 1 1.5 2
\par \pard\li1380 Animation
\par \{
\par \pard\li2100 3 \tab # PingPong
\par 2.0\tab # Period in seconds
\par 0 1 0\tab # Linear velocity in m/s
\par 1 0 0\tab # Axis of rotation
\par 3.0\tab # Angular speed in rad/s
\par \pard\li1380\}
\par \pard\li700\lang2057\f0\}
\par \lang1033\f1 
\par }
209
Scribble209
Wire



objecttypemodifiersequence:000050
Writing



FALSE
19
{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 Arial;}{\f1\fnil Arial;}{\f2\fnil\fprq1\fcharset0 Courier New;}}
{\colortbl ;\red0\green0\blue255;\red0\green128\blue0;\red128\green0\blue0;}
\viewkind4\uc1\pard\cf1\lang2057\b\fs32 Wire\cf0\lang1033\b0\f1\fs20 
\par 
\par \pard\li700\cf2\lang2057\strike\f0 Object type modifier\cf3\strike0\{linkID=175\}\cf0  that renders the object initially in wire frame. The wire frame state of the object is overwritten by the wire frame options in the \cf2\strike data window\cf3\strike0\{linkID=760\}\cf0 .
\par \pard 
\par \b Example:
\par \lang1033\b0\f1 
\par \pard\li680\f2\tab #####
\par # BoxWHD
\par # Syntax: BoxWHD name colour \{ w h d \}
\par BoxWHD MyBoxWHD FFFFFF00
\par \{
\par \pard\li1400 1 1.5 2
\par \pard\li1380 Wire
\par \pard\li700\lang2057\f0\}
\par \lang1033\f1 
\par \pard 
\par }
219
Scribble219
Hidden
Hidden;


objecttypemodifiersequence:000060
Writing



FALSE
19
{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 Arial;}{\f1\fnil Arial;}{\f2\fnil\fprq1\fcharset0 Courier New;}}
{\colortbl ;\red0\green0\blue255;\red0\green128\blue0;\red128\green0\blue0;}
\viewkind4\uc1\pard\cf1\lang2057\b\fs32 Hidden\cf0\lang1033\b0\f1\fs20 
\par 
\par \pard\li700\cf2\lang2057\strike\f0 Object type modifier\cf3\strike0\{linkID=175\}\cf0  that renders the object initially hidden. The hidden state of the object is overwritten by the visible options in the \cf2\strike data window\cf3\strike0\{linkID=760\}\cf0 .
\par \pard 
\par \b Example:
\par \lang1033\b0\f1 
\par \pard\li680\f2\tab #####
\par # BoxWHD
\par # Syntax: BoxWHD name colour \{ w h d \}
\par BoxWHD MyBoxWHD FFFFFF00
\par \{
\par \pard\li1400 1 1.5 2
\par \pard\li1380 Hidden
\par \pard\li700\lang2057\f0\}
\par \lang1033\f1 
\par \pard 
\par }
300
Scribble300
New Object
New Objects;


menusequence:000010
Writing



FALSE
8
{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 Arial;}{\f1\fnil Arial;}}
{\colortbl ;\red0\green0\blue255;\red0\green0\blue0;\red0\green128\blue0;\red128\green0\blue0;}
\viewkind4\uc1\pard\cf1\lang2057\b\fs32 New Object\cf0\lang1033\b0\f1\fs20 
\par 
\par \pard\li700\lang2057\f0 This dialog box allows \cf2 LineDrawer\cf0\lang1033\'99 \cf3\strike objects\cf4\strike0\{linkID=20\}\cf0  to be added via the user interface. Any valid \cf3\lang2057\strike LineDrawer\lang1033\'99 script\cf4\strike0\{linkID=15\}\cf0  can be added here.
\par 
\par \b Tip\b0 : Use Ctrl + Tab to insert tab characters.\f1 
\par }
310
Scribble310
Open
Loading;Open;


menusequence:000020
Writing



FALSE
6
{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 Arial;}{\f1\fnil Arial;}}
{\colortbl ;\red0\green0\blue255;\red0\green128\blue0;\red128\green0\blue0;}
\viewkind4\uc1\pard\cf1\lang2057\b\fs32 Open\cf0\lang1033\b0\f1\fs20 
\par 
\par \pard\li720\lang2057\f0 Prompts the user to open a text file containing \cf2\strike LineDrawer\lang1033\'99 script\cf3\strike0\{linkID=15\}\cf0 . On opening a file, any existing data is cleared. To open a file without clearing existing data see "\cf2\strike Additive Open\cf3\strike0\{linkID=320\}\cf0 ".\f1 
\par }
320
Scribble320
Additive Open
Additive Open;Loading;Open;


menusequence:000030
Writing



FALSE
7
{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 Arial;}{\f1\fnil Arial;}}
{\colortbl ;\red0\green0\blue255;\red0\green128\blue0;\red128\green0\blue0;}
\viewkind4\uc1\pard\cf1\lang2057\b\fs32 Additive Open\cf0\lang1033\b0\f1\fs20 
\par 
\par \pard\li720\lang2057\f0 Prompts the user to open a text file containing \cf2\strike LineDrawer\lang1033\'99 script\cf3\strike0\{linkID=15\}\cf0 . Objects read from the file are added to any existing data. To open a file and automatically clear any existing data see "\cf2\strike Open\cf3\strike0\{linkID=310\}\cf0 ".\f1 
\par \pard 
\par }
330
Scribble330
Save
Saving;


menusequence:000040
Writing



FALSE
6
{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 Arial;}{\f1\fnil Arial;}}
{\colortbl ;\red0\green0\blue255;}
\viewkind4\uc1\pard\cf1\lang2057\b\fs32 Save\cf0\lang1033\b0\f1\fs20 
\par 
\par \pard\li700\lang2057\f0 Saves the currently loaded data to a text file. If the data has not been saved before then the user is prompted for a filename.\lang1033\f1 
\par }
340
Scribble340
Save As
Saving;


menusequence:000050
Writing



FALSE
7
{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 Arial;}{\f1\fnil Arial;}}
{\colortbl ;\red0\green0\blue255;}
\viewkind4\uc1\pard\cf1\lang2057\b\fs32 Save As\cf0\lang1033\b0\f1\fs20 
\par 
\par \pard\li700\lang2057\f0 Prompts the user for a filename and then saves the currently loaded data.\lang1033\f1 
\par \pard 
\par }
350
Scribble350
Set Error Output
Error Output;


menusequence:000060
Writing



FALSE
6
{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 Arial;}{\f1\fnil Arial;}}
{\colortbl ;\red0\green0\blue255;\red0\green0\blue0;}
\viewkind4\uc1\pard\cf1\lang2057\b\fs32 Set Error Output\cf0\lang1033\b0\f1\fs20 
\par 
\par \pard\li720\lang2057\f0 Allows the user to set \cf2 LineDrawer\cf0\lang1033\'99\cf2\lang2057 's behaviour when reporting an error. Error dialog boxes are displayed by default, however, errors can also be logged to a file or suppressed entirely. \cf0\lang1033\f1 
\par }
360
Scribble360
Run Plugin
Plugins;Run Plugin;


menusequence:000070
Writing



FALSE
6
{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 Arial;}{\f1\fnil Arial;}}
{\colortbl ;\red0\green0\blue255;\red0\green128\blue0;\red128\green0\blue0;\red0\green0\blue0;}
\viewkind4\uc1\pard\cf1\lang2057\b\fs32 Run Plugin\cf0\lang1033\b0\f1\fs20 
\par 
\par \pard\li720\lang2057\f0 This option runs plugin dll's created using the \cf2\strike LineDrawer\lang1033\'99\lang2057  SDK\cf3\strike0\{linkID=2015\}\cf4 . For details, please \cf2\strike contact me\cf3\strike0\{linkID=2020\}\cf4 .\cf0\lang1033\f1 
\par }
400
Scribble400
Jump To
Jump To;Origin;Reset;Reset View;


menusequence:000080
Writing



FALSE
14
{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 Arial;}{\f1\fnil Arial;}}
{\colortbl ;\red0\green0\blue255;}
\viewkind4\uc1\pard\cf1\lang2057\b\fs32 Jump To\cf0\lang1033\b0\f1\fs20 
\par 
\par \pard\fi-1020\li1700\tx1720\lang2057\i\f0 Origin:
\par \pard\li1380\i0 Resets the camera to the original position based on the bounding box of the loaded objects.
\par \pard\fi-1020\li1700\tx1720 
\par \i Visible:
\par \pard\li1400\i0 Positions the camera so that all visible objects can be seen.
\par \pard\fi-1020\li1700\tx1720 
\par \i Selected:
\par \pard\li1400\i0 Positions the camera so that the selected objects are visible.\b\fs24 
\par \pard\lang1033\b0\f1\fs20 
\par }
410
Scribble410
Show Origin, Axis, Focus Point
Axis Overlay;Focus Point;Origin;


menusequence:000090
Writing



FALSE
15
{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 Arial;}{\f1\fnil Arial;}}
{\colortbl ;\red0\green0\blue255;}
\viewkind4\uc1\pard\cf1\lang2057\b\fs32 Show Origin, Axis, Focus Point\cf0\lang1033\b0\f1\fs20 
\par 
\par \pard\fi-1560\li2240\tx2280\lang2057\i\f0 Origin:
\par \pard\li1380\i0 Displays an axis at the world 0, 0, 0 co-ordinate. The axis is colour coded where X = red, Y = green, and Z = blue.
\par \pard\fi-1560\li2240\tx2280 
\par \i Axis:\b\i0\fs24 
\par \pard\li1400\b0\fs20 Displays an overlay axis in the lower right hand corner.
\par \pard\fi-1560\li2240\tx2280 
\par \i Focus Point:
\par \pard\li1380\i0 Displays an axis at the focus point of the camera. This is often a helpful reference point when manipulating an object with the mouse.\b\fs24 
\par \pard\fi-1560\li2240\tx2280\lang1033\b0\f1\fs20 
\par \pard 
\par }
420
Scribble420
Lock
Lock;


menusequence:000100
Writing



FALSE
6
{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 Arial;}{\f1\fnil Arial;}}
{\colortbl ;\red0\green0\blue255;}
\viewkind4\uc1\pard\cf1\lang2057\b\fs32 Lock\cf0\lang1033\b0\f1\fs20 
\par 
\par \pard\li720\lang2057\f0 Allows the user to prevent accidental translations, rotations, or zoom of the camera.\lang1033\f1 
\par }
430
Scribble430
View
Camera Properties;View;


menusequence:000110
Writing



FALSE
18
{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 Arial;}{\f1\fnil Arial;}{\f2\fnil\fcharset2 Symbol;}}
{\colortbl ;\red0\green0\blue255;}
\viewkind4\uc1\pard\cf1\lang2057\b\fs32 View\cf0\lang1033\b0\f1\fs20 
\par 
\par \pard\fi-680\li1400\tx1400\lang2057\i\f0 Origin:\b\i0\fs24 
\par \pard\li1380\tx1380\b0\fs20 Orientates the camera so that it is "looking" at the centre of the bounding box that encloses all of the loaded objects\lang1033\f1 
\par \pard\fi-680\li1400\tx1400\lang2057\b\f0\fs24 
\par \b0\i\fs20 Top, Bottom, Left, Right, Front, Back:
\par \pard\li1380\tx1380\i0 Positions and orientates the camera to view the bounding box of the loaded data from various common positions
\par \pard\fi-680\li1400\tx1400 
\par \i Properties:
\par \pard\fi-20\li1380\tx1380\i0 Opens a dialog box that allows the exact setting of the camera's:
\par \pard{\pntext\f2\'B7\tab}{\*\pn\pnlvlblt\pnf2\pnindent0{\pntxtb\'B7}}\fi-180\li1740\tx1740 Position
\par {\pntext\f2\'B7\tab}Orientation
\par {\pntext\f2\'B7\tab}Near clip plane distance
\par {\pntext\f2\'B7\tab}Far clip plane distance
\par {\pntext\f2\'B7\tab}Cull mode\lang1033\f1 
\par {\pntext\f2\'B7\tab}}
440
Scribble440
Inertial Camera
Controls;Flying Camera;Inertial Camera;Keyboard Navigation;Navigation;


menusequence:000120
Writing



FALSE
6
{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 Arial;}{\f1\fnil Arial;}}
{\colortbl ;\red0\green0\blue255;\red0\green128\blue0;\red128\green0\blue0;}
\viewkind4\uc1\pard\cf1\lang2057\b\fs32 Inertial Camera\cf0\lang1033\b0\f1\fs20 
\par 
\par \pard\li680\lang2057\f0 Toggles the inertial camera on / off. A complete description of the inertial camera is given \cf2\strike here\cf3\strike0\{linkID=750\}\cf0 .\lang1033\f1 
\par }
500
Scribble500
Show Selection
Selection;


menusequence:000130
Writing



FALSE
6
{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 Arial;}{\f1\fnil Arial;}}
{\colortbl ;\red0\green0\blue255;\red0\green128\blue0;\red128\green0\blue0;}
\viewkind4\uc1\pard\cf1\lang2057\b\fs32 Show Selection\cf0\lang1033\b0\f1\fs20 
\par 
\par \pard\li720\lang2057\f0 Toggles the visibility of the selection box. The selection box shows the bounding box of the objects selected in the \cf2\strike data window\cf3\strike0\{linkID=760\}\cf0 .\lang1033\f1 
\par }
510
Scribble510
Clear
Clear;Reset;


menusequence:000140
Writing



FALSE
8
{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 Arial;}{\f1\fnil Arial;}}
{\colortbl ;\red0\green0\blue255;\red0\green128\blue0;\red128\green0\blue0;}
\viewkind4\uc1\pard\cf1\lang2057\b\fs32 Clear\cf0\lang1033\b0\f1\fs20 
\par 
\par \pard\li720\lang2057\f0 Clears all currently loaded objects.
\par 
\par \b See also:\b0  \cf2\strike Auto Clear\cf3\strike0\{linkID=520\}\cf0\lang1033\f1 
\par }
520
Scribble520
Auto Clear
Auto Clear;


menusequence:000150
Writing



FALSE
6
{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 Arial;}{\f1\fnil Arial;}}
{\colortbl ;\red0\green0\blue255;\red0\green128\blue0;\red128\green0\blue0;}
\viewkind4\uc1\pard\cf1\lang2057\b\fs32 Auto Clear\cf0\lang1033\b0\f1\fs20 
\par 
\par \pard\li720\lang2057\f0 Auto clear is useful mostly in conjunction with the \cf2\strike Network Listener\cf3\strike0\{linkID=540\}\cf0 . The period specified is the maximum length of time between successive object definitions. If an object definition is received after the timeout, then all data is cleared before the new object is created and displayed.\lang1033\f1 
\par }
530
Scribble530
Auto Refresh
Auto Refresh;


menusequence:000160
Writing



FALSE
9
{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 Arial;}{\f1\fnil Arial;}}
{\colortbl ;\red0\green0\blue255;\red0\green0\blue0;}
\viewkind4\uc1\pard\cf1\lang2057\b\fs32 Auto Refresh\cf0\lang1033\b0\f1\fs20 
\par 
\par \pard\li720\lang2057\f0 Auto refresh is useful mostly in conjunction with source text files that are periodically updated. The specified refresh frequency is the length of time that \cf2 LineDrawer\cf0\lang1033\'99 waits between testing the last modified time of each of the source files. If a file has been modified \cf2\lang2057 LineDrawer\cf0\lang1033\'99 clears existing objects and reloads the objects from all of the source files.
\par 
\par "Auto re-centre focus point" causes the camera to be moved by the amount that the newly loaded data has moved. This is done by using the difference in bounding box centres between the old and new data.\f1 
\par \pard 
\par }
540
Scribble540
Network Listener
Network Listener;TCP Listener;


menusequence:000170
Writing



FALSE
8
{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 Arial;}{\f1\fnil Arial;}}
{\colortbl ;\red0\green0\blue255;\red0\green0\blue0;\red0\green128\blue0;\red128\green0\blue0;}
\viewkind4\uc1\pard\cf1\lang2057\b\fs32 Network Listener\cf0\lang1033\b0\f1\fs20 
\par 
\par \pard\li720\lang2057\f0 This dialog box allows the user to specify an IP address and port number that \cf2 LineDrawer\cf0\lang1033\'99 should listen on for incoming connections. \cf2\lang2057 Object definitions can then be \cf0\lang1033 streamed into \cf2\lang2057 LineDrawer\cf0\lang1033\'99 through this connection\cf2\lang2057 . The sample period is the frequency at which LineDrawer\cf0\lang1033\'99 looks for waiting data. The receive buffer size is the maximum size of a packet that \cf2\lang2057 LineDrawer\cf0\lang1033\'99 will receive.
\par 
\par \b Note:\b0  this functionality is still under \cf3\strike development\cf4\strike0\{linkID=2015\}\cf0 .\f1 
\par }
550
Scribble550
Start Cyclic Objects
GroupCyclic;Start GroupCyclic;


menusequence:000180
Writing



FALSE
6
{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 Arial;}{\f1\fnil Arial;}}
{\colortbl ;\red0\green0\blue255;\red0\green128\blue0;\red128\green0\blue0;}
\viewkind4\uc1\pard\cf1\lang2057\b\fs32 Start Cyclic Objects\cf0\lang1033\b0\f1\fs20 
\par 
\par \pard\li720\lang2057\f0 Toggles \cf2\strike GroupCyclic objects\cf3\strike0\{linkID=160\}\cf0  on / off. \lang1033\f1 
\par }
560
Scribble560
Start Animations
Animation;Start Animations;


menusequence:000190
Writing



FALSE
6
{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 Arial;}{\f1\fnil Arial;}}
{\colortbl ;\red0\green0\blue255;\red0\green128\blue0;\red128\green0\blue0;}
\viewkind4\uc1\pard\cf1\lang2057\b\fs32 Start Animations\cf0\lang1033\b0\f1\fs20 
\par 
\par \pard\li720\lang2057\f0 Toggles the animations for objects with \cf2\strike Animation object modifiers\cf3\strike0\{linkID=199\}\cf0  on / off.\lang1033\f1 
\par }
570
Scribble570
Data List
Data Window;


menusequence:000200
Writing



FALSE
7
{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 Arial;}{\f1\fnil Arial;}}
{\colortbl ;\red0\green0\blue255;\red0\green128\blue0;\red128\green0\blue0;}
\viewkind4\uc1\pard\cf1\lang2057\b\fs32 Data List\cf0\lang1033\b0\f1\fs20 
\par 
\par \pard\li680\lang2057\f0 Opens or brings into focus the Data window. A complete description of the Data window is given \cf2\strike here\cf3\strike0\{linkID=760\}\cf0 .\lang1033\f1 
\par \pard 
\par }
600
Scribble600
Wire frame
Wire frame;


menusequence:000210
Writing



FALSE
6
{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 Arial;}{\f1\fnil Arial;}}
{\colortbl ;\red0\green0\blue255;\red0\green128\blue0;\red128\green0\blue0;}
\viewkind4\uc1\pard\cf1\lang2057\b\fs32 Wire frame\cf0\lang1033\b0\f1\fs20 
\par 
\par \pard\li720\lang2057\f0 Toggles wire frame rendering of everything in the scene. This option is a property of the whole scene and does not effect the wire frame state for individual objects set in the \cf2\strike data window\cf3\strike0\{linkID=760\}\cf0 .\lang1033\f1 
\par }
610
Scribble610
Co-ordinates
Co-ordinates;


menusequence:000220
Writing



FALSE
6
{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 Arial;}{\f1\fnil Arial;}}
{\colortbl ;\red0\green0\blue255;\red0\green128\blue0;\red128\green0\blue0;}
\viewkind4\uc1\pard\cf1\lang2057\b\fs32 Co-ordinates\cf0\lang1033\b0\f1\fs20 
\par 
\par \pard\li700\lang2057\f0 Turns on / off the floating co-ordinates. A complete description of the floating co-ordinates is given \cf2\strike here\cf3\strike0\{linkID=860\}\cf0 .\lang1033\f1 
\par }
620
Scribble620
Render 2D
2D Rendering;3D Rendering;Orthographic Projection;Render 2D;


menusequence:000230
Writing



FALSE
6
{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 Arial;}{\f1\fnil Arial;}}
{\colortbl ;\red0\green0\blue255;\red0\green128\blue0;\red128\green0\blue0;}
\viewkind4\uc1\pard\cf1\lang2057\b\fs32 Render 2D\cf0\lang1033\b0\f1\fs20 
\par 
\par \pard\fi40\li680\lang2057\f0 Switches the projection between perspective (3D) and orthographic (2D). Often useful in conjunction with the floating \cf2\strike co-ordinates\cf3\strike0\{linkID=860\}\cf0  option.\lang1033\f1 
\par }
630
Scribble630
Right-handed
Left-handed Axis;Right-handed Axis;


menusequence:000240
Writing



FALSE
8
{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 Arial;}{\f1\fnil Arial;}}
{\colortbl ;\red0\green0\blue255;\red0\green0\blue0;\red0\green128\blue0;\red128\green0\blue0;}
\viewkind4\uc1\pard\cf1\lang2057\b\fs32 Right-handed\cf0\lang1033\b0\f1\fs20 
\par 
\par \pard\li720\lang2057\f0 Switches \cf2 LineDrawer\cf0\lang1033\'99's axis between right and left handed.
\par 
\par \b Note:\b0  This functionality is still under \cf3\strike development\cf4\strike0\{linkID=2015\}\cf0\f1 
\par }
640
Scribble640
Stereo View
3D Rendering;Stereo View;True 3D;


menusequence:000250
Writing



FALSE
6
{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 Arial;}{\f1\fnil Arial;}}
{\colortbl ;\red0\green0\blue255;\red0\green0\blue0;\red0\green128\blue0;\red128\green0\blue0;}
\viewkind4\uc1\pard\cf1\lang2057\b\fs32 Stereo View\cf0\lang1033\b0\f1\fs20 
\par 
\par \pard\li680\lang2057\f0 Switches \cf2 LineDrawer\cf0\lang1033\'99's rendering mode between normal and stereo view. A complete description of stereo view is given \cf3\strike here\cf4\strike0\{linkID=870\}\cf0 .\f1 
\par }
650
Scribble650
Lighting
Ambient;Attenuation;Directional Light;Lighting;Point Light;Spot light;


menusequence:000260
Writing



FALSE
6
{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 Arial;}{\f1\fnil Arial;}}
{\colortbl ;\red0\green0\blue255;\red0\green128\blue0;\red128\green0\blue0;}
\viewkind4\uc1\pard\cf1\lang2057\b\fs32 Lighting\cf0\lang1033\b0\f1\fs20 
\par 
\par \pard\fi-40\li700\lang2057\f0 Displays the lighting dialog. A complete description of the lighting dialog is given \cf2\strike here\cf3\strike0\{linkID=880\}\cf0 .\lang1033\f1 
\par }
700
Scribble700
Always On Top
Always On Top;


menusequence:000270
Writing



FALSE
6
{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 Arial;}{\f1\fnil Arial;}}
{\colortbl ;\red0\green0\blue255;\red0\green0\blue0;}
\viewkind4\uc1\pard\cf1\lang2057\b\fs32 Always On Top\cf0\lang1033\b0\f1\fs20 
\par 
\par \pard\li720\lang2057\f0 Makes \cf2 LineDrawer\cf0\lang1033\'99 always visible on the desktop.\f1 
\par }
710
Scribble710
Background Colour
Background Colour;


menusequence:000280
Writing



FALSE
6
{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 Arial;}{\f1\fnil Arial;}}
{\colortbl ;\red0\green0\blue255;}
\viewkind4\uc1\pard\cf1\lang2057\b\fs32 Background Colour\cf0\lang1033\b0\f1\fs20 
\par 
\par \pard\li680\lang2057\f0 Displays a colour dialog that allows the background colour to be changed.\lang1033\f1 
\par }
720
Scribble720
LineDrawer Help



menusequence:000290
Writing



FALSE
6
{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 Arial;}{\f1\fnil Arial;}}
{\colortbl ;\red0\green0\blue255;}
\viewkind4\uc1\pard\cf1\lang2057\b\fs32 LineDrawer Help\cf0\lang1033\b0\f1\fs20 
\par 
\par \pard\li700\lang2057\f0 This exceptionally helpful document :)\lang1033\f1 
\par }
730
Scribble730
About LineDrawer



menusequence:000300
Writing



FALSE
6
{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 Arial;}{\f1\fnil Arial;}}
{\colortbl ;\red0\green0\blue255;\red0\green128\blue0;\red128\green0\blue0;}
\viewkind4\uc1\pard\cf1\lang2057\b\fs32 About LineDrawer\lang1033\fs28\'99\cf0\b0\f1\fs20 
\par 
\par \pard\fi40\li680\lang2057\f0 What more can be \cf2\strike said\cf3\strike0\{linkID=2010\}\cf0 ?\lang1033\f1 
\par }
740
Scribble740
Trackball Mouse Navigation
Controls;Mouse Control;Navigation;Rotate;Trackball Mouse Control;Zoom;


mainbrowsesequence:000070
Writing



FALSE
21
{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 Arial;}{\f1\fnil Arial;}}
{\colortbl ;\red0\green0\blue255;\red0\green128\blue0;\red128\green0\blue0;\red0\green0\blue0;}
\viewkind4\uc1\pard\cf1\lang2057\b\fs32 Trackball Mouse Navigation\cf0\lang1033\b0\f1\fs20 
\par 
\par \lang2057\b\f0\fs24 Translation:
\par \pard\li700\b0\fs20 Holding the left mouse button down while dragging the mouse translates the camera perpendicular the direction that the camera is facing.
\par \pard 
\par \b\fs24 Rotation:
\par \pard\li720\b0\fs20 Holding the right mouse button down while dragging the mouse rotates the camera about the \cf2\strike focus point\cf3\strike0\{linkID=410\}\cf0 . The direction of rotation depends on the location of the mouse pointer within the \cf4 LineDrawer\cf0\lang1033\'99 main window when the right mouse button is pressed; toward the edges of the window causes the camera to roll about it's forward axis, towards the centre of the window causes the camera to pan about the focus point.\lang2057\b\fs24 
\par \pard\b0\fs20 
\par \b\fs24 Z Translation:
\par \pard\li700\b0\fs20 Holding the left and right mouse buttons down simultaneously and dragging the mouse causes the camera to move toward or away from the focus point. On a wheel mouse the wheel is also used for Z translation\b\fs24 
\par \pard 
\par Zoom:
\par \pard\li700\b0\fs20 Holding down 'shift' as well as the left and right mouse buttons simultaneously changes the zoom of the camera. A zoom of other than 100% is displayed in the title bar of the \cf4 LineDrawer\cf0\lang1033\'99 main window. \lang2057 On a wheel or three button mouse the middle button is used for zoom.\b\fs24 
\par \pard\li660 
\par \pard Z Translation vs. Zoom?:
\par \pard\li700\b0\fs20 To make an object appear larger in \cf4 LineDrawer\cf0\lang1033\'99 it is preferable to use Z translation which simply moves the camera closer to the object rather than zoom which reduces the horizontal and vertical angle of the camera frustum. Changing the camera frustum by a large amount introduces distortion in the rendered scene. However, if the object becomes too close to the camera it will be clipped by the camera's near clip plane. The near clip plane can be adjusted via the \cf2\strike view properties\cf3\strike0\{linkID=430\}\cf0  or zoom can be used to further magnify the object.
\par 
\par \f1 
\par }
750
Scribble750
Inertial Camera
Controls;Flying Camera;Inertial Camera;Keyboard Navigation;Navigation;


mainbrowsesequence:000080
Writing



FALSE
28
{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 Arial;}{\f1\fnil Arial;}}
{\colortbl ;\red0\green0\blue255;}
\viewkind4\uc1\pard\cf1\lang2057\b\fs32 Inertial Camera\cf0\lang1033\b0\f1\fs20 
\par 
\par \lang2057\f0 The inertial camera is a keyboard driven control method that models the camera as an object with mass that moves as a result of forces applied to it. The inertial camera is most useful for "flying" through a scene.
\par 
\par The generalised keyboard controls are:
\par 
\par \pard\fi-20\li700 Arrow keys for camera rotation
\par Shift + Arrow keys for camera translation
\par \pard 
\par More accurately, the keys for driving the inertial camera are:
\par 
\par \pard\fi-2160\li2820\tx2840\i Pitch Up / Down:\tab\i0 Arrow up / Arrow down\i 
\par Yaw Left / Right:\tab\i0 Arrow left / Arrow right\i 
\par Forward / Backward:\tab\i0 Shift + Arrow up / Shift + Arrow down
\par \i Left / Right:\i0\tab\tab Shift + Arrow left / Shift + Arrow right
\par \i Up / Down:\tab\tab '\i0 A' / 'Z'
\par \i Roll Left / Right:\tab\tab\i0 'X' / 'C'
\par \pard 
\par In addition, these keys increase the linear and angular acceleration of the camera:
\par 
\par \pard\fi-2080\li2800\tx2820\i 'Ctrl':\i0\tab Translation scaled by 5, Rotation scaled by 2
\par \i 'Caps Lock':\i0\tab Translation scaled by 15
\par \i 'Ctrl' + 'Caps Lock'\i0\tab Translation scaled by 75, Rotation scaled by 2
\par \pard 
\par The inertial camera is enabled / disabled via the Navigation menu.\lang1033\f1 
\par }
760
Scribble760
Data Window
Data;Data Window;Hierarchy;List View;Selection;Tree View;Visibility;Wire frame;


mainbrowsesequence:000090
Writing



FALSE
11
{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 Arial;}{\f1\fnil Arial;}}
{\colortbl ;\red0\green0\blue255;\red128\green0\blue0;\red0\green128\blue0;}
\viewkind4\uc1\pard\cf1\lang2057\b\fs32 Data Window\cf0\lang1033\b0\f1\fs20 
\par 
\par \pard\li680\lang2057\f0 Below is an example view of the data window. Click in various areas of the window to get a description of its functionality.
\par \lang1033\f1 
\par \cf2\strike\{bmc DataWindow.shg\}
\par 
\par \cf0\lang2057\b\strike0\f0 Note:\b0  There are a number of \cf3\strike keyboard shortcuts\cf2\strike0\{linkID=2000\}\cf0  for the data window\cf2\lang1033\strike\f1 
\par \pard\strike0 
\par }
770
Scribble770
Selection Mask




Writing



FALSE
6
{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 Arial;}{\f1\fnil Arial;}}
{\colortbl ;\red0\green0\blue255;}
\viewkind4\uc1\pard\cf1\lang2057\b\fs20 Selection Mask\cf0\lang1033\b0\f1 
\par 
\par \lang2057\f0 Objects in the list view are selected if they contain the string typed in here.\lang1033\f1 
\par }
780
Scribble780
Expand / Collapse Hierarchy
Collapse Tree View;Expand Tree View;



Writing



FALSE
6
{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 Arial;}{\f1\fnil Arial;}}
{\colortbl ;\red0\green0\blue255;}
\viewkind4\uc1\pard\cf1\lang2057\b\fs20 Expand / Collapse Hierarchy\cf0\lang1033\b0\f1 
\par 
\par \lang2057\f0 Completely expands or collapses the hierarchy for objects in the tree view. If nothing is selected then all objects are expanded / collapsed.\lang1033\f1 
\par }
790
Scribble790
Tree View




Writing



FALSE
6
{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 Arial;}{\f1\fnil Arial;}}
{\colortbl ;\red0\green0\blue255;}
\viewkind4\uc1\pard\cf1\lang2057\b\fs20 Tree View\cf0\lang1033\b0\f1 
\par 
\par \lang2057\f0 Displays the hierarchy of nested objects.\lang1033\f1 
\par }
800
Scribble800
List View




Writing



FALSE
6
{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 Arial;}{\f1\fnil Arial;}}
{\colortbl ;\red0\green0\blue255;\red0\green0\blue0;}
\viewkind4\uc1\pard\cf1\lang2057\b\fs20 List View\cf0\lang1033\b0\f1 
\par 
\par \lang2057\f0 Displays the properties of all objects currently loaded into \cf2 LineDrawer\cf0\lang1033\'99.\f1 
\par }
810
Scribble810
Visibility
Visibility;



Writing



FALSE
11
{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 Arial;}{\f1\fnil Arial;}}
{\colortbl ;\red0\green0\blue255;}
\viewkind4\uc1\pard\cf1\lang2057\b\fs20 Visibility\cf0\lang1033\b0\f1 
\par 
\par \lang2057\f0 These options allow individual objects to be hidden / shown. Select the objects to modify in the list view and then select one of:.
\par 
\par \pard\fi-2280\li2280\tx2260\i Hide / Show:\tab\i0 Hide / Show an object without changing the visibility of any nested objects.
\par \i Hide Sub / Show Sub:\tab\i0 Hide / Show an object and all of its nested objects.
\par \i Hide All / Show All:\tab\i0 Hide / Show all objects and their nested objects.
\par \i Toggle Visibility:\tab\i0 Invert the visibility of all selected objects.\lang1033\f1 
\par }
820
Scribble820
Wire frame
Wire frame;



Writing



FALSE
11
{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 Arial;}{\f1\fnil Arial;}}
{\colortbl ;\red0\green0\blue255;}
\viewkind4\uc1\pard\cf1\lang2057\b\fs20 Wire frame\cf0\lang1033\b0\f1 
\par 
\par \lang2057\f0 These options allow individual objects to be rendered in wire frame / solid. Select the objects to modify in the list view and then select one of:.
\par 
\par \pard\fi-2280\li2280\tx2280\i Wire / Solid:\tab\i0 Wire frame / solid an object without changing the visibility of any nested objects.
\par \i Wire Sub / Solid Sub:\tab\i0 Wire frame / solid an object and all of its nested objects.
\par \i Wire All / Solid All:\tab\i0 Wire frame / solid all objects and their nested objects.
\par \i Toggle Wire frame:\tab\i0 Toggle between solid and wire frame for all selected objects.\lang1033\f1 
\par }
830
Scribble830
Invert Selection




Writing



FALSE
6
{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 Arial;}{\f1\fnil Arial;}}
{\colortbl ;\red0\green0\blue255;}
\viewkind4\uc1\pard\cf1\lang2057\b\fs20 Invert Selection\cf0\lang1033\b0\f1 
\par 
\par \lang2057\f0 Selects all objects that are not currently selected in the list view.\lang1033\f1 
\par }
840
Scribble840
Delete Selected
Delete Objects;



Writing



FALSE
6
{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 Arial;}{\f1\fnil Arial;}}
{\colortbl ;\red0\green0\blue255;}
\viewkind4\uc1\pard\cf1\lang2057\b\fs20 Delete Selected\cf0\lang1033\b0\f1 
\par 
\par \lang2057\f0 Deletes objects currently selected in the list view.\lang1033\f1 
\par }
850
Scribble850
Edit Selected
Edit Objects;



Writing



FALSE
6
{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 Arial;}{\f1\fnil Arial;}}
{\colortbl ;\red0\green0\blue255;\red0\green128\blue0;\red128\green0\blue0;}
\viewkind4\uc1\pard\cf1\lang2057\b\fs20 Edit Selected\cf0\lang1033\b0\f1 
\par 
\par \lang2057\f0 Displays a dialog box containing the \cf2\strike LineDrawer\lang1033\'99\fs26  \lang2057\fs20 script\cf3\strike0\{linkID=15\}\cf0  used to generate the first selected object in the list view. Modifying this text will update the currently loaded version of the object.\lang1033\f1 
\par }
860
Scribble860
Co-ordinates
Co-ordinates;



Writing



FALSE
10
{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 Arial;}{\f1\fnil Arial;}}
{\colortbl ;\red0\green0\blue255;\red0\green128\blue0;\red128\green0\blue0;}
\viewkind4\uc1\pard\cf1\lang2057\b\fs32 Co-ordinates\cf0\lang1033\b0\f1\fs20 
\par 
\par \pard\li680\lang2057\f0 The co-ordinates option displays a floating window containing the co-ordinate of the mouse pointer in world space. This is calculated by projecting the screen position of the mouse pointer onto the near clipping plane of the camera frustum. For this reason, the co-ordinates option is most useful when the camera is orientated along one of the world space axes. Use the \cf2\strike preset views\cf3\strike0\{linkID=430\}\cf0  or the \cf2\strike view properties\cf3\strike0\{linkID=430\}\cf0  to do this.
\par 
\par \b Tip:\b0  Use the "\cf2\strike Render 2D\cf3\strike0\{linkID=620\}\cf0 " option so that an orthonormal projection is used.
\par 
\par \b Tip:\b0  Use the "\cf2\strike Lock\cf3\strike0\{linkID=420\}\cf0 " option to prevent accidental translations or rotations.\lang1033\f1 
\par }
870
Scribble870
Stereo View
3D Rendering;Stereo View;True 3D;



Writing



FALSE
10
{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 Arial;}{\f1\fnil Arial;}}
{\colortbl ;\red0\green0\blue255;\red128\green0\blue0;}
\viewkind4\uc1\pard\cf1\lang2057\b\fs32 Stereo View\cf0\lang1033\b0\f1\fs20 
\par 
\par \pard\fi-20\li700\lang2057\f0 Stereo view is an option that renders the scene using two cameras separated by a small amount and angled inwards. These two cameras show the scene as it would be viewed by the left and right eye. By focusing your eyes "through" the monitor it is possible to see a third view in the centre that is the scene in true 3D.
\par 
\par \cf2\lang1033\strike\f1\{bmc \lang2057\f0 Stereo View.\lang1033\f1 shg\}\cf0\lang2057\strike0\f0 
\par 
\par \b Note: \b0 If you go cross-eyed (i.e. focus in front of your monitor) you will get some 3D effect although the image will be "inside out".\lang1033\f1 
\par }
880
Scribble880
Lighting
Ambient;Attenuation;Directional Light;Lighting;Point Light;Spot light;


mainbrowsesequence:000110
Writing



FALSE
8
{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 Arial;}{\f1\fnil Arial;}}
{\colortbl ;\red0\green0\blue255;\red128\green0\blue0;}
\viewkind4\uc1\pard\cf1\lang2057\b\fs32 Lighting\cf0\lang1033\b0\f1\fs20 
\par 
\par \pard\li680\lang2057\f0 Below is an example view of the lighting dialog. Click in various areas of the window to get a description of its functionality.
\par \lang1033\f1 
\par \cf2\strike\{bmc \lang2057\f0 Lighting\lang1033\f1 Window.shg\}\cf0\strike0 
\par }
890
Scribble890
Light Source Type




Writing



FALSE
16
{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 Arial;}{\f1\fnil Arial;}}
{\colortbl ;\red0\green0\blue255;}
\viewkind4\uc1\pard\cf1\lang2057\b\fs20 Light Source Type\cf0\lang1033\b0\f1 
\par 
\par \lang2057\f0 The available light sources:
\par 
\par \pard\fi-1700\li1700\tx1720 Ambient:\tab This is a flat, single colour, light source. Useful mainly for wire frame scenes. The only property of the ambient light source is the ambient colour.
\par 
\par These light source types also include the ambient light source. Their colour is set by the diffuse colour property
\par 
\par Point:\tab Point light source.
\par Spot:\tab Conical light source.
\par Directional:\tab Parallel light source.
\par 
\par \b Note:\b0  All light sources are relative to and move with the camera.\lang1033\f1 
\par }
900
Scribble900
Light Source Colour




Writing



FALSE
6
{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 Arial;}{\f1\fnil Arial;}}
{\colortbl ;\red0\green0\blue255;}
\viewkind4\uc1\pard\cf1\lang2057\b\fs20 Light Source Colour\cf0\lang1033\b0\f1 
\par 
\par \lang2057\f0 Control the colour and intensity of the selected light source. All light sources have an ambient component. Point, Spot, and Directional lights use the diffuse colour. The specular colour is not currently used.\lang1033\f1 
\par }
910
Scribble910
Light Source Properties




Writing



FALSE
14
{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 Arial;}{\f1\fnil Arial;}}
{\colortbl ;\red0\green0\blue255;}
\viewkind4\uc1\pard\cf1\lang2057\b\fs20 Light Source Properties\cf0\lang1033\b0\f1 
\par 
\par \pard\fi-1700\li1700\tx1740\lang2057\i\f0 Position:\i0\tab Position of the light relative to the camera. Applies to Point and Spot light sources.
\par \i Direction:\i0\tab Direction of the light source relative to the camera. Applies to Spot and Directional light sources.
\par \i Inner Solid Angle:\i0\tab The solid angle of the maximum intensity light cone for a Spot light source.
\par \i Outer Solid Angle:\i0\tab The solid angle at which the Spot light source fades out.
\par \i Range:\i0\tab The maximum distance that a light source can illuminate. Applies to Point and Spot light sources.
\par \i Fall Off:\i0\tab The change in light intensity between the inner and outer cones of the Spot light source.
\par \i Attenuation:\i0\tab The change in light intensity over distance calculated using:
\par \pard\li2240\tx1740 Attenuation = 1 / (atten0 + atten1 x distance + atten2 x distance\lang1033\fs26\'b2\lang2057\fs20 )
\par \pard\li1680\tx1740 Applies to Point and Spot light sources\lang1033\f1 
\par }
920
Scribble920
Preview Light Settings




Writing



FALSE
6
{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 Arial;}{\f1\fnil Arial;}}
{\colortbl ;\red0\green0\blue255;}
\viewkind4\uc1\pard\cf1\lang2057\b\fs20 Preview Light Settings\cf0\lang1033\b0\f1 
\par 
\par \lang2057\f0 Used to preview the current light settings. Clicking 'Cancel' will restore the old light settings.\lang1033\f1 
\par }
2000
Scribble2000
Keyboard Shortcuts
Controls;Keyboard Shortcuts;Shortcuts;


mainbrowsesequence:000100
Writing



FALSE
24
{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 Arial;}{\f1\fnil Arial;}}
{\colortbl ;\red0\green0\blue255;\red0\green0\blue0;\red0\green128\blue0;\red128\green0\blue0;}
\viewkind4\uc1\pard\cf1\lang2057\b\fs32 Keyboard Shortcuts\cf0\lang1033\b0\f1\fs20 
\par 
\par \pard\li700\lang2057\f0 The following are keyboard shortcuts for the \cf2 LineDrawer\cf0\lang1033\'99 main window:
\par \lang2057 
\par \pard\li1380\tx2820\i Numpad ' 0 ':\i0\tab Set window focus to the \cf3\strike data window\cf4\strike0\{linkID=760\}\cf0 .
\par \i SPACE:\i0\tab Display the \cf3\strike data window\cf4\strike0\{linkID=760\}\cf0 .
\par ' F5 '\tab Refresh the window by reloading objects from source files
\par 'Esc':\tab Close \cf2 LineDrawer\cf0\lang1033\'99\lang2057 .
\par \pard\li700\lang1033\f1 
\par \lang2057\f0 The following are keyboard shortcuts for the \cf3\strike data window\cf4\strike0\{linkID=760\}\cf0 :
\par 
\par \pard\li1380\tx2840\i Numpad ' 0 ':\i0\tab Set window focus to the main window.
\par \i ' F4 ':\i0\tab Set focus to the selection mask edit box.
\par 'Esc':\tab Close the data view.
\par \pard\li700 
\par The following are keyboard shortcuts while the list view or tree view has focus:
\par 
\par \pard\li1380\tx2820\i Ctrl + ' A ':\i0\tab Select all objects in the list view.
\par \i ' W ':\i0\tab Set selected objects to wire frame.
\par \i ' V ' or SPACE:\i0\tab Toggle the visibility of selected objects.
\par \i DEL:\i0\tab Delete selected objects.
\par }
2010
Scribble2010
History
History;


mainbrowsesequence:000130
Writing



FALSE
9
{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 Arial;}{\f1\fnil Arial;}}
{\colortbl ;\red0\green0\blue255;\red0\green0\blue0;}
\viewkind4\uc1\pard\cf1\lang2057\b\fs32 History\cf0\lang1033\b0\f1\fs20 
\par 
\par \pard\li720\cf2\lang2057\f0 LineDrawer\cf0\lang1033\'99 started off as quick and dirty helper application that I needed to test a program I was writing at work. It was 2D only and had very limited navigation abilities. After a while I found I was continuously modifying it to help debug various other problems and realised that writing a more substantial tool, with 3D support, would be worthwhile.\f1 
\par \f0 
\par Version v1.0:
\par \pard\li1380 Released: 03 June 2004.\f1 
\par }
2015
Scribble2015
Development
Bug Reporting;Development;


mainbrowsesequence:000120
Writing



FALSE
9
{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 Arial;}{\f1\fnil Arial;}}
{\colortbl ;\red0\green0\blue255;\red0\green0\blue0;\red0\green128\blue0;\red128\green0\blue0;}
\viewkind4\uc1\pard\cf1\lang2057\b\fs32 Development\cf0\lang1033\b0\f1\fs20 
\par 
\par \pard\li720\cf2\lang2057\f0 LineDrawer\cf0\lang1033\'99 is continually under development. Features are added on a "I need it to do this now" basis. If you have an idea or need for a feature \cf3\strike let me know\cf4\strike0\{linkID=2020\}\cf0 . If you really want me to add the feature money is likely to increase the probability of it being added :).
\par 
\par \b Bugs: \b0 Like any good software \cf2\lang2057 LineDrawer\cf0\lang1033\'99 has its share of bugs. If you find a bug, let me know and I'll try and find the time to fix it. Again, my bug fixing motivation is susceptible to financial influence :).\cf1\b\f1 
\par \pard\cf0\b0 
\par }
2017
Scribble2017
Licence
Licence;


mainbrowsesequence:000140
Writing



FALSE
8
{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 Arial;}{\f1\fnil Arial;}}
{\colortbl ;\red0\green0\blue255;\red0\green0\blue0;\red0\green128\blue0;\red128\green0\blue0;}
\viewkind4\uc1\pard\cf1\lang2057\b\fs32 Licence\cf0\lang1033\b0\f1\fs20 
\par 
\par \pard\fi20\li720\lang2057\f0 This version of \cf2 LineDrawer\cf0\lang1033\'99 \lang2057 is free. You may give it to anyone you think might have a use for it as long as it remains complete and unmodified.
\par 
\par By using, receiving, or even knowing about this software you agree that \cf3\strike I\cf4\strike0\{linkID=2020\}\cf0  am in no way liable for anything.\lang1033\f1 
\par }
2020
Scribble2020
Contact Details
Contact Details;


mainbrowsesequence:000150
Writing



FALSE
9
{\rtf1\ansi\ansicpg1252\deff0\deflang1033{\fonttbl{\f0\fnil\fcharset0 Arial;}{\f1\fnil Arial;}}
{\colortbl ;\red0\green0\blue255;\red0\green0\blue0;}
\viewkind4\uc1\pard\cf1\lang2057\b\fs32 Contact Details\cf0\lang1033\b0\f1\fs20 
\par 
\par \pard\li720\lang2057\f0 Paul Ryland
\par p_ryland@hotmail.com - please put "LINEDRAWER" as the subject of your email so I can spot it amoungst the tons of spam that this email address receives.
\par 
\par If you've received a copy of \cf2 LineDrawer\cf0\lang1033\'99 can you send me a quick mail to let me know you're using it, how you're using it, and what you think? Entirely for my own curiousity. :)\f1 
\par }
0
0
0
83
1 General
2 Overview=Scribble10
2 History=Scribble2010
2 Development=Scribble2015
2 Licence=Scribble2017
2 Contact Details=Scribble2020
1 LineDrawer Script
2 LineDrawer Script=Scribble15
1 LineDrawer Objects
2 Objects=Scribble20
2 List-able Object Types=Scribble27
2 Object Types=Scribble25
2 Object Types
3 Point=Scribble30
3 Line, LineD, LineNL=Scribble40
3 RectangleLU, RectangleWHZ=Scribble50
3 Triangle=Scribble60
3 Quad, QuadLU, QuadWHZ=Scribble70
3 CircleR, CircleRxRyZ=Scribble80
3 BoxLU, BoxWHD=Scribble90
3 CylinderHR, CylinderHRxRy=Scribble100
3 SphereR, SphereRxRyRz=Scribble110
3 FrustumWHNF, FrustumATNF=Scribble120
3 GridWHD=Scribble130
3 Mesh=Scribble140
3 XFile=Scribble150
3 Group, GroupCyclic=Scribble160
3 Example Summary=Scribble170
2 Object Type Modifiers
3 Object Type Modifiers=Scribble175
3 Transform=Scribble177
3 Position=Scribble179
3 Orientation=Scribble189
3 Animation=Scribble199
3 Wire=Scribble209
3 Hidden=Scribble219
1 Menus
2 File Menu
3 New Object=Scribble300
3 Open=Scribble310
3 Additive Open=Scribble320
3 Save=Scribble330
3 Save As=Scribble340
3 Set Error Output=Scribble350
3 Run Plugin=Scribble360
2 Navigation Menu
3 Jump To=Scribble400
3 Show Origin, Axis, Focus Point=Scribble410
3 Lock=Scribble420
3 View=Scribble430
3 Inertial Camera=Scribble440
2 Data Menu
3 Show Selection=Scribble500
3 Clear=Scribble510
3 Auto Clear=Scribble520
3 Auto Refresh=Scribble530
3 Network Listener=Scribble540
3 Start Cyclic Objects=Scribble550
3 Start Animations=Scribble560
3 Data List=Scribble570
2 Rendering Menu
3 Wire frame=Scribble600
3 Co-ordinates=Scribble610
3 Render 2D=Scribble620
3 Right-handed=Scribble630
3 Stereo View=Scribble640
3 Lighting=Scribble650
2 Window Menu
3 Always On Top=Scribble700
3 Background Colour=Scribble710
3 LineDrawer Help=Scribble720
3 About LineDrawer=Scribble730
1 Navigation
2 Trackball Mouse Navigation=Scribble740
2 Inertial Camera=Scribble750
1 Data Window
2 Data Window=Scribble760
1 Rendering
2 Co-ordinates=Scribble860
2 Stereo View=Scribble870
2 Lighting=Scribble880
1 Keyboard Shortcuts
2 Keyboard Shortcuts=Scribble2000
6
*InternetLink
16711680
Courier New
0
10
1
....
0
0
0
0
0
0
*ParagraphTitle
-16777208
Arial
0
11
1
B...
0
0
0
0
0
0
*PopupLink
-16777208
Arial
0
8
1
....
0
0
0
0
0
0
*PopupTopicTitle
16711680
Arial
0
10
1
B...
0
0
0
0
0
0
*TopicText
-16777208
Arial
0
10
1
....
0
0
0
0
0
0
*TopicTitle
16711680
Arial
0
16
1
B...
0
0
0
0
0
0
