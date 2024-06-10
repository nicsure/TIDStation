.ORG        0xA6DE
    LJMP    comDetour1                  ; hook COM function1
resumeComDetour1:

.ORG        0xB31E
    LJMP    comDetour2                  ; hook COM function2
resumeComDetour2:

.ORG        0xEFD0                      ; end of firmware
comDetour1:
    ACALL   comDetourCall               ; call detection function
    LJMP    resumeComDetour1            ; resume at correct point in firmware

comDetour2:
    ACALL   comDetourCall               ; call detection function
    LJMP    resumeComDetour2            ; resume at correct point in firmware

comDetourCall:
    MOV     DPTR, #0x47C                ; the detour overwrote this, so it must be executed before resuming
    MOVX    A, @DPTR                    ; get the first byte received on the COM port
    CJNE    A, #0x52, resumeCom         ; we're only interested in packet ID 0x52, so anything else we just resume
    MOV     DPTR, #0x47F                ; we need to look at the value of the 4th byte
    MOVX    A, @DPTR                    ; get it from external memory
    CJNE    A, #0x20, custom            ; 0x20 is the only value the firmware uses, so if it's not 0x20 we have a custom request
    MOV     DPTR, #0x47C                ; make sure DPTR is set back to pointing @ 47c as we have just changed it
resumeCom:
    RET                                 ; resume

custom:                                 ; other patches will populate this placeholder. You may increase/decrease the space as required by adding/removing 0's
    .BYTE   0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0

    MOV     DPTR, #0x47D
    CLR     A
    MOVX    @DPTR, A
    INC     DPTR
    MOVX    @DPTR, A
    INC     DPTR
    MOV     A, #0x20
    MOVX    @DPTR, A
    MOV     DPTR, #0x47C
    RET
