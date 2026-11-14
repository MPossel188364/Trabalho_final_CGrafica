using System;
using System.Collections.Generic;
using UnityEngine;

namespace NebulaGuard
{
    /// <summary>Um bloco de inimigos iguais dentro de uma onda (ex.: 6 drones).</summary>
    [Serializable]
    public class WaveGroup
    {
        [Tooltip("Prefab do inimigo (precisa ter o componente Enemy).")]
        public Enemy enemyPrefab;

        [Min(1)] public int count = 1;
    }

    /// <summary>Uma onda: os grupos saem em ordem, um inimigo de cada vez.</summary>
    [Serializable]
    public class Wave
    {
        public List<WaveGroup> groups = new List<WaveGroup>();
    }

    /// <summary>
    /// Dados de uma dificuldade (Fácil, Difícil): as ondas e o ritmo delas, e os
    /// multiplicadores aplicados aos inimigos, e os números da partida (créditos iniciais,
    /// vida da nave-mãe, tempo máximo). Um asset por dificuldade.
    /// O multiplicador de pontos do plano entra aqui junto com o ranking.
    /// </summary>
    [CreateAssetMenu(fileName = "DifficultyData", menuName = "NebulaGuard/Difficulty Data")]
    public class DifficultyData : ScriptableObject
    {
        [SerializeField] private string displayName = "Fácil";

        [Tooltip("Créditos com que o jogador começa.")]
        [Min(0)] [SerializeField] private int startingCredits = 100;

        [Tooltip("Vida da nave-mãe.")]
        [Min(1)] [SerializeField] private int motherShipHealth = 20;

        [Tooltip("Tempo máximo da partida, em segundos (conta a partir da primeira onda). Zerou com ondas por vir = derrota.")]
        [Min(10f)] [SerializeField] private float timeLimit = 240f;

        [Tooltip("Segundos entre um inimigo e o próximo dentro da onda.")]
        [Min(0.05f)] [SerializeField] private float spawnInterval = 1.2f;

        [Tooltip("Segundos de preparação antes de cada onda (inclusive a primeira).")]
        [Min(0f)] [SerializeField] private float timeBetweenWaves = 10f;

        [Tooltip("Multiplica a vida de todos os inimigos.")]
        [Min(0.1f)] [SerializeField] private float healthMultiplier = 1f;

        [Tooltip("Multiplica a velocidade de todos os inimigos.")]
        [Min(0.1f)] [SerializeField] private float speedMultiplier = 1f;

        [SerializeField] private List<Wave> waves = new List<Wave>();

        public string DisplayName => displayName;
        public int StartingCredits => startingCredits;
        public int MotherShipHealth => motherShipHealth;
        public float TimeLimit => timeLimit;
        public float SpawnInterval => spawnInterval;
        public float TimeBetweenWaves => timeBetweenWaves;
        public float HealthMultiplier => healthMultiplier;
        public float SpeedMultiplier => speedMultiplier;
        public IReadOnlyList<Wave> Waves => waves;
    }
}
