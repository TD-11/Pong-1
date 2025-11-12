using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;

public class TcpServerChat : MonoBehaviour
{
    private TcpListener listener;
    private Thread serverThread;
    private List<TcpClient> clients = new List<TcpClient>();
    private int port = 6000; // Porta do chat TCP

    void Start()
    {
        listener = new TcpListener(IPAddress.Any, port);
        listener.Start();

        serverThread = new Thread(AcceptClients);
        serverThread.Start();

        Debug.Log($"[Chat TCP] Servidor iniciado na porta {port}");
    }

    void AcceptClients()
    {
        while (true)
        {
            try
            {
                TcpClient client = listener.AcceptTcpClient();
                lock (clients) clients.Add(client);
                Debug.Log("[Chat TCP] Novo cliente conectado");

                Thread t = new Thread(() => HandleClient(client));
                t.Start();
            }
            catch (Exception e)
            {
                Debug.LogError("[Chat TCP] Erro: " + e.Message);
            }
        }
    }

    void HandleClient(TcpClient client)
    {
        NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[1024];

        try
        {
            while (true)
            {
                int bytesRead = stream.Read(buffer, 0, buffer.Length);
                if (bytesRead <= 0) break;

                string msg = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Debug.Log("[Chat TCP] Recebido: " + msg);
                Broadcast(msg, client);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[Chat TCP] Cliente desconectado: " + e.Message);
        }
        finally
        {
            lock (clients) clients.Remove(client);
            client.Close();
        }
    }

    void Broadcast(string message, TcpClient sender)
    {
        byte[] data = Encoding.UTF8.GetBytes(message);
        lock (clients)
        {
            foreach (var c in new List<TcpClient>(clients))
            {
                try
                {
                    if (c.Connected)
                    {
                        NetworkStream s = c.GetStream();
                        s.Write(data, 0, data.Length);
                    }
                }
                catch
                {
                    clients.Remove(c);
                }
            }
        }
    }

    void OnApplicationQuit()
    {
        listener.Stop();
        if (serverThread != null) serverThread.Abort();
        lock (clients)
        {
            foreach (var c in clients)
                c.Close();
        }
    }
}