.ORG    0
reset:

.ORG            0x6dc0
    lcall       myReset

; write eeprom function
.ORG            0xd8c8
writeEeprom:
;
; eeprom address to write to
; R7 - High Byte
; R5 - Low byte
;
; address of data to copy in extmem
; 0x3D - high byte
; 0x3E - low byte
; 0x3C - set to #0x01
;
; length of data to write
; R3

.ORG            0xEFE0                      ; start of patch code
myReset:
    mov         dptr, #0x47c
    movx        a, @dptr
    clr         c
    subb        a, #0x20
    jc          nextCheck
    sjmp        return

nextCheck:
    inc         dptr
    movx        a, @dptr
    anl         a, #0x1f
    jz          loop
    sjmp        return
    
loop:
    mov         dptr, #0x47c
    movx        a, @dptr
    mov         b, a
    mov         r7, a
    inc         dptr
    movx        a, @dptr
    mov         r5, a
    mov         r3, #0x20
    clr         c
    addc        a, #0x20
    jnc         dontIncB
    inc         b
dontIncB:
    movx        @dptr, a
    mov         dptr, #0x47c
    mov         a, b
    movx        @dptr, a
    mov         0x3c, #0x01
    mov         0x3d, #0x04
    mov         0x3e, #0x80
    lcall       writeEeprom
    mov         dptr, #0x47c
    movx        a, @dptr
    cjne        a, #0x20, return
    ljmp        reset
return:
    anl         p0, #0xf0
    ret



