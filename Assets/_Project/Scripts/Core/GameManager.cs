using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NebulaGuard
{
    /// <summary>Os estados de uma partida, na ordem em que acontecem.</summary>
    public enum GameState
    {
        /// <summary>Contagem antes da primeira onda: dá para posicionar as naves.</summary>
        Preparing,
        Playing,
        Paused,
        Victory,
        Defeat,
    }

    /// <summary>
    /// Conduz a partida: aplica os números da dificuldade, começa as ondas, conta o tempo,
    /// pausa com Esc e decide vitória ou derrota. Não faz o trabalho dos outros: só escuta
    /// os eventos da nave-mãe e das ondas e avisa a mudança de estado por evento (o HUD escuta).
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [SerializeField] private DifficultyData difficulty;
        [SerializeField] private WaveSpawner waveSpawner;
        [SerializeField] private MotherShip motherShip;
        [SerializeField] private GameSession session;

        [Tooltip("Para fechar o menu de construção com Esc e bloquear construção na pausa e no fim.")]
        [SerializeField] private BuildController buildController;

        private GameState stateBeforePause;

        public GameState State { get; private set; } = GameState.Preparing;
        public DifficultyData Difficulty => difficulty;

        /// <summary>Segundos que faltam no cronômetro. Começa a contar na primeira onda.</summary>
        public float TimeRemaining { get; private set; }

        /// <summary>Avisa cada troca de estado (o HUD mostra pausa, vitória, derrota).</summary>
        public event Action<GameState> StateChanged;

        private void Awake()
        {
            // Se a cena foi recarregada depois de um fim de partida, o tempo estava parado.
            Time.timeScale = 1f;
        }

        private void OnEnable()
        {
            motherShip.Destroyed += OnMotherShipDestroyed;
            waveSpawner.WaveStarted += OnWaveStarted;
            waveSpawner.AllWavesCleared += OnAllWavesCleared;
        }

        private void OnDisable()
        {
            motherShip.Destroyed -= OnMotherShipDestroyed;
            waveSpawner.WaveStarted -= OnWaveStarted;
            waveSpawner.AllWavesCleared -= OnAllWavesCleared;
        }

        // Start roda depois do Awake de todos os objetos: os outros já estão prontos.
        private void Start()
        {
            if (difficulty == null)
            {
                Debug.LogError($"{name}: falta o DifficultyData no Inspector.", this);
                return;
            }

            session.Begin(difficulty.StartingCredits);
            motherShip.Begin(difficulty.MotherShipHealth);
            TimeRemaining = difficulty.TimeLimit;

            SetState(GameState.Preparing);
            waveSpawner.Begin(difficulty);
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
                OnEscape();

            // Time.deltaTime é 0 na pausa, então o cronômetro para sozinho.
            if (State == GameState.Playing)
            {
                TimeRemaining = Mathf.Max(0f, TimeRemaining - Time.deltaTime);
                if (TimeRemaining <= 0f)
                    EndGame(victory: false, reason: "o tempo acabou");
            }
        }

        /// <summary>Esc: primeiro fecha o menu de construção, se aberto; senão pausa ou retoma.</summary>
        private void OnEscape()
        {
            if (buildController != null && buildController.IsMenuOpen)
            {
                buildController.CloseMenu();
                return;
            }

            if (State == GameState.Paused)
                Resume();
            else if (State == GameState.Preparing || State == GameState.Playing)
                Pause();
        }

        public void Pause()
        {
            if (State != GameState.Preparing && State != GameState.Playing)
                return;

            stateBeforePause = State;
            Time.timeScale = 0f;
            SetBuildingAllowed(false);
            SetState(GameState.Paused);
        }

        public void Resume()
        {
            if (State != GameState.Paused)
                return;

            Time.timeScale = 1f;
            SetBuildingAllowed(true);
            SetState(stateBeforePause);
        }

        private void OnWaveStarted(int wave, int totalWaves)
        {
            // A primeira onda encerra a preparação e liga o cronômetro.
            if (State == GameState.Preparing)
                SetState(GameState.Playing);
        }

        private void OnAllWavesCleared()
        {
            EndGame(victory: true, reason: "todas as ondas foram eliminadas");
        }

        private void OnMotherShipDestroyed()
        {
            EndGame(victory: false, reason: "a nave-mãe foi destruída");
        }

        private void EndGame(bool victory, string reason)
        {
            // Pode chegar mais de um motivo no mesmo frame; vale o primeiro.
            if (State == GameState.Victory || State == GameState.Defeat)
                return;

            waveSpawner.Stop();
            SetBuildingAllowed(false);
            Time.timeScale = 0f; // congela inimigos, naves e projéteis

            Debug.Log($"Fim de partida: {(victory ? "VITÓRIA" : "DERROTA")} ({reason}). Pontos: {session.Score}.");
            SetState(victory ? GameState.Victory : GameState.Defeat);
        }

        private void SetBuildingAllowed(bool allowed)
        {
            if (buildController == null)
                return;

            if (!allowed)
                buildController.CloseMenu();
            buildController.enabled = allowed;
        }

        private void SetState(GameState newState)
        {
            State = newState;
            StateChanged?.Invoke(newState);
        }
    }
}
