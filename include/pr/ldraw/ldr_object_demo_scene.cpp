//***************************************************************************************************
// Ldr Object
//  Copyright (c) Rylogic Ltd 2009
//***************************************************************************************************

#include <string>

namespace pr::ldr
{
	// Generate a scene that demos the supported object types and modifiers.
	std::string CreateDemoScene()
	{
		std::string str;
		str.reserve(4096);
		str.append(""
			// Intro and definitions
			"//********************************************\n"
			"// LineDrawer demo scene\n"
			"//  Copyright (c) Rylogic Ltd 2002\n"
			"//********************************************\n"
			"//\n"
			"//  Syntax:\n"
			"//    LDraw script (.ldr) expects keywords to be preceeded by a '*' character.\n"
			"//    The syntax that follows the keyword depends on the particular keyword.\n"
			"//    Braces, { and }, are used to define Sections which control the nesting of\n"
			"//    data, and allow the parser to skip unrecognised blocks in the script.\n"
			"//    The parser supports a C-Style preprocessor to allow macro substitutions\n"
			"//    and embedding of other code (e.g. C#, lua, etc).\n"
			"//    Keywords are not case sensitive, however Ldr script in general is.\n"
			"//\n"
			"//  Summary:\n"
			"//		*Keyword                    - keywords are identified by '*' characters\n"
			"//		{// Section begin           - nesting of objects within sections implies parenting\n"
			"//			// Line comment         - single line comments\n"
			"//			/* Block comment */     - block comments\n"
			"//			#eval{1+2}              - macro expression evaluation\n"
			"//		}// Section end\n"
			"//\n"
			"//		C-style preprocessing\n"
			"//		#include \"include_file\"   - include other script files\n"
			"//		#define MACRO subst_text    - define text substitution macros\n"
			"//		MACRO                       - macro substitution\n"
			"//		#undef MACRO                - un-defining of macros\n"
			"//		#ifdef MACRO                - nestable preprocessor controlled sections\n"
			"//		#elif MACRO\n"
			"//			#ifndef MACRO\n"
			"//			#endif\n"
			"//		#else\n"
			"//		#endif\n"
			"//		#lit\n"
			"//			literal text\n"
			"//		#end\n"
			"//		#embedded(lua)\n"
			"//			--lua code\n"
			"//		#end\n"
			"//\n"
			"//  Definitions:\n"
			"//    ctx_id - this is a GUID used to identify a source of objects. It allows\n"
			"//             objects to be added/removed/refreshed in discrete sets.\n"
			"//    axis_id - is an integer describing an axis number. It must be one\n"
			"//              of ±1, ±2, ±3 corresponding to ±X, ±Y, ±Z respectively.\n"
			"//    [field] - fields in square brackets mean optional fields.\n"
			"//\n"
			"//  Object Description format:\n"
			"//       *ObjectType [name] [colour]\n"
			"//       {\n"
			"//          ...\n"
			"//       }\n"
			"//    The name and colour parameters are optional and have defaults of:\n"
			"//       name     = 'ObjectType'\n"
			"//       colour   = FFFFFFFF\n"
			"\n"

			// Global flags, commands, includes, cameras, and lights
			"// Allow missing includes to be ignored and not cause errors\n"
			"#ignore_missing \"on\"\n"
			"#include \"missing_file.ldr\"\n"
			"\n"
			"// Add an explicit dependency on another file. This is basically the same as an #include except\n"
			"// the dependent file contents are not added to the script. This is useful for triggering an auto refresh.\n"
			"#depend \"trigger_update.txt\"\n"
			"\n"
			"// A camera section must be at the top level in the script.\n"
			"// Camera descriptions raise an event immediately after being parsed.\n"
			"// The application handles this event to set the camera position.\n"
			"*Camera\n"
			"{\n"
			"	// Note: order is important. Camera properties are set in the order declared\n"
			"	*o2w {*pos{0 0 4}}        // Camera position/orientation within the scene\n"
			"	*LookAt {0 0 0}           // Optional. Point the camera at {x,y,z} from where it currently is. Sets the focus distance\n"
			"	//*Align {0 1 0}          // Optional. Lock the camera's up axis to  {x,y,z}\n"
			"	//*Aspect {1.0}           // Optional. Aspect ratio (w/h). FovY is unchanged, FovX is changed. Default is 1\n"
			"	//*FovX {45}              // Optional. X field of view (deg). Y field of view is determined by aspect ratio\n"
			"	//*FovY {45}              // Optional. Y field of view (deg). X field of view is determined by aspect ratio (default 45 deg)\n"
			"	//*Fov {45 45}            // Optional. {Horizontal,Vertical} field of view (deg). Implies aspect ratio.\n"
			"	//*Near {0.01}            // Optional. Near clip plane distance\n"
			"	//*Far {100.0}            // Optional. Far clip plane distance\n"
			"	//*Orthographic           // Optional. Use an orthographic projection rather than perspective\n"
			"}\n"
			"\n"
			"// Light sources can be top level objects, children of other objects, or contain child objects.\n"
			"// In some ways they are like a *Group object, they have no geometry of their own but can contain\n"
			"// objects with geometry.\n"
			"*DirLight sun FFFF00         // Colour attribute is the colour of the light source\n"
			"{\n"
			"	0 -1 -0.3                 // Direction dx,dy,dz (doesn't need to be normalised)\n"
			"	*Specular {FFFFFF 1000}   // Optional. Specular colour and power\n"
			"	*CastShadow {10}          // Optional. {range} Shadows are cast from this light source out to range\n"
			"	*o2w {*pos{5 5 5}}        // Position/orientation of the object\n"
			"}\n"
			"*PointLight glow FF00FF\n"
			"{\n"
			"	5 5 5                     // Position x,y,z\n"
			"	*Range {100 0}            // Optional. {range, falloff}. Default is infinite\n"
			"	*Specular {FFFFFF 1000}   // Optional. Specular colour and power\n"
			"	//*CastShadow {10}        // Optional. {range} Shadows are cast from this light source out to range\n"
			"	*o2w{*pos{5 5 5}}\n"
			"}\n"
			"*SpotLight spot 00FFFF\n"
			"{\n"
			"	3 5 4                     // Position x,y,z\n"
			"	-1 -1 -1                  // Direction dx,dy,dz (doesn't need to be normalised)\n"
			"	30 60                     // Inner angle (deg), Outer angle (deg)\n"
			"	*Range {100 0}            // Optional. {range, falloff}. Default is infinite\n"
			"	*Specular {FFFFFF 1000}   // Optional. Specular colour and power\n"
			"	//*CastShadow {10}        // Optional. {range} Shadows are cast from this light source out to range\n"
			"	*o2w{*pos{5 5 5}}         // Position and orientation (directional lights shine down -z)\n"
			"}\n"
			"\n"

			// Minimal example
			"// Basic minimal object example:\n"
			"*Box {1 2 3}\n"
			"\n"

			// Object transforms
			"// An example of applying a transform to an object.\n"
			"// All objects have an implicit object-to-parent transform that is identity.\n"
			"// Successive 'o2w' sections pre-multiply this transform for the object.\n"
			"// Fields within the 'o2w' section are applied in the order they are declared.\n"
			"*Box transforms_example FF00FF00\n"
			"{\n"
			"	2 3 1\n"
			"	*o2w\n"
			"	{\n"
			"		// An empty 'o2w' is equivalent to an identity transform\n"
			"		*M4x4 {1 0 0 0  0 1 0 0  0 0 1 0  0 0 0 1}    // {xx xy xz xw  yx yy yz yw  zx zy zz zw  wx wy wz ww} - i.e. row major\n"
			"		*M3x3 {1 0 0  0 1 0  0 0 1}                   // {xx xy xz  yx yy yz  zx zy zz} - i.e. row major\n"
			"		*Pos {0 1 0}                                  // {x y z}\n"
			"		*Align {3 0 1 0}                              // {axis_id dx dy dz } - direction vector, and axis id to align to that direction\n"
			"		*Quat {0 #eval{sin(pi/2)} 0 #eval{cos(pi/2)}} // {x y z s} - quaternion\n"
			"		*QuatPos {0 1 0 0  1 -2 3}                    // {q.x q.y q.z q.s p.x p.y p.z} - quaternion position\n"
			"		*Rand4x4 {0 1 0 2}                            // {cx cy cz r} - centre position, radius. Random orientation\n"
			"		*RandPos {0 1 0 2}                            // {cx cy cz r} - centre position, radius\n"
			"		*RandOri                                      // Randomises the orientation of the current transform\n"
			"		*Scale {1 1.2 1}                              // { sx sy sz } - multiples the lengths of x,y,z vectors of the current transform. Accepts 1 or 3 values\n"
			"		*Normalise                                    // Normalises the lengths of the vectors of the current transform\n"
			"		*Orthonormalise                               // Normalises the lengths and makes orthogonal the vectors of the current transform\n"
			"		*Transpose *Transpose                         // Transposes the current transform\n"
			"		*Inverse *Inverse                             // Inverts the current transform\n"
			"		*Euler {45 30 60}                             // { pitch yaw roll } - all in degrees. Order of rotations is roll, pitch, yaw\n"
			"	}\n"
			"}\n"
			"\n"

			// Object modifiers
			"// There are a number of object modifiers that can be used to\n"
			"// control the colour, texture, visibility, and animation of objects.\n"
			"// These modifiers can be applied to any object (except where noted).\n"
			"*Box modifiers_example FFFF0000\n"
			"{\n"
			"	0.2 0.5 0.4\n"
			"	*Colour {FFFF00FF}         // Override the base colour of the model\n"
			"	*ColourMask {FF000000}     // applies: 'child.colour = (obj.base_colour & mask) | (child.base_colour & ~mask)' to all children recursively\n"
			"	*Reflectivity {0.3}        // Reflectivity (used with environment mapping)\n"
			"	*RandColour                // Apply a random colour to this object\n"
			"	*Hidden                    // Object is created in an invisible state\n"
			"	*Wireframe                 // Object is created with wireframe fill mode\n"
			"	*Animation                 // Add simple animation to this object\n"
			"	{\n"
			"		*Style PingPong        // Animation style, one of: NoAnimation, PlayOnce, PlayReverse, PingPong, PlayContinuous\n"
			"		*Period { 1.2 }        // The period of the animation in seconds\n"
			"		*Velocity { 1 1 1 }    // Linear velocity vector in m/s\n"
			"		*AngVelocity { 1 0 0 } // Angular velocity vector in rad/s\n"
			"	}\n"
			"	*Texture                 // Texture (only supported on some object types)\n"
			"	{\n"
			"		\"#checker\"        // texture filepath, stock texture name (e.g. #white, #black, #checker), or texture id (e.g. #1, #3)\n"
			"		*Addr {Clamp Clamp} // Optional addressing mode for the texture; U, V. Options: Wrap, Mirror, Clamp, Border, MirrorOnce\n"
			"		*Filter {Linear}    // Optional filtering of the texture. Options: Point, Linear, Anisotropic\n"
			"		*Alpha              // Optional. Indicates the texture contains alpha pixels\n"
			"		*o2w                // Optional 3d texture coord transform\n"
			"		{\n"
			"			*Scale{100 100 1}\n"
			"			*Euler{0 0 90}\n"
			"		}\n"
			"	}\n"
			"	//*ScreenSpace    // The object's '*o2w' transform is interpreted as a screen space position/orientation\n"
			"	                  // Screen space coordinates are in the volume:\n"
			"	                  //  (-1,-1, 0) = bottom, left, near plane\n"
			"	                  //  (+1,+1,-1) = top, right, far plane\n"
			"	//*NoZWrite       // Don't write to the Z buffer\n"
			"	//*NoZTest        // Don't depth-text\n"
			"}\n"
			"\n"

			// Instancing
			"// Model Instancing.\n"
			"// An instance can be created from any previously defined object.\n"
			"// The instance will share the renderable model from the object it is an instance of.\n"
			"// Note that properties of the object are not inherited by the instance.\n"
			"*Box instancing_example FF0000FF\n"
			"{\n"
			"	// 'false' in the object declaration means this model will be created\n"
			"	// in memory only and not displayed. It can be used for instancing still.\n"
			"	0.8 1 2\n"
			"	*RandColour // Note: this will not be inherited by the instances\n"
			"	*Hidden // Don't show this instance\n"
			"}\n"
			"*Instance instancing_example FFFF0000   // The name indicates which model to instance\n"
			"{\n"
			"	*o2w {*Pos {5 0 -2}}\n"
			"}\n"
			"*Instance instancing_example FF0000FF\n"
			"{\n"
			"	*o2w {*Pos {-4 0.5 0.5}}\n"
			"}\n"
			"\n"

			// Nesting
			"// Object Nesting.\n"
			"// Nested objects are in the space of their parent so a parent transform is applied to all children.\n"
			"*Box nesting_example1 80FFFF00\n"
			"{\n"
			"	0.4 0.7 0.3\n"
			"	*o2w {*pos {0 3 0} *randori}\n"
			"	*ColourMask { FF000000 }              // This colour mask is applied to all child recursively\n"
			"	*Box nested1_1 FF00FFFF               // Positioned relative to 'nesting_example1'\n"
			"	{\n"
			"		0.4 0.7 0.3\n"
			"		*o2w {*pos {1 0 0} *randori}\n"
			"		*Box nested1_2 FF00FFFF           // Positioned relative to 'nested1_1'\n"
			"		{\n"
			"			0.4 0.7 0.3\n"
			"			*o2w {*pos {1 0 0} *randori}\n"
			"			*Box nested1_3 FF00FFFF       // Positioned relative to 'nested1_2'\n"
			"			{\n"
			"				0.4 0.7 0.3\n"
			"				*o2w {*pos {1 0 0} *randori}\n"
			"			}\n"
			"		}\n"
			"	}\n"
			"}\n"
			"\n"

			// Group
			"// Groups are a special object type that doesn't contain geometry\n"
			"// but has a coordinate space and child objects.\n"
			"*Group group\n"
			"{\n"
			"	*Wireframe     // Object modifiers applied to groups are applied recursively to children within the group\n"
			"	*Box b FF00FF00 { 0.4 0.1 0.2 }\n"
			"	*Sphere s FF0000FF { 0.3 *o2w{*pos{0 1 2}}}\n"
			"}\n"
			"\n"

			// Text
			"// Text objects are textured quads that contain GDI text.\n"
			"// There is support for screen space, billboard, and full 3D text objects.\n"
			"\n"
			"// Fonts declared at global scope set the default for following objects\n"
			"// All fields are optional, replacing the default if given.\n"
			"*Font\n"
			"{\n"
			"	*Name {\"tahoma\"} // Font name\n"
			"	*Size {18}         // Font size (in points)\n"
			"	*Weight {300}      // Font weight (100 = ultra light, 300 = normal, 900 = heavy)\n"
			"	*Style {Normal}    // Style. One of: Normal Italic Oblique\n"
			"	*Stretch {5}       // Stretch. 1 = condensed, 5 = normal, 9 = expanded\n"
			"	//*Underline       // Underlined text\n"
			"	//*Strikeout       // Strikethrough text\n"
			"}\n"
			"\n"
			"// Screen space text.\n"
			"*Text camera_space_text\n"
			"{\n"
			"	*ScreenSpace\n"
			"\n"
			"	// Text is concatenated, with changes of style applied\n"
			"	// Text containing multiple lines has the line-end whitespace and indentation tabs removed.\n"
			"	*Font { *Name {\"Times New Roman\"} *Colour {FF0000FF} *Size{18}}\n"
			"	\"This is camera space \n"
			"	text with new lines in it, and \"\n"
			"	*NewLine\n"
			"	*Font {*Colour {FF00FF00} *Size{24} *Style{Oblique}}\n"
			"	\"with varying colours \n"
			"	and \"\n"
			"	*Font {*Colour {FFFF0000} *Weight{800} *Style{Italic} *Strikeout}\n"
			"	\"stiles \"\n"
			"	*Font {*Colour {FFFFFF00} *Style{Italic} *Underline}\n"
			"	\"styles \"\n"
			"\n"
			"	// The background colour of the quad\n"
			"	*BackColour {40A0A0A0}\n"
			"\n"
			"	// Anchor defines the origin of the quad. (-1,-1) = bottom,left. (0,0) = centre (default). (+1,+1) = top,right\n"
			"	*Anchor { -1 +1 }\n"
			"\n"
			"	// Padding between the quad boundary and the text\n"
			"	*Padding {10 20 10 10}\n"
			"\n"
			"	// *o2w is interpreted as a 'text to camera space' transform\n"
			"	// (-1,-1,0) is the lower left corner on the near plane.\n"
			"	// (+1,+1,1) is the upper right corner on the far plane.\n"
			"	// The quad is automatically scaled to make the text unscaled on-screen\n"
			"	*o2w {*pos{-1, +1, 0}}\n"
			"}\n"
			"\n"
			"// Billboard text always faces the camera and is scaled based on camera distance\n"
			"*Text billboard_text\n"
			"{\n"
			"	*Font {*Colour {FF00FF00}} // Note: Font changes must come before text that uses the font\n"
			"	\"This is billboard text\"\n"
			"	*Billboard\n"
			"\n"
			"	*Anchor {0 0}\n"
			"\n"
			"	// The rotational part of *o2w is ignored, only the 3d position is used\n"
			"	*o2w {*pos{0,1,0}}\n"
			"}\n"
			"\n"
			"// Texture quad containing text\n"
			"*Text three_dee_text\n"
			"{\n"
			"	*Font {*Name {\"Courier New\"} *Size {40}}\n"
			"\n"
			"	// Normal text string, everything between quotes (including new lines)\n"
			"	\"This is normal\n"
			"	3D text. \"\n"
			"\n"
			"	*CString\n"
			"	{\n"
			"		// Text can also be given as a C-Style string that uses escape characters.\n"
			"		// Everything between matched quotes (including new lines, although\n"
			"		// indentation tabs are not removed for C-Style strings)\n"
			"		\"It can even       \n"
			"		use\\n \\\"C-Style\\\" strings\"\n"
			"	}\n"
			"\n"
			"	*AxisId {+3}    // Optional. Set the direction of the quad normal. One of: X = ±1, Y = ±2, Z = ±3\n"
			"	*Dim {512 128}  // Optional. The size used to determine the layout area\n"
			"	*Format         // Optional\n"
			"	{\n"
			"		Left      // Horizontal alignment. One of: Left, CentreH, Right\n"
			"		Top       // Vertical Alignment. One of: Top, CentreV, Bottom\n"
			"		Wrap      // Text wrapping. One of: NoWrap, Wrap, WholeWord, Character, EmergencyBreak\n"
			"	}\n"
			"	*BackColour {40000000}\n"
			"	*o2w {*scale{0.02}}\n"
			"}\n"
			"\n"

			// Objects
			"// ************************************************************************************\n"
			"// Objects\n"
			"// ************************************************************************************\n"
			"// Below is an example of every supported object type with notes on their syntax.\n"
			"\n"

			// Point sprites
			"// Points ***********************************************\n"
			"\n"
			"// A list of points\n"
			"*Point pts\n"
			"{\n"
			"	0 0 0        // x y z point positions\n"
			"	1 1 1\n"
			"\n"
			"	// Using embedded C# to generate points\n"
			"	#embedded(CSharp)\n"
			"	var rng = new Random();\n"
			"	for (int i = 0; i != 100; ++i)\n"
			"		Out.AppendLine(v4.Random3(1.0f, 1.0f, rng).ToString3());\n"
			"	#end\n"
			"\n"
			"	*Size {20}                // Optional. Specify a size for the point\n"
			"	*Style { Circle }          // Optional. One of: Square, Circle, Star, .. Requires 'Width'\n"
			"	*Texture {\"#whitespot\"}    // Optional. A texture for each point sprite. Requires 'Width'. Ignored if 'Style' given.\n"
			"}\n"
			"\n"
	
			// Lines
			"// Lines ***********************************************\n"
			"\n"
			"// Line modifiers:\n"
			"//   *Width {w} - Render the lines with the thickness 'w' specified (in pixels).\n"
			"//   *Param {t0 t1} - Clip/Extend the previous line to the parametric values given.\n"
			"//   *dashed {on off} - Convert the line to a dashed line with dashes of length 'on' and gaps of length 'off'.\n"
			"//   For objects that support optional colours, all or none of the points/lines/segments must have colours.\n"
			"\n"
			"// A model containing an arbitrary list of line segments\n"
			"*Line lines\n"
			"{\n"
			"	-2  1  4  2 -3 -1 FFFF00FF  // x0 y0 z0  x1 y1 z1 [colour] Start and end points for a line followed by optional colour\n"
			"	+1 -2  4 -1 -3 -1 FF00FFFF\n"
			"	-2  4  1  4 -3  1 FFFFFF00\n"
			"}\n"
			"\n"
			"// A list of line segments given point and direction\n"
			"*LineD lineds FF00FF00\n"
			"{\n"
			"	0  1  0 -1  0  0       // x y z dx dy dz [colour] - start and direction for a line folled by optional colour\n"
			"	0  1  0  0  0 -1\n"
			"	0  1  0  1  0  0\n"
			"	0  1  0  0  0  1\n"
			"	*Param {0.2 0.6}       // Optional. Parametric values. Applies to the previous line only\n"
			"}\n"
			"\n"
			"// A model containing a sequence of line segments given by a list of points\n"
			"*LineStrip linestrip\n"
			"{\n"
			"	0 0 0 FF00FF00         // x y y [colour]. A vertex in the line strip followed by an optional colour\n"
			"	0 0 1 FF0000FF         // *Param can only be used from the second vertex onwards\n"
			"	0 1 1 FFFF00FF *Param {0.2 0.4}\n"
			"	1 1 1 FFFFFF00\n"
			"	1 1 0 FF00FFFF\n"
			"	1 0 0 FFFFFFFF\n"
			"	*Dashed {0.1 0.05}\n"
			"	*Smooth                // Optional. Turns the line segments into a smooth spline\n"
			"}\n"
			"\n"
			"// A cuboid made from lines\n"
			"*LineBox linebox\n"
			"{\n"
			"	2 4 1 // Width, height, depth. Accepts 1, 2, or 3 dimensions. 1dim = cube, 2 = rod, 3 = arbitrary box\n"
			"}\n"
			"\n"
			"// A grid of lines\n"
			"*Grid grid FFA08080\n"
			"{\n"
			"	4 5    // width, height\n"
			"	8 10   // Optional, w,h divisions. If omitted defaults to width/height\n"
			"}\n"
			"\n"
			"// A curve described by a start and end point and two control points.\n"
			"*Spline spline\n"
			"{\n"
			"	0 0 0  0 0 1  1 0 1  1 0 0 FF00FF00 // p0 p1 p2 p3 [colour] - all points are positions, tangents given by p1-p0, p3-p2. Followed by optional colour\n"
			"	0 0 0  1 0 0  1 1 0  1 1 1 FFFF0000 // each spline is a separate spline segment\n"
			"	*Width { 4 }                        // Optional line width\n"
			"}\n"
			"\n"
			"// A line with pointy ends\n"
			"*Arrow arrow\n"
			"{\n"
			"	FwdBack                             // Type of  arrow. One of Line, Fwd, Back, or FwdBack\n"
			"	-1 -1 -1 FF00FF00                   // Corner points forming a line strip of connected lines, followed by optional colour\n"
			"	-2  3  4 FFFF0000                   // Note, colour blends smoothly between each vertex\n"
			"	+2  0 -2 FFFFFF00\n"
			"	*Smooth                             // Optional. Turns the line segments into a smooth spline\n"
			"	*Width { 5 }                        // Optional line width and arrow head size\n"
			"}\n"
			"\n"
			"// A matrix drawn as a set of three basis vectors (X=red, Y=green, Z=blue)\n"
			"*Matrix3x3 a2b_transform\n"
			"{\n"
			"	1 0 0      // X\n"
			"	0 1 0      // Y\n"
			"	0 0 1      // Z\n"
			"}\n"
			"\n"
			"// A set of basis vectors\n"
			"*CoordFrame a2b\n"
			"{\n"
			"	*Scale {0.1}                       // Optional, scale\n"
			"	*LeftHanded                        // Optional, create a left handed coordinate frame\n"
			"}\n"
			"\n"
	
			// 2D Shapes
			"// 2D Shapes ***********************************************\n"
			"\n"
			"// A circle or ellipse\n"
			"*Circle circle\n"
			"{\n"
			"	1.6                                 // radius\n"
			"	*AxisId {-2}                        // Optional, normal direction. axis_id: ±1 = ±x, ±2 = ±y, ±3 = ±z \n"
			"	*Solid                              // Optional, if omitted then the circle is an outline only\n"
			"	*RandColour *o2w{*RandPos{0 0 0 2}} // Object colour is the outline colour\n"
			"	//*Facets { 40 }                    // Optional, controls the smoothness of the edge\n"
			"}\n"
			"*Circle ellipse\n"
			"{\n"
			"	1.6 0.8                             // radiusx, radiusy\n"
			"	*AxisId {-2}                        // Optional, normal direction. axis_id: ±1 = ±x, ±2 = ±y, ±3 = ±z\n"
			"	*Solid                              // Optional, if omitted then the circle is an outline only\n"
			"	*RandColour *o2w{*RandPos{0 0 0 2}} // Object colour is the outline colour\n"
			"	//*Facets { 40 }                    // Optional, controls the smoothness of the edge\n"
			"}\n"
			"\n"
			"// A pie or wedge\n"
			"*Pie pie FF00FFFF\n"
			"{\n"
			"	10 45                              // Start angle, End angle in degress (from the 'x' axis). Equal values creates a ring\n"
			"	0.1 0.7                            // inner radius, outer radius\n"
			"	*AxisId {-2}                       // Optional, normal direction. axis_id: ±1 = ±x, ±2 = ±y, ±3 = ±z\n"
			"	*Scale {1.0 0.8}                   // Optional, X,Y scale factors\n"
			"	*Solid                             // Optional, if omitted then the shape is an outline only\n"
			"	//*Facets { 40 }                   // Optional, controls the smoothness of the inner and outer edges\n"
			"}\n"
			"\n"
			"// A rectangle\n"
			"*Rect rect FF0000FF\n"
			"{\n"
			"	1.2                                // width\n"
			"	1.3                                // Optional height. If omitted, height = width\n"
			"	*AxisId {-2}                       // Optional, normal direction. axis_id: ±1 = ±x, ±2 = ±y, ±3 = ±z\n"
			"	*Solid                             // Optional, if omitted then the shape is an outline only\n"
			"	*CornerRadius { 0.2 }              // Optional corner radius for rounded corners\n"
			"	*Facets { 2 }                      // Optional, controls the smoothness of the corners\n"
			"}\n"
			"\n"
			"// A 2D polygon\n"
			"*Polygon poly FFFFFFFF\n"
			"{\n"
			"	1.0f, 3.0f                         // A list of 2D points with CCW winding order describing the polygon\n"
			"	1.4f, 1.7f                         // Optionally followed by a colour per vertex\n"
			"	0.4f, 2.0f\n"
			"	1.5f, 1.2f\n"
			"	1.0f, 0.0f\n"
			"	1.7f, 1.0f\n"
			"	2.5f, 0.5f\n"
			"	2.0f, 1.5f\n"
			"	2.0f, 2.0f\n"
			"	1.5f, 2.5f\n"
			"	*AxisId {3}                        // Optional, Normal direction. axis_id: ±1 = ±x, ±2 = ±y, ±3 = ±z\n"
			"	*Solid                             // Optional, Filled polygon\n"
			"}\n"
			"\n"

			// Quad shapes
			"// A list of triangles\n"
			"*Triangle triangle FFFFFFFF\n"
			"{\n"
			"	-1.5 -1.5 0 FFFF0000               // Three corner points of the triangle, optionally followed by a colour per vertex\n"
			"	+1.5 -1.5 0 FF00FF00\n"
			"	+0.0  1.5 0 FF0000FF\n"
			"	*Texture {\"#checker\"}              // Optional texture\n"
			"	*o2w {*randpos{0 0 0 2}}\n"
			"}\n"
			"\n"
			"// A quad given by 4 corner points\n"
			"*Quad quad FFFFFFFF\n"
			"{\n"
			"	-1.5 -1.5 0 FFFF0000          // Four corner points of the quad, followed by an optional vertex colour\n"
			"	+1.5 -1.5 0 FF00FF00          // Corner order should be 'S' layout\n"
			"	-1.5  1.5 0 FF0000FF          // i.e.\n"
			"	+1.5  1.5 0 FFFF00FF          //  (-x,-y)  (x,-y)  (-x,y)  (x,y)\n"
			"	*Texture                      // Optional texture\n"
			"	{\n"
			"		\"#checker\"                                // texture filepath, stock texture name (e.g. #white, #black, #checker), or texture id (e.g. #1, #3)\n"
			"		*Addr {Clamp Clamp}                       // Optional addressing mode for the texture; U, V. Options: Wrap, Mirror, Clamp, Border, MirrorOnce\n"
			"		*Filter {Linear}                          // Optional filtering of the texture. Options: Point, Linear, Anisotropic\n"
			"		*o2w { *scale{4 4 1}  *pos {-2 -2 0} *euler{0 0 10} }  // Optional 3d texture coord transform\n"
			"	}\n"
			"	*AxisId {-2}\n"
			"	*o2w {*randpos{0 0 0 2}}\n"
			"}\n"
			"\n"
			"// A quad to represent a plane\n"
			"*Plane plane FF000080\n"
			"{\n"
			"	0 -2 -2                 // x y z - centre point of the plane\n"
			"	1 1 1                   // dx dy dz - forward direction of the plane\n"
			"	0.5 0.5                 // width, height of the edges of the plane quad\n"
			"	*Texture {\"#checker\"}   // Optional texture\n"
			"}\n"
			"\n"
			"// A triangle strip of quads following a line\n"
			"*Ribbon ribbon FF00FFFF\n"
			"{\n"
			"	-1 -2  0 FFFF0000       // Vertices of the central line of the ribbon, optionally followed by a colour per vertex\n"
			"	-1  3  0 FF00FF00\n"
			"	+2  0  0 FF0000FF\n"
			"	*AxisId {3}                 // Optional. The forward facing axis for the ribbon\n"
			"	*Width {0.1}                // Optional. Width (in world space) (default 10)\n"
			"	*Smooth                     // Optional. Generates a spline through the points\n"
			"	*Texture{\"#checker\"}      // Optional texture repeated along each quad of the ribbon\n"
			"	*o2w {*randpos{0 0 0 2} *randori}\n"
			"}\n"
			"\n"

			// 3D Shapes
			"// A box\n"
			"*Box box\n"
			"{\n"
			"	0.2 0.5 0.3              // Width, [height], [depth]. Accepts 1, 2, or 3 dimensions. 1=cube, 2=rod, 3=arbitrary box\n"
			"	*o2w{*RandPos{0 0 0 2}} *RandColour\n"
			"}\n"
			"\n"
			"// A box between two points with a width and height in the other two directions\n"
			"*Bar bar\n"
			"{\n"
			"	0 1 0  1 2 1  0.1 0.15            // x0 y0 z0  x1 y1 z1  width [height]. height = width if omitted\n"
			"	*Up {0 1 0}                       // Optional. Controls the orientation of width and height for the box\n"
			"	*o2w{*RandPos{0 0 0 2}} *RandColour\n"
			"}\n"
			"\n"
			"// A list of boxes all with the same dimensions at the given locations\n"
			"*BoxList boxlist\n"
			"{\n"
			"	+0.4  0.2  0.5 // Box dimensions: width, height, depth.\n"
			"	-1.0 -1.0 -1.0 // locations: x,y,z\n"
			"	-1.0  1.0 -1.0\n"
			"	+1.0 -1.0 -1.0\n"
			"	+1.0  1.0 -1.0\n"
			"	-1.0 -1.0  1.0\n"
			"	-1.0  1.0  1.0\n"
			"	+1.0 -1.0  1.0\n"
			"	+1.0  1.0  1.0\n"
			"}\n"
			"\n"
			"// A frustum given by width, height, near plane and far plane.\n"
			"// Width, Height given at '1' along the z axis by default, unless *ViewPlaneZ is given.\n"
			"*FrustumWH frustumwh\n"
			"{\n"
			"	1 1 0 1.5                           // width, height, near plane, far plane.\n"
			"	*AxisId {2}                         // Optional. Main axis direction of the frustum: ±1 = ±x, ±2 = ±y, ±3 = ±z\n"
			"	*ViewPlaneZ { 2 }                   // Optional. The distance at which the frustum has dimensions width,height\n"
			"	*o2w{*RandPos{0 0 0 2}} *RandColour\n"
			"}\n"
			"\n"
			"// A frustum given by field of view (in Y), aspect ratio, and near and far plane distances.\n"
			"*FrustumFA frustumfa\n"
			"{\n"
			"	90 1 0.4 1.5                        // fovY, aspect, near plane, far plane. axis_id: ±1 = ±x, ±2 = ±y, ±3 = ±z\n"
			"	*AxisId {-1}                        // Optional. Main axis direction of the frustum: ±1 = ±x, ±2 = ±y, ±3 = ±z\n"
			"	*o2w{*RandPos{0 0 0 2}} *RandColour\n"
			"}\n"
			"\n"
			"// A sphere given by radius\n"
			"*Sphere sphere\n"
			"{\n"
			"	0.2                                  // radius\n"
			"	*Facets {3}                          // Optional. Controls the faceting of the sphere\n"
			"	*Texture                             // Optional texture\n"
			"	{\n"
			"		\"#checker2\"\n"
			"		*Addr {Wrap Wrap}\n"
			"		*o2w {*scale{10 10 1}}\n"
			"	}\n"
			"	*o2w{*RandPos{0 0 0 2}} *RandColour\n"
			"}\n"
			"*Sphere ellipsoid\n"
			"{\n"
			"	0.2 0.4 0.6                        // xradius [yradius] [zradius]\n"
			"	*Texture {\"#checker2\"}              // Optional texture\n"
			"	*o2w{*RandPos{0 0 0 2}} *RandColour\n"
			"}\n"
			"\n"
			"// A cylinder given by axis number, height, and radius\n"
			"*CylinderHR cylinder\n"
			"{\n"
			"	0.6 0.2                           // height, radius\n"
			"	*AxisId {2}                       // Optional. Major axis: ±1 = ±x, ±2 = ±y, ±3 = ±z (default +3)\n"
			"	*Facets {3,50}                    // Optional. layers, wedges. controls the faceting of the cylinder\n"
			"	*Scale {1.2 0.8}                  // Optional. X,Y scale factors\n"
			"	*Texture {\"#checker3\"}             // Optional texture\n"
			"	*o2w{*RandPos{0 0 0 2}} *RandColour\n"
			"}\n"
			"*CylinderHR cone FFFF00FF\n"
			"{\n"
			"	0.8 0.5 0                         // height, base radius, [tip radius].\n"
			"	*AxisId {2}                       // Optional. Major axis: ±1 = ±x, ±2 = ±y, ±3 = ±z (default +3)\n"
			"	*Facets {3,50}                    // Optional. layers, wedges. controls the faceting of the cylinder\n"
			"	*Scale {1.5 0.4}                  // Optional. X,Y scale factors\n"
			"	*Texture {\"#checker3\"}             // Optional texture\n"
			"	*o2w{*RandPos{0 0 0 2}} *RandColour\n"
			"}\n"
			"\n"
			"// A cone given by axis number, two heights, and solid angle\n"
			"*ConeHA coneha FF00FFFF\n"
			"{\n"
			"	0.1 1.2 0.5                       // tip-to-top distance, tip-to-base distance, solid angle(rad).\n"
			"	*AxisId {2}                       // Optional. Major axis: ±1 = ±x, ±2 = ±y, ±3 = ±z (default +3)\n"
			"	*Facets {3,50}                    // Optional. layers, wedges. controls the faceting of the cylinder\n"
			"	*Scale {1 1}                      // Optional. X,Y scale factors\n"
			"	*Texture {\"#checker3\"}           // Optional texture\n"
			"	*o2w{*RandPos{0 0 0 2}} *RandColour\n"
			"}\n"
			"\n"
			"// An extrusion along a path\n"
			"*Tube tube FFFFFFFF\n"
			"{\n"
			"	*Style\n"
			"	{\n"
			"		// The cross section type; one of Round, Square, CrossSection\n"
			"		//Round 0.2 0.3  // Elliptical profile. radius X [radius Y]  \n"
			"		//Square 0.2 0.3 // Rectangular profile. radius X [radius Y]\n"
			"		CrossSection     // <list of X,Y pairs> Arbitrary profile\n"
			"		-0.2 -0.2\n"
			"		+0.2 -0.2\n"
			"		+0.05 +0.0\n"
			"		+0.2 +0.2\n"
			"		-0.2 +0.2\n"
			"		-0.05 +0.0\n"
			"		*Facets { 50 }  // Optional. Used for Round cross section profiles\n"
			"		//*Smooth       // Optional. Use smooth normals for the walls of the tube\n"
			"	}\n"
			"	0  1  0 FFFF0000      // Vertices of the extrusion path, optionally followed by a colour per vertex\n"
			"	0  0  1 FF00FF00\n"
			"	0  1  2 FF0000FF\n"
			"	1  1  2 FFFF00FF\n"
			"	*Smooth               // Optional. Generates a spline through the extrusion path\n"
			"	*Closed               // Optional. Fills in the end caps of the tube\n"
			"	*o2w{*randpos{0 0 0 2} *randori}\n"
			"}\n"
			"\n"
			"// A mesh of lines, faces, or tetrahedra.\n"
			"// Syntax:\n"
			"//	*Mesh [name] [colour]\n"
			"//	{\n"
			"//		*Verts { x y z ... }\n"
			"//		[*Normals { nx ny nz ... }]                            // One per vertex\n"
			"//		[*Colours { c0 c1 c2 ... }]                            // One per vertex\n"
			"//		[*TexCoords { tx ty ... }]                             // One per vertex\n"
			"//		*Faces { f00 f01 f02  f10 f11 f12  f20 f21 f22  ...}   // Indices of faces\n"
			"//		*Lines { l00 l01  l10 l11  l20 l21  l30 l31 ...}       // Indices of lines\n"
			"//		*Tetra { t00 t01 t02 t03  t10 t11 t12 t13 ...}         // Indices of tetrahedra\n"
			"//		[GenerateNormals]                                      // Only works for faces or tetras\n"
			"//	}\n"
			"// Each 'Faces', 'Lines', 'Tetra' block is a sub-model within the mesh\n"
			"// Vertex, normal, colour, and texture data must be given before faces, lines, or tetra data\n"
			"// Textures must be defined before faces, lines, or tetra\n"
			"*Mesh mesh FFFFFF00\n"
			"{\n"
			"	*Verts {\n"
			"	1.087695 -2.175121 0.600000\n"
			"	1.087695  3.726199 0.600000\n"
			"	2.899199 -2.175121 0.600000\n"
			"	2.899199  3.726199 0.600000\n"
			"	1.087695  3.726199 0.721147\n"
			"	1.087695 -2.175121 0.721147\n"
			"	2.899199 -2.175121 0.721147\n"
			"	2.899199  3.726199 0.721147\n"
			"	1.087695  3.726199 0.721147\n"
			"	1.087695  3.726199 0.600000\n"
			"	1.087695 -2.175121 0.600000\n"
			"	1.087695 -2.175121 0.721147\n"
			"	2.730441  3.725990 0.721148\n"
			"	2.740741 -2.175321 0.721147\n"
			"	2.740741 -2.175321 0.600000\n"
			"	2.730441  3.725990 0.600000\n"
			"	}\n"
			"	*Faces {\n"
			"	0,1,2;,  // commas and semicolons treated as whitespace\n"
			"	3,2,1;,\n"
			"	4,5,6;,\n"
			"	6,7,4;,\n"
			"	8,9,10;,\n"
			"	8,10,11;,\n"
			"	12,13,14;,\n"
			"	14,15,12;;\n"
			"	}\n"
			"	*GenerateNormals {30}\n"
			"}\n"
			"\n"
			"// Find the convex hull of a point cloud\n"
			"*ConvexHull convexhull FFFFFF00\n"
			"{\n"
			"	*Verts {\n"
			"	-0.998  0.127 -0.614\n"
			"	+0.618  0.170 -0.040\n"
			"	-0.300  0.792  0.646\n"
			"	+0.493 -0.652  0.718\n"
			"	+0.421  0.027 -0.392\n"
			"	-0.971 -0.818 -0.271\n"
			"	-0.706 -0.669  0.978\n"
			"	-0.109 -0.762 -0.991\n"
			"	-0.983 -0.244  0.063\n"
			"	+0.142  0.204  0.214\n"
			"	-0.668  0.326 -0.098\n"
			"	}\n"
			"	*GenerateNormals {30}\n"
			"	*RandColour *o2w{*RandPos{0 0 -1 2}}\n"
			"}\n"
			"\n"
			"// Create a chart from a table of values.\n"
			"// Expects text data in a 2D matrix. Plots columns 1,2,3,.. vs. column 0.\n"
			"*Chart chart\n"
			"{\n"
			"	//#include \"filepath.csv\" // Alternatively, #include a file containing the data:\n"
			"	index, col1, col2 // Rows containing non-number values are ignored\n"
			"	0,  5, -5,       // Chart data\n"
			"	1,  5,  0,\n"
			"	2,  0,  8,\n"
			"	3,  2,  5,\n"
			"	4,  6,  5,       // Trailing blank values are ignored\n"
			"	// Trailing new lines are ignored\n"
			"	*AxisId {3}       // Optional. Facing direction of the chart: ±1 = ±x, ±2 = ±y, ±3 = ±z (default +3)\n"
			"	*Width { 1 }      // Optional. A width for the lines\n"
			"	*XColumn { 0 }    // Optional. Define which column to use as the X axis values (default 0). Use -1 to plot vs. index position\n"
			"	*XAxis { 0 }      // Optional. Explicit X axis Y intercept\n"
			"	*YAxis { 0 }      // Optional. Explicit Y axis X intercept\n"
			"	*Colours          // Optional. Assign colours to each column\n"
			"	{\n"
			"		FF00FF00\n"
			"		FFFF0000\n"
			"	}\n"
			"}\n"
			"\n"
			"// Model from a 3D model file.\n"
			"// Supported formats: *.3ds, *.stl, *.p3d, (so far)\n"
			"//*Model model_from_file\n"
			"//{\n"
			"//	\"filepath\"                   // The file to create the model from\n"
			"//	*Part { n }                  // For model formats that contain multiple models, allows a specific sub model to be selected\n"
			"//	*GenerateNormals {30}        // Generate normals for the model (smoothing angle between faces)\n"
			"//	*BakeTransform {*pos{0 0 0}} // Optional. Bake a transform into the model (independent of *o2w)\n"
			"//}\n"
			"\n"

			// Embedded code
			"// Embedded lua code can be used to programmatically generate script\n"
			"#embedded(lua,support)\n"
			"	-- Lua code\n"
			"	-- Between the embedded/end tags, all text is treated as lua code\n"
			"	-- and passed to the lua interpretter. The 'support' tag indicates\n"
			"	-- that this block is only adding supporting functions and variables\n"
			"	function make_box(box_number)\n"
			"		return \"*box b\"..box_number..\" FFFF0000 { 1 *o2w{*randpos {0 1 0 2}}}\\n\"\n"
			"	end\n"
			"\n"
			"	function make_boxes()\n"
			"		local str = \"\"\n"
			"		for i = 0,10 do\n"
			"			str = str..make_box(i)\n"
			"		end\n"
			"		return str\n"
			"	end\n"
			"#end\n"
			"\n"
			"*Group luaboxes1\n"
			"{\n"
			"	#embedded(lua) return make_boxes() #end\n"
			"	*o2w {*pos {-10 0 0}}\n"
			"}\n"
			"*Group luaboxes2\n"
			"{\n"
			"	#embedded(lua) return make_boxes() #end\n"
			"	*o2w {*pos {10 0 0}}\n"
			"}\n"
			"\n"
			"// Embedded C# code is also supported in managed applications.\n"
			"// Embedded CSharp code is constructed as follows:\n"
			"//	 namespace ldr\n"
			"//	 {\n"
			"//	 	public class Main\n"
			"//	 	{\n"
			"//	 		private StringBuilder Out = new StringBuilder();\n"
			"//\n"
			"//			//The 'Main' object exists for the whole script\n"
			"//			//successive #embedded(CSharp,support) sections are appended.\n"
			"//			#embedded(CSharp,support) code added here.\n"
			"//\n"
			"//	 		public string Execute()\n"
			"//	 		{\n"
			"//				//Each #embedded(CSharp) section replaces the previous one.\n"
			"//	 			#embedded(CSharp) code added here\n"
			"//\n"
			"//				Add to the StringBuilder object 'Out'\n"
			"//	 			return Out.ToString();\n"
			"//	 		}\n"
			"//	 	}\n"
			"//	 }\n"
			"#embedded(CSharp,support)\n"
			"Random m_rng;\n"
			"public Main()\n"
			"{\n"
			"	m_rng = new Random();\n"
			"}\n"
			"m4x4 O2W\n"
			"{\n"
			"	get { return m4x4.Random4x4(v4.Origin, 2.0f, m_rng); }\n"
			"}\n"
			"#end\n"
			"\n"
			"*Group csharp_spheres\n"
			"{\n"
			"	#embedded(CSharp)\n"
			"	Out.AppendLine(Ldr.Box(\"CS_Box\", 0xFFFF0080, 0.4f, O2W));\n"
			"	Log.Info(\"You can also write to the log window (when used in LDraw)\");\n"
			"	#end\n"
			"}\n"
			"\n"
		);

		return str;
	}
}