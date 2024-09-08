using Silk.NET.Windowing;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using ImGuiNET;
using Color = System.Drawing.Color;
using Silk.NET.Maths;
using TheIdkTool.Dialogs;
using System.Numerics;
using System.Diagnostics;

namespace TheIdkTool.Windows{
    public class ContextFileWindow{

        public static GL gl = null;

        private Vector4[] colors = new Vector4[10];

        private string currentCode = string.Empty;

        public bool[] extraCode = [false, false, false, false];

        public static string path;
        public static string command;

        public bool isShred = false;

        public void CalculateColors() {
            colors[0] = Manager.HexToVector4("403037");
            colors[1] = Manager.HexToVector4("AE8F9A");
            colors[2] = Manager.HexToVector4("664F5C");
            colors[3] = Manager.HexToVector4("DBB7C2");
            colors[4] = Manager.HexToVector4("DDA49C");
            colors[5] = Manager.HexToVector4("C27B7F");
            colors[6] = Manager.HexToVector4("B4666F");
            colors[7] = Manager.HexToVector4("79B386");
            colors[8] = Manager.HexToVector4("000000");
        }

        public ContextFileWindow(){
            using var window = Silk.NET.Windowing.Window.Create(WindowOptions.Default);

            ImGuiController controller = null;
            IInputContext inputContext = null;

            window.Load += () =>{
                controller = new ImGuiController(
                    gl = window.CreateOpenGL(),//load OpenGL
                    window,
                    inputContext = window.CreateInput()
                );

                if (window.Native!.Win32.HasValue)
                    WindowsDarkmodeUtil.SetDarkmodeAware(window.Native.Win32.Value.Hwnd);

                window.Size = new Vector2D<int>(400, 400);

                try{
                    window.SetWindowIcon(Manager.GetRawimageSpan(Environment.ProcessPath.Replace("TheIdkTool.exe", "\\resources\\logo.png")));           
                }catch(Exception ex){
                    DrawUtilRender.AddDrawUtil(new WarningDialog(), "Something went wrong.\n" + ex.Message);
                }
            };

            //Handle resizes
            window.FramebufferResize += s =>{
                gl.Viewport(s);
            };

            CalculateColors();

            if (ContextFileWindow.command.StartsWith("shred"))
                this.isShred = true;

            window.Render += delta =>{
                controller.Update((float)delta);

                gl.ClearColor(Color.FromArgb(255, (int)(.45f * 255), (int)(.55f * 255), (int)(.60f * 255)));
                gl.Clear((uint)ClearBufferMask.ColorBufferBit);

                ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.DockingEnable;
                ImGui.DockSpaceOverViewport();

                // ImGui.PushStyleColor(ImGuiCol.TitleBg, new Vector4(0.2f, 0.2f, 0.8f, 1.0f));
                ImGui.PushStyleColor(ImGuiCol.TitleBgActive, colors[2]);

                ImGui.PushStyleColor(ImGuiCol.Button, colors[0]);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, colors[1]);
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, colors[3]);

                ImGui.PushStyleColor(ImGuiCol.ResizeGrip, colors[2]);
                ImGui.PushStyleColor(ImGuiCol.ResizeGripHovered, colors[1]);
                ImGui.PushStyleColor(ImGuiCol.ResizeGripActive, colors[3]);

                ImGui.PushStyleColor(ImGuiCol.FrameBg, colors[6]);
                ImGui.PushStyleColor(ImGuiCol.FrameBgHovered, colors[5]);
                ImGui.PushStyleColor(ImGuiCol.FrameBgActive, colors[4]);
               
                ImGui.PushStyleColor(ImGuiCol.CheckMark, colors[7]);

                ImGui.PushStyleColor(ImGuiCol.TextSelectedBg, colors[1]);

                ImGui.PushStyleColor(ImGuiCol.Tab, colors[2]);
                ImGui.PushStyleColor(ImGuiCol.TabHovered, colors[0]);
                ImGui.PushStyleColor(ImGuiCol.TabActive, colors[0]);

               // ImGui.PushStyleColor(ImGuiCol., colors[7]);

