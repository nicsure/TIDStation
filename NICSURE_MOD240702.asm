
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

.ORG        0x7602                  ; replace original AM indicator
    lcall   myModIndicator

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
.ORG        0x5e15
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

.org        0xef94
feedWatchdog:

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

.org 0xbd93
nop
nop
nop

;.org 0x8a94
;    setb p3.5
;halt:
;sjmp halt

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
.org        0x7df6                  ; prevent ptt-a pulsing when remapped to ptt-b
    .byte   0x00, 0x80

.ORG        0x2a39
    LJMP    fineStepDetour
    NOP
fineStepResume:

.org        0X10AD
    .BYTE   0                       ; OVERFLOW TEST FOR LOGO AREA

.ORG        0xCEE                   ; logo area repurpose

myModIndicator:
    mov     dptr, #0x720
    movx    a, @dptr
    jz      noIndictorOverride
    inc     dptr
    movx    a, @dptr
    mov     r1, a
    inc     dptr
    movx    a, @dptr
    mov     r2, a
noIndictorOverride:
    ljmp    printRegularText

copyX16:
    mov     dpl, r0
    mov     dph, r1
    movx    a, @dptr
    mov     b, a
    inc     dptr
    movx    a, @dptr
    inc     r2
    mov     dpl, r2
    mov     dph, r3
    movx    @dptr, a
    mov     a, b
    dec     r2
    mov     dpl, r2
    movx    @dptr, a
    ret

getStep:
    mov     dptr, #0x14b
    movx    a, @dptr
    anl     a, #0x70
    rr      a
    rr      a
    ;.byte   0x25, 0xe0 ; add a,a
    ;.byte   0x25, 0xe0 ; add a,a
    add     a, #0x50
    mov     dpl, a
    clr     a
    addc    a, #0xbb
    mov     dph, a
    inc     dptr
    inc     dptr
    clr     a
    movc    a, @a+dptr
    mov     r7, a
    inc     dptr
    clr     a
    movc    a, @a+dptr
    mov     r6, a
    ret

addStepScan:
    mov     dptr, #0x793
    sjmp    addStepNow

addStepBase:
    mov     dptr, #0x797

addStepNow:
    movx    a, @dptr
    add     a, r6
    movx    @dptr, a
    dec     dpl
    movx    a, @dptr
    addc    a, r7
    movx    @dptr, a
    dec     dpl
    movx    a, @dptr
    addc    a, #0
    movx    @dptr, a
    dec     dpl
    movx    a, @dptr
    addc    a, #0
    movx    @dptr, a
    ret

subStepScan:
    mov     dptr, #0x793
    sjmp    subStepNow

subStepBase:
    mov     dptr, #0x797

subStepNow:
    movx    a, @dptr
    clr     c
    subb    a, r6
    movx    @dptr, a
    dec     dpl
    movx    a, @dptr
    subb    a, r7
    movx    @dptr, a
    dec     dpl
    movx    a, @dptr
    subb    a, #0
    movx    @dptr, a
    dec     dpl
    movx    a, @dptr
    subb    a, #0
    movx    @dptr, a
    ret

bigChNameHook:
    mov     0x71, #0
    mov     dptr, #0x707
    movx    a, @dptr
    jz      noOffsetIsSet
    rl      a
    mov     b, a
    mov     dph, r2
    mov     dpl, r1
    movx    a, @dptr
    add     a, b
    clr     c
    subb    a, #58
    jc      nineOrLess
    add     a, #48
    mov     0x71, #1
    sjmp    printFreq
nineOrLess:
    add     a, #58
printFreq:
    movx    @dptr, a
noOffsetIsSet:
    lcall   0xa5b7
    mov     a, 0x71
    jz      printSpace
    mov     dptr, #num1
    sjmp    print1
printSpace:
    mov     dptr, #R
print1:
    mov     r2, dph
    mov     r1, dpl
    mov     r3, #0xff
    mov     r5, #1
    lcall   0xa5b7
    ret

enterFreqBlank:
    lcall   0xa5b7
    mov     dptr, #R
    sjmp    print1

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

