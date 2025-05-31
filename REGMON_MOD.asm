.ORG        0xE6a3                  ; BK4819 Set Register function & Hook (4 bytes)
setReg:                             ; Original code: 8f 4d      MOV     0x4d,r7
    LCALL   setRegDetour            ;                8d 4e      MOV     0x4e,r5
    NOP
setRegResume:


.ORG        0xd0e4                  ; RDA5807 send byte
;    LCALL   rda5807Detour

.ORG        0xd857                  ; 5807 read byte
;    LCALL   rda5807ReadDetour


.ORG        0xcf85                  ; bt test, reads SBUF
;    LCALL   sbufMonitor

.ORG        0x8b79                  ; serial read tick
;    LCALL   tickTest

.ORG        0xEb14                  ; Serial send byte function, byte to send in R7
sendSerialByte:

.ORG        0xcf6b
            ret


.ORG        0xf03b                  ; start of mod code



tickTest:
    jnb     0xB3, BTLOW
    setb    0xb5
    ljmp    0xcf6b
BTLOW:
    clr     0xb5
    ljmp    0xcf6b

sbufMonitor:
    mov     a, 0x99
    movx    @dptr, a
    mov     b, r6
    push    b
    mov     r7, a
    LCALL   sendSerialByte
    pop     b
    mov     r6, b
    ret


rda5807ReadDetour:
    mov     a, 0x31
    push    acc
    mov     r7, #0x98
    lcall   sendSerialByte
    pop     acc
    push    acc
    mov     r7, a
    lcall   sendSerialByte
    pop     acc
    mov     r7, a
    ;mov     r7, 0x31
    ret

rda5807Detour:
    mov     a, r7
    push    acc
    mov     r7, #0x99
    lcall   sendSerialByte
    pop     acc
    push    acc
    mov     r7, a
    lcall   sendSerialByte
    pop     acc
    mov     r7, a
    mov     0x4d, r7
    clr     a
    ret

setRegDetour:
    mov     dptr, #0x800
    movx    a, @dptr
    jnz     skipSerialInit
    mov     a, #1
    movx    @dptr, a

    CLR     A
    MOV     0x35,A                   
    MOV     0x34,#0xe1               
    MOV     0x33,A                   
    MOV     0x32,A                   
    MOV     R7,#0x10
    MOV     R6,A
    LCALL   0xdb26                     
    CLR     A
    MOV     0x35,#0x80               
    MOV     0x34,#0x25               
    MOV     0x33,A                   
    MOV     0x32,A                   
    MOV     R7,#0x30
    MOV     R6,A
    LCALL   0xddf9                     




skipSerialInit:
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
    