
.ORG        0x6B12                  ; mic gain fix
    .BYTE   0x4, 0x7, 0xA, 0xD, 0x10, 0x13, 0x16, 0x19, 0x1C, 0x1F

.ORG        0xC875                  ; BK4819 Read Register function
getReg:

.ORG        0xd4a9                  ; fill screen area
fillArea:

.ORG        0xAEcd                  ; print regular size text?  r1=low byte code mem, r2=high byte code mem
printRegularText:

.ORG        0xCd14                  ; print small size tex? r1=low byte code mem, r2=high byte code mem
    ;LCALL   printSmallText
    ;NOP
printSmall:                         ; r1=low pointer, r2=high pointer, r3=0xff, 0x43=y coord, 0x44/0x45=color, r5=x coord

.ORG        0xE6a3                  ; BK4819 Set Register function & Hook (4 bytes)
setReg:                             ; Original code: 8f 4d      MOV     0x4d,r7
    LCALL   setRegDetour            ;                8d 4e      MOV     0x4e,r5
    NOP
setRegResume:

.ORG        0x7602                  ; remove original AM indicator
    NOP
    NOP
    NOP

.ORG        0x8324                  ; remove "POWER" text
    LCALL   plus40

.ORG        0x8334                  ; remove rssi "bracket"
    LJMP    rssiBracket
resumeRssiBracket:

.ORG        0x830d                  ; 9 on the meter
    LCALL   green9

.ORG        0x82c3                  ; vertical location of s-meter numbers sync mode
    .BYTE   0x05
.ORG        0x82cd                  ; vertical location of s-meter numbers non sync mode
    .BYTE   0x05

.ORG        0x5e0d                  ; hook signal level poll function (3 bytes)
    LJMP    rssiDetour              ; Original code: 90 02 f5   MOV     DPTR,#0x2f5
.ORG        0x5e1d
resumeRssiDetour:
.ORG        0x5e73
rssiNoSignal:

.ORG        0xEb14                  ; Serial send byte function, byte to send in R7
sendSerialByte:

.ORG        0x0122
    .BYTE   0x31, 0, 0x33, 0, 0x35, 0, 0x37, 0, 0x39, 0, 0x34, 0x30, 0x2b, 0
.ORG        0x82d5
    mov     r5, #0x04
.ORG        0x82e1
    mov     r5, #0x16
.ORG        0x82ef
    mov     r5, #0x28
.ORG        0x82fd
    mov     r5, #0x3a
.ORG        0x830b
    mov     r5, #0x4c
.ORG        0x8322
    mov     r5, #0x5f

.ORG        0x2d14
    .byte   0x0a
.ORG        0x2d16
    .byte   0xd1

;.ORG        0x4abe ; currently unknown
;    .byte   0x0a
;.ORG        0x4ac0
;    .byte   0xd1
;
;.ORG        0x4f3f ; currently unknown
;    .byte   0x0a
;.ORG        0x4f41
;    .byte   0xd1
;
;.ORG        0x6d0b ; currently unknown
;    .byte   0x0a
;.ORG        0x6d0d
;    .byte   0xd1
;
;.ORG        0x7940 ; currently unknown
;    .byte   0x0a
;.ORG        0x7942
;    .byte   0xd1


.org        0xd2a8                  ; read eeprom function address
readEeprom:
    ljmp    readEepromHook
    nop
readEepromResume:

.ORG        0xd8ed                  ; write eeprom function address
writeEeprom:

.ORG        0x6e28                  ; button scanner
    LJMP   buttonScanner
buttonScannerResume:

;ID    KEY
; 0 = no key
; 1 = 0
; 2 = 1
; ...
; 9 = 8
; A = 9
; B = Menu (blue)
; C = up
; D = down
; E = Cancel (red)
; F = *
;10 = #
;12 = flashlight
;13 = PTT-A (large)
;1A = PTT-B (small)


.ORG        0xd141                  ; menu mode key press
    LCALL   menuKey

.ORG        0x61ca                  ; vfo mode key press
    LCALL   vfoKey

.ORG        0x2a39
    LJMP    fineStepDetour
    NOP
fineStepResume:

.ORG        0x2400
ctcssDiffs:
    .byte   50,47,54,52,53,56,58,60,64,62,68,54,53,73,76,76,81,83,86,89,93,97,99,101,108,109,64,50,68,49,71,51,72,54,74,56,77,60,78,60,82,62,87,153,157,70,93,169,176,78
