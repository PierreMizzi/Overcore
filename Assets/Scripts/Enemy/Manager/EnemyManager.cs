using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/*

    Use timeline
    Each tracks manages the spawn rate of each enemies
    Special event for special waves
    Controlable

*/

[ExecuteInEditMode]
public partial class EnemyManager : MonoBehaviour
{
	#region Fields

    private Camera m_camera = null;

    [SerializeField]
    private PoolingChannel m_poolingChannel = null;

    [SerializeField]
    private List<EnemyPoolConfig> m_enemyPoolConfigs = new List<EnemyPoolConfig>();

    [SerializeField]
    private List<EnemySpawnShortcutConfig> m_enemySpawnShortcutConfigs =
        new List<EnemySpawnShortcutConfig>();

    private Dictionary<EnemyType, EnemySpawner> m_enemyTypeToSpawner =
        new Dictionary<EnemyType, EnemySpawner>();


    #region Spawn Bounds

    [Header("Spawn Bounds")]
    [SerializeField]
    private float m_offsetSpawnBounds = 1f;

    private Bounds m_enemySpawnBounds;

    #endregion

	#endregion

	#region Methods

    #region MonoBehaviour

    private void Awake()
    {
        m_camera = Camera.main;
        CreateEnemySpawnBounds();
    }

    private void OnEnable()
    {
        m_camera = Camera.main;
        CreateEnemySpawnBounds();
    }

    private void Start()
    {
        InitializeEnemyPools();
        InitializeEnemySpawners();
    }

    private void Update()
    {
        ManageQuickSpawn();
    }

    private void LateUpdate()
    {
        UpdateEnemySpawnBounds();
    }

    private void OnDestroy() { }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(m_enemySpawnBounds.center, m_enemySpawnBounds.size);
    }

    #endregion

    #region Spawning

    private void InitializeEnemySpawners()
    {
        foreach (Transform child in transform)
        {
            if (child.TryGetComponent(out EnemySpawner spawner))
            {
                spawner.Initialize(this);
                m_enemyTypeToSpawner.Add(spawner.enemy.type, spawner);
            }
        }
    }

    public void ChangeEnemySpawnConfig(EnemySpawnConfig config)
    {
        if(m_enemyTypeToSpawner.ContainsKey(config.prefab.type))
        {
            m_enemyTypeToSpawner[config.prefab.type].ChangeConfig(config);
        }
    }

    public void SpawnEnemy(GameObject prefab)
    {
        Enemy enemy = m_poolingChannel.onGetFromPool.Invoke(prefab).GetComponent<Enemy>();
        enemy.GetFromPool(this);

        enemy.transform.position = GetCameraEdgeRandomPosition();
    }

    private void InitializeEnemyPools()
    {
        foreach (EnemyPoolConfig config in m_enemyPoolConfigs)
        {
            m_poolingChannel.onCreatePool.Invoke(config);
        }
    }

    #endregion

    #region Quick Spawning

    private void ManageQuickSpawn()
    {
        foreach (EnemySpawnShortcutConfig config in m_enemySpawnShortcutConfigs)
        {
            if (Input.GetKeyDown(config.quickSpawnKey))
                SpawnEnemy(config.prefab.gameObject);
        }
    }

    #endregion

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

    private Vector3 GetCameraEdgeRandomPosition()
    {
        Vector3 randomPosition = new Vector2();

        bool horizontalOrVertical = Random.Range(0, 2) == 0;
        bool positiveOrNegative = Random.Range(0, 2) == 0;

        if (horizontalOrVertical)
        {
            randomPosition.x = Random.Range(
                -m_enemySpawnBounds.extents.x,
                m_enemySpawnBounds.extents.x
            );
            randomPosition.y = (positiveOrNegative ? 1 : -1) * m_enemySpawnBounds.extents.y;
        }
        else
        {
            randomPosition.y = Random.Range(
                -m_enemySpawnBounds.extents.y,
                m_enemySpawnBounds.extents.y
            );
            randomPosition.x = (positiveOrNegative ? 1 : -1) * m_enemySpawnBounds.extents.x;
        }

        randomPosition += m_enemySpawnBounds.center;
        return randomPosition;
    }

    #endregion

    #region DestroyEnemy

    public void Release(GameObject gameObject)
    {
        m_poolingChannel.onReleaseFromPool.Invoke(gameObject);
    }

    #endregion

	#endregion
}
