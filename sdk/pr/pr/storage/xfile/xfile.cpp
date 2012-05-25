//************************************************************************
//
// Functions for loading and saving xfiles
//
//************************************************************************

#include <rmxftmpl.h>
#include "pr/storage/xfile/xfile.h"
#include "pr/storage/xfile/xloader.h"
#include "pr/storage/xfile/xsaver.h"
#include "pr/storage/xfile/xfileinternal.h"

unsigned char* D3DTemplates     = D3DRM_XTEMPLATES;
unsigned int   D3DTemplateBytes = D3DRM_XTEMPLATE_BYTES;

namespace pr
{
	namespace xfile
	{
		namespace impl
		{
			// Return the name of a data object
			std::string GetName(D3DPtr<ID3DXFileData> data)
			{
				SIZE_T name_length;
				Verify(data->GetName(0, &name_length));

				std::string name;
				name.resize(name_length, 0);
				Verify(data->GetName(&name[0], &name_length));
				return name;
			}

			// Return the GUID of a data object
			GUID GetGUID(D3DPtr<ID3DXFileData> data)
			{
				GUID guid;
				Verify(data->GetType(&guid));
				return guid;
			}

			// Return the number of child objects in 'data'
			std::size_t GetNumChildren(D3DPtr<ID3DXFileData> data)
			{
				SIZE_T num_children = 0;
				Verify(data->GetChildren(&num_children));
				return num_children;
			}

			// Recursive convert function
			void Convert(D3DPtr<ID3DXFileData> object, D3DPtr<ID3DXFileSaveData> parent)
			{
				// Get access to the data of this object
				XData xdata(object);

				// Save this data
				D3DPtr<ID3DXFileSaveData> save_data;
				if( Failed(parent->AddDataObject(impl::GetGUID(object), impl::GetName(object).c_str(), 0, xdata.m_size, xdata.m_ptr.m_data, &save_data.m_ptr)) ) { throw Exception(EResult_AddDataFailed); }

				// Convert any children of 'object'
				SIZE_T num_children = GetNumChildren(object);
				for( std::size_t c = 0; c != num_children; ++c )
				{
					// Get the child object
					D3DPtr<ID3DXFileData> child = 0;
					if( Failed(object->GetChild(c, &child.m_ptr)) ) { throw Exception(EResult_GetChildFailed); }

					// Convert the child
					Convert(child, save_data);
				}
			}
		}//namespace impl
	}//namespace xfile
}//namespace pr

using namespace pr;
using namespace pr::xfile;

//*****
// Load an x file
EResult pr::xfile::Load(const char *xfilename, Geometry& geometry, const TGUIDSet* partial_load_set, const void* custom_templates, unsigned int custom_templates_size)
{
	XLoader xloader(custom_templates, custom_templates_size);
	return xloader.Load(xfilename, geometry, partial_load_set);
}

//*****
// Save an x file
EResult pr::xfile::Save(const Geometry& geometry, const char* xfilename, const TGUIDSet* partial_save_set, const void* custom_templates, unsigned int custom_templates_size)
{
	XSaver xsaver;
	return xsaver.Save(geometry, xfilename, partial_save_set, custom_templates, custom_templates_size);
}

//*****
// Convert an x file
EResult pr::xfile::Convert(const char* xfilename_in, const char* xfilename_out, EConvert::Type convert_type)
{
	D3DPtr<ID3DXFile> d3d_xfile;
	
	// Create an X file interface
	Verify(D3DXFileCreate(&d3d_xfile.m_ptr));

	// Register the direct x templates
	Verify(d3d_xfile->RegisterTemplates((void*)D3DTemplates, D3DTemplateBytes));

	// Create the enum object
	D3DPtr<ID3DXFileEnumObject> enum_object;
	if( Failed(d3d_xfile->CreateEnumObject(xfilename_in, D3DXF_FILELOAD_FROMFILE, &enum_object.m_ptr)) )
	{
		return EResult_EnumerateFileFailed;
	}

	// Register templates given in the x file
	Verify(d3d_xfile->RegisterEnumTemplates(enum_object.m_ptr));

	// Create the output file a.k.a the save object
	D3DPtr<ID3DXFileSaveObject> save_object;
	if (Failed(d3d_xfile->CreateSaveObject(xfilename_out, D3DXF_FILESAVE_TOFILE, convert_type, &save_object.m_ptr)))
	{
		return EResult_FailedToCreateSaveObject;
	}

	try
	{
		// Enumerate the top level objects
		SIZE_T num_top_level_objects = 0;
		Verify(enum_object->GetChildren(&num_top_level_objects));
		for( std::size_t i = 0; i != num_top_level_objects; ++i )
		{
			// Get the object
			D3DPtr<ID3DXFileData> object = 0;
			if( Failed(enum_object->GetChild(i, &object.m_ptr)) ) { throw Exception(EResult_GetChildFailed); }

			// Get access to the data of this object
			XData xdata(object);

			// Make a save data object for this object
			D3DPtr<ID3DXFileSaveData> save_data = 0;
			if( Failed(save_object->AddDataObject(impl::GetGUID(object), impl::GetName(object).c_str(), 0, xdata.m_size, xdata.m_ptr.m_data, &save_data.m_ptr)) )	{ throw Exception(EResult_AddDataFailed); }

			// Convert any children of 'object'
			SIZE_T num_children = impl::GetNumChildren(object);
			for( std::size_t c = 0; c != num_children; ++c )
			{
				// Get the child object
				D3DPtr<ID3DXFileData> child = 0;
				if( Failed(object->GetChild(c, &child.m_ptr)) ) { throw Exception(EResult_GetChildFailed); }

				// Convert the child
				impl::Convert(child, save_data);
			}
		}

		// Save the converted xfile
		if (Failed(save_object->Save())) { throw Exception(EResult_SaveFailed); }
	}
	catch(pr::xfile::Exception const& e) { return e.code(); }
	return EResult_Success;
}
