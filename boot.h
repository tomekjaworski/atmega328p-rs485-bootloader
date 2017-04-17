/*
 * boot.h
 *
 * Created: 3/31/2017 21:06:33
 *  Author: Tomasz Jaworski
 */ 


#ifndef BOOT_H_
#define BOOT_H_

void bootInitialize(void);
void bootRestart(void);
void bootStorePage(uint32_t page, const uint8_t* buf);

// sample demo program in user space
void ___boot_demo(void);

#endif /* BOOT_H_ */