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
	 .high = (FUSE_SPIEN & FUSE_EESAVE & FUSE_BOOTSZ0 & FUSE_BOOTSZ1 & FUSE_BOOTRST),
	 .extended = FUSE_BODLEVEL0 & FUSE_BODLEVEL1,
 };

 LOCKBITS = (BLB1_MODE_2);

