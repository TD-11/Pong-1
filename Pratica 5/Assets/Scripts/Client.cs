using UnityEngine;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System.Globalization;
using System.Collections.Concurrent;

public class UdpClientTwoClients : MonoBehaviour
{
    public int myId = -1; // Agora público para a Bola acessar
    UdpClient client;
    Thread receiveThread;
    IPEndPoint serverEP;

    private Vector3 remotePos; // não começa mais em zero
    public int Velocidade = 20;
    private float minY;
    private float maxY;
    
    public GameObject localCube;
    public GameObject remoteCube1;
    public GameObject remoteCube2;
    public GameObject remoteCube3;
    public GameObject bola; // referência à bola no Inspector

    // Fila segura para passar mensagens da thread de rede -> main thread
    private ConcurrentQueue<string> messageQueue = new ConcurrentQueue<string>();

    void Start()
    {
        client = new UdpClient();
        serverEP = new IPEndPoint(IPAddress.Parse("10.57.1.27"), 5001);
        client.Connect(serverEP);

        receiveThread = new Thread(ReceiveData);
        receiveThread.Start();

        client.Send(Encoding.UTF8.GetBytes("HELLO"), 5);

        // bola sempre começa no centro
        if (bola != null)
        {
            bola.transform.position = Vector3.zero;
            var rb = bola.GetComponent<Rigidbody2D>();
            if (rb != null)
                rb.linearVelocity = Vector2.zero;
        }
    }

    void Update()
    {
        // processa mensagens vindas da thread de rede
        while (messageQueue.TryDequeue(out string msg))
        {
            ProcessMessage(msg);
        }

        if (myId == -1 || localCube == null) return;

        // Movimento vertical da raquete
        float v = Input.GetAxis("Vertical");
        localCube.transform.Translate(new Vector3(0, v, 0) * Time.deltaTime * Velocidade);

        // Limite no eixo Y
        Vector3 pos = localCube.transform.position;
        
        if (myId == 1 || myId == 2)
        {
            minY = 1.5f;
            maxY = 3f;
        }
        else if (myId == 3 || myId == 4)
        {
            minY = -3f;
            maxY = -1.5f;
        }
        
        pos.y = Mathf.Clamp(pos.y, minY, maxY);
        localCube.transform.position = pos;

        // Envia posição da raquete
        string msgPos = "POS:" + myId + ";" + localCube.transform.position.x.ToString("F2", CultureInfo.InvariantCulture) + ";" + localCube.transform.position.y.ToString("F2", CultureInfo.InvariantCulture);

        SendUdpMessage(msgPos);

        // Atualiza posição do outro jogador suavemente
        if (remoteCube1 != null)
        {
            remoteCube1.transform.position = Vector3.Lerp(remoteCube1.transform.position, remotePos, Time.deltaTime * 10f);
        }
        if (remoteCube2 != null)
        {
            remoteCube2.transform.position = Vector3.Lerp(remoteCube2.transform.position, remotePos, Time.deltaTime * 10f);
        }
        if (remoteCube3 != null)
        {
            remoteCube3.transform.position = Vector3.Lerp(remoteCube3.transform.position, remotePos, Time.deltaTime * 10f);
        }
    }

    void ReceiveData()
    {
        IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);