ctcssTones:
    .byte   "067.0",0
    .byte   "069.3",0
    .byte   "071.9",0
    .byte   "074.4",0
    .byte   "077.0",0
    .byte   "079.7",0
    .byte   "082.5",0
    .byte   "085.4",0
    .byte   "088.5",0
    .byte   "091.5",0

    .byte   "094.8",0
    .byte   "097.4",0
    .byte   "100.0",0
    .byte   "103.5",0
    .byte   "107.2",0
    .byte   "110.9",0
    .byte   "114.8",0
    .byte   "118.8",0
    .byte   "123.0",0
    .byte   "127.3",0

    .byte   "131.8",0
    .byte   "136.5",0
    .byte   "141.3",0
    .byte   "146.2",0
    .byte   "151.4",0
    .byte   "156.7",0
    .byte   "159.8",0
    .byte   "162.2",0
    .byte   "165.5",0
    .byte   "167.9",0

    .byte   "171.3",0
    .byte   "173.8",0
    .byte   "177.3",0
    .byte   "179.9",0
    .byte   "183.5",0
    .byte   "186.2",0
    .byte   "189.9",0
    .byte   "192.8",0
    .byte   "196.6",0
    .byte   "199.5",0

    .byte   "203.5",0
    .byte   "206.5",0
    .byte   "210.7",0
    .byte   "218.1",0
    .byte   "225.7",0
    .byte   "229.1",0
    .byte   "233.6",0
    .byte   "241.8",0
    .byte   "250.3",0
    .byte   "254.1",0

;fiveSpaces:
;    .byte   " "
;fourSpaces:
;    .byte   "    ",0

allZeros:
    .byte   "000.0",0

fineSteps:
    .byte   0,0,0,1
    .byte   0,0,0,2
    .byte   0,0,0,5
    .byte   0,0,0,10
    .byte   0,0,0,25
    .byte   0,0,0,50
    .byte   0,0,0,100

labelUSB:
    .BYTE   "USB",0
labelAM:
    .BYTE   "AM ",0
labelFM:
    .BYTE   "FM ",0

.ORG        0xf03b                  ; start of mod code

fineStepDetour:
    push    acc
    mov     dptr, #0x702
    MOVX    a, @dptr
    jz      noFineTuneActive
    pop     b
    push    acc
    mov     dptr, #0x465
    mov     a, #2
    movx    @dptr, a
    mov     dptr, #0x4a3
    mov     a, #0x07
    movx    @dptr, a
    mov     dptr, #0x14b
    movx    a, @dptr
    anl     a, #0x0f
    orl     a, #0x70
    movx    @dptr, a
    pop     acc
    dec     a
    mov     dptr, #fineSteps
    mov     b, #4
    mul     ab
    clr     C
    addc    a, dpl
    mov     dpl, a
    mov     a, dph
    addc    a, #0x00
    mov     dph, a
    ret
noFineTuneActive:
    pop     acc
    .byte   0x25, 0xe0, 0x25, 0xe0
    LJMP    fineStepResume

getRegSafe:
    mov     dptr, #0x2f5
    movx    a, @dptr
    push    acc
    inc     dptr
    movx    a, @dptr
    push    acc
    mov     dptr, #0x2c3
    movx    a, @dptr
    push    acc
    mov     dptr, #0x2e5
    movx    a, @dptr
    push    acc
    LCALL   getReg
    mov     dptr, #0x2f5
    movx    a, @dptr
    mov     r5, a
    inc     dptr
    movx    a, @dptr
    mov     r3, a
    mov     dptr, #0x2e5
    pop     acc
    movx    @dptr, a
    mov     dptr, #0x2c3
    pop     acc
    movx    @dptr, a
    mov     dptr, #0x2f6
    pop     acc
    movx    @dptr, a
    mov     dptr, #0x2f5
    pop     acc
    movx    @dptr, a
    ret

