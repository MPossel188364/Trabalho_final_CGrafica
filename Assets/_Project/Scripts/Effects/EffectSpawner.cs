using UnityEngine;

namespace NebulaGuard
{
    /// <summary>
    /// Toca um efeito de partículas (prefab) num ponto do mundo. Usado pela morte do inimigo
    /// (DeathEffect) e pelo impacto do projétil (Projectile).
    /// O prefab cuida de sumir sozinho: o Particle System dele tem Stop Action = Destroy,
    /// então o objeto é apagado quando a última partícula morre.
    /// </summary>
    public static class EffectSpawner
    {
        /// <param name="size">Escala do efeito: 1 = tamanho do prefab. Funciona porque o
        /// Particle System do prefab usa Scaling Mode = Hierarchy (segue a escala do objeto).</param>
        public static void Play(ParticleSystem prefab, Vector3 position, float size = 1f)
        {
            if (prefab == null)
                return;

            ParticleSystem effect = Object.Instantiate(prefab, position, Quaternion.identity);
            effect.transform.localScale = Vector3.one * size;
        }
    }
}
