using System;
using System.Collections.Generic;
using UnityEngine;

namespace NebulaGuard
{
    /// <summary>Uma linha do ranking: quantos pontos, em qual dificuldade e quando.</summary>
    [Serializable]
    public class RankingEntry
    {
        public int score;
        public string difficulty;
        public string date;
    }

    /// <summary>
    /// Ranking local das 5 melhores partidas, guardado em PlayerPrefs (sobrevive ao fechar
    /// o jogo). Não é um componente: é uma classe estática que o GameManager chama no fim da
    /// partida e o menu principal lê para mostrar a lista.
    /// </summary>
    public static class Ranking
    {
        public const int MaxEntries = 5;

        private const string Key = "NebulaGuard.Ranking";

        // O PlayerPrefs só guarda número e texto; a lista inteira vai como um texto JSON.
        [Serializable]
        private class Data
        {
            public List<RankingEntry> entries = new List<RankingEntry>();
        }

        /// <summary>As melhores partidas, da maior pontuação para a menor (no máximo 5).</summary>
        public static IReadOnlyList<RankingEntry> GetTop()
        {
            return Load().entries;
        }

        /// <summary>
        /// Registra uma partida terminada. Devolve a posição (1 a 5) se ela entrou no ranking,
        /// ou 0 se não entrou. Partida com 0 pontos não entra. Em caso de empate, quem já
        /// estava no ranking fica na frente.
        /// </summary>
        public static int Submit(int score, string difficulty)
        {
            if (score <= 0)
                return 0;

            Data data = Load();

            // Primeiro lugar da lista onde a nova pontuação é estritamente maior.
            int index = data.entries.FindIndex(entry => score > entry.score);
            if (index < 0)
                index = data.entries.Count;
            if (index >= MaxEntries)
                return 0;

            data.entries.Insert(index, new RankingEntry
            {
                score = score,
                difficulty = difficulty,
                date = DateTime.Now.ToString("dd/MM/yyyy"),
            });

            if (data.entries.Count > MaxEntries)
                data.entries.RemoveRange(MaxEntries, data.entries.Count - MaxEntries);

            Save(data);
            return index + 1;
        }

        /// <summary>Apaga o ranking.</summary>
        public static void Clear()
        {
            PlayerPrefs.DeleteKey(Key);
            PlayerPrefs.Save();
        }

        private static Data Load()
        {
            string json = PlayerPrefs.GetString(Key, string.Empty);
            if (string.IsNullOrEmpty(json))
                return new Data();

            try
            {
                Data data = JsonUtility.FromJson<Data>(json);
                return data ?? new Data();
            }
            catch (ArgumentException)
            {
                // Texto corrompido: começa um ranking novo em vez de travar o menu.
                return new Data();
            }
        }

        private static void Save(Data data)
        {
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }
    }
}
