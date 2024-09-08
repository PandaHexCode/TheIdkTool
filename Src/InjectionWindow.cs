using ImGuiNET;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using TheIdkTool.Dialogs;

namespace TheIdkTool.Windows{

    public class InjectionWindow : DrawWindow{

        #region Import 

        // P/Invoke declarations
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, int processId);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        private static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress,
            uint dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress,
            byte[] lpBuffer, uint nSize, out UIntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        private static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes,
            uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, out IntPtr lpThreadId);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hHandle);

        // Constants for memory allocation and process access
        private const uint PROCESS_CREATE_THREAD = 0x0002;
        private const uint PROCESS_VM_OPERATION = 0x0008;
        private const uint PROCESS_VM_WRITE = 0x0020;
        private const uint PROCESS_VM_READ = 0x0010;

        private const uint MEM_COMMIT = 0x00001000;
        private const uint MEM_RESERVE = 0x00002000;
        private const uint PAGE_READWRITE = 4;

        #endregion 

        private Process? currentProcess = null;

        public override void Draw(){
            ImGui.InputText("Process name", ref this.inputRefs[0], 200);
            ImGui.InputText("##p3", ref inputRefs[1], 1000);
            Manager.SelectFileButton(ref inputRefs[1], "Dll-Path");

            if (!this.inputRefs[1].EndsWith(".dll")){
                ImGui.TextWrapped("Not a dll.");
                return;
            }

            if (ImGui.Button("Refresh")){
                try{
                    this.currentProcess = Process.GetProcessesByName(this.inputRefs[0])[0];
                }catch(Exception ex){
                    DrawUtilRender.AddDrawUtil(new WarningDialog(), "Process not found!");
                }
            }
            if(this.currentProcess != null){
                ImGui.SameLine();
                ImGui.TextWrapped("Process found\nId: " + this.currentProcess.Id);

                if (ImGui.Button("Simple-Inject"))
                    SimpleDLLInject();
            }
        }       

        public void SimpleDLLInject(){
            IntPtr hProcess = OpenProcess(PROCESS_CREATE_THREAD | PROCESS_VM_OPERATION | PROCESS_VM_WRITE | PROCESS_VM_READ, false, this.currentProcess.Id);

            if (hProcess == IntPtr.Zero){
                DrawUtilRender.AddDrawUtil(new WarningDialog(), "Failed to open target process.");
                return;
            }

            // Allocate memory in the target process for the DLL path
            IntPtr allocMemAddress = VirtualAllocEx(hProcess, IntPtr.Zero, (uint)((this.inputRefs[1].Length + 1) * Marshal.SizeOf(typeof(char))), MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
            if (allocMemAddress == IntPtr.Zero){
                DrawUtilRender.AddDrawUtil(new WarningDialog(), "Failed to allocate memory in target process.");
                CloseHandle(hProcess);
                return;
            }

            // Write the DLL path into the allocated memory
            byte[] dllBytes = System.Text.Encoding.Default.GetBytes(this.inputRefs[1]);
            WriteProcessMemory(hProcess, allocMemAddress, dllBytes, (uint)dllBytes.Length, out _);

            // Get the address of LoadLibraryA function
            IntPtr loadLibraryAddr = GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryA");
            if (loadLibraryAddr == IntPtr.Zero){
                DrawUtilRender.AddDrawUtil(new WarningDialog(), "Failed to get LoadLibraryA address.");
                CloseHandle(hProcess);
                return;
            }

            // Create a remote thread that calls LoadLibraryA with the DLL path
            IntPtr hThread = CreateRemoteThread(hProcess, IntPtr.Zero, 0, loadLibraryAddr, allocMemAddress, 0, out _);
            if (hThread == IntPtr.Zero){
                DrawUtilRender.AddDrawUtil(new WarningDialog(), "Failed to create remote thread in target process."); 
                CloseHandle(hProcess);
                return;
            }

            // Wait for the thread to finish
            WaitForSingleObject(hThread, 0xFFFFFFFF);

            // Clean up
            CloseHandle(hThread);
            CloseHandle(hProcess);

            DrawUtilRender.AddDrawUtil(new WarningDialog(), "DLL successfully injected!");
        }

    }

}