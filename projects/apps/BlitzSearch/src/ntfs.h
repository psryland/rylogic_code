//***********************************************************************
// NTFS
//  Copyright (c) Rylogic Ltd 2024
//***********************************************************************
#pragma once
#include <memory>
#include <format>
#include <windows.h>
#include <winioctl.h>
#include "pr/common/hresult.h"

void ReadMFT()
{
	auto handle = std::shared_ptr<void>(
		CreateFileW(L"\\\\.\\C:", 0, FILE_SHARE_READ | FILE_SHARE_WRITE, nullptr, OPEN_EXISTING, 0, nullptr),
		CloseHandle);

	BOOL r;
	DWORD byte_returned;

	DISK_GEOMETRY pdg;
	r = DeviceIoControl(handle.get(), IOCTL_DISK_GET_DRIVE_GEOMETRY, nullptr, 0, &pdg, sizeof(pdg), &byte_returned, nullptr);
	pr::Check(r, "Failed to IOCTL_DISK_GET_DRIVE_GEOMETRY");

	NTFS_VOLUME_DATA_BUFFER ntfs_data = {};
	r = DeviceIoControl(handle.get(), FSCTL_GET_NTFS_VOLUME_DATA, nullptr, 0, &ntfs_data, sizeof(ntfs_data), &byte_returned, nullptr);
	pr::Check(r, "Failed to FSCTL_GET_NTFS_VOLUME_DATA");
}
