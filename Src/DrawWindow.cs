using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheIdkTool.Windows{
    
    public class DrawWindow{

        public string name;
        public bool showWindow;
        public bool isAdvanced;
        public string[] inputRefs;
        public int minX, minY;
        public void StartDraw(){
            ImGui.Begin(name);
            Manager.CheckMinWindowSize(minX, minY);
            Draw();
        }

        public virtual void Draw(){

        }

    }

    public class WindowManager{

        public static List<DrawWindow> drawWindows = new List<DrawWindow>();

        public static void InitWindows(){
            InitWindow(new ProcessesWindow(), "Processes", 10, true, false, 485, 260);
            InitWindow(new FileWindow(), "Files & tools", 15, false, false, 295, 245);
            InitWindow(new ConnectionsWindow(), "Connections", 10, false, false, 470, 530);
            InitWindow(new TodoWindow(), "Todo", 10, false, false, 215, 310);
        }

        public static void InitWindow(DrawWindow window, string name, int inputRefsSize, bool autoShow, bool isAdvanced, int minX, int minY){
            window.inputRefs = new string[inputRefsSize];
            for (int i = 0; i < window.inputRefs.Length; i++){
                if(window.inputRefs[i] == null)
                    window.inputRefs[i] = string.Empty;
            }
            window.isAdvanced = isAdvanced;
            window.name = name;
            window.showWindow = autoShow;
            window.minX = minX;
            window.minY = minY;
            WindowManager.drawWindows.Add(window);
        }

        public static void Draw(){
            foreach(DrawWindow window in WindowManager.drawWindows){
                if (window.showWindow){
                    window.StartDraw();
                }
            }
        }

        public static void DrawCheckbox(){
            foreach (DrawWindow window in WindowManager.drawWindows){
                if(!window.isAdvanced | (window.isAdvanced && MainWindow.showAdvancedButtons))
                    ImGui.Checkbox(window.name, ref window.showWindow);
            }

            ImGui.Checkbox("Show advanced buttons", ref MainWindow.showAdvancedButtons);
        }

    }

}
