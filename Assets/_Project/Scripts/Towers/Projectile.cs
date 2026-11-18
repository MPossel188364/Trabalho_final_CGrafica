using System.Collections.Generic;
using UnityEngine;

namespace NebulaGuard
{
    /// <summary>
    /// Projétil disparado por uma nave-torre. Persegue o alvo e, ao tocar um inimigo
    /// (trigger), tira vida dele e some. Se o alvo morrer no caminho, segue reto na
    /// última direção e some sozinho depois de um tempo. Se a nave definiu um raio de
    /// explosão (Bombardeiro), os outros inimigos dentro do raio também levam o dano.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class Projectile : MonoBehaviour
    {
        [Tooltip("Segundos até o projétil sumir se não acertar nada.")]
        [Min(0.1f)] [SerializeField] private float lifetime = 2f;

        [Tooltip("Mesmo ajuste do Enemy: sprites que apontam para cima precisam de -90.")]
        [SerializeField] private float spriteAngleOffset = -90f;

        [Header("Efeito ao acertar (opcional)")]
        [Tooltip("Prefab tocado no ponto do impacto (ex.: Prefabs/Effects/Explosao no míssil). Vazio = sem efeito.")]
        [SerializeField] private ParticleSystem impactEffectPrefab;

        [Tooltip("Tamanho do efeito para tiros sem área. Com raio de explosão, o tamanho é o próprio raio: o jogador vê a área que levou dano.")]
        [Min(0.1f)] [SerializeField] private float impactEffectSize = 0.4f;

        [Tooltip("Rastro (ex.: fumaça do míssil): Particle System filho, em espaço World. Vazio = sem rastro.")]
        [SerializeField] private ParticleSystem trail;

        private Enemy target;
        private float damage;
        private float speed;
        private float splashRadius;
        private Vector2 direction;
        private bool hit;
        private float age;

        /// <summary>
        /// A nave chama logo depois de instanciar o projétil no cano. O raio da explosão é
        /// opcional: 0 (o padrão) = só o inimigo atingido leva dano, como sempre foi.
        /// </summary>
        public void Launch(Enemy newTarget, float newDamage, float newSpeed, float newSplashRadius = 0f)
        {
            target = newTarget;
            damage = newDamage;
            speed = newSpeed;
            splashRadius = newSplashRadius;
            direction = transform.up;
        }

        private void Update()
        {
            // Some sozinho se não acertar nada. Contado aqui (e não com Destroy(obj, tempo))
            // para passar pelo Despawn, que solta o rastro antes de apagar o projétil.
            age += Time.deltaTime;
            if (age >= lifetime)
            {
                Despawn();
                return;
            }

            // Enquanto o alvo vive, a direção é atualizada a cada frame (persegue).
            // "target != null" também é falso quando o objeto já foi destruído pelo Unity.
            if (target != null && target.IsAlive)
            {
                Vector2 toTarget = (Vector2)target.transform.position - (Vector2)transform.position;
                if (toTarget.sqrMagnitude > 0.0001f)
                    direction = toTarget.normalized;
            }

            transform.position += (Vector3)(direction * speed * Time.deltaTime);

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + spriteAngleOffset;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Mesmo critério da MotherShip: reconhece o inimigo pelo componente.
            // Acerta qualquer inimigo que tocar, não só o alvo (como um tiro de verdade).
            if (hit || !other.TryGetComponent(out Enemy enemy) || !enemy.IsAlive)
                return;

            hit = true;
            enemy.TakeDamage(damage);

            if (splashRadius > 0f)
                DamageNearby(enemy);

            float effectSize = splashRadius > 0f ? splashRadius : impactEffectSize;
            EffectSpawner.Play(impactEffectPrefab, transform.position, effectSize);
            AudioManager.Play(splashRadius > 0f ? Sfx.BigExplosion : Sfx.Impact);

            Despawn();
        }

        /// <summary>
        /// Apaga o projétil. O rastro é solto antes (vira objeto independente) e para de
        /// emitir: a fumaça que já saiu se desfaz no tempo dela, em vez de sumir de uma vez
        /// junto com o míssil. O Stop Action = Destroy do rastro apaga-o no fim.
        /// </summary>
        private void Despawn()
        {
            if (trail != null)
            {
                trail.transform.SetParent(null, true);
                trail.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }

            Destroy(gameObject);
        }

        /// <summary>
        /// Explosão: os outros inimigos vivos dentro do raio levam o mesmo dano. O inimigo
        /// que o projétil tocou já levou o dano acima, então é pulado (não leva duas vezes).
        /// </summary>
        private void DamageNearby(Enemy alreadyHit)
        {
            // Mesma busca da TowerShip: só a layer Enemy, e os inimigos usam collider trigger.
            var filter = new ContactFilter2D();
            filter.SetLayerMask(LayerMask.GetMask("Enemy"));
            filter.useTriggers = true;

            var nearby = new List<Collider2D>();
            Physics2D.OverlapCircle(transform.position, splashRadius, filter, nearby);

            foreach (Collider2D col in nearby)
            {
                if (col.TryGetComponent(out Enemy neighbour) && neighbour != alreadyHit && neighbour.IsAlive)
                    neighbour.TakeDamage(damage);
            }
        }

        // Kinematic: movido por código, mas os triggers funcionam (igual ao Enemy).
        private void Reset()
        {
            if (TryGetComponent(out Rigidbody2D body))
                body.bodyType = RigidbodyType2D.Kinematic;
        }
    }
}
