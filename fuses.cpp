/*
 * fuses.cpp
 *
 * Created: 4/1/2017 13:34:32
 *  Author: Tomasz Jaworski
 */ 

#include <avr/io.h>
#include <avr/fuse.h> // after IO
#include <avr/lock.h>

FUSES =
{
	.low = 0xFF,
#if defined (DEBUG)
	// DEBUG
	// 2048 words for bootloader
	.high = (FUSE_SPIEN & FUSE_EESAVE & FUSE_BOOTRST & FUSE_BOOTSZ0 & FUSE_BOOTSZ1 ),
#else
	// RELEASE
	// 512 words for bootloader; 512 words = 1024 bytes
	// entry point .text=0x3E00
	//.high = (FUSE_SPIEN & FUSE_EESAVE & FUSE_BOOTRST & FUSE_BOOTSZ0 /*& FUSE_BOOTSZ1*/ ),

	// 1024 words for bootloader; 1024 word = 2048 bytes;
	// entry point: 0x3C00 (byte 0x7800)
	.high = (FUSE_SPIEN & FUSE_EESAVE & FUSE_BOOTRST & /*FUSE_BOOTSZ0 &*/ FUSE_BOOTSZ1 ),

#endif
	.extended = FUSE_BODLEVEL0 & FUSE_BODLEVEL1,
};

LOCKBITS = (BLB1_MODE_2);

