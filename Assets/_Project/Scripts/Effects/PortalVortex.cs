using System;
using UnityEngine;

namespace NebulaGuard
{
    /// <summary>
    /// Anima o portal: cada camada (filho com a imagem do redemoinho) gira na sua velocidade
    /// e todas pulsam de leve, como se o portal "respirasse". É só visual: quem gera os
    /// inimigos continua sendo o WaveSpawner, no início da ponte de luz.
    ///
    /// Duas camadas iguais girando em velocidades diferentes (a de dentro mais rápida) dão a
    /// impressão de profundidade, o mesmo truque do parallax do fundo.
    /// </summary>
    public class PortalVortex : MonoBehaviour
    {
        [Serializable]
        private struct SpinLayer
        {
            public Transform layer;

            [Tooltip("Graus por segundo. Positivo = anti-horário: os braços parecem sair do centro (o portal \"cospe\" os inimigos).")]
            public float degreesPerSecond;
        }

        [SerializeField] private SpinLayer[] layers = Array.Empty<SpinLayer>();

        [Tooltip("Quanto as camadas crescem e encolhem na pulsação (0,06 = 6%).")]
        [Range(0f, 0.3f)] [SerializeField] private float pulseAmount = 0.06f;

        [Tooltip("Pulsações por segundo.")]
        [Min(0f)] [SerializeField] private float pulsesPerSecond = 0.8f;

        private Vector3[] baseScales;

        private void Awake()
        {
            // Guarda a escala original: a pulsação é sempre em volta dela, sem acumular erro.
            baseScales = new Vector3[layers.Length];
            for (int i = 0; i < layers.Length; i++)
                baseScales[i] = layers[i].layer != null ? layers[i].layer.localScale : Vector3.one;
        }

        private void Update()
        {
            // Seno vai de -1 a 1: a escala oscila entre (1 - pulso) e (1 + pulso).
            float pulse = 1f + Mathf.Sin(Time.time * pulsesPerSecond * 2f * Mathf.PI) * pulseAmount;

            for (int i = 0; i < layers.Length; i++)
            {
                Transform layer = layers[i].layer;
                if (layer == null)
                    continue;

                layer.Rotate(0f, 0f, layers[i].degreesPerSecond * Time.deltaTime);
                layer.localScale = baseScales[i] * pulse;
            }
        }
    }
}
