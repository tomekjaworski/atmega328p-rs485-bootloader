/*
 * demo_boot.cpp
 *
 * Created: 4/17/2017 11:56:40
 *  Author: Tomasz Jaworski
 */ 

#include <avr/io.h>
#include <stddef.h>
#include "uart.h"

void ___boot_demo(void) __attribute__ ((unused, section (".BL")));

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
		UCSR0A |= _BV(TXC0);
		UDR0 = arr[i];
		while (!(UCSR0A & _BV(TXC0)));

		for (uint32_t j = 0; j < 1000000; j++)
			asm volatile("nop");
	}
	asm volatile("nop\n");
	asm volatile("nop\n");
	asm volatile("nop\n");
}

