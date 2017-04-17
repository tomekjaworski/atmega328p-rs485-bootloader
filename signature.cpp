/*
 * signature.cpp
 *
 * Created: 4/17/2017 13:30:15
 *  Author: Tomasz Jaworski
 */ 

//#include <avr/signature.h>
#include <avr/io.h>
#include <stddef.h>
#include "config.h"

 const unsigned char __signature[3] __attribute__((__used__, __section__(".signature"))) =
	{ 'T', 'J', BOOTLOADER_HARDWARE_ADDRESS };