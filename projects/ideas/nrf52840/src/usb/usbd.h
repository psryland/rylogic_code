#ifndef USBD_H
#define USBD_H

#include <stdint.h>

// Set up the USB-CDC support so that the dongle shows up as a COMM port on the host PC.
// USBD-CDC-ACM = USB - Communications Device Class - Abstract Control Model:
//   The USB Communications Device Class (CDC) allows converting the USB device into a serial
//   communication device. It is an abstract USB class protocol defined by the USB Implementers
//   Forum. This protocol allows devices to provide a virtual COM port to a PC application.

// Initialise the USB CDC module
void USB_Init();

// Pump the USB event queue
void USB_Process();

// Write data to the USB port
uint32_t USB_Write(void const* data, size_t size);

#endif