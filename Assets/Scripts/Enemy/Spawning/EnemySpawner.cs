using UnityEngine;
using System;
using System.Collections;

public class EnemySpawner : MonoBehaviour
{
    private EnemyManager m_manager = null;

    [SerializeField]
    private Enemy m_enemy;

    public Enemy enemy
    {
        get { return m_enemy; }
    }

    private EnemySpawnConfig m_config;

    private int currentCount;

    private IEnumerator m_spawnCoroutine;

    public void Initialize(EnemyManager manager)
    {
        m_manager = manager;
    }

    public void ChangeConfig(EnemySpawnConfig config)
    {
        Debug.Log($"CHANGED CONFIG FOR : {config.prefab.type}");
        if (Application.isPlaying)
        {
            m_config = config;
            StartSpawning();
        }
    }

    public void StartSpawning()
    {
        if (m_spawnCoroutine == null)
        {
            m_spawnCoroutine = SpawningCoroutine();
            StartCoroutine(m_spawnCoroutine);
        }
    }

    public void StopSpawning()
    {
        if (m_spawnCoroutine != null)
        {
            StopCoroutine(m_spawnCoroutine);
            m_spawnCoroutine = null;
        }
    }

    public void PlaySpawning()
    {
        if (m_spawnCoroutine != null)
            StartCoroutine(m_spawnCoroutine);
    }

    public void PauseSpawning()
    {
        if (m_spawnCoroutine != null)
            StopCoroutine(m_spawnCoroutine);
    }

    public IEnumerator SpawningCoroutine()
    {
        while (true)
        {
            m_manager.SpawnEnemy(m_config.prefab.gameObject, m_config.batchCount);
            yield return new WaitForSeconds(m_config.spawnFrequency);
        }
    }


}
