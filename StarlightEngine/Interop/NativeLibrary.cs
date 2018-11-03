using System;
using System.IO;
using System.Runtime.InteropServices;

namespace StarlightEngine.Interop
{
    /// <summary>
    /// Provides a common interface for loading and marshalling native libraries
    /// </summary>
    public class NativeLibrary
    {
        IntPtr m_handle;
        string m_path;

        /// <summary>
        /// Loads (or gets a handle to if already loaded) the specified library.
        /// </summary>
        /// <param name="library">The library to load</name>
        public NativeLibrary(string library, bool soname = true)
        {
            string prefix = "";
            string suffix = "";

            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                prefix = "lib";
                suffix = ".so";
            }
            else if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                prefix = "";
                suffix = ".dll";
            }

            if (soname)
            {
                m_path = Path.GetFullPath(Path.Combine("./lib64/", prefix + library + suffix));
            }
            else
            {
                m_path = Path.GetFullPath(library);
            }

            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                m_handle = dlopen(m_path, RTLD_NOW);
            }
            else if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                m_handle = LoadLibrary(@"./lib64/glfw3.dll");
                Console.WriteLine("Loaded: {0}", m_path);
            }

            if (m_handle == IntPtr.Zero)
            {
                throw new ApplicationException(string.Format("Error loading {0}", m_path));
            }
        }

        ~NativeLibrary()
        {
            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                int result = dlclose(m_handle);
                if (result != 0)
                {
                    Console.WriteLine("Failed to unload library: {0}", m_path);
                }
            }
            else if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                bool result = FreeLibrary(m_handle);
                if (!result)
                {
                    Console.WriteLine("Failed to unload library: {0}", m_path);
                }
            }
        }

        public TDelegate GetDelegateForUnmanagedFunction<TDelegate>(string functionName)
        {
            // Get handle to unmanaged function
            IntPtr functionHandle = IntPtr.Zero;
            if (Environment.OSVersion.Platform == PlatformID.Unix)
            {
                functionHandle = dlsym(m_handle, functionName);
            }
            else if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                functionHandle = GetProcAddress(m_handle, functionName);
            }

            if (functionHandle != IntPtr.Zero)
            {
                return Marshal.GetDelegateForFunctionPointer<TDelegate>(functionHandle);
            }
            else
            {
                throw new ApplicationException("Symbol not found: " + functionName);
            }
        }

        #region Windows Native Calls
        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        static extern IntPtr LoadLibrary(string lpLibFileName);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        static extern bool FreeLibrary(IntPtr hModule);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern long GetLastError();
        #endregion

        #region UNIX Native Calls
        const int RTLD_NOW = 2;

        [DllImport("dl", CharSet = CharSet.Ansi)]
        static extern IntPtr dlopen(string filename, int flags);

        [DllImport("dl", CharSet = CharSet.Ansi)]
        static extern IntPtr dlsym(IntPtr handle, string symbol);

        [DllImport("dl", CharSet = CharSet.Ansi)]
        static extern int dlclose(IntPtr handle);
        #endregion
    }
}