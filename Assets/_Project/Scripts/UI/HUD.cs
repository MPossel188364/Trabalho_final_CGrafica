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

        [Tooltip("Cor da barra conforme a vida: esquerda = vazia, direita = cheia.")]
        [SerializeField] private Gradient healthColors = DefaultHealthColors();
        [SerializeField] private TMP_Text healthText;
        [SerializeField] private TMP_Text waveText;
        [SerializeField] private TMP_Text timerText;
        [SerializeField] private TMP_Text creditsText;
        [SerializeField] private TMP_Text scoreText;

        [Tooltip("Abaixo destes segundos, o cronômetro fica na cor de alerta e pulsa.")]
        [SerializeField] private float timerWarningSeconds = 30f;
        [SerializeField] private Color timerWarningColor = new Color(1f, 0.35f, 0.3f);

        [Header("Contagem antes da onda")]
        [SerializeField] private TMP_Text countdownText;

        [Header("Pausa")]
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private Button resumeButton;

        [Tooltip("Opcional: botão \"Menu principal\" da tela de pausa.")]
        [SerializeField] private Button pauseMenuButton;

        [Header("Fim de partida")]
        [SerializeField] private GameObject endPanel;
        [SerializeField] private TMP_Text endTitleText;
        [SerializeField] private TMP_Text endScoreText;

        [Tooltip("Opcional: frase com o motivo do fim (\"A nave-mãe foi destruída\").")]
        [SerializeField] private TMP_Text endReasonText;
        [SerializeField] private Button restartButton;

        [Tooltip("Opcional: botão \"Menu principal\" da tela de fim.")]
        [SerializeField] private Button endMenuButton;

        [Tooltip("Nome da cena do menu principal (precisa estar no Build Settings).")]
        [SerializeField] private string menuSceneName = "MainMenu";

        [Tooltip("Segundos do fade da tela de fim.")]
        [SerializeField] private float endFadeDuration = 0.6f;

        [Header("Cores")]
        [SerializeField] private Color victoryColor = new Color(0.35f, 1f, 0.55f);
        [SerializeField] private Color defeatColor = new Color(1f, 0.35f, 0.35f);

        private float countdownRemaining;
        private int countdownWave;
        private Color timerNormalColor;
        private CanvasGroup endGroup;
        private float endFadeTime = -1f;

        private void Awake()
        {
            // Os botões são ligados aqui, e não no OnClick do Inspector: menos um passo
            // manual para esquecer.
            if (restartButton != null)
                restartButton.onClick.AddListener(Restart);
            if (resumeButton != null)
                resumeButton.onClick.AddListener(gameManager.Resume);
            if (pauseMenuButton != null)
                pauseMenuButton.onClick.AddListener(BackToMenu);
            if (endMenuButton != null)
                endMenuButton.onClick.AddListener(BackToMenu);

            // Som de clique em todos os botões da tela.
            foreach (Button button in new[] { restartButton, resumeButton, pauseMenuButton, endMenuButton })
            {
                if (button != null)
                    button.onClick.AddListener(AudioManager.PlayClick);
            }

            pausePanel.SetActive(false);
            endPanel.SetActive(false);
            countdownText.gameObject.SetActive(false);

            timerNormalColor = timerText.color;

            // CanvasGroup controla a transparência do painel inteiro (fundo, textos e botão).
            if (!endPanel.TryGetComponent(out endGroup))
                endGroup = endPanel.AddComponent<CanvasGroup>();
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
            float time = gameManager.TimeRemaining;
            timerText.text = FormatTime(time);

            // Pouco tempo: cor de alerta pulsando uma vez por segundo (só durante a partida).
            bool warning = gameManager.State == GameState.Playing && time <= timerWarningSeconds;
            float pulse = 0.5f + 0.5f * Mathf.Cos(time * Mathf.PI * 2f);
            timerText.color = warning ? Color.Lerp(timerNormalColor, timerWarningColor, 0.4f + 0.6f * pulse) : timerNormalColor;

            if (countdownRemaining > 0f)
            {
                // Time.deltaTime: na pausa a contagem para junto com as ondas.
                countdownRemaining -= Time.deltaTime;
                int seconds = Mathf.Max(1, Mathf.CeilToInt(countdownRemaining));
                countdownText.text = $"Onda {countdownWave} em {seconds}";
            }

            UpdateEndFade();
        }

        // O fim de partida para o tempo (timeScale = 0), então o fade usa o tempo real.
        private void UpdateEndFade()
        {
            if (endFadeTime < 0f)
                return;

            endFadeTime += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(endFadeTime / Mathf.Max(0.01f, endFadeDuration));
            float eased = 1f - (1f - t) * (1f - t); // começa rápido e desacelera no fim
            endGroup.alpha = eased;
            endPanel.transform.localScale = Vector3.one * Mathf.Lerp(0.92f, 1f, eased);

            if (t >= 1f)
                endFadeTime = -1f;
        }

        private void OnWaveCountdownStarted(int wave, float seconds)
        {
            countdownWave = wave;
            countdownRemaining = seconds;
            countdownText.gameObject.SetActive(seconds > 0f);

            // Antes da primeira onda ainda não há onda atual para mostrar.
            if (wave == 1)
                waveText.text = $"0/{waveSpawner.TotalWaves}";
        }

        private void OnWaveStarted(int wave, int totalWaves)
        {
            countdownRemaining = 0f;
            countdownText.gameObject.SetActive(false);
            waveText.text = $"{wave}/{totalWaves}";
        }

        // Só o número: o ícone ao lado (raio = créditos, estrela = pontos) diz o que é.
        private void OnCreditsChanged(int credits) => creditsText.text = credits.ToString();

        private void OnScoreChanged(int score) => scoreText.text = score.ToString();

        private void OnHealthChanged(int current, int max)
        {
            float fraction = max > 0 ? (float)current / max : 0f;
            healthFill.fillAmount = fraction;
            healthFill.color = healthColors.Evaluate(fraction);
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
                endScoreText.text = ScoreLine();
                if (endReasonText != null)
                    endReasonText.text = Capitalize(gameManager.EndReason);

                endGroup.alpha = 0f;
                endFadeTime = 0f;
            }
        }

        /// <summary>Recarrega a cena atual: a partida começa do zero.</summary>
        private void Restart()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        /// <summary>Volta ao menu principal. Tira a pausa antes: a cena nova não pode nascer parada.</summary>
        private void BackToMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(menuSceneName);
        }

        // "Pontos: 120", com a posição quando a partida entrou no ranking (top 5).
        private string ScoreLine()
        {
            int position = gameManager.LastRankingPosition;
            if (position == 1)
                return $"Pontos: {session.Score} - novo recorde!";
            if (position > 1)
                return $"Pontos: {session.Score} - #{position} no ranking";
            return $"Pontos: {session.Score}";
        }

        private static string Capitalize(string text) =>
            string.IsNullOrEmpty(text) ? text : char.ToUpper(text[0]) + text.Substring(1);

        // Vermelho com pouca vida, amarelo na metade, verde cheia.
        private static Gradient DefaultHealthColors()
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.95f, 0.25f, 0.25f), 0f),
                    new GradientColorKey(new Color(1f, 0.8f, 0.2f), 0.5f),
                    new GradientColorKey(new Color(0.3f, 0.9f, 0.4f), 1f),
                },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            return gradient;
        }

        private static string FormatTime(float seconds)
        {
            int total = Mathf.CeilToInt(seconds);
            return $"{total / 60}:{total % 60:00}";
        }
    }
}
