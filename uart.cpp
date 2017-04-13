/*
 * uart.cpp
 *
 * Created: 4/1/2017 13:33:23
 *  Author: Tomek
 */ 

 #include <avr/io.h>

 
 #define SERIAL_BAUD 19200	// 8E1 (!!!)
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


 void uartInitialize(void)
 {
	 // init RX485 line driver interface
	 DDRD = 0b00000110; // 0:RX, 1:TX, 3:DIR

	DDRB |= _BV(PORTB2);
	DDRB |= _BV(PORTB1);
	DDRB |= _BV(PORTB5);
	DDRD = 0b00000110; // 0:RX, 1:TX, 3:DIR			1-wyjscie (1 i 3) ddr - data direction


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
