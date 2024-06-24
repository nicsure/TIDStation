
.ORG        0
RESET:

.ORG        0x6901                  ; mic gain fix
    .BYTE   0x44, 0x47, 0x4A, 0x4D, 0x50, 0x53, 0x56, 0x59, 0x5C, 0x5F
.ORG        0x7F71
    .BYTE   0xE9
.ORG        0x7F73
    .BYTE   0x7D

.ORG        0x6af1
NOP
NOP
NOP
NOP
NOP

.ORG        0x7c43
test2:
.ORG        0x7c3b
    SJMP    test2                   ; prevents PTT pulsing, needs proper integration
.ORG        0x7c32
    SJMP    test2

.ORG        0x7ceb                  ; brings radio out of sleep/low power mode
wakeUp:

.ORG        0x5DE7                  ; hook signal level poll function (3 bytes)
    LJMP    rssiDetour              ; Original code: 90 02 f5   MOV     DPTR,#0x2f5
resumeRssiDetour:

.ORG        0x6DCA                  ; function that scans the keypad
    LJMP    keyPadMonitor
keyPadResume:

.ORG        0xA768                  ; first packet 57 processor
    LCALL   process57               ; original code : LCALL 0xE909
.ORG        0xB385                  ; second packet 57 processor
    LCALL   process57               ; original code : LCALL 0xE909

.ORG        0xC06E                  ; first packet 50 processor
    LCALL   process50               ; Original code: 90 04 7D   MOV     DPTR,#0x47D
.ORG        0xC478                  ; second packet 50 processor 
    LCALL   process50               ; Original code: 90 04 7D   MOV     DPTR,#0x47D

.ORG        0xC80F                  ; BK4819 Read Register function
getReg:

.ORG        0xE647                  ; BK4819 Set Register function & Hook (4 bytes)
setReg:                             ; Original code: 8f 4d      MOV     0x4d,r7
    LCALL   setRegDetour            ;                8d 4e      MOV     0x4e,r5
    NOP
setRegResume:

.ORG        0xE91D                  ; checksum calculator, returns CS in R7
calcChecksum:

.ORG        0xEAB8                  ; Serial send byte function, byte to send in R7
sendSerialByte:

.ORG        0xEFE0                  ; end of original firmware, start of patch code

keyPadMonitor:
    ANL     P0, #0xF0               ; execute replaced code from hooking
    LCALL   keyPadResume            ; now actually run the function in its entirety
    MOV     DPTR, #0x4FE            ; 4FE contains the byte of the key we need to simulate being pressed
    MOVX    A, @DPTR                ; get the byte into A
    MOV     DPTR, #0x478            ; 478 is set by the original function as the key currently being pressed
    JZ      endSimKey               ; if A is 0, this means we're not simulating any key press so just finish
    MOVX    @DPTR, A                ; replace 478 with the key we want to simulate
    MOV     A, 0xEA                 ; here's we're getting the serial control register for the USB-C port
    JNB     ACC.0, endSimKey        ; test to see if any data is available, if not just finish
    ANL     0xEA, #0xFE             ; if data is available this is an indication that the pressed key (PTT) has been released, so clear the RI flag
    MOV     R7, #0x07               ; 7 is the ack byte for custom packets
    LCALL   sendSerialByte          ; send an ack
    CLR     A                       ; set A to 0
    MOV     DPTR, #0x4FE            ; requested simulated key address
    MOVX    @DPTR, A                ; clear this
    MOV     DPTR, #0x478            ; actual key pressed address
    MOVX    @DPTR, A                ; clear this also
endSimKey:
    MOVX    A, @DPTR                ; get the key currently being pressed
    CJNE    A, #0x13, notKey13      ; if it's not 13 (vfoa ptt) move to check next ptt button
    SJMP    txMode                  ; we're transmitting
notKey13:
    CJNE    A, #0x1A, rxMode        ; if it's not 1A either (vfob ptt) then we're not transmitting
    SJMP    txMode                  ; we're transmitting
rxMode:                             ; NO TRANSMIT
    MOV     DPTR, #0x4FC            ; 4fc is a flag byte to tell us what tx/rx state we were previously in
    MOVX    A, @DPTR                ; get this state 1=TX 0=RX
    JZ      simKeyRet2              ; if it's 0, we were already in RX state so don't do anything else
    CLR     A                       ; set A to 0
    MOVX    @DPTR, A                ; set the state to 0
    MOV     R7, #0xF0               ; 0xF0 is a single byte packet id to tell the host we've switched to RX mode
    LCALL   sendSerialByte          ; send F0 to the host.
    RET