rssiBracket:
    ;mov     r7, #0x36
    ;mov     r5, #0xff
    ;mov     r3, #0xa2
    ;LCALL   setReg

    ;mov     a, 0xc1             ; p4 control SFR
    ;clr     acc.7               ; set bit 7 to input (uhf filter off)
    ;mov     0xc1, a             ; put back into SFR

    ;mov     a, 0xc1             ; p4 control SFR
    ;setb    acc.5               ; set bit 5 to output (vhf filter)
    ;mov     0xc1, a             ; put back into SFR
    ;mov     a, 0xc0             ; p4 state SFR
    ;clr     acc.5               ; set bit 5 to low (sync) (turn on VHF filter?)
    ;mov     0xc0, a             ; put back into SFR


    ;mov     a, 0xd9              ; p5 control SFR
    ;clr     acc.5                ; set bit 5 to input (uhf rx filter off)
    ;mov     0xd9, a              ; put back into SFR

    ;mov     a, 0xc1              ; p4 control SFR
    ;setb    acc.4                ; set bit 4 to output (vhf rx filter)
    ;mov     0xc1, a              ; put back into SFR
    ;mov     a, 0xc0              ; p4 state SFR
    ;clr     acc.4                ; set bit 4 to low (sync) (turn on VHF rx filter)
    ;mov     0xc0, a              ; put back into SFR





    mov     dptr, #0x750
    mov     a, #1
    movx    @dptr, a
    mov     0x50, #0xff
    mov     0x51, #0xff
    mov     r3, #1
    mov     r5, #15
    mov     r7, #0
    lcall   fillArea
    .byte   0xc2, 0x69, 0xe4
    ljmp    resumeRssiBracket

green9:
    mov     0x44, #0xf0
    mov     0x45, #0x3f
    LCALL   printSmall
    ret

plus40:
    mov     0x43, 0x37
    dec     0x43
    mov     0x44, #0xff
    mov     0x45, #0x22
    LCALL   printSmall
    ret

toneMonitor:
    mov     r7, #0x68
    lcall   getRegSafe
    mov     a, r5
    anl     a, #0x80
    jnz     noCtcssDetected
    mov     r7, #2
    sjmp    toneDetected
noCtcssDetected:
    mov     r7, #0x69
    lcall   getRegSafe
    mov     a, r5
    anl     a, #0x80
    jnz     noDcsDetected
    mov     r7, #1
    sjmp    toneDetected
noDcsDetected:
    mov     dptr, #0x600
    movx    a, @dptr
    jz      noPreviousToneDisplayed
    mov     dptr, #0x600
    clr     a
    movx    @dptr, a

    mov     dptr, #allZeros
    MOV     R2, DPH
    MOV     R1, DPL
    MOV     R3, #0xFF
    MOV     R5, #96
    MOV     0x43, #102
    mov     0x44, #0xef
    mov     0x45, #0xef
    LCALL   printSmall


;    mov     r5, #12
;workOutVertical:
;    mov     dptr, #0x145
;    movx    a, @dptr
;    jnb     acc.6, noSync2
;    mov     a, #63
;    sjmp    gotVertical
;noSync2:
;    mov     dptr, #0xb8
;    movx    a, @dptr
;    jz      isVfoA2
;    mov     a, #85
;    sjmp    gotVertical
;isVfoA2:
;    mov     a, #103
;gotVertical:
;    mov     dptr, #fiveSpaces
;    acall   blankSpaces
;    mov     dptr, #0x600
;    clr     a
;    movx    @dptr, a
noPreviousToneDisplayed:
    ret

toneDetected:
    mov     dptr, #0x600
    mov     a, r7
    movx    @dptr, a
    mov     a, r5
    cjne    r7, #1, isCtcss
isDcs:
    anl     a, #0xf
    mov     dptr, #0x601
    movx    @dptr, a
    inc     dptr
    mov     a, r3
    movx    @dptr, a
    mov     r7, #0x6a    
    lcall   getRegSafe
    mov     dptr, #0x603
    mov     a, r5
    anl     a, #0xf
    movx    @dptr, a
    inc     dptr
    mov     a, r3
    movx    @dptr, a
    sjmp    toneReady1
isCtcss:
    anl     a, #0x1f
    mov     dptr, #0x601
    movx    @dptr, a
    inc     dptr
    mov     a, r3
    movx    @dptr, a
toneReady1:
    mov     dptr, #0x600
    movx    a, @dptr
    cjne    a, #2, toneisDcs     
    sjmp    ctcssTone
toneisDcs:
    ajmp    dcsTone


sub16:
    MOV     A, R3
    CLR     C
    SUBB    A, R0
    MOV     R3, A
    MOV     A, R5
    SUBB    A, R1    
    MOV     R5, A
    RET

ctcssTone:
    mov     dptr, #0x602
    MOVX    a, @dptr
    mov     r3, a
    mov     dptr, #0x601
    MOVX    a, @dptr
    mov     r5, a
    mov     r1, #0x05
    mov     r0, #0x4e
    acall   sub16
    mov     r1, #0
    clr     b
