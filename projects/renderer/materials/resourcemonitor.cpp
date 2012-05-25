//*********************************************
// Renderer
//	(c)opyright Rylogic Ltd 2007
//*********************************************

// Create an instance of this object to monitor a directory of resouce files
// and reload any that have been updated/modified
#include "renderer/utility/stdafx.h"
#include "pr/renderer/materials/resourcemonitor.h"
#include "pr/renderer/renderer/renderer.h"

using namespace pr;
using namespace pr::rdr;
using namespace pr::rdr::watch;

const char g_include_directive[] = "#include";

namespace pr
{
	namespace rdr
	{
		namespace watch
		{
			// Default sync function
			bool DefaultOnSyncFunc(Resource&, Renderer&)
			{
				PR_ASSERT_STR(PR_DBG_RDR, false, "OnSync function not provided");
				return false;
			}

			Resource::Resource()
			:m_filename("")
			,m_last_modified(0)
			,m_has_dependents(false)
			,m_sync_func(DefaultOnSyncFunc)
			,m_user_data(0)
			{}

			// Return a resource for a built in effect
			bool OnSyncBuiltInEffect(Resource& res, Renderer& renderer)
			{
				RdrId id = static_cast<RdrId>(reinterpret_cast<std::ptrdiff_t>(res.m_user_data));
				bool result = Succeeded(renderer.m_material_manager.ReplaceEffect(id, res.m_filename.c_str()));
				if( !result ) { PR_INFO(PR_DBG_RDR, Fmt("Failed to refresh effect '%s'\n", res.m_filename.c_str()).c_str()); }
				return result;
			}
			Resource BuiltInEffect(std::string filename, RdrId effect_id)
			{
				Resource res;
				res.m_filename			= filename;
				res.m_has_dependents	= true;
				res.m_sync_func			= OnSyncBuiltInEffect;
				res.m_user_data			= reinterpret_cast<void*>(static_cast<std::ptrdiff_t>(effect_id));
				return res;
			}

			// Return a resource for a texture
			bool OnSyncTexture2D(Resource& res, Renderer& renderer)
			{
				RdrId id = static_cast<RdrId>(reinterpret_cast<std::ptrdiff_t>(res.m_user_data));
				bool result = Succeeded(renderer.m_material_manager.ReplaceTexture(id, res.m_filename.c_str()));
				if( !result ) { PR_INFO(PR_DBG_RDR, Fmt("Failed to refresh texture '%s'\n", res.m_filename.c_str()).c_str()); }
				return result;
			}
			Resource Texture2d(std::string filename, RdrId texture_id)
			{
				Resource res;
				res.m_filename			= filename;
				res.m_has_dependents	= false;
				res.m_sync_func			= OnSyncTexture2D;
				res.m_user_data			= reinterpret_cast<void*>(static_cast<std::ptrdiff_t>(texture_id));
				return res;
			}

			// A user loaded effect
			bool OnSyncUserEffect(Resource& res, Renderer& renderer)
			{
				Effect* effect = static_cast<Effect*>(res.m_user_data);
				bool result = effect->Create(	renderer.GetD3DDevice(),
												renderer.m_material_manager.GetEffectPool(),
												effect->m_effect_id,
												effect->m_geometry_type,
												res.m_filename.c_str(),
												"v9_9");
				if( !result ) { PR_INFO(PR_DBG_RDR, Fmt("Failed to refresh user effect'%s'\n", res.m_filename.c_str()).c_str()); }
				return result;
			}
			Resource UserEffect(std::string filename, Effect* effect)
			{
				Resource res;
				res.m_filename			= filename;
				res.m_has_dependents	= true;
				res.m_sync_func			= OnSyncUserEffect;
				res.m_user_data			= effect;
				return res;
			}
		}//namespace watch
	}//namespace rdr
}//namespace pr

//*****
ResourceMonitor::ResourceMonitor(Renderer& renderer, const TWatched& watched, const TPaths& include_paths)
:m_renderer(&renderer)
,m_include_paths(include_paths)
,m_step_divisor(0)
,m_message_id(0)
{
	// Build up a map of the resource files to watch.
	for( TWatched::const_iterator w = watched.begin(), w_end = watched.end(); w != w_end; ++w )
	{
		// Make a crc for the filename to use in the map
		std::string filename = ResolveFilename(w->m_filename);
		if( filename.empty() ) continue;
		CRC file_crc = Crc(&filename[0], filename.size());

		// Add to the resource map
		Resource& res		= m_resource[file_crc];
		res					= *w;
		res.m_filename		= filename;
		res.m_last_modified	= pr::filesys::GetFileTimeStats(filename).m_last_modified;

        // If the resource can have dependences add them
		if( res.m_has_dependents )
		{
			AddDependents(res.m_filename, file_crc);
		}
	}
}

// Look for the first full path that exists
std::string ResourceMonitor::ResolveFilename(std::string const& filename) const
{
	// Check the filename itself first
	if( pr::filesys::DoesFileExist(filename) ) return filename;

	// Try concatinating it with the include directories
	std::string full_path;
	for( TPaths::const_iterator p = m_include_paths.begin(), p_end = m_include_paths.end(); p != p_end; ++p )
	{
		full_path = pr::filesys::Make(*p, filename);
		if( pr::filesys::DoesFileExist(full_path) ) return full_path;
	}

	// <shrug>
	PR_INFO(PR_DBG_RDR, Fmt("Failed to resolve path for '%s'\n", filename.c_str()).c_str());
	return "";
}

