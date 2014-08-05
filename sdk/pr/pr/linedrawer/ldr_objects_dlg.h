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
			virtual void Show(pr::ldr::ObjectCont const& store, HWND parent = 0) = 0;

			// Display a window containing the example script
			virtual void ShowScript(std::string script, HWND parent) = 0;

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

			// Display the object manager window
			void Show(pr::ldr::ObjectCont const& store, HWND parent) override
			{
				m_dlg->Show(store, parent);
			}

			// Display a window containing the example script
			void ShowScript(std::string script, HWND parent) override
			{
				m_dlg->ShowScript(script, parent);
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
