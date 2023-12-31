using System.Collections;
using System.Collections.Generic;
using Bitfrost.Application;
using Bitfrost.Gameplay.Enemies;
using Bitfrost.Gameplay.Energy;
using PierreMizzi.Pause;
using PierreMizzi.SoundManager;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Playables;

namespace Bitfrost.Gameplay
{

    /// <summary>
    /// Most important class when it come to in-game logic. Manages time, timeline to go through stages,
    /// win conditions, loose conditions ... A little bit too much maybe ? 
    /// </summary>
    public class LevelManager : MonoBehaviour, IPausable
    {
        #region Fields

        [Header("Channels")]
        [SerializeField]
        private ApplicationChannel m_appChannel = null;

        [SerializeField]
        private LevelChannel m_levelChannel = null;

        [SerializeField]
        private EnemyChannel m_enemyChannel;

        [SerializeField]
        private CrystalShardsChannel m_crytalShardsChannel;

        [Header("Arena")]
        [SerializeField]
        private float m_arenaDiameter = 10f;

        public static float arenaRadius;

        public static float arenaRadiusSqr = 0f;

        public bool isPaused { get; set; }

        #region Time

        /// <summary>
        /// Time elapsed since the beginning of one game
        /// </summary>
        public float time { get; private set; }

        #endregion

        #region Timeline Stage

        /// <summary>
        /// Playable Director playing the stage timeline
        /// </summary>
        private PlayableDirector m_director;

        private bool m_isStageCompleted;

        private bool m_isStageDurationOver;

        #endregion

        #region Game Over

        /// <summary>
        /// Checks if the player has lost the game
        /// </summary>
        private IEnumerator m_loosingConditionCoroutine;

        #endregion

        /// <summary>
        /// The longer the player stays alive, the higher the difficulty.
        /// This number is purely visual, actual difficulty is managed by the timeline stage
        /// </summary>
        private int m_currentStageDifficulty;

        [Header("Tutorial")]
        [SerializeField] private bool m_displayTutorial = true;

        [Header("Debug")]
        [SerializeField]
        private bool m_useStartingTime = false;

        [SerializeField]
        private int m_startingTime = 0;

        [SerializeField]
        private SoundSource m_musicSoundSource = null;

        #endregion

        #region Methods

        #region MonoBehaviour

        private void Awake()
        {
            m_director = GetComponent<PlayableDirector>();
        }

        private IEnumerator Start()
        {
            InitializeArena();

            // Subscribes to events
            if (m_levelChannel != null)
            {
                m_levelChannel.onTutorialStartClicked += StartGameWithTutorial;
                m_levelChannel.onChangeStageDifficulty += CallbackChangeStageDifficulty;
                m_levelChannel.onAllEnemiesKilled += CallbackAllEnemiesKilled;
                m_levelChannel.onGameOver += CallbackGameOver;

                m_levelChannel.onRestart += CallbackRestart;
                m_levelChannel.onReset += CallbackReset;

                m_levelChannel.onPauseGame += Pause;
                m_levelChannel.onResumeGame += Resume;
            }

            yield return new WaitForSeconds(0.1f);

            // If debugging, we starts the game in debug mode
            if (m_levelChannel.isDebugging)
                ManageDebugMode();
            // Otherwise, we start the game normaly using timeline stage
            else
            {
                EnableTimelineStage();
                PlayStage();
            }

            // Tutorial Panel & adequate music
            if (m_displayTutorial)
                DisplayTutorial();
            else
                StartGameWithoutTutorial();

            m_musicSoundSource.SetLooping(true);
            m_musicSoundSource.Play();
        }

        private void Update()
        {
            if (!isPaused)
                time += Time.unscaledDeltaTime;
        }

