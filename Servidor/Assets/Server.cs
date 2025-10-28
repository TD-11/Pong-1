using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;

public class UdpServerFourClients : MonoBehaviour
{
    private UdpClient server;
    private IPEndPoint anyEP;
    private Thread receiveThread;
    private Dictionary<string, int> clientIds = new Dictionary<string, int>();
    private int nextId = 1;
    private object lockObj = new object();

    void Start()
    {
        server = new UdpClient(5001);
        anyEP = new IPEndPoint(IPAddress.Any, 0);

        receiveThread = new Thread(ReceiveData);
        receiveThread.IsBackground = true;
        receiveThread.Start();

        Debug.Log("Servidor iniciado na porta 5001");
    }

    void ReceiveData()
    {
        while (true)
        {
            try
            {
                byte[] data = server.Receive(ref anyEP);
                string msg = Encoding.UTF8.GetString(data);
                string key = anyEP.Address + ":" + anyEP.Port;

                lock (lockObj)
                {
                    // Registra novo cliente
                    if (!clientIds.ContainsKey(key))
                    {
                        if (clientIds.Count < 4) // limite de 4 jogadores
                        {
                            clientIds[key] = nextId++;
                            string assignMsg = "ASSIGN:" + clientIds[key];
                            server.Send(Encoding.UTF8.GetBytes(assignMsg), assignMsg.Length, anyEP);
                            Debug.Log($"Novo cliente {key} => ID {clientIds[key]}");
                        }
                        else
                        {
                            string fullMsg = "SERVER_FULL";
                            server.Send(Encoding.UTF8.GetBytes(fullMsg), fullMsg.Length, anyEP);
                            Debug.Log($"Conexão recusada (lotado): {key}");
                            continue;
                        }
                    }
                }

                Debug.Log($"Recebido de {key}: {msg}");

                // Reenvia para todos os clientes
                if (msg.StartsWith("POS:") || msg.StartsWith("BALL:") || msg.StartsWith("SCORE:"))
                {
                    Broadcast(msg);
                }
            }
            catch (SocketException ex)
            {
                Debug.LogError("Erro de socket: " + ex.Message);
            }
        }
    }

    void Broadcast(string message)
    {
        byte[] bdata = Encoding.UTF8.GetBytes(message);

        lock (lockObj)
        {
            foreach (var kvp in clientIds)
            {
                string[] parts = kvp.Key.Split(':');
                IPEndPoint ep = new IPEndPoint(IPAddress.Parse(parts[0]), int.Parse(parts[1]));
                server.Send(bdata, bdata.Length, ep);
                Debug.Log($"Enviado para {kvp.Key}: {message}");
            }
        }
    }

    void OnApplicationQuit()
    {
        receiveThread?.Abort();
        server?.Close();
    }
}