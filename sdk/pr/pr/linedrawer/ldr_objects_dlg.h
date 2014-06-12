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
		{
			std::shared_ptr<ObjectManagerDlgImpl> m_dlg; // pImpl pattern, to hide the atl includes. shared_ptr because it's an incomplete type

			ObjectManagerDlg(ObjectManagerDlg const&);
			ObjectManagerDlg& operator=(ObjectManagerDlg const&);

		public:
			ObjectManagerDlg(HWND parent = 0);
			bool IsChild(HWND hwnd) const;

			// Remove all objects
			void Clear();

			// Add 'obj' to the dialog
			void Add(LdrObjectPtr obj);

			// Add 'objects' recursively to the dialog
			template <typename TCont> void Populate(TCont const& objects)
			{
				for (auto& obj : objects)
					Add(obj);
			}

			// Display the object manager window
			void Show(bool show);

			// Return the number of selected objects
			size_t SelectedCount() const;

			// Display a window containing the example script
			void ShowScript(std::string script, HWND parent);

			// Return a bounding box of the objects
			pr::BBox GetBBox(EObjectBounds bbox_type) const;

			// Get/Set settings for the object manager window
			std::string Settings() const;
			void Settings(char const* settings);

		//private:
		//	// An object has been added to the data manager
		//	void OnEvent(Evt_LdrObjectAdd const&) override;

		//	// Empty the tree and list controls, all objects have been deleted
		//	void OnEvent(Evt_DeleteAll const&) override;

		//	// Remove an object from the tree and list controls
		//	void OnEvent(Evt_LdrObjectDelete const&) override;
		};
	}
}

#endif
