using System;
using System.Collections.Generic;
using UnityEngine;

namespace NebulaGuard
{
    /// <summary>Uma linha do ranking: quem jogou, quantos pontos, em qual dificuldade e quando.</summary>
    [Serializable]
    public class RankingEntry
    {
        public string name;
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

        /// <summary>Tamanho máximo do nome (cabe na linha do ranking com a fonte grande).</summary>
        public const int MaxNameLength = 12;

        /// <summary>Nome usado quando o jogador ainda não digitou nenhum.</summary>
        public const string DefaultName = "Jogador";

        private const string Key = "NebulaGuard.Ranking";
        private const string LastNameKey = "NebulaGuard.LastPlayerName";

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
        public static int Submit(int score, string difficulty, string name)
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
                name = CleanName(name),
                score = score,
                difficulty = difficulty,
                date = DateTime.Now.ToString("dd/MM/yyyy"),
            });

            if (data.entries.Count > MaxEntries)
                data.entries.RemoveRange(MaxEntries, data.entries.Count - MaxEntries);

            Save(data);
            return index + 1;
        }

        /// <summary>
        /// Troca o nome de uma linha do ranking (posição 1 a 5, a que o Submit devolveu) e
        /// guarda o nome como o último usado, para já vir preenchido na próxima partida.
        /// </summary>
        public static void SetName(int position, string name)
        {
            string clean = CleanName(name);
            PlayerPrefs.SetString(LastNameKey, clean);

            Data data = Load();
            int index = position - 1;
            if (index >= 0 && index < data.entries.Count)
            {
                data.entries[index].name = clean;
                Save(data);
            }
            else
            {
                PlayerPrefs.Save();
            }
        }

        /// <summary>O último nome salvo, ou "Jogador" se ninguém salvou ainda.</summary>
        public static string LastPlayerName => PlayerPrefs.GetString(LastNameKey, DefaultName);

        /// <summary>Tira espaços das pontas, corta no tamanho máximo e troca vazio por "Jogador".</summary>
        public static string CleanName(string name)
        {
            string clean = (name ?? string.Empty).Trim();
            if (clean.Length > MaxNameLength)
                clean = clean.Substring(0, MaxNameLength);
            return clean.Length > 0 ? clean : DefaultName;
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
