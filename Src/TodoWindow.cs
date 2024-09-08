﻿using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using TheIdkTool.Dialogs;

namespace TheIdkTool.Windows{ 

    public class TodoWindow : DrawWindow{

        public static List<string> categories = new List<string>();
        public static List<Todo> todos = new List<Todo>();
        public  int currentBoxItem = 0;
        public  bool changeCatergory = false;
        public  bool isEditCategory = false;
        public  int currentEditCategory = 0;
        public  bool isEditTodo = false;
        public  int currentEditTodo = 0;
        public  bool onlyViewMode = false;

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

        public override void Draw(){
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

        public  void DrawAddTodo(){
            if (isEditTodo)
                ImGui.Text("Editing - Todo");
            if(isEditTodo && ImGui.Button("Cancel editing")){
                isEditTodo = false;
                currentEditTodo = 0;
                return;
            }
            ImGui.InputText("Title", ref inputRefs[0], 500);
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
                ImGui.InputText("", ref inputRefs[2], 500);
                if(ImGui.Selectable("Add new catergory")){
                    if (categories.Contains(inputRefs[2]))
                        DrawUtilRender.AddDrawUtil(new WarningDialog(), "There is already a catergory with the same name.\n");
                    else{
                        categories.Add(inputRefs[2]);
                        inputRefs[2] = string.Empty;
                    }
                }
                ImGui.EndListBox();
            }

            ImGui.NewLine();
            ImGui.InputTextMultiline("Description", ref inputRefs[3], 10000, new System.Numerics.Vector2(500, 200));
            ImGui.NewLine();

            if (isEditTodo){
                if(ImGui.Button("Finish editing")){
                    if (!inputRefs[0].Equals(todos[currentEditTodo].title) && !CheckIfTitleIsAvaible(inputRefs[0], categories[currentBoxItem]))
                        DrawUtilRender.AddDrawUtil(new WarningDialog(), "There is already a title in that catergory with the same name.\n");
                    else{
                        todos[currentEditTodo].title = inputRefs[0];
                        todos[currentEditTodo].description = inputRefs[3];
                        todos[currentEditTodo].catergory = categories[currentBoxItem];
                        isEditTodo = false;
                    }
                }
            }else if (ImGui.Button("Add##2")){
                if (!CheckIfTitleIsAvaible(inputRefs[0], categories[currentBoxItem]))
                    DrawUtilRender.AddDrawUtil(new WarningDialog(), "There is already a title in that catergory with the same name.\n");
                else
                    todos.Add(new Todo(inputRefs[0], inputRefs[3], categories[currentBoxItem], false));
            }
        }

        public  void DrawTodos(){
            int deleteButtonId = 0;
            int editButtonId = 0;
            string[] tempCatergories = categories.ToArray();
            Todo[] tempTodos = todos.ToArray();

            if (isEditCategory){
                ImGui.InputText("New name", ref inputRefs[4], 500);
                if (ImGui.Button("Finish")){
                    string oldCategory = categories[currentEditCategory];
                    if (!inputRefs[4].Equals(oldCategory) && categories.Contains(inputRefs[4])){
                        DrawUtilRender.AddDrawUtil(new WarningDialog(), "There is already a catergory with the same name.\n");
                        return;
                    }
                    foreach(Todo todo in tempTodos){
                        if (todo.catergory.Equals(oldCategory))
                            todo.catergory = inputRefs[4];
                    }
                    categories[currentEditCategory] = inputRefs[4];
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
                            inputRefs[4] = category;
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
                                inputRefs[0] = todo.title;
                                inputRefs[3] = todo.description;
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

        public  bool CheckIfTitleIsAvaible(string title, string catergory){
            foreach(Todo todo in todos){
                if (todo.title == title && todo.catergory == catergory)
                    return false;
            }
            return true;
        }

    }

}