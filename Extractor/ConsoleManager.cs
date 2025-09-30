using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Extractor
{
    internal static class ConsoleManager
    {
        private const int ATTACH_PARENT_PROCESS = -1;
        private const int ERROR_ACCESS_DENIED = 5;

        public static bool EnsureConsole()
        {
            if (AttachConsole(ATTACH_PARENT_PROCESS))
            {
                InitializeStreams();
                return false;
            }

            var error = Marshal.GetLastWin32Error();
            if (error == ERROR_ACCESS_DENIED)
            {
                InitializeStreams();
                return false;
            }

            if (!AllocConsole())
            {
                return false;
            }

            InitializeStreams();
            return true;
        }

        private static void InitializeStreams()
        {
            try
            {
                var standardOutput = new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };
                Console.SetOut(standardOutput);
                Console.SetError(new StreamWriter(Console.OpenStandardError()) { AutoFlush = true });
                Console.SetIn(new StreamReader(Console.OpenStandardInput()));
            }
            catch
            {
                // Ignore failures; console will fall back to default.
            }
        }

        public static void ReleaseConsole(bool allocated)
        {
            if (allocated)
            {
                FreeConsole();
            }
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AttachConsole(int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeConsole();
    }
}
