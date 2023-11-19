﻿using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using TheIdkTool.Windows;
using static System.Net.Mime.MediaTypeNames;
using Microsoft.Win32;
using TheIdkTool.Dialogs;
using System.Numerics;
using ImGuiNET;
using static TheIdkTool.Windows.TodoWindow;
using System.Security.Cryptography;
using FolderBrowserEx;
using System.Windows.Forms;
using Silk.NET.Core;
using System.Drawing.Imaging;
using System.Drawing;
using Image = SixLabors.ImageSharp.Image;
using System.Net;

namespace TheIdkTool{

    public class Manager
    {

        [DllImport("user32.dll")]
        public static extern int FindWindow(string ClassName, string WindowName);

        [DllImport("user32.dll")]
        public static extern int SendMessage(int hWnd, uint Msg, int wParam, int lParam);

        public const int WM_SYSCOMMAND = 0x0112;
        public const int SC_CLOSE = 0xF060;

        public static bool IsAdministrator()
        {
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        public static void CheckVersion(){
            try{
                string url = "https://raw.githubusercontent.com/PandaHexCode/TheIdkTool/master/VersionIndex?token=GHSAT0AAAAAACKQHTA7HASQYGXGRRAYBL4MZK2MCCA";
                using (WebClient client = new WebClient()){
                    string version = client.DownloadString(url);

                    version = version.Trim();

                    float versionFloat = StringToFloat(version);
                    if(versionFloat > MainWindow.currentVersion)
                        DrawUtilRender.AddDrawUtil(new WarningDialog(), "Your version is outdated.\nCheckout the newest release on https://github.com/PandaHexCode/TheIdkTool");
                }
            }catch (Exception ex){
                DrawUtilRender.AddDrawUtil(new WarningDialog(), "Something went wrong.\n" + ex.Message);
            }
        }

        public static RawImage GetRawImage(string path){
            if (!File.Exists(path))
                throw new FileNotFoundException(path + " not found.");

            using (var image2 = Image.Load<Rgba32>(path)){
                byte[] pixelData = new byte[image2.Width * image2.Height * 4];

                image2.CopyPixelDataTo(pixelData);

                Memory<byte> memory = new Memory<byte>(pixelData);
                RawImage image = new RawImage(553, 492, memory);
                return image;
            }
        }

        public static ReadOnlySpan<RawImage> GetRawimageSpan(string path){
            if (!File.Exists(path))
                throw new FileNotFoundException(path + " not found.");
            using (var image2 = Image.Load<Rgba32>(path)){
                byte[] pixelData = new byte[image2.Width * image2.Height * 4];

                image2.CopyPixelDataTo(pixelData);

                Memory<byte> memory = new Memory<byte>(pixelData);
                RawImage image = new RawImage(553, 492, memory);
                var imageSpan = new ReadOnlySpan<RawImage>(new[] { image });
                return imageSpan;
            }
        }

        public static void SelectFolderButton(ref string input, string text){
            ImGui.SameLine();
            if (ImGui.Button("..")){
                string output = SelectFolder(input);
                if (output == string.Empty)
                    output = input;
                input = output;
            }
            ImGui.SameLine();
            ImGui.Text(text);
        }

        public static string SelectFolder(string trySetDefault){
            FolderBrowserEx.FolderBrowserDialog folderBrowserDialog = new FolderBrowserEx.FolderBrowserDialog();
            folderBrowserDialog.Title = "Select a folder";
            if (Directory.Exists(trySetDefault))
                folderBrowserDialog.InitialFolder = trySetDefault;
            else
                folderBrowserDialog.InitialFolder = @"C:\";
            folderBrowserDialog.AllowMultiSelect = false;
            string result = string.Empty;
            if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK){
                result += folderBrowserDialog.SelectedFolder;
                return result;
            }else
                return string.Empty;
        }

        public static void SelectFileButton(ref string input, string text){
            ImGui.SameLine();
            if (ImGui.Button("..")){
                string temp = input;
                Thread fileSelectThread = new Thread(() => SelectFile(ref temp, temp));
                fileSelectThread.SetApartmentState(ApartmentState.STA);//Because OpenFileDialog freezes without being in a STA
                fileSelectThread.Start();
                fileSelectThread.Join();
                if (temp == string.Empty)
                    temp = input;
                input = temp;
            }
            ImGui.SameLine();
            ImGui.Text(text);
        }


