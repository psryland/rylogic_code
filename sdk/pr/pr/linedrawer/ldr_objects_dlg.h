//***************************************************************************************************
// Ldr Object Manager
//  Copyright (c) Rylogic Ltd 2009
//***************************************************************************************************

#pragma once
#ifndef PR_LDR_OBJECT_MANAGER_DLG_H
#define PR_LDR_OBJECT_MANAGER_DLG_H

#include <set>
#include <memory>
#include "pr/common/events.h"
#include "pr/maths/maths.h"
#include "pr/linedrawer/ldr_object.h"

namespace pr
{
	namespace ldr
	{
		// A GUI for modifying the LdrObjects in existence.
		// LdrObject is completely unaware that this class exists.
		// Note: this object does not add references to LdrObjects
		struct ObjectManagerDlgImpl;
		class ObjectManagerDlg
			:pr::events::IRecv<Evt_LdrObjectAdd>
			,pr::events::IRecv<Evt_DeleteAll>
			,pr::events::IRecv<Evt_LdrObjectDelete>
		{
			std::shared_ptr<ObjectManagerDlgImpl> m_dlg; // pImpl pattern
			std::set<ContextId> m_ignore_ctxids;         // Context ids not to display in the object manager
			mutable pr::BBox m_scene_bbox;               // A cached bounding box of all objects we know about (lazy updated)

			ObjectManagerDlg(ObjectManagerDlg const&);
			ObjectManagerDlg& operator=(ObjectManagerDlg const&);

		public:
			ObjectManagerDlg(HWND parent = 0);
			bool IsChild(HWND hwnd) const;

			// Display the object manager window
			void Show(bool show);

			// Return the number of selected objects
			size_t SelectedCount() const;

			// Display a window containing the example script
			void ShowScript(std::string script, HWND parent);

			// Set the ignore state for a particular context id
			// Should be called before objects are added to the obj mgr
			void IgnoreContextId(ContextId id, bool ignore);

			// Return a bounding box of the objects
			pr::BBox GetBBox(EObjectBounds bbox_type) const;

			// Get/Set settings for the object manager window
			std::string Settings() const;
			void Settings(char const* settings);

		private:
			// An object has been added to the data manager
			void OnEvent(Evt_LdrObjectAdd const&) override;

			// Empty the tree and list controls, all objects have been deleted
			void OnEvent(Evt_DeleteAll const&) override;

			// Remove an object from the tree and list controls
			void OnEvent(Evt_LdrObjectDelete const&) override;
		};
	}
}

#endif
