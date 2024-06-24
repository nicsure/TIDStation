.ORG        0x65a
    .BYTE   "AM/USB OVR"
.ORG        0x9d5
    .BYTE   "AM "
.ORG        0x9df
    .BYTE   "USB "

.ORG        0x6901                  ; mic gain fix
    .BYTE   0x44, 0x47, 0x4A, 0x4D, 0x50, 0x53, 0x56, 0x59, 0x5C, 0x5F
.ORG        0x7F71
    .BYTE   0xE9
.ORG        0x7F73
    .BYTE   0x7D

.ORG        0xC80F                  ; BK4819 Read Register function
getReg:

.ORG        0xd443                  ; fill screen area
fillArea:

.ORG        0xAE67                  ; print regular size text?  r1=low byte code mem, r2=high byte code mem
printRegularText:

.ORG        0xCCAE                  ; print small size tex? r1=low byte code mem, r2=high byte code mem
    ;LCALL   printSmallText
    ;NOP
printSmall:

.ORG        0xE647                  ; BK4819 Set Register function & Hook (4 bytes)
setReg:                             ; Original code: 8f 4d      MOV     0x4d,r7
    LCALL   setRegDetour            ;                8d 4e      MOV     0x4e,r5
    NOP
setRegResume:

.ORG        0x75a4                  ; remove original AM indicator
    NOP
    NOP
    NOP

.ORG        0x82c5                  ; remove "POWER" text
    LCALL   plus40

.ORG        0x82d5                  ; remove rssi "bracket"
    LJMP    rssiBracket

.ORG        0x82ae                  ; 9 on the meter
    LCALL   green9

.ORG        0x8264                  ; vertical location of s-meter numbers sync mode
    .BYTE   0x05
.ORG        0x826e                  ; vertical location of s-meter numbers non sync mode
    .BYTE   0x05

.ORG        0x5DDA                  ; hook signal level poll function (3 bytes)
    LJMP    rssiDetour              ; Original code: 90 02 f5   MOV     DPTR,#0x2f5
.ORG        0x5dea
resumeRssiDetour:
.ORG        0x5e40
rssiNoSignal:

.ORG        0xEAB8                  ; Serial send byte function, byte to send in R7
sendSerialByte:

.ORG        0x0122
    .BYTE   0x31, 0, 0x33, 0, 0x35, 0, 0x37, 0, 0x39, 0, 0x34, 0x30, 0x2b, 0
.ORG        0x8276
    mov     r5, #0x04
.ORG        0x8282
    mov     r5, #0x16
.ORG        0x8290
    mov     r5, #0x28
.ORG        0x829e
    mov     r5, #0x3a
.ORG        0x82ac
    mov     r5, #0x4c
.ORG        0x82c3
    mov     r5, #0x5f

.ORG        0x2d12
    .byte   0x0a
.ORG        0x2d14
    .byte   0xd1

.ORG        0x4abe
    .byte   0x0a
.ORG        0x4ac0
    .byte   0xd1

.ORG        0x4f3a
    .byte   0x0a
.ORG        0x4f3c
    .byte   0xd1

.ORG        0x6cfc
    .byte   0x0a
.ORG        0x6cfe
    .byte   0xd1

.ORG        0x7931
    .byte   0x0a
.ORG        0x7933
    .byte   0xd1


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


.ORG        0xd0db                  ; menu mode key press
    LCALL   menuKey


.ORG        0xEFE0                  ; start of mod code

rssiBracket:
    mov     0x50, #0xff
    mov     0x51, #0xff
    mov     r3, #1
    mov     r5, #15
    mov     r7, #0
    lcall   fillArea
    ret

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

noSignalDetected:
    mov     0x50, #0
    mov     0x51, #0
    mov     r3, #8
    mov     r5, #16
    mov     r7, #0
    lcall   fillArea
skipRssi:
    LJMP    rssiNoSignal
rssiDetour:
    MOV     A, 0x25
    JNB     ACC.1, noSignalDetected
    MOV     A, 0x27
    JNB     ACC.3, skipRssi
    CLR     ACC.3
    MOV     0x27, A
    MOV     R7, #0x65
    LCALL   getReg
    MOV     DPTR, #0x2F6
    MOVX    A, @DPTR
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
    LCALL   getReg
    POP     ACC
    MOV     R0, A
    MOV     DPTR, #0x2F5            ; get address of the rssi high byte
    MOVX    A, @DPTR                ; move high byte into A
    ANL     A, #0x01                ; we only need bit 0
    mov     b, a
    MOV     DPTR, #0x2F6            ; get the address in extmem of the rssi low byte
    MOVX    A, @DPTR                ; put the low byte into A    
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
    mov     0x51, a
    mov     r3, #6
    mov     r5, #17
    mov     r7, #0
    lcall   rssiBar

