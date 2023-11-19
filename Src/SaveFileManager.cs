﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TheIdkTool.Dialogs;
using TheIdkTool.Windows;

namespace TheIdkTool{

    public class SaveFileManager{
        
        public static string dirPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\TheIdkTool";
        public static string filePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)+
            "\\TheIdkTool\\Settings.dat";
        public static string todosFilePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) +
         "\\TheIdkTool\\Todos.dat";

        public static void SaveFile(){
            if (!Directory.Exists(dirPath))
                Directory.CreateDirectory(dirPath);

            string outputString = string.Empty;
            outputString = outputString + ProcessesWindow.killListPath + "\n";
            outputString = outputString + ProcessesWindow.showWindow + "\n";
            outputString = outputString + FileWindow.showWindow + "\n";
            outputString = outputString + ConnectionsWindow.showWindow + "\n";
            outputString = outputString + TodoWindow.showWindow + "\n";
            outputString = outputString + FileWindow.currentDirectoryInput + "\n";
            outputString = outputString + FileWindow.currentPathInput + "\n";
            outputString = outputString + FileWindow.otherInputs[7] + "\n";
            Manager.SaveFile(filePath, outputString);
            SaveTodos();
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

        public static void LoadFile(){
            if (!File.Exists(filePath))
                return;

            try{
                string[] lines = Manager.GetFileIn(filePath).Split('\n');
                ProcessesWindow.killListPath = lines[0];
                ProcessesWindow.showWindow = Manager.StringToBool(lines[1]);
                FileWindow.showWindow = Manager.StringToBool(lines[2]);
                ConnectionsWindow.showWindow = Manager.StringToBool(lines[3]);
                TodoWindow.showWindow = Manager.StringToBool(lines[4]);
                FileWindow.currentDirectoryInput = lines[5];
                FileWindow.currentPathInput = lines[6];
                FileWindow.otherInputs[7] = lines[7];
            }catch(Exception ex){
                DrawUtilRender.AddDrawUtil(new WarningDialog(), "Error loading settings.\n" + ex.Message);
            }

            try{
                LoadTodos(todosFilePath);
            }catch(Exception ex){
                DrawUtilRender.AddDrawUtil(new WarningDialog(), "Error loading todo settings.\n" + ex.Message);
            }
        }

    }

}
