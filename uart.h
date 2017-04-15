/*
 * uart.h
 *
 * Created: 4/1/2017 13:34:02
 *  Author: Tomek
 */ 


#ifndef UART_H_
#define UART_H_


void uartInitialize(void);



inline uint8_t uartReceive()
{
	while (!(UCSR0A & _BV(RXC0)));
	return UDR0;
}

inline int uartReceiveNoBlock()
{
	if (!(UCSR0A & _BV(RXC0))) // no data
		return -1;

	return UDR0;
}

inline bool uartReceiveNoBlock(uint8_t& data)
{
	if (!(UCSR0A & _BV(RXC0))) // no data
		return false;

	data = UDR0;
	return true;
}

inline void uartSend(uint8_t data)
{
	UCSR0A |= _BV(TXC0);
	UDR0 = data;
	while (!(UCSR0A & _BV(TXC0)));
}


#endif /* UART_H_ */