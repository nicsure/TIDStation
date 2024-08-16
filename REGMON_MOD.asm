.ORG        0xE6a3                  ; BK4819 Set Register function & Hook (4 bytes)
setReg:                             ; Original code: 8f 4d      MOV     0x4d,r7
    LCALL   setRegDetour            ;                8d 4e      MOV     0x4e,r5
    NOP
setRegResume:


.ORG        0xEb14                  ; Serial send byte function, byte to send in R7
sendSerialByte:


.ORG        0xf03b                  ; start of mod code

setRegDetour:
    mov     dptr, #707
    mov     a, r7
    movx    @dptr, a
    mov     dptr, #705
    mov     a, r5
    movx    @dptr, a
    mov     dptr, #703
    mov     a, r3
    movx    @dptr, a

    mov     r7, #0x91
    lcall   sendSerialByte    
    mov     dptr, #707
    movx    a, @dptr
    mov     r7, a
    lcall   sendSerialByte    
    mov     dptr, #705
    movx    a, @dptr
    mov     r7, a
    lcall   sendSerialByte    
    mov     dptr, #703
    movx    a, @dptr
    mov     r7, a
    lcall   sendSerialByte    


    mov     dptr, #707
    movx    a, @dptr
    mov     r7, a
    mov     dptr, #705
    movx    a, @dptr
    mov     r5, a
    mov     dptr, #703
    movx    a, @dptr
    mov     r3, a


    mov     0x4d, r7
    mov     0x4e, r5
    ljmp    setRegResume
    