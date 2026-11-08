using System.Collections.Generic;
using UnityEngine;

namespace NebulaGuard
{
    /// <summary>
    /// Nave-torre: procura inimigos dentro do alcance, escolhe um alvo, gira até ele e
    /// atira quando o tempo de recarga acaba. Os números vêm do TowerData; quem causa o
    /// dano é o Projectile.
    /// </summary>
    public class TowerShip : MonoBehaviour
    {
        [SerializeField] private TowerData data;

        [Tooltip("Ponto de onde o projétil sai (objeto filho na ponta da nave).")]
        [SerializeField] private Transform muzzle;

        [Tooltip("Layers onde a nave procura alvos. Deve ser só a layer Enemy.")]
        [SerializeField] private LayerMask enemyLayers;

        [Tooltip("Só atira quando a mira está a menos destes graus do alvo.")]
        [Range(1f, 90f)] [SerializeField] private float aimTolerance = 10f;

        [Tooltip("Mesmo ajuste do Enemy: sprites que apontam para cima precisam de -90.")]
        [SerializeField] private float spriteAngleOffset = -90f;

        // Reaproveitada a cada busca para não criar uma lista nova por frame.
        private readonly List<Collider2D> hits = new List<Collider2D>();
        private ContactFilter2D filter;
        private float cooldown;
        private Enemy target;

        public TowerData Data => data;
        public Enemy Target => target;

        private void Awake()
        {
            filter = new ContactFilter2D();
            filter.SetLayerMask(enemyLayers);
            filter.useTriggers = true; // os inimigos usam collider trigger
        }

        private void Update()
        {
            if (data == null)
                return;

            cooldown -= Time.deltaTime;
            target = FindTarget();
            if (target == null)
                return;

            bool aimed = AimAt(target.transform.position);
            if (aimed && cooldown <= 0f)
            {
                Fire();
                cooldown = 1f / data.FireRate;
            }
        }

        /// <summary>Lista os inimigos vivos dentro do alcance e escolhe um pela estratégia da nave.</summary>
        private Enemy FindTarget()
        {
            Physics2D.OverlapCircle(transform.position, data.Range, filter, hits);

            Enemy best = null;
            foreach (Collider2D col in hits)
            {
                if (!col.TryGetComponent(out Enemy enemy) || !enemy.IsAlive)
                    continue;

                if (best == null || IsBetterTarget(enemy, best))
                    best = enemy;
            }
            return best;
        }

        private bool IsBetterTarget(Enemy candidate, Enemy current)
        {
            switch (data.Targeting)
            {
                case TargetingMode.Strongest:
                    return candidate.CurrentHealth > current.CurrentHealth;
                case TargetingMode.First:
                default:
                    return candidate.DistanceTravelled > current.DistanceTravelled;
            }
        }

        /// <summary>Gira em direção ao ponto, limitado pela velocidade de giro. Devolve se já está mirando.</summary>
        private bool AimAt(Vector2 point)
        {
            Vector2 direction = point - (Vector2)transform.position;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + spriteAngleOffset;
            Quaternion wanted = Quaternion.Euler(0f, 0f, angle);

            transform.rotation = Quaternion.RotateTowards(transform.rotation, wanted, data.TurnSpeed * Time.deltaTime);
            return Quaternion.Angle(transform.rotation, wanted) <= aimTolerance;
        }

        private void Fire()
        {
            if (data.ProjectilePrefab == null)
            {
                Debug.LogWarning($"{name}: o TowerData {data.name} não tem Projectile Prefab.", this);
                return;
            }

            Transform origin = muzzle != null ? muzzle : transform;
            Projectile shot = Instantiate(data.ProjectilePrefab, origin.position, transform.rotation);
            shot.Launch(target, data.Damage, data.ProjectileSpeed);
        }

        // Chamado ao adicionar o componente: já marca a layer Enemy.
        private void Reset()
        {
            enemyLayers = LayerMask.GetMask("Enemy");
        }

        // Círculo do alcance na Scene view, com a nave selecionada.
        private void OnDrawGizmosSelected()
        {
            if (data == null)
                return;

            Gizmos.color = new Color(0.25f, 1f, 0.4f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, data.Range);
        }
    }
}