ctcssLoop:
    mov     dptr, #ctcssDiffs
    mov     a, dpl
    clr     c
    addc    a, b
    mov     dpl, a
    mov     a, #0
    addc    a, dph
    mov     dph, a
    clr     a
    movc    a, @a+dptr
    mov     r0, a
    acall   sub16
    jc      foundCtcss
    inc     b
    mov     a, b
    cjne    a, #50, ctcssLoop
noValidTone:
    ret
foundCtcss:
;    mov     dptr, #0x145
;    movx    a, @dptr
;    jnb     acc.6, noSync
;    mov     r7, #63
;    sjmp    foundCtcss1
;noSync:
;    mov     dptr, #0xb8
;    movx    a, @dptr
;    jz      isVfoA
;    mov     r7, #85
;    sjmp    foundCtcss1
;isVfoA:
;    mov     r7, #103
foundCtcss1:
    mov     a, b                    ; b is the index
    mov     b, #6
    mul     ab
    mov     dptr, #ctcssTones
    clr     c
    addc    a, dpl
    mov     dpl, a
    mov     a, b
    addc    a, dph
    mov     dph, a
    ; r1=low pointer, r2=high pointer, r3=0xff, 0x43=y coord, 0x44/0x45=color, r5=x coord
    MOV     R2, DPH
    MOV     R1, DPL
    MOV     R3, #0xFF
    MOV     R5, #96
    MOV     0x43, #102
    ;mov     a, r7
    ;push    acc
    mov     0x44, #0xef
    mov     0x45, #0xef
    LCALL   printSmall
    ret

    ;MOV     R5, #52
    ;mov     dptr, #fourSpaces
    ;pop     acc
;blankSpaces:
    ;MOV     R2, DPH
    ;MOV     R1, DPL
    ;MOV     R3, #0xFF
    ;MOV     0x53, a
    ;mov     0x54, #0xff
    ;mov     0x55, #0xff
    ;LCALL   printRegularText

dcsTone:
    ret


noSignalDetected:
    mov     dptr, #0x750
    movx    a, @dptr
    jz      skipRssi
    clr     a
    movx    @dptr, a
    mov     0x50, #0
    mov     0x51, #0
    mov     r3, #8
    mov     r5, #16
    mov     r7, #0
    lcall   fillArea
skipRssi:
    LJMP    rssiNoSignal
rssiDetour:
    mov     dptr, #0x701
    movx    a, @dptr
    jnz     noKillDisable
    mov     dptr, #0x14a
    movx    a, @dptr
    anl     a, #0xef
    movx    @dptr, a
noKillDisable:
    MOV     A, 0x25
    JNB     ACC.1, noSignalDetected
    MOV     A, 0x27
    JNB     ACC.3, skipRssi
    CLR     ACC.3
    MOV     0x27, A
    mov     dptr, #0x703            ; hooks setting
    movx    a, @dptr
    jz      hooksOkay
    MOV     DPTR, #0x2F5            ; move the high byte address into DPTR to put state back to how it should be
    LJMP    resumeRssiDetour        ; jump back to original function    
hooksOkay:
    mov     dptr, #0x704            ; tone monitor setting
    movx    a, @dptr
    jz      noToneMonitorSet
    mov     dptr, #0x4ff            ; ext menu active?
    movx    a, @dptr
    jnz     noToneMonitorSet
    lcall   toneMonitor
noToneMonitorSet:
    MOV     R7, #0x65
    LCALL   getRegSafe
    MOV     A, R3
    ANL     A, #0x7F
    MOV     B, A
    MOV     A, #0x65
    CLR     C
    SUBB    A, B
    JNC     noCarry1
    CLR     A
noCarry1:
    PUSH    ACC
    MOV     R7, #0x67
    LCALL   getRegSafe
    MOV     A, R5                   ; move high byte into A
    ANL     A, #0x01                ; we only need bit 0
    mov     r1, a
    POP     ACC
    MOV     R0, A
    mov     b, r1
    mov     a, r3
    CLR     C
    ADDC    A, R0
    JNC     noCarry2
    INC     B
noCarry2:
    JNB     B.1, notTooHigh
    mov     a, #0xff
    mov     b, #1
notTooHigh:
    ANL     a, #0xfe
    RR      a
    JNB     B.0, not9Bit
    ORL     a, #0x80
not9Bit:
    subb    a, #50
    jnc     notneg
    clr     a
notneg:
    mov     dptr, #0x750
    movx    @dptr, a
    mov     0x51, a
    mov     r3, #6
    mov     r5, #17
    mov     r7, #0
    lcall   rssiBar

