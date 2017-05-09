/*
 * uart.h
 *
 * Created: 4/1/2017 13:34:02
 *  Author: Tomasz Jaworski
 */ 

#ifndef UART_H_
#define UART_H_

#include "comm.h"

#define RS485_DIR_SEND		do { PORTD |= _BV(PORTD2); } while(0); //1
#define RS485_DIR_RECEIVE	do { PORTD &= ~_BV(PORTD2); } while(0);//0

void uartInitialize(void);
void send_response(MessageType msg_type, uint8_t addr, const uint8_t* buffer, uint8_t count);



uint8_t uartReceive();


inline int uartReceiveNoBlock()
{
	if (!(UCSR0A & _BV(RXC0))) // no data
		return -1;

	return UDR0;
}

inline void uartSend(uint8_t data)
{
	UCSR0A |= _BV(TXC0);
	UDR0 = data;
	while (!(UCSR0A & _BV(TXC0)));
}


#endif /* UART_H_ */