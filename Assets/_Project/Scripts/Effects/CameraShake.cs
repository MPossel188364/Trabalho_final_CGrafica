using UnityEngine;

namespace NebulaGuard
{
    /// <summary>
    /// Tremida da câmera quando a nave-mãe leva dano, e uma tremida forte quando ela é destruída.
    /// Fica na Main Camera e só escuta os eventos da MotherShip.
    ///
    /// Como funciona ("trauma"): cada pancada soma um valor entre 0 e 1 ao trauma, que vai
    /// baixando sozinho com o tempo. O deslocamento da câmera é proporcional ao trauma ao
    /// quadrado: pancada pequena mal mexe, pancada grande sacode bem, e o fim é suave.
    /// A direção vem do ruído de Perlin (um "aleatório" contínuo), então a câmera balança
    /// em vez de pular de um ponto a outro a cada frame.
    /// </summary>
    public class CameraShake : MonoBehaviour
    {
        [SerializeField] private MotherShip motherShip;

        [Tooltip("Trauma de qualquer pancada, por menor que seja.")]
        [Range(0f, 1f)] [SerializeField] private float traumaPerHit = 0.2f;

        [Tooltip("Trauma a mais por ponto de vida perdido (o Drone tira 1; o Caça alienígena e o Couraçado, 5).")]
        [Range(0f, 1f)] [SerializeField] private float traumaPerDamage = 0.1f;

        [Tooltip("Trauma quando a nave-mãe é destruída (1 = o máximo).")]
        [Range(0f, 1f)] [SerializeField] private float traumaOnDestroyed = 1f;

        [Tooltip("Deslocamento máximo, em unidades, com trauma 1.")]
        [Min(0f)] [SerializeField] private float maxOffset = 0.5f;

        [Tooltip("Rotação máxima, em graus, com trauma 1.")]
        [Min(0f)] [SerializeField] private float maxAngle = 2f;

        [Tooltip("Quanto trauma some por segundo. 1,5 = uma tremida cheia dura ~0,7 s.")]
        [Min(0.1f)] [SerializeField] private float recoveryPerSecond = 1.5f;

        [Tooltip("Velocidade do balanço (quantas \"voltas\" do ruído por segundo).")]
        [Min(0.1f)] [SerializeField] private float frequency = 25f;

        private Vector3 restPosition;
        private Quaternion restRotation;
        private float trauma;
        private int lastHealth = -1;
        private float noiseTime;

        private void Awake()
        {
            restPosition = transform.localPosition;
            restRotation = transform.localRotation;
        }

        private void OnEnable()
        {
            if (motherShip == null)
            {
                Debug.LogWarning($"{name}: CameraShake sem a MotherShip no Inspector; a câmera não vai tremer.", this);
                return;
            }

            motherShip.HealthChanged += OnHealthChanged;
            motherShip.Destroyed += OnDestroyed;
        }

        private void OnDisable()
        {
            if (motherShip == null)
                return;

            motherShip.HealthChanged -= OnHealthChanged;
            motherShip.Destroyed -= OnDestroyed;
        }

        /// <summary>Soma trauma (0 a 1). Público para outros efeitos poderem sacudir a tela.</summary>
        public void AddTrauma(float amount)
        {
            trauma = Mathf.Clamp01(trauma + amount);
        }

        private void OnHealthChanged(int current, int max)
        {
            // O evento também é disparado quando a vida enche (início da partida):
            // só treme quando a vida diminuiu.
            if (lastHealth >= 0 && current < lastHealth)
                AddTrauma(traumaPerHit + (lastHealth - current) * traumaPerDamage);

            lastHealth = current;
        }

        private void OnDestroyed()
        {
            AddTrauma(traumaOnDestroyed);
        }

        // LateUpdate: roda depois de todos os Update, então nada move a câmera depois da tremida.
        private void LateUpdate()
        {
            // Tempo sem escala: na derrota o jogo para (timeScale = 0), mas a tremida final
            // ainda precisa acontecer.
            float dt = Time.unscaledDeltaTime;

            if (trauma <= 0f)
            {
                transform.localPosition = restPosition;
                transform.localRotation = restRotation;
                return;
            }

            noiseTime += dt * frequency;
            float shake = trauma * trauma;

            // PerlinNoise devolve 0..1; "* 2 - 1" passa para -1..1. Cada eixo lê uma linha
            // diferente do ruído (0, 10, 20) para não andarem juntos.
            float x = (Mathf.PerlinNoise(noiseTime, 0f) * 2f - 1f) * maxOffset * shake;
            float y = (Mathf.PerlinNoise(noiseTime, 10f) * 2f - 1f) * maxOffset * shake;
            float angle = (Mathf.PerlinNoise(noiseTime, 20f) * 2f - 1f) * maxAngle * shake;

            transform.localPosition = restPosition + new Vector3(x, y, 0f);
            transform.localRotation = restRotation * Quaternion.Euler(0f, 0f, angle);

            trauma = Mathf.Max(0f, trauma - recoveryPerSecond * dt);
        }
    }
}
