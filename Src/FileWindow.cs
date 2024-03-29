﻿using ImGuiNET;
using TheIdkTool.Dialogs;
using YoutubeDLSharp;
using YoutubeDLSharp.Options;

namespace TheIdkTool.Windows{
    public class FileWindow : DrawWindow{

        public  bool[] otherToggles = new bool[1] { false};
        public  List<string> youtubeLinks = new List<string>();
        private  YoutubeDL ytdl = null;
        private  List<string> youtubeDownloadProgressList = new List<string>();
        private  List<CancellationTokenSource> youtubeDownloadTokens = new List<CancellationTokenSource>();

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
                    if(ImGui.TreeNodeEx("File names replacer")){
                        ImGui.TextWrapped("Replace names of every file." +
                         "\nExample:" +
                         "\nBefore:File1,File2" +
                         "\nTo replace: File" +
                         "\nNew text: Test"+
                         "\nAfter:Test1,Test2");
                        ImGui.Checkbox("Use as start", ref otherToggles[0]);
                        ImGui.InputText("To replace", ref inputRefs[3], 500);
                        ImGui.InputText("New text", ref inputRefs[4], 500);

                        if (ImGui.Button("Replace")){
                            string[] files = Directory.GetFiles(inputRefs[10]);
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
                    }
                }
                ImGui.TreePop();
            }

            if(ImGui.TreeNodeEx("YouTube converter(Ytdl)")){
                if(ytdl == null){
                    try{
                        ytdl = new YoutubeDL();
                        ytdl.YoutubeDLPath = Environment.ProcessPath.Replace("TheIdkTool.exe", "\\dl\\yt-dlp.exe");
                        ytdl.FFmpegPath = Environment.ProcessPath.Replace("TheIdkTool.exe", "\\dl\\ffmpeg.exe");
                    }catch(Exception e){
                        DrawUtilRender.AddDrawUtil(new WarningDialog(), "Something went wrong.\n" + e.Message);
                        return;
                    }
                }

                ImGui.InputText("##o", ref inputRefs[7], 1000);
                Manager.SelectFolderButton(ref inputRefs[7], "Output directory");

                ImGui.InputText("", ref inputRefs[6], 800);
                ImGui.SameLine();
                if (ImGui.Button("+")){
                    if (!inputRefs[6].StartsWith("https://www.youtube.com") && !inputRefs[6].StartsWith("www.youtube.com") && !inputRefs[6].StartsWith("youtube.com")){
                        DrawUtilRender.AddDrawUtil(new WarningDialog(), "That is not a valid youtube link.\n");
                        return;
                    }

                    if (!inputRefs[6].Contains("/playlist") && inputRefs[6].Contains("list=")){
                        DrawUtilRender.AddDrawUtil(new WarningDialog(), "If you want to download a playlist,\nplease use a link that does not have a video selected.\n");
                    }

                    if (inputRefs[6].StartsWith("www."))
                        inputRefs[6] = "https://" +inputRefs[6];

                    if (inputRefs[6].StartsWith("you", StringComparison.OrdinalIgnoreCase))
                        inputRefs[6] = "https://www." + inputRefs[6];

                    if (youtubeLinks.Contains(inputRefs[6])){
                        DrawUtilRender.AddDrawUtil(new WarningDialog(), "You have already added that link.\n");
                        return;
                    }

                    youtubeLinks.Add(inputRefs[6]);
                }
                ImGui.SameLine();
                ImGui.Text("New link");

                string[] tempLinks = youtubeLinks.ToArray();

                for (int i = 0; i < tempLinks.Length; i++){
                    string p = string.Empty;
                    if (tempLinks[i].Contains("/playlist"))
                        p = "(Playlist detected) - ";
                    ImGui.TextWrapped(tempLinks[i].Replace("https://www.", p));
                    ImGui.SameLine();
                    if (ImGui.Button("-##"+i)){
                        youtubeLinks.Remove(tempLinks[i]);
                    }
                }

                ytdl.OutputFolder = inputRefs[7];
                if(ImGui.Button("Download as mp4(Video)")){
                    Task.Run(() => YtTaskRunner(youtubeLinks.ToArray(), 0));
                }

                if(ImGui.Button("Download as m4a(Audio[Best Quality])")){
                    Task.Run(() => YtTaskRunner(youtubeLinks.ToArray(), 1));
                }

                if(ImGui.Button("Download as mp3(Audio)")){
                    Task.Run(() => YtTaskRunner(youtubeLinks.ToArray(), 2));
                }


                if (youtubeDownloadProgressList.Count > 0){
                    ImGui.Text("Progresses");
                    for (int i = 0; i < youtubeDownloadProgressList.Count; i++){
                        if (string.IsNullOrEmpty(youtubeDownloadProgressList[i]))
                            ImGui.Text("Starting...");
                        else
                            ImGui.Text(youtubeDownloadProgressList[i]);
                        ImGui.SameLine();
                        if (ImGui.Button("Cancel")){
                            youtubeDownloadTokens[i].Cancel();
                            youtubeDownloadTokens.RemoveAt(i);
                            youtubeDownloadProgressList.RemoveAt(i);
                            break;
                        }
                    }
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

        public  void Checker(object? o, int i){
            try{
                if (o is string)
                    youtubeDownloadProgressList[i] = (string)o;
            }catch(Exception e){}
        }

        public  async void YtTaskRunner(string[] links, int task){
            try{
                if (!Directory.Exists(inputRefs[7]))
                    Directory.CreateDirectory(inputRefs[7]);

                youtubeDownloadProgressList.Add(string.Empty);
                int i = youtubeDownloadProgressList.Count - 1;
                var progress = new Progress<DownloadProgress>(p => Checker(p.DownloadSpeed, i));
                var cts = new CancellationTokenSource();
                youtubeDownloadTokens.Add(cts);

                if (task == 0){
                    foreach (string link in youtubeLinks){
                        if (link.Contains("/playlist"))
                            await ytdl.RunVideoPlaylistDownload(link, default, default, default, default, VideoRecodeFormat.Mp4, cts.Token, progress);
                        else
                            await ytdl.RunVideoDownload(link, default, default, VideoRecodeFormat.Mp4, cts.Token, progress);
                    }
                }
                if (task == 1){
                    foreach (string link in youtubeLinks){
                        if (link.Contains("/playlist"))
                            await ytdl.RunAudioPlaylistDownload(link, default, default, default, AudioConversionFormat.M4a, cts.Token, progress);
                        else
                            await ytdl.RunAudioDownload(link, AudioConversionFormat.M4a, cts.Token, progress);
                    }
                }
                if (task == 2){
                    foreach (string link in youtubeLinks){
                        if (link.Contains("/playlist"))
                            await ytdl.RunAudioPlaylistDownload(link, default, default, default, AudioConversionFormat.Mp3, cts.Token, progress);
                        else
                            await ytdl.RunAudioDownload(link, AudioConversionFormat.Mp3, cts.Token, progress);
                    }
                }
                youtubeDownloadProgressList.RemoveAt(i);
                youtubeDownloadTokens.RemoveAt(i);
            }catch (Exception e){
                DrawUtilRender.AddDrawUtil(new WarningDialog(), "Something went wrong.\n" + e.Message);
            }
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

    }
}
