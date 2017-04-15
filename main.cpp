/*
 * atmega328p_bootloader.cpp
 *
 * Created: 3/28/2017 22:10:49
 * Author : Tomek
 */ 
#include <stddef.h>
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

		if (cnt > (int)sizeof(uint16_t))
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

void send_response(uint8_t command, uint8_t addr, const uint8_t* buffer, uint8_t count)
{
	uint16_t checksum = addr + command + count;

	RS485_DIR_SEND;

	// send header
	uartSend(addr); // protocol: address
	uartSend(command); // protocol: command
	uartSend(count); // protocol: payload size

	// send payload
	while (count-- > 0)
	{
		checksum += *buffer;
		uartSend(*buffer);
		buffer++;
	}

	// send checksum
	uartSend(checksum >> 8); // protocol: checksum's msb
	uartSend(checksum & 0x00FF); // protocol: checksum's lsb

	RS485_DIR_RECEIVE;
}

static_assert(sizeof(const void *) == sizeof(uint16_t), "Pointer type different then 2; update the protocol!");

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
	while(1)
	{
		// receive address
		uint8_t addr = uartReceive();
		//if (addr != BOOTLOADER_HARDWARE_ADDRESS)
		if (addr != BOOTLOADER_HARDWARE_ADDRESS && addr != 0x40&& addr != 0x50&& addr != 0x52&& addr != 0xF0)
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

		//txt("chk");

		if (checksum != 0)
			continue; // error in message

		if (command == BL_COMMAND_PING) // got challenge, send response -- identify itself
		{
			send_response(BL_COMMAND_PING, addr, NULL, 0);
		}

		if (command == BL_COMMAND_REBOOT) { // restart whole device
			send_response(BL_COMMAND_REBOOT, addr, NULL, 0);
			bootRestart();
		}

		if (command == BL_COMMAND_READ_PAGE) {
			memcpy_P(rx.data, *(uint8_t**)rx.data, SPM_PAGESIZE);
			send_response(BL_COMMAND_READ_PAGE, addr, rx.data, SPM_PAGESIZE);
		}

		if (command == BL_COMMAND_WRITE_PAGE) {
			bootStorePage(*(uint32_t*)rx.data, rx.data + sizeof(uint16_t));
			send_response(BL_COMMAND_WRITE_PAGE, addr, NULL, 0);
		}

	}

	
}