exitBarDraw:
    MOV     DPTR, #0x2F5            ; move the high byte address into DPTR to put state back to how it should be
    LJMP    resumeRssiDetour        ; jump back to original function

exitSetRegTop:
    LJMP    exitSetReg

setRegDetour:
    MOV     0x4D, R7                ; perform one of the instructions replaced by the hook
    mov     dptr, #0x703            ; hooks setting
    movx    a, @dptr
    jnz     exitSetRegTop           ; exit if hooks are blocked
    CJNE    R7, #0x47, not47        ; check to see if we're setting reg 47 (modulation)
    SJMP    reg47
not47:
    CJNE    R7, #0x3D, not3D        ; check to see if we're setting reg 3D
    SJMP    reg3D
not3D:
    CJNE    R7, #0x73, not73        ; check to see if we're setting reg 73
    SJMP    reg73
not73:
    mov     dptr, #0x900
    mov     a, r7
    movx    @dptr, a
    inc     dptr
    mov     a, r5
    movx    @dptr, a
    inc     dptr
    mov     a, r3
    movx    @dptr, a
    mov     r7, #0x99
    lcall   sendSerialByte
    mov     dptr, #0x900
    movx    a, @dptr
    mov     r7, a
    lcall   sendSerialByte
    mov     dptr, #0x901
    movx    a, @dptr
    mov     r5, a
    mov     dptr, #0x902
    movx    a, @dptr
    mov     r3, a
    SJMP    exitSetReg

reg73:
    MOV     DPTR, #0x700            ; 700 is mod ovr setting
    MOVX    A, @DPTR                ; get this byte
    JZ      normalMode
    MOV     A, R3
    ORL     A, #0x10
    sjmp    adjusted73
normalMode:
    ANL     A,#0xEF
adjusted73:
    MOV     R3, A
    SJMP    exitSetReg

reg3D:
    MOV     DPTR, #0x700            ; 700 is mod ovr setting
    MOVX    A, @DPTR                ; get byte from ext mem
    CJNE    A, #0x2, set2AAB
    CLR     A                       ; set A to 0
    MOV     R3, A                   ; for USB we need reg 3D to be 0, so set high (R5) and low (R3) bytes to 0 (A)
    MOV     R5, A
    SJMP    exitSetReg              ; resume
set2AAB:
    MOV     R5, #0x2A
    MOV     R3, #0xAB
    SJMP    exitSetReg              ; resume

reg47:
    MOV     DPTR, #0x700            ; 700 is mod ovr setting
    MOVX    A, @DPTR                ; grab this byte from extmem
    JNZ     overrideModul           ; if any bits are set A will be non zero, this means we need to override the moduation
    MOV     0x4E, R5
    LCALL   setRegResume
    MOV     R5, #0x2A
    MOV     R3, #0xAB
    MOV     R7, #0x3D
    MOV     0x4D, R7
    MOV     0x4E, R5
    LCALL   setRegResume
    MOV     R7, #0x73
    LCALL   getRegSafe              ; sets r3 and r5
    MOV     A, R3
    ANL     A, #0xEF
    MOV     R3, A
    MOV     R7, #0x73
    MOV     0x4D, R7
    MOV     0x4E, R5
    LCALL   setRegResume
    LCALL   setModLabel
    POP     ACC
    POP     ACC
    RET
    
exitSetReg:
    MOV     0x4E, R5                ; perform the other instruction replaced by the hook
    RET                             ; return to original function




overrideModul:
    PUSH    ACC
    CJNE    A, #0x1, isUSB          ; 0x1 is the code for AM
isAM:
    MOV     R5, #0x67               ; 0x67 is the high byte modulation register value for AM, replaces the original value
    SJMP    exitSetReg2
isUSB:
    MOV     R5, #0x65               ; 0x65 is the high byte modulation register value for USB
exitSetReg2:
    MOV     R3, #0x40
    MOV     0x4E, R5
    LCALL   setRegResume 
setAFC:
    POP     ACC
    CJNE    A, #0x2, notUSB
    MOV     R5, #0x00
    MOV     R3, #0x00
    SJMP    set3DRegister
notUSB:
    MOV     R5, #0x2A
    MOV     R3, #0xAB
set3DRegister:
    MOV     R7, #0x3D
    MOV     0x4D, R7
    MOV     0x4E, R5
    LCALL   setRegResume
