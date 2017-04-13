/*
 * atmega328p_bootloader.cpp
 *
 * Created: 3/28/2017 22:10:49
 * Author : Tomek
 */ 

#include <avr/io.h>
#include <util/crc16.h>
#include <util/delay.h>
#include "boot.h"
#include "uart.h"

#define BOOTLOADER_HARDWARE_ADDRESS	0x51

//const char *block = "AABBCCDDEEFFGGHHIIJJKKLLMMNNOOPPPPOOIIAABBCCDDEEFFGGHHIIJJKKLLMMNNOOPPPPOOIIAABBCCDDEEFFGGHHIIJJKKLLMMNNOOPPPPOOIIAABBCCDDEEFFGGHHIIJJKKLLMMNNOOPPPPOOIIAABBCCDDEEFFGGHHIIJJKKLLMMNNOOPPPPOOIIAABBCCDDEEFFGGHHIIJJKKLLMMNNOOPPPPOOIIAABBCCDDEEFFGGHHIIJJKKLLMMNN"; // 78a0
  //const char *block = "AABBCCDDEEFFGGHHIIJJKKLLMMNNOOPPPPOOIIAABBCCDDEEFFGGHHIIJJKKLLMMNNOOPPPPOOIIAABBCCDDEEFFGGHHIIJJKKLLMMNNOOPPPPOOIIAABBCCDDEEFFGGHHIIJJKKLLMMNNOOPPPPOOIIAABBCCDDEEFFGGHHIIJJKKLLMMNNOOPPPPOOIIAABBCCDDEEFFGGHHIIJJKKLLMMNNOOPPPPOOIIAABBCCDDEEFFGGHHIIJJKKLLMMNA@DE@90AD"; // 78a0
//const char* p = block;

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


uint8_t receive_page(void)
{
	uint16_t crc = 0xffff;
	uint8_t *pdata = (uint8_t *)&page;

	int cnt = sizeof(uint16_t) + SPM_PAGESIZE + sizeof(uint16_t);
	for (; cnt > 0; cnt--)
	{
		uint8_t high = uartReceive();
		uint8_t low = uartReceive();

		if (high < '@' || high > 'P')
			return 0x01; // error - format 
		if (low < '@' || low > 'P')
			return 0x02; // error - format

		uint8_t b = (uint8_t)(high - '@') << 4 | (uint8_t)(low - '@');
		*pdata++ = b;

		if (cnt > sizeof(uint16_t))
		{
			crc = crc16_update(crc, high);
			crc = crc16_update(crc, low);
		}
	}

	// check CRC 16
	if (crc != page.crc)
		return 0x03; // error - CRC

	return 0x00;
}

void txt(const char* ptr)
{
	RS485_DIR_SEND;
	while(*ptr)
		uartSend(*ptr++);
	RS485_DIR_RECEIVE;
}

int main(void)
{
	uartInitialize();
	bootInitialize();

	RS485_DIR_RECEIVE;
	uint16_t wait_counter = 0;
	//txt("Start\r\n");
	while(1) {

		// wait for advert
		int adv = uartReceiveNoBlock();
		if (adv == 'A' ) // advert received
		{
			//txt("Bootloader mode\r\n");
			break;
		}

		_delay_ms(1);
		if (wait_counter++ > 2000) // wait 2 secs
		{
			while(1) { txt("User program\r\n"); 	_delay_ms(500); }
		}
	}

	
	while(1)
	{
		uint8_t cmd = uartReceive();

		if (cmd == 'C') // got challenge, send response
		{
			_delay_ms((int)BOOTLOADER_HARDWARE_ADDRESS << 3); // wait some time
			RS485_DIR_SEND;
			uartSend('c');
			uartSend(BOOTLOADER_HARDWARE_ADDRESS);
			RS485_DIR_RECEIVE;
		}

		if (cmd == 'R') // restart whole device
			bootRestart();

		if (cmd == 'P') { // receive a page
			uint8_t addr = uartReceive();
			uint8_t res = receive_page();

			//if (addr != )
			RS485_DIR_SEND;
			uartSend('p');
			uartSend(0xF0 | res);
			RS485_DIR_RECEIVE;
		}
	}


/*
	uint32_t page = 0;
	receive_page();

	bootRestart();
	*/
	while(1)
		asm("nop");
	
}

