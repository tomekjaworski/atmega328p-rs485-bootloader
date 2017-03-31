/*
 * atmega328p_bootloader.cpp
 *
 * Created: 3/28/2017 22:10:49
 * Author : Tomek
 */ 

#include <avr/io.h>
#include <avr/fuse.h>
#include <avr/boot.h>

FUSES =
{
	.low = (FUSE_CKSEL0 & FUSE_CKSEL2 & FUSE_CKSEL3 & FUSE_SUT0 & FUSE_CKDIV8),
	.high = (FUSE_BOOTSZ0 & FUSE_BOOTSZ1 & FUSE_SPIEN & FUSE_BOOTRST),
	.extended = EFUSE_DEFAULT,
};

#define SERIAL_BAUD 9600	// 8E1 (!!!)
#define SERIAL_UX2

//////////////////////////////////////////////////////////////////////////

#if defined(SERIAL_UX2)
// UX2 = 1
#define UBR0_VALUE (F_CPU/(8UL*SERIAL_BAUD))-1
#else
// UX2 = 0
#define UBR0_VALUE (F_CPU/(16UL*SERIAL_BAUD))-1
#endif


void uartInitialize(void)
{
	// init RX485 line driver interface
	DDRD = 0b00000110; // 0:RX, 1:TX, 3:DIR

	// init serial port
	uint16_t br = UBR0_VALUE;
	UBRR0H = (uint8_t)(br >> 8);
	UBRR0L = (uint8_t)br;
		
	#if defined(BAUD_UX2)
	UCSR0A = _BV(U2X0);
	#else
	UCSR0A = 0x00;
	#endif
		
	UCSR0C = _BV(UCSZ01) | _BV(UCSZ00) | _BV(UPM01);	// 8E1
	UCSR0B = _BV(RXEN0) | _BV(TXEN0);	// trun on RX and TX part of the serial controller
	//UCSR0B |= (1 << RXCIE0);
}

inline uint8_t uartReceive()
{
	while (!(UCSR0A & _BV(RXC0)));
	return UDR0;
}


inline uint8_t uartSend(uint8_t data)
{
    UDR0 = data;
	while (!(UCSR0A & _BV(TXC0)));
}

int main(void)
{
	
}