read73Register:
    MOV     R7, #0x73
    LCALL   getRegSafe
    MOV     A, r3
    ORL     A, #0x10
set73Register:
    MOV     R3, A
    MOV     R7, #0x73
    MOV     0x4D, R7
    MOV     0x4E, R5 
    LCALL   setRegResume
    LCALL   setModLabel
    POP     ACC
    POP     ACC
    RET

setModLabel:
    mov     dptr, #0x4ff
    movx    a, @dptr
    jnz     setModLabelExit
    MOV     R7, #0x47
    LCALL   getRegSafe
    mov     a, r5
    JB      ACC.2, amOrUSB
    JB      ACC.0, inFM
    RET
amOrUSB:
    JB      ACC.1, inAM
inUSB:
    MOV     DPTR, #labelUSB
    SJMP    printLabel
inFM:
    MOV     DPTR, #labelFM
    SJMP    printLabel
inAM:
    MOV     DPTR, #labelAM
printLabel:
    MOV     R2, DPH
    MOV     R1, DPL
    MOV     R3, #0xFF
    ;LCALL   0x4782
    MOV     R5, #0x52
    MOV     0x53, #0x1A
    LCALL   printRegularText
setModLabelExit:
    RET

; 0 = no key
; 1 = 0
; ...
; A = 9
; B = Menu (blue)
; C = up
; D = down
; E = Cancel (red)
; F = star
;10 = #
;12 = flashlight
;13 = PTT (large)
;1A = PTT (small)

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
    mov     R7, #0x1a ; high byte eeprom address
    mov     R5, #0x00 ; low byte eeprom address
    mov     dptr, #0x750
    mov     a, #1
    movx    @dptr, a
notFirstRun:
    .byte   0x8b, 0x4c, 0xaa, 0x05
    LJMP    readEepromResume

buttonScanner:
    mov     dptr, #0xf31
    movx    a, @dptr
    jz      noUpdate
    clr     A
    movx    @dptr, a
    mov     r5, #0x00
    mov     r7, #0x1a
    mov     r3, #0x10
    mov     0x3c, #0x01
    mov     0x3d, #0x07
    mov     0x3e, #0x00
    lcall   writeEeprom
noUpdate:
    anl     p0, #0xf0
    LJMP    buttonScannerResume

vfoKey:
    mov     dptr, #0x4ff
    clr     a
    movx    @dptr, a
    mov     dptr, #0x479
    ret

menuKey:
    mov     dptr, #0x4ff
    movx    a, @dptr
    jz      normalMenu
    dec     a
    jz      exMenu
    mov     dptr, #0x479
    ret    
normalMenu:
    mov     dptr, #0x479
    movx    a, @dptr
    cjne    a, #0x13, resumeMenu
    mov     dptr, #0x4ff
    mov     a, #1
    movx    @dptr, a
    LCALL   displayMenu
    pop     acc
    pop     acc
resumeMenu:
    ret
exMenu:
    mov     dptr, #0x479
    movx    a, @dptr
    cjne    a, #0x0e, tryUp
    mov     dptr, #0x4ff
    clr     a
    movx    @dptr, a
    mov     dptr, #0x479
    ret
tryUp:
    cjne    a, #0xc, tryDown
    mov     a, #0xff
    acall   upDown
    sjmp    exitExMenu
tryDown:
    cjne    a, #0xd, tryBlue
    mov     a, #1
    acall   upDown
    sjmp    exitExMenu
tryBlue:
    cjne    a, #0xb, exitExMenu
    mov     dptr, #0xe10
    movx    a, @dptr
    inc     a
    cjne    a, #0x05, notTooMuch  ; total menu check
    clr     a
notTooMuch:
    mov     dptr, #0xe10
    movx    @dptr, a
    LCALL   displayMenu
exitExMenu:
    pop     acc
    pop     acc
    ret

upDown:
    mov     b, a
    mov     dptr, #0xe00
    movx    a, @dptr
    mov     r2, a                   ; menu number
    mov     dptr, #0xe04
    movx    a, @dptr
    mov     r1, a                   ; option count
    mov     dptr, #0xe06            ; selected option
    movx    a, @dptr
    clr     c
    addc    a, b
    cjne    a, #0xff, notMinusOne
    mov     a, r1
    dec     a
    sjmp    noOverFlow
notMinusOne:
    mov     b, r1
    cjne    a, b, noOverFlow
    clr     a
