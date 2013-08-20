using System;
using System.Runtime.InteropServices;

namespace Fuzzman.Core.Interop
{
    public enum EXCEPTION_CODE : uint
    {
        DBG_CONTROL_C = 0x40010005,
        DBG_PRINTEXCEPTION_C = 0x40010006,
        DBG_RIPEXCEPTION = 0x40010007,
        DBG_COMMAND_EXCEPTION = 0x40010009,
        EXCEPTION_ACCESS_VIOLATION = NTSTATUS.STATUS_ACCESS_VIOLATION,
        EXCEPTION_DATATYPE_MISALIGNMENT = NTSTATUS.STATUS_DATATYPE_MISALIGNMENT,
        EXCEPTION_BREAKPOINT = NTSTATUS.STATUS_BREAKPOINT,
        EXCEPTION_SINGLE_STEP = NTSTATUS.STATUS_SINGLE_STEP,
        EXCEPTION_ARRAY_BOUNDS_EXCEEDED = NTSTATUS.STATUS_ARRAY_BOUNDS_EXCEEDED,
        EXCEPTION_FLT_DENORMAL_OPERAND = NTSTATUS.STATUS_FLOAT_DENORMAL_OPERAND,
        EXCEPTION_FLT_DIVIDE_BY_ZERO = NTSTATUS.STATUS_FLOAT_DIVIDE_BY_ZERO,
        EXCEPTION_FLT_INEXACT_RESULT = NTSTATUS.STATUS_FLOAT_INEXACT_RESULT,
        EXCEPTION_FLT_INVALID_OPERATION = NTSTATUS.STATUS_FLOAT_INVALID_OPERATION,
        EXCEPTION_FLT_OVERFLOW = NTSTATUS.STATUS_FLOAT_OVERFLOW,
        EXCEPTION_FLT_STACK_CHECK = NTSTATUS.STATUS_FLOAT_STACK_CHECK,
        EXCEPTION_FLT_UNDERFLOW = NTSTATUS.STATUS_FLOAT_UNDERFLOW,
        EXCEPTION_INT_DIVIDE_BY_ZERO = NTSTATUS.STATUS_INTEGER_DIVIDE_BY_ZERO,
        EXCEPTION_INT_OVERFLOW = NTSTATUS.STATUS_INTEGER_OVERFLOW,
        EXCEPTION_PRIV_INSTRUCTION = NTSTATUS.STATUS_PRIVILEGED_INSTRUCTION,
        EXCEPTION_IN_PAGE_ERROR = NTSTATUS.STATUS_IN_PAGE_ERROR,
        EXCEPTION_ILLEGAL_INSTRUCTION = NTSTATUS.STATUS_ILLEGAL_INSTRUCTION,
        EXCEPTION_NONCONTINUABLE_EXCEPTION = NTSTATUS.STATUS_NONCONTINUABLE_EXCEPTION,
        EXCEPTION_STACK_OVERFLOW = NTSTATUS.STATUS_STACK_OVERFLOW,
        EXCEPTION_INVALID_DISPOSITION = NTSTATUS.STATUS_INVALID_DISPOSITION,
        EXCEPTION_GUARD_PAGE = NTSTATUS.STATUS_GUARD_PAGE_VIOLATION,
        EXCEPTION_INVALID_HANDLE = NTSTATUS.STATUS_INVALID_HANDLE,
        EXCEPTION_POSSIBLE_DEADLOCK = NTSTATUS.STATUS_POSSIBLE_DEADLOCK,
        EXCEPTION_CPLUSPLUS = 0xE06D7363,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct EXCEPTION_RECORD
    {
        public EXCEPTION_CODE ExceptionCode;
        public uint ExceptionFlags;
        public IntPtr ExceptionRecord;
        public IntPtr ExceptionAddress;
        public uint NumberParameters;
        public IntPtr ExceptionInformation0; // Dirty hack, but the CLR cannot marshal union arrays...
        public IntPtr ExceptionInformation1;
        public IntPtr ExceptionInformation2;
        public IntPtr ExceptionInformation3;
        public IntPtr ExceptionInformation4;
        public IntPtr ExceptionInformation5;
        public IntPtr ExceptionInformation6;
        public IntPtr ExceptionInformation7;
        public IntPtr ExceptionInformation8;
        public IntPtr ExceptionInformation9;
        public IntPtr ExceptionInformationA;
        public IntPtr ExceptionInformationB;
        public IntPtr ExceptionInformationC;
        public IntPtr ExceptionInformationD;
        public IntPtr ExceptionInformationE;
    }
}
