/*
 * demo_boot.cpp
 *
 * Created: 4/17/2017 11:56:40
 *  Author: Tomasz Jaworski
 */ 

#include <avr/io.h>
#include <util/delay.h>
#include <stddef.h>
#include "uart.h"

//
// For Arduino Pro Mini 328 the LED_TOGGLE toggles on-board LED diode.
// If this bootloader is used on other boards, change this line or remove it completely
#define LED_TOGGLE do { PORTB ^= _BV(PORTB5); } while (0);


void ___boot_demo(void) __attribute__ ((__used__, section (".BL")));
void ___boot_demo(void)
{
	uint8_t arr[3];
	arr[0] = 'A'; arr[1] = 'B'; arr[2] = 'C';
	asm volatile("nop\n");
	asm volatile("nop\n");
	asm volatile("nop\n");
	RS485_DIR_SEND;

	while(1)
	for (uint8_t i = 0; i < 3; i++) {

		// send a byte
		UCSR0A |= _BV(TXC0);
		UDR0 = arr[i];
		while (!(UCSR0A & _BV(TXC0)));

		// toggle some led
		LED_TOGGLE;

#if defined (DEBUG)
		_delay_ms(100);
#else
		for (uint32_t j = 0; j < 1000000; j++)
			asm volatile("nop");
#endif
	}
	asm volatile("nop\n");
	asm volatile("nop\n");
	asm volatile("nop\n");
}

