﻿using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TheIdkTool.Windows{

    public class ConnectionsWindow{

        public static bool showWindow = true;
        public static void Draw(){
            if (!showWindow)
                return;
            int shutdownButtonId = 0;
            ImGui.Begin("Connections");
            Manager.CheckMinWindowSize(370, 530);

            if (ImGui.TreeNode("Tcp")){
                IPGlobalProperties properties = IPGlobalProperties.GetIPGlobalProperties();
                TcpConnectionInformation[] connections = properties.GetActiveTcpConnections();
                foreach (TcpConnectionInformation c in connections){
                    if (ImGui.TreeNodeEx(c.RemoteEndPoint.ToString() + " <==> " + c.LocalEndPoint.ToString())){
                        ImGui.Text("State:" + c.State.ToString());
                        if (ImGui.Button("Try shutdown##" + shutdownButtonId)){
                            Socket mySocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream,
                         ProtocolType.Tcp);
                            try{
                                mySocket.Bind(c.LocalEndPoint);
                                mySocket.Shutdown(SocketShutdown.Both);
                            }catch (Exception ex) { Console.WriteLine(ex.Message); Console.WriteLine(ex.StackTrace); }
                            finally{
                                mySocket.Close();
                            }
                        }
                        shutdownButtonId = shutdownButtonId + 1;
                        ImGui.TreePop();
                    }
                }
                ImGui.TreePop();
            }

            if (ImGui.TreeNode("Udp")){
                IPEndPoint[] list_all_ports = IPGlobalProperties.GetIPGlobalProperties().GetActiveUdpListeners();
                foreach (IPEndPoint ip in list_all_ports){
                    if (ImGui.TreeNodeEx(ip.ToString())){
                        ImGui.TreePop();
                    }
                }
                ImGui.TreePop();
            }
        }

    }

}