        public static void SelectFile(ref string output, string trySetDefault){
            string selectedFilePath = string.Empty;
            using (System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog()){
                openFileDialog.Filter = "|*.*";
                openFileDialog.ShowHiddenFiles = true;
                if (Directory.Exists(Path.GetDirectoryName(trySetDefault)))
                    openFileDialog.InitialDirectory = Path.GetDirectoryName(trySetDefault);
                else
                    openFileDialog.InitialDirectory = @"C:\";

                if (openFileDialog.ShowDialog() == DialogResult.OK){
                    selectedFilePath = openFileDialog.FileName;
                }
            }
            output = selectedFilePath;
        }

        public static void AddToStartup(string appName, string appPath)
        {
            try
            {
                string startupFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), appName + ".url");

                using (StreamWriter writer = new StreamWriter(startupFolderPath))
                {
                    writer.WriteLine("[InternetShortcut]");
                    writer.WriteLine("URL=file:///" + appPath.Replace('\\', '/'));
                    writer.WriteLine("IconIndex=0");
                    writer.WriteLine("IconFile=" + appPath.Replace('\\', '/'));
                    writer.Flush();
                }
            }
            catch (Exception ex)
            {
                DrawUtilRender.AddDrawUtil(new WarningDialog(), "Something went wrong.\n");
            }
        }

        public static void CheckMinWindowSize(int width, int height)
        {
            if (ImGui.GetWindowWidth() < width)
                ImGui.SetWindowSize(new System.Numerics.Vector2(width, ImGui.GetWindowHeight()));
            else if (ImGui.GetWindowHeight() < height)
                ImGui.SetWindowSize(new System.Numerics.Vector2(ImGui.GetWindowHeight(), height));
        }

        public static void DebugWindowHelper()
        {
            ImGui.Text(ImGui.GetWindowWidth() + " " + ImGui.GetWindowHeight());
        }

        public static void Tooltip(string tooltip)
        {
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.SetTooltip(tooltip);
                ImGui.EndTooltip();
            }
        }

        public static void RemoveFromStartup(string appName)
        {
            try
            {
                string startupFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), appName + ".url");

                if (File.Exists(startupFolderPath))
                {
                    File.Delete(startupFolderPath);
                }
            }
            catch (Exception ex)
            {
                DrawUtilRender.AddDrawUtil(new WarningDialog(), "Something went wrong.\n");
            }
        }

        public static void ChangePositionInList<T>(List<T> list, T obj, int newPosition){
            if (list.Contains(obj)){
                list.Remove(obj);

                list.Insert(newPosition, obj);
            }else{
            }
        }

        public static bool IsInStartup(string appName){
            string startupFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Startup), appName + ".url");

            return File.Exists(startupFolderPath);
        }

        public static Vector4 HexToVector4(string hex){
            if (hex.StartsWith("#")){
                hex = hex.Substring(1);
            }

            if (int.TryParse(hex, System.Globalization.NumberStyles.HexNumber, null, out int colorValue)){
                byte red = (byte)((colorValue >> 16) & 0xFF);
                byte green = (byte)((colorValue >> 8) & 0xFF);
                byte blue = (byte)(colorValue & 0xFF);
                byte alpha = 255;

                return new Vector4(red / 255.0f, green / 255.0f, blue / 255.0f, alpha / 255.0f);
            }else{
                Console.WriteLine("Invalid hex: " + hex);
                return Vector4.One;
            }
        }

        public static void TryToKillProcess(string processName){
            Process[] prc = Process.GetProcessesByName(processName);
            foreach (Process process in prc)
                TryToKillProcess(process);
        }

        public static void TryToKillProcess(Process process){
            try{
                process.Kill();
            }catch (Exception ex){
                try{
                    int HWND = FindWindow(null, process.MainWindowTitle);

                    SendMessage(HWND, WM_SYSCOMMAND, SC_CLOSE, 0);
                }catch (Exception exp){
                    Console.WriteLine("Can't kill " + process.ProcessName + "!");
                    Console.WriteLine(exp.Message);
                    Console.WriteLine(exp.StackTrace);
                }
            }
        }

        public static void CheckForNewProcesses(){
            List<int> savedProcessIds = new List<int>();
            foreach (Process process in Process.GetProcesses()){
                savedProcessIds.Add(process.Id);
            }

            while (true){
                if (ProcessesWindow.checkForNewProcessesThread == null){
                    Console.WriteLine("Stopped check for new processes thread.");
                    return;
                }

                List<int> currentProcessIds = new List<int>();
                foreach (Process process in Process.GetProcesses()){
                    int processId = process.Id;
                    if (!savedProcessIds.Contains(processId)){
                        ProcessesWindow.checkForNewProcessesRegisteredProcesses.Add(process);
                    }
                    currentProcessIds.Add(processId);
                }

                List<int> terminatedProcessIds = savedProcessIds.Except(currentProcessIds).ToList();
                foreach (int processId in terminatedProcessIds){
                    savedProcessIds.Remove(processId);
                }

                List<int> newProcessIds = currentProcessIds.Except(savedProcessIds).ToList();
                savedProcessIds.AddRange(newProcessIds);

                Thread.Sleep(100);
            }
        }

        public static void CheckForNewModules(){
            List<ProcessesWindow.Module> savedProcessModules = new List<ProcessesWindow.Module>();
            foreach (Process process in Process.GetProcesses()){
                try{
                    foreach (ProcessModule module in process.Modules)
                        savedProcessModules.Add(new ProcessesWindow.Module(process, module));
                }catch(Exception ex){ continue; }
            }

            while (true){
                if (ProcessesWindow.checkForNewModulesThread == null){
                    Console.WriteLine("Stopped check for new modules thread.");
                    return;
                }
                foreach (Process process in Process.GetProcesses()){
                    try
                    {
                        bool processAlreadyThere = false;
                        foreach (ProcessesWindow.Module module1 in savedProcessModules){
                            if (module1.process == process)
                                processAlreadyThere = true;
                        }

                        if (!processAlreadyThere)
                            continue;

                        foreach (ProcessModule module in process.Modules)
                        {
                            foreach (ProcessesWindow.Module module1 in savedProcessModules)
                            {
                                if (module1.module == module)
                                    continue;
                                else
                                {
                                    ProcessesWindow.Module newModule = new ProcessesWindow.Module(process, module);
                                    ProcessesWindow.checkForNewModulesRegisteredModules.Add(newModule);
                                    savedProcessModules.Add(newModule);
                                }
                            }
                        }
                    }catch (Exception ex) { continue; }
                }

                foreach (Process process in Process.GetProcesses()){
                    bool processAlreadyThere = false;
                    foreach (ProcessesWindow.Module module1 in savedProcessModules){
                        if (module1.process == process)
                            processAlreadyThere = true;
                    }
                    if (!processAlreadyThere){
                        try{
                            foreach (ProcessModule module in process.Modules)
                                savedProcessModules.Add(new ProcessesWindow.Module(process, module));
                        }catch(Exception ex) { continue; }
                    }
                }
                Thread.Sleep(100);
            }
        }

        public static string GetFileIn(string path){
            if (File.Exists(path)){
                StreamReader readStm2 = new StreamReader(path);
                string fileIn2 = readStm2.ReadToEnd();
                readStm2.Close();

                return fileIn2;
            }else
                return string.Empty;
        }

        public static string[] GetMusicFilesFromDirectory(string directory){
            string[] musicFiles = Directory.GetFiles(directory, "*.*")
                    .Where(file => file.ToLower().EndsWith(".mp3") ||
                                   file.ToLower().EndsWith(".wav") ||
                                   file.ToLower().EndsWith(".m4a"))
                    .ToArray();
            return musicFiles;
        }

        public static void SaveFile(string path, string content){
            if (File.Exists(path))
                File.Delete(path);

            StreamWriter sw = new StreamWriter(path, true, Encoding.UTF8);
            sw.Write(content);
            sw.Close();
        }

        public static float StringToFloat(string str){
            float fl = 0f;
            float.TryParse(str, out fl);
            return fl;
        }

        public static int StringToInt(string str){
            int i = 0;
            int.TryParse(str, out i);
            return i;
        }

        public static bool StringToBool(string str){
            bool b = false;
            bool.TryParse(str, out b);
            return b;
        }

    }

}