        private void OnDestroy()
        {
            if (m_levelChannel != null)
            {
                m_levelChannel.onTutorialStartClicked += StartGameWithTutorial;
                m_levelChannel.onChangeStageDifficulty -= CallbackChangeStageDifficulty;
                m_levelChannel.onAllEnemiesKilled -= CallbackAllEnemiesKilled;
                // m_levelChannel.onPlayerDead -= CallbackGameOver;
                m_levelChannel.onGameOver -= CallbackGameOver;
                m_levelChannel.onReset -= CallbackReset;
                m_levelChannel.onRestart -= CallbackRestart;

                m_levelChannel.onPauseGame -= Pause;
                m_levelChannel.onResumeGame -= Resume;
            }
        }

        #endregion

        #region Tutorial

        private void StartGameWithoutTutorial()
        {
            m_musicSoundSource.SetSoundData(SoundDataID.MUSIC_IN_GAME);
            StartCheckLoosingConditions();
        }

        private void DisplayTutorial()
        {
            m_musicSoundSource.SetSoundData(SoundDataID.MUSIC_TUTORIAL);
            m_levelChannel.onPauseGame.Invoke();
            m_levelChannel.onDisplayTutorialPanel.Invoke();
        }

        private void StartGameWithTutorial()
        {
            m_levelChannel.onResumeGame.Invoke();
            m_musicSoundSource.FadeTransition(SoundDataID.MUSIC_IN_GAME);
            StartCheckLoosingConditions();
        }

        #endregion

        #region Arena

        private void InitializeArena()
        {
            arenaRadius = m_arenaDiameter / 2f;
            arenaRadiusSqr = math.pow(arenaRadius, 2f);
        }

        public static bool IsInsideArena(Vector3 position)
        {
            return position.sqrMagnitude < arenaRadiusSqr;
        }

        public static Vector3 RandomPositionInArena(float edgeRadius)
        {
            return RandomPosition(Vector3.zero, arenaRadius - edgeRadius);
        }

        public static Vector3 RandomPosition(Vector3 origin, float radius)
        {
            float randomAngle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            float randomLength = UnityEngine.Random.Range(0f, 1f);

            return origin + radius * randomLength * new Vector3(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle), 0);
        }

        public static List<Vector3> RandomPositions(Vector3 origin, int count, float radius)
        {
            List<Vector3> positions = new List<Vector3>();

            for (int i = 0; i < count; i++)
                positions.Add(RandomPosition(origin, radius));


            return positions;
        }

        #endregion

        #region Timeline Stage

        private void PlayStage()
        {
            m_director.Play();
            m_isStageDurationOver = false;
            m_isStageCompleted = false;
        }

        // Linked to Signal Emitter
        public void CallbackStayStage()
        {
            m_isStageDurationOver = true;

            if (!m_isStageCompleted)
                m_director.Pause();
            else
                PlayStage();
        }

        // Linked to Signal Emitter
        public void DisplayStageCleared()
        {
            m_levelChannel?.onDisplayStageCleared.Invoke();
        }

        // Linked to Signal Emitter
        public void DisplayHostileDetected()
        {
            m_levelChannel?.onDisplayHostilesDetected.Invoke();
        }

        public void CallbackAllEnemiesKilled()
        {
            m_isStageCompleted = true;

            if (m_isStageDurationOver)
                PlayStage();
        }

        private void ResetStage()
        {
            m_director.Stop();
            m_director.Play();

            m_currentStageDifficulty = 0;
        }

        private void CallbackChangeStageDifficulty(int value)
        {
            m_currentStageDifficulty = value;
        }

        private void EnableTimelineStage()
        {
            m_director.enabled = true;
            m_displayTutorial = true;
        }

        private void DisableTimelineStage()
        {
            m_director.enabled = false;
            m_displayTutorial = false;
        }

        #endregion

        #region Game Over

        private void CallbackGameOver()
        {
            GameOverData data = new GameOverData(m_currentStageDifficulty, time, m_enemyChannel.killCount);
            SaveManager.ManageBestScore(data);

            m_appChannel.onSetCursor.Invoke(CursorType.Normal);

            m_levelChannel.onPauseGame.Invoke();
            m_levelChannel.onDefeatPanel.Invoke(data);
        }

