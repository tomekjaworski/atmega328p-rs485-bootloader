/*
 * config.h
 *
 * Created: 4/17/2017 11:26:17
 *  Author: Tomasz Jaworski
 */ 


#ifndef CONFIG_H_
#define CONFIG_H_

//
// serial port baud rate
#define SERIAL_BAUD 19200	// 8E1 (!!!)

//
// bootloader address
#define BOOTLOADER_HARDWARE_ADDRESS	(uint8_t)0x51

//
// advertisement wait time [ms] - time that bootloader spends waiting for C&C software to send the activation byte.
// If the time given in ADVERTISEMENT_WAIT_TIME passes, bootloader jumps into user space code - addr 0x0000
#define ADVERTISEMENT_WAIT_TIME	15000


//
// Bootloader Protocol Version
#define PROTOCOL_VERSION 3

#endif /* CONFIG_H_ */