txMode:                             ; TRANSMIT
    MOV     DPTR, #0x4FC            ; 4fc is a flag byte to tell us what tx/rx state we were previously in
    MOVX    A, @DPTR                ; get this state 1=TX 0=RX
    JNZ     simKeyRet2              ; if it's NOT 0, we were already in TX state so don't do anything else
    INC     A                       ; set A to 1 (as it must be 0 here)
    MOVX    @DPTR, A                ; set the state to 1
    MOV     R7, #0xF1               ; 0xF1 is a single byte packet id to tell the host we've switched to TX mode
    LCALL   sendSerialByte          ; send F1 to the host
simKeyRet2:
    RET

process50:                          ; intercept hook for the reception of initial packets with ID 0x50
    MOV     DPTR, #0x47D            ; the byte after the first 0x50 (also executes replaced hook instruction)
    MOVX    A, @DPTR                ; move it into A
    CJNE    A, #0x56, custom50      ; for a legit 0x50 packet, this byte should be 56
    RET                             ; if it is 56, then just RET to let the firmware handle the packet normally
custom50:
    POP     DPH                     ; pop the return address so a ret will return 2 layers back
    POP     DPL                     ; effectively returning from the original function instead of back to the instruction after the hooked LCALL
    JZ      simulateKey             ; if a is 0, this is the packet's SUB-ID for simulating a key press
                                    ; further mods should use subsequent IDs 0x01, 0x02 etc.. then a simple 'DEC A' and 'JZ functionLabel' can be appended here to initiate calls to handlers
    DEC     A                       ; sub 1 from A, A will be zero if the SUB-ID was 1
    JZ      readExtMem              ; 1 is peek ext mem
    DEC     A                       ; sub 1 from A, A will be zero if the SUB-ID was 2
    JZ      writeExtMem             ; 2 is poke ext mem
    DEC     A                       ; sub 1 from A, A will be zero if the SUB-ID was 3
    JZ      allExtMem               ; 3 is read all ext mem
    RET                             ; otherwise just return, the firmware will then ignore this packet
allExtMem:
    MOV     R7, #0x9A
    LCALL   sendSerialByte
    MOV     DPTR, #0x00
raLoop:
    MOVX    A, @DPTR
    MOV     R7, A
    LCALL   sendSerialByte
    INC     DPTR
    MOV     A, DPH
    CJNE    A, #0x05, raLoop    
    RET
readExtMem:
    MOV     R7, #0x99               ; 0x99 is the debug packet id, it is followed by a single byte
    LCALL   sendSerialByte          ; send packet id
    MOV     DPTR, #0x47E            ; byte in request packet data is the extmem peek address hi byte
    MOVX    A, @DPTR                ; get the hi byte
    MOV     B, A                    ; save it in B
    MOV     DPTR, #0x47F            ; byte in request packet data is the extmem peek address lo byte
    MOVX    A, @DPTR                ; get the lo byte
    MOV     DPH, B                  ; set the DPTR high byte with B we saved earlier
    MOV     DPL, A                  ; set the DPTR low byte with A we just fetched
    MOVX    A, @DPTR                ; get the extmem value we want
    MOV     R7, A                   ; move it to R7 for serial transmission
    LCALL   sendSerialByte          ; send value to host
    RET
writeExtMem:
    MOV     DPTR, #0x47E            ; byte in request packet data is the extmem poke address hi byte
    MOVX    A, @DPTR                ; get the hi address byte
    MOV     B, A                    ; save it in B
    MOV     DPTR, #0x47F            ; byte in request packet data is the extmem poke address lo byte
    MOVX    A, @DPTR                ; get the lo address byte
    MOV     R0, A                   ; save it in R0
    MOV     DPTR, #0x480            ; byte in request packet data is the extmem poke value
    MOVX    A, @DPTR                ; get the poke value
    MOV     DPH, B                  ; set the DPTR high byte with B we saved earlier
    MOV     DPL, R0                 ; set the DPTR low byte with R0 we saved earlier
    MOVX    @DPTR, A                ; poke the value into extmem
    MOV     R7, #0x07               ; 0x07 is a single byte ack packet ID
    LCALL   sendSerialByte          ; send the ack
    RET
