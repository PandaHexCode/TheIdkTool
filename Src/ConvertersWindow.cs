using ImGuiNET;
using NAudio.Wave;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using TheIdkTool.Dialogs;
using Xabe.FFmpeg;
using YoutubeDLSharp;
using YoutubeDLSharp.Options;

namespace TheIdkTool.Windows{

    public class ConvertersWindow : DrawWindow{
        
        /*YouTube*/
        public List<string> youtubeLinks = new List<string>();
        private YoutubeDL ytdl = null;
        private List<string> youtubeDownloadProgressList = new List<string>();
        private List<CancellationTokenSource> youtubeDownloadTokens = new List<CancellationTokenSource>();

        /*FFmpeg*/
        private bool hasInitFFmpeg = false;
        private bool isInFFConversion = false;
        private CancellationTokenSource ffCancelToken;
        private string ffProgress = string.Empty;
        private enum OptimizeParm { Nvidia, Intel, Amd, None}
        private OptimizeParm currentOptimizeParm = OptimizeParm.Intel;
        private int cpuThreads = 4;
        private int crf = 0;
        private int gop = 0;

        public bool[] otherToggles = new bool[8] { false, false, false, false, false, false, false, false};

        public override void Draw(){
            ImGui.TextWrapped("File Converters(FFmpeg)");

            ImGui.Spacing();

            DrawFFmpegTreeNode();

            ImGui.Spacing();

            ImGui.NewLine();
            ImGui.TextWrapped("YouTube Converter(Ytdl)");
            ImGui.Spacing();
            DrawYouTubeTreeNode();
        }

        private void CheckOptimizeParm(OptimizeParm optimizeParm, int i, string name){
            if (this.currentOptimizeParm == optimizeParm)
                this.otherToggles[i] = true;
            else
                this.otherToggles[i] = false;

            if (ImGui.Checkbox(name, ref this.otherToggles[i]))
                this.currentOptimizeParm = optimizeParm;
        }

        #region YouTube
        public void DrawYouTubeTreeNode(){
            if (ytdl == null){
                string dlPath = Environment.ProcessPath.Replace("TheIdkTool.exe", "\\dl\\yt-dlp.exe");
                string FFmpegPath = Environment.ProcessPath.Replace("TheIdkTool.exe", "\\dl\\ffmpeg.exe");

                if (!File.Exists(dlPath))
                    ImGui.TextWrapped("File " + dlPath + " can't be found!\nPlease try reinstall the programm.");
                if(!File.Exists(FFmpegPath))
                    ImGui.TextWrapped("File " + FFmpegPath + " can't be found!\nPlease try reinstall the programm.");

                try{
                    ImGui.Text("Loading...");
                    ytdl = new YoutubeDL();
                    ytdl.YoutubeDLPath = Environment.ProcessPath.Replace("TheIdkTool.exe", "\\dl\\yt-dlp.exe");
                    ytdl.FFmpegPath = Environment.ProcessPath.Replace("TheIdkTool.exe", "\\dl\\ffmpeg.exe");
                }catch (Exception e){
                    DrawUtilRender.AddDrawUtil(new WarningDialog(), "Something went wrong.\n" + e.Message);
                    return;
                }
            }

            ImGui.InputText("##o", ref inputRefs[7], 1000);
            Manager.SelectFolderButton(ref inputRefs[7], "Output directory", 1);

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
                    inputRefs[6] = "https://" + inputRefs[6];

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
                if (ImGui.Button("-##" + i)){
                    youtubeLinks.Remove(tempLinks[i]);
                }
            }

            ytdl.OutputFolder = inputRefs[7];

            if (ImGui.Button("Download as m4a(Audio[Best Quality])")){
                Task.Run(() => YtTaskRunner(youtubeLinks.ToArray(), 1));
            }

            ImGui.SameLine();

            if (ImGui.Button("Download as mp3(Audio)")){
                Task.Run(() => YtTaskRunner(youtubeLinks.ToArray(), 2));
            }

