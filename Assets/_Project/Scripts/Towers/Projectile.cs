using UnityEngine;

namespace NebulaGuard
{
    /// <summary>
    /// Projétil disparado por uma nave-torre. Persegue o alvo e, ao tocar um inimigo
    /// (trigger), tira vida dele e some. Se o alvo morrer no caminho, segue reto na
    /// última direção e some sozinho depois de um tempo.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class Projectile : MonoBehaviour
    {
        [Tooltip("Segundos até o projétil sumir se não acertar nada.")]
        [Min(0.1f)] [SerializeField] private float lifetime = 2f;

        [Tooltip("Mesmo ajuste do Enemy: sprites que apontam para cima precisam de -90.")]
        [SerializeField] private float spriteAngleOffset = -90f;

        private Enemy target;
        private float damage;
        private float speed;
        private Vector2 direction;
        private bool hit;

        /// <summary>A nave chama logo depois de instanciar o projétil no cano.</summary>
        public void Launch(Enemy newTarget, float newDamage, float newSpeed)
        {
            target = newTarget;
            damage = newDamage;
            speed = newSpeed;
            direction = transform.up;
            Destroy(gameObject, lifetime);
        }

        private void Update()
        {
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
            Destroy(gameObject);
        }

        // Kinematic: movido por código, mas os triggers funcionam (igual ao Enemy).
        private void Reset()
        {
            if (TryGetComponent(out Rigidbody2D body))
                body.bodyType = RigidbodyType2D.Kinematic;
        }
    }
}
