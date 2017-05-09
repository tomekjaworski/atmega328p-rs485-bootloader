/*
 * atmega328p_bootloader.cpp
 *
 * Created: 3/28/2017 22:10:49
 * Author : Tomasz Jaworski
 */ 
#include <avr/io.h>
#include <avr/boot.h>
#include <avr/pgmspace.h>
#include <avr/eeprom.h>
#include <util/crc16.h>
#include <util/delay.h>
#include <stddef.h>
#include "comm.h"
#include "boot.h"
#include "uart.h"
#include "config.h"

static_assert(sizeof(const void *) == sizeof(uint16_t), "Pointer type different then 2; update the protocol!");
static_assert(sizeof(MessageType) == sizeof(uint8_t), "sizeof(MessageType) == sizeof(uint8_t)");

#if !defined (DEBUG)
void __reset(void) __attribute__ ((naked, section(".init9")));
void __reset(void)
{
	asm volatile (".set __stack, RAMEND");
	SP = RAMEND;
	asm volatile ("clr __zero_reg__"); 
	asm volatile ("rjmp main");
}
#endif


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

		// instead of _delay_ms(1) use loop. Execution time is similar
		// in release mode, but binary code is shorter. In debug mode this doesn't matter.
		for (int i = 0; i < 1660; i++)
			asm volatile("nop");

		//_delay_ms(1);
		if (wait_counter++ > ADVERTISEMENT_WAIT_TIME) { // wait some time for bootloader activation byte
#if defined (NDEBUG)
			___boot_demo();
#endif
			asm volatile("jmp 0000");
		}
	}

	
	// 00 - uint8_t: address
	// 01 - uint8_t: command
	while(1)
	{
		// receive address
		uint8_t addr = uartReceive();
		if (rx.timeout) continue;

#if defined (DEBUG)
		// in debug mode: respond to requests on multiple addresses
		if (addr != BOOTLOADER_HARDWARE_ADDRESS && addr != 0x40&& addr != 0x50&& addr != 0x52&& addr != 0xB0)
#else
		// in release mode: respond only to the selected address
		if (addr != BOOTLOADER_HARDWARE_ADDRESS)
#endif
			continue; // its not for me

		// receive command
		MessageType msg_type = (MessageType)uartReceive();
		if (rx.timeout) continue;

		// receive payload length
		uint8_t payload_size = uartReceive();
		if (rx.timeout) continue;


		if (payload_size > MAX_PAYLOAD_SIZE)
			continue; // something is wrong

		uint16_t checksum = (uint16_t)addr + (uint16_t)msg_type + (uint16_t)payload_size;

		// receive data
		rx.ptr = rx.data;
		rx.endptr = rx.data + payload_size;
		while (rx.endptr != rx.ptr) {
			uint8_t data = uartReceive();
			if (rx.timeout) break;

			*rx.ptr++ = data;
			checksum += (uint16_t)data;
		}
		if (rx.timeout) continue;

		// apply checksum
		checksum -= (uint16_t)uartReceive() << 8;
		if (rx.timeout) continue;

		checksum -= uartReceive();
		if (rx.timeout) continue;

		if (checksum != 0)
			continue; // error in message

		if (msg_type == MessageType::Ping) // got challenge, send response -- identify itself
		{
			send_response(msg_type, addr, rx.data, payload_size);
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

		if (msg_type == MessageType::ReadSignature) {
			for (uint8_t addr = 0x00; addr < 0x20; addr++)
				rx.data[addr] = boot_signature_byte_get(addr);
			send_response(msg_type, addr, rx.data, 0x20);
		}

		if (msg_type == MessageType::ReadBootloaderVersion) {

			#define STR_HELPER(x) #x
			#define STR(x) STR_HELPER(x)
			#define NOP asm volatile("nop");
			char ver[] = "v=" STR(PROTOCOL_VERSION) ";d=" __DATE__ ";t=" __TIME__;
			send_response(msg_type, addr, (const uint8_t*)ver, sizeof(ver));
			#undef STR_HELPER
			#undef STR
		}

	}

	
}

