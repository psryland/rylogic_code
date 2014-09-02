// Macro include file for custom xfile templates
// Note: use "UUIDGen" in "C:\Program Files\Microsoft Visual Studio .NET\Common7\Tools\Bin" to generate the guid
//
//	Format: 
//	DECLARE_XFILE_TEMPLATE(template_name, GUID_1, GUID_2, GUID_3, GUID_4_1, GUID_4_2, GUID_4_3, GUID_4_4, GUID_4_5, GUID_4_6, GUID_4_7, GUID_4_8,
//		text_description
//		)
// e.g.
//DECLARE_XFILE_TEMPLATE(PRGeometryType, 0x176e845d, 0x7fb5, 0x49a5, 0xb7, 0xc6, 0x53, 0x6b, 0x5b, 0xe2, 0x61, 0xf1,
//	"template PRGeometryType {\n"
//		"<176e845d-7fb5-49a5-b7c6-536b5be261f1>\n"
//		"STRING	geometry_type;\n"
//		"}\n")
//

// Default macro definition
#ifndef DECLARE_XFILE_TEMPLATE
#define DECLARE_XFILE_TEMPLATE(template_name, GUID_1, GUID_2, GUID_3, GUID_4_1, GUID_4_2, GUID_4_3, GUID_4_4, GUID_4_5, GUID_4_6, GUID_4_7, GUID_4_8, text_description)
#endif//DECLARE_XFILE_TEMPLATE

#undef DECLARE_XFILE_TEMPLATE
