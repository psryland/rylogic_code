//***************************************************************************************************
// Ldr Object Manager
//  Copyright (c) Rylogic Ltd 2009
//***************************************************************************************************

#pragma once

#include <set>
#include <memory>
#include "pr/common/events.h"
#include "pr/maths/maths.h"
#include "pr/linedrawer/ldr_object.h"

namespace pr
{
	namespace ldr
	{
		// User interface for managing LdrObjects
		// LdrObject is completely unaware that this class exists.
		// Note: this object does not add references to LdrObjects
		struct LdrObjectManagerUI
		{
		protected:

			// pImpl pattern to hide window implementation
			std::unique_ptr<LdrObjectManagerUI> m_ui;

			struct Internal {};
			LdrObjectManagerUI(Internal const&) {}

		public:

			virtual ~LdrObjectManagerUI() {}
			LdrObjectManagerUI(HWND parent);
			LdrObjectManagerUI(LdrObjectManagerUI const&) = delete;
			LdrObjectManagerUI& operator=(LdrObjectManagerUI const&) = delete;

			// Implicit conversion to HWND
			virtual operator HWND() const { return m_ui->operator HWND(); }

			// Get/Set settings for the object manager window
			virtual std::string Settings() const { return m_ui->Settings(); }
			virtual void Settings(std::string const& settings) { m_ui->Settings(settings); }

			// Display the object manager window
			virtual void Show(HWND parent = nullptr) { m_ui->Show(parent); }

			// Repopulate the dialog with the collection 'cont'
			template <typename ObjectCont> void Populate(ObjectCont const& cont)
			{
				struct L {
				static LdrObject* pointer(LdrObjectPtr p) { return p.m_ptr; }
				static LdrObject* pointer(LdrObject*   p) { return p; }
				};
				BeginPopulate();
				for (auto obj : cont) Add(L::pointer(obj));
				EndPopulate();
			}

			// Begin repopulating the dialog
			virtual void BeginPopulate() { m_ui->BeginPopulate(); }
			
			// Add a root level object recursively to the dialog
			virtual void Add(LdrObject* obj) { m_ui->Add(obj); }

			// Finished populating the dialog
			virtual void EndPopulate() { m_ui->EndPopulate(); }

			// Return the number of selected objects
			virtual size_t SelectedCount() const { return m_ui->SelectedCount(); }

			// Enumerate the selected items
			// 'iter' is an 'in/out' parameter, initialise it to -1 for the first call
			// Returns 'nullptr' after the last selected item
			virtual LdrObject const* EnumSelected(int& iter) const { return m_ui->EnumSelected(iter); }

			// Position the window relative to the owner window
			virtual void PositionWindow(int x, int y, int w, int h) { return m_ui->PositionWindow(x,y,w,h); }

			// Get/Set the visibility of the window
			virtual bool Visible() const { return m_ui->Visible(); }
			virtual void Visible(bool show) { m_ui->Visible(show); }

			// Hide the window instead of closing
			virtual bool HideOnClose() const { return m_ui->HideOnClose(); }
			virtual void HideOnClose(bool enable) { m_ui->HideOnClose(enable); }
		};

		#pragma region Events

		// Called when one or more objects have changed state
		struct Evt_Refresh
		{
			LdrObjectManagerUI* m_ui;  // The sender of the event
			LdrObjectPtr        m_obj; // The object that has changed. If null, then more than one object has changed

			Evt_Refresh(LdrObjectManagerUI* sender) :m_ui(sender) ,m_obj(0) {}
			Evt_Refresh(LdrObjectManagerUI* sender, LdrObjectPtr obj) :m_ui(sender) ,m_obj(obj) {}
		};

		// Event fired from the UI when the selected object changes
		struct Evt_LdrObjectSelectionChanged
		{
			LdrObjectManagerUI* m_ui;
			Evt_LdrObjectSelectionChanged(LdrObjectManagerUI* sender) :m_ui(sender) {}
		};

		// Sent by the object manager UI whenever its settings have changed
		struct Evt_SettingsChanged
		{
			LdrObjectManagerUI* m_ui;
			Evt_SettingsChanged(LdrObjectManagerUI* sender) :m_ui(sender) {}
		};

		#pragma endregion
	}
}
