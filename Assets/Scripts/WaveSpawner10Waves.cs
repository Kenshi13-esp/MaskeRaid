using System.Collections;
using UnityEngine;

public class WaveSpawner10Waves : MonoBehaviour
{
    [Header("Waves")]
    [SerializeField] private int totalWaves = 10;
    [SerializeField] private int startEnemies = 4;
    [SerializeField] private int addPerWave = 2;

    [Header("Spawning Area (rectangle)")]
    [SerializeField] private Vector2 minSpawn = new Vector2(-7f, -4f);
    [SerializeField] private Vector2 maxSpawn = new Vector2(7f, 4f);

    [Header("References")]
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private Transform enemyParent;

    [Tooltip("Si lo dejas vacío, lo buscará por Tag Player.")]
    [SerializeField] private Transform player;

    private int currentWave = 0;
    private int aliveEnemies = 0;

    private void Start()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        StartCoroutine(RunWaves());
    }

    private IEnumerator RunWaves()
    {
        for (currentWave = 1; currentWave <= totalWaves; currentWave++)
        {
            int count = startEnemies + addPerWave * (currentWave - 1);
            Debug.Log($"WAVE {currentWave}/{totalWaves} - Spawning {count} enemies");

            SpawnWave(count);

            // Esperar a que NO quede ningún enemigo vivo
            while (aliveEnemies > 0)
                yield return null;

            Debug.Log($"WAVE {currentWave} cleared!");
            yield return new WaitForSeconds(0.4f);
        }

        Debug.Log("VICTORY! Completed wave 10.");
    }

    private void SpawnWave(int count)
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("WaveSpawner: enemyPrefab is NULL.");
            return;
        }

        for (int i = 0; i < count; i++)
        {
            Vector2 pos = new Vector2(
                Random.Range(minSpawn.x, maxSpawn.x),
                Random.Range(minSpawn.y, maxSpawn.y)
            );

            GameObject go = Instantiate(enemyPrefab, pos, Quaternion.identity, enemyParent);

            EnemyHealth hp = go.GetComponent<EnemyHealth>();
            if (hp != null)
                hp.OnDeath += HandleEnemyDeath;

            EnemyChase2D chase = go.GetComponent<EnemyChase2D>();
            if (chase != null && player != null)
                chase.SetTarget(player);

            aliveEnemies++;
        }
    }

    private void HandleEnemyDeath(EnemyHealth dead)
    {
        aliveEnemies = Mathf.Max(0, aliveEnemies - 1);

        if (dead != null)
            dead.OnDeath -= HandleEnemyDeath;
    }

    // Gizmos para ver el rectángulo de spawn en la escena
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 center = (minSpawn + maxSpawn) * 0.5f;
        Vector3 size = new Vector3(Mathf.Abs(maxSpawn.x - minSpawn.x), Mathf.Abs(maxSpawn.y - minSpawn.y), 0.1f);
        Gizmos.DrawWireCube(center, size);
    }
}
