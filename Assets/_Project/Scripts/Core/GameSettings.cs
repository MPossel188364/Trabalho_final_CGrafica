namespace NebulaGuard
{
    /// <summary>
    /// Escolhas feitas no menu principal que a cena Game precisa ler. Cada cena do Unity é
    /// destruída ao ser trocada, então o que precisa sobreviver à troca fica numa classe
    /// estática. Por enquanto só guarda a dificuldade.
    /// </summary>
    public static class GameSettings
    {
        /// <summary>
        /// Dificuldade escolhida no menu. Fica nula se a cena Game foi aberta direto no
        /// editor: aí o GameManager usa a dificuldade que está no Inspector dele.
        /// </summary>
        public static DifficultyData SelectedDifficulty { get; set; }
    }
}
