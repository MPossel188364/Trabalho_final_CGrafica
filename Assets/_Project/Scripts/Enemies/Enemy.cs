using System;
using UnityEngine;

namespace NebulaGuard
{
    /// <summary>
    /// Inimigo: nasce no início da ponte de luz, anda de waypoint em waypoint até a
    /// nave-mãe e pode levar dano. Os números (vida, velocidade, dano à base, recompensas)
    /// vêm do EnemyData. Quem decide o que acontece quando o inimigo morre (dar créditos
    /// e pontos) é quem escuta os eventos Died e ReachedBase, não o próprio inimigo.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class Enemy : MonoBehaviour
    {
        [SerializeField] private EnemyData data;

        [Tooltip("Ângulo somado à rotação para o sprite apontar para onde anda. Os sprites do Kenney apontam para cima, por isso -90.")]
        [SerializeField] private float spriteAngleOffset = -90f;

        private LightBridge bridge;
        private int nextWaypoint;
        private float speed;
        private float maxHealth;
        private float currentHealth;
        private float distanceTravelled;
        private bool initialized;
        private bool finished;
        private bool warnedEndOfPath;

        /// <summary>Disparado quando a vida chega a 0 (o jogador destruiu o inimigo).</summary>
        public event Action<Enemy> Died;

        /// <summary>Disparado quando o inimigo alcança a nave-mãe (some sem dar pontos).</summary>
        public event Action<Enemy> ReachedBase;

        public EnemyData Data => data;
        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;

        /// <summary>Distância já percorrida na ponte. As torres usam para achar o "primeiro da fila".</summary>
        public float DistanceTravelled => distanceTravelled;

        /// <summary>Verdadeiro entre o Initialize e a morte (ou a chegada à nave-mãe).</summary>
        public bool IsAlive => initialized && !finished;

        /// <summary>
        /// Coloca o inimigo no início da ponte e liga os multiplicadores da dificuldade.
        /// O WaveSpawner chama logo depois de instanciar o prefab.
        /// </summary>
        public void Initialize(LightBridge path, float healthMultiplier = 1f, float speedMultiplier = 1f)
        {
            if (data == null)
            {
                Debug.LogError($"{name}: falta o EnemyData no Inspector.", this);
                return;
            }

            if (path == null || path.WaypointCount == 0)
            {
                Debug.LogError($"{name}: a ponte de luz não existe ou não tem waypoints.", this);
                return;
            }

            bridge = path;
            maxHealth = data.MaxHealth * healthMultiplier;
            currentHealth = maxHealth;
            speed = data.Speed * speedMultiplier;
            distanceTravelled = 0f;

            transform.position = bridge.GetWaypoint(0);
            nextWaypoint = 1;
            initialized = true;
        }

        private void Update()
        {
            if (!IsAlive)
                return;

            // Passou do último waypoint e continua vivo: a nave-mãe deveria tê-lo pego.
            if (nextWaypoint >= bridge.WaypointCount)
            {
                WarnEndOfPath();
                return;
            }

            Vector2 current = transform.position;
            Vector2 target = bridge.GetWaypoint(nextWaypoint);
            Vector2 next = Vector2.MoveTowards(current, target, speed * Time.deltaTime);

            distanceTravelled += Vector2.Distance(current, next);
            transform.position = next;
            FaceDirection(target - current);

            // MoveTowards devolve exatamente o alvo quando chega nele.
            if (next == target)
                nextWaypoint++;
        }

        /// <summary>Tira vida do inimigo. Chamado pelos projéteis. Chegando a 0, ele é destruído.</summary>
        public void TakeDamage(float amount)
        {
            if (!IsAlive || amount <= 0f)
                return;

            currentHealth -= amount;
            if (currentHealth <= 0f)
                Die();
        }

        /// <summary>Chamado pela MotherShip quando o inimigo toca nela: avisa e some.</summary>
        public void ConsumeAtBase()
        {
            if (!IsAlive)
                return;

            finished = true;
            ReachedBase?.Invoke(this);
            Destroy(gameObject);
        }

        private void Die()
        {
            finished = true;
            Died?.Invoke(this);
            Destroy(gameObject);
        }

        private void FaceDirection(Vector2 direction)
        {
            if (direction.sqrMagnitude < 0.0001f)
                return;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + spriteAngleOffset;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        private void WarnEndOfPath()
        {
            if (warnedEndOfPath)
                return;

            warnedEndOfPath = true;
            Debug.LogWarning($"{name} chegou ao fim da ponte sem tocar a nave-mãe. Confira se o trigger da MotherShip cobre o último waypoint.", this);
        }

        // Chamado pelo editor ao adicionar o componente: já deixa a física certa.
        // Kinematic = o movimento é feito por código, a física não empurra o inimigo,
        // mas os triggers continuam funcionando.
        private void Reset()
        {
            if (TryGetComponent(out Rigidbody2D body))
                body.bodyType = RigidbodyType2D.Kinematic;
        }
    }
}
