#ifndef USBD_CONFIG_H
#define USBD_CONFIG_H

// VID/PID
// Note: We cannot use the USB logo without registering our own VID at https://www.usb.org/getting-vendor-id
#define APP_USBD_VID 0x1915 // Nordic's registered VID
#define APP_USBD_PID 0x5942 // A made up PID for Rex

// Supported languages identifiers. Comma separated list of supported languages. 
#define APP_USBD_STRINGS_LANGIDS ((uint16_t)APP_USBD_LANG_ENGLISH | (uint16_t)APP_USBD_SUBLANG_ENGLISH_US)

// Define manufacturer string ID.
// Comma separated list of manufacturer names for each defined language.
// The order of manufacturer names has to be the same like in @ref APP_USBD_STRINGS_LANGIDS.
#define APP_USBD_STRING_ID_MANUFACTURER 1
#define APP_USBD_STRINGS_MANUFACTURER APP_USBD_STRING_DESC("Rex Bionics")
#define APP_USBD_STRINGS_MANUFACTURER_EXTERN 0

// Product name string descriptor
// List of product names defined the same way like in @ref APP_USBD_STRINGS_MANUFACTURER
#define APP_USBD_STRING_ID_PRODUCT 2
#define APP_USBD_STRINGS_PRODUCT APP_USBD_STRING_DESC("RexNode Dongle")
#define APP_USBD_STRINGS_PRODUCT_EXTERN 0

// Serial number string descriptor
// Create serial number string descriptor using @ref APP_USBD_STRING_DESC,
// or configure it to point to any internal variable pointer filled with descriptor.
// note: There is only one SERIAL number inside the library and it is Language independent.
#define APP_USBD_STRING_ID_SERIAL 3
#define APP_USBD_STRING_SERIAL g_extern_serial_number
#define APP_USBD_STRING_SERIAL_EXTERN 1

// User strings default values
// This value stores all application specific user strings with its default initialization.
// The setup is done by X-macros.
// Expected macro parameters:
// @code
// X(mnemonic, [=str_idx], ...)
// @endcode
// - @c mnemonic: Mnemonic of the string descriptor that would be added to
//                @ref app_usbd_string_desc_idx_t enumerator.
// - @c str_idx : String index value, may be set or left empty.
//                For example WinUSB driver requires descriptor to be present on 0xEE index.
//                Then use X(USBD_STRING_WINUSB, =0xEE, (APP_USBD_STRING_DESC(...)))
// - @c ...     : List of string descriptors for each defined language.
//
// Setup this dongle as a 'WCID' (Windows Compatible ID) by setting a descriptor
// at index '0xEE'. See 'https://github.com/pbatard/libwdi/wiki/WCID-Devices' for a good description.
#if 1
#define APP_USBD_STRINGS_USER\
	X(USBD_STRING_WINUSB, =0xEE,\
	{\
		0x12, /* length (18 bytes) */\
		0x3, /* Descriptor type */\
		'0','M',\
		'0','S',\
		'0','F',\
		'0','T',\
		'0','1',\
		'0','0',\
		'0','0', /* Signature */\
		0x01, 0xff, /* Vendor code index */\
	})
#endif

#endif
