#!/usr/bin/env python3
# -*- coding: utf-8 -*- 
# Project build script
# Based on Makefiles from the Nordic SDK examples and SES

import sys, os, glob, json

repo_root = os.path.abspath(os.path.join(os.path.dirname(__file__), "../../.."))
sys.path += [os.path.join(repo_root, "script")]
from Make import ESystem, Builder, Join

# Project root directory
proj_root = os.path.dirname(__file__)
sdk_nrf = Join(repo_root, 'sdk/nordic/nrf/nRF5_SDK_17.1.0_ddde560')
sdk_arm = Join(repo_root, 'sdk/arm/build-tools/arm-gnu-toolchain-13.2.Rel1-mingw-w64-i686-arm-none-eabi')

# Instance of builder for the project
class Project(Builder):

	def __init__(self):
		Builder.__init__(self, ESystem.ARM)
		Builder.pp      = Join(sdk_arm, 'bin/arm-none-eabi-cpp.exe')
		Builder.cc      = Join(sdk_arm, 'bin/arm-none-eabi-gcc.exe')
		Builder.asm     = Join(sdk_arm, 'bin/arm-none-eabi-as.exe')
		Builder.linker  = Join(sdk_arm, 'bin/arm-none-eabi-ld.exe')
		Builder.objcopy = Join(sdk_arm, 'bin/arm-none-eabi-objcopy.exe')
		Builder.nm      = Join(sdk_arm, 'bin/arm-none-eabi-nm.exe')
		#Builder.mkld    = Join(sdk_arm, 'bin/mkld.exe')
		self.project_file = __file__
		self.target_fname = 'Project.elf'
		self.compile_via_asm = False
		self.verbosity = 1
		self.stop_on_first_error = True

	# Populate build input data
	def Setup(self):
		# The hardware to target
		self.target = 'PCA10059'
		#self.target = 'PCA10056'
		self.softdevice = True
		self.mbr_present = True

		# Set the output path
		self.outdir = Join(proj_root, 'obj', self.config, check_exists=False)

		# Source Files
		if True:
			# Any .c/.cpp/.s file under 'src/'
			for extn in ['*.c', '*.cpp', '*.s']:
				files = glob.glob(Join(proj_root, 'src', f'**/{extn}'), recursive=True)
				self.src.extend(files)

			# Nordic SDK modules
			self.src += [
				# nRF5 SDK
					# Components
						Join(sdk_nrf, 'components/boards/boards.c'),
						Join(sdk_nrf, "components/ble/common/ble_advdata.c"),
						Join(sdk_nrf, "components/ble/common/ble_conn_params.c"),
						Join(sdk_nrf, "components/ble/common/ble_srv_common.c"),
						Join(sdk_nrf, "components/ble/nrf_ble_scan/nrf_ble_scan.c"),
						Join(sdk_nrf, "components/libraries/atomic/nrf_atomic.c"),
						Join(sdk_nrf, "components/libraries/atomic_fifo/nrf_atfifo.c"),
						Join(sdk_nrf, "components/libraries/balloc/nrf_balloc.c"),
						Join(sdk_nrf, "components/libraries/bsp/bsp.c"),
						Join(sdk_nrf, "components/libraries/button/app_button.c"),
						Join(sdk_nrf, "components/libraries/cli/nrf_cli.c"),
						Join(sdk_nrf, "components/libraries/cli/rtt/nrf_cli_rtt.c"),
						Join(sdk_nrf, "components/libraries/cli/cdc_acm/nrf_cli_cdc_acm.c"),
						Join(sdk_nrf, "components/libraries/crc32/crc32.c"),
						Join(sdk_nrf, "components/libraries/experimental_section_vars/nrf_section_iter.c"),
						Join(sdk_nrf, "components/libraries/log/src/nrf_log_backend_rtt.c"),
						Join(sdk_nrf, "components/libraries/log/src/nrf_log_backend_serial.c"),
						Join(sdk_nrf, "components/libraries/log/src/nrf_log_default_backends.c"),
						Join(sdk_nrf, "components/libraries/log/src/nrf_log_frontend.c"),
						Join(sdk_nrf, "components/libraries/log/src/nrf_log_str_formatter.c"),
						Join(sdk_nrf, "components/libraries/memobj/nrf_memobj.c"),
						Join(sdk_nrf, "components/libraries/pwr_mgmt/nrf_pwr_mgmt.c"),
						Join(sdk_nrf, "components/libraries/queue/nrf_queue.c"),
						Join(sdk_nrf, "components/libraries/ringbuf/nrf_ringbuf.c"),
						Join(sdk_nrf, "components/libraries/sortlist/nrf_sortlist.c"),
						Join(sdk_nrf, "components/libraries/strerror/nrf_strerror.c"),
						Join(sdk_nrf, "components/libraries/timer/app_timer2.c"),
						Join(sdk_nrf, "components/libraries/timer/drv_rtc.c"),
						Join(sdk_nrf, 'components/libraries/usbd/app_usbd.c'),
						Join(sdk_nrf, 'components/libraries/usbd/app_usbd_core.c'),
						Join(sdk_nrf, 'components/libraries/usbd/app_usbd_serial_num.c'),
						Join(sdk_nrf, 'components/libraries/usbd/app_usbd_string_desc.c'),
						Join(sdk_nrf, 'components/libraries/usbd/class/cdc/acm/app_usbd_cdc_acm.c'),
						Join(sdk_nrf, "components/libraries/util/app_error.c"),
						Join(sdk_nrf, "components/libraries/util/app_error_handler_gcc.c"),
						Join(sdk_nrf, "components/libraries/util/app_error_weak.c"),
						Join(sdk_nrf, "components/libraries/util/app_util_platform.c"),
						Join(sdk_nrf, "components/libraries/util/nrf_assert.c"),
					# Soft device
						Join(sdk_nrf, "components/softdevice/common/nrf_sdh.c"),
						Join(sdk_nrf, "components/softdevice/common/nrf_sdh_ble.c"),
						Join(sdk_nrf, "components/softdevice/common/nrf_sdh_soc.c"),
					# External
						Join(sdk_nrf, 'external/fnmatch/fnmatch.c'),
						Join(sdk_nrf, 'external/fprintf/nrf_fprintf.c'),
						Join(sdk_nrf, 'external/fprintf/nrf_fprintf_format.c'),
						Join(sdk_nrf, "external/segger_rtt/SEGGER_RTT.c"),
						Join(sdk_nrf, "external/segger_rtt/SEGGER_RTT_printf.c"),
						Join(sdk_nrf, "external/segger_rtt/SEGGER_RTT_Syscalls_GCC.c"),
						Join(sdk_nrf, 'external/utf_converter/utf.c'),
					# Drivers
						Join(sdk_nrf, "modules/nrfx/drivers/src/nrfx_clock.c"),
						Join(sdk_nrf, "modules/nrfx/drivers/src/nrfx_power.c"),
						Join(sdk_nrf, "modules/nrfx/drivers/src/nrfx_gpiote.c"),
						Join(sdk_nrf, "modules/nrfx/drivers/src/nrfx_rtc.c"),
						Join(sdk_nrf, 'modules/nrfx/drivers/src/nrfx_usbd.c'),
						Join(sdk_nrf, "modules/nrfx/soc/nrfx_atomic.c"),
					# Legacy drivers
						Join(sdk_nrf, "integration/nrfx/legacy/nrf_drv_clock.c"),
						Join(sdk_nrf, 'integration/nrfx/legacy/nrf_drv_power.c'),
					# Startup
						Join(sdk_nrf, 'modules/nrfx/mdk/ses_startup_nrf_common.s'),
						Join(sdk_nrf, 'modules/nrfx/mdk/ses_startup_nrf52840.s'),
						Join(sdk_nrf, 'modules/nrfx/mdk/system_nrf52840.c'),
			]

		# Includes
		if True:
			# Application include paths
			self.includes += [
				Join(proj_root, "src"),
				Join(proj_root, "src/config"),
			]

			# Nordic SDK
			self.includes += [
				# nRF5 SDK
					sdk_nrf,
					Join(sdk_nrf, "config/nrf52840/config"),
					# Components
						Join(sdk_nrf, "components/boards"),
						Join(sdk_nrf, "components/ble"),
						Join(sdk_nrf, "components/ble/common"),
						Join(sdk_nrf, "components/ble/nrf_ble_scan"),
						Join(sdk_nrf, "components/libraries/atomic"),
						Join(sdk_nrf, 'components/libraries/atomic_fifo'),
						Join(sdk_nrf, "components/libraries/balloc"),
						Join(sdk_nrf, "components/libraries/bsp"),
						Join(sdk_nrf, "components/libraries/button"),
						Join(sdk_nrf, "components/libraries/cli"),
						Join(sdk_nrf, "components/libraries/cli/cdc_acm"),
						Join(sdk_nrf, "components/libraries/cli/rtt"),
						Join(sdk_nrf, "components/libraries/crc32"),
						Join(sdk_nrf, "components/libraries/delay"),
						Join(sdk_nrf, "components/libraries/experimental_section_vars"),
						Join(sdk_nrf, "components/libraries/fstorage"),
						Join(sdk_nrf, "components/libraries/log"),
						Join(sdk_nrf, "components/libraries/log/src"),
						Join(sdk_nrf, "components/libraries/memobj"),
						Join(sdk_nrf, "components/libraries/mutex"),
						Join(sdk_nrf, "components/libraries/pwr_mgmt"),
						Join(sdk_nrf, "components/libraries/queue"),
						Join(sdk_nrf, "components/libraries/ringbuf"),
						Join(sdk_nrf, "components/libraries/scheduler"),
						Join(sdk_nrf, "components/libraries/sortlist"),
						Join(sdk_nrf, "components/libraries/strerror"),
						Join(sdk_nrf, "components/libraries/timer"),
						Join(sdk_nrf, "components/libraries/usbd"),
						Join(sdk_nrf, "components/libraries/usbd/class/cdc"),
						Join(sdk_nrf, "components/libraries/usbd/class/cdc/acm"),
						Join(sdk_nrf, "components/libraries/util"),
						Join(sdk_nrf, "components/softdevice/common"),
						Join(sdk_nrf, "components/softdevice/s140/headers"),
						Join(sdk_nrf, "components/softdevice/s140/headers/nrf52"),
						Join(sdk_nrf, "components/toolchain/cmsis/include"),
						Join(sdk_nrf, "components/toolchain/cmsis/dsp/GCC"),
						Join(sdk_nrf, "components/toolchain/gcc"),
					# External
						Join(sdk_nrf, "external/fnmatch"),
						Join(sdk_nrf, "external/fprintf"),
						Join(sdk_nrf, "external/segger_rtt"),
						Join(sdk_nrf, 'external/utf_converter'),
					# Drivers
						Join(sdk_nrf, "modules/nrfx"),
						Join(sdk_nrf, "modules/nrfx/drivers"),
						Join(sdk_nrf, "modules/nrfx/drivers/include"),
						Join(sdk_nrf, "modules/nrfx/hal"),
						Join(sdk_nrf, "modules/nrfx/mdk"),
						Join(sdk_nrf, "integration/nrfx"),
						Join(sdk_nrf, "integration/nrfx/legacy"),
			]

			# System include paths
			self.sys_includes += [
				Join(sdk_arm, 'lib/gcc/arm-none-eabi/13.2.1/include'),
				Join(sdk_arm, 'arm-none-eabi/include'),
			]

		# Libraries
		if True:
			# Libraries
			self.libraries += [
			]
			if self.config == 'Debug':
				self.libraries += []
			if self.config == 'Release':
				self.libraries += []

			# System libraries
			arm_lib = Join(sdk_arm, 'lib/gcc/arm-none-eabi/13.2.1/thumb/v7e-m+fp/hard')
			self.sys_libraries += [
				Join(arm_lib, 'crt0.o'),
				Join(arm_lib, 'libc.a'),
				Join(arm_lib, 'libg.a'),
				Join(arm_lib, 'libm.a'),
				#Join(sdk_segger, 'lib/libdebugio_mempoll_v7em_fpv4_sp_d16_hard_t_le_eabi.a'),
				#Join(sdk_segger, 'lib/libvfprintf_v7em_fpv4_sp_d16_hard_t_le_eabi.o'),
				#Join(sdk_segger, 'lib/libvfscanf_v7em_fpv4_sp_d16_hard_t_le_eabi.o'),
				#Join(sdk_segger, 'lib/libdebugio_v7em_fpv4_sp_d16_hard_t_le_eabi.a'),
				#Join(sdk_segger, 'lib/libcpp_v7em_fpv4_sp_d16_hard_t_le_eabi.a'),
				#Join(sdk_segger, 'lib/libc_v7em_fpv4_sp_d16_hard_t_le_eabi.a'),
				#Join(sdk_segger, 'lib/libm_v7em_fpv4_sp_d16_hard_t_le_eabi.a'),
				#Join(sdk_segger, 'lib/libcxxabi_v7em_fpv4_sp_d16_hard_t_le_eabi.a'),
			]

		# Defines
		if True:
			# Application defines
			self.defines += [
				'__SIZEOF_WCHAR_T=4',
				'__ARM_ARCH_7EM__',
				'__SES_ARM',
				'__ARM_ARCH_FPV4_SP_D16__',
				'__HEAP_SIZE__=8192',
				'__SES_VERSION=45203',
				'__GNU_LINKER',

				'NRF52840',
				'NRF52840_XXAA',
				'NRF52_SERIES',
				f'BOARD_{self.target}',
				'INITIALIZE_USER_SECTIONS',
				'FLOAT_ABI_HARD',
				'ENABLE_FEM',

				'NO_VTOR_CONFIG',
				'USE_APP_CONFIG',
				'CONFIG_APP_IN_CORE',
				'CONFIG_GPIO_AS_PINRESET',
				'NRF_SD_BLE_API_VERSION=7',
				#'CLI_OVER_USB_CDC_ACM=0',
			]
			if self.softdevice:
				self.defines += ['SOFTDEVICE_PRESENT','S140']
			if self.mbr_present:
				self.defines += ['MBR_PRESENT']
			if self.config == 'Debug':
				self.defines += ['DEBUG', 'DEBUG_NRF']
			if self.config == 'Release':
				self.defines += ['NDEBUG']

		# Flags
		if True:
			# Flags passed to 'cc' and 'as' for both .s and .c/.cpp files
			self.flags = [
				'-mcpu=cortex-m4',
				'-mlittle-endian',
				'-mfloat-abi=hard',
				'-mfpu=fpv4-sp-d16',
				'-mthumb',
			]

			# Flags for compiling .c/.cpp files
			ccflags = [
				'-mtp=soft',
				'-munaligned-access',
				'-fomit-frame-pointer',
				'-fno-dwarf2-cfi-asm',
				'-fno-builtin',
				'-ffunction-sections',
				'-fdata-sections',
				'-fshort-enums',
				'-fno-common',
				'-fdiagnostics-show-option',
				'-fdiagnostics-show-labels',
				'-fdiagnostics-show-caret',
				'-fmessage-length=0',
				'-nostdinc',
				#'-quiet',
				#'-H', # show includes
			]
			if self.config == 'Debug': ccflags += [
				'-O0', # No optimisation
				'-g3', # Debug symbols
				'-gpubnames',
			]
			if self.config == 'Release': ccflags += [
				'-Os', # Optimise for space
				#'-O3', # Level3 optimisation
			]
			self.cflags = ccflags + [
				'-std=c11',
			]
			self.cxxflags = ccflags + [
				'-std=c++17',
			]
			
			# Flags for compiling .s files with 'cc'
			self.sflags = [
				'-fmessage-length=0',
				'-fno-diagnostics-show-caret',
				'-E',
				'-nostdinc',
				'-lang-asm',
				#'-quiet',
			]
			if self.config == 'Debug': self.sflags += [
			]
			if self.config == 'Release': self.sflags += [
			]

			# Linker flags
			self.lflags = [
				'-X',
				'--omagic',
				'-eReset_Handler',
				#'--defsym=__vfprintf=__vfprintf_short_float_long',
				#'--defsym=__vfprintf=__vfprintf_long',
				#'--defsym=__vfscanf=__vfscanf_long',
				'-EL',
				'--gc-sections',
				'-u_vectors',
				'--emit-relocs',
				'--fatal-warnings',
			]

			# Warning flags
			self.warnings = [
				'-Wall',
				'-Wextra',
				#'-Wno-packed-bitfield-compat',
				'-Wno-expansion-to-defined',
				'-Wno-unused-parameter',
			]
			self.sdk_warnings = [
				'-w',
			]

		# Linker Script
		if True:
			self.flash_placement = Join(proj_root, 'flash_placement.xml')
			self.linker_script_symbols = [
				"__STACKSIZE__=8192",
				"__STACKSIZE_PROCESS__=0",
				"__HEAPSIZE__=8192",
			]
			self.placements_segments = [
				"FLASH RX 0x0 0x100000",
				"RAM RWX 0x20000000 0x40000",
			]
			self.placement_macros = [
				"FLASH_PH_START=0x0",
				"FLASH_PH_SIZE=0x100000",
				"RAM_PH_START=0x20000000",
				"RAM_PH_SIZE=0x40000",
			]
			if self.softdevice:
				self.placement_macros += [
					"FLASH_START=0x27000",
					"FLASH_SIZE=0xd9000",
					"RAM_START=0x20002e00", #"RAM_START=0x200018d8",
					"RAM_SIZE=0x3d200",     #"RAM_SIZE=0x3e728",
				]
			elif self.mbr_present:
				self.placement_macros += [
					"FLASH_START=0x1000",
					"FLASH_SIZE=0xdf000",
					"RAM_START=0x20000008",
					"RAM_SIZE=0x3fff8",
				]
			else:
				self.placement_macros += [
					"FLASH_START=0x0",
					"FLASH_SIZE=0x100000",
					"RAM_START=0x20000000",
					"RAM_SIZE=0x40000",
				]

		# Fix intellisense in vscode
		self.UpdateVSCode()

	# Update VSCode meta files based on includes/defines
	def UpdateVSCode(self):
		c_cpp_properties = os.path.join(proj_root, '.vscode', 'c_cpp_properties.json')
		if os.path.exists(c_cpp_properties):
			with open(c_cpp_properties, "r") as props_json:
				props = json.load(props_json)
			for cfg in props['configurations']:
				if cfg['name'] != 'nRF52840': continue
				cfg['includePath'] = [f'${{workspaceFolder}}/{os.path.relpath(i, proj_root).replace(os.sep,"/")}' for i in self.includes]
				cfg['defines'] = [d for d in self.defines]
			with open(c_cpp_properties, "w") as props_json:
				json.dump(props, props_json, indent=4)
		return

	# Build finished
	def BuildComplete(self):

		# Convert the .elf to a .hex
		args = []
		args += [Join(self.outdir, f'{self.target_name}.elf', check_exists=False)]
		args += [Join(self.outdir, f'{self.target_name}.hex', check_exists=False)]
		args += ['-Oihex']
		success = self.ObjCopy(args)
		if not success:
			raise RuntimeError("Converting .elf to .hex failed")
		
		# Done
		Builder.BuildComplete(self)

# EntryPoint
if __name__ == '__main__':
	builder = Project()
	builder.ignore_project_file_changes = True
	# builder.FindSymbol('vfprintf', [
	# 	Join(sdk_arm, 'lib/gcc/arm-none-eabi/13.2.1/thumb/v7e-m+fp/hard'),
	# 	Join(sdk_nrf, ''),
	# ])
	builder.Build(sys.argv)
	
	