simulateKey:
    MOV     DPTR, #0x47E            ; 47e is the 3rd byte of the packet and contains the key id we want to simulate
    MOVX    A, @DPTR                ; get this key id into A
    MOV     DPTR, #0x4FE            ; 4fe is the address used to request a simulated key press
    MOVX    @DPTR, A                ; store the key code into this address
    MOV     R7, #0x07               ; 7 is custom ack
    LCALL   sendSerialByte          ; send ack
    RET

process57:
    LCALL   calcChecksum            ; get calculated checksum of 57 packet in R7 (replaces original code)
    MOV     DPTR, #0x4A0            ; address of checksum in sent packet
    MOVX    A, @DPTR                ; get proposed checksum of 57 packet
    XRL     A, R7                   ; XOR them together, result should be 0x00 if CS is correct or 0xff for a custom packet
    INC     A                       ; We're interested in packets with an incorrect checksum (A=0xFF here) so add one to A to make it 0 in this case
    JZ      custom57                ; If we get a match then this is a custom packet so jump to the custom handler
    RET                             ; we can just return to the original function here so it can do what it normally does with 57 packets

custom57:                       
    POP     DPH                     ; the original function just RETs for a bad checksum, but we're one call level deeper here
    POP     DPL                     ; so we just pop two values out of the stack, then a RET will return from the original function as well as this one
    MOV     DPTR, #0x480            ; first byte of data block
    MOVX    A, @DPTR                ; move it into A, this byte is the custom packet id
    JZ      setModulationP          ; 0x00 is id of modulation override request
    DEC     A                       ; sub 1 from a to check next id of 0x01
    JZ      scopeModP               ; 0x01 is id of the spectrum scope request
    DEC     A                       ; sub 1 from a to check next id of 0x02
    JZ      shiftModP               ; 0x02 is id of the frequency shifter
    DEC     A                       ; no valid custom id, so return back to the firmware that will just ignore this packet
    JZ      freqScan                ; 0x03 is id of the jet frequency scanner
    RET                             ; no valid custom id, so return back to the firmware that will just ignore this packet

setModulationP:
    AJMP    setModulation
scopeModP:
    AJMP    scopeMod
shiftModP:
    AJMP    shiftMod

freqScan:
    LCALL   wakeUp
    MOV     R7, #0xB5
    LCALL   sendSerialByte
    MOV     DPTR, #0x480
freqScanLoop:
    INC     DPTR
    MOV     A, DPL
    CJNE    A, #0x9D, scanContinue
    RET
scanContinue:
    MOVX    A, @DPTR
    MOV     R5, A
    INC     DPTR
    MOVX    A, @DPTR
    MOV     R3, A
    MOV     R7, #0x39
    PUSH    DPH
    PUSH    DPL
    LCALL   setReg
    POP     DPL
    POP     DPH
    INC     DPTR
    MOVX    A, @DPTR
    MOV     R5, A
    INC     DPTR
    MOVX    A, @DPTR
    MOV     R3, A
    MOV     R7, #0x38
    PUSH    DPH
    PUSH    DPL
    LCALL   setReg
    MOV     R7, #0x30
    MOV     R5, #0x00
    MOV     R3, #0x00
    LCALL   setReg
    MOV     R7, #0x30
    MOV     R5, #0xBF
    MOV     R3, #0xF1
    LCALL   setReg
    MOV     R0, #0xFF               ; spin delay to allow rssi to build up
cdelay1:                            ;
    CLR     C                       ;
    MOV     A, #0xFF                ;
cdelay2:                            ;
    NOP                             ;
    SUBB    A, #0x01                ;
    JNC     cdelay2                 ;
    DJNZ    R0, cdelay1             ;
    MOV     R7, #0x65               ; register 65 is noise level
    LCALL   getReg                  ; get the register value
    MOV     DPTR, #0x2F6            ; only need low byte
    MOVX    A, @DPTR                ; move noise byte into A
    ANL     A, #0x7F                ; only bits 0-6 is used
    MOV     R7, A
    LCALL   sendSerialByte
scanNext:
    POP     DPL
    POP     DPH
    SJMP    freqScanLoop

