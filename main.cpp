/*
 * atmega328p_bootloader.cpp
 *
 * Created: 3/28/2017 22:10:49
 * Author : Tomek
 */ 

#include <avr/io.h>
#include <util/crc16.h>
#include "boot.h"
#include "uart.h"


//const char *block = "AABBCCDDEEFFGGHHIIJJKKLLMMNNOOPPPPOOIIAABBCCDDEEFFGGHHIIJJKKLLMMNNOOPPPPOOIIAABBCCDDEEFFGGHHIIJJKKLLMMNNOOPPPPOOIIAABBCCDDEEFFGGHHIIJJKKLLMMNNOOPPPPOOIIAABBCCDDEEFFGGHHIIJJKKLLMMNNOOPPPPOOIIAABBCCDDEEFFGGHHIIJJKKLLMMNNOOPPPPOOIIAABBCCDDEEFFGGHHIIJJKKLLMMNN"; // 78a0
  const char *block = "AABBCCDDEEFFGGHHIIJJKKLLMMNNOOPPPPOOIIAABBCCDDEEFFGGHHIIJJKKLLMMNNOOPPPPOOIIAABBCCDDEEFFGGHHIIJJKKLLMMNNOOPPPPOOIIAABBCCDDEEFFGGHHIIJJKKLLMMNNOOPPPPOOIIAABBCCDDEEFFGGHHIIJJKKLLMMNNOOPPPPOOIIAABBCCDDEEFFGGHHIIJJKKLLMMNNOOPPPPOOIIAABBCCDDEEFFGGHHIIJJKKLLMMNA@DE@\x49\x40\x4a\x4d"; // 78a0
const char* p = block;

#define LED_ON do { PORTB |= _BV(PORTB5); } while (0);
#define LED_OFF do { PORTB &= ~_BV(PORTB5); } while (0);
#define LED_TOGGLE do { PORTB ^= _BV(PORTB5); } while (0);

#define LED0_OFF do { PORTB |= _BV(PORTB2); } while (0);
#define LED0_ON do { PORTB &= ~_BV(PORTB2); } while (0);
#define LED0_TOGGLE do { PORTB ^= _BV(PORTB2); } while (0);

#define LED1_OFF do { PORTB |= _BV(PORTB1); } while (0);
#define LED1_ON do { PORTB &= ~_BV(PORTB1); } while (0);
#define LED1_TOGGLE do { PORTB ^= _BV(PORTB1); } while (0);

#define RS485_DIR_SEND		do { PORTD |= _BV(PORTD2); } while(0); //1
#define RS485_DIR_RECEIVE	do { PORTD &= ~_BV(PORTD2); } while(0);//0


bool receive_byte(uint8_t& ch)
{
	ch = *p++;
	return true;
}

uint16_t crc16_update(uint16_t crc, uint8_t data)
{
	return _crc16_update(crc, data);
}

union PAGE_DATA {
	struct {
		uint16_t address; // address of received page
		uint8_t payload[SPM_PAGESIZE]; // contente of the page
		uint16_t crc; // its checksum (data+address)
	};
	uint8_t data[sizeof(uint16_t) + SPM_PAGESIZE + sizeof(uint16_t)];
} page = {};


bool receive_page(void)
{
	uint16_t crc = 0xffff;
	uint8_t *pdata = (uint8_t *)&page;

	int cnt = sizeof(uint16_t) + SPM_PAGESIZE + sizeof(uint16_t);
	for (; cnt > 0; cnt--)
	{
		uint8_t high, low;

		if (!receive_byte(high))
			return false; 
		if (!receive_byte(low))
			return false;

		if (high < '@' || high > 'P')
			return false;
		if (low < '@' || low > 'P')
			return false;

		uint8_t b = (uint8_t)(high - '@') << 4 | (uint8_t)(low - '@');
		*pdata++ = b;

		if (cnt > sizeof(uint16_t))
		{
			crc = crc16_update(crc, high);
			crc = crc16_update(crc, low);
		}
	}

	// check CRC 16
	return crc == page.crc;
}
  /*
  int
  checkcrc(void)
  {
	  uint16_t crc = 0xffff, i;

	  for (i = 0; i < sizeof serno; i++)
	  crc = crc16_update(crc, serno[i]);

	  return crc; // must be 0
  }*/

int main(void)
{
	uartInitialize();
		
	//bootInitialize();

		while(1){
		//LED_TOGGLE;
		//for (int i = 0; i < 20000; i++)
		//	asm volatile ("nop");

			uartSend('A');
			uartSend('B');
			uartSend('C');
			uartSend('D');
			uartSend('E');
			uartSend('F');
			uartSend('G');
			uartSend('H');
			{

			
		for (int i = 0; i < 20000; i++)
			asm volatile ("nop");
			}
		}




	uint32_t page = 0;
	receive_page();

	bootRestart();

	while(1)
		asm("nop");
	
}

