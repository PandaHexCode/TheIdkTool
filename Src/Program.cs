using System.Diagnostics;
using TheIdkTool;
using TheIdkTool.Windows;

if(args.Length > 0){
    if (args[0].Equals("forceFullScreen")){
        Process process = Process.GetProcessesByName(args[1])[0];
        Manager.ForceFullScreen(process);
        Process.GetCurrentProcess().Kill();
    }
}else
    new MainWindow();