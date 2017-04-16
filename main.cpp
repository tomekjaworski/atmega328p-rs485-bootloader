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
#include "comm.h"
#include "boot.h"
#include "uart.h"

#define BOOTLOADER_HARDWARE_ADDRESS	(uint8_t)0x51



void ___boot(void) __attribute__ ((section (".BL")));

void ___boot(void)
{
	uint8_t arr[] = {'A','B','C'};
	asm volatile("nop\n");
	asm volatile("nop\n");
	asm volatile("nop\n");
	RS485_DIR_SEND;

	while(1)
		for (uint8_t i = 0; i < 3; i++) {
			UCSR0A |= _BV(TXC0);
			UDR0 = arr[i];
			while (!(UCSR0A & _BV(TXC0)));
		}
	asm volatile("nop\n");
	asm volatile("nop\n");
	asm volatile("nop\n");
}


static_assert(sizeof(const void *) == sizeof(uint16_t), "Pointer type different then 2; update the protocol!");
static_assert(sizeof(MessageType) == sizeof(uint8_t), "sizeof(MessageType) == sizeof(uint8_t)");

int main(void)
{
	uartInitialize();
	bootInitialize();

	uint16_t wait_counter = 0;
	while(1) {

		// wait for advert
		int adv = uartReceiveNoBlock();
		if (adv == 'A' )
			break; // advert received - enter bootloader mode!!

		_delay_ms(1);
		if (wait_counter++ > 2000) // wait 2 secs
			asm("jmp 0000");
	}

	
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
			eeprom_update_block(rx.data + sizeof(uint16_t), *(uint8_t**)rx.data, SPM_PAGESIZE);
			eeprom_busy_wait();
			send_response(msg_type, addr, NULL, 0);
		}

	}

	
}

