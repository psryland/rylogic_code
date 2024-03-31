// Copyright (c) Rex Bionics 2020
#ifndef BLE_H
#define BLE_H

#include "forward.h"

// Set up the BLE Stack
ret_code_t BLE_Init();

// Read received sensor data
bool BLE_ReceivedDataGet(rex_node_sensor_packet_t* data_out);

#endif
