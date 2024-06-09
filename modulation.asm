

.ORG        0xA6DE
    LJMP    comDetour1          ; hook COM function1
resumeComDetour1:

.ORG        0xB31E
    LJMP    comDetour2          ; hook COM function2
resumeComDetour2:

.ORG        0xBD1B
    MOV     SP, #0xCE

.ORG        0xE638              ; BK4819 Set Reg function
    LJMP modDetour
    NOP
resumeModDetour:

.ORG        0xEFD0                     ; end of flash

comDetour1:
    ACALL   comDetourCall
    LJMP    resumeComDetour1

comDetour2:
    ACALL   comDetourCall
    LJMP    resumeComDetour2

comDetourCall:
    MOV     DPTR, #0x47C
    MOVX    A, @DPTR
    CJNE    A, #0x52, resumeCom
    MOV     DPTR, #0x47F
    MOVX    A, @DPTR
    CJNE    A, #0x10, restoreCom
    MOV     DPTR, #0x47D
    MOVX    A, @DPTR  
    MOV     0xCD, A    
    MOV     A, #0x45
    MOV     DPTR, #0x47C
    MOVX    @DPTR, A
    RET
restoreCom:
    MOV     DPTR, #0x47C
resumeCom:
    RET


modDetour:
    MOV     0x4D, R7
    CJNE    R7, #0x47, notReg47
    ;PUSH    ACC
    MOV     A, 0xCD
    ANL     A, #0x03
    JNZ     override
    ;POP     ACC
notReg47:
    CJNE    R7, #0x3D, exitModDetour
    MOV     A, 0xCD
    ANL     A, #0x03
    CJNE    A, #0x02, exitModDetour
    MOV     R5, #0x00
    MOV     R3, #0x00
exitModDetour:
    MOV     0x4E, R5
    LJMP    resumeModDetour
override:
    CJNE    A, #0x01, not01
    MOV     R5, #0x67
    SJMP    resumeMod
not01:
    CJNE    A, #0x02, not02
    MOV     0x4D, #0x3D
    MOV     0x4E, #0x00
    MOV     R3, #0x00
    LCALL   resumeModDetour
    MOV     R5, #0x65
    MOV     0x4D, #0x47
    SJMP    resumeMod
not02:
    MOV     R5, #0x61
resumeMod:
    MOV     R3, #0x40
    ;POP     ACC
    SJMP    exitModDetour


