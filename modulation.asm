.ORG        0xBD1B
    MOV     SP, #0xCE

.ORG        0xE638              ; BK4819 Set Reg function
    LJMP    modDetour
    NOP
resumeModDetour:

.ORG        0xEFEC
modOverride:
    CJNE    A, #0x10, noModOverride
    MOV     DPTR, #0x47D
    MOVX    A, @DPTR  
    MOV     0xCD, A
noModOverride:

.ORG        0xF100
modDetour:
    MOV     0x4D, R7
    CJNE    R7, #0x47, notReg47
    MOV     A, 0xCD
    ANL     A, #0x03
    JNZ     override
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
    SJMP    exitModDetour


