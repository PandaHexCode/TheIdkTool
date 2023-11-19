﻿using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using TheIdkTool.Dialogs;

namespace TheIdkTool.Windows{ 

    public class TodoWindow{

        public static bool showWindow = true;
        public static List<string> categories = new List<string>();
        public static List<Todo> todos = new List<Todo>();
        public static string[] todoInputs = new string[5] { string.Empty, string.Empty, string.Empty, string.Empty, string.Empty };
        public static int currentBoxItem = 0;
        public static bool changeCatergory = false;
        public static bool isEditCategory = false;
        public static int currentEditCategory = 0;
        public static bool isEditTodo = false;
        public static int currentEditTodo = 0;
        public static bool onlyViewMode = false;

        public class Todo{
            public string title;
            public string description;
            public string catergory;
            public bool isChecked = false;

            public Todo(string title, string description, string catergory, bool isChecked){
                this.title = title;
                this.description = description;
                this.catergory = catergory;
                this.isChecked = isChecked;
            }
        }

        public static void Draw(){
            if (!showWindow)
                return;
            ImGui.Begin("Todo");
            Manager.CheckMinWindowSize(215, 310);

            ImGui.Checkbox("Only view mode", ref onlyViewMode);
            Manager.Tooltip("Reduces CPU usage so it can run well in the background while making tasks.");

            try
            { 
            if (categories.Count <= 0){
                categories.Add("No catergory");
            }

            if (isEditTodo)
                ImGui.SetNextItemOpen(true, ImGuiCond.Once);//open treenode

            if (!onlyViewMode && ImGui.TreeNodeEx("Add todo")){
                Manager.CheckMinWindowSize(645, 310);

                DrawAddTodo();
                ImGui.TreePop();
            }

            ImGui.NewLine();

            DrawTodos();
            }catch(Exception ex){Console.WriteLine(ex.Message);}
        }

        public static void DrawAddTodo(){
            if (isEditTodo)
                ImGui.Text("Editing - Todo");
            if(isEditTodo && ImGui.Button("Cancel editing")){
                isEditTodo = false;
                currentEditTodo = 0;
                return;
            }
            ImGui.InputText("Title", ref todoInputs[0], 500);
            ImGui.NewLine();

            if (categories.Count < 0)
                return;

            ImGui.TextWrapped("Catergory: " + categories[currentBoxItem]);
            ImGui.SameLine();
            ImGui.Checkbox("Change", ref changeCatergory);

            if (changeCatergory && ImGui.BeginListBox("Catergory")){
                for (int i = 0; i < categories.Count; i++){
                    bool isSelected = currentBoxItem == i;
                    if (ImGui.Selectable(categories[i], isSelected)){
                        currentBoxItem = i;
                        changeCatergory = false;
                    }
                }
                ImGui.InputText("", ref todoInputs[2], 500);
                if(ImGui.Selectable("Add new catergory")){
                    if (categories.Contains(todoInputs[2]))
                        DrawUtilRender.AddDrawUtil(new WarningDialog(), "There is already a catergory with the same name.\n");
                    else{
                        categories.Add(todoInputs[2]);
                        todoInputs[2] = string.Empty;
                    }
                }
                ImGui.EndListBox();
            }

            ImGui.NewLine();
            ImGui.InputTextMultiline("Description", ref todoInputs[3], 10000, new System.Numerics.Vector2(500, 200));
            ImGui.NewLine();

            if (isEditTodo){
                if(ImGui.Button("Finish editing")){
                    if (!todoInputs[0].Equals(todos[currentEditTodo].title) && !CheckIfTitleIsAvaible(todoInputs[0], categories[currentBoxItem]))
                        DrawUtilRender.AddDrawUtil(new WarningDialog(), "There is already a title in that catergory with the same name.\n");
                    else{
                        todos[currentEditTodo].title = todoInputs[0];
                        todos[currentEditTodo].description = todoInputs[3];
                        todos[currentEditTodo].catergory = categories[currentBoxItem];
                        isEditTodo = false;
                    }
                }
            }else if (ImGui.Button("Add##2")){
                if (!CheckIfTitleIsAvaible(todoInputs[0], categories[currentBoxItem]))
                    DrawUtilRender.AddDrawUtil(new WarningDialog(), "There is already a title in that catergory with the same name.\n");
                else
                    todos.Add(new Todo(todoInputs[0], todoInputs[3], categories[currentBoxItem], false));
            }
        }

