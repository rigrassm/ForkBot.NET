CC=cl
CXX=cl
CXX_OUT_FLAG=/Fo
C_OUT_FLAG=/Fo
OBJ_EXT=.obj
LIB_EXT=.lib
AR=lib
AR_OUTFLAG=/OUT:
EXE_EXT=.exe
LINK=cl
LINK_OUT_FLAG=/Fe
SO_EXT=.dll
SLINK=cl
SLINK_OUT_FLAG=/Fe
OS_DEFINES=/D _WINDOWS
AR_FLAGS=/nologo 
LINK_FLAGS=/nologo /MD
SLINK_FLAGS=/nologo /LD
CXXFLAGS=/nologo /c /Zi /W3 /WX- /O2 /Oy- /D _EXTERNAL_RELEASE /D WIN32 /D NDEBUG /D _CONSOLE /D _WINDOWS /D ASYNC_COMMANDS /Gm- /EHsc /GS /Gd /arch:SSE2  /MD
LINK_EXTRA_FLAGS=/link /DEBUG /MACHINE:X86 /SUBSYSTEM:CONSOLE /INCREMENTAL:NO /STACK:8388608 /OPT:REF /OPT:ICF /TLBID:1 /DYNAMICBASE /NXCOMPAT 
SLINK_EXTRA_FLAGS=/link /DEBUG /MACHINE:X86 /SUBSYSTEM:WINDOWS /INCREMENTAL:NO /STACK:8388608 /OPT:REF /OPT:ICF /TLBID:1 /DYNAMICBASE:NO 
CFLAGS=$(CXXFLAGS)
