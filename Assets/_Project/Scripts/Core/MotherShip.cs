using System;
using UnityEngine;

namespace NebulaGuard
{
    /// <summary>
    /// Nave-mãe, a base que o jogador defende. Tem vida. Quando um inimigo toca o
    /// trigger dela, perde vida igual ao dano do inimigo e o inimigo some sem dar pontos.
    /// Quem mostra a vida (HUD) e quem decide a derrota (GameManager) só escutam os eventos.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class MotherShip : MonoBehaviour
    {
        [Tooltip("Usado só se não houver GameManager na cena; com ele, a vida vem do DifficultyData (20 no Fácil, 15 no Difícil).")]
        [Min(1)] [SerializeField] private int maxHealth = 20;

        private int currentHealth;

        public int MaxHealth => maxHealth;
        public int CurrentHealth => currentHealth;
        public bool IsDestroyed => currentHealth <= 0;

        /// <summary>Avisa a interface quando a vida muda: (vida atual, vida máxima).</summary>
        public event Action<int, int> HealthChanged;

        /// <summary>Disparado uma vez, quando a vida chega a 0 (derrota).</summary>
        public event Action Destroyed;

        private void Awake()
        {
            currentHealth = maxHealth;
        }

        /// <summary>Define a vida máxima e enche a vida. Chamado pelo GameManager no início da partida.</summary>
        public void Begin(int health)
        {
            maxHealth = Mathf.Max(1, health);
            currentHealth = maxHealth;
            HealthChanged?.Invoke(currentHealth, maxHealth);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Reconhece o inimigo pelo componente, não pela tag: errar o nome de uma tag
            // não dá erro nenhum, o jogo só deixa de funcionar.
            if (!other.TryGetComponent(out Enemy enemy) || !enemy.IsAlive)
                return;

            int damage = enemy.Data.BaseDamage;
            enemy.ConsumeAtBase();
            TakeDamage(damage);
        }

        public void TakeDamage(int amount)
        {
            if (IsDestroyed || amount <= 0)
                return;

            currentHealth = Mathf.Max(0, currentHealth - amount);
            HealthChanged?.Invoke(currentHealth, maxHealth);

            if (IsDestroyed)
                Destroyed?.Invoke();
        }
    }
}
