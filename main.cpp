/*
 * atmega328p_bootloader.cpp
 *
 * Created: 3/28/2017 22:10:49
 * Author : Tomek
 */ 

#include <avr/io.h>
#include <util/crc16.h>
#include <util/delay.h>
#include <avr/pgmspace.h>
#include "boot.h"
#include "uart.h"

#define BOOTLOADER_HARDWARE_ADDRESS	(uint8_t)0x51

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

void ___boot(void) __attribute__ ((section (".BL")));
void ___boot(void)
{
	asm volatile("nop\n");
	asm volatile("nop\n");
	asm volatile("nop\n");
	asm volatile(".string \"Ala ma kota\"");
	asm volatile("nop\n");
	asm volatile("nop\n");
	asm volatile("nop\n");
}

union PAGE_DATA {
	struct {
		uint16_t address; // address of received page
		uint8_t payload[SPM_PAGESIZE]; // contente of the page
		uint16_t checksum; // its checksum (data+address)
	};
	uint8_t data[sizeof(uint16_t) + SPM_PAGESIZE + sizeof(uint16_t)];
} page = {};

		#define MAX_PAYLOAD_SIZE	(128+4)

struct RX {
	uint8_t data[MAX_PAYLOAD_SIZE];
	uint8_t *ptr, *endptr;
} rx;


uint8_t receive_page(void)
{
	uint16_t checksum = 0;
	uint8_t *pdata = (uint8_t *)&page;

	int cnt = sizeof(uint16_t) + SPM_PAGESIZE + sizeof(uint16_t);
	for (; cnt > 0; cnt--)
	{
		uint8_t data = uartReceive();
		*pdata++ = data;

		if (cnt > sizeof(uint16_t))
			checksum -= data;
	}

	// check CRC
	if (checksum != page.checksum)
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
	txt("Start\r\n");
	while(1) {

		// wait for advert
		int adv = uartReceiveNoBlock();
		if (adv == 'A' ) // advert received - activate bootloader mode!!
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

	#define BL_COMMAND_ACTIVATE	'A'
	#define BL_COMMAND_DEACTIVATE 'B'
	#define BL_COMMAND_PING '?'
	#define BL_COMMAND_REBOOT 'R'

	#define BL_COMMAND_READ_PAGE 'X'
	#define BL_COMMAND_WRITE_PAGE 'W'
	
	// 00 - uint8_t: address
	// 01 - uint8_t: command
	bool activated = false;
	while(1)
	{
		// receive address
		uint8_t addr = uartReceive();
		if (addr != BOOTLOADER_HARDWARE_ADDRESS) 
			continue; // its not for me

		// receive command
		uint8_t command = uartReceive();

		// receive payload length
		uint8_t payload_size = uartReceive();

		if (payload_size > MAX_PAYLOAD_SIZE)
			continue; // something is wrong

		uint16_t checksum = (uint16_t)addr + (uint16_t)command + (uint16_t)payload_size;

		// receive data
		rx.ptr = rx.data;
		rx.endptr = rx.data + payload_size;
		while (rx.endptr != rx.ptr) {
			uint8_t data = uartReceive();
			*rx.ptr++ = data;
			checksum += (uint16_t)data;
		}

		// apply checksum
		checksum -= (uint16_t)uartReceive() << 8;
		checksum -= uartReceive();

		if (checksum != 0)
			continue; // error in message
			/*
		if (!activated)
		{
			if (command != BL_COMMAND_ACTIVATE) // command: activate
				continue;
			if (uartReceive() != ~BOOTLOADER_HARDWARE_ADDRESS) // magic 1
				continue;
			if (uartReceive() != (BOOTLOADER_HARDWARE_ADDRESS & 0x0F >> 4 | BOOTLOADER_HARDWARE_ADDRESS & 0xF0 << 4)) // magic 2
				continue;
			if (uartReceive() != ~(BOOTLOADER_HARDWARE_ADDRESS & 0x0F >> 4 | BOOTLOADER_HARDWARE_ADDRESS & 0xF0 << 4)) // magic 3
				continue;
			if (uartReceive() != 84) // magic 4
				continue;
			if (uartReceive() != 74) // magic 5
				continue;

			// ok, bootloader activated
			activated = true;
		}

		if (command == BL_COMMAND_DEACTIVATE) // deactivate bootloader
		{
			activated = false;
		}
*/
		if (command == BL_COMMAND_PING) // got challenge, send response -- identify itself
		{
			RS485_DIR_SEND;
			uartSend(BOOTLOADER_HARDWARE_ADDRESS);
			uartSend(BL_COMMAND_PING);
			RS485_DIR_RECEIVE;
		}

		if (command == BL_COMMAND_REBOOT) // restart whole device
			bootRestart();

		if (command == BL_COMMAND_READ_PAGE) // 
		{
			memcpy_P(rx.data, *(uint8_t**)rx.data, SPM_PAGESIZE);
			RS485_DIR_SEND;
			uartSend(BOOTLOADER_HARDWARE_ADDRESS);
			uartSend(BL_COMMAND_READ_PAGE);
			for (rx.ptr = rx.data, rx.endptr = rx.data + SPM_PAGESIZE; rx.endptr != rx.ptr; rx.ptr++)
				uartSend(*rx.ptr);
			RS485_DIR_RECEIVE;
		}

		if (command == BL_COMMAND_WRITE_PAGE)
		{
			bootStorePage(*(uint32_t*)rx.data, rx.data + sizeof(uint32_t));
			RS485_DIR_SEND;
			uartSend(BOOTLOADER_HARDWARE_ADDRESS);
			uartSend(BL_COMMAND_WRITE_PAGE);
			RS485_DIR_RECEIVE;
		}

	}

	
}

