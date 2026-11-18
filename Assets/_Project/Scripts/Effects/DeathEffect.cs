using UnityEngine;

namespace NebulaGuard
{
    /// <summary>
    /// Explosão quando o inimigo é destruído. Fica no prefab do inimigo, ao lado do Enemy,
    /// e só escuta o evento Died dele: o Enemy continua sem saber que existe efeito.
    /// Chegar à nave-mãe (ReachedBase) não explode aqui; quem reage a isso é a tremida
    /// da câmera (CameraShake).
    /// </summary>
    [RequireComponent(typeof(Enemy))]
    public class DeathEffect : MonoBehaviour
    {
        [Tooltip("Prefab da explosão (Prefabs/Effects/Explosao).")]
        [SerializeField] private ParticleSystem explosionPrefab;

        [Tooltip("Tamanho da explosão: 1 = a do prefab. Inimigo maior, explosão maior.")]
        [Min(0.1f)] [SerializeField] private float size = 1f;

        private Enemy enemy;

        private void Awake()
        {
            enemy = GetComponent<Enemy>();
        }

        private void OnEnable()
        {
            enemy.Died += OnDied;
        }

        private void OnDisable()
        {
            enemy.Died -= OnDied;
        }

        private void OnDied(Enemy dead)
        {
            EffectSpawner.Play(explosionPrefab, dead.transform.position, size);
        }
    }
}