noOverFlow:
    mov     dph, #0x07
    mov     dpl, r2
    movx    @dptr, a
    mov     a, r2
    acall   displayMenu
    mov     dptr, #0xf31
    mov     a, #1
    movx    @dptr, a
    ret



displayMenu:
    mov     0x50, #0
    mov     0x51, #0
    mov     r3, #140
    mov     r5, #20
    mov     r7, #0
    lcall   fillArea
    mov     dptr, #0xe10
    movx    a, @dptr
    mov     r7, a                   ; menu number into r7
    mov     dptr, #menuTitles
    mov     b, #18
    mul     ab
    clr     c
    addc    a, dpl
    mov     dpl, a
    mov     a, b
    addc    a, dph
    mov     dph, a
    clr     a
    movc    a, @a+dptr
    mov     r1, a                   ; options hi byte into r1
    inc     dptr
    clr     a
    movc    a, @a+dptr
    mov     r0, a                   ; options lo byte into r0
    inc     dptr
    clr     a
    movc    a, @a+dptr
    mov     r2, a                   ; number of options into r2
    inc     dptr
    mov     r4, dph
    mov     r3, dpl                 ; title string into r3(lo) and r4(hi)
    mov     dph, #0x7
    mov     dpl, r7
    movx    a, @dptr
    mov     r5, a                   ; selected option into r5
    mov     dptr, #0xe00
    mov     a, r7
    movx    @dptr, a                ; menu number into 0xe00
    inc     dptr
    mov     a, r4
    movx    @dptr, a
    inc     dptr
    mov     a, r3
    movx    @dptr, a                ; title address into 0xe01
    inc     dptr
    mov     a, r2
    movx    @dptr, a                ; option count into 0xe03
    inc     dptr
    movx    @dptr, a                ; static option count into 0xe04
    inc     dptr
    mov     a, r5
    movx    @dptr, a                ; selected option into 0xe05
    inc     dptr
    movx    @dptr, a                ; static selected option into 0xe06
    inc     dptr
    mov     a, r1
    movx    @dptr, a
    inc     dptr
    mov     a, r0
    movx    @dptr, a                ; options address into 0xe07
    inc     dptr
    mov     a, #32
    movx    @dptr, a                ; position into 0xe09


    mov     dptr, #0xe01
    movx    a, @dptr
    mov     r2, a
    inc     dptr
    movx    a, @dptr
    mov     r1, a
    MOV     R3, #0xFF
    mov     0x54, #0xff
    mov     0x55, #0xff
    mov     r5, #0
    mov     0x53, #28
    LCALL   printRegularText

optionsLoop:
    mov     dptr, #0xe03
    movx    a, @dptr
    jnz     moreOptions
    ret
moreOptions:
    dec     a
    movx    @dptr, a
    mov     dptr, #0xe05
    movx    a, @dptr
    push    acc
    dec     a
    movx    @dptr, a
    mov     dptr, #0xe04
    movx    a, @dptr
    clr     C
    subb    a, #6
    jc      notSmall
useSmallText:
    mov     dptr, #0xe07
    movx    a, @dptr
    mov     r2, a
    inc     dptr
    movx    a, @dptr
    mov     r1, a
    clr     c
    addc    a, #13
    movx    @dptr, a
    jnc     skipHiByteInc    
    mov     dptr, #0xe07
    movx    a, @dptr
    inc     a
    movx    @dptr, a
skipHiByteInc:
    mov     dptr, #0xe09
    movx    a, @dptr
    clr     c
    add     a, #10
    movx    @dptr, a
    mov     0x43, a
    push    acc
    mov     r3, #0xff
    mov     0x44, #0xff
    mov     0x45, #0xff
    mov     r5, #20
    LCALL   printSmall
    pop     acc
    mov     0x43, a
    pop     acc
    jnz     optionsLoop
    mov     r3, #0xff
    mov     0x44, #0xff
    mov     0x45, #0xff
    mov     r5, #2
    mov     dptr, #indicator
    mov     r2, dph
    mov     r1, dpl
    LCALL   printSmall
    ajmp    optionsLoop
notSmall:
    mov     dptr, #0xe07
    movx    a, @dptr
    mov     r2, a
    inc     dptr
    movx    a, @dptr
    mov     r1, a
    clr     c
    addc    a, #13
    movx    @dptr, a
    jnc     skipHiByteIncS
    mov     dptr, #0xe07
    movx    a, @dptr
    inc     a
    movx    @dptr, a
