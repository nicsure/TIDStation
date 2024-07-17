.org        0x1c37
    .BYTE   0x31, 0, 0x33, 0, 0x35, 0, 0x37, 0, 0x39, 0, 0x2b, 0x34, 0x30, 0  ; s meter digits

.org        0x9478          ; adjust address of 40+
    mov     r1, #0x41

.org        0x947c          ; stop 40+ lowering by 2
    nop
    nop

.org        0x9439          ; 1  (horizontal position adjustment of digits)
    mov     r5, #13
.org        0x9445          ; 3
    mov     r5, #35
.org        0x9453          ; 5
    mov     r5, #57
.org        0x9461          ; 7
    mov     r5, #79
.org        0x946f          ; 9
    mov     r5, #101
.org        0x9486          ; 40+
    mov     r5, #123

.org        0xecac
sendSerial:

.org        0xd6b4
rxTicker:
    ljmp    rxTickerHook
.org        0xd6bc
resumeRxTicker:

.org        0xdb78
fillArea:

.org        0x949b          ; power bracket
    LJMP    powerBracket
    nop
    nop
resumeBracket:

.org        0xc4f7
readRegister:
;    LJMP    readRegisterHook
;    NOP
resumeReadRegister:







.org        0xf051                        ; end of original firmware

rxNoSignal:
    mov     dptr, #0x0600
    movx    a, @dptr
    jnz     retRxTicker
    inc     a
    movx    @dptr, a
    mov     0x51, #0
    mov     r3, #6
    mov     r5, #17
    mov     r7, #0
    lcall   rssiBar
    ret

rxTickerHook:
    .byte   0xc2, 0x66
    mov     a, 0x25
    jnb     acc.3, rxNoSignal
    jnb     acc.4, retRxTicker
    mov     r7, #0x9b
    lcall   readRegister
    mov     dptr, #0x01b4
    movx    a, @dptr
    mov     b, a
    mov     dptr, #0x01b5
    movx    a, @dptr
    xrl     a, #0xff
    clr     c
    addc    a, b
    jc      aLot
notALot:
    clr     c
    subb    a, #220
    jnc     scaleDB
    clr     a
    sjmp    scaleDB
aLot:
    clr     c
    subb    a, #220
scaleDB:
    mov     b, #187
    mul     ab
    mov     a, b
    .byte   0x25, 0xe0                     ; ADD A, A
    mov     0x51, a
    mov     r3, #6
    mov     r5, #17
    mov     r7, #0
    lcall   rssiBar
    mov     dptr, #0x600
    clr     a
    movx    @dptr, a
    ljmp    resumeRxTicker
retRxTicker:
    ret

rssiBar:
    MOV     0x4d,r7 
    MOV     0x4e,r5 
    MOV     0x4f,r3 
    MOV     r7,#0x2a
    LCALL   0xeed6         
    MOV     r7,0x4d 
    LCALL   0xefa1         
    MOV     r7,0x4d 
    LCALL   0xefa1         
    MOV     r7,#0x2b
    LCALL   0xeed6         
    MOV     r7,0x4e 
    LCALL   0xefa1         
    MOV     r7,0x4e 
    LCALL   0xefa1         
    MOV     r7,#0x2c
    LCALL   0xeed6         
    CLR     A
    MOV     0x52,A       
LAB_CODE_dba4:                     
    MOV     A,0x52       
    CLR     CY
    SUBB    A,0x4f       
    JNC     LAB_CODE_dbc4
    MOV     0x53,0x4d
    mov     0x50, #0
LAB_CODE_dbae:                     
    MOV     A,0x53
    cjne    a, 0x51, skipBlackFlag
    orl     0x50, #1
skipBlackFlag:   
    CLR     CY
    SUBB    A,#0xa0
    JNC     LAB_CODE_dbc0
    mov     a, 0x50
    jnz     isBlack

    mov     a, 0x53
    jb      acc.1, isBlack
    clr     c
    subb    a, #0x3f
    jnc     lastHalf
firstHalf:                      ; green full, red increasing
    mov     a, 0x53
    mov     b, #0x80
    mul     ab
    mov     a, b
    rl      a
    rl      a
    rl      a
    orl     a, #7
    mov     r5, a
    mov     r7, #0xc0
    sjmp    drawDot
lastHalf:                       ; red full, green decreasing
    mov     a, 0x53
    clr     c
    subb    a, #0x3f
    mov     b, #0x80
    mul     ab
    mov     a, b
    clr     c
    subb    a, #0x1f
    jnc     tooHigh
    mov     a, b
    sjmp    invertA
tooHigh:
    mov     a, #0x1f
invertA:
    xrl     a, #0x1f
    rr      a
    rr      a
    mov     r0, a
    anl     a, #0xc0
    mov     r7, a
    mov     a, r0
    orl     a, #0xf8
    mov     r5, a
    sjmp    drawDot




    ;mov     a, 0x53             ; green
    ;jb      acc.1, isBlack
    ;add     a, #0x5f
    ;xrl     a, #0xff
    ;anl     a, #0xf8
    ;rl      a
    ;rl      a
    ;rl      a
    ;mov     b, a
    ;anl     b, #0x07
    ;anl     a, #0xc0
    ;mov     r7, a
    ;mov     a, 0x53             ; red
    ;add     a, #0x5f
    ;anl     a, #0xf8
    ;orl     a, b
    ;mov     r5, a
    ;sjmp    drawDot
isBlack:
    mov     r5, #0
    mov     r7, #0
drawDot:
    LCALL   0xed55        
    INC     0x53        
    SJMP    LAB_CODE_dbae
LAB_CODE_dbc0:                    
    INC     0x52        
    SJMP    LAB_CODE_dba4
LAB_CODE_dbc4:                    
    RET



powerBracket:
    .byte   0xc2, 0xca
    mov     0x3a, #0x0a
    mov     dptr, #0x600
    clr     a
    movx    @dptr, a
    LJMP    resumeBracket

readRegisterHook:
    cjne    r7, #0x9c, not9c
    sjmp    noAction
not9c:
    mov     a, r7
    push    acc
    .byte   0xc2, 0x44
    .byte   0xc2, 0x28
    lcall   resumeReadRegister
    
    mov     r7, #0x91
    lcall   sendSerial
    pop     acc
    push    acc
    mov     r7, a
    lcall   sendSerial
    mov     dptr, #0x1b4
    movx    a, @dptr
    mov     r7, a
    lcall   sendSerial
    mov     dptr, #0x1b5
    movx    a, @dptr
    mov     r7, a
    lcall   sendSerial

    pop     acc
    mov     r7, a
noAction:
    .byte   0xc2, 0x44
    .byte   0xc2, 0x28
    LJMP    resumeReadRegister