.ORG        0x29c4                  ; overflow test byte for GMRS data space
    .byte   0

.ORG        0x2400                  ; gmrs channel area repurpose
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
    .BYTE   "SB",0
labelAM:
    .BYTE   "AM",0
labelFM:
    .BYTE   "FM",0
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

indicator:
    .byte   ">",0
menuOptCount:
    .byte   7
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
    .word   menuOptions5
    .byte   2,"5.Batt Display",0
    .word   menuOptions6
    .byte   2,"6.Sigbar Style",0
    .word   menuOptions7
    .byte   5,"7.Freq Adjust ",0
    .word   menuOptions8
    .byte   2,"8.WL Copy Freq",0
    .word   menuOptions9
    .byte   3,"9.PTT Options ",0

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
menuOptions5:
    .byte   "Icon        ",0
    .byte   "Percentage  ",0
menuOptions6:
    .byte   "Solid       ",0
    .byte   "Segmented   ",0
menuOptions7:
    .byte   "Off         ",0
    .byte   "+200 Mhz    ",0
    .byte   "+400 Mhz    ",0
    .byte   "+600 Mhz    ",0
    .byte   "+800 Mhz    ",0
menuOptions8:
    .byte   "Default     ",0
    .byte   "433.250 Mhz ",0
menuOptions9:
    .byte   "Default     ",0
    .byte   "Switch VFOs ",0
    .byte   "Single PTT  ",0

U:
    .byte   "U",0
R:
    .byte   " ",0

vfoKey:
    mov     dptr, #0x4ff
    clr     a
    movx    @dptr, a
    mov     dptr, #0x709
    movx    a, @dptr
    jz      pttOptsDisabled
    dec     a
    jz      checkVfoSw
singlePTT:
    mov     dptr, #0x479
    movx    a, @dptr
    cjne    a, #0x1a, notPTTBPress
    clr     a
    movx    @dptr, a
    sjmp    pttOptsDisabled
notPTTBPress:
    cjne    a, #0x13, pttOptsDisabled
    mov     dptr, #0xb8
    movx    a, @dptr
    jz      pttAisSelected
pttBisSelected:
    mov     a, #0x1a
    sjmp    remapPTTKey
pttAisSelected:
    mov     a, #0x13
remapPTTKey:
    mov     dptr, #0x479
    movx    @dptr, a
pttOptsDisabled:
    mov     dptr, #0x479
    ret
checkVfoSw:
    mov     dptr, #0xb8
    movx    a, @dptr
    mov     b, a
    mov     dptr, #0x479
    movx    a, @dptr
    cjne    a, #0x13, notPTTA
    mov     r0, a
    mov     a, b
    jnz     switchVFO
    sjmp    notPTTB
notPTTA:
    cjne    a, #0x1a, notPTTB
    mov     r0, a
    mov     a, b
    jz      switchVFO
notPTTB:
    mov     dptr, #0x479
    ret
switchVFO:
    mov     dptr, #0x4fe
    mov     a, r0
    movx    @dptr, a
    mov     dptr, #0x479
    mov     a, #0x10
    movx    @dptr, a
    ret

scopeInit:
    mov     dptr, #0x7a0
    mov     a, #63
    movx    @dptr, a

    mov     r7, #0x39 
    lcall   getRegSafe
    mov     dptr, #0x794
    mov     a, r5
    movx    @dptr, a
    inc     dptr
    mov     a, r3
    movx    @dptr, a

    mov     r7, #0x38 
    lcall   getRegSafe
    mov     dptr, #0x796
    mov     a, r5
    movx    @dptr, a
    inc     dptr
    mov     a, r3
    movx    @dptr, a

    mov     r3, #32
middlizeFreq:
    lcall   getStep
    lcall   subStepBase
    djnz    r3, middlizeFreq

    ret

clamp60:
    mov     b, a
    clr     c
    subb    a, #0x58
    jc      dontClamp60
    mov     b, #0x58
dontClamp60:
    ret