skipHiByteIncS:
    mov     dptr, #0xe09
    movx    a, @dptr
    clr     c
    add     a, #16
    movx    @dptr, a
    mov     0x53, a
    push    acc
    mov     r3, #0xff
    mov     0x54, #0xff
    mov     0x55, #0xff
    mov     r5, #20
    LCALL   printRegularText
    pop     acc
    mov     0x53, a
    pop     acc
    jnz     skipMarker
    mov     r3, #0xff
    mov     0x54, #0xff
    mov     0x55, #0xff
    mov     r5, #2
    mov     dptr, #indicator
    mov     r2, dph
    mov     r1, dpl
    LCALL   printRegularText
skipMarker:
    ajmp    optionsLoop
noMoreOptions:
    ret

indicator:
    .byte   ">",0
menuTitles:
    .word   menuOptions0
    .byte   3,"0.AM/USB Ovr  ",0
    .word   menuOptions1
    .byte   2,"1.Kill Killer ",0
    .word   menuOptions2
    .byte   8,"2.Fine Step   ",0
    .word   menuOptions3
    .byte   2,"3.Mod Hooks   ",0
    .word   menuOptions4
    .byte   2,"4.Tone Monitor",0
;    .word   menuOptions5
;    .byte   8,"5.TX Power Ovr",0
menuOptions0:
    .byte   "OFF         ",0
    .byte   "AM          ",0
    .byte   "USB         ",0
menuOptions1:
    .byte   "Prevent Kill",0
    .byte   "Normal      ",0
menuOptions2:
    .byte   "OFF         ",0
    .byte   "0.01 K      ",0
    .byte   "0.02 K      ",0
    .byte   "0.05 K      ",0
    .byte   "0.10 K      ",0
    .byte   "0.25 K      ",0
    .byte   "0.50 K      ",0
    .byte   "1.00 K      ",0
menuOptions3:
    .byte   "Allowed     ",0
    .byte   "Blocked     ",0
menuOptions4:
    .byte   "OFF         ",0
    .byte   "Enabled     ",0
;menuOptions5:
;    .byte   "  OFF       ",0
;    .byte   "  0.0 pct   ",0
;    .byte   " 16.7 pct   ",0
;    .byte   " 33.3 pct   ",0
;    .byte   " 50.0 pct   ",0
;    .byte   " 66.7 pct   ",0
;    .byte   " 83.3 pct   ",0
;    .byte   "100.0 pct   ",0



rssiBar:
   ;MOV     0x54,R0
   MOV     0x4d,R7                   
   MOV     0x4e,R5              
   MOV     0x4f,R3              
   MOV     R7,#0x2a ; ABSOLUTE CALLS
   LCALL   0xeecc  ;;                   
   MOV     A,0x4d                   
   ADD     A,#0x20
   MOV     R7,A
   LCALL   0xef7e ;;                     
   MOV     A,0x4d                   
   ADD     A,#0x20
   MOV     R7,A
   LCALL   0xef7e ;;                     
   MOV     R7,#0x2b
   LCALL   0xeecc ;;                     
   MOV     R7,0x4e             
   LCALL   0xef7e ;;                     
   MOV     R7,0x4e             
   LCALL   0xef7e ;;                     
   MOV     R7,#0x2c
   LCALL   0xeecc ;;                     
   CLR     A
   MOV     0x52,A       
LAB_CODE_d475:                   
   MOV     A,0x52       
   CLR     CY
   SUBB    A,0x4f                   
   JNC     LAB_CODE_d495
   MOV     0x53,0x4d
   mov     0x50, #0
LAB_CODE_d47f:                       
   MOV     A,0x53
   cjne    a, #79, stayCurrent
   orl     0x50, #1
stayCurrent:
   cjne    a, 0x51, noBlackout
   orl     0x50, #2
noBlackout:
   CLR     CY
   SUBB    A,#0x80
   JNC     LAB_CODE_d491
   mov     a, 0x50
   jz      isGreen
   dec     a
   jz      isRed
isBlack:
   mov     r7, #0
   mov     r5, #0
   sjmp    drawDot
isGreen:
   mov     r7, #0xf0
   mov     r5, #0x3f
   sjmp    drawDot
isRed:
   mov     r7, #0xff
   mov     r5, #0x08
drawDot:                        ; ABSOLUTE CALLS
   LCALL   0xed07 ;;                     
   INC     0x53                     
   SJMP    LAB_CODE_d47f
LAB_CODE_d491:                       
   INC     0x52         
   SJMP    LAB_CODE_d475
LAB_CODE_d495:                       
   RET
