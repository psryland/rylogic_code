# ESP32

## ESP32 S3 dev board

- There is a 'RGB' solder bridge you need to connect on the board to enable the RGB LED
- The built-in RGB LED is actually on pin 47, not 48. Don't use the RGB_BUILTIN macro
- The JTAG port on pins 39-42 is not physically connected. To enable it you need to burn an "E-Fuse" which permenantly disables the USB JTAG
- ESP32 support was added to OpenOCD around 2019
- You need to manually install the "E:\ESP\tools\idf-driver\idf-driver-esp32-usb-jtag-2021-07-15\USB_JTAG_debug_unit.inf" driver. The install process doesn't seem to work. When you do, you can connect with openocd, using the config:
  ```
  source [find interface/esp_usb_jtag.cfg]
  transport select "jtag"
  adapter speed 40000
  source [find target/esp32s3.cfg]
  ```
- You can run openocd using:
  ```
  openocd -c "set ESP_RTOS none" -f board/esp32s3-builtin.cfg
  ```
  Apparently, the `-c "set ESP_RTOS none"` part is needed to handle FreeRTOS tasks properly...

- Plug the USB cable into the 'USB' port, not the 'COM' port for JTAG debugging


# FreeRTOS

## Naming Conventions in FreeRTOS

FreeRTOS uses a naming convention to indicate the type and purpose of functions and variables:

- x Prefix: Functions that return a status or handle (e.g., xTaskCreate, xQueueReceive).
- v Prefix: Functions that return void (no return value) (e.g., vTaskDelete, vTaskDelay).
- pd Prefix: Defines or macros that relate to periods or durations (e.g., pdMS_TO_TICKS).
- pv Prefix: Generic pointers (void pointers) (e.g., pvPortMalloc).
- ul Prefix: Unsigned long data types.
- uc Prefix: Unsigned char data types.
- prv Prefix: Functions that are private to the file in which they are declared.

## Tasks

### Tasks as Threads:

- In FreeRTOS, tasks are similar to threads in other operating systems. Each task is an independent path of execution and has its own stack and context. Tasks can run concurrently, and the FreeRTOS scheduler decides which task runs at any given time.
- Task Creation: Tasks are created using the xTaskCreate or xTaskCreateStatic function. Each task is defined by a task function and runs independently.

### Task States:

- Running: The task is currently executing.
- Ready: The task is ready to run, waiting for the CPU.
- Blocked: The task is waiting for an event (e.g., delay, semaphore, or queue).
- Suspended: The task is suspended and not available for scheduling.
- Deleted: The task has been deleted and is waiting for its resources to be reclaimed.

### Task Priority:

- Tasks can have different priorities. The scheduler always chooses the highest-priority task that is ready to run. Tasks with the same priority use time-slicing (round-robin scheduling).

### Blocking in Tasks

- Blocking: A task can block itself by calling functions that place it in the blocked state, such as vTaskDelay, xQueueReceive, or xSemaphoreTake. When a task blocks, it does not consume any CPU time until the event it is waiting for occurs.

- Impact of Blocking: Blocking allows other tasks to run, making efficient use of CPU time. Properly using blocking mechanisms is essential for responsive and efficient multitasking.