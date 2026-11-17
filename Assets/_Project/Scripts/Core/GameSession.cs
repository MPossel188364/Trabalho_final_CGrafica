using System;
using UnityEngine;

namespace NebulaGuard
{
    /// <summary>
    /// Estado de uma partida: créditos (para construir) e pontos (para o ranking).
    /// Escuta os eventos dos inimigos e quem mostra os valores (HUD, menu de construção)
    /// escuta os eventos daqui. Não conhece nenhuma interface.
    /// </summary>
    public class GameSession : MonoBehaviour
    {
        [Tooltip("Usado só se não houver GameManager na cena; com ele, os créditos iniciais vêm do DifficultyData.")]
        [Min(0)] [SerializeField] private int startingCredits = 100;

        private int credits;
        private int score;
        private float scoreMultiplier = 1f;

        public int Credits => credits;
        public int Score => score;

        /// <summary>Avisa a interface quando os créditos mudam (valor novo).</summary>
        public event Action<int> CreditsChanged;

        /// <summary>Avisa a interface quando os pontos mudam (valor novo).</summary>
        public event Action<int> ScoreChanged;

        private void Awake()
        {
            credits = startingCredits;
            score = 0;
        }

        /// <summary>
        /// Recomeça a partida com estes créditos e zero pontos. Chamado pelo GameManager.
        /// O multiplicador (da dificuldade) vale para todos os pontos ganhos depois.
        /// </summary>
        public void Begin(int initialCredits, float newScoreMultiplier = 1f)
        {
            credits = Mathf.Max(0, initialCredits);
            score = 0;
            scoreMultiplier = newScoreMultiplier;
            CreditsChanged?.Invoke(credits);
            ScoreChanged?.Invoke(score);
        }

        /// <summary>
        /// Passa a escutar um inimigo: se ele for destruído, o jogador ganha créditos e
        /// pontos; se chegar à nave-mãe, não ganha nada. O WaveSpawner chama ao gerar cada um.
        /// </summary>
        public void Register(Enemy enemy)
        {
            enemy.Died += OnEnemyDied;
            enemy.ReachedBase += OnEnemyReachedBase;
        }

        /// <summary>Gasta créditos se houver saldo. Devolve se conseguiu (false = saldo insuficiente).</summary>
        public bool TrySpend(int amount)
        {
            if (amount < 0 || amount > credits)
                return false;

            credits -= amount;
            CreditsChanged?.Invoke(credits);
            return true;
        }

        public void AddCredits(int amount)
        {
            if (amount <= 0)
                return;

            credits += amount;
            CreditsChanged?.Invoke(credits);
        }

        public void AddScore(int amount)
        {
            if (amount <= 0)
                return;

            // O multiplicador entra aqui, e não no ranking: assim o HUD, a tela de fim e o
            // ranking mostram sempre o mesmo número.
            score += Mathf.RoundToInt(amount * scoreMultiplier);
            ScoreChanged?.Invoke(score);
        }

        private void OnEnemyDied(Enemy enemy)
        {
            Unsubscribe(enemy);
            AddCredits(enemy.Data.CreditsReward);
            AddScore(enemy.Data.ScoreReward);
        }

        private void OnEnemyReachedBase(Enemy enemy)
        {
            Unsubscribe(enemy);
        }

        private void Unsubscribe(Enemy enemy)
        {
            enemy.Died -= OnEnemyDied;
            enemy.ReachedBase -= OnEnemyReachedBase;
        }
    }
}
