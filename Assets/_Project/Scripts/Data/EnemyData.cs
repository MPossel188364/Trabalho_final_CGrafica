using UnityEngine;

namespace NebulaGuard
{
    /// <summary>
    /// Dados de balanceamento de um tipo de inimigo (Drone, Caça alienígena, Couraçado).
    /// Os números ficam num asset (ScriptableObject): para balancear, edita-se o asset
    /// no Inspector, sem mexer no código. Um asset por tipo de inimigo.
    /// Os valores padrão abaixo são os do Drone, do documento de design.
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyData", menuName = "NebulaGuard/Enemy Data")]
    public class EnemyData : ScriptableObject
    {
        [SerializeField] private string displayName = "Drone";

        [Tooltip("Vida inicial, antes do multiplicador da dificuldade.")]
        [Min(1f)] [SerializeField] private float maxHealth = 15f;

        [Tooltip("Velocidade em unidades por segundo, antes do multiplicador da dificuldade.")]
        [Min(0.1f)] [SerializeField] private float speed = 2.5f;

        [Tooltip("Quanta vida da nave-mãe o inimigo tira ao chegar nela.")]
        [Min(1)] [SerializeField] private int baseDamage = 1;

        [Tooltip("Créditos que o jogador ganha ao destruir o inimigo.")]
        [Min(0)] [SerializeField] private int creditsReward = 5;

        [Tooltip("Pontos que o jogador ganha ao destruir o inimigo.")]
        [Min(0)] [SerializeField] private int scoreReward = 10;

        public string DisplayName => displayName;
        public float MaxHealth => maxHealth;
        public float Speed => speed;
        public int BaseDamage => baseDamage;
        public int CreditsReward => creditsReward;
        public int ScoreReward => scoreReward;
    }
}
