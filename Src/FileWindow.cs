﻿using ImGuiNET;
using System.Diagnostics;
using TheIdkTool.Dialogs;

namespace TheIdkTool.Windows{
    public class FileWindow : DrawWindow{

        public  bool[] otherToggles = new bool[4] { false, false, true, false };

        private List<FileSystemWatcher> fileSystemWatchers = new List<FileSystemWatcher>();
        private Drive[] avaibleDrives;

        public class Drive{
            public string drive;
            public List<string> newFiles = new List<string>();
            public List<string> newChanges = new List<string>();
            public List<string> newDeletions = new List<string>();
            public List<string> newRenamed = new List<string>();
        }

        private int removeLettersInt = 0;
        public override void Draw(){
            if (ImGui.TreeNodeEx("Specific file")){
                ImGui.InputText("##p3", ref inputRefs[9], 1000);
                Manager.SelectFileButton(ref inputRefs[9], "Path");
                DrawFileTreeNode(inputRefs[9]);
                if((inputRefs[9].EndsWith(".mp3") | inputRefs[9].EndsWith(".wav") | inputRefs[9].EndsWith(".mp4")
                    | inputRefs[9].EndsWith(".m4a") | inputRefs[9].EndsWith(".ogg")) && ImGui.TreeNodeEx("Meta")){
                    DrawMetaEditerNode(inputRefs[9]);
                }
                ImGui.TreePop();
            }

            if(ImGui.TreeNodeEx("Directory tools")){
                ImGui.InputText("##p2", ref inputRefs[10], 1000);
                Manager.SelectFolderButton(ref inputRefs[10], "Path");
                if (DrawDirectoryTreeNode(inputRefs[10])){
                    if (ImGui.TreeNodeEx("Audio meta replacer(TagLib)")){
                        ImGui.TextWrapped("Replace the meta of every music file of the directory." +
                            "\nLeave empty for not replace.");
                        ImGui.Text(Manager.GetMusicFilesFromDirectory(inputRefs[10]).Length + " music files found.");
                        ImGui.InputText("Author", ref inputRefs[1], 500);
                        ImGui.InputText("Album", ref inputRefs[2], 500);
                        ImGui.InputText("##cv", ref inputRefs[0], 1000);
                        Manager.SelectFileButton(ref inputRefs[0], "Cover path");

                        if (File.Exists(inputRefs[0])){
                            try{
                                
                            }catch(Exception e){
                                ImGui.Text(e.Message);
                            }
                        }

                        if (ImGui.Button("Replace")){
                            if (inputRefs[0] != "" && !File.Exists(inputRefs[0])){
                                DrawUtilRender.AddDrawUtil(new WarningDialog(), "File path is incorrect.");
                                return;
                            }

                            if (inputRefs[0] != "" && !inputRefs[0].EndsWith(".jpg") && !inputRefs[0].EndsWith(".png")){
                                DrawUtilRender.AddDrawUtil(new WarningDialog(), "Please use .jpg or .png format.");
                                return;
                            }

                            TagLib.IPicture cover = null;
                            if (inputRefs[0] != "")
                                cover = new TagLib.Picture(inputRefs[0]);

                            foreach (string path in Manager.GetMusicFilesFromDirectory(inputRefs[10])){
                                try{
                                    using (var file = TagLib.File.Create(path)){
                                        if (inputRefs[0] != "")
                                            file.Tag.Pictures = new TagLib.IPicture[] { cover };
                                        if (inputRefs[1] != "")
                                            file.Tag.Performers = new string[] { inputRefs[1] };
                                        if (inputRefs[2] != "")
                                            file.Tag.Album = inputRefs[2];
                                        file.Save();
                                    }
                                }catch(Exception e){
                                    Console.WriteLine("Failed at " + path);
                                    continue;
                                }
                            }

                            DrawUtilRender.AddDrawUtil(new WarningDialog(), "Finished.");
                        }

                        ImGui.TreePop();
                    }
                    if (ImGui.TreeNodeEx("File names replacer")){
                        ImGui.TextWrapped("Replace names of every file." +
                         "\nExample:" +
                         "\nBefore:File1,File2" +
                         "\nTo replace: File" +
                         "\nNew text: Test"+
                         "\nAfter:Test1,Test2");
                        ImGui.Checkbox("Use as start", ref otherToggles[0]);
                        ImGui.InputText("To replace", ref inputRefs[3], 500);
                        ImGui.InputText("New text", ref inputRefs[4], 500);
                        ImGui.Checkbox("Subfolders", ref otherToggles[1]);

                        if (ImGui.Button("Replace")){
                            string[] files = null;
                            if (otherToggles[1])
                                files = Manager.GetEveryFileName(inputRefs[10]);
                            else
                                files = Directory.GetFiles(inputRefs[10]);

                            foreach (string filePath in files){
                                try{
                                    if (otherToggles[0])
                                        File.Move(filePath, filePath.Replace(filePath.Substring(filePath.IndexOf(inputRefs[3])), inputRefs[4]));
                                    else
                                        File.Move(filePath, filePath.Replace(inputRefs[3], inputRefs[4]));
                                }catch(Exception e) { continue; }
                            }
                            DrawUtilRender.AddDrawUtil(new WarningDialog(), "Finished.");
                        }

                        ImGui.Text("Add text to end of the file names.");
                        ImGui.InputText("Text", ref inputRefs[5], 500);
                        if (ImGui.Button("Start")){
                            string[] files = Directory.GetFiles(inputRefs[10]);
                            foreach (string filePath in files){
                                try{
                                    File.Move(filePath, filePath + inputRefs[5]);
                                }
                                catch (Exception e) { continue; }
                            }
                            DrawUtilRender.AddDrawUtil(new WarningDialog(), "Finished.");
                        }
                        ImGui.TreePop();
                    }
                    if (ImGui.TreeNodeEx("Remove letters from start (file name)")){
                        ImGui.InputInt("Count", ref removeLettersInt);

                        if (ImGui.Button("Start")){
                            string[] files = Directory.GetFiles(inputRefs[10]);
                            foreach (string filePath in files){
                                try{
                                    string directory = Path.GetDirectoryName(filePath);
                                    string fileName = Path.GetFileName(filePath);

                                    if (fileName.Length > 4){
                                        string newFileName = fileName.Substring(removeLettersInt);
                                        string newFilePath = Path.Combine(directory, newFileName);

                                        File.Move(filePath, newFilePath);
                                    }
                                }catch (Exception e) { continue; }
                            }
                            DrawUtilRender.AddDrawUtil(new WarningDialog(), "Finished.");
                        }

                        ImGui.TreePop();
                    }
                }
                ImGui.TreePop();
            }

            if(ImGui.TreeNode("File Listener")){
                if (this.avaibleDrives == null | ImGui.Button("Refresh drives"))
                    this.avaibleDrives = Manager.GetEveryFileDrive();

                ImGui.SameLine();
                ImGui.Checkbox("Spaces", ref this.otherToggles[2]);
                ImGui.Checkbox("Ignore temp & cache files", ref this.otherToggles[3]);

                foreach (Drive drive in this.avaibleDrives){
                    if (ImGui.TreeNodeEx(drive.drive)){
                        FileSystemWatcher watcher = IsFileWatcherActive(drive.drive);
                        if (watcher != null){
                            if (ImGui.Button("Stop listening")){
                                this.fileSystemWatchers.Remove(watcher);
                                watcher.Dispose();
                            }
                            DrawListeningNode("New files", ref drive.newFiles);
                            DrawListeningNode("New changes", ref drive.newChanges);
                            DrawListeningNode("New deletions", ref drive.newDeletions);
                            DrawListeningNode("New renamed", ref drive.newRenamed);
                        }else if(watcher == null && ImGui.Button("Start listening")){
                            this.fileSystemWatchers.Add(MonitorDirectory(drive, 0));
                        }
                        ImGui.TreePop();
                    }
                }

                ImGui.TreePop();
            }
        }

