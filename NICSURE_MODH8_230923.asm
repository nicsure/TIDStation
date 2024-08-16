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

.org        0xd2b1
printSmall:

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

;.org        0x55fd
;    .byte   0x3f
;.org        0x55ff
;    .byte   0xab
;.org        0x5601
;    .byte   0x01

;.org        0x5fb9
;    .byte   0x3f
;.org        0x5fbb
;    .byte   0xab
;.org        0x5fbd
;    .byte   0x01

;.org        0x6c7f
;    .byte   0x3f
;.org        0x6c82
;    .byte   0xab
;.org        0x6c85
;    .byte   0x01

;.org        0x6d04
;    .byte   0x3f
;.org        0x6d06
;    .byte   0xab
;.org        0x6d08
;    .byte   0x01

;.org        0x832d
;    .byte   0x3f
;.org        0x832f
;    .byte   0xab
;.org        0x8331
;    .byte   0x01

;.org        0x8aff                        ; first hook for backlight PWM (on)
;    ljmp    backlightOn
;.org        0xe44f                        ; second hook for backlight PWM (on)
;    lcall   backlightOn
;.org        0xccc4                        ; third hook for backlight PWM (off)
;    lcall   backlightOff
;    nop
;.org        0xdae2                        ; forth hook for backlight test on
;    ;.byte   0x20, 0x78, 0x44
;    ret

.org        0xecc5                        ; set freq function
    LJMP    setFreqHook
    NOP
setFreqResume:

.org        0x8aff
    ljmp    brightnessHookOn
.org        0xccc4
    lcall   brightnessHookOff
    nop
.org        0xe44f
    lcall   brightnessHookOn
.org        0xdae2
    ljmp    screenOnTest

.org        0xa661
    lcall   pwmInitHook

.org 0xd904
    .byte 0x43

.org        0xf051                        ; end of original firmware

pwmInitHook:
    ;.byte   0xd2, 0xc3
    lcall   0xd8fd
    ret   
    mov     dptr, #0xf043
    mov     a, #5
    movx    @dptr, a
    mov     dptr, #0xf120
    clr     a
    movx    @dptr, a
    inc     dptr
    movx    a, @dptr
    orl     a, #1
    movx    @dptr, a
    mov     dptr, #0xf123
    mov     a, #1
    movx    @dptr, a
    mov     dptr, #0xf129
    movx    a, @dptr
    orl     a, #1
    movx    @dptr, a
    mov     dptr, #0xf122
    movx    a, @dptr
    anl     a, #0xfe
    movx    @dptr, a
    clr     a
    mov     dptr, #0xf12a
    movx    @dptr, a
    mov     dptr, #0xf127
    movx    a, @dptr
    orl     a, #1
    movx    @dptr, a
    clr     a
    mov     dptr, #0xf131
    movx    @dptr, a
    mov     a, #0xc8
    mov     dptr, #0xf130
    movx    @dptr, a
    clr     a
    mov     dptr, #0xf141
    movx    @dptr, a
    mov     dptr, #0xf140
    movx    @dptr, a
    mov     dptr, #0xf126
    movx    a, @dptr
    orl     a, #1
    movx    @dptr, a 
    ret

screenOnTest:
    mov     a, 0x77
    jnz     screenIsOn
screenIsOff:
    ljmp    0xdae5
screenIsOn:
    ljmp    0xdb29

brightnessHookOn:
    mov     0x77, #1
    mov     dptr, #0x704
    movx    a, @dptr
    xrl     a, #0xff
    mov     b, a
    mov     a, #6
    add     a, b
    mov     dptr, #brightPWM
    ;lcall   addAtoDPTR
    clr     a
    movc    a, @a+dptr
    mov     r7, #255 ; a
    acall   setPWM
    clr     a
    ret

brightnessHookOff:
    clr     a
    mov     0x77, a
    mov     r7, a
    ;acall   setPWM
    mov     r7, #0x28
    ret

setPWM:
    ;;.byte   0xc2, 0xc3
    mov     dptr, #0xf126
    movx    a, @dptr
    anl     a, #0xfe
    movx    @dptr, a
    mov     dptr, #0xf140
    mov     a, r7
    movx    @dptr, a
    mov     dptr, #0xf126
    movx    a, @dptr
    orl     a, #0x01
    movx    @dptr, a
    ;.byte   0xd2, 0xc3
    ret