invert60:
    mov     a, #0x58
    clr     c
    subb    a, b
    ret

blankBottom:
    mov     0x50, #0
    mov     0x51, #0
    mov     r3, #0x20
    mov     r5, #0x60
    mov     r7, #0
    lcall   fillArea
    ret

scopeKey:
    cjne    a, #0x0e, nextKey1                  ; exit
    ret
nextKey1:
    cjne    a, #0x0c, nextKey2                  ; up
    lcall   addStepBase
nextKey2:
    cjne    a, #0x0d, nextKey3                  ; down
    lcall   subStepBase
nextKey3:
retScopeKey:
    sjmp    noResetFreq

scope:
    acall   blankBottom
drawScope:
    lcall   feedWatchdog
    mov     dptr, #0x7a0
    movx    a, @dptr
    inc     a
    anl     a, #0x3f
    movx    @dptr, a    
    jnz     testKey
resetFreq:
    mov     r1, #7
    mov     r0, #0x94
    mov     r3, #7
    mov     r2, #0x90
    lcall   copyX16
    mov     r0, #0x96
    mov     r2, #0x92
    lcall   copyX16
testKey:
    lcall   origButtonScanner
    mov     dptr, #0x479
    movx    a, @dptr
    jz      noResetFreq
    sjmp    scopeKey
noResetFreq:
    mov     dptr, #0x790
    movx    a, @dptr
    mov     r5, a
    inc     dptr
    movx    a, @dptr
    mov     r3, a
    mov     r7, #0x39
    LCALL   setReg
    mov     dptr, #0x792
    movx    a, @dptr
    mov     r5, a
    inc     dptr
    movx    a, @dptr
    mov     r3, a
    mov     r7, #0x38
    LCALL   setReg
    MOV     R7, #0x30
    MOV     R5, #0x00
    MOV     R3, #0x00
    LCALL   setReg
    MOV     R7, #0x30
    MOV     R5, #0xBF
    MOV     R3, #0xF1
    LCALL   setReg      
    mov     r0, #0xa0
fDelay2:
    mov     r1, #0xff
fDelay1:
    djnz    r1, fDelay1
    djnz    r0, fDelay2
    MOV     R7, #0x65               ; register 65 is noise level
    LCALL   getReg                  ; get the register value
    MOV     DPTR, #0x2F6            ; only need low byte
    MOVX    A, @DPTR                ; move noise byte into A
    ANL     A, #0x7F                ; only bits 0-6 is used
    acall   clamp60
    acall   invert60
    rl      a
    acall   clamp60
    acall   invert60

signalCalced:
    push    acc
    mov     0x50,#0xff
    mov     0x51,#0xff
    mov     0x4e, a
    mov     b, a
    clr     c
    subb    a, #0x40
    jnc     noDecentSig
    mov     0x51,#0x8f
noDecentSig:    
    mov     a, b
    mov     0x4f, #0x60
    mov     dptr, #0x7a0
    movx    a, @dptr
    cjne    a, #32, notMiddle
    mov     0x50,#0xfa
    mov     0x51,#0xaf
notMiddle:
    rl      a
    mov     0x4d, a
    inc     a
    inc     a
    mov     0x52, a
    lcall   rectangle

    pop     acc
    mov     0x50,#0
    mov     0x51,#0
    mov     0x4e, 0
    mov     0x4f, a
    mov     dptr, #0x7a0
    movx    a, @dptr
    rl      a
    mov     0x4d, a
    inc     a
    inc     a
    mov     0x52, a
    lcall   rectangle   ;rssibar
    lcall   getStep
    lcall   addStepScan
    ajmp    drawScope

.org 0xe42b
ljmp 0


.org 0x8ab6
    lcall    battTest

.org 0xdf28
    reti

.org 0x7163
    reti

;.ORG        0xd099                  ; hook to capture battery level
;    LCALL   captureBattLevel
;    NOP

;.ORG        0xddb6                  ; routine that draws the battery icon
;    lcall   battLevel

.org        0xf7ff                  ; end byte for overlap detection
    .byte   0

