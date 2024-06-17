
.ORG        0x5DE7              ; hook signal level poll function (3 bytes)
    LJMP    rssiDetour          ; Original code: 90 02 f5   MOV     DPTR,#0x2f5
resumeRssiDetour:

.ORG        0xC05F              ; Hook COM ID (3 bytes) 
    LCALL   comIdHook           ; Original code: 90 04 7D   MOV     DPTR,#0x47D
.ORG        0xC469              ; Hook COM ID (3 bytes) 
    LCALL   comIdHook           ; Original code: 90 04 7D   MOV     DPTR,#0x47D

.ORG        0xC800              ; BK4819 Read Register function
getReg:

.ORG        0xE638              ; BK4819 Set Register function & Hook (4 bytes)
setReg:                         ; Original code: 8f 4d      MOV     0x4d,r7
    LJMP    modDetour           ;                8d 4e      MOV     0x4e,r5
    NOP
resumeSetReg:

.ORG        0xEAA9              ; Serial send byte function
sendSerialByte:

.ORG        0xEFD0              ; end of original firmware, start of patch code
comIdHook:
    MOV     DPTR,#0x47D
    MOVX    A, @DPTR
    CJNE    A, #0x56, customRequest
    RET

customRequest:
    POP     DPH
    POP     DPL
    CJNE    A, #0x00, notAmFmUsb
    SJMP    amFmUsb
notAmFmUsb:
    CJNE    A, #0x01, notSpectMid
    SJMP    spectMid
notSpectMid:
    CJNE    A, #0x02, notSpectStep
    SJMP    spectStep
notSpectStep:
    RET

amFmUsb:
    MOV     DPTR, #0x47E
    MOVX    A, @DPTR
    ANL     A, #0x03
    MOV     B, A
    MOV     DPTR, #0x4FF
    MOVX    A, @DPTR
    ANL     A, #0xFC
    ORL     A, B
    MOVX    @DPTR, A
    MOV     R7, #0x07
    LCALL   sendSerialByte
    RET

spectMid:
    MOV     DPTR, #0x47E
    MOVX    A, @DPTR
    MOV     DPTR, #0x4F0
    MOVX    @DPTR, A    
    MOV     DPTR, #0x47F
    MOVX    A, @DPTR
    MOV     DPTR, #0x4F1
    MOVX    @DPTR, A
    MOV     DPTR, #0x480
    MOVX    A, @DPTR
    MOV     DPTR, #0x4F2
    MOVX    @DPTR, A
    MOV     DPTR, #0x481
    MOVX    A, @DPTR
    MOV     DPTR, #0x4F3
    MOVX    @DPTR, A
    MOV     R7, #0x07
    LCALL   sendSerialByte    
    RET

spectStep:
    MOV     DPTR, #0x47E
    MOVX    A, @DPTR
    MOV     DPTR, #0x4F4
    MOVX    @DPTR, A    
    MOV     DPTR, #0x47F
    MOVX    A, @DPTR
    MOV     DPTR, #0x4F5
    MOVX    @DPTR, A
    MOV     DPTR, #0x480
    MOVX    A, @DPTR
    MOV     DPTR, #0x4FE
    MOVX    @DPTR, A
    MOV     R7, #0xB4
    LCALL   sendSerialByte
    MOV     DPTR, #0x4FE
    MOVX    A, @DPTR
    MOV     R7, A    
    LCALL   sendSerialByte
    ACALL   copyFreq
    SJMP    initLoop
signalLoop:
    ACALL   delay
    MOV     R7, #0x67
    LCALL   getReg
    MOV     DPTR, #0x2F5
    MOVX    A, @DPTR
    ANL     A, #0x01;
    RR      A
    MOV     B, A
    INC     DPTR
    MOVX    A, @DPTR
    ANL     A, #0xFE
    RR      A
    ORL     A, B
    MOV     R7, A
    LCALL   sendSerialByte
initLoop:
    MOV     DPTR, #0x4FE
    MOVX    A, @DPTR
    CLR     C
    SUBB    A, #0x01
    JC      endScan
    MOVX    @DPTR, A
    ACALL   getFreq
    ACALL   applyFreq
    ACALL   getFreq
    ACALL   addStep
    ACALL   setFreq
    SJMP    signalLoop
endScan:
    RET

applyFreq:
    MOV     R7, #0x39
    MOV     A, R0
    MOV     R5, A
    MOV     A, R1
    MOV     R3, A
    LCALL   setReg
    ACALL   getFreq
    MOV     R7, #0x38
    MOV     A, R2
    MOV     R5, A
    LCALL   setReg
    MOV     R7, #0x30
    MOV     R5, #0x00
    MOV     R3, #0x00
    LCALL   setReg
    MOV     R7, #0x30
    MOV     R5, #0xBF
    MOV     R3, #0xF1
    LCALL   setReg
    RET

