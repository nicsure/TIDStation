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

.org        0xbe9c
    LJMp    menuKeyHook
resumeMenuKey:

.org        0xbab2
printMedium:

.org        0xdbc5
writeEeprom:

.org        0xd5b0                        ; read eeprom function address
readEeprom:
    ljmp    readEepromHook
    nop
readEepromResume:


;.org 0xdae2
;ret

.org        0xd4b0                        ; capture batt level
    LCALL   captureBattLevel
    NOP
.ORG        0xdf44                        ; routine that draws the battery icon
    ljmp    battLevel

.org        0x8aff                        ; first hook for backlight PWM (on)
    ljmp    backlightOn
.org        0xe44f                        ; second hook for backlight PWM (on)
    lcall   backlightOn
.org        0xccc4                        ; third hook for backlight PWM (off)
    lcall   backlightOff
    nop
.org        0xdae2                        ; forth hook for backlight test on
    ;.byte   0x20, 0x78, 0x44
    ret

.org        0xf051                        ; end of original firmware

backlightInterrupt:
    inc     0x78
    mov     b, 0x78
    reti


backlightOn:
    mov     0x78, #1
    mov     0xd7, #0
    mov     0xd4, #0x8f
    mov     0xd3, #0xff
    mov     dptr, #0x1046
    mov     a, #0x88
    movx    @dptr, a
    inc     dptr
    clr     a
    movx    @dptr, a
    clr     a
    ret

backlightOff:
    mov     0x78, #0
    mov     r7, #0x28
    mov     dptr, #0x1046
    mov     a, #0x80
    movx    @dptr, a
    inc     dptr
    clr     a
    movx    @dptr, a
    ret


battLevel:
    mov     dptr, #0x701            ; batt setting
    movx    a, @dptr
    jnz     percentage
    ljmp    0xab39                  ; relocate
percentage:
    mov     dptr, #0xa06            ; high byte batt level
    movx    a, @dptr
    mov     b, a
    mov     dptr, #0xa07            ; low byte batt level
    movx    a, @dptr
    mov     r0, A
    mov     a, b
    subb    a, #0x0c
    rr      a
    rr      a
    mov     b, a
    mov     a, r0
    rr      a
    rr      a
    anl     a, #0x3f
    orl     a, b
setBattColor:
    mov     b, a
    clr     c
    subb    a, #50
    jc      redBatt
    subb    a, #167
    jc      whiteBatt
    mov     0x54, #0xc0
    mov     0x55, #0x07
    sjmp    battColSet
redBatt:
    mov     0x54, #0x00
    mov     0x55, #0xf8
    sjmp    battColSet
whiteBatt:
    mov     0x54, #0xff
    mov     0x55, #0xff
battColSet:
    mov     a, b
    mov     b, #10                  ; scale by 10
    mul     ab
    mov     r3, b                   ; percentage 10's digit into r3
    mov     b, #10                  ; a now has the low byte of the scaled value, scale this again by 10
    mul     ab                      ; this gives us a 100 scale for 1's digit
    push    b                       ; save the 1's digit to the stack
    
    mov     dptr, #0x900
    mov     a, #0x1a
    movx    @dptr, a
    
    mov     a, r3
    mov     r5, #126
    acall   printDigit
    pop     acc
    mov     r5, #134
    acall   printDigit
    mov     a, #10
    mov     r5, #142
    ;acall   printDigit
    ;ret

printDigit:                         ; a=digit, xm0x900=y, r5=x
    mov     dptr, #num0
    mov     r1, dpl
    mov     r2, dph
    rl      a
    clr     c
    addc    a, r1
    mov     r1, a
    clr     a
    addc    a, r2
    mov     r2, a
    MOV     R3, #0xFF
    mov     dptr, #0x900
    movx    a, @dptr
    mov     0x53, a
    LCALL   printMedium
    ret

num0:
    .byte 0x30,0
num1:
    .byte 0x31,0
num2:
    .byte 0x32,0
num3:
    .byte 0x33,0
num4:
    .byte 0x34,0
num5:
    .byte 0x35,0
num6:
    .byte 0x36,0
num7:
    .byte 0x37,0
num8:
    .byte 0x38,0
num9:
    .byte 0x39,0
pct:
    .byte "%",0

captureBattLevel:
    .byte   0xe5, 0x0e              ; mov a, bank1_r6
    MOV     DPTR, #0xA06
    MOVX    @DPTR, A
    .byte   0xe5, 0x0f              ; mov a, bank1_r7
    MOV     DPTR, #0xA07
    MOVX    @DPTR, A
    SUBB    A, #0x22
    ret

readEepromHook:
    mov     dptr, #0xf30
    movx    a, @dptr
    jnz     notFirstRun
    .byte   0x8b, 0x4c, 0xaa, 0x05
    LCALL   readEepromResume
    mov     dptr, #0xf30
    mov     a, #1
    movx    @dptr, a
    mov     0x4d, #0x01 ; read
    mov     0x4e, #0x07 ; high byte dest address
    mov     0x4f, #0x00 ; low byte dest address
    mov     R3, #0x10 ; read 16 byte
    mov     R7, #0x17 ; high byte eeprom address
    mov     R5, #0x00 ; low byte eeprom address
    mov     dptr, #0x750
    mov     a, #1
    movx    @dptr, a
notFirstRun:
    .byte   0x8b, 0x4c, 0xaa, 0x05
    LJMP    readEepromResume