.org        0x30b8                  ; vhf/uhf transition 230 MHz
    mov     r7, #0x00
    mov     r6, #0xf4
    mov     r5, #0x5e
    mov     r4, #0x01
.org        0x6a22
    mov     r7, #0x00
    mov     r6, #0xf4
    mov     r5, #0x5e
    mov     r4, #0x01
.org        0x79b3
    mov     r7, #0x00
    mov     r6, #0xf4
    mov     r5, #0x5e
    mov     r4, #0x01
.org        0x79cc
    mov     r6, #0xf4
    mov     r5, #0x5e
    mov     r4, #0x01

.org        0x54ea
    lcall   hotspotPowerHook

.org        0x7ca5
    .byte   0x00, 0x80
.org        0x7cb8
    .byte   0x00, 0x80              ; disable scramble function
.org        0x80dc
    .byte   0x00, 0x80
.org        0x80ef
    .byte   0x00, 0x80

.org        0x676
    .byte   "ULow Power"            ; change scramble menu

.org        0x74b0                  ; set U for H/L power indicator
    LCALL   uPowerIndicatorHook

.org        0x6c8d                  ; wireless mode detection
    lcall   wirelessFlag

.org        0x86a6                  ; big channel name mod
    lcall   bigChNameHook
.org        0x86a5
    .byte   0xd
.org        0x86b9
    .byte   0x61
.org        0x42ad
    .byte   0xd
.org        0x42c1
    .byte   0x61
.org        0x42ae
    lcall   enterFreqBlank

.org        0x834a
    nop
    nop
    nop
.org        0x8358
    .byte   0x2d
.org        0x836d
    .byte   0x2d
.org        0x8363
    .byte   0x18

.org        0x836e
    ljmp    txMeterHook
txMeterResume:

.ORG        0xf03b                  ; start of mod code

; xmem 0x2c name of channel on a
; xmem 0x34 name of channel on b
; xmem 0xb8 selected vfo 0=a 1=b
; xmem 0x145 bits 0,1 vfo-A mode
; xmem 0x146 bits 0,1 vfo-b mode


txMeterHook:
    push    0x71
    mov     0x51, 0x3e
    mov     r3, #6
    mov     r5, #17
    mov     r7, #0
    mov     0x71, #1
    lcall   rssiBar
    pop     0x71
    ret

wirelessFlag:
    mov     dptr, #0x779
    mov     a, #1
    movx    @dptr, a
    mov     dptr, #0x708
    movx    a, @dptr
    jz      noWLOverride
    mov     dptr, #0x778
    mov     a, #1
    movx    @dptr, a
noWLOverride:
    mov     dptr, #0x3dd
    ret

hotspotPowerHook:
    mov     r0, a
    mov     dptr, #0x2c2
    movx    a, @dptr
    jb      acc.6, ULP
    mov     dptr, #0x779
    movx    a, @dptr
    jnz     ULP
    mov     a, r0
    sjmp    hotspotResume
ULP:
    clr     a
hotspotResume:
    mov     0x34, a
    ljmp    0x69e9

uPowerIndicatorHook:
    jnb     acc.6, noLowPowerIndicator
    mov     dptr, #U
    mov     r2, dph
    mov     r1, dpl
noLowPowerIndicator:
    mov     0x53, 0x4e
    ret

battLevel:
    mov     dptr, #0x705            ; batt setting
    movx    a, @dptr
    jnz     percentage
    ljmp    0x99a5                  ; relocate
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
    mov     0x54, #0xf5
    mov     0x55, #0x5f
    sjmp    battColSet
redBatt:
    mov     0x54, #0xff
    mov     0x55, #0x55
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
    mov     r5, #102
    acall   printDigit
    pop     acc
    mov     r5, #110
    acall   printDigit
    mov     a, #10
    mov     r5, #118
    acall   printDigit
    ret

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
    LCALL   printRegularText
    ret

battTest:
    LCALL   0xdb26 ; init Serial
    .byte   0xc2, 0xaf ; clrb ea

    .byte   0xd2, 0xb3 ; setb int1