            if (ImGui.Button("Download as mp4(Video)")){
                Task.Run(() => YtTaskRunner(youtubeLinks.ToArray(), 0));
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

        public void YtChecker(object? o, int i){
            try{
                if (o is string)
                    youtubeDownloadProgressList[i] = (string)o;
            }catch(Exception e){}
        }

        public async void YtTaskRunner(string[] links, int task){
            try{
                if (!Directory.Exists(inputRefs[7]))
                    Directory.CreateDirectory(inputRefs[7]);

                youtubeDownloadProgressList.Add(string.Empty);
                int i = youtubeDownloadProgressList.Count - 1;
                var progress = new Progress<DownloadProgress>(p => YtChecker(p.DownloadSpeed, i));
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
            }catch (Exception ex){
                DrawUtilRender.AddDrawUtil(new WarningDialog(), "Something went wrong.\n" + ex.Message);
            }
        }
        #endregion

        #region FFmpeg
        public void DrawFFmpegTreeNode(){
            ImGui.Text("GPU-Optimizer");
            CheckOptimizeParm(OptimizeParm.Nvidia, 0, "NVIDIA NVENC");
            ImGui.SameLine();
            CheckOptimizeParm(OptimizeParm.Intel, 1, "Intel Quick Sync Video");
            ImGui.SameLine();
            CheckOptimizeParm(OptimizeParm.Amd, 2, "AMD VCE");
            ImGui.SameLine();
            CheckOptimizeParm(OptimizeParm.None, 3, "None");

            ImGui.Spacing();

            if (ImGui.Checkbox("Preset veryfast (Lower quality)", ref otherToggles[4]))
                this.otherToggles[5] = false;
            ImGui.SameLine();
            if (ImGui.Checkbox("Preset veryslow (Best quality)", ref otherToggles[5]))
                this.otherToggles[4] = false;

            ImGui.Spacing();
            ImGui.PushItemWidth(70);
            ImGui.InputInt("CPU-Threads", ref this.cpuThreads);
            ImGui.PopItemWidth();

            ImGui.SameLine();
            ImGui.PushItemWidth(70);
            ImGui.InputInt("CRF", ref this.crf, 1, 1);
            Manager.Tooltip("Constant Rate Factor (Constant Quality):\n0 = Best Quality (No Loss, Large File)\n51 = Lowest Quality (Smaller File)");
            if (this.crf > 51)
                this.crf = 51;
            if (this.crf < 0)
                this.crf = 0;
            ImGui.PopItemWidth();

            ImGui.SameLine();
            ImGui.PushItemWidth(70);
            ImGui.InputInt("GOP", ref this.gop, 1, 1);
            Manager.Tooltip("Group of Pictures Size:\n1 = Best Quality (Large File)\n50 = Lower Quality (Smaller File)");
            if (this.gop > 50)
                this.gop = 50;
            if (this.gop < 1)
                this.gop = 1;
            ImGui.PopItemWidth();

            ImGui.Spacing();

            ImGui.InputText("##p3", ref inputRefs[0], 1000);
            Manager.SelectFileButton(ref inputRefs[0], "Input path");
            ImGui.InputText("##p2", ref inputRefs[1], 1000);
            Manager.SelectFolderButton(ref inputRefs[1], "Output directory");

            if (!this.hasInitFFmpeg){
                ImGui.TextWrapped("Loading...");
                this.cpuThreads = Environment.ProcessorCount * 2;

                try{
                    string FFmpegPath = Environment.ProcessPath.Replace("TheIdkTool.exe", "\\dl\\");
                    FFmpeg.SetExecutablesPath(FFmpegPath);
                    this.hasInitFFmpeg = true;
                }catch(Exception ex){
                    DrawUtilRender.AddDrawUtil(new WarningDialog(), "Something went wrong.\n" + ex.Message);
                }

                return;
            }

            if (this.isInFFConversion){
                ImGui.TextWrapped("Converting...");
                ImGui.TextWrapped(this.ffProgress);

                if (ImGui.Button("Cancel")){
                    this.ffCancelToken.Cancel();
                    this.isInFFConversion = false;
                }
            }else{
                ImGui.Spacing();
                ImGui.Text("Video");

                DrawConvertButton("mp4");
                ImGui.SameLine();
                DrawConvertButton("mkv");
                ImGui.SameLine();
                DrawConvertButton("avi");
                ImGui.SameLine();
                DrawConvertButton("mov");

                DrawConvertButton("wmv");
                ImGui.SameLine();
                DrawConvertButton("flv");
                ImGui.SameLine();
                DrawConvertButton("webm");
                ImGui.SameLine();
                DrawConvertButton("mpeg");

                ImGui.Spacing();
                ImGui.Text("Audio");

                DrawConvertButton("mp3");
                ImGui.SameLine();
                DrawConvertButton("aac");
                ImGui.SameLine();
                DrawConvertButton("wav");
                ImGui.SameLine();

                DrawConvertButton("flac");
                DrawConvertButton("ogg");
                ImGui.SameLine();
                DrawConvertButton("wma");
                ImGui.SameLine();
                DrawConvertButton("m4a");
            }
        }

        public void DrawConvertButton(string format){
            if (ImGui.Button("To ." + format))
                StartFFConversion("." + format);
        }

        public void StartFFConversion(string outputFormat){
            if (this.isInFFConversion)
                Console.WriteLine("Multiple conversions.");

            string outputPath = this.inputRefs[1] + "\\" + Path.GetFileNameWithoutExtension(this.inputRefs[0]) + outputFormat;

            if (File.Exists(outputPath))
                File.Delete(outputPath);

            Task.Run(() => FFConversion(outputPath));
        }


        public async void FFConversion(string outputFile){
            this.isInFFConversion = true;
            this.ffProgress = string.Empty;
            try{
                IConversion conversion = null;

                switch (this.currentOptimizeParm){

                    default:
                    case OptimizeParm.None:
                        conversion = await FFmpeg.Conversions.FromSnippet.Convert(this.inputRefs[0], outputFile);
                        break;

                    case OptimizeParm.Nvidia:
                        conversion = conversion = FFmpeg.Conversions.New()
                        .AddParameter($"-hwaccel cuda -i {this.inputRefs[0]}")
                        .AddParameter($"-c:v h264_nvenc");
                        break;

                    case OptimizeParm.Intel:
                        conversion = FFmpeg.Conversions.New()
                        .AddParameter($"-hwaccel qsv -i {this.inputRefs[0]}")
                        .AddParameter($"-c:v h264_qsv");
                        break;

                    case OptimizeParm.Amd:
                        conversion = FFmpeg.Conversions.New()
                        .AddParameter($"-hwaccel vdpau -i {this.inputRefs[0]}")
                        .AddParameter($"-c:v h264_vaapi");
                        break;
                }

                conversion.AddParameter($"-threads {this.cpuThreads}");

                if (this.otherToggles[4])
                    conversion.AddParameter($"-preset veryfast");

                if (this.otherToggles[5])
                    conversion.AddParameter($"-preset veryslow");

                conversion.AddParameter($"-crf {this.crf}");
                conversion.AddParameter($"-bf 4");
                conversion.AddParameter($"-g {this.gop}");
                conversion.AddParameter(outputFile);

                this.ffCancelToken = new CancellationTokenSource();
                CancellationToken cancellationToken = this.ffCancelToken.Token;

                conversion.OnProgress += (sender, args) =>{
                    this.ffProgress = "Progress: " + args.Percent + "%, Time: " + args.Duration + " from " + args.TotalLength;
                };

                await conversion.Start(cancellationToken);

                this.isInFFConversion = false;
            }catch (Xabe.FFmpeg.Exceptions.ConversionException ce){
                Console.WriteLine("t");
                DrawUtilRender.AddDrawUtil(new WarningDialog(), "Something went wrong.\n" + ce.Message);
            }catch (Exception ex){
                Console.WriteLine("t");
                DrawUtilRender.AddDrawUtil(new WarningDialog(), "Something went wrong.\n" + ex.Message);
                this.isInFFConversion = false;
            }
        }
        #endregion

    }

}