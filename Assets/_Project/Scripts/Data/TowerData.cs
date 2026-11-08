using UnityEngine;

namespace NebulaGuard
{
    /// <summary>Como a nave escolhe o alvo entre os inimigos dentro do alcance.</summary>
    public enum TargetingMode
    {
        /// <summary>O que mais andou na ponte, ou seja, o mais perto da nave-mãe (Caça).</summary>
        First,

        /// <summary>O que tem mais vida no momento (Cruzador).</summary>
        Strongest,
    }

    /// <summary>
    /// Dados de balanceamento de uma nave-torre (Caça, Bombardeiro, Cruzador).
    /// Mesmo esquema do EnemyData: um asset por nave, editado no Inspector.
    /// Os valores padrão abaixo são os do Caça, do documento de design.
    /// </summary>
    [CreateAssetMenu(fileName = "TowerData", menuName = "NebulaGuard/Tower Data")]
    public class TowerData : ScriptableObject
    {
        [SerializeField] private string displayName = "Caça";

        [Tooltip("Créditos para posicionar a nave.")]
        [Min(0)] [SerializeField] private int cost = 50;

        [Tooltip("Raio, em unidades, dentro do qual a nave enxerga inimigos.")]
        [Min(0.1f)] [SerializeField] private float range = 3.5f;

        [Tooltip("Disparos por segundo.")]
        [Min(0.01f)] [SerializeField] private float fireRate = 3f;

        [Tooltip("Dano de cada projétil.")]
        [Min(0f)] [SerializeField] private float damage = 5f;

        [Tooltip("Velocidade do projétil, em unidades por segundo.")]
        [Min(0.1f)] [SerializeField] private float projectileSpeed = 14f;

        [Tooltip("Quantos graus por segundo a nave gira para mirar.")]
        [Min(1f)] [SerializeField] private float turnSpeed = 540f;

        [SerializeField] private TargetingMode targeting = TargetingMode.First;

        [Tooltip("Prefab do projétil (precisa ter o componente Projectile).")]
        [SerializeField] private Projectile projectilePrefab;

        public string DisplayName => displayName;
        public int Cost => cost;
        public float Range => range;
        public float FireRate => fireRate;
        public float Damage => damage;
        public float ProjectileSpeed => projectileSpeed;
        public float TurnSpeed => turnSpeed;
        public TargetingMode Targeting => targeting;
        public Projectile ProjectilePrefab => projectilePrefab;
    }
}
