using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class UdpClientTwoClients : MonoBehaviour
{
    public int myId; // 1, 1.1, 2, 2.2 ou 4 (servidor)
    public GameObject localCube;
    public GameObject remoteCube1;
    public GameObject remoteCube2;
    public GameObject remoteCube3;
    public GameObject bola;

    private Vector3 remotePos1;
    private Vector3 remotePos2;
    private Vector3 remotePos3;

    private Vector3 smoothVel1;
    private Vector3 smoothVel2;
    private Vector3 smoothVel3;

    private UdpClient udp;
    private IPEndPoint remoteEndPoint;
    private Thread receiveThread;

    private bool bolaAtiva = false;
    private Vector3 bolaPos;

    void Start()
    {
        udp = new UdpClient(5000 + myId);
        remoteEndPoint = new IPEndPoint(IPAddress.Broadcast, 5000);

        udp.EnableBroadcast = true;

        receiveThread = new Thread(new ThreadStart(ReceiveData));
        receiveThread.IsBackground = true;
        receiveThread.Start();
    }

    void Update()
    {
        // MOVIMENTO LOCAL (apenas quem controla a raquete)
        if (localCube != null && myId != 4)
        {
            float move = Input.GetAxisRaw("Vertical");
            localCube.transform.Translate(Vector3.up * move * 5f * Time.deltaTime);

            string msg = "POS:" +
                         myId + ";" +
                         localCube.transform.position.x.ToString(System.Globalization.CultureInfo.InvariantCulture) +
                         ";" +
                         localCube.transform.position.y.ToString(System.Globalization.CultureInfo.InvariantCulture);

            SendUdpMessage(msg);
        }

        // MOVIMENTO REMOTO (interpolação suave)
        if (remoteCube1 != null)
            remoteCube1.transform.position =
                Vector3.SmoothDamp(remoteCube1.transform.position, remotePos1, ref smoothVel1, 0.1f);

        if (remoteCube2 != null)
            remoteCube2.transform.position =
                Vector3.SmoothDamp(remoteCube2.transform.position, remotePos2, ref smoothVel2, 0.1f);

        if (remoteCube3 != null)
            remoteCube3.transform.position =
                Vector3.SmoothDamp(remoteCube3.transform.position, remotePos3, ref smoothVel3, 0.1f);

        // ATUALIZA POSIÇÃO DA BOLA (para todos, exceto host)
        if (bola != null && myId != 4 && bolaAtiva)
            bola.transform.position = bolaPos;
    }

    private void ReceiveData()
    {
        IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);
        try
        {
            while (true)
            {
                byte[] data = udp.Receive(ref anyIP);
                string text = Encoding.UTF8.GetString(data);
                ProcessMessage(text);
            }
        }
        catch (Exception e)
        {
            Debug.Log("Erro na thread UDP: " + e.Message);
        }
    }

    public void SendUdpMessage(string msg)
    {
        try
        {
            byte[] data = Encoding.UTF8.GetBytes(msg);
            udp.Send(data, data.Length, remoteEndPoint);
        }
        catch (Exception e)
        {
            Debug.Log("Erro ao enviar UDP: " + e.Message);
        }
    }

    private void ProcessMessage(string msg)
    {
        if (string.IsNullOrEmpty(msg)) return;

        // --------------------------
        // POSIÇÃO DAS RAQUETES
        // --------------------------
        if (msg.StartsWith("POS:"))
        {
            string[] parts = msg.Substring(4).Split(';');
            if (parts.Length < 3) return;

            if (!float.TryParse(parts[1], System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out float x)) return;
            if (!float.TryParse(parts[2], System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out float y)) return;

            int senderId = (int)float.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture);

            Vector3 receivedPos = new Vector3(x, y, 0);

            // Evita sobrescrever a posição do próprio jogador
            if (senderId == myId) return;

            // Atribui ao cubo remoto correto
            if (remoteCube1 != null && remoteCube1.name.Contains(senderId.ToString()))
                remotePos1 = receivedPos;
            else if (remoteCube2 != null && remoteCube2.name.Contains(senderId.ToString()))
                remotePos2 = receivedPos;
            else if (remoteCube3 != null && remoteCube3.name.Contains(senderId.ToString()))
                remotePos3 = receivedPos;
        }

        // --------------------------
        // POSIÇÃO DA BOLA
        // --------------------------
        else if (msg.StartsWith("BALL:"))
        {
            string[] parts = msg.Substring(5).Split(';');
            if (parts.Length < 2) return;

            if (float.TryParse(parts[0], System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out float x) &&
                float.TryParse(parts[1], System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out float y))
            {
                bolaPos = new Vector3(x, y, 0);
                bolaAtiva = true;
            }
        }

        // --------------------------
        // PLACAR
        // --------------------------
        else if (msg.StartsWith("SCORE:"))
        {
            Debug.Log("Placar recebido: " + msg);
        }

        // --------------------------
        // GAME OVER
        // --------------------------
        else if (msg.StartsWith("GAMEOVER:"))
        {
            Debug.Log("Fim de jogo recebido: " + msg);
        }
    }

    private void OnApplicationQuit()
    {
        if (receiveThread != null) receiveThread.Abort();
        if (udp != null) udp.Close();
    }
}