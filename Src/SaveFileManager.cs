using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheIdkTool.Dialogs;
using TheIdkTool.Windows;

namespace TheIdkTool{

    public class SaveFileManager{
        
        public static string dirPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\TheIdkTool";
        public static string todosFilePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) +
         "\\TheIdkTool\\Todos.dat";

        public static void SaveFiles(){
            if (!Directory.Exists(dirPath))
                Directory.CreateDirectory(dirPath);

            string main = string.Empty;
            main = MainWindow.showAdvancedButtons.ToString();
            Manager.SaveFile(dirPath + "\\Main.dat", main);

            foreach(DrawWindow window in WindowManager.drawWindows){
                string output = window.showWindow.ToString() + "\n";
                foreach(string input in window.inputRefs)
                    output = output + input + "\n";
                Manager.SaveFile(dirPath + "\\" + window.name + ".dat", output);
            }

            SaveTodos();
        }

        public static void LoadFiles(){
            if (!File.Exists(dirPath + "\\Main.dat"))
                return;

            try{
                string[] lines = Manager.GetFileIn(dirPath + "\\Main.dat").Split('\n');
                MainWindow.showAdvancedButtons = Manager.StringToBool(lines[0]);
                foreach (DrawWindow window in WindowManager.drawWindows){
                    try{ 
                        string path = dirPath + "\\" + window.name + ".dat";
                        if (!File.Exists(path)){
                            Console.WriteLine("Can't find " + path + ".");
                            continue;
                        }
                        string[] lines2 = Manager.GetFileIn(path).Split('\n');
                        window.showWindow = Manager.StringToBool(lines2[0]);
                        for (int i = 0; i < lines2.Length; i++){
                            if (i > 0 && i - 1 < window.inputRefs.Length){
                                window.inputRefs[i - 1] = lines2[i];
                            }
                        }
                    }catch(Exception ex){
                        Console.WriteLine(ex.Message + "\n" + ex.StackTrace);
                    }
                }
            }catch (Exception ex){
                Console.WriteLine(ex.Message);
                DrawUtilRender.AddDrawUtil(new WarningDialog(), "Error loading settings.\n" + ex.Message);
            }

            try{
                LoadTodos(todosFilePath);
            }catch (Exception ex){
                DrawUtilRender.AddDrawUtil(new WarningDialog(), "Error loading todo settings.\n" + ex.Message);
            }
        }

        public static void SaveTodos(){
            if (!Directory.Exists(dirPath))
                Directory.CreateDirectory(dirPath);

            string outputString = string.Empty;
            foreach (string category in TodoWindow.categories){
                outputString = outputString + "<--->NewCategory<--->" + category + "\n";
            }
            outputString = outputString + "<--->NewCategory<---><--->EndCategory<--->\n<--->NewTodo<---><--->BeginTodos<--->";
            foreach (TodoWindow.Todo todo in TodoWindow.todos){
                outputString = outputString + "<--->NewTodo<--->" + todo.title + "<--->next<--->" + todo.catergory + "<--->next<--->" + todo.description + "<--->next<--->" + todo.isChecked + "\n";
            }
            Manager.SaveFile(todosFilePath, outputString);
        }

        public static void LoadTodos(string path){
            if (!File.Exists(path))
                return;

            string[] categoryLines = Manager.GetFileIn(path).Split("<--->NewCategory<--->");
            foreach(string line in categoryLines){
                try{
                    if (string.IsNullOrEmpty(line))
                        continue;
                    if (line.Contains("<--->EndCategory<--->"))
                        break;

                    TodoWindow.categories.Add(line.Replace("\n", ""));
                }catch (Exception ex){
                    Console.WriteLine("Error loading todo " + line + " " + ex.Message);
                    continue;
                }
            }

            string[] todoLines = Manager.GetFileIn(path).Split("<--->NewTodo<--->");
            foreach(string line in todoLines){
                try{
                    if (string.IsNullOrEmpty(line) | line.Contains("<--->BeginTodos<--->"))
                        continue;
                    string[] args = line.Split("<--->next<--->");
                    TodoWindow.todos.Add(new TodoWindow.Todo(args[0], args[2], args[1], Manager.StringToBool(args[3])));
                }catch(Exception ex){
                    Console.WriteLine("Error loading todo " + line + " " + ex.Message);
                    continue;
                }
            }
        }

    }

}
