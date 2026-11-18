using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NebulaGuard
{
    /// <summary>
    /// Os efeitos sonoros do jogo. Cada valor aponta para um arquivo em
    /// <c>Audio/Resources/Sfx/</c> (o nome do arquivo está em <see cref="AudioManager"/>).
    /// O valor numérico fica salvo nos assets (TowerData.fireSound): só acrescentar ao FIM.
    /// </summary>
    public enum Sfx
    {
        Laser,          // tiro do Caça (0 = padrão dos TowerData antigos)
        Missile,        // tiro do Bombardeiro
        Railgun,        // tiro do Cruzador
        Impact,         // tiro acertando o inimigo
        Explosion,      // inimigo destruído
        BigExplosion,   // explosão do míssil (dano em área)
        BaseHit,        // inimigo bate na nave-mãe
        Build,          // nave posicionada
        Sell,           // nave vendida
        Spawn,          // inimigo sai do portal
        Click,          // botão de interface
        Victory,
        Defeat,
    }

    /// <summary>
    /// Sons e música do jogo. É criado sozinho ao abrir o jogo (não precisa de objeto na cena) e
    /// sobrevive à troca de cena, então a música não corta entre o menu e a partida.
    ///
    /// Como funciona:
    /// - Quem quer tocar um efeito chama <c>AudioManager.Play(Sfx.X)</c> (estático; sem
    ///   AudioManager ainda, não faz nada). Quem sabe de cada evento (Projectile, TowerShip...)
    ///   só pede o som: o AudioManager decide volume, variação de tom e limite de repetição.
    /// - Os eventos que já existem (vida da nave-mãe, estado da partida) são escutados aqui
    ///   mesmo, a cada cena nova: base atingida, alarme de vida baixa, vitória e derrota.
    /// - Os arquivos são carregados por <c>Resources.Load</c> (pasta Resources): nenhuma
    ///   referência para arrastar no Inspector, nenhuma cena para editar.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        private const int PoolSize = 12;

        // Nome do arquivo (sem extensão) de cada efeito, na ordem do enum Sfx.
        private static readonly string[] FileNames =
        {
            "laser", "missile", "railgun", "impact", "explosion", "big_explosion",
            "base_hit", "build", "sell", "spawn", "click", "victory", "defeat",
        };

        // Volume de cada efeito (0..1), na ordem do enum. Tiros e impactos são baixos porque
        // tocam o tempo todo; base atingida e fim de jogo são altos.
        private static readonly float[] Volumes =
        {
            0.35f, 0.5f, 0.55f, 0.25f, 0.55f, 0.7f,
            0.9f, 0.6f, 0.6f, 0.3f, 0.5f, 0.8f, 0.8f,
        };

        // Variação aleatória de tom (± fração), na ordem do enum: tiros repetidos não soam "metralhadora de cópia".
        private static readonly float[] PitchSpread =
        {
            0.08f, 0.05f, 0.04f, 0.1f, 0.1f, 0.06f,
            0.03f, 0f, 0f, 0.05f, 0.03f, 0f, 0f,
        };

        // Tempo mínimo (s) entre dois toques do mesmo efeito: 10 inimigos morrendo no mesmo
        // frame viram um som só, em vez de um estouro de volume.
        private const float MinInterval = 0.05f;

        [Header("Volumes gerais (0..1)")]
        [Range(0f, 1f)] [SerializeField] private float sfxVolume = 0.8f;
        [Range(0f, 1f)] [SerializeField] private float musicVolume = 0.35f;

        [Tooltip("Volume do alarme da nave-mãe com pouca vida.")]
        [Range(0f, 1f)] [SerializeField] private float alarmVolume = 0.3f;

        [Tooltip("A nave-mãe com vida igual ou abaixo desta fração (0..1) liga o alarme.")]
        [Range(0f, 1f)] [SerializeField] private float alarmHealthFraction = 0.3f;

        [Tooltip("Fração do volume da música durante a pausa e a tela de fim.")]
        [Range(0f, 1f)] [SerializeField] private float musicDuck = 0.3f;

        private static AudioManager instance;

        private AudioSource[] pool;
        private int nextSource;
        private AudioSource music;
        private AudioSource alarm;
        private AudioClip[] clips;
        private float[] lastPlayed;

        // Ligações da cena atual (trocadas a cada cena nova).
        private GameManager gameManager;
        private MotherShip motherShip;
        private int lastHealth = -1;

        // Com "Enter Play Mode Options" (domínio não recarregado), os campos estáticos sobram da
        // partida anterior: zera antes de qualquer cena carregar.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            instance = null;
        }

        // Cria o AudioManager antes da primeira cena; se por algum motivo ainda não existir
        // quando a cena já carregou, a segunda chamada (AfterSceneLoad) cria.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void BootstrapBeforeScene()
        {
            Bootstrap();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void BootstrapAfterScene()
        {
            EnsureExists();
        }

        /// <summary>
        /// Garante que o AudioManager existe. Os scripts que abrem uma cena (MainMenu, GameManager) e o
        /// próprio <c>Play</c> chamam isto: mesmo que a criação automática no início do jogo não tenha
        /// rodado, o primeiro pedido de som cria o objeto e liga a cena atual.
        /// </summary>
        public static void EnsureExists()
        {
            if (instance != null)
                return;

            Bootstrap();
            // A cena já carregou sem ele: liga as ligações da cena atual (música, vida da base) agora.
            instance.OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        private static void Bootstrap()
        {
            if (instance != null)
                return;

            var holder = new GameObject("AudioManager");
            DontDestroyOnLoad(holder);
            holder.AddComponent<AudioManager>();
            Debug.Log("AudioManager criado.");
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;

            clips = new AudioClip[FileNames.Length];
            lastPlayed = new float[FileNames.Length];
            for (int i = 0; i < FileNames.Length; i++)
            {
                clips[i] = Resources.Load<AudioClip>("Sfx/" + FileNames[i]);
                lastPlayed[i] = -10f;
                if (clips[i] == null)
                    Debug.LogWarning($"AudioManager: faltou o som Resources/Sfx/{FileNames[i]}.", this);
            }

            pool = new AudioSource[PoolSize];
            for (int i = 0; i < PoolSize; i++)
                pool[i] = CreateSource("Sfx " + i, false);

            music = CreateSource("Música", true);
            alarm = CreateSource("Alarme", true);
            alarm.clip = Resources.Load<AudioClip>("Sfx/alarm");
        }

        private AudioSource CreateSource(string sourceName, bool loop)
        {
            var child = new GameObject(sourceName);
            child.transform.SetParent(transform, false);
            AudioSource source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f; // 2D: o jogo é plano, o som não depende da posição
            return source;
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
        }

        private void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        // ---------------------------------------------------------------- API

        /// <summary>Toca um efeito. Se o AudioManager ainda não existe, cria.</summary>
        public static void Play(Sfx sound)
        {
            EnsureExists();
            instance.PlayInternal(sound);
        }

        /// <summary>Atalho para ligar num botão: <c>button.onClick.AddListener(AudioManager.PlayClick)</c>.</summary>
        public static void PlayClick()
        {
            Play(Sfx.Click);
        }

        private void PlayInternal(Sfx sound)
        {
            int index = (int)sound;
            if (index < 0 || index >= clips.Length || clips[index] == null)
                return;

            // Tempo sem escala: na pausa e no fim da partida (timeScale = 0) o som precisa tocar.
            float now = Time.unscaledTime;
            if (now - lastPlayed[index] < MinInterval)
                return;
            lastPlayed[index] = now;

            AudioSource source = pool[nextSource];
            nextSource = (nextSource + 1) % PoolSize;

            float spread = PitchSpread[index];
            source.pitch = 1f + UnityEngine.Random.Range(-spread, spread);
            source.volume = Volumes[index] * sfxVolume;
            source.clip = clips[index];
            source.Play();
        }

        // ---------------------------------------------------------------- cenas

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Música: o menu tem a dele; qualquer outra cena (Game, Sandbox) usa a da partida.
            PlayMusic(scene.name == "MainMenu" ? "Music/menu" : "Music/jogo");

            Disconnect();

            // Só a cena de jogo tem GameManager e MotherShip.
            gameManager = FindFirstObjectByType<GameManager>();
            motherShip = FindFirstObjectByType<MotherShip>();
            lastHealth = -1;

            if (gameManager != null)
                gameManager.StateChanged += OnStateChanged;
            if (motherShip != null)
            {
                motherShip.HealthChanged += OnHealthChanged;
                motherShip.Destroyed += OnBaseDestroyed;
            }
        }

        private void OnSceneUnloaded(Scene scene)
        {
            Disconnect();
            StopAlarm();
        }

        private void Disconnect()
        {
            // "!= null" também é falso para objeto já destruído pelo Unity: só desliga o que existe.
            if (gameManager != null)
                gameManager.StateChanged -= OnStateChanged;
            if (motherShip != null)
            {
                motherShip.HealthChanged -= OnHealthChanged;
                motherShip.Destroyed -= OnBaseDestroyed;
            }

            gameManager = null;
            motherShip = null;
            music.volume = musicVolume;
        }

        private void PlayMusic(string path)
        {
            AudioClip clip = Resources.Load<AudioClip>(path);
            if (clip == null)
            {
                Debug.LogWarning($"AudioManager: faltou a música Resources/{path}.", this);
                return;
            }

            music.volume = musicVolume;

            // Reiniciar a partida recarrega a cena: a mesma música continua, sem voltar ao início.
            if (music.clip == clip && music.isPlaying)
                return;

            music.clip = clip;
            music.Play();
        }

        // ---------------------------------------------------------------- eventos do jogo

        private void OnHealthChanged(int current, int max)
        {
            // O evento também dispara quando a vida enche (início da partida): só é "pancada"
            // quando a vida diminuiu (mesmo critério do CameraShake).
            if (lastHealth >= 0 && current < lastHealth)
                Play(Sfx.BaseHit);

            lastHealth = current;

            bool low = current > 0 && max > 0 && (float)current / max <= alarmHealthFraction;
            if (low)
                StartAlarm();
            else
                StopAlarm();
        }

        private void OnBaseDestroyed()
        {
            StopAlarm();
        }

        private void OnStateChanged(GameState state)
        {
            switch (state)
            {
                case GameState.Playing:
                    music.volume = musicVolume;
                    break;
                case GameState.Paused:
                    music.volume = musicVolume * musicDuck;
                    break;
                case GameState.Victory:
                    StopAlarm();
                    music.volume = musicVolume * musicDuck;
                    Play(Sfx.Victory);
                    break;
                case GameState.Defeat:
                    StopAlarm();
                    music.volume = musicVolume * musicDuck;
                    Play(Sfx.Defeat);
                    break;
            }
        }

        private void StartAlarm()
        {
            if (alarm.clip == null || alarm.isPlaying)
                return;

            alarm.volume = alarmVolume * sfxVolume;
            alarm.Play();
        }

        private void StopAlarm()
        {
            if (alarm != null && alarm.isPlaying)
                alarm.Stop();
        }
    }
}