freqOffsets:
    .byte   0x2c, 0xcf ; -200
    .byte   0xa0, 0x0f ; +66
    .byte   0xad, 0xf8 ; -30

setFreqHook:
    mov     dptr, #0x702
    movx    a, @dptr
    jz      noShift
    dec     a
    rl      a
    mov     dptr, #freqOffsets
    lcall   addAtoDPTR
    clr     a
    movc    a, @a+dptr
    clr     c
    addc    a, r5
    mov     r5, a
    inc     dptr
    clr     a
    movc    a, @a+dptr
    addc    a, r7
    mov     r7, a
noShift:
    .byte   0x8b, 0x82, 0xab, 0x07  ; mov dpl, r3 ; mov r3, r7
    ljmp    setFreqResume 

battLevel:
    mov     dptr, #0x701            ; batt setting
    movx    a, @dptr
    jnz     percentage
    ljmp    0xab39                  ; relocate
voltage:

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
S:
    .byte "S",0
plusses:
    .byte "+00",0
    .byte "+10",0
    .byte "+20",0
    .byte "+30",0
    .byte "+40",0
    .byte "+60",0
    .byte "+OL",0
    .byte "+A1",0
    .byte "+A2",0
    .byte "+A3",0
    .byte "+A4",0
    .byte "+A5",0
    .byte "+A6",0

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
    jnz     origReadEeprom
isFirstRun:
    LCALL   origReadEeprom
    mov     dptr, #0xf30
    mov     a, #1
    movx    @dptr, a
    mov     0x4d, #0x01 ; read
    mov     0x4e, #0x07 ; high byte dest address
    mov     0x4f, #0x00 ; low byte dest address
    mov     R3, #0x20 ; read 32 byte
    mov     R7, #0x11 ; high byte eeprom address
    mov     R5, #0x00 ; low byte eeprom address
    LCALL   origReadEeprom
    mov     dptr, #0x71f
    movx    a, @dptr
    cjne    a, #0xaf, noEepromCheckByte
    ret
noEepromCheckByte:
    mov     a, #0xaf
    movx    @dptr, a
    clr     a
    mov     dph, #0x07
    mov     r1, #0
eepromClearLoop:
    mov     dpl, r1
    movx    @dptr, a
    inc     r1
    cjne    r1, #0x1f, eepromClearLoop
    ret
origReadEeprom:
    .byte   0x8b, 0x4c, 0xaa, 0x05
    LJMP    readEepromResume

commitNicModSettings:
    mov     r5, #0x00
    mov     r7, #0x11
    mov     r3, #0x20
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
    cjne    a, #0x11, tryPrevious
    mov     a, 0x71
    inc     a
    mov     b, #5
    acall   rollAcc
    mov     0x71, a
    sjmp    drawMenu
tryPrevious:
    cjne    a, #0x12, exitProcessKey
    mov     a, 0x71
    dec     a
    mov     b, #5
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
    lcall   mulABaddDPTR
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
    
    mov     a, 0x72                     ; selected option
    clr     c
    subb    a, 0x75
    jc      notBadOption
    clr     a
    mov     0x72, a
    mov     r1, dph
    mov     r2, dpl
    mov     dph, #0x07
    mov     dpl, 0x71
    movx    @dptr, a
    mov     dph, r1
    mov     dpl, r2

notBadOption:
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
    .byte   3,  "0.SigBar Style", 0
    .word   battOptions
    .byte   3,  "1.Batt Display", 0
    .word   freqOptions
    .byte   4,  "2.Freq Adjust ", 0
    .word   sCalOptions
    .byte   13, "3.SMeter Calib", 0
    .word   sCalOptions
    .byte   6,  "4.Brightness  ", 0

sigBarStyleOptions:
    .byte   "Solid       ",0
    .byte   "Segmented   ",0
    .byte   "S-Meter Pro ",0
battOptions:
    .byte   "Icon        ",0
    .byte   "Percentage  ",0
    .byte   "Voltage     ",0
freqOptions:
    .byte   "OFF         ",0
    .byte   "-200 MHz    ",0
    .byte   "+64 MHz     ",0
    .byte   "-30 MHz     ",0