shiftMod:
    INC     DPTR                    ; next byte in the data block
    MOVX    A, @DPTR                ; this byte is the shift mode
    ANL     A, #0x80                ; we only need bit 7
    MOV     B, A                    ; save this in B, as we need A again
    MOV     DPTR, #0x4FF            ; extmem 4ff is a byte reserved for custom data, bit 7 is the shift mode
    MOVX    A, @DPTR                ; get the custom byte
    ANL     A, #0x7F                ; clear bit 7
    ORL     A, B                    ; set bit 7 with the requested value
    MOVX    @DPTR, A                ; move it back into 4ff
    SJMP    cAck                    ; send ack

setModulation:
    INC     DPTR                    ; next byte in the data block
    MOVX    A, @DPTR                ; this byte is the modulation mode we want to force
    ANL     A, #0x03                ; we only need bits 0,1
    MOV     B, A                    ; save this in B, as we need A again
    MOV     DPTR, #0x4FF            ; extmem 4ff is a byte reserved for custom data, bits 0,1 are the forced modulation mode
    MOVX    A, @DPTR                ; get the custom byte
    ANL     A, #0xFC                ; clear bits 0,1    
    ORL     A, B                    ; set bits 0,1 with the requested value
    MOVX    @DPTR, A                ; move it back into 4ff
cAck:
    MOV     R7, #0x07               ; 0x07 is the ack byte for custom packets
    LCALL   sendSerialByte          ; send the ack byte
    RET                             ; return back to firmware, the firmware will just ignore this packet

endScan:
    RET
scopeMod:
    LCALL   wakeUp
    MOV     R7, #0xB4               ; b4 is the packet id of scanning signal levels
    LCALL   sendSerialByte          ; send b4 to the host
    MOV     DPTR, #0x487            ; address of step count
    MOVX    A, @DPTR                ; move count into A
    MOV     R7, A                   ; move to R7 for serial send
    LCALL   sendSerialByte          ; send count to the host
    ;MOV     R7, #0x37               ; register 37 for RX enable
    ;MOV     R5, #0x1F               ; high register value byte
    ;MOV     R3, #0x0F               ; low register value byte
    LCALL   setReg                  ; turn RX on
scopeLoop:
    MOV     DPTR, #0x487            ; address of counter (steps)
    MOVX    A, @DPTR                ; move counter into A
    JZ      endScan                 ; end the scan if counter is 0
    DEC     A                       ; take one from counter
    MOVX    @DPTR, A                ; put it back into extmem
    MOV     DPTR, #0x483            ; address of frequency low word
    MOVX    A, @DPTR                ; high byte of low word of frequency
    MOV     R5, A                   ; into R5 for setReg function
    INC     DPTR                    ; next address   
    MOVX    A, @DPTR                ; low byte of low word of frequency
    MOV     R3, A                   ; into R3 for setReg function
    MOV     R7, #0x38               ; register 38 is low word of current frequency
    LCALL   setReg                  ; set the low word register
    MOV     DPTR, #0x481            ; address of frequency high word
    MOVX    A, @DPTR                ; high byte of high word of frequency
    MOV     R5, A                   ; into R5 for setReg function
    INC     DPTR                    ; next address   
    MOVX    A, @DPTR                ; low byte of high word of frequency
    MOV     R3, A                   ; into R3 for setReg function
    MOV     R7, #0x39               ; register 39 is high word of current frequency
    LCALL   setReg                  ; set the high word register
    MOV     R7, #0x30               ; toggle register 30 bit 0 to apply frequency
    MOV     R5, #0x00               ;
    MOV     R3, #0x00               ;
    LCALL   setReg                  ;
    MOV     R7, #0x30               ;
    MOV     R5, #0xBF               ;
    MOV     R3, #0xF1               ;
    LCALL   setReg                  ;
    MOV     R0, #0x40               ; spin delay to allow rssi to build up
delay1:                             ;
    CLR     C                       ;
    MOV     A, #0xFF                ;
