using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TheIdkTool.Dialogs;

namespace TheIdkTool.Windows{

    public class ConnectionsWindow : DrawWindow{

        public bool isInTransfer = false;
        public bool isSending = false;

        public override void Draw(){
            int shutdownButtonId = 0;

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

            if(ImGui.TreeNode("File transfer (TCP)")){
                ImGui.InputText("Ip", ref inputRefs[0], 100);
                ImGui.InputText("Port", ref inputRefs[1], 10);
                ImGui.InputText("Monitor Port", ref inputRefs[4], 10);
                ImGui.SameLine();
                if (ImGui.Button("Auto")){
                    inputRefs[0] = GetLocalIPv4Address().ToString();
                    inputRefs[1] = FindFreePort(IPAddress.Parse(inputRefs[0])).ToString();
                    inputRefs[4] = FindFreePort(IPAddress.Parse(inputRefs[0])).ToString();
                }


                if (!isInTransfer && ImGui.Button("Start Server"))
                    StartFileTransferServer();
                ImGui.SameLine();
                if (!isInTransfer && ImGui.Button("Connect to Server"))
                    ConnectToFileTransferServer();

                if (isInTransfer && isServer && client != null && client.Connected)
                    ImGui.TextWrapped("Client connected.");
                else if (isInTransfer && !isServer && client != null && client.Connected)
                    ImGui.TextWrapped("Connected to server.");
                else if (isInTransfer && client == null)
                    ImGui.TextWrapped("Waiting for connection...");
                else if (isInTransfer && client != null && !client.Connected)
                    ImGui.TextWrapped("Connection lost.");

                ImGui.InputText("Path(For sending)", ref inputRefs[2], 1000);
                ImGui.NewLine();
                Manager.SelectFolderButton(ref inputRefs[2], "Folder");
                Manager.SelectFileButton(ref inputRefs[2], "File");

                ImGui.InputText("Path(For receving)", ref inputRefs[3], 1000);
                ImGui.NewLine();
                Manager.SelectFolderButton(ref inputRefs[3], "", 2);

                if (client != null && client.Connected && !isSending && ImGui.Button("Start sending")){
                    isSending = true;

                    Task t = new Task(() => {
                         SendTask(client);
                    }
                      );
                    t.Start();
                }

                if (isInTransfer &&  ImGui.Button("Stop")){
                    if (isServer && server != null)
                        server.Stop();
                    else if (client != null)
                        client.Dispose();

                    if (isServer && monitorServer != null)
                        monitorServer.Stop();
                    else if (monitorClient != null)
                        monitorClient.Dispose();

                    isInTransfer = false;
                    isSending = false;
                }

                ImGui.TreePop();
            }
        }

        private static TcpClient client;
        private static TcpListener server;
        private static bool isServer = false;

        private static TcpClient monitorClient;
        private static TcpListener monitorServer;

        public async void StartFileTransferServer(){
            try{
                isInTransfer = true;
                isServer = true;
                server = new TcpListener(IPAddress.Parse(inputRefs[0]), Manager.StringToInt(inputRefs[1]));
                server.Start();
                client = await server.AcceptTcpClientAsync();

                Task t = new Task(() => {
                    StatusMonitorTask();
                }
                    );
                t.Start();

            }
            catch(Exception ex){
                isInTransfer = false;
                DrawUtilRender.AddDrawUtil(new WarningDialog(), "Something went wrong.\n" + ex.Message);
            }
        }

        public void ConnectToFileTransferServer(){
            try{
                isInTransfer = true;
                client = new TcpClient(inputRefs[0], Manager.StringToInt(inputRefs[1]));
                isServer = false;

                Task t = new Task(() => {
                    StatusMonitorTask();
                }
                   );
                t.Start();
            }
            catch(Exception ex){
                isInTransfer = false;
                DrawUtilRender.AddDrawUtil(new WarningDialog(), "Something went wrong.\n" + ex.Message);
            }
        }

        public async Task SendTask(TcpClient client){
            try{
                await SendObject(inputRefs[2]);

                isSending = false;
                isInTransfer = false;
            }catch(Exception ex){
                DrawUtilRender.AddDrawUtil(new WarningDialog(), "Something went wrong.\n" + ex.Message + "\n" + ex.StackTrace);
            }
        }

        public async Task SendObject(string filePath){
            NetworkStream stream = client.GetStream();
            bool isDir = false;

            if(Directory.Exists(filePath))
                isDir = true;

            bool isFinishObject = false;
            if (filePath.Equals("finish"))
                isFinishObject = true;

            string fileName = Path.GetFileName(filePath);
            if (!isDir){
                fileName = fileName + "?FILE";
                fileName = fileName + "???SIZE:" + new FileInfo(filePath).Length;
            }else
                fileName = fileName + "?DIR";

            if (isFinishObject)
                fileName = "?FINISH";

            byte[] fileNameBuffer = Encoding.UTF8.GetBytes(fileName);

            byte[] fileNameLengthBuffer = BitConverter.GetBytes(fileNameBuffer.Length);
            await stream.WriteAsync(fileNameLengthBuffer, 0, fileNameLengthBuffer.Length);

            await stream.WriteAsync(fileNameBuffer, 0, fileNameBuffer.Length);

            if (!isDir)
                await SendFile(filePath, client);
            else
                await SendDirectory(filePath, client);
        }

