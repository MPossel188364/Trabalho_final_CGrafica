using System;
using System.Collections;
using UnityEngine;

namespace NebulaGuard
{
    /// <summary>
    /// Gera os inimigos em ondas, conforme o DifficultyData: espera a preparação, solta os
    /// inimigos da onda um a um pela ponte de luz e só passa para a próxima quando todos
    /// da onda atual morreram ou chegaram à nave-mãe. Avisa o andamento por eventos;
    /// quem mostra (HUD) e quem decide vitória (GameManager) só escutam.
    /// </summary>
    public class WaveSpawner : MonoBehaviour
    {
        [Tooltip("Pontes por onde os inimigos saem, na ordem. A dificuldade diz quantas delas usar (Bridge Count); com mais de uma, os inimigos se alternam.")]
        [SerializeField] private LightBridge[] bridges;

        [Tooltip("Objetos que só existem com a segunda ponte (o portal dela): ficam desligados quando a dificuldade usa uma ponte só.")]
        [SerializeField] private GameObject[] secondBridgeObjects;

        [Tooltip("Quem dá créditos e pontos quando um inimigo morre.")]
        [SerializeField] private GameSession session;

        private DifficultyData difficulty;
        private int aliveInWave;
        private int nextBridge;
        private int activeBridges = 1;

        /// <summary>Começou a contagem antes de uma onda: (número da onda, a partir de 1; segundos).</summary>
        public event Action<int, float> WaveCountdownStarted;

        /// <summary>Uma onda começou a soltar inimigos: (número da onda, a partir de 1; total de ondas).</summary>
        public event Action<int, int> WaveStarted;

        /// <summary>Todos os inimigos de uma onda saíram de cena: (número da onda, a partir de 1).</summary>
        public event Action<int> WaveCleared;

        /// <summary>A última onda foi eliminada.</summary>
        public event Action AllWavesCleared;

        public int CurrentWave { get; private set; }
        public int TotalWaves => difficulty != null ? difficulty.Waves.Count : 0;
        public bool Finished { get; private set; }

        /// <summary>
        /// Começa as ondas da dificuldade escolhida. Chamado pelo GameManager no início
        /// da partida (é ele quem sabe a dificuldade).
        /// </summary>
        public void Begin(DifficultyData chosen)
        {
            if (chosen == null || bridges == null || bridges.Length == 0 || session == null)
            {
                Debug.LogError($"{name}: falta a dificuldade, ou Bridges e Session no Inspector.", this);
                return;
            }

            difficulty = chosen;
            UseBridges(chosen.BridgeCount);
            StartCoroutine(RunWaves());
        }

        // Liga só as pontes que a dificuldade usa: as outras e o portal delas somem da cena.
        private void UseBridges(int wanted)
        {
            activeBridges = Mathf.Clamp(wanted, 1, bridges.Length);

            for (int i = 0; i < bridges.Length; i++)
                bridges[i].gameObject.SetActive(i < activeBridges);

            if (secondBridgeObjects == null)
                return;

            foreach (GameObject extra in secondBridgeObjects)
            {
                if (extra != null)
                    extra.SetActive(activeBridges >= 2);
            }
        }

        /// <summary>Para de soltar inimigos (fim de partida).</summary>
        public void Stop()
        {
            StopAllCoroutines();
        }

        // Corrotina: uma função que pode pausar (yield) e continuar no frame seguinte ou
        // depois de um tempo, sem travar o jogo. WaitForSeconds respeita o Time.timeScale,
        // então a pausa do jogo também pausa as ondas.
        private IEnumerator RunWaves()
        {
            for (int i = 0; i < difficulty.Waves.Count; i++)
            {
                CurrentWave = i + 1;

                WaveCountdownStarted?.Invoke(CurrentWave, difficulty.TimeBetweenWaves);
                yield return new WaitForSeconds(difficulty.TimeBetweenWaves);

                WaveStarted?.Invoke(CurrentWave, TotalWaves);
                yield return SpawnWave(difficulty.Waves[i]);

                // Espera a onda ser eliminada antes de seguir.
                yield return new WaitUntil(() => aliveInWave == 0);
                WaveCleared?.Invoke(CurrentWave);
            }

            Finished = true;
            AllWavesCleared?.Invoke();
        }

        private IEnumerator SpawnWave(Wave wave)
        {
            foreach (WaveGroup group in wave.groups)
            {
                if (group.enemyPrefab == null)
                {
                    Debug.LogWarning($"{name}: um grupo da onda {CurrentWave} está sem prefab.", this);
                    continue;
                }

                for (int n = 0; n < group.count; n++)
                {
                    Spawn(group.enemyPrefab);
                    yield return new WaitForSeconds(difficulty.SpawnInterval);
                }
            }
        }

        private void Spawn(Enemy prefab)
        {
            LightBridge bridge = bridges[nextBridge];
            nextBridge = (nextBridge + 1) % activeBridges;

            Enemy enemy = Instantiate(prefab, bridge.GetWaypoint(0), Quaternion.identity);
            enemy.Initialize(bridge, difficulty.HealthMultiplier, difficulty.SpeedMultiplier);

            session.Register(enemy);
            AudioManager.Play(Sfx.Spawn);
            enemy.Died += OnEnemyGone;
            enemy.ReachedBase += OnEnemyGone;
            aliveInWave++;
        }

        // Morreu ou chegou à base: para a onda, os dois casos contam como "saiu de cena".
        private void OnEnemyGone(Enemy enemy)
        {
            enemy.Died -= OnEnemyGone;
            enemy.ReachedBase -= OnEnemyGone;
            aliveInWave--;
        }
    }
}
