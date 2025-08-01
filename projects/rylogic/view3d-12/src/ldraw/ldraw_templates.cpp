﻿//***************************************************************************************************
// Ldr Object
//  Copyright (c) Rylogic Ltd 2009
//***************************************************************************************************
//
// Template example syntax:
//   *Keyword [<name>] [<colour>]
//   {
//      (<x> <y> <z>)
//      [*SomeFlag One|Two|Three:sf]
//      [*SomethingElse:se:!sf]
//      $ObjectModifiers
//   }
//   **pos { <x> <y> <z> }
//   **o2w { <field> @pos } 
//   **colour {<aarrggbb>}
//   **ObjectModifiers
//   {
//      [*Something]
//      [@colour]
//      [@o2w]
//   }
// 
// Templates start with a keyword marked by '*'
// '*' templates are visible at the hierarchy level they're declared at.
// '**' templates are not visible at the current hierarchy level, but can be referenced
//     using '@' or '$' template references at or below the current hierarchy level.
// '*!' templates are only allowed at root level and can not recursively include
//     other root level templates.
// '@' is a reference to a template. Only valid when used within a template.
// '$' is a reference to the children of a template. Only valid when used within a template.
//     $ references get expanded by adding each child in the referenced template to the parent
// Literal text is text that is required.
// Text within <> is a field
//     <> fields cannot nest and must contain identifiers
// Text within [] is optional.
//     [] can also nest.
// Text within () is a repeatable section.
//     () implies one or more
//     [()] implies zero or more
// | means select one of the tokens on either side of the bar.
//     If the adjacent token is a [], or () select between the matched tokens.
//     This form: [one] | [two] | [three] means one of the options is required.
//     This form: [[one] | [two] | [three]] means the selection is optional.
// ; means newline break
//
// Templates should be multi-line, users can replace the \n characters with ' ' if needed.
// Templates can contain comments, but comments must not contain: {}[]()<>|*@$
#include <string>