battLoop1:
    lcall   0xef94

    .byte   0x43, 0xdf, 0x02   ;ORL     PCON1,#0x2                         = ??

    mov     r7, #0x91
    lcall   sendSerialByte

    mov     a, 0xdd
    mov     r7, a
    lcall   sendSerialByte

    mov     a, 0xdc
    mov     r7, a
    lcall   sendSerialByte

    sjmp    battLoop1



;captureBattLevel:
;    .byte   0xe5, 0x0e              ; mov a, bank1_r6
;    MOV     DPTR, #0xA06
;    MOVX    @DPTR, A
;    .byte   0xe5, 0x0f              ; mov a, bank1_r7
;    MOV     DPTR, #0xA07
;    MOVX    @DPTR, A
;    SUBB    A, #0x22
;    ret

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
    mov     0x44, #0xef
    mov     0x45, #0xef
    LCALL   printSmall

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
    jnz     abortRssiBar
    sjmp    hooksOkay
abortRssiBar:
    ;MOV     DPTR, #0x2F5            ; move the high byte address into DPTR to put state back to how it should be
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
    push    0x71
    mov     0x71, #0
    lcall   rssiBar
    pop     0x71

exitBarDraw:
    ;MOV     DPTR, #0x2F5            ; move the high byte address into DPTR to put state back to how it should be
    LJMP    resumeRssiDetour        ; jump back to original function

setRegDetour:
    mov     dptr, #0x707
    movx    a, @dptr
    jz      freqAdjustNotSet
    sjmp    adjustmentSet
freqAdjustNotSet:
    mov     dptr, #0x778
    movx    a, @dptr
    jnz     adjustmentSet
    ajmp    skipAdjust
adjustmentSet:
    cjne    r7, #0x39, snot39
    mov     0x77, r5
    mov     0x76, r3
    sjmp    nowSetFreq
snot39:
    cjne    r7, #0x38, skipAdjust
    mov     0x75, r5
    mov     0x74, r3
nowSetFreq:
    mov     dptr, #0x778
    movx    a, @dptr
    jnz     add9
    mov     dptr, #0x707
    movx    a, @dptr
    dec     a
    jz      add200
    dec     a
    jz      add400
    dec     a
    jz      add600
add800:
    mov     0x7c, #4
    mov     0x7b, #0xc4
    mov     0x7a, #0xb4
    sjmp    adjustFreq
add600:
    mov     0x7c, #3
    mov     0x7b, #0x93
    mov     0x7a, #0x87
    sjmp    adjustFreq
add200:
    mov     0x7c, #1
    mov     0x7b, #0x31
    mov     0x7a, #0x2d
    sjmp    adjustFreq
add9:
    mov     0x7c, #0
    mov     0x7b, #0x0e
    mov     0x7a, #0x27
    sjmp    adjustFreq
add400:
    mov     0x7c, #2
    mov     0x7b, #0x62
    mov     0x7a, #0x5a
adjustFreq:
    mov     r0, 0x75
    mov     r1, 0x76
    mov     r2, 0x77

    clr     c
    mov     a, r0
    addc    a, 0x7a
    mov     r0, a
    mov     a, r1
    addc    a, 0x7b
    mov     r1, a
    mov     a, r2
    addc    a, 0x7c
    mov     r2, a
    MOV     0x4D, #0x38
    MOV     0x4E, r0
    mov     r3, 0x74
    lcall   setRegResume
    mov     r7, #0x39
    mov     a, r2
    mov     r5, a
    mov     a, r1
    mov     r3, a
    sjmp    skipAdjust

exitSetRegTop:
    LJMP    exitSetReg

skipAdjust:
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
    mov     dptr, #0x720
    mov     a, #1
    movx    @dptr, a
    inc     dptr
    mov     a, r1
    movx    @dptr, a
    inc     dptr
    mov     a, r2
    movx    @dptr, a
    
    ;MOV     R3, #0xFF
    ;LCALL   0x4a96                      ; relocate
    ;MOV     R5, #0x52
    ;MOV     0x53, #0x1A
    ;LCALL   printRegularText
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
    mov     dptr, #0x4fe
    movx    a, @dptr
    jz      justResumeBS
    clr     a
    movx    @dptr, a
    mov     dptr, #0x479
    movx    @dptr, a
    ret
