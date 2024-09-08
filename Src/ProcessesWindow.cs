﻿using ImGuiNET;
using Silk.NET.Core.Native;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Management;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text;
using TheIdkTool.Dialogs;

namespace TheIdkTool.Windows{

    public class ProcessesWindow : DrawWindow{

        public static Thread? checkForNewProcessesThread = null;
        public static Thread? checkForNewModulesThread = null;
        public static List<Process> checkForNewProcessesRegisteredProcesses = new List<Process>();
        public static List<Module> checkForNewModulesRegisteredModules = new List<Module>() ;
        private List<Manager.ModuleSummary> moduleSummaries = new List<Manager.ModuleSummary>();
        private ManagementEventWatcher? monitorProcessesWatcher = null;

        public class Module{
            public Process process;
            public ProcessModule module;

            public Module(Process process, ProcessModule module){
                this.process = process;
                this.module = module;
            }
        }

        public override void Draw(){
            ImGui.InputText("##kl", ref inputRefs[0], 1024);
            Manager.SelectFileButton(ref inputRefs[0], "Kill list path");
            if(ImGui.Button("Try to kill list")){
                if(!File.Exists(inputRefs[0]))
                    DrawUtilRender.AddDrawUtil(new WarningDialog(), "The path is incorrect!");

                string[] lines = Manager.GetFileIn(inputRefs[0]).Split('\n');
                foreach (string str in lines){
                    if (String.IsNullOrEmpty(str))
                        continue;

                    Manager.TryToKillProcess(str);
                    Manager.TryToKillProcess(str.Remove(str.Length - 1));
                    Manager.TryToKillProcess(str + ".exe");//just in case
                    Manager.TryToKillProcess(str.Remove(str.Length - 1));
                    Manager.TryToKillProcess(str.Remove(str.Length - 1));
                    Console.WriteLine("Try killed: " + str);
                }
            }
            Manager.Tooltip("Try killing every process from a text file with names.\nSeparating names with new lines.");

            if (checkForNewProcessesThread == null)
                ImGui.SameLine();
            if(checkForNewProcessesThread == null && ImGui.Button("Check for new processes")){
                checkForNewProcessesThread = new Thread(() => Manager.CheckForNewProcesses());
                checkForNewProcessesThread.IsBackground = true;
                checkForNewProcessesThread.Start();
            }

            if (checkForNewModulesThread == null)
                ImGui.SameLine();
            if (checkForNewModulesThread == null && ImGui.Button("Check for new modules")){
                checkForNewModulesThread = new Thread(() => Manager.CheckForNewModules());
                checkForNewModulesThread.IsBackground = true;
                checkForNewModulesThread.Start();
            }

            ImGui.SameLine();
            if (ImGui.Button("Test scan"))
                TestScan();

            if (checkForNewProcessesThread != null){
                if (ImGui.TreeNodeEx("NewProcesses")){
                    ImGui.TreePop();
                    ImGui.SameLine();
                    if (ImGui.Button("Clear"))
                        checkForNewProcessesRegisteredProcesses.Clear();
                    ImGui.SameLine();
                    if (ImGui.Button("Stop")){
                        checkForNewProcessesThread = null;
                        checkForNewProcessesRegisteredProcesses.Clear();
                    }
                    ImGui.TreePush();
                    List<Process> needToRemove = new List<Process>();
                   
                    foreach(Process process in checkForNewProcessesRegisteredProcesses){
                        if (ImGui.TreeNodeEx(process.ProcessName + ",pid: " + process.Id)){
                            DrawProcessTreeNode(process);
                            ImGui.SameLine();
                            if (ImGui.Button("Remove"))
                                needToRemove.Add(process);
                            ImGui.TreePop();
                        }
                    }

                    foreach (Process process in needToRemove)
                        checkForNewProcessesRegisteredProcesses.Remove(process);

                    ImGui.TreePop();
                }
            }

            if (checkForNewModulesThread != null){
                if (ImGui.TreeNodeEx("NewModules")){
                    ImGui.TreePop();
                    ImGui.SameLine();
                    if (ImGui.Button("Clear"))
                        checkForNewModulesRegisteredModules.Clear();
                    ImGui.SameLine();
                    if (ImGui.Button("Stop")){
                        checkForNewModulesThread = null;
                        checkForNewModulesRegisteredModules.Clear();
                    }
                    ImGui.TreePush();
                    List<Module> needToRemove = new List<Module>();

                    foreach (Module module in checkForNewModulesRegisteredModules){
                        if (ImGui.TreeNodeEx(module.module.ModuleName)){
                            if(ImGui.TreeNodeEx("Process:" + module.process.ProcessName)){
                                DrawProcessTreeNode(module.process);
                                ImGui.SameLine();
                                if (ImGui.Button("Remove"))
                                    needToRemove.Add(module);
                                ImGui.TreePop();
                            }
                        }
                    }

                    foreach (Module module in needToRemove)
                        checkForNewModulesRegisteredModules.Remove(module);

                    ImGui.TreePop();
                }
            }

            /*
            if (this.monitorProcessesWatcher == null && ImGui.Button("Start controlling new processes"))
                this.monitorProcessesWatcher = MonitorProcesses();
            else if (this.monitorProcessesWatcher != null && ImGui.Button("Stop controlling new processes")){
                this.monitorProcessesWatcher.Stop();
                this.monitorProcessesWatcher.Dispose();
                this.monitorProcessesWatcher = null;
            }
  
            ImGui.SameLine();
            */

            if(ImGui.Button("Force every window darkmode")){
                foreach(Process process in Process.GetProcesses())
                    WindowsDarkmodeUtil.SetDarkmodeAware(process.MainWindowHandle);
            }

            if (MainWindow.showAdvancedButtons){
                ImGui.SameLine();
                if (InProcessMenu.inputThread == null){
                    if (ImGui.Button("InProcessMenu-Start"))
                        InProcessMenu.StartCheckThread();
                }else{
                    if (ImGui.Button("InProcessMenu-Stop"))
                        InProcessMenu.StopCheckThread();
                }
                Manager.Tooltip("Menu that can be opened by numpad 1 and numpad 5");
            }

            if (ImGui.TreeNodeEx("Active Processes")){ 
                foreach (Process process in Process.GetProcesses().Reverse()){
                    if (ImGui.TreeNodeEx(process.ProcessName + ",pid: " + process.Id)){
                        DrawProcessTreeNode(process);
                        ImGui.TreePop();
                    }
                }
                ImGui.TreePop();
            }

            if(MainWindow.showAdvancedButtons && ImGui.TreeNodeEx("Active Modules")){
                ImGui.SameLine();
                if (ImGui.Button("Refresh"))
                    this.moduleSummaries = Manager.GetAllProcessModules();
                foreach(Manager.ModuleSummary moduleSummary in this.moduleSummaries){
                    if(ImGui.TreeNodeEx($"{moduleSummary.ModuleName} | {moduleSummary.Count}")){
                        ImGui.TextWrapped(moduleSummary.FilePath);
                        ImGui.TreePop();
                    }
                }
            }
        }