namespace pr::rdr12::ldraw
{
	// Return the auto completion templates
	std::string AutoCompleteTemplates()
	{
		std::string str;
		str.reserve(4096);
		str.append(""

			// Global
			"*!Wireframe {}\n"
			"*!Camera\n"
			"{\n"
			"	[@o2w]\n"
			"	[*LookAt {<x> <y> <z>}]\n"
			"	[*Align {<x> <y> <z>}]\n"
			"	[*Aspect {<aspect>}]\n"
			"	[*FovX {<fovx>}]\n"
			"	[*FovY {<fovy>}]\n"
			"	[*Fov {<fovx> <fovy>}]\n"
			"	[*Near {<near>}]\n"
			"	[*Far {<far>}]\n"
			"	[*Orthographic]\n"
			"}\n"
			"*!Font\n"
			"{\n"
			"	$Font\n"
			"}\n"

			// Common
			"**o2w\n"
			"{\n"
			"	[*m4x4 {<xx> <xy> <xz> <xw>; <yx> <yy> <yz> <yw>; <zx> <zy> <zz> <zw>; <wx> <wy> <wz> <ww>;}]\n"
			"	[*m3x3 {<xx> <xy> <xz>; <yx> <yy> <yz>; <zx> <zy> <zz>;}]\n"
			"	[*pos {<x> <y> <z>}]\n"
			"	[*Euler {<pitch> <yaw> <roll>}]\n"
			"	[*Align {<axis_id> <dx> <dy> <dz>}]\n"
			"	[*LookAt {<x> <y> <z>}]\n"
			"	[*Scale {<sx> [<sy> [<sz>]]}]\n"
			"	[*Quat {<x> <y> <z> <s>}]\n"
			"	[*QuatPos {<qx> <qy> <qz> <qs>  <px> <py> <pz>}]\n"
			"	[*Rand4x4 {<x> <y> <z> <radius>}]\n"
			"	[*RandPos {<x> <y> <z> <radius>}]\n"
			"	[*RandOri {}]\n"
			"	[*Normalise {}]\n"
			"	[*Orthonormalise {}]\n"
			"	[*Transpose {}]\n"
			"	[*Inverse {}]\n"
			"	[*NonAffine {}]\n"
			"}\n"
			"**Font\n"
			"{\n"
			"	[*Name {<family_name>}]\n"
			"	[*Size {<points>}]\n"
			"	[*Colour {<aarrggbb>}]\n"
			"	[*Weight {<weight_100_to_900>}]\n"
			"	[*Style {Normal|Italic|Oblique}]\n"
			"	[*Stretch {<stretch_1_to_9>}]\n"
			"	[*Underline {}]\n"
			"	[*Strikeout {}]\n"
			"}\n"
			"**Texture\n"
			"{\n"
			"	*FilePath {<texture_filepath>}\n"
			"	[*Addr {Wrap|Mirror|Clamp|Border|MirrorOnce  Wrap|Mirror|Clamp|Border|MirrorOnce}]\n"
			"	[*Filter {Point|Linear|Anisotropic}]\n"
			"	[*Alpha {}]\n"
			"	[@o2w]\n"
			"}\n"
			"**Video\n"
			"{\n"
			"	*FilePath {<video_filepath>}\n"
			"}\n"
			"**RootAnimation\n"
			"{\n"
			"	[*Style {NoAnimation|Once|Repeat|Continuous|PingPong}]\n"
			"	[*Period {<seconds>}]\n"
			"	[*Velocity {<dx> <dy> <dz>}]\n"
			"	[*Accel {<ddx> <ddy> <ddz>}]\n"
			"	[*AngVelocity {<dax> <day> <daz>}]\n"
			"	[*AngAccel {<ddax> <dday> <ddaz>}]\n"
			"}\n"
			"**Animation\n"
			"{\n"
			"	[*Style {NoAnimation|Once|Repeat|Continuous|PingPong}]\n"
			"	[*Frame {<frame>}]\n"
			"	[*FrameRange {<start> <end>}]\n"
			"	[*TimeRange {<start> <end>}]\n"
			"	[*Stretch {<speed_multiplier>}]\n"
			"}\n"
			"**ObjectModifiers\n"
			"{\n"
			"	[@o2w]\n"
			"	[*Colour {<colour>}]\n"
			"	[*ColourMask {<mask>}]\n"
			"	[*Reflectivity {<amount>}]\n"
			"	[*RandColour {}]\n"
			"	[*Hidden {}]\n"
			"	[*Wireframe {}]\n"
			"	[*ScreenSpace {}]\n"
			"	[*NoZTest {}]\n"
			"	[*NoZWrite {}]\n"
			"	[@RootAnimation]\n"
			"	[@Font]\n"
			"}\n"
			"**Textured\n"
			"{\n"
			"	[@Texture]\n"
			"	[@Video]\n"
			"}\n"
			"**AxisId {<axis_id>}\n"
			"**PointSprite\n"
			"{\n"
			"	[*Size {<width> [<height>]}]\n"
			"	[*Style {Square|Circle|Triangle|Star|Annulus}]\n"
			"	[*Depth {}]\n"
			"}\n"
			"**BakeTransform {$o2w}\n"
			"**GenerateNormals {<smoothing_angle>}\n"
			"**Width {<thickness>}\n"
			"**Smooth {}\n"
			"**Parametrics\n"
			"{\n"
			"	(<idx> <t0> <t1>)\n"
			"}\n"
			"**Dashed {<on_length> <off_length>}\n"
			"**PerItemColour {}\n"
			"**PerItemParametrics {}\n"
			"**Solid {}\n"

			// Special objects
			"*Group [<name>] [<colour>]\n"
			"{\n"
			"	$ObjectModifiers\n"
			"}\n"
			"*Instance [<name>] [<colour>]\n"
			"{\n"
			"	[@Animation]\n"
			"	$ObjectModifiers\n"
			"}\n"
			"*Text [<name>] [<colour>]\n"
			"{\n"
			"	[*ScreenSpace {}]\n"
			"	[*Billboard {}]\n"
			"	[*Billboard3D]\n"
			"	(*Data {<text>})\n"
			"	([*CString {<c_style_string>}])\n"
			"	([*NewLine {}])\n"
			"	[*BackColour {<colour>}]\n"
			"	[*Anchor {<nss_x> <nss_y>}]\n"
			"	[*Padding {<left> <top> <right> <bottom>}]\n"
			"	[*Format {Left|CentreH|Right Top|CentreV|Bottom NoWrap|Wrap|WholeWord|Character|EmergencyBreak}]\n"
			"	[@Font]\n"
			"	[@AxisId]\n"
			"	$ObjectModifiers\n"
			"}\n"
			"*LightSource [<name>] [<colour>]\n"
			"{\n"
			"	*Style {Point|Directional|Spot}\n"
			"	[*Ambient {<colour>}]\n"
			"	[*Diffuse {<colour>}]\n"
			"	[*Specular {<colour> <power>}]\n"
			"	[*Range {<range> <falloff>}]\n"
			"	[*Cone {<inner> <outer>}]\n"
			"	[*CastShadow {<range>}]\n"
			"	[@o2w]\n"
			"}\n"

			// Point Sprites
			"*Point [<name>] [<colour>]\n"
			"{\n"
			"	[@PerItemColour]\n"
			"	*Data {(<x> <y> <z> [<colour>])}\n"
			"	$PointSprite\n"
			"	$Textured\n"
			"	$ObjectModifiers\n"
			"}\n"

			// Line geometry
			"*Line [<name>] [<colour>]\n"
			"{\n"
			"	[@PerItemColour]\n"
			"	[@PerItemParametrics]\n"
			"	*Data {(<x0> <y0> <z0>  <x1> <y1> <z1> [<line_colour>] [<t0> <t1>])}\n"
			"	[@Width]\n"
			"	[@Dashed]\n"
			"	[@Parametrics]\n"
			"	$ObjectModifiers\n"
			"}\n"
			"*LineD [<name>] [<colour>]\n"
			"{\n"
			"	[@PerItemColour]\n"
			"	[@PerItemParametrics]\n"
			"	*Data {(<x0> <y0> <z0>  <dx> <dy> <dz> [<line_colour>] [<t0> <t1>])}\n"
			"	[@Width]\n"
			"	[@Dashed]\n"
			"	[@Parametrics]\n"
			"	$ObjectModifiers\n"
			"}\n"
			"*LineStrip [<name>] [<colour>]\n"
			"{\n"
			"	[@PerItemColour]\n"
			"	*Data {(<x> <y> <z> [<vertex_colour>])}\n"
			"	[@Width]\n"
			"	[@Dashed]\n"
			"	[@Parametrics]\n"
			"	[@Smooth]\n"
			"	$ObjectModifiers\n"
			"}\n"
			"*LineBox [<name>] [<colour>]\n"
			"{\n"
			"	*Data {<width> [<height> [<depth>]]}\n"
			"	[@Width]\n"
			"	[@Dashed]\n"
			"	$ObjectModifiers\n"
			"}\n"
			"*Grid [<name>] [<colour>]\n"
			"{\n"
			"	*Data {<width> <height> [<width_divisions> <height_divisions>]}\n"
			"	[@Width]\n"
			"	[@Dashed]\n"
			"	[@AxisId]\n"
			"	$ObjectModifiers\n"
			"}\n"
			"*Spline [<name>] [<colour>]\n"
			"{\n"
			"	[*PerItemColour]\n"
			"	*Data {(<x0> <y0> <z0>  <dx0> <dy0> <dz0>   <dx1> <dy1> <dz1>  <x1> <y1> <z1> [<colour>])}\n"
			"	[@Width]\n"
			"	$ObjectModifiers\n"
			"}\n"
			"*Arrow [<name>] [<colour>]\n"
			"{\n"
			"	*Style {Line|Fwd|Back|FwdBack}\n"
			"	[*PerItemColour]\n"
			"	*Data {(<x> <y> <z> [<vertex_colour>])}\n"
			"	[@Width]\n"
			"	[@Smooth]\n"
			"	$ObjectModifiers\n"
			"}\n"
			"*CoordFrame [<name>] [<colour>]\n"
			"{\n"
			"	[*LeftHanded]\n"
			"	[*Scale {<scale>}]\n"
			"	[@Width]\n"
			"	$ObjectModifiers\n"
			"}\n"

			// 2D shapes
			"*Circle [<name>] [<colour>]\n"
			"{\n"
			"	*Data {<radius0> [<radius1>]}\n"
			"	[*Facets {<facet_count>}]\n"
			"	[@AxisId]\n"
			"	[@Solid]\n"
			"	$Textured\n"
			"	$ObjectModifiers\n"
			"}\n"
			"*Pie [<name>] [<colour>]\n"
			"{\n"
			"	*Data {<angle0> <angle1> <inner_radius> <outer_radius>}\n"
			"	[*Facets {<facet_count>}]\n"
			"	[*Scale {<sx> <sy>}]\n"
			"	[@AxisId]\n"
			"	[@Solid]\n"
			"	$Textured\n"
			"	$ObjectModifiers\n"
			"}\n"
			"*Rect [<name>] [<colour>]\n"
			"{\n"
			"	*Data {<width> [<height>]}\n"
			"	[*CornerRadius {<radius>}]\n"
			"	[*Facets {<facet_count>}]\n"
			"	[@AxisId]\n"
			"	[@Solid]\n"
			"	$Textured\n"
			"	$ObjectModifiers\n"
			"}\n"
			"*Polygon [<name>] [<colour>]\n"
			"{\n"
			"	[*PerItemColour]\n"
			"	*Data {(<x> <y> [<colour>])}\n"
			"	[@AxisId]\n"
			"	[@Solid]\n"
			"	$Textured\n"
			"	$ObjectModifiers\n"
			"}\n"

			// Quad shapes
			"*Triangle [<name>] [<colour>]\n"
			"{\n"
			"	[*PerItemColour]\n"
			"	*Data {(<pt0_x> <pt0_y> <pt0_z> [<colour>]; <pt1_x> <pt1_y> <pt1_z> [<colour>]; <pt2_x> <pt2_y> <pt2_z> [<colour>];)}\n"
			"	[@AxisId]\n"
			"	$Textured\n"
			"	$ObjectModifiers\n"
			"}\n"
			"*Quad [<name>] [<colour>]\n"
			"{\n"
			"	[*PerItemColour]\n"
			"	*Data {(<pt0_x> <pt0_y> <pt0_z> [<colour>]; <pt1_x> <pt1_y> <pt1_z> [<colour>]; <pt2_x> <pt2_y> <pt2_z> [<colour>]; <pt3_x> <pt3_y> <pt3_z> [<colour>];)}\n"
			"	[@AxisId]\n"
			"	$Textured\n"
			"	$ObjectModifiers\n"
			"}\n"
			"*Plane [<name>] [<colour>]\n"
			"{\n"
			"	*Data {<width> <height>}\n"
			"	[@AxisId]\n"
			"	$Textured\n"
			"	$ObjectModifiers\n"
			"}\n"
			"*Ribbon [<name>] [<colour>]\n"
			"{\n"
			"	[*PerItemColour]\n"
			"	*Data {(<x> <y> <z> [<colour>])}\n"
			"	[@AxisId]\n"
			"	[@Width]\n"
			"	[@Smooth]\n"
			"	$Textured\n"
			"	$ObjectModifiers\n"
			"}\n"

			// 3D shapes
			"*Box [<name>] [<colour>]\n"
			"{\n"
			"	*Data {<width> [<height> [<depth>]]}\n"
			"	$Textured\n"
			"	$ObjectModifiers\n"
			"}\n"
			"*Bar [<name>] [<colour>]\n"
			"{\n"
			"	*Data {<x0> <y0> <z0>  <x1> <y1> <z1> <width> [<height>]}\n"
			"	[*Up {<dx> <dy> <dz>}]\n"
			"	$Textured\n"
			"	$ObjectModifiers\n"
			"}\n"
			"*BoxList [<name>] [<colour>]\n"
			"{\n"
			"	*Dim {<width> <height> <depth>}\n"
			"	*Data {(<x> <y> <z>)}\n"
			"	$Textured\n"
			"	$ObjectModifiers\n"
			"}\n"
			"*FrustumWH [<name>] [<colour>]\n"
			"{\n"
			"	*Data {<width> <height> <near> <far>}\n"
			"	[*ViewPlaneZ {<distance_of_width_height>}]\n"
			"	[@AxisId]\n"
			"	$Textured\n"
			"	$ObjectModifiers\n"
			"}\n"
			"*FrustumFA [<name>] [<colour>]\n"
			"{\n"
			"	*Data {<fovY> <aspect> <near> <far>}\n"
			"	[@AxisId]\n"
			"	$Textured\n"
			"	$ObjectModifiers\n"
			"}\n"
			"*Sphere [<name>] [<colour>]\n"
			"{\n"
			"	*Data {<radius0> [<radius1> [<radius2>]]}\n"
			"	[*Facets {<facet_count>}]\n"
			"	$Textured\n"
			"	$ObjectModifiers\n"
			"}\n"
			"*Cylinder [<name>] [<colour>]\n"
			"{\n"
			"	*Data {<height> <radius> [<radius_tip>]}\n"
			"	[*Facets {<layers>, <wedges>}]\n"
			"	[*Scale {<sx> <sy>}]\n"
			"	[@AxisId]\n"
			"	$Textured\n"
			"	$ObjectModifiers\n"
			"}\n"
			"*Cone [<name>] [<colour>]\n"
			"{\n"
			"	*Data {<solid_angle> <tip_to_top> <tip_to_base>}\n"
			"	[*Facets {<layers>, <wedges>}]\n"
			"	[*Scale {<sx> <sy>}]\n"
			"	[@AxisId]\n"
			"	$Textured\n"
			"	$ObjectModifiers\n"
			"}\n"
			"*Tube [<name>] [<colour>]\n"
			"{\n"
			"	*CrossSection\n"
			"	{\n"
			"		[*Round {<radius0> [<radius1>]}]\n"
			"		[*Square {<radius0> [<radius1>]}]\n"
			"		[*Polygon {(<x> <y>)}]\n"
			"		[*Facets {<facet_count>}]\n"
			"		[@Smooth]\n"
			"	}\n"
			"	[*PerItemColour]\n"
			"	*Data {(<x> <y> <z> [<colour>])}\n"
			"	[*Closed {}]\n"
			"	[@Smooth]\n"
			"	$ObjectModifiers\n"
			"}\n"
			"*Mesh [<name>] [<colour>]\n"
			"{\n"
			"	*Verts {(<x> <y> <z>)}\n"
			"	[*Normals {(<nx> <ny> <nz>)}]\n"
			"	[*Colours {(<colour>)}]\n"
			"	[*TexCoords {(<tx> <ty>)}]\n"
			"	[*Lines {(<i0> <i1>)}]\n"
			"	[*LineList {(<i0> <i1>)}]\n"
			"	[*LineStrip {<i0> (<i1>)}]\n"
			"	[*Faces {(<i0> <i1> <i2>)}]\n"
			"	[*TriList {(<i0> <i1> <i2>)}]\n"
			"	[*TriStrip {<i0> <i1> (<i2>)}]\n"
			"	[*Tetra {(<i0> <i1> <i2> <i3>)}]\n"
			"	[@GenerateNormals]\n"
			"	$Textured\n"
			"	$ObjectModifiers\n"
			"}\n"
			"*ConvexHull [<name>] [<colour>]\n"
			"{\n"
			"	*Verts {(<x> <y> <z>)}\n"
			"	[@GenerateNormals]\n"
			"	$Textured\n"
			"	$ObjectModifiers\n"
			"}\n"
			"**Series [<name>] [<colour>]\n"
			"{\n"
			"	*XAxis {<expr>}\n"
			"	*YAxis {<expr>}\n"
			"	[@Width]\n"
			"	[@Dashed]\n"
			"	[@Smooth]\n"
			"}\n"
			"*Chart [<name>] [<colour>]\n"
			"{\n"
			"	[*FilePath {<datafile_csv>}]\n"
			"	[*Dim {<width> [<height>]}]\n"
			"	[*Data {(<val>)}]\n"
			"	[@Series]\n"
			"	$ObjectModifiers\n"
			"}\n"
			"*Model [<name>] [<colour>]\n"
			"{\n"
			"	*FilePath {<model_filepath>}\n"
			"	[@Animation]\n"
			"	[@GenerateNormals]\n"
			"	[@BakeTransform]\n"
			"	$ObjectModifiers\n"
			"}\n"
			"**EquationAxis\n"
			"{\n"
			"	[*Range {<min> <max>}]\n"
			"	[*Colours {(<value> <colour>)}]\n"
			"}\n"
			"*Equation [<name>] [<colour>]\n"
			"{\n"
			"	*Data {<equation>}\n"
			"	[*Resolution {<vertex_count>}]\n"
			"	[*Param {<name> <value>}]\n"
			"	[*Weight {<weight>}]\n"
			"	[*XAxis {$EquationAxis}]\n"
			"	[*YAxis {$EquationAxis}]\n"
			"	[*ZAxis {$EquationAxis}]\n"
			"	$ObjectModifiers\n"
			"}\n"
		);
		return str;
	}
}