delay2:                             ;
    NOP                             ;
    SUBB    A, #0x01                ;
    JNC     delay2                  ;
    DJNZ    R0, delay1              ;
    MOV     R7, #0x67               ; register 67 is current rssi
    LCALL   getReg                  ; get the register value
    MOV     DPTR, #0x2F5            ; get the high byte address of register value
    MOVX    A, @DPTR                ; move high byte into A
    ANL     A, #0x01                ; only bit 0 is used
    RR      A                       ; RR causes bit 0 to move to bit 7
    MOV     B, A                    ; store this in B
    INC     DPTR                    ; increment DPTR to address of low byte of rssi value
    MOVX    A, @DPTR                ; move low byte into A
    ANL     A, #0xFE                ; discard bit 0
    RR      A                       ; bit shift it right by 1 leaving bit 7 as 0
    ORL     A, B                    ; combine with B to set bit 7 (A now has the original 9 bit rssi value divided by 2, thus 8 bits)
    MOV     R7, A                   ; move the 8 bit rssi value into R7 for serial send
    LCALL   sendSerialByte          ; send it to the host
    CLR     C                       ; clear carry bit

    MOV     DPTR, #0x481            ; add the step to the frequency, first grab the address of the high byte of the current frequence
    MOVX    A, @DPTR                ; move it into A
    MOV     R0, A                   ; store in R0
    INC     DPTR                    ; next freq byte (lesser order)
    MOVX    A, @DPTR                ; move to a
    MOV     R1, A                   ; store in R1
    INC     DPTR                    ; next freq byte
    MOVX    A, @DPTR                ; move to A
    MOV     R2, A                   ; store in R2
    INC     DPTR                    ; last freq byte (low byte)
    MOVX    A, @DPTR                ; move to A
    MOV     R3, A                   ; store in R3
    INC     DPTR                    ; address of high byte of step size
    MOVX    A, @DPTR                ; move into A
    MOV     R4, A                   ; store in R4
    INC     DPTR                    ; address of low byte of step size
    MOVX    A, @DPTR                ; move into A
    CLR     C                       ; clear the carry bit so it doesn't interfere with the first add
    ADDC    A, R3                   ; add A (the low step size byte) to R3 (the low frequency byte, byte 0)
    MOV     R3, A                   ; store this back into R3
    MOV     A, R4                   ; move the high step size byte into A
    ADDC    A, R2                   ; add this to byte 1 of frequency (R2) including the carry if set
    MOV     R2, A                   ; store back into R2
    MOV     A, R1                   ; get byte 2 of frequency into A
    ADDC    A, #0x00                ; just add the carry if set
    MOV     R1, A                   ; store back into R1
    MOV     A, R0                   ; get byte 3 (high byte) of frequency
    ADDC    A, #0x00                ; just add the carry bit if set
    MOV     R0, A                   ; store back into R0
    MOV     DPTR, #0x481            ; now we need to put the added frequency back to extmem, so load the first address of the frequency (high byte) in extmem again
    MOV     A, R0                   ; move R0 (high byte) of the added frequency into A
    MOVX    @DPTR, A                ; replace the byte in extmem
    INC     DPTR                    ; next extmem byte address
    MOV     A, R1                   ; move next added byte (R1) into A
    MOVX    @DPTR, A                ; replace extmem byte
    INC     DPTR                    ; next extmem byte
    MOV     A, R2                   ; next added byte (R2) into A
    MOVX    @DPTR, A                ; replace
    INC     DPTR                    ; next byte address
    MOV     A, R3                   ; next added byte
    MOVX    @DPTR, A                ; replace
    AJMP    scopeLoop               ; next frequency


setRegDetour:
    MOV     0x4D, R7                ; perform one of the instructions replaced by the hook
    CJNE    R7, #0x47, not47        ; check to see if we're setting reg 47 (modulation)
    SJMP    reg47
not47:
    CJNE    R7, #0x3D, not3D        ; check to see if we're setting reg 3D
    SJMP    reg3D
not3D:
    CJNE    R7, #0x39, not39        ; check to see if we're setting reg 39
    SJMP    reg39
not39:
    CJNE    R7, #0x73, not73        ; check to see if we're setting reg 39
    SJMP    reg73
not73:
    SJMP    exitSetReg

reg73:
    MOV     DPTR, #0x4FF            ; bits 0,1 of 0x4ff is a flag indicate forced mode
    MOVX    A, @DPTR                ; get this byte
    ANL     A, #0x03                ; we only need bits 0,1
    JZ      exitSetReg
    CJNE    A, #0x03, notFM3
    MOV     A, R3
    ANL     A, #0xEF
    SJMP    adjusted73
    notFM3:
    MOV     A, R3
    ORL     A, #0x10
adjusted73:
    MOV     R3, A
    SJMP    exitSetReg

