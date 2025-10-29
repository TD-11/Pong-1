using UnityEngine;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System.Globalization;
using System.Collections.Concurrent;

public class UdpClientFourPlayers : MonoBehaviour
{
    public int myId = -1; // ID do jogador

    private UdpClient client;
    private Thread receiveThread;
    private IPEndPoint serverEP;

    public int Velocidade = 20;

    public GameObject localCube;
    public GameObject[] remoteCubes = new GameObject[3];
    public GameObject bola;

    private Vector3[] remotePositions = new Vector3[3];
    private ConcurrentQueue<string> messageQueue = new ConcurrentQueue<string>();

    void Start()
    {
        client = new UdpClient();
        serverEP = new IPEndPoint(IPAddress.Parse("10.57.1.27"), 5001); // IP do servidor
        client.Connect(serverEP);

        receiveThread = new Thread(ReceiveData);
        receiveThread.Start();

        client.Send(Encoding.UTF8.GetBytes("HELLO"), 5);

        if (bola != null)
        {
            bola.transform.position = Vector3.zero;
            var rb = bola.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;
        }
    }

    void Update()
    {
        while (messageQueue.TryDequeue(out string msg)) ProcessMessage(msg);

        if (myId == -1 || localCube == null) return;

        // Movimento vertical local
        float v = Input.GetAxis("Vertical");
        localCube.transform.Translate(Vector3.up * v * Time.deltaTime * Velocidade);

        // Limites Y
        Vector3 pos = localCube.transform.position;
        pos.y = Mathf.Clamp(pos.y, -3f, 3f);
        localCube.transform.position = pos;

        // Envia posição local
        string msgPos = $"POS:{myId};{localCube.transform.position.x.ToString("F2", CultureInfo.InvariantCulture)};" +
                        $"{localCube.transform.position.y.ToString("F2", CultureInfo.InvariantCulture)}";
        SendUdpMessage(msgPos);

        // Atualiza posições remotas
        for (int i = 0; i < 3; i++)
        {
            if (remoteCubes[i] != null)
                remoteCubes[i].transform.position = Vector3.Lerp(remoteCubes[i].transform.position, remotePositions[i], Time.deltaTime * 10f);
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
            Debug.Log($"Meu ID = {myId}");
            AssignPlayers();
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

                    // Mapear ID para remotePositions
                    int idx = GetRemoteIndex(id);
                    if (idx != -1) remotePositions[idx] = new Vector3(x, y, 0);
                }
            }
        }
        else if (msg.StartsWith("BALL:") && myId != 1)
        {
            string[] parts = msg.Substring(5).Split(';');
            if (parts.Length == 2)
            {
                float x = float.Parse(parts[0], CultureInfo.InvariantCulture);
                float y = float.Parse(parts[1], CultureInfo.InvariantCulture);
                if (bola != null) bola.transform.position = new Vector3(x, y, 0);
            }
        }
        else if (msg.StartsWith("SCORE:"))
        {
            string[] parts = msg.Substring(6).Split(';');
            if (parts.Length == 2)
            {
                int scoreA = int.Parse(parts[0]);
                int scoreB = int.Parse(parts[1]);
                if (bola != null)
                {
                    Bola bScript = bola.GetComponent<Bola>();
                    bScript.PontoA = scoreA;
                    bScript.PontoB = scoreB;
                    bScript.textoPontoA.text = scoreA.ToString();
                    bScript.textoPontoB.text = scoreB.ToString();
                }
            }
        }
    }

    int GetRemoteIndex(int id)
    {
        int idx = 0;
        for (int i = 1; i <= 4; i++)
        {
            if (i == myId) continue;
            if (i == id) return idx;
            idx++;
        }
        return -1;
    }

    void AssignPlayers()
    {
        // Assumindo nomes dos objetos no Inspector
        if (myId == 1)
        {
            localCube = GameObject.Find("Player1");
            remoteCubes[0] = GameObject.Find("Player2");
            remoteCubes[1] = GameObject.Find("Player3");
            remoteCubes[2] = GameObject.Find("Player4");
        }
        else if (myId == 2)
        {
            localCube = GameObject.Find("Player2");
            remoteCubes[0] = GameObject.Find("Player1");
            remoteCubes[1] = GameObject.Find("Player3");
            remoteCubes[2] = GameObject.Find("Player4");
        }
        else if (myId == 3)
        {
            localCube = GameObject.Find("Player3");
            remoteCubes[0] = GameObject.Find("Player1");
            remoteCubes[1] = GameObject.Find("Player2");
            remoteCubes[2] = GameObject.Find("Player4");
        }
        else if (myId == 4)
        {
            localCube = GameObject.Find("Player4");
            remoteCubes[0] = GameObject.Find("Player1");
            remoteCubes[1] = GameObject.Find("Player2");
            remoteCubes[2] = GameObject.Find("Player3");
        }
    }

    public void SendUdpMessage(string msg)
    {
        client.Send(Encoding.UTF8.GetBytes(msg), msg.Length);
    }

    void OnApplicationQuit()
    {
        if (receiveThread != null) receiveThread.Abort();
        if (client != null) client.Close();
    }
}