        public void TestScan(){
            foreach(Process process in Process.GetProcesses()){
                try{
                    string path = process.MainModule.FileName;
                    string name = Path.GetFileNameWithoutExtension(process.MainModule.FileName);

                    if (name.Equals("svchost") || name.Equals("sihost")){
                       
                    }
                }catch(Exception ex){
                    Console.WriteLine("No permissions for " + process.ProcessName);
                    continue;
                }
            }
        }

        public void DrawProcessTreeNode(Process process){
            try{
                if (process.HasExited)
                    ImGui.Text("Already exited.");
                ImGui.TextWrapped(process.MainModule.FileName);
            }
            catch(Exception e){
                ImGui.Text("Exited infos access is denied.");
            }

            if (ImGui.TreeNodeEx("Infos")){
                ImGui.Text(MakeInfosString(process));
                ImGui.TreePop();
            }

            if (ImGui.TreeNodeEx("Threads")){
                ImGui.SameLine();
                if (ImGui.Button("Suspend-All-Threads##" + process.MainModule.BaseAddress)){
                    foreach (ProcessThread thread in process.Threads){
                        IntPtr threadHandle = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)thread.Id);
                        SuspendThread(threadHandle);
                        CloseHandle(threadHandle);
                    }
                }

                ImGui.SameLine();
                if (ImGui.Button("Resume-All-Threads##" + process.MainModule.BaseAddress)){
                    foreach (ProcessThread thread in process.Threads){
                        IntPtr threadHandle = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)thread.Id);
                        ResumeThread(threadHandle);
                        CloseHandle(threadHandle);
                    }
                }

                foreach (ProcessThread thread in process.Threads){
                    try{
                        if (ImGui.TreeNodeEx(thread.Id.ToString())){
                          
                            try{
                                ulong cycleTime;
                                IntPtr threadHandleInformations = OpenThread(ThreadAccess.QUERY_INFORMATION, false, (uint)thread.Id);
                                QueryThreadCycleTime(threadHandleInformations, out cycleTime);
                                IntPtr threadHandle = OpenThread(ThreadAccess.ALL_ACCESS, false, (uint)thread.Id);

                                ImGui.Text("Priority: " + GetThreadPriority(threadHandle));
                                ImGui.SameLine();
                                if (ImGui.Button("-15##" + thread.Id))
                                    SetThreadPriority(threadHandle, -15);
                                ImGui.SameLine();
                                if (ImGui.Button("-1##" + thread.Id))
                                    SetThreadPriority(threadHandle, -1);
                                ImGui.SameLine();
                                if (ImGui.Button("0##" + thread.Id))
                                    SetThreadPriority(threadHandle, 0);
                                ImGui.SameLine();
                                if (ImGui.Button("1##" + thread.Id))
                                    SetThreadPriority(threadHandle, 1);
                                ImGui.SameLine();
                                if (ImGui.Button("2##" + thread.Id))
                                    SetThreadPriority(threadHandle, 2);
                                ImGui.SameLine();
                                if (ImGui.Button("15##" + thread.Id))
                                    SetThreadPriority(threadHandle, 15);

                                ImGui.Text("StartTime: " + thread.StartTime.ToString() +
                              "\nThreadCycleTime: " + cycleTime);

                            }catch(Exception ex){
                                ImGui.Text(ex.Message);
                            }

                            if (ImGui.Button("Suspend-Thread##" + thread.Id)){
                                IntPtr threadHandle = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)thread.Id);
                                SuspendThread(threadHandle);
                                CloseHandle(threadHandle);
                            }

                            ImGui.SameLine();

                            if (ImGui.Button("Resume-Thread##" + thread.Id)){
                                IntPtr threadHandle = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)thread.Id);
                                ResumeThread(threadHandle);
                                CloseHandle(threadHandle);
                            }

                            ImGui.TreePop();
                        }

                      
                    }catch (Exception ex){
                        ImGui.Text("Error");
                        Console.WriteLine(ex.Message);
                        continue;
                    }
                }
                ImGui.TreePop();
            }

            if (ImGui.TreeNodeEx("Modules")){
                try{
                    foreach (ProcessModule module in process.Modules){
                        try{
                            if (ImGui.TreeNodeEx(module.ModuleName)){
                                ImGui.Text("BaseAddres:" + module.BaseAddress +
                               "\nFileName:" + module.FileName + "\nMemorySize:" + module.ModuleMemorySize);

                                if (ImGui.Button("Free-Library##" + module.BaseAddress))
                                    FreeLibrary(module.BaseAddress);

                                ImGui.TreePop();
                            }
                           
                            ImGui.Spacing();
                        }catch (Exception ex){
                            ImGui.Text("Error");
                            Console.WriteLine(ex.Message);
                            continue;
                        }
                    }
                }
                catch (Exception ex) { ImGui.Text("Error"); }
                ImGui.TreePop();
            }

            if (ImGui.TreeNodeEx("WindowClasses")){
                List<string> classNames = GetAllWindowClasses(process);
                foreach (string className in classNames){
                    if (ImGui.TreeNodeEx(className)){
                        List<IntPtr> handles = GetWindowHandles(process.ProcessName, className);

                        ImGui.SameLine();
                        if (ImGui.Button("Screenshot-all##" + className)){
                            ScreenShotHandeList(handles);
                        }

                        foreach (IntPtr handle in handles){
                            if (ImGui.TreeNodeEx(handle + " - " + GetWindowTitle(handle) + ", Childrens: " + handles.Count)){
                                DrawWindowHandle(handle);
                                DrawChildHandles(handle);
                            }
                        }

                        ImGui.TreePop();
                    }
                }
                ImGui.TreePop();
            }

            if (ImGui.Button("Kill##" + process.Id))
                Manager.TryToKillProcess(process);

            ImGui.SameLine();

            if (ImGui.Button("Foreground"))
                Manager.ForceProcessToForeground(process);

            if (ImGui.Button("Force window darkmode"))
                WindowsDarkmodeUtil.SetDarkmodeAware(process.MainWindowHandle);

            ImGui.SameLine();

            if (ImGui.Button("Force fullscreen window"))
                Manager.ForceFullScreen(process);

            ImGui.SameLine();

            if (ImGui.Button("Force normal window"))
                Manager.ReverseFullScreen(process);
        }

        #region WindowClasses/Childrens

        public void ScreenShotHandeList(List<IntPtr> handles){
            foreach (IntPtr handle in handles){
                if (GetChildWindows(handle).Count != 0)
                    ScreenShotHandeList(GetChildWindows(handle));
                CaptureWindow(handle, $"out\\Screenshot_{handle}.png");
            }
        }

        public void DrawWindowHandle(IntPtr handle){
            ImGui.SameLine();

            if (ImGui.Button("Screenshot##" + handle))
                CaptureWindow(handle, $"out\\Screenshot_{handle}.png");

            ImGui.SameLine();

            if (ImGui.Button("Test##" + handle)){
                DrawTextOnWindow(handle, "Hello World1", 50, 50);
                IntPtr newWndProc = Marshal.GetFunctionPointerForDelegate(new WndProcDelegate(WndProc));
                SetWindowLong(handle, GWL_WNDPROC, newWndProc);
            }

            Color? bgColor = GetBackgroundColor(handle);
            ImGui.SameLine();
            ImGui.Text(bgColor.ToString());
        }

        public void DrawChildHandles(IntPtr parentHandle){
            List<IntPtr> childHandles = GetChildWindows(parentHandle);
            foreach (IntPtr childHandle in childHandles){
                if (ImGui.TreeNodeEx(childHandle + " - " + GetWindowTitle(childHandle) + ", Childrens: " + GetChildWindows(childHandle).Count + ", ClassName: " + GetClassName(childHandle))){
                    DrawWindowHandle(childHandle);
                    if (GetChildWindows(childHandle).Count != 0)
                        DrawChildHandles(childHandle);
                
                    ImGui.TreePop();;
                }
            }           
        }

        public static string GetClassName(IntPtr handle){
            if (handle == IntPtr.Zero){
                return string.Empty;
            }

            const int maxClassNameLength = 256;
            StringBuilder classNameBuilder = new StringBuilder(maxClassNameLength);

            int length = GetClassName(handle, classNameBuilder, maxClassNameLength);

            if (length == 0){
                return string.Empty;
            }

            return classNameBuilder.ToString();
        }

        public static List<IntPtr> GetChildWindows(IntPtr parentHandle){
            List<IntPtr> childHandles = new List<IntPtr>();

            EnumChildWindows(parentHandle, delegate (IntPtr hWnd, IntPtr lParam){
                childHandles.Add(hWnd);

                return true;
            }, IntPtr.Zero);

            return childHandles;
        }

        public static List<string> GetAllWindowClasses(Process process){
            List<string> classNames = new List<string>();

            EnumWindows(delegate (IntPtr hWnd, IntPtr lParam){
                GetWindowThreadProcessId(hWnd, out uint processId);

                if (processId == process.Id){
                    StringBuilder classNameBuilder = new StringBuilder(256);
                    GetClassName(hWnd, classNameBuilder, 256);
                    string className = classNameBuilder.ToString();

                    if (!classNames.Contains(className) && !string.IsNullOrEmpty(className)){
                        classNames.Add(className);
                    }
                }

                return true;
            }, IntPtr.Zero);

            return classNames;
        }

        public static List<IntPtr> GetWindowHandles(string processName, string className){
            List<IntPtr> handleList = new List<IntPtr>();
            Process[] processes = Process.GetProcessesByName(processName);
            Process proc = null;

            EnumWindows(delegate (IntPtr hWnd, IntPtr lParam){
                GetWindowThreadProcessId(hWnd, out uint processId);

                proc = processes.FirstOrDefault(p => p.Id == processId);

                if (proc != null){
                    StringBuilder classNameBuilder = new StringBuilder(256);
                    GetClassName(hWnd, classNameBuilder, 256);

                    if (classNameBuilder.ToString() == className){
                        handleList.Add(hWnd);
                    }
                }

                return true;
            }, IntPtr.Zero);

            return handleList;
        }
        private static string GetWindowTitle(IntPtr hWnd){
            const int maxTitleLength = 256;
            StringBuilder sb = new StringBuilder(maxTitleLength);
            GetWindowText(hWnd, sb, maxTitleLength);
            return sb.ToString();
        }

        public static Color? GetBackgroundColor(IntPtr hWnd)
        {
            // Get the window background brush
            IntPtr hbrBackground = GetStockObject(COLOR_WINDOW);

            if (hbrBackground == IntPtr.Zero)
            {
                return null;
            }

            // Retrieve the window class background brush
            IntPtr brush = GetClassLong(hWnd, GCLP_HBRBACKGROUND);

            if (brush == IntPtr.Zero)
            {
                brush = GetStockObject(COLOR_WINDOW);
            }

            // For some types of windows, you might need to query the control color
            IntPtr color = SendMessage(hWnd, WM_CTLCOLORSTATIC, brush, IntPtr.Zero);

            if (color == IntPtr.Zero)
            {
                color = brush;
            }

            return GetColorFromBrush(color);
        }

        private static Color? GetColorFromBrush(IntPtr brush)
        {
            if (brush == IntPtr.Zero)
            {
                return null;
            }

            // Create a Graphics object to query the color from the brush
            using (var graphics = Graphics.FromHwnd(IntPtr.Zero))
            {
                using (var solidBrush = new SolidBrush(Color.FromArgb(255, 0, 0)))
                {
                    return solidBrush.Color;
                }
            }
        }

        public static void CaptureWindow(IntPtr hWnd, string outputFile){
            try{
                if (File.Exists(outputFile))
                    File.Delete(outputFile);

                if (!Directory.Exists("\\out"))
                    Directory.CreateDirectory("\\out");

                RECT rect;
                GetWindowRect(hWnd, out rect);
                int width = rect.right - rect.left;
                int height = rect.bottom - rect.top;

                IntPtr hdcWindow = GetDC(hWnd);
                IntPtr hdcMemDC = CreateCompatibleDC(hdcWindow);
                IntPtr hBitmap = CreateCompatibleBitmap(hdcWindow, width, height);

                SelectObject(hdcMemDC, hBitmap);
                BitBlt(hdcMemDC, 0, 0, width, height, hdcWindow, 0, 0, CopyPixelOperation.SourceCopy);

                Bitmap bitmap = Image.FromHbitmap(hBitmap);

                bitmap.Save(outputFile, ImageFormat.Png);

                DeleteObject(hBitmap);
                DeleteDC(hdcMemDC);
                ReleaseDC(hWnd, hdcWindow);

                Console.WriteLine("Screenshot saved at " + outputFile + " .");
            }catch(Exception ex){
                Console.WriteLine(ex.Message);
            }
        }

        public static void DrawTextOnWindow(IntPtr hWnd, string text, int x, int y){
            IntPtr hdc = GetDC(hWnd);
            TextOut(hdc, x, y, text, text.Length);
            ReleaseDC(hWnd, hdc);
        }

        #endregion

        public string MakeInfosString(Process process){
            try{
                string infos = string.Empty;
                try{infos = infos + "Priority:" + process.BasePriority.ToString();}catch(Exception e){};
                try{infos = infos + ",Handle:" + process.Handle;}catch(Exception e){};
                try{infos = infos + ",HandleCount:" + process.HandleCount;} catch (Exception e){};
                try{infos = infos + ",MainWindowHandle:" + process.MainWindowHandle;} catch (Exception e){};
                try{infos = infos + ",\nMin,MaxWorkingSet:" + process.MinWorkingSet + "," + process.MaxWorkingSet;} catch (Exception e){};
                try{infos = infos + "\nPagedMemorySize64:" + process.PagedMemorySize64;} catch (Exception e){};
                try{infos = infos + ",PeakMemorySize64:" + process.PeakPagedMemorySize64;} catch (Exception e){};
                try{infos = infos + ",PeakVirtualMemorySize64:" + process.PeakVirtualMemorySize64;} catch (Exception e){};
                try{infos = infos + "\nPeakWorkingSet64:" + process.PeakWorkingSet64;} catch (Exception e){};
                try{infos = infos + ",PrivateMemorySize64:" + process.PrivateMemorySize64;} catch (Exception e){};
                try{infos = infos + "\nSessionId:" + process.SessionId;} catch (Exception e){};
                try{infos = infos + ",StartTime:" + process.StartTime.ToString();} catch (Exception e){};
                try{infos = infos + ",TotalProcessorTime:" + process.TotalProcessorTime;} catch (Exception e){};
                try{infos = infos + "\nWorkingSet64:" + process.WorkingSet64;} catch (Exception e){};
                return infos;
            }
            catch(Exception ex){
                Console.WriteLine(ex.Message + "\n" + ex.StackTrace);
                return string.Empty;
            }
        }

        public ManagementEventWatcher MonitorProcesses(){
            ManagementEventWatcher startWatch = new ManagementEventWatcher(
            new WqlEventQuery("SELECT * FROM Win32_ProcessStartTrace"));

            startWatch.EventArrived += new EventArrivedEventHandler(MonitorProcessStarted);
            startWatch.Start();

            return startWatch;
        }

        private async void MonitorProcessStarted(object sender, EventArrivedEventArgs e){
            int processId = Convert.ToInt32(e.NewEvent.Properties["ProcessID"].Value);
            try{
                Process process = Process.GetProcessById(processId);
                await Task.Delay(500);
             
                if(!process.HasExited)
                    Console.WriteLine("Process started: {0} (ID: {1})", process.ProcessName, process.Id);

            }catch (ArgumentException){
                   Console.WriteLine("Process started with ID: {0}, but it exited before details could be retrieved.", processId);
            }
        }

        #region DllImport

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        public delegate bool Win32Callback(IntPtr hwnd, IntPtr lParam);
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        public static extern bool EnumChildWindows(IntPtr parentHandle, Win32Callback callback, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern int SetWindowText(IntPtr hWnd, string lpString);

        [DllImport("gdi32.dll", SetLastError = true)]
        public static extern IntPtr CreateSolidBrush(uint color);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool RedrawWindow(IntPtr hWnd, IntPtr lprcUpdate, IntPtr hrgnUpdate, uint flags);

        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern IntPtr GetStockObject(int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetClassLong(IntPtr hWnd, int nIndex);

        private const int GCLP_HBRBACKGROUND = -10;

        private const int WM_CTLCOLORSTATIC = 0x0018; // Beispiel-Nachricht für Textfelder
        private const int COLOR_WINDOW = 5; // Standard-Fensterfarbe
        private const int COLOR_BTNFACE = 15; // Button-Farben

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int SetClassLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("gdi32.dll", SetLastError = true)]
        private static extern bool DeleteObject(IntPtr hObject);

        private const uint RED_COLOR = 0x00FF0000; // Rot in RGB

        private const int WM_ERASEBKGND = 0x0014;

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern uint SuspendThread(IntPtr hThread);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern uint ResumeThread(IntPtr hThread);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr hObject);

        [Flags]
        public enum ThreadAccess : int{
            TERMINATE = 0x0001,
            SUSPEND_RESUME = 0x0002,
            GET_CONTEXT = 0x0008,
            SET_CONTEXT = 0x0010,
            SET_INFORMATION = 0x0020,
            QUERY_INFORMATION = 0x0040,
            SET_THREAD_TOKEN = 0x0080,
            IMPERSONATE = 0x0100,
            DIRECT_IMPERSONATION = 0x0200,
            ALL_ACCESS = TERMINATE | SUSPEND_RESUME | GET_CONTEXT | SET_CONTEXT | SET_INFORMATION | QUERY_INFORMATION
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle, uint dwThreadId);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool QueryThreadCycleTime(IntPtr ThreadHandle, out ulong CycleTime);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool FreeLibrary(IntPtr hModule);

        [DllImport("user32.dll")]
        public static extern IntPtr GetDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll")]
        public static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, CopyPixelOperation dwRop);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

        [DllImport("gdi32.dll")]
        private static extern IntPtr SelectObject(IntPtr hdc, IntPtr h);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteDC(IntPtr hdc);

        private static IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            switch (msg)
            {
                case WM_ERASEBKGND:
                    using (Graphics g = Graphics.FromHdc(wParam))
                    {
                        g.Clear(Color.Red);
                    }
                    return (IntPtr)1;

                case WM_PAINT:
                    PAINTSTRUCT ps;
                    IntPtr hdc = BeginPaint(hWnd, out ps);
                    using (Graphics g = Graphics.FromHdc(hdc))
                    {
                        g.Clear(Color.Red);
                    }
                    EndPaint(hWnd, ref ps);
                    return IntPtr.Zero;
            }
            return CallWindowProc(oldWndProc, hWnd, msg, wParam, lParam);
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr CallWindowProc(WndProcDelegate lpPrevWndFunc, IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT{
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PAINTSTRUCT{
            public IntPtr hdc;
            public bool fErase;
            public RECT rcPaint;
            public bool fRestore;
            public bool fIncUpdate;
            [MarshalAs(System.Runtime.InteropServices.UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] rgbReserved;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("gdi32.dll")]
        public static extern bool TextOut(IntPtr hdc, int nXStart, int nYStart, string lpString, int cbString);

        private const int WM_PAINT = 0x000F;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern IntPtr BeginPaint(IntPtr hWnd, out PAINTSTRUCT lpPaint);

        [DllImport("user32.dll")]
        private static extern bool EndPaint(IntPtr hWnd, [In] ref PAINTSTRUCT lpPaint);

        private const int GWL_WNDPROC = -4;
        private static WndProcDelegate oldWndProc;

        private delegate IntPtr WndProcDelegate(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool SetThreadPriority(IntPtr hThread, int nPriority);

        [DllImport("kernel32.dll")]
        public static extern int GetThreadPriority(IntPtr hThread);

        const int THREAD_PRIORITY_HIGHEST = 2;
    

        #endregion

    }

}