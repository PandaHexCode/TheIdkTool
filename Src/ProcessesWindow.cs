﻿using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheIdkTool.Dialogs;

namespace TheIdkTool.Windows{

    public class ProcessesWindow{

        public static string killListPath = string.Empty;
        public static Thread? checkForNewProcessesThread = null;
        public static Thread? checkForNewModulesThread = null;
        public static List<Process> checkForNewProcessesRegisteredProcesses = new List<Process>();
        public static List<Module> checkForNewModulesRegisteredModules = new List<Module>() ;
        public static bool showWindow = true;

        public class Module{
            public Process process;
            public ProcessModule module;

            public Module(Process process, ProcessModule module){
                this.process = process;
                this.module = module;
            }
        }

        public static void Draw(){
            if (!showWindow)
                return;
            ImGui.Begin("Processes");
            Manager.CheckMinWindowSize(485, 260);

            ImGui.InputText("##kl", ref killListPath, 1024);
            Manager.SelectFileButton(ref killListPath, "Kill list path");
            if(ImGui.Button("Try to kill list")){
                if(!File.Exists(killListPath))
                    DrawUtilRender.AddDrawUtil(new WarningDialog(), "The path is incorrect!");

                string[] lines = Manager.GetFileIn(killListPath).Split('\n');
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

            if (ImGui.TreeNodeEx("Active Processes")){ 
                foreach (Process process in Process.GetProcesses()){
                    if (ImGui.TreeNodeEx(process.ProcessName + ",pid: " + process.Id)){
                        DrawProcessTreeNode(process);
                        ImGui.TreePop();
                    }
                }
            }
        }

        public static void DrawProcessTreeNode(Process process){
            try{
                if (process.HasExited)
                    ImGui.Text("Already exited.");
            }catch(Exception e){
                ImGui.Text("Exited infos access is denied.");
            }
            if (ImGui.TreeNodeEx("Infos")){
                ImGui.Text(MakeInfosString(process));
                ImGui.TreePop();
            }
            if (ImGui.TreeNodeEx("Threads")){
                foreach (ProcessThread thread in process.Threads){
                    try{
                        ImGui.Text("Id:" + thread.Id.ToString() + ",Priority:" + thread.CurrentPriority +
                            ",StartTime:" + thread.StartTime.ToString());
                        if (ImGui.Button("Dispose##"+process.Id + thread.Id))
                            thread.Dispose();
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
                            ImGui.Text("Name:" + module.ModuleName + ",BaseAddres:" + module.BaseAddress +
                                "\nFileName:" + module.FileName + "\nMemorySize:" + module.ModuleMemorySize);
                            if (ImGui.Button("Dispose##" + process.Id + module.BaseAddress))
                                module.Dispose();
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
            if (ImGui.Button("Kill##" + process.Id))
                Manager.TryToKillProcess(process);
        }

        public static string MakeInfosString(Process process){
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

    }

}