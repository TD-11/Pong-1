using UnityEngine;
using TMPro;

public class Bola : MonoBehaviour
{
    private Rigidbody2D rb;
    private UdpClientTwoClients udpClient;
    private bool bolaLancada = false;

    public int PontoA = 0;
    public int PontoB = 0;

    [Header("UI")]
    public TextMeshProUGUI textoPontoA;
    public TextMeshProUGUI textoPontoB;
    public TextMeshProUGUI VitoriaTime1;
    public TextMeshProUGUI VitoriaTime2;

    [Header("Configuração da Bola")]
    public float velocidade = 5f;    // Velocidade base
    public float fatorDesvio = 2f;   // Influência do ponto de contato no ângulo
    public int pontosParaVencer = 5; // Placar máximo

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        udpClient = FindObjectOfType<UdpClientTwoClients>();

        // Só o servidor (ID 4) lança a bola
        if (udpClient != null && udpClient.myId == 4)
        {
            Invoke(nameof(LancarBola), 1f);
        }
    }

    void LancarBola()
    {
        float dirX = Random.Range(0, 2) == 0 ? -1 : 1;
        float dirY = Random.Range(-0.5f, 0.5f);
        rb.linearVelocity = new Vector2(dirX, dirY).normalized * velocidade;
        bolaLancada = true;
    }

    void Update()
    {
        if (udpClient == null) return;

        // Somente o servidor envia posição da bola
        if (udpClient.myId == 4 && bolaLancada)
        {
            string msg = $"BALL:{transform.position.x.ToString(System.Globalization.CultureInfo.InvariantCulture)};" +
                         $"{transform.position.y.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
            udpClient.SendUdpMessage(msg);
        }
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (udpClient == null) return;

        if (col.gameObject.CompareTag("Raquete"))
        {
            Rebater(col);
        }
        else if (col.gameObject.CompareTag("Gol1"))
        {
            PontoB++;
            AtualizarPlacar();
        }
        else if (col.gameObject.CompareTag("Gol2"))
        {
            PontoA++;
            AtualizarPlacar();
        }
    }

    void Rebater(Collision2D col)
    {
        float posYbola = transform.position.y;
        float posYraquete = col.transform.position.y;
        float alturaRaquete = col.collider.bounds.size.y;

        float diferenca = (posYbola - posYraquete) / (alturaRaquete / 2f);

        Vector2 direcao = new Vector2(Mathf.Sign(rb.linearVelocity.x), diferenca * fatorDesvio);
        rb.linearVelocity = direcao.normalized * velocidade;
    }

    void AtualizarPlacar()
    {
        textoPontoA.text = PontoA.ToString();
        textoPontoB.text = PontoB.ToString();

        string msg = $"SCORE:{PontoA};{PontoB}";
        udpClient.SendUdpMessage(msg);

        if (PontoA >= pontosParaVencer || PontoB >= pontosParaVencer)
        {
            GameOver();
        }
        else
        {
            ResetBola();
        }
    }

    void ResetBola()
    {
        transform.position = Vector3.zero;
        rb.linearVelocity = Vector2.zero;
        bolaLancada = false;

        if (udpClient.myId == 4)
            Invoke(nameof(LancarBola), 1f);
    }

    void GameOver()
    {
        transform.position = Vector3.zero;
        rb.linearVelocity = Vector2.zero;
        bolaLancada = false;

        bool time1Venceu = PontoA >= pontosParaVencer;

        if (time1Venceu && (udpClient.myId == 1 || udpClient.myId == 3))
            VitoriaTime1.gameObject.SetActive(true);
        else if (!time1Venceu && (udpClient.myId == 2 || udpClient.myId == 4))
            VitoriaTime2.gameObject.SetActive(true);

        string msg = $"GAMEOVER:{(time1Venceu ? "A" : "B")}";
        udpClient.SendUdpMessage(msg);
    }

    public void AtualizarPosicaoRemota(float x, float y)
    {
        transform.position = new Vector3(x, y, 0);
    }

    public void AtualizarPlacarRemoto(int a, int b)
    {
        PontoA = a;
        PontoB = b;
        textoPontoA.text = PontoA.ToString();
        textoPontoB.text = PontoB.ToString();
    }

    public void MostrarVitoriaRemota(string vencedor)
    {
        if (vencedor == "A")
            VitoriaTime1.gameObject.SetActive(true);
        else
            VitoriaTime2.gameObject.SetActive(true);
    }
}