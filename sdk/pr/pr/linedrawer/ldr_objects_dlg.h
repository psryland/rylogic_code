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
		// Object Manager Interface
		struct IObjectManagerDlg
		{
			virtual ~IObjectManagerDlg() {}

			// Create the non-modal window
			virtual void Create(HWND parent) = 0;

			// Close/Destroy the dialog window
			virtual void Close() = 0;
			virtual void Detach() = 0;

			// Get/Set the visibility of the window
			virtual bool Visible() const = 0;
			virtual void Visible(bool show) = 0;

			// Get/Set settings for the object manager window
			virtual std::string Settings() const = 0;
			virtual void Settings(std::string settings) = 0;

			// Display the object manager window
			virtual void Show(HWND parent = 0) = 0;

			// Begin repopulating the dlg
			virtual void BeginPopulate() = 0;
			
			// Add a root level object recursively to the dlg
			virtual void Add(LdrObject* obj) = 0;

			// Finished populating the dlg
			virtual void EndPopulate() = 0;

			// Return the number of selected objects
			virtual size_t SelectedCount() const = 0;

			// Enumerate the selected items
			// 'iter' is an 'in/out' parameter, initialise it to -1 for the first call
			// Returns 'nullptr' after the last selected item
			virtual LdrObject const* EnumSelected(int& iter) const = 0;
		};
	
		// A GUI for modifying the LdrObjects in existence.
		// LdrObject is completely unaware that this class exists.
		// Note: this object does not add references to LdrObjects
		class ObjectManagerDlg :IObjectManagerDlg
		{
			// pImpl pattern, to hide the atl includes.
			std::unique_ptr<IObjectManagerDlg> m_dlg;

			ObjectManagerDlg(ObjectManagerDlg const&);
			ObjectManagerDlg& operator=(ObjectManagerDlg const&);

			LdrObject* pointer(LdrObjectPtr p) { return p.m_ptr; }
			LdrObject* pointer(LdrObject*   p) { return p; }

			// Begin repopulating the dlg
			void BeginPopulate()
			{
				m_dlg->BeginPopulate();
			}
			
			// Add a root level object recursively to the dlg
			void Add(LdrObject* obj)
			{
				m_dlg->Add(obj);
			}

			// Finished populating the dlg
			void EndPopulate()
			{
				m_dlg->EndPopulate();
			}

		public:
			ObjectManagerDlg();

			// Create the non-modal window
			void Create(HWND parent) override
			{
				m_dlg->Create(parent);
			}

			// Close/Destroy the dialog window
			void Close() override
			{
				m_dlg->Close();
			}
			void Detach() override
			{
				m_dlg->Detach();
			}

			// Display the object manager window
			void Show(HWND parent) override
			{
				m_dlg->Show(parent);
			}

			// Get/Set the visibility of the window
			bool Visible() const
			{
				return m_dlg->Visible();
			}
			void Visible(bool show)
			{
				m_dlg->Visible(show);
			}

			// Get/Set settings for the object manager window
			std::string Settings() const override
			{
				return m_dlg->Settings();
			}
			void Settings(std::string settings) override
			{
				m_dlg->Settings(settings);
			}

			// Repopulate the dialog with the collection 'cont'
			template <typename ObjectCont> void Populate(ObjectCont const& cont)
			{
				BeginPopulate();
				for (auto obj : cont) Add(pointer(obj));
				EndPopulate();
			}

			// Return the number of selected objects
			size_t SelectedCount() const override
			{
				return m_dlg->SelectedCount();
			}

			// Enumerate the selected items
			// 'iter' is an 'in/out' parameter, initialise it to -1 for the first call
			// Returns 'nullptr' after the last selected item
			LdrObject const* EnumSelected(int& iter) const
			{
				return m_dlg->EnumSelected(iter);
			}
		};

		#pragma region Events

		// Called when one or more objects have changed state
		struct Evt_Refresh
		{
			IObjectManagerDlg* m_ui;  // The sender of the event
			LdrObjectPtr       m_obj; // The object that has changed. If null, then more than one object has changed

			Evt_Refresh(IObjectManagerDlg* sender) :m_ui(sender) ,m_obj(0) {}
			Evt_Refresh(IObjectManagerDlg* sender, LdrObjectPtr obj) :m_ui(sender) ,m_obj(obj) {}
		};

		// Event fired from the UI when the selected object changes
		struct Evt_LdrObjectSelectionChanged
		{
			IObjectManagerDlg* m_ui;
			Evt_LdrObjectSelectionChanged(IObjectManagerDlg* sender) :m_ui(sender) {}
		};

		// Sent by the object manager ui whenever its settings have changed
		struct Evt_SettingsChanged
		{
			IObjectManagerDlg* m_ui;
			Evt_SettingsChanged(IObjectManagerDlg* sender) :m_ui(sender) {}
		};

		#pragma endregion

	}
}