        public void DrawListeningNode(string label, ref List<string> array){
            if (ImGui.TreeNodeEx(label)){
                if (ImGui.Button("Clear"))
                    array.Clear();
                for (int i = 0; i < array.Count; i++){
                    string file = array[i];
                    if (this.otherToggles[3] && file.Contains(".tmp", StringComparison.OrdinalIgnoreCase)
                        || file.Contains(".temp", StringComparison.OrdinalIgnoreCase) || file.Contains("cache", StringComparison.OrdinalIgnoreCase))
                        continue;

                    ImGui.TextWrapped(file);
                    if (this.otherToggles[2])
                        ImGui.Spacing();
                }
                ImGui.TreePop();
            }
        }

        public  void DrawMetaEditerNode(string path){
            TagLib.File file = TagLib.File.Create(path);
            string authors = string.Empty;
            foreach (string author in file.Tag.Performers)
                authors = authors + author + ",";
            ImGui.TextWrapped("Album:" + file.Tag.Album + "\nAuthor(s):" + authors);
            ImGui.TreePop();
        }

        public  bool DrawFileTreeNode(string path){
            ImGui.Text("Path:" + path);
            if (!File.Exists(path)){
                ImGui.Text("Path is incorrect.");
                return false;
            }
            if (ImGui.TreeNodeEx("Infos")){
                try{
                    ImGui.TextWrapped("CreationTime:" + File.GetCreationTimeUtc(path).ToString() 
                        + "\nLastAccessTime:" + File.GetLastAccessTimeUtc(path).ToString() 
                        + "\nLastWriteTime:" + File.GetLastWriteTimeUtc(path));
                }catch(Exception e){ImGui.Text("Error");};
                ImGui.TreePop();
            }
            if (ImGui.TreeNodeEx("Try to read")){
                if(ImGui.TreeNodeEx("As string")){
                    try{
                        string fileIn = Manager.GetFileIn(path);
                        ImGui.InputTextMultiline(string.Empty, ref fileIn, uint.MaxValue, new System.Numerics.Vector2(800, 500));
                    }catch(Exception ex){ImGui.Text("Error");};
                    ImGui.TreePop();
                }
                if (ImGui.TreeNodeEx("As bytes")){
                    try{
                        byte[] bytes = File.ReadAllBytes(path);
                        string str = string.Empty;
                        int i = 0;
                        int y = 100;
                        foreach (byte b in bytes){
                            str = str + b + " ";
                            if (i > 5){
                                str = str + "\n";
                                i = 0;
                            }
                            i++;
                            y = y + 1;
                        }
                        ImGui.InputTextMultiline(string.Empty, ref str, uint.MaxValue, new System.Numerics.Vector2(230, y));
                    }catch (Exception ex){ImGui.Text("Error");};
                    ImGui.TreePop();
                }
                ImGui.TreePop();
            }
            DrawEncryptionNode(path);
            return true;
        }

