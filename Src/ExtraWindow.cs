using ImGuiNET;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using TheIdkTool.Dialogs;

namespace TheIdkTool.Windows{

    public class ExtraWindow : DrawWindow{

        public override void Draw(){
            if (ImGui.Button("Test"))
                InProcessMenu.StartCheckThread();

            /**ImGui.TextWrapped("CEC");
            if (ImGui.Button("Test")){
                string wrapperPath = "D:\\C#\\TheIdkToolWrapper\\bin\\Debug\\net8.0\\TheIdkToolWrapper.exe";

                ProcessStartInfo startInfo = new ProcessStartInfo{
                    FileName = wrapperPath,
                    RedirectStandardOutput = true,
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                    CreateNoWindow = false
                };

                using (Process process = Process.Start(startInfo)){
                    if (process != null){
                        using (StreamWriter writer = process.StandardInput){
                            writer.WriteLine("PowerOnDevices");
                        }

                        using (StreamReader reader = process.StandardOutput){
                            string result = reader.ReadToEnd();
                            Console.WriteLine(result);
                        }
                    }
                }
            }*/
        }

    }

}