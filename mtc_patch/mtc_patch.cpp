//////////////////////////////////////////////////////////////////////////////
//
//  Detours Test Program (simple.cpp of simple.dll)
//
//  Microsoft Research Detours Package
//
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//
//  This DLL will detour the Windows SleepEx API so that TimedSleep function
//  gets called instead.  TimedSleepEx records the before and after times, and
//  calls the real SleepEx API through the TrueSleepEx function pointer.
//
#define _CRT_SECURE_NO_WARNINGS
#include <stdio.h>
#include <windows.h>
#include <detours/detours.h>

void *dummy() {
    return NULL;
}

void dbg_printf(const char* fmt, ...) {
    char dbg[10240];
    va_list args;
    va_start(args, fmt);
    vsprintf(dbg, fmt, args);
    OutputDebugStringA(dbg);
    OutputDebugStringA("\n");
    va_end(args);
}


class CDetour {
public:
    void hook_WgwMsgHandler(void *arg1, void *commandStream, void *dataStream);
    static void (CDetour::* ori_WgwMsgHandler)(void* arg1, void* commandStream, void* dataStream);
};

struct CommonString {
    static void(CommonString::* Common_String_String)(const char* str, int len);
    static void(CommonString::* Common_String_toString)(void *);

    static void(CommonString::* Common_String_delString)();
    static void(CommonString::* Common_String_operator_in)(void* output, void* key);
    static void *(CommonString::* Common_String_begin)(void* output);
    static bool(CommonString::* Common_String_compareIter)(void* val);
    static void* (CommonString::* Common_String_bufferString)();
    static void* (CommonString::* Common_String_cstr)();
    static size_t (CommonString::* Common_String_size)();

    static void String(void* th, const char* str, int len) {
        ((*(CommonString *)th).*Common_String_String)(str, len);
    }
    static void toString(void* th, void *out) {
        ((*(CommonString*)th).*Common_String_toString)(out);
    }
    static void operator_in(void* commandStream, void* output, void* key) {
        ((*(CommonString*)commandStream).*Common_String_operator_in)(output, key);
    }
    static void delString(void* th) {
        ((*(CommonString*)th).*Common_String_delString)();
    }

    static void* begin(void* th, void *output) {
        return ((*(CommonString*)th).*Common_String_begin)(output);
    }
    static bool compareIter(void* th, void* val) {
        return ((*(CommonString*)th).*Common_String_compareIter)(val);
    }
    static char *bufferString(void* th) {
        return (char *)((*(CommonString*)th).*Common_String_bufferString)();
    }
    static const char* c_str(void* th) {
        return (const char *)((*(CommonString*)th).*Common_String_cstr)();
    }
    static size_t size(void* th) {
        return ((*(CommonString*)th).*Common_String_size)();
    }
};

intptr_t slide = 0;

void (CDetour::* CDetour::ori_WgwMsgHandler)(void* arg1, void* commandStream, void* dataStream) = NULL;

void(CommonString::* CommonString::Common_String_String)(const char* str, int len) = NULL;
void(CommonString::* CommonString::Common_String_toString)(void *) = NULL;
void(CommonString::* CommonString::Common_String_delString)() = NULL;
void(CommonString::* CommonString::Common_String_operator_in)(void* output, void* key) = NULL;
void *(CommonString::* CommonString::Common_String_begin)(void* output) = NULL;
bool(CommonString::* CommonString::Common_String_compareIter)(void* val) = NULL;
void *(CommonString::* CommonString::Common_String_bufferString)() = NULL;
void* (CommonString::* CommonString::Common_String_cstr)() = NULL;
size_t (CommonString::* CommonString::Common_String_size)() = NULL;
void* (*Rsd_NtfnCreate)(const char*) = NULL;
void* (*Rsd_NtfnAddStr)(void *, const char*, const char *) = NULL;
void* (*Rsd_NtfnAddStrX)(void*, const char*, const char*, size_t) = NULL;
void* Rsd_EnbLeaveNtfnX = NULL;
void* (*Zos_ModPerform)(int, void *, const char*, void *) = NULL;

#define ASSIGN(a, b) *(void **)(&a) = (void *)(b + slide)

