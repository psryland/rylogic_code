//****************************************
//
//	Custom Xfile template definitions
//
//****************************************

#ifndef PRXFILETEMPLATES_H
#define PRXFILETEMPLATES_H

// GUID definitions
#define DECLARE_XFILE_TEMPLATE(template_name, GUID_1, GUID_2, GUID_3, GUID_4_1, GUID_4_2, GUID_4_3, GUID_4_4, GUID_4_5, GUID_4_6, GUID_4_7, GUID_4_8, text_description) \
	static const GUID TID_##template_name = { GUID_1, GUID_2, GUID_3, GUID_4_1, GUID_4_2, GUID_4_3, GUID_4_4, GUID_4_5, GUID_4_6, GUID_4_7, GUID_4_8 };
#include "PRXFileTemplatesInc.h"

// CustomTemplates string
#define DECLARE_XFILE_TEMPLATE(template_name, GUID_1, GUID_2, GUID_3, GUID_4_1, GUID_4_2, GUID_4_3, GUID_4_4, GUID_4_5, GUID_4_6, GUID_4_7, GUID_4_8, text_description) text_description
static const char CustomTemplates[] = 
	"xof 0302txt 0064\n"
	#include "PRXFileTemplatesInc.h"
	"";

// Length of the CustomTemplates string.
// NOTE: the '-1' is because registering templates will fail if a '0' is found in the string
static const unsigned int CustomTemplates_Bytes = (unsigned int)sizeof(CustomTemplates) - 1;

// Array of GUIDs
#define DECLARE_XFILE_TEMPLATE(template_name, GUID_1, GUID_2, GUID_3, GUID_4_1, GUID_4_2, GUID_4_3, GUID_4_4, GUID_4_5, GUID_4_6, GUID_4_7, GUID_4_8, text_description) &TID_##template_name,
static const GUID* CustomTemplateGUIDArray[] = 
{
	#include "PRXFileTemplatesInc.h"
	0
};

// Number of items in 'CustomTemplateGUIDArray'
static const unsigned int CustomTemplateGUIDArray_Count = (unsigned int)(sizeof(CustomTemplateGUIDArray) / sizeof(GUID*) - 1);

#endif//PRXFILETEMPLATES_H