reg39:
    MOV     DPTR, #0x4FF            ; bit 7 of 0x4ff is a flag indicating frequency shift mode
    MOVX    A, @DPTR                ; get this byte
    ANL     A, #0x80                ; we only need bit 7
    JZ      exitSetReg              ; if 0 then the flag is not set to just exit
    INC     R5                      ; otherwise we increase R5 by two to increase the frequency by 335 MHz
    INC     R5
    SJMP    exitSetReg

reg3D:
    MOV     DPTR, #0x4FF            ; address of custom config byte
    MOVX    A, @DPTR                ; get byte from ext mem
    ANL     A, #0x03                ; only need bits 0,1
    CJNE    A, #0x02, exitSetReg    ; if the value of these two bits is not 2 (0%10) then we're not in USB mode, so leave reg 3D unchanged
    CLR     A                       ; set A to 0
    MOV     R3, A                   ; for USB we need reg 3D to be 0, so set high (R5) and low (R3) bytes to 0 (A)
    MOV     R5, A
    SJMP    exitSetReg              ; resume

reg47:
    MOV     DPTR, #0x4FF            ; 4ff is a byte containing data for custom use
    MOVX    A, @DPTR                ; grab this byte from extmem
    ANL     A, #0x03                ; we only want bits 0,1
    JNZ     overrideModul           ; if any bits are set A will be non zero, this means we need to override the moduation
exitSetReg:
    MOV     0x4E, R5                ; perform the other instruction replaced by the hook
    RET                             ; return to original function
overrideModul:
    PUSH    ACC
    CJNE    A, #0x01, notAM         ; 0x01 is the code for AM
isAM:
    MOV     R5, #0x67               ; 0x67 is the high byte modulation register value for AM, replaces the original value
    SJMP    exitSetReg2
notAM:
    CJNE    A, #0x02, isFM          ; 0x02 is the code for USB
isUSB:
    MOV     R5, #0x65               ; 0x65 is the high byte modulation register value for USB
    SJMP    exitSetReg2
isFM:                               ; 0x03 is the code for FM, the only other possible value remaining so no need to perform a check
    MOV     R5, #0x61               ; 0x61 is the high byte modulation register value for FM
exitSetReg2:
    MOV     R3, #0x40
    MOV     0x4E, R5
    LCALL   setRegResume 
setAFC:
    POP     ACC
    PUSH    ACC
    CJNE    A, #0x02, notUSB
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
    LCALL   getReg
    MOV     DPTR, #0x2F5
    MOVX    A, @DPTR
    MOV     R5, A
    INC     DPTR
    MOVX    A, @DPTR
    MOV     R3, A
    POP     ACC
    CJNE    A, #0x03, notFM2
    MOV     A, R3
    ANL     A, #0xEF
    SJMP    set73Register
notFM2:    
    MOV     A, R3
    ORL     A, #0x10
set73Register:
    MOV     R3, A
    MOV     R7, #0x73
    MOV     0x4D, R7
    MOV     0x4E, R5  
    RET





rssiDetour:                         ; make sure 2f5 is in DPTR before resuming
    MOV     R7, #0xA4               ; A4 is the packet ID for RSSI info
    LCALL   sendSerialByte          ; send A4 to the host
    MOV     DPTR, #0x2F6            ; get the address in extmem of the rssi low byte
    MOVX    A, @DPTR                ; put the low byte into A    
    MOV     R7, A                   ; move to R7 for the serial function
    LCALL   sendSerialByte          ; send it to the host
    MOV     DPTR, #0x2F5            ; get address of the rssi high byte
    MOVX    A, @DPTR                ; move high byte into A
    ANL     A, #0x01                ; we only need bit 0
    MOV     R7, A                   ; move to R7 for serial sending
    LCALL   sendSerialByte          ; send it to the host
    MOV     R7, #0x65               ; 65 is the noiselevel register
    LCALL   getReg                  ; read the register from the BK4819
    MOV     DPTR, #0x2F6            ; get the address of the low byte (is all that's relevant)
    MOVX    A, @DPTR                ; move it into A
    ANL     A, #0x7F                ; is only a 7 bit value
    MOV     R7, A                   ; move to R7 for serial sending
    LCALL   sendSerialByte          ; send noise level byte to host
    MOV     R7, #0x67               ; putting the state back to how it needs to be to resume, so we need to read register 67 again
    LCALL   getReg                  ; read reg 67
    MOV     DPTR, #0x2F5            ; move the high byte address into DPTR to put state back to how it should be
    LJMP    resumeRssiDetour        ; jump back to original function