exitBarDraw:
    MOV     DPTR, #0x2F5            ; move the high byte address into DPTR to put state back to how it should be
    LJMP    resumeRssiDetour        ; jump back to original function


;printRegularText:
;    mov     0x52,R5
;    mov     0x4f,R3
;    ret

;printSmallText:
;    mov     0x40,R2
;    mov     0x3f,R3
;    ret

;fillArea:
;    mov     0x4d, r7
;    mov     0x4e, r5
;    ret


setRegDetour:
    MOV     0x4D, R7                ; perform one of the instructions replaced by the hook
    CJNE    R7, #0x47, not47        ; check to see if we're setting reg 47 (modulation)
    SJMP    reg47
not47:
    CJNE    R7, #0x3D, not3D        ; check to see if we're setting reg 3D
    SJMP    reg3D
not3D:
    CJNE    R7, #0x73, not73        ; check to see if we're setting reg 73
    SJMP    reg73
not73:
    SJMP    exitSetReg

reg73:
    MOV     DPTR, #0x146            ; bits 6,7 of 0x146 is PO menu setting
    MOVX    A, @DPTR                ; get this byte
    ANL     A, #0xc0                ; we only need bits 6,7
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
    MOV     DPTR, #0x146            ; bits 6,7 of 0x146 is PO menu setting
    MOVX    A, @DPTR                ; get byte from ext mem
    ANL     A, #0xc0                ; only need bits 6,7
    CJNE    A, #0x80, set2AAB
    CLR     A                       ; set A to 0
    MOV     R3, A                   ; for USB we need reg 3D to be 0, so set high (R5) and low (R3) bytes to 0 (A)
    MOV     R5, A
    SJMP    exitSetReg              ; resume
set2AAB:
    MOV     R5, #0x2A
    MOV     R3, #0xAB
    SJMP    exitSetReg              ; resume

reg47:
    MOV     DPTR, #0x146            ; bits 6,7 of 0x146 is PO menu setting
    MOVX    A, @DPTR                ; grab this byte from extmem
    ANL     A, #0xc0                ; we only want bits 6,7
    JNZ     overrideModul           ; if any bits are set A will be non zero, this means we need to override the moduation
    MOV     0x4E, R5
    LCALL   setRegResume
    MOV     R5, #0x2A
    MOV     R3, #0xAB
    MOV     R7, #0x3D
    MOV     0x4D, R7
    MOV     0x4E, R5
    LCALL   setRegResume

    MOV     DPTR, #0x2F5
    MOVX    A, @DPTR
    PUSH    ACC
    inc     dptr
    MOVX    A, @DPTR
    PUSH    ACC

    MOV     R7, #0x73
    LCALL   getReg
    MOV     DPTR, #0x2F5
    MOVX    A, @DPTR
    MOV     R5, A
    INC     DPTR
    MOVX    A, @DPTR
    ANL     A, #0xEF
    MOV     R3, A
    MOV     R7, #0x73
    MOV     0x4D, R7
    MOV     0x4E, R5

    MOV     DPTR, #0x2F6
    POP     ACC
    MOVX    @DPTR, A
    MOV     DPTR, #0x2F5
    POP     ACC
    MOVX    @DPTR, A

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
    CJNE    A, #0x40, isUSB         ; 0x40 is the code for AM
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
    CJNE    A, #0x80, notUSB
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

    MOV     DPTR, #0x2F5
    MOVX    A, @DPTR
    PUSH    ACC
    inc     dptr
    MOVX    A, @DPTR
    PUSH    ACC

    MOV     R7, #0x73
    LCALL   getReg
    MOV     DPTR, #0x2F5
    MOVX    A, @DPTR
    MOV     R5, A
    INC     DPTR
    MOVX    A, @DPTR
    ORL     A, #0x10
set73Register:
    MOV     R3, A
    MOV     R7, #0x73
    MOV     0x4D, R7
    MOV     0x4E, R5 

    MOV     DPTR, #0x2F6
    POP     ACC
    MOVX    @DPTR, A
    MOV     DPTR, #0x2F5
    POP     ACC
    MOVX    @DPTR, A

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
    LCALL   getReg
    MOV     DPTR, #0x2F5
    MOVX    A, @DPTR
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
    LCALL   0x4782
    MOV     R5, #0x52
    MOV     0x53, #0x1A
    LCALL   printRegularText
