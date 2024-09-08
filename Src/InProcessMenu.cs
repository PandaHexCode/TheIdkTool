using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace TheIdkTool.Windows{ 
    
    public class InProcessMenu{

        public static Thread? inputThread = null;

        public static void StartCheckThread(){
            InProcessMenu.inputThread = new Thread(CheckInput);
            InProcessMenu.inputThread.Start();
        }

        public static void StopCheckThread(){
            InProcessMenu.inputThread.Abort();
            InProcessMenu.inputThread = null;
        }

        public static void CheckInput(){
            while (true){
                if (GetAsyncKeyState(InProcessMenu.VK_NUMPAD1) != 0 && GetAsyncKeyState(InProcessMenu.VK_NUMPAD5) != 0)
                    Manager.ForceProcessToForeground(Process.GetCurrentProcess());

                Thread.Sleep(100);
            }
        }

        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int vKey);
        public const int VK_SPACE = 0x20;
        public const int VK_NUMPAD1 = 0x61;
        public const int VK_NUMPAD5 = 0x65;
        public const int VK_ESCAPE = 0x1B;

    }

}
