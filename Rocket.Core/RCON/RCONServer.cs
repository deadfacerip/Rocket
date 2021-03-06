﻿using System;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using System.Reflection;
using Rocket.Core.Logging;
using UnityEngine;
using Rocket.API;

namespace Rocket.Core.RCON
{
    public class RCONServer : MonoBehaviour
    {
        private static List<RCONConnection> clients = new List<RCONConnection>();
        private TcpListener listener;
        private bool exiting = false;
        private Thread waitingThread;

        private static Queue<string> commands = new Queue<string>();

        public void Awake()
        {
            listener = new TcpListener(IPAddress.Any, R.Settings.Instance.RCON.Port);
            listener.Start();

            // Logger.Log("Waiting for new connection...");

            waitingThread = new Thread(() =>
            {
                while (!exiting)
                {
                    RCONConnection newclient = new RCONConnection(listener.AcceptTcpClient());
                    clients.Add(newclient);
                    newclient.Send("RocketRcon v" + Assembly.GetExecutingAssembly().GetName().Version + "\r\n");
                    ThreadPool.QueueUserWorkItem(handleConnection, newclient);
                }
            });
            waitingThread.Start();
        }

        private static void handleConnection(object obj)
        {
            try
            {
                RCONConnection newclient = (RCONConnection)obj;
                string command = "";
                while (newclient.Client.Client.Connected)
                {
                    Thread.Sleep(100);
                    command = newclient.Read();
                    if (command == "") break;
                    command = command.TrimEnd('\n', '\r', ' ');
                    if (command == "quit") break;
                    if (command == "ia")
                    {
                        //newclient.Send("Toggled interactive mode");
                        newclient.Interactive = !newclient.Interactive;
                    }
                    if (command == "") continue;
                    if (command == "login")
                    {
                        if (newclient.Authenticated)
                            newclient.Send("Notice: You are already logged in!\r\n");
                        else
                            newclient.Send("Syntax: login <password>");
                        continue;
                    }
                    if (command.Split(' ').Length > 1 && command.Split(' ')[0] == "login")
                    {
                        if (newclient.Authenticated)
                        {
                            newclient.Send("Notice: You are already logged in!\r\n");
                            continue;
                        }
                        else
                        {

                            if (command.Split(' ')[1] == R.Settings.Instance.RCON.Password)
                            {
                                newclient.Authenticated = true;
                                //newclient.Send("Success: You have logged in!\r\n");
                                //Logger.Log("Client has logged in!");
                                continue;
                            }
                            else
                            {
                                newclient.Send("Error: Invalid password!\r\n");
                                Logging.Logger.Log("Client has failed to log in.");
                                break;
                            }
                        }
                    }

                    if (command == "set")
                    {
                        newclient.Send("Syntax: set [option] [value]");
                        continue;
                    }
                    if (!newclient.Authenticated)
                    {
                        newclient.Send("Error: You have not logged in yet!\r\n");
                        continue;
                    }
                    if (command != "ia")
                        Logging.Logger.Log("Client has executed command \"" + command + "\"");

                    lock (commands)
                    {
                        commands.Enqueue(command);
                    }
                    command = "";
                }


                clients.Remove(newclient);
                newclient.Send("Good bye!");
                Thread.Sleep(1500);
                Logging.Logger.Log("Client has disconnected! (IP: " + newclient.Client.Client.RemoteEndPoint + ")");
                newclient.Close();

            }
            catch (Exception ex)
            {
                Logging.Logger.LogException(ex);
            }
        }

        private void FixedUpdate()
        {
            lock (commands)
            {
                while(commands.Count!=0)
                    R.Commands.Execute(new ConsolePlayer(), commands.Dequeue());
            }
        }

        public static void Broadcast(string message)
        {
            foreach (RCONConnection client in clients)
            {
                if (client.Authenticated)
                    client.Send(message);
            }
        }

        private void OnDestroy()
        {
            exiting = true;
            // Force all connected RCON clients to disconnect from the server on shutdown. The server will get stuck in the shutdown process until all clients disconnect.
            foreach (RCONConnection client in clients)
            {
                client.Close();
            }
            clients.Clear();
            waitingThread.Abort();
            listener.Stop();
        }

        public static string Read(TcpClient client)
        {
            byte[] _data = new byte[6];
            string data = "";
            NetworkStream _stream = client.GetStream();

            while (true)
            {
                try
                {
                    int k = _stream.Read(_data, 0, 1);
                    if (k == 0)
                        return "";
                    string kk = Encoding.UTF8.GetString(_data, 0, 1);
                    data += kk;
                    if (kk == "\n")
                        break;
                }
                catch(Exception ex)
                {
                    Logging.Logger.LogException(ex);
                    return "";
                }
            }
            return data;
        }

        public static void Send(TcpClient client, string text)
        {
            byte[] data = new UTF8Encoding().GetBytes(text);
            client.GetStream().Write(data, 0, data.Length);
        }
    }
}