sCalOptions:
    .byte   "+6          ",0
    .byte   "+5          ",0
    .byte   "+4          ",0
    .byte   "+3          ",0
    .byte   "+2          ",0
    .byte   "+1          ",0
    .byte   "0           ",0
    .byte   "-1          ",0
    .byte   "-2          ",0
    .byte   "-3          ",0
    .byte   "-4          ",0
    .byte   "-5          ",0
    .byte   "-6          ",0

brightPWM:
    .byte   255,215,175,135,85,35

rxNoSignal:    
    mov     dptr, #0x0600
    movx    a, @dptr
    jz      okayToBlank
    ret
okayToBlank:
    inc     a
    movx    @dptr, a
    mov     0x50, #0
    mov     0x51, #0
    mov     dptr, #0x700
    movx    a, @dptr
    cjne    a, #2, regularBarBlank
proBarBlank:
    mov     r3, #16
    mov     r5, #7
    sjmp    setLeftEdge
regularBarBlank:    
    mov     r3, #6
    mov     r5, #17
setLeftEdge:
    mov     r7, #0
    lcall   fillArea
    mov     0x55, #0
    acall   rssiBar2    
    ret

calibFloor:
    mov     dptr, #0x703
    movx    a, @dptr
    rl      a
    rl      a
    add     a, #196
    mov     r7, a
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
    acall   calibFloor
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
    subb    a, r7
    jnc     scaleDB
    clr     a
    sjmp    scaleDB
aLot:
    clr     c
    subb    a, r7
scaleDB:
    mov     0x55, a
    acall   rssiBar2
    mov     a, 0x53
    cjne    a, #2, set600    
    acall   sigText
set600:
    mov     dptr, #0x600
    clr     a
    movx    @dptr, a
    ljmp    resumeRxTicker
retRxTicker:
    ret

sigText:
    mov     a, 0x55
    push    acc
    mov     dptr, #S
    mov     r2, dph
    mov     r1, dpl
    mov     r3, #0xff
    mov     r5, #115
    mov     0x54, #0xff
    mov     0x55, #0xff
    mov     0x53, #8
    lcall   printMedium
    pop     acc
    mov     b, #25
    mul     ab
    mov     a, b
    clr     c
    push    acc
    subb    a, #10
    jc      sAsIs
    mov     b, #9
sAsIs:
    mov     a, b
    rl      a
    mov     dptr, #num0
    clr     c
    addc    a, dpl
    mov     r1, a
    clr     a
    addc    a, dph
    mov     r2, a
    mov     r3, #0xff
    mov     r5, #123
    mov     0x54, #0xff
    mov     0x55, #0xff
    lcall   printMedium
    pop     acc
    clr     c
    subb    a, #10
    jc      plusZero
    mov     b, a
    subb    a, #6
    jnc     clamp6
    inc     b
    sjmp    doPlus
    clamp6:
    mov     b, #6
    sjmp    doPlus
    plusZero:
    mov     b, #0
    doPlus:    
    mov     a, b
    rl      a
    rl      a
    mov     dptr, #plusses
    clr     c
    addc    a, dpl
    mov     r1, a
    clr     a
    addc    a, dph
    mov     r2, a
    mov     r3, #0xff
    mov     0x45, #0x00
    mov     0x46, #0xf8
    mov     r5, #131
    mov     0x44, #10
    lcall   printSmall
    ret

