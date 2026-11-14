using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace NebulaGuard
{
    /// <summary>
    /// Interface da partida: barra do topo (vida da nave-mãe, onda, cronômetro, créditos,
    /// pontos), contagem antes de cada onda, tela de pausa e tela de fim. Os elementos são
    /// montados no editor (Canvas); este script só escuta os eventos do jogo e atualiza os
    /// textos. Nenhum script de jogo conhece o HUD.
    /// </summary>
    public class HUD : MonoBehaviour
    {
        [Header("De onde vêm as informações")]
        [SerializeField] private GameManager gameManager;
        [SerializeField] private WaveSpawner waveSpawner;
        [SerializeField] private GameSession session;
        [SerializeField] private MotherShip motherShip;

        [Header("Barra do topo")]
        [Tooltip("Image com Image Type = Filled, Fill Method = Horizontal.")]
        [SerializeField] private Image healthFill;
        [SerializeField] private TMP_Text healthText;
        [SerializeField] private TMP_Text waveText;
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private TMP_Text creditsText;
        [SerializeField] private TMP_Text scoreText;

        [Header("Contagem antes da onda")]
        [SerializeField] private TMP_Text countdownText;

        [Header("Pausa")]
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private Button resumeButton;

        [Header("Fim de partida")]
        [SerializeField] private GameObject endPanel;
        [SerializeField] private TMP_Text endTitleText;
        [SerializeField] private TMP_Text endScoreText;
        [SerializeField] private Button restartButton;

        [Header("Cores")]
        [SerializeField] private Color victoryColor = new Color(0.35f, 1f, 0.55f);
        [SerializeField] private Color defeatColor = new Color(1f, 0.35f, 0.35f);

        private float countdownRemaining;
        private int countdownWave;

        private void Awake()
        {
            // Os botões são ligados aqui, e não no OnClick do Inspector: menos um passo
            // manual para esquecer.
            if (restartButton != null)
                restartButton.onClick.AddListener(Restart);
            if (resumeButton != null)
                resumeButton.onClick.AddListener(gameManager.Resume);

            pausePanel.SetActive(false);
            endPanel.SetActive(false);
            countdownText.gameObject.SetActive(false);
        }

        // OnEnable de todos os objetos roda antes de qualquer Start: assim o HUD já está
        // escutando quando o GameManager.Start aplica créditos, vida e começa as ondas.
        private void OnEnable()
        {
            gameManager.StateChanged += OnStateChanged;
            waveSpawner.WaveCountdownStarted += OnWaveCountdownStarted;
            waveSpawner.WaveStarted += OnWaveStarted;
            session.CreditsChanged += OnCreditsChanged;
            session.ScoreChanged += OnScoreChanged;
            motherShip.HealthChanged += OnHealthChanged;
        }

        private void OnDisable()
        {
            gameManager.StateChanged -= OnStateChanged;
            waveSpawner.WaveCountdownStarted -= OnWaveCountdownStarted;
            waveSpawner.WaveStarted -= OnWaveStarted;
            session.CreditsChanged -= OnCreditsChanged;
            session.ScoreChanged -= OnScoreChanged;
            motherShip.HealthChanged -= OnHealthChanged;
        }

        private void Update()
        {
            // O cronômetro muda todo frame, então é lido direto em vez de ter um evento.
            timerText.text = FormatTime(gameManager.TimeRemaining);

            if (countdownRemaining > 0f)
            {
                // Time.deltaTime: na pausa a contagem para junto com as ondas.
                countdownRemaining -= Time.deltaTime;
                int seconds = Mathf.Max(1, Mathf.CeilToInt(countdownRemaining));
                countdownText.text = $"Onda {countdownWave} em {seconds}";
            }
        }

        private void OnWaveCountdownStarted(int wave, float seconds)
        {
            countdownWave = wave;
            countdownRemaining = seconds;
            countdownText.gameObject.SetActive(seconds > 0f);

            // Antes da primeira onda ainda não há onda atual para mostrar.
            if (wave == 1)
                waveText.text = $"Onda 0/{waveSpawner.TotalWaves}";
        }

        private void OnWaveStarted(int wave, int totalWaves)
        {
            countdownRemaining = 0f;
            countdownText.gameObject.SetActive(false);
            waveText.text = $"Onda {wave}/{totalWaves}";
        }

        private void OnCreditsChanged(int credits) => creditsText.text = $"Créditos: {credits}";

        private void OnScoreChanged(int score) => scoreText.text = $"Pontos: {score}";

        private void OnHealthChanged(int current, int max)
        {
            healthFill.fillAmount = max > 0 ? (float)current / max : 0f;
            if (healthText != null)
                healthText.text = $"{current}/{max}";
        }

        private void OnStateChanged(GameState state)
        {
            pausePanel.SetActive(state == GameState.Paused);

            if (state == GameState.Victory || state == GameState.Defeat)
            {
                bool victory = state == GameState.Victory;
                countdownText.gameObject.SetActive(false);
                endPanel.SetActive(true);
                endTitleText.text = victory ? "VITÓRIA" : "DERROTA";
                endTitleText.color = victory ? victoryColor : defeatColor;
                endScoreText.text = $"Pontos: {session.Score}";
            }
        }

        /// <summary>Recarrega a cena atual: a partida começa do zero.</summary>
        private void Restart()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private static string FormatTime(float seconds)
        {
            int total = Mathf.CeilToInt(seconds);
            return $"{total / 60}:{total % 60:00}";
        }
    }
}
