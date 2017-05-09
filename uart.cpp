/*
 * uart.cpp
 *
 * Created: 4/1/2017 13:33:23
 *  Author: Tomasz Jaworski
 */ 

 #include <avr/io.h>
 #include "comm.h"
 #include "uart.h"
 #include "config.h"


 #define SERIAL_UX2

 //////////////////////////////////////////////////////////////////////////

 #if defined(SERIAL_UX2)
 // UX2 = 1
 #define UBR0_VALUE (F_CPU/(8UL*SERIAL_BAUD))-1
 #else
 // UX2 = 0
 #define UBR0_VALUE (F_CPU/(16UL*SERIAL_BAUD))-1
 #endif

#define RS485_DIR_RECEIVE	do { PORTD &= ~_BV(PORTD2); } while(0);//0
#define RS485_DIR_SEND		do { PORTD |= _BV(PORTD2); } while(0); //1

RX rx;


uint8_t uartReceive()
{
	rx.timeout = false;
	uint16_t counter = 0;
	while (!(UCSR0A & _BV(RXC0)))
		if (counter++ > 2000)
		{
			rx.timeout = true;
			return 0;
		}
	return UDR0;
}

void uartInitialize(void)
{
	// init RX485 line driver interface
	DDRD = 0b00000110; // 0:RX, 1:TX, 3:DIR
	DDRB |= _BV(PORTB5) | _BV(PORTB2) | _BV(PORTB1);



	// init serial port
	uint16_t br = UBR0_VALUE;
	UBRR0H = (uint8_t)(br >> 8);
	UBRR0L = (uint8_t)br;
	 
	#if defined(SERIAL_UX2)
	UCSR0A = _BV(U2X0);
	#else
	UCSR0A = 0x00;
	#endif
	 
	UCSR0C = _BV(UCSZ01) | _BV(UCSZ00) | _BV(UPM01);	// 8E1
	UCSR0B = _BV(RXEN0) | _BV(TXEN0);	// trun on RX and TX part of the serial controller
	//UCSR0B |= (1 << RXCIE0);

	RS485_DIR_RECEIVE;

 }

 void send_response(MessageType msg_type, uint8_t addr, const uint8_t* buffer, uint8_t count)
 {
	uint16_t checksum = addr + (uint16_t)msg_type + count;

	RS485_DIR_SEND;

	// send header
	uartSend(addr); // protocol: address
	uartSend((uint8_t)msg_type); // protocol: command
	uartSend(count); // protocol: payload size

	// send payload
	while (count-- > 0) {
		checksum += *buffer;
		uartSend(*buffer);
		buffer++;
	}

	// send checksum
	uartSend(checksum >> 8); // protocol: checksum's msb
	uartSend(checksum & 0x00FF); // protocol: checksum's lsb

	RS485_DIR_RECEIVE;
 }