        while (true)
        {
            byte[] data = client.Receive(ref remoteEP);
            string msg = Encoding.UTF8.GetString(data);

            // joga mensagem na fila
            messageQueue.Enqueue(msg);
        }
    }

    void ProcessMessage(string msg)
    {
        if (msg.StartsWith("ASSIGN:"))
        {
            myId = int.Parse(msg.Substring(7));
            Debug.Log("[Cliente] Meu ID = " + myId);

            if (myId == 1)
            {
                localCube = GameObject.Find("Player 1");
                remoteCube1 = GameObject.Find("Player 1.1");
                remoteCube2 = GameObject.Find("Player 2");
                remoteCube3 = GameObject.Find("Player 2.2");


                localCube.transform.position = new Vector3(-8f, 2f, 0f); // Esquerda
                remoteCube1.transform.position = new Vector3(-8f, -2f, 0f);  // Direita
                remoteCube2.transform.position = new Vector3(8f, 2f, 0f);  // Direita
                remoteCube3.transform.position = new Vector3(8f, -2f, 0f); // Esquerda

                // Inicializa remotePos corretamente
                remotePos = remoteCube1.transform.position;
                remotePos = remoteCube2.transform.position;
                remotePos = remoteCube3.transform.position;

            }
            else if (myId == 2)
            {
                localCube = GameObject.Find("Player 2");
                remoteCube1 = GameObject.Find("Player 2.2");
                remoteCube2 = GameObject.Find("Player 1");
                remoteCube3 = GameObject.Find("Player 1.1");

                localCube.transform.position = new Vector3(8f, 2f, 0f);   // Direita
                remoteCube1.transform.position = new Vector3(8f, -2f, 0f); // Esquerda
                remoteCube2.transform.position = new Vector3(-8f, 2f, 0f); // Esquerda
                remoteCube3.transform.position = new Vector3(-8f, -2f, 0f);   // Direita

                // Inicializa remotePos corretamente
                remotePos = remoteCube1.transform.position;
                remotePos = remoteCube2.transform.position;
                remotePos = remoteCube3.transform.position;

            }
            else if (myId == 3)
            {
                localCube = GameObject.Find("Player 1.1");
                remoteCube1 = GameObject.Find("Player 1");
                remoteCube3 = GameObject.Find("Player 2.2");
                remoteCube2 = GameObject.Find("Player 2");

                localCube.transform.position = new Vector3(-8f, -2f, 0f);   // Direita
                remoteCube1.transform.position = new Vector3(-8f, 2f, 0f); // Esquerda
                remoteCube2.transform.position = new Vector3(8f, -2f, 0f); // Esquerda
                remoteCube3.transform.position = new Vector3(8f, 2f, 0f);   // Direita

                // Inicializa remotePos corretamente
                remotePos = remoteCube1.transform.position;
                remotePos = remoteCube2.transform.position;
                remotePos = remoteCube3.transform.position;

            }
            else if (myId == 4)
            {
                localCube = GameObject.Find("Player 2.2");
                remoteCube1 = GameObject.Find("Player 2");
                remoteCube3 = GameObject.Find("Player 1.1");
                remoteCube2 = GameObject.Find("Player 1");

                localCube.transform.position = new Vector3(8f, -2f, 0f);   // Direita
                remoteCube1.transform.position = new Vector3(8f, 2f, 0f); // Esquerda
                remoteCube2.transform.position = new Vector3(-8f, -2f, 0f); // Esquerda
                remoteCube3.transform.position = new Vector3(-8f, 2f, 0f);   // Direita

                // Inicializa remotePos corretamente
                remotePos = remoteCube1.transform.position;
                remotePos = remoteCube2.transform.position;
                remotePos = remoteCube3.transform.position;

            }

            // Reset da bola
            if (bola != null)
            {
                bola.transform.position = Vector3.zero;
                var rb = bola.GetComponent<Rigidbody2D>();
                if (rb != null)
                    rb.linearVelocity = Vector2.zero;
            }
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
                    remotePos = new Vector3(x, y, 0);
                }
            }
        }
        else if (msg.StartsWith("BALL:"))
        {
            // Só atualiza se não for o host da bola (ID 2)
            if (myId != 2)
            {
                string[] parts = msg.Substring(5).Split(';');
                if (parts.Length == 2)
                {
                    float x = float.Parse(parts[0], CultureInfo.InvariantCulture);
                    float y = float.Parse(parts[1], CultureInfo.InvariantCulture);

                    if (bola != null)
                        bola.transform.position = new Vector3(x, y, 0);
                }
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
                    Bola bolaScript = bola.GetComponent<Bola>();
                    bolaScript.PontoA = scoreA;
                    bolaScript.PontoB = scoreB;
                    bolaScript.textoPontoA.text = scoreA.ToString();
                    bolaScript.textoPontoB.text = scoreB.ToString();
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