copyFreq:
    MOV     DPTR, #0x4F0
    MOVX    A, @DPTR
    MOV     DPTR, #0x4F6
    MOVX    @DPTR, A
    MOV     DPTR, #0x4F1
    MOVX    A, @DPTR
    MOV     DPTR, #0x4F7
    MOVX    @DPTR, A
    MOV     DPTR, #0x4F2
    MOVX    A, @DPTR
    MOV     DPTR, #0x4F8
    MOVX    @DPTR, A
    MOV     DPTR, #0x4F3
    MOVX    A, @DPTR
    MOV     DPTR, #0x4F9
    MOVX    @DPTR, A
    RET

getFreq:
    MOV     DPTR, #0x4F6
    MOVX    A, @DPTR
    MOV     R0, A
    INC     DPTR
    MOVX    A, @DPTR
    MOV     R1, A
    INC     DPTR
    MOVX    A, @DPTR
    MOV     R2, A
    INC     DPTR
    MOVX    A, @DPTR
    MOV     R3, A
    RET

setFreq:
    MOV     DPTR, #0x4F6
    MOV     A, R0
    MOVX    @DPTR, A
    INC     DPTR
    MOV     A, R1
    MOVX    @DPTR, A
    INC     DPTR
    MOV     A, R2
    MOVX    @DPTR, A
    INC     DPTR
    MOV     A, R3
    MOVX    @DPTR, A
    RET

addStep:
    MOV     DPTR, #0x4F4
    MOVX    A, @DPTR
    MOV     R4, A
    INC     DPTR
    MOVX    A, @DPTR
    MOV     R5, A
    CLR     C
    MOV     A, R3
    ADD     A, R5
    MOV     R3, A
    MOV     A, R2
    ADDC    A, R4
    MOV     R2, A
    MOV     A, R1
    ADDC    A, #0x00
    MOV     R1, A
    MOV     A, R0
    ADDC    A, #0x00
    MOV     R0, A
    RET

delay:
    MOV     R0, #0x40
delay1:
    CLR     C
    MOV     A, #0xFF
delay2:
    NOP
    SUBB    A, #0x01
    JNC     delay2  
    DJNZ    R0, delay1
    RET

modDetour:
    MOV     0x4D, R7
    CJNE    R7, #0x47, notReg47
    MOV     DPTR, #0x4FF
    MOVX    A, @DPTR
    ANL     A, #0x03
    JNZ     override
    SJMP    notReg47
exitDetour:
    MOV     R3, #0x40
notReg47:
    MOV     0x4E, R5
    LJMP    resumeSetReg
override:
    CJNE    A, #0x01, not01
    MOV     R5, #0x67               ; AM
    SJMP    exitDetour
not01:
    CJNE    A, #0x02, not02
    MOV     R5, #0x65               ; USB
    SJMP    exitDetour
not02:
    MOV     R5, #0x61               ; FM
    SJMP    exitDetour


rssiDetour:                         ; make sure 2f5 is in DPTR before resuming
    MOV     R7, #0xA4
    LCALL   sendSerialByte
    MOV     DPTR, #0x2F6
    MOVX    A, @DPTR
    MOV     R7, A
    LCALL   sendSerialByte
    MOV     DPTR, #0x2F5
    MOVX    A, @DPTR
    ANL     A, #0x01
    MOV     R7, A
    LCALL   sendSerialByte
    MOV     R7, #0x65
    LCALL   getReg
    MOV     DPTR, #0x2F6
    MOVX    A, @DPTR
    ANL     A, #0x7F
    MOV     R7, A
    LCALL   sendSerialByte
    MOV     R7, #0x67
    LCALL   getReg
    MOV     DPTR, #0x2F5
    LJMP    resumeRssiDetour

setRegBits:                     ; A should be set with the SoC register number before the call
    PUSH    ACC                 ; save A to stack, as getReg will clobber it
    MOV     R7, A               ; getReg requires the register number to be in R7
    LCALL   getReg              ; read SoC register, 16 bit result is placed at ext 0x2F5+ big endian
    POP     ACC                 ; restore A (reg number)
    MOV     R7, A               ; Put A (reg number) back into R7, it's needed to be there for setReg later
    MOV     DPTR, #0x2F5        ; store 16 bit register value in R5 and R3 (16 bit, R5 is high byte)
    MOVX    A, @DPTR
    MOV     R5, A
    INC     DPTR
    MOVX    A, @DPTR
    MOV     R3, A               
    MOV     A, 0x4C             ; 4C (high byte) and 4D (low byte) int mem, to be set (precall) with the inverted bit mask of bits to clear
    ANL     A, R5
    MOV     R5, A
    MOV     A, 0x4D
    ANL     A, R3
    MOV     R3, A
    MOV     A, 0x4E             ; 4E (high byte) and 4F (low byte) int mem, to be set (precall) with the bits to set
    ORL     A, R5
    MOV     R5, A
    MOV     A, 0x4F
    ORL     A, R3
    MOV     R3, A
    LCALL   setReg              ; set the SoC register with new value, setReg takes params R7=RegNumber, R5=RegValue(High), R3=RegValue(Low)
    RET
