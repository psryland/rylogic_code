//*****************************************************************************
//
//	Resource monitor
//
//*****************************************************************************
// Create an instance of this object to monitor a directory of resouce files
// and reload any that have been updated/modified
#ifndef PR_RDR_RESOURCE_MONITOR_H
#define PR_RDR_RESOURCE_MONITOR_H

#include "pr/common/StdVector.h"
#include "pr/common/StdMap.h"
#include "pr/common/StdString.h"
#include "pr/common/PRTypes.h"
#include "pr/common/Fmt.h"
#include "pr/common/Crc.h"
#include "pr/filesys/filesys.h"
#include "pr/renderer/types/Forward.h"

namespace pr
{
	namespace rdr
	{
		namespace watch
		{
			struct Resource;

			// Sync function for a resource
			typedef bool (*OnSyncFunc)(Resource&, Renderer&);

			struct Resource
			{
				Resource();
				std::string				m_filename;			// The filename for the resource relative to root directory
				pr::filesys::uint64		m_last_modified;	// The filetime when it was last modified
				bool					m_has_dependents;	// True if resources of this type have dependent files
				OnSyncFunc				m_sync_func;		// The sync function for the resource
				void*					m_user_data;		// Extra data associated with the resource
			};
			typedef std::vector<Resource> TWatched;

			// A record of a file that at least one resource is dependent on
			struct Dependent
			{
				typedef std::vector<CRC> TDeps;

				std::string				m_filename;			// The filename of the dependent file
				pr::filesys::uint64		m_last_modified;	// The filetime when it was last modified
				TDeps					m_dependents;		// The crc's of Resource filenames that depend on this file
			};

			// Effect type helper functions. 
			// Example usage: 
			//	TWatched watched;
			//	watched.push_back(watch::BuildInEffect("XYZTint.fx", EEffect_XYZTint));
			Resource BuiltInEffect(std::string filename, RdrId effect_id);
			Resource Texture2d    (std::string filename, RdrId texture_id);
			Resource UserEffect   (std::string filename, Effect* effect);
		}//namespace watch
		
		// Include paths
		typedef std::vector<std::string> TPaths;

		// Create one of these to have the resources in a directory refreshed when their source files change
		class ResourceMonitor
		{
		public:
			ResourceMonitor(Renderer& renderer, const watch::TWatched& watched, const TPaths& include_paths);

			// Call this periodically to refresh changed resources. Returns true if all resource
			// updates occurred successfully, false if any failed.
			bool Sync();
			bool Sync(std::size_t step_division) { if( ++m_step_divisor >= step_division ) { return Sync(); } else { return true; } }

		private:
			std::string ResolveFilename(const std::string& filename) const;
			void AddDependents(const std::string& filename, CRC resource_crc);
			void AddDependent (const std::string& filename, const std::string& include_file, CRC resource_crc);

		private:
			typedef std::map<CRC, watch::Resource>	TResources;
			typedef std::map<CRC, watch::Dependent>	TDependents;
			
			Renderer*		m_renderer;
			TPaths			m_include_paths;
			TResources		m_resource;
			TDependents		m_dependent;
			std::size_t		m_step_divisor;
			std::size_t		m_message_id;
		};

	}//namespace rdr
}//namespace pr

#endif//PR_RDR_RESOURCE_MONITOR_H