        // Signal Emitter in Timeline
        public void CallbackVictory()
        {
            StopCheckLoosingConditions();

            GameOverData data = new GameOverData(m_currentStageDifficulty, time, m_enemyChannel.killCount);
            SaveManager.ManageBestScore(data);

            m_appChannel.onSetCursor.Invoke(CursorType.Normal);

            m_levelChannel.onPauseGame.Invoke();
            m_levelChannel.onVictoryPanel.Invoke(data);
        }

        private void CallbackRestart()
        {
            m_levelChannel.onResumeGame.Invoke();
            m_levelChannel.onReset.Invoke();
        }

        private void CallbackReset()
        {
            StartCheckLoosingConditions();

            // Time
            time = 0;

            // Stage
            ResetStage();
        }

        #region Loosing Condition

        private void StartCheckLoosingConditions()
        {
            if (m_loosingConditionCoroutine == null)
            {
                m_loosingConditionCoroutine = CheckLoosingConditionsCoroutine();
                StartCoroutine(m_loosingConditionCoroutine);
            }
        }

        private void StopCheckLoosingConditions()
        {
            if (m_loosingConditionCoroutine != null)
            {
                StopCoroutine(m_loosingConditionCoroutine);
                m_loosingConditionCoroutine = null;
            }
        }

        private IEnumerator CheckLoosingConditionsCoroutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(1);

                if (IsPlayerDead())
                    GameOverPlayerDead();

                if (HasInsufficientEnergy())
                    GameOverInsufficientEnergy();

            }
        }

        #region Player Health

        private bool IsPlayerDead()
        {
            return m_levelChannel.player.healthEntity.currentHealth <= 0;
        }

        private void GameOverPlayerDead()
        {
            m_levelChannel.onDisablePlayerControls.Invoke();
            StopCheckLoosingConditions();
            m_levelChannel.player.SetDead();
            SoundManager.PlaySFX(SoundDataID.PLAYER_DEATH);
        }

        #endregion

        #region Insufficient Energy

        private bool HasInsufficientEnergy()
        {
            // Combined total health of all enemies currently alive
            float enemiesTotalHealth = m_enemyChannel.onGetActiveEnemiesTotalHealth.Invoke();

            // Total energy possibly extracted from all crystals shards 
            float crystalsMaxTotalExtracted = m_crytalShardsChannel.onGetActiveCrystalsTotalEnergy.Invoke();
            crystalsMaxTotalExtracted *= m_levelChannel.player.turretSettings.productionRatio;

            // Total damage possibly dealt from all energy potentily extracted
            float totalPossibleDamage = crystalsMaxTotalExtracted * m_levelChannel.player.turretSettings.bulletConfig.damage;

            // string log = "#### Insufficient Energy \r\n";
            // log += $"enemiesTotalHealth : {enemiesTotalHealth}\r\n";
            // log += $"crystalsMaxTotalExtracted : {crystalsMaxTotalExtracted}\r\n";
            // log += $"totalPossibleDamage : {totalPossibleDamage}\r\n";
            // Debug.Log(log);

            return totalPossibleDamage < enemiesTotalHealth;
        }

        private void GameOverInsufficientEnergy()
        {
            m_levelChannel.onDisablePlayerControls.Invoke();
            m_levelChannel.onInsufficientEnergy.Invoke();
            StopCheckLoosingConditions();
        }

        #endregion

        #endregion

        #endregion

        #region Pause

        public void Pause()
        {
            isPaused = true;
            m_director.Pause();
            m_appChannel.onSetCursor.Invoke(CursorType.Normal);
        }

        public void Resume()
        {
            isPaused = false;
            m_director.Play();
        }

        #endregion

        #region Debug

        private void ManageDebugMode()
        {
            if (m_useStartingTime)
            {
                EnableTimelineStage();
                m_director.time = m_startingTime;
                PlayStage();
            }
            else
                DisableTimelineStage();
        }

        #endregion

        #endregion
    }
}