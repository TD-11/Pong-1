using UnityEngine;
using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Concurrent;
using TMPro;

public class TcpClientChat : MonoBehaviour
{
    [Header("Configuração do Servidor")]
    public string serverIP = "10.57.10.23"; // IP da máquina do servidor
    public int port = 6000;

    [Header("HUD do Chat")]
    public TextMeshProUGUI chatText;

    [Header("Jogador")]
    public int myId = -1; // defina de acordo com o seu jogo (1 a 4)
    private string[] playerNames = { "Jogador 1.1", "Jogador 1.2", "Jogador 2.2", "Jogador 2.1" };

    private TcpClient client;
    private Thread receiveThread;
    private ConcurrentQueue<string> messageQueue = new ConcurrentQueue<string>();

    // Mensagens rápidas
    private string[] mensagensRapidas = {
        "Boa!",
        "Foi por pouco!",
        "Defende aí!",
        "Toca pra mim!",
        "GG!"
    };

    void Start()
    {
        try
        {
            client = new TcpClient(serverIP, port);
            receiveThread = new Thread(ReceiveMessages);
            receiveThread.Start();
            Debug.Log("[Chat TCP] Conectado ao servidor de chat");
        }
        catch (Exception e)
        {
            Debug.LogError("[Chat TCP] Erro ao conectar: " + e.Message);
        }

        if (chatText != null)
            chatText.text = "";
    }

    void Update()
    {
        // Exibir mensagens recebidas
        while (messageQueue.TryDequeue(out string msg))
        {
            MostrarMensagem(msg);
        }

        // Teclas rápidas (1 a 5)
        if (Input.GetKeyDown(KeyCode.Alpha1)) EnviarMensagemRapida(0);
        if (Input.GetKeyDown(KeyCode.Alpha2)) EnviarMensagemRapida(1);
        if (Input.GetKeyDown(KeyCode.Alpha3)) EnviarMensagemRapida(2);
        if (Input.GetKeyDown(KeyCode.Alpha4)) EnviarMensagemRapida(3);
        if (Input.GetKeyDown(KeyCode.Alpha5)) EnviarMensagemRapida(4);
    }

    void EnviarMensagemRapida(int index)
    {
        if (client == null || !client.Connected) return;
        if (index < 0 || index >= mensagensRapidas.Length) return;

        string nome = (myId >= 1 && myId <= 4) ? playerNames[myId - 1] : "Jogador";
        string msg = $"{nome}: {mensagensRapidas[index]}";
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

    void MostrarMensagem(string texto)
    {
        if (chatText == null) return;

        StopAllCoroutines();
        chatText.text = texto;
        chatText.color = new Color(1f, 1f, 1f, 0.9f);
        StartCoroutine(LimparMensagem());
    }

    System.Collections.IEnumerator LimparMensagem()
    {
        yield return new WaitForSeconds(3f);
        if (chatText != null)
            chatText.text = "";
    }

    void OnApplicationQuit()
    {
        try
        {
            client?.Close();
            receiveThread?.Abort();
        }
        catch { }
    }
}