BOOL WINAPI DllMain(HINSTANCE hinst, DWORD dwReason, LPVOID reserved)
{
    LONG error;
    (void)hinst;
    (void)reserved;

    if (DetourIsHelperProcess()) {
        return TRUE;
    }

    if (dwReason == DLL_PROCESS_ATTACH) {
        char* mtcBase = (char*)GetModuleHandleA("mtc");
        slide = (mtcBase - (char*)0x10000000);

        ASSIGN(CDetour::ori_WgwMsgHandler, 0x1043C4E0);
        ASSIGN(CommonString::Common_String_String, 0x105E5790);
        ASSIGN(CommonString::Common_String_toString, 0x10608160);
        ASSIGN(CommonString::Common_String_delString, 0x105E6820);
        ASSIGN(CommonString::Common_String_operator_in, 0x102D57F0);
        ASSIGN(CommonString::Common_String_begin, 0x102C8D10);
        ASSIGN(CommonString::Common_String_compareIter, 0x1029B730);
        ASSIGN(CommonString::Common_String_bufferString, 0x102D54E0);
        ASSIGN(CommonString::Common_String_cstr, 0x105EB7C0);
        ASSIGN(CommonString::Common_String_size, 0x10602020);
        ASSIGN(Rsd_NtfnCreate, 0x102BCEC0);
        ASSIGN(Rsd_NtfnAddStr, 0x102BD2B0);
        ASSIGN(Rsd_NtfnAddStrX, 0x102BD320);
        ASSIGN(Rsd_EnbLeaveNtfnX, 0x102AC3F0);
        ASSIGN(Zos_ModPerform, 0x1090BA50);

        DetourRestoreAfterWith();

        DetourTransactionBegin();
        DetourUpdateThread(GetCurrentThread());
        
        auto pfMine = &CDetour::hook_WgwMsgHandler;
        DetourAttach(&(PVOID&)CDetour::ori_WgwMsgHandler, *(PBYTE*)&pfMine);
        error = DetourTransactionCommit();

        if (error == NO_ERROR) {
            dbg_printf("Successfully Hooked WgwOnMessage!");
        }
        else {
            dbg_printf("Error detouring: %ld\n", error);
        }
    }
    else if (dwReason == DLL_PROCESS_DETACH) {
        return FALSE;
    }
    return TRUE;
}


void CDetour::hook_WgwMsgHandler(void* arg1, void* commandStream, void* dataStream) {
    dbg_printf("   hook_WgwMsgHandler! (this:%p)", this);
    char dataStr[96] = { 0 };
    void* tempVar = NULL;
    CommonString::toString(dataStream, dataStr);

    char tempStr[96] = { 0 };
    void* fromRet = NULL;
    void* gwOidRet = NULL;
    void* timeRet = NULL;
    CommonString::String(tempStr, "Sip.Message.From", -1);
    CommonString::operator_in(commandStream, &fromRet, tempStr);
    CommonString::delString(tempStr);

    CommonString::String(tempStr, "Sip.MessageGw.Oid", -1);
    CommonString::operator_in(commandStream, &gwOidRet, tempStr);
    CommonString::delString(tempStr);

    CommonString::String(tempStr, "Sip.Message.Time", -1);
    CommonString::operator_in(commandStream, &timeRet, tempStr);
    CommonString::delString(tempStr);

    if (!CommonString::compareIter(&fromRet, CommonString::begin(commandStream, &tempVar))) {
        dbg_printf("MtcWgwMsgReciver invalid message.");
        return;
    }

    if (!CommonString::compareIter(&gwOidRet, CommonString::begin(commandStream, &tempVar))) {
        dbg_printf("MtcWgwMsgReciver invalid message.");
        return;
    }

    if (!CommonString::compareIter(&timeRet, CommonString::begin(commandStream, &tempVar))) {
        dbg_printf("MtcWgwMsgReciver invalid message.");
        return;
    }

    auto gwoid = CommonString::c_str(CommonString::bufferString(&gwOidRet) + 40);

    void* v26 = Rsd_NtfnCreate("MtcWgwDataRecvedNotification");
    auto fromCStr = CommonString::c_str(CommonString::bufferString(&fromRet) + 40);
    Rsd_NtfnAddStr(v26, "MtcWgwUsernameKey", fromCStr);
    auto timeCStr = CommonString::c_str(CommonString::bufferString(&timeRet) + 40);
    Rsd_NtfnAddStr(v26, "MtcWgwInstanceIdKey", timeCStr);
    auto dataCStr = CommonString::c_str(dataStr);
    auto dataSize = CommonString::size(dataStr);
    Rsd_NtfnAddStrX(v26, "MtcWgwDataKey", dataCStr, dataSize);
    dbg_printf("Got GwOid: %s, From: %s, Time: %s, Content: %s", gwoid, fromCStr, timeCStr, dataCStr);
    Zos_ModPerform(15, Rsd_EnbLeaveNtfnX, "%p", v26);
    CommonString::delString(dataStr);
}