        public async Task SendFile(string filePath, TcpClient client){
            NetworkStream stream = client.GetStream();
            using (FileStream fileStream = File.OpenRead(filePath)){
                await fileStream.CopyToAsync(stream);
                fileStream.Close();
            }
        }

        public async Task SendDirectory(string filePath, TcpClient client){
            foreach(string file in Directory.GetFiles(filePath)){
                await SendObject(file);
            }

            foreach (string file in Directory.GetDirectories(filePath)){
                await SendObject(file);
            }

            await SendObject("finish");
        }

        public async Task ReceiveTask(TcpClient client){
            try{
                await ReceiveObject(client);

                isSending = false;
                isInTransfer = false;
            }catch(Exception ex){
                DrawUtilRender.AddDrawUtil(new WarningDialog(), "Something went wrong.\n" + ex.Message + "\n" + ex.StackTrace);
            }
        }

        public bool isReceiveFinish = false;
        public string currentDir = string.Empty;
        public bool isInReceive = false;
        public async Task ReceiveObject(TcpClient client){
            isInReceive = true;

            NetworkStream stream = client.GetStream();
        
            byte[] fileNameLengthBuffer = new byte[sizeof(int)];
            await stream.ReadAsync(fileNameLengthBuffer, 0, fileNameLengthBuffer.Length);

            int fileNameLength = BitConverter.ToInt32(fileNameLengthBuffer, 0);

            byte[] fileNameBuffer = new byte[fileNameLength];
            await stream.ReadAsync(fileNameBuffer, 0, fileNameBuffer.Length);

            string fileName = Encoding.UTF8.GetString(fileNameBuffer);

            Console.WriteLine(fileName);

            long fileLength = 0;

            if (fileName.Contains("?FINISH")){
                isReceiveFinish = true;
                currentDir = string.Empty;
                return;
            }

            bool isDir = false;
            if (fileName.Contains("?DIR"))
                isDir = true;
            fileName = fileName.Replace("?FILE", "");
            fileName = fileName.Replace("?DIR", "");

            if (fileName.Contains("???SIZE:")){
                string[] args = fileName.Split("???SIZE:");
                long.TryParse(args[1], out fileLength);
                fileName = args[0];
            }

            string filePath = inputRefs[3] + "\\" + fileName;

            if(currentDir != string.Empty){
                filePath = currentDir + "\\" + fileName;
            }

            if (!isDir)
                await ReceiveFile(filePath, fileLength,client);
            else{
                if (!Directory.Exists(filePath))
                    Directory.CreateDirectory(filePath);

                currentDir = filePath;

                isReceiveFinish = false;
                isInReceive = false;

                while (!isReceiveFinish){
                    while(!isInReceive)
                        await ReceiveObject(client);
                }
            }

            isInReceive = false;
        }

        public async Task ReceiveFile(string filePath, long fileLength,TcpClient client){
            NetworkStream stream = client.GetStream();

            using (FileStream fileStream = File.Create(filePath)){
                Console.WriteLine(fileLength);
                try{
                    await stream.CopyToAsync(fileStream, (int)fileLength, tokenSource.Token);
                }catch(OperationCanceledException ex){

                }
                fileStream.Close();
            }
            GC.Collect();
        }

        public CancellationTokenSource tokenSource = new CancellationTokenSource();

        public async Task StatusMonitorTask(){
            NetworkStream stream;
            if (isServer){
                monitorServer = new TcpListener(IPAddress.Parse(inputRefs[0]), Manager.StringToInt(inputRefs[4]));
                monitorServer.Start();
                await monitorServer.AcceptTcpClientAsync();
            }else
                monitorClient = new TcpClient(inputRefs[0], Manager.StringToInt(inputRefs[4]));

            stream = client.GetStream();

            while (true){
                byte[] buffer = new byte[sizeof(int)];
                await stream.ReadAsync(buffer, 0, buffer.Length);
                int command = BitConverter.ToInt32(buffer, 0);
                if (command == 0){
                    Task t = new Task(() => {
                        ReceiveObject(client);
                    }
                   );
                    t.Start();
                }else if (command == 1)
                    tokenSource.Cancel();
            }
        }

        public IPAddress GetLocalIPv4Address(){
            var host = Dns.GetHostEntry(Dns.GetHostName());
            return host.AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
        }

        public int FindFreePort(IPAddress ipAddress){
            TcpListener tempListener = new TcpListener(ipAddress, 0);
            tempListener.Start();
            int port = ((IPEndPoint)tempListener.LocalEndpoint).Port;
            tempListener.Stop();
            return port;
        }

    }

}
