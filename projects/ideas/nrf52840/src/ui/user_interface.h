// Copyright (c) Rex Bionics 2020
#ifndef USER_INTERFACE_H
#define USER_INTERFACE_H
#include "forward.h"

// Initialise user interface support (i.e. board specific LEDs/Buttons/etc)
void UserInterface_Init();

// Main loop processing for the UI
void UserInterface_Process();

#endif
