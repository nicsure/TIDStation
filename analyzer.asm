.ORG        0xC800              ; BK4819 Read Register
getReg:

.ORG        0xE638              ; BK4819 Set Register function
setReg:

.ORG        0xEAA9              ; Serial send byte function
sendSerialByte:

.ORG        0xEFF5
    CJNE    A, #0x11, not11
    LCALL   saveStartFreqHi
    SJMP    noAnalyzer
not11:
    CJNE    A, #0x12, not12
    LCALL   saveStartFreqLo
    SJMP    noAnalyzer
not12:
    CJNE    A, #0x13, not13
    LCALL   saveStepSize
    SJMP    noAnalyzer
not13:
    CJNE    A, #0x14, noAnalyzer
    LCALL   SetStepCount
noAnalyzer:

.ORG        0xF300
saveStartFreqHi:
    MOV     DPTR, #0x47D
    MOVX    A, @DPTR
    MOV     DPTR, #0x4F0
    MOVX    @DPTR, A
    MOV     DPTR, #0x47E
    MOVX    A, @DPTR
    MOV     DPTR, #0x4F1
    MOVX    @DPTR, A
    RET
saveStartFreqLo:
    MOV     DPTR, #0x47D
    MOVX    A, @DPTR
    MOV     DPTR, #0x4F2
    MOVX    @DPTR, A
    MOV     DPTR, #0x47E
    MOVX    A, @DPTR
    MOV     DPTR, #0x4F3
    MOVX    @DPTR, A
    RET
saveStepSize:
    MOV     DPTR, #0x47D
    MOVX    A, @DPTR
    MOV     DPTR, #0x4F4
    MOVX    @DPTR, A
    MOV     DPTR, #0x47E
    MOVX    A, @DPTR
    MOV     DPTR, #0x4F5
    MOVX    @DPTR, A
    RET
SetStepCount:
    MOV     R7, #0xB4
    LCALL   sendSerialByte
    MOV     DPTR, #0x47D
    MOVX    A, @DPTR
    MOV     R7, A
    LCALL   sendSerialByte
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
    MOV     DPTR, #0x47D
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

getFreq:
    MOV     DPTR, #0x4F0
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
    MOV     DPTR, #0x4F0
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

; 17/06/2024 Mon 13:00 Lidl Skegness 