commitNicModSettings:
    mov     r5, #0x00
    mov     r7, #0x17
    mov     r3, #0x10
    mov     0x3c, #0x01
    mov     0x3d, #0x07
    mov     0x3e, #0x00
    ljmp    writeEeprom

menuKeyHook:
    mov     r7, #0x99
    lcall   sendSerial
    mov     dptr, #0x379
    movx    a, @dptr
    mov     r7, a
    lcall   sendSerial
    mov     a, 0x70
    jz      regularMenu
customMenu:
    mov     a, 0x76
    jnz     stopKeyRepeat
    mov     0x76, #1
    mov     dptr, #0x379
    movx    a, @dptr
    cjne    a, #0x0e, processMenuKey
cm_Exit:
    mov     0x70, #0
    sjmp    noCustomMenu
stopKeyRepeat:
    ret
regularMenu:
    mov     dptr, #0x379
    movx    a, @dptr
    cjne    a, #0x13, rm_NotPTT
rm_PTT:
    mov     0x70, #1
    mov     0x76, #1
    sjmp    drawMenu
rm_NotPTT:
noCustomMenu:
    mov     dptr, #0x379
    ljmp    resumeMenuKey

processMenuKey:
tryUp:
    cjne    a, #0x0c, tryDown
    acall   getSelection
    inc     acc
    mov     b, 0x75
    acall   rollAcc
    acall   setSelection
    lcall   commitNicModSettings
    sjmp    drawMenu
tryDown:
    cjne    a, #0x0d, tryNext
    acall   getSelection
    dec     acc
    mov     b, 0x75
    acall   rollAcc
    acall   setSelection
    lcall   commitNicModSettings
    sjmp    drawMenu
tryNext:
    cjne    a, #0x12, tryPrevious
    mov     a, 0x71
    inc     a
    mov     b, #3
    acall   rollAcc
    mov     0x71, a
    sjmp    drawMenu
tryPrevious:
    cjne    a, #0x11, exitProcessKey
    mov     a, 0x71
    dec     a
    mov     b, #3
    acall   rollAcc
    mov     0x71, a
    sjmp    drawMenu
exitProcessKey:
    ret

rollAcc:
    mov    r0, a
    cjne   a, #0xff, notMinusOne
    mov    a, b
    dec    a
    ret
notMinusOne: 
    clr    c
    subb   a, b
    jc     accInRange
    clr    a
    ret
accInRange:
    mov    a, r0
    ret

getSelection:
    mov     dph, #0x07
    mov     dpl, 0x71                   ; current menu 71
    movx    a, @dptr
    mov     0x72, a                     ; selected option 72
    ret

setSelection:
    mov     dph, #0x07
    mov     dpl, 0x71                   ; current menu 71
    movx    @dptr, a
    ret

drawMenu:
    mov     0x50, #0
    mov     0x51, #0
    mov     r3, #64
    mov     r5, #64
    mov     r7, #0
    lcall   fillArea
    mov     dptr, #menuTitle
    mov     r2, dph
    mov     r1, dpl
    mov     r3, #0xff
    mov     0x54, #0xfe
    mov     0x55, #0x39
    mov     R5, #0x0a
    mov     0x53, #70
    lcall   printMedium
    acall   getSelection
    mov     dptr, #menuHeaders
    mov     a, 0x71
    mov     b, #18
    mul     ab
    clr     c
    addc    a, dpl
    mov     dpl, a
    mov     a, b
    add     a, dph
    mov     dph, a
    clr     a
    movc    a, @a+dptr
    mov     0x73, a                     ; high byte options address 73
    inc     dptr
    clr     a
    movc    a, @a+dptr
    mov     0x74, a                     ; low byte options address 74
    inc     dptr
    clr     a
    movc    a, @a+dptr
    mov     0x75, a                     ; number of options 75
    inc     dptr
    mov     r2, dph
    mov     r1, dpl
    mov     r3, #0xff
    mov     0x54, #0xff
    mov     0x55, #0xff
    mov     R5, #0x13
    mov     0x53, #88
    lcall   printMedium
    mov     r2, 0x73
    mov     r1, 0x74
    mov     a, 0x72
    mov     b, #13
    mul     ab
    clr     c
    addc    a, r1
    mov     r1, a
    mov     a, b
    addc    a, r2
    mov     r2, a
    mov     r3, #0xff
    mov     0x54, #0xc0
    mov     0x55, #0x07
    mov     R5, #0x1a
    mov     0x53, #106
    lcall   printMedium
    ret

menuTitle:
    .byte   "nicmod Menu", 0

menuHeaders:
    .word   sigBarStyleOptions
    .byte   2, "0.SigBar Style", 0
    .word   battOptions
    .byte   2, "1.Batt Display", 0
    .word   dimmerOptions
    .byte   5, "2. Brightness ", 0

sigBarStyleOptions:
    .byte   "Solid       ",0
    .byte   "Segmented   ",0
battOptions:
    .byte   "Icon        ",0
    .byte   "Percentage  ",0
dimmerOptions:
    .byte   "Dimmest     ",0
    .byte   "Dim         ",0
    .byte   "Medium      ",0
    .byte   "Bright      ",0
    .byte   "Brightest   ",0

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
    mov     dptr, #0x379
    movx    a, @dptr
    jnz     dontReset76
    mov     0x76, #0
dontReset76:
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
    mov     b, 0x53
    mov     dptr, #0x700
    movx    a, @dptr
    jz      noSegments
    jb      b.1, isBlack
noSegments:
    mov     a, b
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
    mov     0x70, #0
    LJMP    resumeBracket





