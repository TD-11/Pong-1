using UnityEngine;
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Concurrent;
using TMPro;

public class TcpClientChat : MonoBehaviour
{
    public string serverIP = "10.57.1.183";
    public int port = 6000;
    public int myId = -1;
    public TextMeshProUGUI chatText;

    private TcpClient client;
    private Thread receiveThread;
    private ConcurrentQueue<string> messageQueue = new ConcurrentQueue<string>();

    private string[] playerNames = { "Jogador 1.1", "Jogador 1.2", "Jogador 2.2", "Jogador 2.1" };

    void Start()
    {
        try
        {
            client = new TcpClient(serverIP, port);
            receiveThread = new Thread(ReceiveMessages);
            receiveThread.Start();

            Debug.Log("[Chat TCP] Conectado ao servidor");
        }
        catch (Exception e)
        {
            Debug.LogError("[Chat TCP] Erro ao conectar: " + e.Message);
        }
    }

    void Update()
    {
        // Mostra mensagens recebidas
        while (messageQueue.TryDequeue(out string msg))
        {
            if (chatText != null)
            {
                chatText.text = msg;
                CancelInvoke(nameof(LimparChat));
                Invoke(nameof(LimparChat), 3f);
            }
        }

        // Teclas rápidas
        if (Input.GetKeyDown(KeyCode.Alpha1)) EnviarChat("Boa!");
        if (Input.GetKeyDown(KeyCode.Alpha2)) EnviarChat("Foi por pouco!");
        if (Input.GetKeyDown(KeyCode.Alpha3)) EnviarChat("Defende aí!");
        if (Input.GetKeyDown(KeyCode.Alpha4)) EnviarChat("Toca pra mim!");
        if (Input.GetKeyDown(KeyCode.Alpha5)) EnviarChat("GG!");
    }

    void EnviarChat(string mensagem)
    {
        if (client == null || !client.Connected) return;

        string nome = (myId >= 1 && myId <= 4) ? playerNames[myId - 1] : "Jogador";
        string msg = $"{nome}: {mensagem}";
        byte[] data = Encoding.UTF8.GetBytes(msg);

        try
        {
            client.GetStream().Write(data, 0, data.Length);
        }
        catch (Exception e)
        {
            Debug.LogError("[Chat TCP] Falha ao enviar: " + e.Message);
        }
    }

    void ReceiveMessages()
    {
        NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[1024];

        try
        {
            while (true)
            {
                int bytes = stream.Read(buffer, 0, buffer.Length);
                if (bytes <= 0) break;

                string msg = Encoding.UTF8.GetString(buffer, 0, bytes);
                messageQueue.Enqueue(msg);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[Chat TCP] Desconectado: " + e.Message);
        }
    }

    void LimparChat()
    {
        if (chatText != null)
            chatText.text = "";
    }

    void OnApplicationQuit()
    {
        try
        {
            if (client != null) client.Close();
            if (receiveThread != null) receiveThread.Abort();
        }
        catch { }
    }
}