                DrawMainMenuBar();
                if (this.isShred)
                    DrawShredCheck();
                else
                    DrawCodeInput();
                ImGui.End();

                controller.Render();
            };

            window.Closing += () => {
                controller?.Dispose();

                inputContext?.Dispose();

                gl?.Dispose();

                SaveFileManager.SaveFiles();
            };
    
            window.Run();

            window.Dispose();
        }

        public void DrawShredCheck(){
            ImGui.TextWrapped(ContextFileWindow.path + " - " + ContextFileWindow.command);
            ImGui.TextWrapped("Are you sure?");

            if (ImGui.Button("Yes")){
                if (ContextFileWindow.command.EndsWith("f")){
                    foreach(string file in Directory.GetFiles(ContextFileWindow.path))
                        Manager.ShredFile(file);
                }else
                    Manager.ShredFile(ContextFileWindow.path);

                Environment.Exit(0);
            }

            if (ImGui.Button("No"))
                Environment.Exit(0);
        }

        public void DrawCodeInput(){
            ImGui.TextWrapped(ContextFileWindow.path + " - " + ContextFileWindow.command);
            ImGui.InputText("##p3", ref this.currentCode, 1000);
            for (int i = 0; i <= 9; i++){
                if (ImGui.Button(i.ToString()))
                    this.currentCode = this.currentCode + i.ToString();
                if(i == 2){
                    ImGui.SameLine();
                    if (ImGui.Button("<-") && this.currentCode.Length != 0){
                        this.currentCode = this.currentCode.Substring(0, this.currentCode.Length - 1);
                    }
                }
                if(i != 2 && i !=5)
                    ImGui.SameLine();
            }

            ImGui.NewLine();
            ImGui.Checkbox("TvCowtjewt", ref this.extraCode[0]);
            ImGui.SameLine();
            ImGui.Checkbox("twtSRTjt", ref this.extraCode[1]);
            ImGui.Checkbox("JWOtwdnwst", ref this.extraCode[2]);
            ImGui.SameLine();
            ImGui.Checkbox("J$§$&tLzsTS", ref this.extraCode[3]);

            if (ImGui.Button("Start"))
                Start();
        }

        public void Start(){
            try{
                if (this.extraCode[0])
                    this.currentCode = this.currentCode + "TvCowtjewt";
                if (this.extraCode[1])
                    this.currentCode = this.currentCode + "twtSRTjt";
                if (this.extraCode[2])
                    this.currentCode = this.currentCode + "JWOtwdnwst";
                if (this.extraCode[3])
                    this.currentCode = this.currentCode + "J$§$&tLzsTS";
                byte[] key = Manager.GetKeyBytes(this.currentCode);

                switch (command){
                    case "encrypt":
                        Manager.EncryptFile(ContextFileWindow.path, string.Empty, key);
                        break;

                    case "decrypt":
                        Manager.DecryptFile(ContextFileWindow.path, string.Empty, key);
                        break;

                    case "encryptf":
                        foreach (string file in Directory.GetFiles(path))
                            Manager.EncryptFile(file, string.Empty, key);
                        break;

                    case "decryptf":
                        foreach (string file in Directory.GetFiles(path))
                            Manager.DecryptFile(file, string.Empty, key);
                        break;

                    case "open":
                        Manager.DecryptFile(ContextFileWindow.path, string.Empty, key);                       
                        Process.Start(ContextFileWindow.path);
                        Manager.EncryptFile(ContextFileWindow.path, string.Empty, key);
                        break;
                }

                Environment.Exit(1);
            }catch(Exception ex){
                Console.WriteLine(ex.Message + ex.StackTrace);
            }
        }

        public bool[] checkBoxReferences = new bool[1] { false } ;
        public void DrawMainMenuBar(){
            if (ImGui.BeginMainMenuBar()){
                if (ImGui.BeginMenu("Other")){
                    if (ImGui.MenuItem("Close program"))
                        Environment.Exit(1);
                    ImGui.EndMenu();
                }
                ImGui.EndMainMenuBar();
            }
        }

    }
}