justResumeBS:
    acall   origButtonScanner
    mov     dptr, #0x479
    jnz     dontReset777
    mov     dptr, #0x777
    clr     a
    movx    @dptr, a
dontReset777:
    ret

origButtonScanner:
    anl     p0, #0xf0
    ljmp    buttonScannerResume

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
    cjne    a, #0x13, tryFlashlight
    sjmp    customMenuRequested
tryFlashlight:
    cjne    a, #0x12, resumeMenu
    lcall   scopeInit
    lcall   scope
    mov     dptr, #0x479
    mov     a, #0xe
    movx    @dptr, a
    sjmp    resumeMenu
customMenuRequested:
    mov     dptr, #0x4ff
    mov     a, #1
    movx    @dptr, a
    LCALL   displayMenu
abortMenu:
    pop     acc
    pop     acc
resumeMenu:
    ret
exMenu:
    mov     dptr, #0x777
    movx    a, @dptr
    jz      noKeyRepeat
    sjmp    abortMenu
noKeyRepeat:
    mov     a, #1
    movx    @dptr, a
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
    cjne    a, #0x0a, notTooMuch  ; total menu check count
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

    clr     c
    subb    a, r2
    jc      notInvalidSelection
    clr     a
    mov     r5, a
    movx    @dptr, a
notInvalidSelection:

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

setStartPos:
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
   ret

rssiBar:
   ;MOV     0x54,R0
   MOV     0x4d,R7                   
   MOV     0x4e,R5              
   MOV     0x4f,R3              
   acall   setStartPos          
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
   mov     r0, 0x50
   cjne    r0, #3, calcColour
   sjmp    noBlackout
calcColour:                 
   cjne    a, #79, stayCurrent
   mov     0x50, #1
stayCurrent:
   cjne    a, #44, stayCurrent2
   mov     0x50, #2
stayCurrent2:
   mov     b, 0x71
   jnb     b.0, stayCurrent3
   mov     0x50, #1
stayCurrent3:
   cjne    a, 0x51, noBlackout
   mov     0x50, #3
noBlackout:
   CLR     CY
   SUBB    A,#0x80
   JNC     LAB_CODE_d491
   mov     dptr, #0x706
   movx    a, @dptr
   jz      solidBar
   MOV     A,0x53
   jb      acc.1, isBlack 
solidBar:
   mov     a, 0x71
   mov     a, 0x50
   jz      isGreen
   dec     a
   jz      isRed
   dec     a
   jz      isYellow
isBlack:
   mov     r7, #0
   mov     r5, #0
   sjmp    drawDot
isGreen:
   mov     r7, #0xf0
   mov     r5, #0x3f
   sjmp    drawDot
isYellow:
   mov     r7, #0xff
   mov     r5, #0x0f
   sjmp    drawDot
isRed:
   mov     r7, #0xff
   mov     r5, #0x88
drawDot:                        ; ABSOLUTE CALLS
   LCALL   0xed07 ;;                     
   INC     0x53                     
   SJMP    LAB_CODE_d47f
LAB_CODE_d491:   
   INC     0x52         
   SJMP    LAB_CODE_d475
LAB_CODE_d495:                       
   RET

rectangle:
nextLine:
    acall   setStartPos                          
LCd4db:
    mov     a, 0x4e
    clr     c
    subb    a, 0x4f
    jnc     LCd4fb
    MOV     0x53,0x4d       
LCd4e5:                      
    MOV     A,0x53       
    CLR     CY
    subb    a, 0x52
    JNC     LCd4f7
    MOV     r5,0x51   
    MOV     r7,0x50   
    LCALL   0xed07                  
    INC     0x53           
    SJMP    LCd4e5
LCd4f7:     
    inc     0x4e
    sjmp    nextLine
LCd4fb:
    RET
