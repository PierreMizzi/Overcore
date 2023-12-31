using UnityEngine;
using System.Collections.Generic;
using PierreMizzi.Useful.PoolingObjects;
using PierreMizzi.Pause;

namespace Bitfrost.Gameplay.Enemies
{

    public class EnemyManager : MonoBehaviour, IPausable
    {

        #region Fields

        [Header("Channels")]
        [SerializeField]
        private LevelChannel m_levelChannel = null;

        [SerializeField]
        private EnemyChannel m_enemyChannel;

        private Camera m_camera = null;

        [Header("Pooling")]
        [SerializeField]
        private PoolingChannel m_poolingChannel = null;

        [SerializeField]
        private List<EnemyPoolConfig> m_enemyPoolConfigs = new List<EnemyPoolConfig>();

        /// <summary>
        /// Associate one type of enemy to it respective spawner
        /// </summary>
        private Dictionary<EnemyType, EnemySpawner> m_enemyTypeToSpawner =
            new Dictionary<EnemyType, EnemySpawner>();

        /// <summary>
        /// All enemies out of the pool and currently active
        /// </summary>
        private List<Enemy> m_activeEnemies = new List<Enemy>();

        #region Spawn Bounds

        [Header("Spawn Bounds")]
        [SerializeField]
        private float m_offsetSpawnBounds = 1f;

        private float m_spawnBoundsAreaSize = 2f;

        private Bounds m_enemySpawnBounds;

        #endregion

        #region Stage Enemy Kill Count

        /// <summary>
        /// Number of enemies to kill to go to next stage
        /// </summary>
        private int m_stageEnemyCount;

        /// <summary>
        /// Current number of enemies killed 
        /// </summary>
        private int m_stageKilledEnemyCount;

        public bool areAllEnemiesKilled
        {
            get
            {
                return m_stageKilledEnemyCount >= m_stageEnemyCount;
            }
        }

        #endregion

        #region Pause

        public bool isPaused { get; set; }

        #endregion

        #endregion

        #region Methods

        #region MonoBehaviour

        private void Awake()
        {
            m_camera = Camera.main;
            CreateEnemySpawnBounds();
        }

        private void Start()
        {
            m_enemyChannel.killCount = 0;
            InitializeEnemyPools();

            if (m_levelChannel != null)
            {
                m_levelChannel.onReset += CallbackReset;
                m_levelChannel.onPauseGame += Pause;
                m_levelChannel.onResumeGame += Resume;
            }

            if (m_enemyChannel != null)
                m_enemyChannel.onGetActiveEnemiesTotalHealth += GetActiveEnemiesTotalHealth;
        }

        private void Update()
        {

#if UNITY_EDITOR
            ManageQuickSpawn();
#endif
        }

        private void LateUpdate()
        {
            UpdateEnemySpawnBounds();
        }

        private void OnDestroy()
        {
            if (m_levelChannel != null)
            {
                m_levelChannel.onReset -= CallbackReset;
                m_levelChannel.onPauseGame -= Pause;
                m_levelChannel.onResumeGame -= Resume;
            }

            if (m_enemyChannel != null)
                m_enemyChannel.onGetActiveEnemiesTotalHealth -= GetActiveEnemiesTotalHealth;
        }

        private void OnDrawGizmos()
        {
            Gizmos.DrawWireCube(m_enemySpawnBounds.center, m_enemySpawnBounds.size);
            Gizmos.DrawWireCube(
                m_enemySpawnBounds.center,
                m_enemySpawnBounds.size * m_spawnBoundsAreaSize
            );
        }

        #endregion

        #region Spawning

        public void ChangeEnemySpawnConfig(EnemySpawnConfig config)
        {
            if (m_enemyTypeToSpawner.ContainsKey(config.prefab.type))
            {
                m_enemyTypeToSpawner[config.prefab.type].ChangeConfig(config);
                m_stageEnemyCount += config.count;
            }
        }

        /// <summary>
        /// Spawn multiple enemies at once
        /// </summary>
        /// <param name="prefab">prefab of the enemy</param>
        /// <param name="count">amount in batch</param>
        public void SpawnEnemyBatch(GameObject prefab, int count)
        {
            for (int i = 0; i < count; i++)
                SpawnEnemy(prefab);
        }

        public void SpawnEnemy(GameObject prefab)
        {
            Enemy enemy = m_poolingChannel.onGetFromPool.Invoke(prefab).GetComponent<Enemy>();
            enemy.transform.position = GetCameraEdgeRandomPosition();
            enemy.OutOfPool(this);

            if (!m_activeEnemies.Contains(enemy))
                m_activeEnemies.Add(enemy);
        }

        private void InitializeEnemyPools()
        {
            foreach (EnemyPoolConfig config in m_enemyPoolConfigs)
            {
                m_poolingChannel.onCreatePool.Invoke(config);
                CreateEnemySpawner(config);
            }
        }

        private void CreateEnemySpawner(EnemyPoolConfig config)
        {
            GameObject newGameObject = new GameObject(config.prefab.name + "Spawner");
            newGameObject.transform.parent = transform;
            EnemySpawner spawner = newGameObject.AddComponent<EnemySpawner>();
            spawner.Initialize(this);

            if (!m_enemyTypeToSpawner.ContainsKey(config.type))
                m_enemyTypeToSpawner.Add(config.type, spawner);
        }

