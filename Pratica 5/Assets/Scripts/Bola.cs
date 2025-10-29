using UnityEngine;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System.Globalization;
using System.Collections.Concurrent;
using System.Collections.Generic;

public class UdpClientFourClients : MonoBehaviour
{
    public int myId = -1;
    UdpClient client;
    Thread receiveThread;
    IPEndPoint serverEP;

    public int Velocidade = 20;
    public GameObject bola;

    // referência para todos os 4 jogadores
    private Dictionary<int, GameObject> players = new Dictionary<int, GameObject>();
    private Dictionary<int, Vector3> remotePositions = new Dictionary<int, Vector3>();

    private ConcurrentQueue<string> messageQueue = new ConcurrentQueue<string>();

    void Start()
    {
        client = new UdpClient();
        serverEP = new IPEndPoint(IPAddress.Parse("10.57.1.146"), 5001);
        client.Connect(serverEP);

        receiveThread = new Thread(ReceiveData);
        receiveThread.Start();

        client.Send(Encoding.UTF8.GetBytes("HELLO"), 5);

        // Busca todas as raquetes
        for (int i = 1; i <= 4; i++)
        {
            GameObject p = GameObject.Find("Player " + i);
            if (p != null) players[i] = p;
            remotePositions[i] = Vector3.zero;
        }

        if (bola != null)
        {
            bola.transform.position = Vector3.zero;
            var rb = bola.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;
        }
    }

    void Update()
    {
        // processa mensagens
        while (messageQueue.TryDequeue(out string msg))
            ProcessMessage(msg);

        if (myId == -1 || !players.ContainsKey(myId)) return;

        // movimentação local
        float v = Input.GetAxis("Vertical");
        GameObject local = players[myId];
        local.transform.Translate(new Vector3(0, v, 0) * Time.deltaTime * Velocidade);

        // limite
        Vector3 pos = local.transform.position;
        pos.y = Mathf.Clamp(pos.y, -3f, 3f);
        local.transform.position = pos;

        // envia posição
        string msgPos = $"POS:{myId};{pos.x.ToString("F2", CultureInfo.InvariantCulture)};{pos.y.ToString("F2", CultureInfo.InvariantCulture)}";
        SendUdpMessage(msgPos);

        // interpola raquetes remotas
        foreach (var kv in players)
        {
            int id = kv.Key;
            if (id == myId) continue;
            kv.Value.transform.position = Vector3.Lerp(kv.Value.transform.position, remotePositions[id], Time.deltaTime * 10f);
        }
    }

    void ReceiveData()
    {
        IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
        while (true)
        {
            byte[] data = client.Receive(ref remoteEP);
            string msg = Encoding.UTF8.GetString(data);
            messageQueue.Enqueue(msg);
        }
    }

    void ProcessMessage(string msg)
    {
        if (msg.StartsWith("ASSIGN:"))
        {
            myId = int.Parse(msg.Substring(7));
            Debug.Log("[Cliente] Meu ID = " + myId);

            // posicionamento inicial
            switch (myId)
            {
                case 1: players[1].transform.position = new Vector3(-8f, 2f, 0); break;
                case 2: players[2].transform.position = new Vector3(-8f, -2f, 0); break;
                case 3: players[3].transform.position = new Vector3(8f, 2f, 0); break;
                case 4: players[4].transform.position = new Vector3(8f, -2f, 0); break;
            }

            // inicializa posição remota
            foreach (var id in players.Keys)
                remotePositions[id] = players[id].transform.position;
        }
        else if (msg.StartsWith("POS:"))
        {
            string[] parts = msg.Substring(4).Split(';');
            if (parts.Length == 3)
            {
                int id = int.Parse(parts[0]);
                if (id != myId)
                {
                    float x = float.Parse(parts[1], CultureInfo.InvariantCulture);
                    float y = float.Parse(parts[2], CultureInfo.InvariantCulture);
                    remotePositions[id] = new Vector3(x, y, 0);
                }
            }
        }
        else if (msg.StartsWith("BALL:"))
        {
            if (myId != 3) // suponha que jogador 3 é o "host da bola"
            {
                string[] parts = msg.Substring(5).Split(';');
                if (parts.Length == 2 && bola != null)
                {
                    float x = float.Parse(parts[0], CultureInfo.InvariantCulture);
                    float y = float.Parse(parts[1], CultureInfo.InvariantCulture);
                    bola.transform.position = new Vector3(x, y, 0);
                }
            }
        }
    }

    public void SendUdpMessage(string msg)
    {
        client.Send(Encoding.UTF8.GetBytes(msg), msg.Length);
    }

    void OnApplicationQuit()
    {
        receiveThread.Abort();
        client.Close();
    }
}