// Look for the dependents of 'filename'. 'filename' should be a full path
void ResourceMonitor::AddDependents(std::string const& filename, CRC resource_crc)
{
	// Search through the file for "#include"
	Handle file = FileOpen(filename.c_str(), EFileOpen_Reading);
	if (file == INVALID_HANDLE_VALUE) { PR_INFO(PR_DBG_RDR, Fmt("Failed to open dependent file: '%s'", filename.c_str()).c_str()); return; }

	std::size_t const buf_size		= 4096;
	std::size_t const overlap_size	= 256;
	std::string buf(buf_size, '\0');
	
	std::size_t buffer_size = buf_size;
	char*		buffer      = &buf[0];
	DWORD		bytes_read  = 0;
	for(;;)
	{
		FileRead(file, buffer, DWORD(buffer_size), &bytes_read);

		// Search for the include directive in the buffer
		std::size_t ofs = buf.find(g_include_directive);
		while( ofs != std::string::npos )
		{
			// Find the starting '"' or '<' character
			std::size_t start_ofs = buf.find_first_of("\"<\n", ofs + sizeof(g_include_directive));
			if( start_ofs == std::string::npos ) break;

			// If a newline was found before the include path then start searching again
			if( buf[start_ofs] == '\n' )
			{
				PR_INFO(PR_DBG_RDR, Fmt("Failed to find path following an include directive in '%s'", filename.c_str()).c_str());
				ofs = buf.find(g_include_directive, start_ofs + 1);
				continue;
			}

			// Find the ending '"' or '>' character
			std::size_t ending_ofs = buf.find_first_of("\">\n", start_ofs + 1);
			if( ending_ofs == std::string::npos ) break;

			// If a newline was found before the include path then start searching again
			if( buf[ending_ofs] == '\n' )
			{
				PR_INFO(PR_DBG_RDR, Fmt("Newline found in path following an include directive in '%s'", filename.c_str()).c_str());
				ofs = buf.find(g_include_directive, ending_ofs + 1);
				continue;
			}

			// We've got an include path
			std::string include_file = buf.substr(start_ofs + 1, ending_ofs - start_ofs - 1);

			// Add the dependency
			AddDependent(filename, include_file, resource_crc);			

			// Continue the search
			ofs = buf.find(g_include_directive, ending_ofs + 1);
		}

		if( bytes_read == buffer_size )
		{
			// Copy the last 256 bytes to the start of the buffer in case the
			// include directory happens to fall on a read boundary
			memcpy(&buf[0], &buf[buf_size - overlap_size], overlap_size);
			buffer		= &buf[overlap_size];
			buffer_size = buf_size - overlap_size;
		}
		else
		{
			break;
		}
	}
}

// Add a file that the resource 'resource_crc' is dependent on
void ResourceMonitor::AddDependent(std::string const& filename, std::string const& include_file, CRC resource_crc)
{
	// Add the directory from 'filename' to the include paths for the purpose
	// of resolving this include file into a full path.
	m_include_paths.push_back(pr::filesys::GetDirectory(filename));
	std::string dependent_filename = ResolveFilename(include_file);
	m_include_paths.pop_back();
	
	// Failed to resolve the include file, an error has already been reported, just return
	if( dependent_filename.empty() ) return;
	
	CRC file_crc = Crc(&dependent_filename[0], dependent_filename.size());

	// See whether we already have this dependency
	TDependents::iterator dep = m_dependent.find(file_crc);
	if( dep == m_dependent.end() )
	{
        // Add the dependency
		Dependent dependent;
		dependent.m_filename      = dependent_filename;
		dependent.m_last_modified = pr::filesys::GetFileTimeStats(dependent_filename).m_last_modified;

		dep = m_dependent.insert(TDependents::value_type(file_crc, dependent)).first;
	}

	// Ensure that 'resource_crc' is in the dependents list for this file
	Dependent& dependent = dep->second;
	if( std::find(dependent.m_dependents.begin(), dependent.m_dependents.end(), resource_crc) == dependent.m_dependents.end() )
	{
		dependent.m_dependents.push_back(resource_crc);
	}

	// Look for dependents of this file
	AddDependents(dependent_filename, resource_crc);
}

// Synchronise any modified resources
bool ResourceMonitor::Sync()
{
	// Search through the dependencies looking for any that have changed.
	// For those that have, mark the resources so that they'll get updated.
	for( TDependents::iterator d = m_dependent.begin(), d_end = m_dependent.end(); d != d_end; ++d )
	{
		pr::filesys::uint64 last_modified_time = pr::filesys::GetFileTimeStats(d->second.m_filename).m_last_modified;
		if( last_modified_time != d->second.m_last_modified )
		{
			d->second.m_last_modified = last_modified_time;
			for( Dependent::TDeps::iterator dep = d->second.m_dependents.begin(), dep_end = d->second.m_dependents.end(); dep != dep_end; ++dep )
			{
				TResources::iterator res = m_resource.find(*dep);
				PR_ASSERT_STR(PR_DBG_RDR, res != m_resource.end(), "Resource not found");
				res->second.m_last_modified = 0;
			}
		}
	}

	bool result = true;
	bool update_occurred = false;

	// Search through the resources looking for files whose time stamp is different to what we have recorded for them
	for( TResources::iterator r = m_resource.begin(), r_end = m_resource.end(); r != r_end; ++r )
	{
		pr::filesys::uint64 last_modified_time = pr::filesys::GetFileTimeStats(r->second.m_filename).m_last_modified;
		if( last_modified_time != r->second.m_last_modified )
		{
			update_occurred = true;
			Resource& res		= r->second;
			res.m_last_modified = last_modified_time;
			result &= res.m_sync_func(res, *m_renderer);
		}
	}

	if( update_occurred ) { PR_INFO(PR_DBG_RDR, Fmt("(%d) Updated %s\n", ++m_message_id, result ? "Succeeded" : "Failed").c_str()); }
	m_step_divisor = 0;
	return result;
}