        /// <summary>
        /// Used for debugging.
        /// With the assigned keyboard key, user can spawn one enemy in Editor Mode
        /// </summary>
        private void ManageQuickSpawn()
        {
            foreach (EnemyPoolConfig config in m_enemyPoolConfigs)
            {
                if (Input.GetKeyDown(config.quickSpawnKey))
                    SpawnEnemy(config.prefab.gameObject);
            }
        }

        #region Spawn Bounds

        private void UpdateEnemySpawnBounds()
        {
            Vector3 center = m_camera.transform.position;
            center.z = 0;
            m_enemySpawnBounds.center = center;
        }

        private void CreateEnemySpawnBounds()
        {
            m_enemySpawnBounds = new Bounds();

            // Center
            UpdateEnemySpawnBounds();

            // Size
            Vector3 size = new Vector3();

            size.y = m_camera.orthographicSize * 2f + m_offsetSpawnBounds * 2f;
            size.x = (m_camera.aspect * m_camera.orthographicSize * 2f) + m_offsetSpawnBounds * 2f;

            m_enemySpawnBounds.size = size;
        }

        /// <summary>
        /// All enemies spawn on the edge of caemras's bound.
        /// Returns a random position along a random edge of the camera
        /// </summary>
        /// <returns></returns>
        private Vector3 GetCameraEdgeRandomPosition()
        {
            // Compute random position on the bounds
            Vector3 randomPosition = new Vector2();

            bool horizontalOrVertical = Random.Range(0, 2) == 0;
            bool positiveOrNegative = Random.Range(0, 2) == 0;

            if (horizontalOrVertical)
            {
                // Random position along the horizontal camera size
                randomPosition.x = Random.Range(
                    -m_enemySpawnBounds.extents.x,
                    m_enemySpawnBounds.extents.x
                );

                // Set the vertical position at the top or bottom side of the camera
                randomPosition.y = (positiveOrNegative ? 1 : -1) * m_enemySpawnBounds.extents.y;
            }
            else
            {
                // Random position along the vertical camera size
                randomPosition.y = Random.Range(
                    -m_enemySpawnBounds.extents.y,
                    m_enemySpawnBounds.extents.y
                );

                // Set the horieontal position at the left or right side of the camera
                randomPosition.x = (positiveOrNegative ? 1 : -1) * m_enemySpawnBounds.extents.x;
            }

            Vector3 dirToPosition = randomPosition.normalized;

            randomPosition += m_enemySpawnBounds.center;
            randomPosition += dirToPosition * Random.Range(1f, m_spawnBoundsAreaSize);

            return randomPosition;
        }

        #endregion

        #endregion

        #region Killing

        /// <summary>
        /// Releases an enemy 
        /// </summary>
        /// <param name="enemy"></param>
        public void ReleaseToPool(Enemy enemy)
        {
            m_poolingChannel.onReleaseToPool.Invoke(enemy.gameObject);

            if (m_activeEnemies.Contains(enemy))
                m_activeEnemies.Remove(enemy);

            m_stageKilledEnemyCount += 1;

            m_enemyChannel.killCount++;

            if (areAllEnemiesKilled)
                m_levelChannel.onAllEnemiesKilled.Invoke();
        }

        #endregion

        #region Reset

        public void CallbackReset()
        {
            m_enemyChannel.killCount = 0;
            m_stageEnemyCount = 0;
            m_stageKilledEnemyCount = 0;

            ResetEnemySpawner();

            ResetActiveEnemies();
        }

        public void ResetEnemySpawner()
        {
            foreach (KeyValuePair<EnemyType, EnemySpawner> pair in m_enemyTypeToSpawner)
                pair.Value.Reset();
        }

        public void ResetActiveEnemies()
        {
            foreach (Enemy enemy in m_activeEnemies)
            {
                enemy.ChangeState(EnemyStateType.Inactive);
                m_poolingChannel.onReleaseToPool.Invoke(enemy.gameObject);
            }

            m_activeEnemies.Clear();
        }

        #endregion

        #region Pause


        public void Pause()
        {
            isPaused = true;

            foreach (KeyValuePair<EnemyType, EnemySpawner> pair in m_enemyTypeToSpawner)
                pair.Value.Pause();

            foreach (Enemy enemy in m_activeEnemies)
                enemy.Pause();
        }

        public void Resume()
        {
            isPaused = false;

            foreach (KeyValuePair<EnemyType, EnemySpawner> pair in m_enemyTypeToSpawner)
                pair.Value.Resume();

            foreach (Enemy enemy in m_activeEnemies)
                enemy.Resume();
        }

        #endregion

        #region Behaviour

        private float GetActiveEnemiesTotalHealth()
        {
            float totalHealth = 0;
            foreach (Enemy enemy in m_activeEnemies)
                totalHealth += enemy.healthEntity.currentHealth;

            return totalHealth;
        }

        #endregion

        #endregion

    }
}