        public  bool DrawDirectoryTreeNode(string path){
            ImGui.Text("Path:" + path);
            if (!Directory.Exists(path)){
                ImGui.Text("Path is incorrect.");
                return false;
            }
            ImGui.Text("Files: " + Directory.GetFiles(path).Length);
            DrawEncryptionNode(path);
            return true;
        }

        public  void DrawEncryptionNode(string path){
            if (ImGui.TreeNodeEx("Encryption/Decryption")){ 
                bool isDirectory = false;
                if (Directory.Exists(path))
                    isDirectory = true;
                ImGui.Text("Is directory: " + isDirectory);

                try{
                    if (ImGui.TreeNodeEx("Advanced(Cryptography)")){
                        ImGui.InputText("Key", ref inputRefs[8], 500);
                        ImGui.SameLine();
                        if (ImGui.Button("Randomize"))
                            inputRefs[8] = Manager.GetKey();

                        if (ImGui.Button("Encrypt")){
                            if (isDirectory){
                                foreach (string file in Directory.GetFiles(path))
                                    Manager.EncryptFile(file, inputRefs[8]);
                            }else 
                                Manager.EncryptFile(path, inputRefs[8]);

                            DrawUtilRender.AddDrawUtil(new WarningDialog(), "Finished.");
                        }
                        ImGui.SameLine();
                        if (ImGui.Button("Decrypt")){
                            if (isDirectory){
                                foreach (string file in Directory.GetFiles(path))
                                    Manager.DecryptFile(file, inputRefs[8]);
                            }else
                                Manager.DecryptFile(path, inputRefs[8]);
                            DrawUtilRender.AddDrawUtil(new WarningDialog(), "Finished.");
                        }

                        ImGui.TreePop();
                    }
                }catch(Exception ex){
                    DrawUtilRender.AddDrawUtil(new WarningDialog(), "Something went wrong.\n" + ex.Message);
                }
                ImGui.TreePop();
            }
        }

        public static void MonitorNewFileTrackAdd(ref List<string> array, string name){
            int processId = Manager.GetProcessIdFromFilePath(name);
            if (processId == 0)
                array.Add(name + ", From: " + "denied");
            else{
                Process process = Process.GetProcessById(processId);
                array.Add(name + ", From: " + process.ProcessName);
            }
        }
        
        public static FileSystemWatcher MonitorDirectory(Drive drive, int type) {
            FileSystemWatcher watcher = new FileSystemWatcher();

            watcher.Path = drive.drive;
            watcher.Filter = "*.*";

            watcher.EnableRaisingEvents = true;
            watcher.IncludeSubdirectories = true;

            watcher.Created += (sender, e) => {
                MonitorNewFileTrackAdd(ref drive.newFiles, e.Name);
            };

            watcher.Changed += (sender, e) => {
                MonitorNewFileTrackAdd(ref drive.newChanges, e.Name);
            };

            watcher.Deleted += (sender, e) => {
                MonitorNewFileTrackAdd(ref drive.newDeletions, e.Name);
            };

            watcher.Renamed += (sender, e) => {
                MonitorNewFileTrackAdd(ref drive.newRenamed, e.OldName + " to " + e.Name);
            };

            return watcher;
        }

        public FileSystemWatcher IsFileWatcherActive(string path){
            foreach(FileSystemWatcher watcher in this.fileSystemWatchers){
                if (watcher.Path.Equals(path))
                    return watcher;
            }

            return null;
        }

    }
}
