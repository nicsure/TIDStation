.ORG        0x5DE7              ; hooking signal level
    LJMP    rssiDetour
resumeRssiDetour:

.ORG        0xC800              ; BK4819 Read Register
getReg:

.ORG        0xEAA9              ; Serial send byte function
sendSerialByte:

.ORG        0xF200
rssiDetour:                     ; make sure 2f5 is in DPTR before resuming
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
    