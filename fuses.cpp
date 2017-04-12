/*
 * fuses.cpp
 *
 * Created: 4/1/2017 13:34:32
 *  Author: Tomek
 */ 

 #include <avr/io.h>
 #include <avr/fuse.h> // after IO
 
 FUSES =
 {
	 .low = 0xFF,
	 .high = (FUSE_SPIEN & FUSE_EESAVE & FUSE_BOOTSZ0 & FUSE_BOOTSZ1 & FUSE_BOOTRST),
	 .extended = FUSE_BODLEVEL0 & FUSE_BODLEVEL1,
 };


