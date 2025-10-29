using UnityEngine;
using TMPro;

public class Bola : MonoBehaviour
{
    private Rigidbody2D rb;
    private UdpClientFourPlayers udpClient;
    private bool bolaLancada = false;

    public int PontoA = 0;
    public int PontoB = 0;

    public TextMeshProUGUI textoPontoA;
    public TextMeshProUGUI textoPontoB;
    public TextMeshProUGUI VitoriaTime1;
    public TextMeshProUGUI VitoriaTime2;

    public float velocidade = 5f; // Velocidade da bola
    public float fatorDesvio = 2f; // Influência do ponto de contato

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        udpClient = FindObjectOfType<UdpClientFourPlayers>();

        if (udpClient != null && udpClient.myId == 1) // Host da bola
        {
            Invoke("LancarBola", 1f);
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

        // Host envia posição da bola
        if (udpClient.myId == 1 && bolaLancada)
        {
            string msg = $"BALL:{transform.position.x.ToString(System.Globalization.CultureInfo.InvariantCulture)};" +
                         $"{transform.position.y.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
            udpClient.SendUdpMessage(msg);
        }
    }

    void OnCollisionEnter2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("Raquete"))
        {
            float posYbola = transform.position.y;
            float posYraquete = col.transform.position.y;
            float alturaRaquete = col.collider.bounds.size.y;
            float diferenca = (posYbola - posYraquete) / (alturaRaquete / 2f);

            Vector2 direcao = new Vector2(Mathf.Sign(rb.linearVelocity.x), diferenca * fatorDesvio);
            rb.linearVelocity = direcao.normalized * velocidade;
        }
        else if (col.gameObject.CompareTag("Gol1"))
        {
            PontoB++;
            textoPontoB.text = PontoB.ToString();
            ResetBola();
        }
        else if (col.gameObject.CompareTag("Gol2"))
        {
            PontoA++;
            textoPontoA.text = PontoA.ToString();
            ResetBola();
        }
    }

    void ResetBola()
    {
        transform.position = Vector3.zero;
        rb.linearVelocity = Vector2.zero;
        bolaLancada = false;

        if (PontoA > 5 || PontoB > 5)
        {
            GameOver();
        }
        else if (udpClient != null && udpClient.myId == 1)
        {
            Invoke("LancarBola", 1f);
            string msgScore = $"SCORE:{PontoA};{PontoB}";
            udpClient.SendUdpMessage(msgScore);
        }
    }

    void GameOver()
    {
        transform.position = Vector3.zero;
        rb.linearVelocity = Vector2.zero;

        if (PontoA > 5) VitoriaTime1.gameObject.SetActive(true);
        else if (PontoB > 5) VitoriaTime2.gameObject.SetActive(true);
    }
}