using UnityEngine;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System.Globalization;
using System.Collections.Concurrent;
using TMPro; // ← importante

public class UdpClientFourClients : MonoBehaviour
{
    public int myId = -1;
    UdpClient client;
    Thread receiveThread;
    IPEndPoint serverEP;

    public GameObject[] players; // 4 players arrastados no Inspector
    public GameObject bola;

    private Vector3[] remotePos = new Vector3[4];
    private ConcurrentQueue<string> messageQueue = new ConcurrentQueue<string>();
    public int Velocidade = 15;

    // --- HUD ---
    [Header("HUD")]
    public TextMeshProUGUI playerInfoText; // Texto discreto que mostra o jogador
    private string[] playerNames = { "Jogador 1.1", "Jogador 1.2", "Jogador 2.2", "Jogador 2.1" };

    void Start()
    {
        client = new UdpClient();
        serverEP = new IPEndPoint(IPAddress.Parse("10.57.1.27"), 5001);
        client.Connect(serverEP);

        receiveThread = new Thread(ReceiveData);
        receiveThread.Start();

        client.Send(Encoding.UTF8.GetBytes("HELLO"), 5);

        for (int i = 0; i < 4; i++)
            remotePos[i] = Vector3.zero;

        if (bola != null)
        {
            bola.transform.position = Vector3.zero;
            var rb = bola.GetComponent<Rigidbody2D>();
            if (rb != null)
                rb.linearVelocity = Vector2.zero;
        }

        // --- HUD ---
        if (playerInfoText != null)
            playerInfoText.text = "Conectando ao servidor...";
    }

    void Update()
    {
        while (messageQueue.TryDequeue(out string msg))
            ProcessMessage(msg);

        if (myId == -1) return;

        float v = Input.GetAxis("Vertical");
        GameObject local = players[myId - 1];
        local.transform.Translate(new Vector3(0, v, 0) * Time.deltaTime * Velocidade);

        Vector3 pos = local.transform.position;
        pos.y = Mathf.Clamp(pos.y, -3f, 3f);
        local.transform.position = pos;

        string msgPos = $"POS:{myId};{pos.x.ToString("F2", CultureInfo.InvariantCulture)};{pos.y.ToString("F2", CultureInfo.InvariantCulture)}";
        SendUdpMessage(msgPos);

        for (int i = 0; i < 4; i++)
        {
            if (i != myId - 1)
                players[i].transform.position = Vector3.Lerp(players[i].transform.position, remotePos[i], Time.deltaTime * 10f);
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

            PosicionarJogadores();

            // --- HUD ---
            if (playerInfoText != null && myId >= 1 && myId <= 4)
            {
                playerInfoText.text = $"Você é o <b>{playerNames[myId - 1]}</b>";
                playerInfoText.color = new Color(1f, 1f, 1f, 0.6f); // texto branco semitransparente
            }
        }
        else if (msg.StartsWith("POS:"))
        {
            string[] p = msg.Substring(4).Split(';');
            if (p.Length == 3)
            {
                int id = int.Parse(p[0]);
                if (id != myId)
                {
                    float x = float.Parse(p[1], CultureInfo.InvariantCulture);
                    float y = float.Parse(p[2], CultureInfo.InvariantCulture);
                    remotePos[id - 1] = new Vector3(x, y, 0);
                }
            }
        }
        else if (msg.StartsWith("BALL:") && myId != 4)
        {
            string[] p = msg.Substring(5).Split(';');
            if (p.Length == 2)
            {
                float x = float.Parse(p[0], CultureInfo.InvariantCulture);
                float y = float.Parse(p[1], CultureInfo.InvariantCulture);
                if (bola != null)
                    bola.transform.position = new Vector3(x, y, 0);
            }
        }
        else if (msg.StartsWith("SCORE:"))
        {
            string[] parts = msg.Substring(6).Split(';');
            if (parts.Length == 2)
            {
                int a = int.Parse(parts[0]);
                int b = int.Parse(parts[1]);
                if (bola != null)
                {
                    BolaFourPlayers bolaScript = bola.GetComponent<BolaFourPlayers>();
                    bolaScript.PontoA = a;
                    bolaScript.PontoB = b;
                    bolaScript.textoPontoA.text = a.ToString();
                    bolaScript.textoPontoB.text = b.ToString();
                }
            }
        }
    }

    void PosicionarJogadores()
    {
        players[0].transform.position = new Vector3(-8f, 0f, 0f); // 1.1
        players[1].transform.position = new Vector3(-6f, 0f, 0f); // 1.2
        players[2].transform.position = new Vector3(6f, 0f, 0f);  // 2.2
        players[3].transform.position = new Vector3(8f, 0f, 0f);  // 2.1

        if (bola != null)
        {
            bola.transform.position = Vector3.zero;
            var rb = bola.GetComponent<Rigidbody2D>();
            if (rb != null)
                rb.linearVelocity = Vector2.zero;
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