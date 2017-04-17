/*
 * boot.cpp
 *
 * Created: 3/31/2017 20:59:42
 *  Author: Tomasz Jaworski
 */ 

 #include <avr/boot.h>
 #include <avr/wdt.h>
 #include <avr/interrupt.h>
 #include "boot.h"
 
 void bootInitialize(void)
 {
	cli();
	MCUSR = 0x00;
	WDTCSR = 0x00;
	wdt_disable();
	eeprom_busy_wait();
 }
 
 void bootStorePage(uint32_t page, const uint8_t* buf)
 {
	boot_page_erase(page);
	boot_spm_busy_wait();

	for (int i = 0; i < SPM_PAGESIZE; i+=2)
	{
		uint16_t w = *buf++;
		w |= (*buf++) << 8;
		boot_page_fill(page + i, w);
	}
	boot_page_write(page);
	boot_spm_busy_wait();

	boot_rww_enable();
 }

 void bootRestart(void)
{
	// allow new code to be executed
	boot_rww_enable();

	// start watchdog and lock so we can reboot
	wdt_enable(WDTO_15MS);
	while(1);
}