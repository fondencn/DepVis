using System.Runtime.InteropServices;

namespace DepVis
{
    internal static class NativeMethods
    {

        [DllImport("psapi.dll", SetLastError = true)]
        public static extern bool EnumProcessModulesEx(IntPtr hProcess, IntPtr lphModule, uint cb, out uint lpcbNeeded, uint dwFilterFlag);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool FreeLibrary(IntPtr hModule);

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetCurrentProcess();

        [DllImport("psapi.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern uint GetModuleFileNameEx(IntPtr hProcess, IntPtr hModule, [Out] char[] lpFilename, uint nSize);

        // P/Invoke declarations for WinAPI functions.
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr LoadLibraryEx(string lpFileName, IntPtr hFile, LoadLibraryFlags dwFlags);


        [DllImport("kernel32.dll")]
        public static extern uint SetErrorMode(uint uMode);

        // Error mode constants
        public const uint SEM_FAILCRITICALERRORS = 0x0001;
        public const uint SEM_NOOPENFILEERRORBOX = 0x8000;
        public const uint LIST_MODULES_ALL = 0x03;

        [DllImport("dbghelp.dll", SetLastError = true)]
        public static extern bool SymInitialize(IntPtr hProcess, string? UserSearchPath, bool fInvadeProcess);

        [DllImport("dbghelp.dll", SetLastError = true)]
        public static extern bool SymCleanup(IntPtr hProcess);

        [DllImport("dbghelp.dll", SetLastError = true)]
        public static extern ulong SymLoadModuleEx(
            IntPtr hProcess,
            IntPtr hFile,
            string ImageName,
            string? ModuleName,
            ulong BaseOfDll,
            uint DllSize,
            IntPtr Data,
            uint Flags);

        [DllImport("dbghelp.dll", SetLastError = true)]
        public static extern bool SymEnumerateModules(
            IntPtr hProcess,
            SymEnumerateModulesProc64 EnumModulesCallback,
            IntPtr UserContext);

        public delegate bool SymEnumerateModulesProc64(string ModuleName, ulong BaseOfDll, IntPtr UserContext);

    }



    [Flags]
    internal enum LoadLibraryFlags : uint
    {
        NONE = 0x00000000, 
        DONT_RESOLVE_DLL_REFERENCES = 0x00000001, // Load the library without resolving dependencies.
        LOAD_IGNORE_CODE_AUTHZ_LEVEL = 0x00000010,
        LOAD_LIBRARY_AS_DATAFILE = 0x00000002,
        LOAD_LIBRARY_AS_IMAGE_RESOURCE = 0x00000020,
        LOAD_WITH_ALTERED_SEARCH_PATH = 0x00000008
    }

}