setModLabelExit:
    RET

labelUSB:
    .BYTE   "USB",0
labelAM:
    .BYTE   "AM ",0
labelFM:
    .BYTE   "FM ",0


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


menuKey:
    mov     dptr, #0x4ff
    movx    a, @dptr
    jz      normalMenu
    dec     a
    jz      exMenu
    mov     dptr, #0x478
    ret    
normalMenu:
    mov     dptr, #0x478
    movx    a, @dptr
    cjne    a, #0x13, resumeMenu
    mov     dptr, #0x4ff
    mov     a, #1
    movx    @dptr, a
    mov     dptr, #0xe10
    movx    a, @dptr
    LCALL   displayMenu
    pop     acc
    pop     acc
resumeMenu:
    ret
exMenu:
    mov     dptr, #0x478
    movx    a, @dptr
    cjne    a, #0x0e, processKey
    mov     dptr, #0x4ff
    clr     a
    movx    @dptr, a
    mov     dptr, #0x478
    ret
processKey:
    cjne    a, #0xc, notUp
    mov     a, #0xff
    acall   upDown
    sjmp    exitExMenu
notUp:
    cjne    a, #0xd, notDown
    mov     a, #1
    acall   upDown
    sjmp    exitExMenu
notDown:
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
    mov     dptr, #0xe06
    movx    a, @dptr
    clr     c
    addc    a, b
    cjne    a, #0xff, notMinusOne
    mov     a, r1
    dec     a
notMinusOne:
    mov     b, a
    clr     c
    subb    a, r1
    jnz     noOverflow
    clr     b
noOverFlow:
    mov     a, b                   ; new option
    mov     dph, #0x0f
    mov     dpl, r2
    movx    @dptr, a
    mov     a, r2
    acall   displayMenu
    ret



displayMenu:
    mov     0x50, #0
    mov     0x51, #0
    mov     r3, #140
    mov     r5, #20
    mov     r7, #0
    lcall   fillArea
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
    mov     dph, #0xf
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
    jz      noMoreOptions
    dec     a
    movx    @dptr, a
    mov     dptr, #0xe05
    movx    a, @dptr
    push    acc
    dec     a
    movx    @dptr, a
    
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
    jnz     optionsLoop
    mov     r3, #0xff
    mov     0x54, #0xff
    mov     0x55, #0xff
    mov     r5, #2
    mov     dptr, #indicator
    mov     r2, dph
    mov     r1, dpl
    LCALL   printRegularText
    sjmp    optionsLoop




noMoreOptions:

    ;MOV     R2, DPH
    ;MOV     R1, DPL
    ;MOV     R3, #0xFF
    ;LCALL   0x4782
    ;MOV     R5, #0x52
    ;MOV     0x53, #0x1A
    ;LCALL   printRegularText



    ret



indicator:
    .byte   ">",0
menuTitles:
    .word   menuOptions0
    .byte   3,"0. AM/USB Ovr ",0
    .word   menuOptions1
    .byte   2,"1. Kill/Stun  ",0
menuOptions0:
    .byte   "OFF         ",0
    .byte   "AM          ",0
    .byte   "USB         ",0
menuOptions1:
    .byte   "Enabled     ",0
    .byte   "Disabled    ",0



rssiBar:
   ;MOV     0x54,R0
   MOV     0x4d,R7                   
   MOV     0x4e,R5              
   MOV     0x4f,R3              
   MOV     R7,#0x2a
   LCALL   0xee70                     
   MOV     A,0x4d                   
   ADD     A,#0x20
   MOV     R7,A
   LCALL   0xef22                     
   MOV     A,0x4d                   
   ADD     A,#0x20
   MOV     R7,A
   LCALL   0xef22                     
   MOV     R7,#0x2b
   LCALL   0xee70                     
   MOV     R7,0x4e             
   LCALL   0xef22                     
   MOV     R7,0x4e             
   LCALL   0xef22                     
   MOV     R7,#0x2c
   LCALL   0xee70                     
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
drawDot:
   LCALL   0xecab                     
   INC     0x53                     
   SJMP    LAB_CODE_d47f
LAB_CODE_d491:                       
   INC     0x52         
   SJMP    LAB_CODE_d475
LAB_CODE_d495:                       
   RET
