using UnityEngine;

namespace NebulaGuard
{
    /// <summary>
    /// Explosão quando o inimigo some: destruído pelas naves (Died) ou batendo na nave-mãe
    /// (ReachedBase). Fica no prefab do inimigo, ao lado do Enemy, e só escuta os eventos
    /// dele: o Enemy continua sem saber que existe efeito. Na batida a explosão é um pouco
    /// maior; a tremida da câmera, que vem junto, é do CameraShake.
    /// </summary>
    [RequireComponent(typeof(Enemy))]
    public class DeathEffect : MonoBehaviour
    {
        [Tooltip("Prefab da explosão (Prefabs/Effects/Explosao).")]
        [SerializeField] private ParticleSystem explosionPrefab;

        [Tooltip("Tamanho da explosão: 1 = a do prefab. Inimigo maior, explosão maior.")]
        [Min(0.1f)] [SerializeField] private float size = 1f;

        [Tooltip("Multiplica o tamanho quando o inimigo bate na nave-mãe (0 = não explode na batida).")]
        [Min(0f)] [SerializeField] private float impactSizeMultiplier = 1.3f;

        private Enemy enemy;

        private void Awake()
        {
            enemy = GetComponent<Enemy>();
        }

        private void OnEnable()
        {
            enemy.Died += OnDied;
            enemy.ReachedBase += OnReachedBase;
        }

        private void OnDisable()
        {
            enemy.Died -= OnDied;
            enemy.ReachedBase -= OnReachedBase;
        }

        private void OnDied(Enemy dead)
        {
            EffectSpawner.Play(explosionPrefab, dead.transform.position, size);
            AudioManager.Play(Sfx.Explosion);
        }

        private void OnReachedBase(Enemy crashed)
        {
            if (impactSizeMultiplier > 0f)
                EffectSpawner.Play(explosionPrefab, crashed.transform.position, size * impactSizeMultiplier);
        }
    }
}