        public static void DrawTodos(){
            int deleteButtonId = 0;
            int editButtonId = 0;
            string[] tempCatergories = categories.ToArray();
            Todo[] tempTodos = todos.ToArray();

            if (isEditCategory){
                ImGui.InputText("New name", ref todoInputs[4], 500);
                if (ImGui.Button("Finish")){
                    string oldCategory = categories[currentEditCategory];
                    if (!todoInputs[4].Equals(oldCategory) && categories.Contains(todoInputs[4])){
                        DrawUtilRender.AddDrawUtil(new WarningDialog(), "There is already a catergory with the same name.\n");
                        return;
                    }
                    foreach(Todo todo in tempTodos){
                        if (todo.catergory.Equals(oldCategory))
                            todo.catergory = todoInputs[4];
                    }
                    categories[currentEditCategory] = todoInputs[4];
                    isEditCategory = false;
                    currentEditCategory = 0;
                    oldCategory = null;
                }
                ImGui.SameLine();
                if (ImGui.Button("Cancel")){
                    isEditCategory = false;
                    currentEditCategory = 0;
                }
                return;
            }

            foreach (string category in tempCatergories){
                if (ImGui.TreeNodeEx(category)){
                    List<Todo> categoryTodos = new List<Todo>();

                    if (!onlyViewMode){
                        ImGui.SameLine();
                        int index = categories.IndexOf(category);
                        if (index > 0){
                            if(ImGui.Button("^##" + index))
                                 Manager.ChangePositionInList(categories, category, categories.IndexOf(category) - 1);
                            ImGui.SameLine();
                        }
                        if (index != categories.Count - 1&& ImGui.Button("v##" + index)){
                            Manager.ChangePositionInList(categories, category, categories.IndexOf(category) + 1);
                        }
                        ImGui.SameLine();
                        if (ImGui.Button("Delete##" + deleteButtonId)){
                            foreach (Todo todo in tempTodos)
                            {
                                if (todo.catergory.Equals(category))
                                    todos.Remove(todo);
                            }
                            categories.Remove(category);
                        }
                        ImGui.SameLine();
                        if (ImGui.Button("Edit##" + editButtonId)){
                            isEditCategory = true;
                            todoInputs[4] = category;
                            currentEditCategory = categories.IndexOf(category);
                            break;
                        }
                        deleteButtonId = deleteButtonId + 1;
                        editButtonId = editButtonId + 1;
                    }

                    foreach (Todo todo in tempTodos){//Todo reduce foreach loops, (fix lazy spagethi code)
                        if (todo.catergory.Equals(category)){
                            categoryTodos.Add(todo);
                        }
                    }

                    foreach(Todo todo in categoryTodos){
                        ImGui.Checkbox(todo.title, ref todo.isChecked);
                        Manager.Tooltip(todo.description);
                        if (!onlyViewMode){
                            ImGui.SameLine();
                            int index = categoryTodos.IndexOf(todo);
                            if (index > 0){
                                if (ImGui.Button("^##" + (index * 2)))
                                    Manager.ChangePositionInList(todos, todo, todos.IndexOf(todo) - 1);
                                ImGui.SameLine();
                            }
                            if (index != categoryTodos.Count - 1 && ImGui.Button("v##" + (index * 2))){
                                Manager.ChangePositionInList(todos, todo, todos.IndexOf(todo) + 1);
                            }
                            ImGui.SameLine();
                            if (ImGui.Button("Delete##" + deleteButtonId))
                                todos.Remove(todo);
                            ImGui.SameLine();
                            if (ImGui.Button("Edit##" + editButtonId)){
                                todoInputs[0] = todo.title;
                                todoInputs[3] = todo.description;
                                currentBoxItem = categories.IndexOf(todo.catergory);
                                isEditTodo = true;
                                currentEditTodo = todos.IndexOf(todo);
                                break;
                            }
                            editButtonId = editButtonId + 1;
                            deleteButtonId = deleteButtonId + 1;
                        }
                        
                    }
                    ImGui.TreePop();
                }
            }
        }

        public static bool CheckIfTitleIsAvaible(string title, string catergory){
            foreach(Todo todo in todos){
                if (todo.title == title && todo.catergory == catergory)
                    return false;
            }
            return true;
        }

    }

}