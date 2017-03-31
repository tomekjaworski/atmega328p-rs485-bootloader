/*
 * atmega328p_bootloader.cpp
 *
 * Created: 3/28/2017 22:10:49
 * Author : Tomek
 */ 

#include <avr/io.h>
#include <avr/fuse.h>
#include <avr/boot.h>

FUSES =
{
	.low = (FUSE_CKSEL0 & FUSE_CKSEL2 & FUSE_CKSEL3 & FUSE_SUT0 & FUSE_CKDIV8),
	.high = (FUSE_BOOTSZ0 & FUSE_BOOTSZ1 & FUSE_SPIEN & FUSE_BOOTRST),
	.extended = EFUSE_DEFAULT,
};

int main(void)
{
	
}

