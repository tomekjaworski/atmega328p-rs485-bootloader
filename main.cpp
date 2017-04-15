/*
 * atmega328p_bootloader.cpp
 *
 * Created: 3/28/2017 22:10:49
 * Author : Tomek
 */ 
#include <avr/io.h>
#include <avr/pgmspace.h>
#include <avr/eeprom.h>
#include <util/crc16.h>
#include <util/delay.h>
#include <stddef.h>
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
	asm volatile(".string \"Lorem ipsum dolor sit amet, consectetur adipiscing elit. Duis vitae ipsum eu augue varius porta sit amet sed tellus. Aliquam erat volutpat. Sed ornare blandit tortor quis congue. Pellentesque et augue ut magna efficitur condimentum. Quisque elementum leo sed justo molestie tempor. Interdum et malesuada fames ac ante ipsum primis in faucibus. Aenean commodo semper justo at laoreet. Etiam sodales nulla neque, id dictum orci scelerisque posuere.\"");
	asm volatile("nop\n");
	asm volatile("nop\n");
	asm volatile("nop\n");
}

#define MAX_PAYLOAD_SIZE	(128+4)

enum class MessageType : uint8_t
{
	Activate = 'A',
	Deactivate = 'B',

	Ping = '?',
	Reboot = 'R',

	ReadFlashPage = 'X',
	WriteFlashPage = 'W',
	ReadEepromPage = 'E',
	WriteEepromPage = 'F'
};

struct RX {
	uint8_t data[MAX_PAYLOAD_SIZE];
	uint8_t *ptr, *endptr;
} rx;

void txt(const char* ptr)
{
	RS485_DIR_SEND;
	while(*ptr)
		uartSend(*ptr++);
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
static_assert(sizeof(MessageType) == sizeof(uint8_t), "sizeof(MessageType) == sizeof(uint8_t)");

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
		if (adv == 'A' )
			break; // advert received - enter bootloader mode!!

		_delay_ms(1);
		if (wait_counter++ > 2000) // wait 2 secs
		{
			while(1) { txt("User program\r\n"); 	_delay_ms(500); }
		}
	}

	
	// 00 - uint8_t: address
	// 01 - uint8_t: command
	while(1)
	{
		// receive address
		uint8_t addr = uartReceive();
		//if (addr != BOOTLOADER_HARDWARE_ADDRESS)
		if (addr != BOOTLOADER_HARDWARE_ADDRESS /*&& addr != 0x40&& addr != 0x50&& addr != 0x52&& addr != 0xF0*/)
			continue; // its not for me

		// receive command
		MessageType msg_type = (MessageType)uartReceive();

		// receive payload length
		uint8_t payload_size = uartReceive();

		if (payload_size > MAX_PAYLOAD_SIZE)
			continue; // something is wrong

		uint16_t checksum = (uint16_t)addr + (uint16_t)msg_type + (uint16_t)payload_size;

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

		if (msg_type == MessageType::Ping) // got challenge, send response -- identify itself
		{
			send_response(msg_type, addr, NULL, 0);
		}

		if (msg_type == MessageType::Reboot) { // restart whole device
			send_response(msg_type, addr, NULL, 0);
			bootRestart();
		}

		if (msg_type == MessageType::ReadFlashPage) {
			memcpy_P(rx.data, *(uint8_t**)rx.data, SPM_PAGESIZE);
			send_response(msg_type, addr, rx.data, SPM_PAGESIZE);
		}

		if (msg_type == MessageType::WriteFlashPage) {
			bootStorePage(*(uint16_t*)rx.data, rx.data + sizeof(uint16_t));
			send_response(msg_type, addr, NULL, 0);
		}
		
		if (msg_type == MessageType::ReadEepromPage) {
			eeprom_read_block(rx.data, *(uint8_t**)rx.data , SPM_PAGESIZE);
			eeprom_busy_wait();
			send_response(msg_type, addr, rx.data, SPM_PAGESIZE);
		}

		if (msg_type == MessageType::WriteEepromPage) {
			eeprom_write_block(rx.data + sizeof(uint16_t), *(uint8_t**)rx.data, SPM_PAGESIZE);
			eeprom_busy_wait();
			send_response(msg_type, addr, NULL, 0);
		}

	}

	
}

