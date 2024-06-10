.ORG        0x5DE7              ; hooking signal level
    LJMP    rssiDetour
resumeRssiDetour:

.ORG        0xEAA9              ; Serial send byte function
sendSerialByte:

.ORG        0xF200
rssiDetour:                     ; make sure 2f5 is in DPTR before resuming
    MOV     DPTR, #0x2F5
    MOVX    A, @DPTR
    ANL     A, #0x01
    MOV     B, A
    INC     DPTR
    MOVX    A, @DPTR
    MOV     R7, #0xA4
    LCALL   sendSerialByte
    MOV     R7, A
    LCALL   sendSerialByte
    MOV     R7, B
    LCALL   sendSerialByte
    MOV     DPTR, #0x2F5
    LJMP    resumeRssiDetour
    