rssiBar2:
    ;MOV     0x4d,r7                     ; start col
    ;MOV     0x4e,r5                     ; start line
    ;MOV     0x4f,r3                     ; end line
    ;mov     0x50,r1                     ; end col
    ;mov     0x52,r0                     ; sig strength
    MOV     0x4f,#23                    ; end line
    mov     dptr, #0x700
    movx    a, @dptr
    mov     0x53, a                     ; sig bar style
    cjne    a, #2, notPro
    isProMeter:          
    mov     0x50, #111                  ; 111 end col for pro meter
    mov     0x4d, #6                    ; 6 start col for pro meter
    mov     0x4e, #7                    ; 7 start line for pro meter    
    mov     b, #201
    mov     a, 0x55
    sjmp    scaleSig
    notPro:
    mov     0x50, #160                  ; 160 end col for non pro meter
    mov     0x4d, #0                    ; 0 start col for non pro meter
    mov     0x4e, #17                   ; 17 start line for non pro meter
    mov     b, #140
    mov     a, 0x55                     ; signal strength
    .byte   0x25, 0xe0                  ; ADD A, A (double A)
    jnc     scaleSig
    mov     a, #0xfe    
    scaleSig:
    mul     ab
    mov     0x52, b
    nextLine:
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
        mov     0x51, 0x4d
        mov     0x54, #0xff
        mov     0x56, #0
        nextCol:
            mov     a, 0x51
            cjne    a, 0x50, notAtEndCol
            sjmp    atEndCol
            notAtEndCol:
            clr     c
            subb    a, 0x52
            jc      skipGreyFlag
            mov     0x56, #1
            skipGreyFlag:
            mov     a, 0x53
            cjne    a, #2, regularMeterColors

            proMeterColors:
            acall   threeCounter
            jnc     drawAsBlack
            mov     a, 0x56
            jnz     greyBarsP
            mov     a, 0x51
            clr     c
            subb    a, #42
            jc      greenBarsP
            subb    a, #36
            jc      yellowBarsP
            redBarsP:
            mov     r5, #0xf8
            mov     r7, #0x00
            sjmp    drawPixel
            yellowBarsP:
            mov     r5, #0xff
            mov     r7, #0xc0
            sjmp    drawPixel
            greenBarsP:
            mov     r5, #0x07
            mov     r7, #0xc0
            sjmp    drawPixel
            greyBarsP:
            mov     r5, #0x18
            mov     r7, #0xc6
            sjmp    drawPixel

            regularMeterColors:
            mov     a, 0x56
            jnz     drawAsBlack
            mov     a, 0x53
            jz      solidBar
            mov     a, 0x51
            jb      acc.1, drawAsBlack
            solidBar:
            mov     a, 0x51
            acall   regMeterGradient                           ; red level
            rl      a
            rl      a
            rl      a
            mov     r5, a
            mov     a, #159
            clr     c
            subb    a, 0x51
            acall   regMeterGradient                           ; green level
            rr      a
            rr      a
            mov     b, a
            anl     a, #7
            orl     a, r5
            mov     r5, a
            mov     a, b
            anl     a, #0xc0
            mov     r7, a
            sjmp    drawPixel

            drawAsBlack:
            mov     r5, #0
            mov     r7, #0
            drawPixel:
            LCALL   0xed55
            inc     0x51
        sjmp    nextCol
        atEndCol:
        inc     0x4e
        mov     a, 0x4e
    cjne    a, 0x4f, nextLineProxy
    ret
    nextLineProxy:
    ajmp    nextLine

regMeterGradient:
    mov     b, #100
    mul     ab
    mov     a, b
    clr     c
    subb    a, #0x20
    jnc     max1f
    mov     a, b
    ret
    max1f:
    mov     a, #0x1f
    ret

threeCounter:
    clr     c
    inc     0x54
    mov     a, 0x54
    cjne    a, #6, not6
    clr     a
    mov     0x54, a
not6:
    subb    a, #3
    ret

powerBracket:
    .byte   0xc2, 0xca
    mov     0x3a, #0x0a
    mov     dptr, #0x600
    clr     a
    movx    @dptr, a
    mov     0x70, #0
    ;ret
    LJMP    resumeBracket

drawNumbersHook:
    mov     dptr, #0x700
    movx    a, @dptr
    cjne    a, #2, showNumbers
    sjmp    powerBracket
showNumbers:
    mov     r2, #0x1c
    mov     r1, #0x37
    ljmp    numbersResume

txMeterHook:
    mov     0x55, 0x3f
    lcall   rssiBar2
    ret

mulABaddDPTR:
    mul     ab
    clr     c
    addc    a, dpl
    mov     dpl, a
    mov     a, b
    addc    a, dph
    mov     dph, a
    ret

addAtoDPTR:
    clr     c
    addc    a, dpl
    mov     dpl, a
    mov     a, dph
    addc    a, #0
    mov     dph, a
    ret

.org        0x94b2
    nop
    nop
    nop

.org        0x94ea
    ljmp    txMeterHook


.org        0x9432
    ljmp    drawNumbersHook
    nop
numbersResume:

.org        0x94cb
    .byte   0x23

.org        0x94e2
    .byte   0x3b

.org        0x94e9
    .byte   0x6a



