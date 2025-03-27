using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZombieManager : MonoBehaviour
{
    private static ZombieManager _instance;

    public static ZombieManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<ZombieManager>();

                if (_instance == null)
                {
                    GameObject newInstance = new GameObject("ZombieManager");
                    _instance = newInstance.AddComponent<ZombieManager>();
                }
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    [Header("Zombie Settings")]
    [SerializeField] private Mesh m_dummyZombieMesh;
    [SerializeField] private Material m_dummyZombieMaterial;
    [SerializeField] private PlayerHealth m_playerHealth;
    [SerializeField] private float m_spawnRadius;
    [SerializeField] private float m_dummyChaseSpeed;
    [SerializeField] private int m_maxZombie;
    [SerializeField] private GameObject m_activeZombiePrefab;
    [SerializeField] private Transform m_player;
    [SerializeField] private Terrain m_terrain;
    [SerializeField] private LayerMask m_groundLayer;
    [SerializeField] private Transform m_zombieTransform;
    [SerializeField] private int m_activeZombieLimit;

    [Header("Spawn Points")]
    [SerializeField] private Transform[] m_spawnPoints;

    [Header("Time Spawning Settings")]
    [SerializeField] private float m_spawnInterval;
    [SerializeField] private int m_initialSpawnAmount;
    [SerializeField] private int m_spawnIncrement;
    [SerializeField] private int m_maxSpawnPerWave;

    public bool CanSpawnZombies { set; get; } = true;
    public int KilledZombieCount { set; get; } = 0;
    public int ActiveZombieCount { set; get; } = 0;

    private List<DummyZombieData> _dummyZombies = new List<DummyZombieData>();
    private Queue<GameObject> _activeZombiePool = new Queue<GameObject>();
    private int _activeZombieId = 1;
    private int _currentSpawnAmount = 0;

    public void StartSpawnZombies()
    {
        _currentSpawnAmount = m_initialSpawnAmount;
        SpawnZombies(_currentSpawnAmount);
        StartCoroutine(SpawnZombiesPeriodically());
    }

    private IEnumerator SpawnZombiesPeriodically()
    {
        while (CanSpawnZombies)
        {
            // If the current count is already at or above the maximum, pause spawning
            yield return new WaitUntil(() => _dummyZombies.Count < m_maxZombie);

            yield return new WaitForSeconds(m_spawnInterval);

            // Limit the spawn amount to maxSpawnPerWave
            int zombiesToSpawn = Mathf.Min(_currentSpawnAmount, m_maxSpawnPerWave);
            SpawnZombies(zombiesToSpawn);

            // Increment the spawn amount for the next wave
            _currentSpawnAmount += m_spawnIncrement;
        }
    }

    private void SpawnZombies(int count)
    {
        for (int i = 0; i < count; ++i)
        {
            Vector3 randomOffset = Random.insideUnitSphere * m_spawnRadius;
            randomOffset.y = 0;
            Vector3 newPosition = SnapToGround(GetSpawnPoint() + randomOffset);
            Quaternion newRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            var newZombie = new DummyZombieData
            {
                Position = newPosition,
                Rotation = newRotation,
                Up = Vector3.up,
                IsSwitchedToActive = false,
                ActiveZombieInstance = null,
                ZombieAI = null,
                IsPauseMovement = false
            };

            _dummyZombies.Add(newZombie);
        }
    }

    private void Update()
    {
        if (GameManager.Instance.CurrentGameState != GameManager.GameState.Playing)
        {
            return;
        }

        TrySwitchAndDespawnZombies();
        UpdateActiveZombies();
        UpdateDummyZombieMovement();
        RenderDummyZombies();

        if (ActiveZombieCount < m_activeZombieLimit)
        {
            foreach (var dummyZombie in _dummyZombies)
            {
                dummyZombie.IsPauseMovement = false;
            }
        }
    }

    private Vector3 GetSpawnPoint()
    {
        return (m_spawnPoints != null && m_spawnPoints.Length > 0)
                ? m_spawnPoints[Random.Range(0, m_spawnPoints.Length)].position
                : Random.insideUnitSphere * m_spawnRadius;
    }

    private Vector3 SnapToGround(Vector3 pos)
    {
        RaycastHit hit;
        Vector3 origin = pos + Vector3.up * 2f;
        if (Physics.Raycast(origin, Vector3.down, out hit, 5f, m_groundLayer))
        {
            return hit.point;
        }

        pos.y = m_terrain.SampleHeight(pos);
        return pos;
    }

    private void TrySwitchAndDespawnZombies()
    {
        foreach (var dummyZombie in _dummyZombies)
        {
            Vector3 viewportPoint = Camera.main.WorldToViewportPoint(dummyZombie.Position);
            bool isVisible = viewportPoint.z > 0 &&
                             viewportPoint.x >= 0 && viewportPoint.x <= 1 &&
                             viewportPoint.y >= 0 && viewportPoint.y <= 1;

            // If the zombie is active but out of range, despawn it
            if (dummyZombie.IsSwitchedToActive && !isVisible)
            {
                if (dummyZombie.ActiveZombieInstance != null)
                {
                    dummyZombie.Position = dummyZombie.ActiveZombieInstance.transform.position;
                    dummyZombie.Rotation = dummyZombie.ActiveZombieInstance.transform.rotation;
                    dummyZombie.Position = SnapToGround(dummyZombie.Position);
                    ReturnToPool(dummyZombie.ActiveZombieInstance);
                    dummyZombie.ActiveZombieInstance = null;
                }
                dummyZombie.ZombieAI = null;
                dummyZombie.IsSwitchedToActive = false;
                --ActiveZombieCount;
                continue;
            }

            // If the zombie is inactive and in range, activate it if possible
            if (!dummyZombie.IsSwitchedToActive && isVisible)
            {
                if (ActiveZombieCount < m_activeZombieLimit)
                {
                    GameObject activeZombie = GetFromPool();
                    if (activeZombie == null)
                    {
                        continue;
                    }

                    activeZombie.name = string.Format("Zombie - {0}", _activeZombieId);
                    ++_activeZombieId;
                    Vector3 spawnPos = SnapToGround(dummyZombie.PrevPosition);
                    activeZombie.transform.position = spawnPos;
                    activeZombie.transform.rotation = dummyZombie.Rotation;
                    activeZombie.SetActive(true);

                    var controller = activeZombie.GetComponent<CharacterController>();
                    if (controller != null)
                    {
                        controller.enabled = false;
                        controller.Move(Vector3.down * 0.01f);
                    }

                    var ai = activeZombie.GetComponent<ZombieAI>();
                    ai.Initialize(m_player, m_playerHealth, dummyZombie);

                    dummyZombie.ActiveZombieInstance = activeZombie;
                    dummyZombie.ZombieAI = ai;
                    dummyZombie.IsSwitchedToActive = true;
                    ++ActiveZombieCount;
                }
                else
                {
                    dummyZombie.IsPauseMovement = true;
                }
            }
        }
    }

    private void UpdateActiveZombies()
    {
        foreach (var zombie in _dummyZombies)
        {
            if (zombie.IsSwitchedToActive && zombie.ZombieAI != null)
            {
                zombie.ZombieAI.ManualUpdate();
            }
        }
    }

    private void UpdateDummyZombieMovement()
    {
        foreach (var zombie in _dummyZombies)
        {
            if (zombie.IsSwitchedToActive)
            {
                continue;
            }

            zombie.PrevPosition = zombie.Position;

            if (zombie.IsPauseMovement)
            {
                continue;
            }

            Vector3 dir = (m_player.position - zombie.Position).normalized;
            Quaternion rot = Quaternion.LookRotation(new Vector3(dir.x, 0, dir.z));

            Vector3 nextPos = Vector3.MoveTowards(zombie.Position, m_player.position, m_dummyChaseSpeed * Time.deltaTime);
            zombie.Position = SnapToGround(nextPos);

            zombie.Rotation = rot;

            RaycastHit hit;
            Vector3 rayOrigin = zombie.Position + Vector3.up * 1.5f;
            if (Physics.Raycast(rayOrigin, Vector3.down, out hit, 5f, m_groundLayer))
            {
                zombie.Up = hit.normal;
            }
            else
            {
                zombie.Up = Vector3.up;
            }
        }
    }

    private void RenderDummyZombies()
    {
        List<Matrix4x4> matrices = new List<Matrix4x4>();
        foreach (var zombie in _dummyZombies)
        {
            if (!zombie.IsSwitchedToActive)
            {
                matrices.Add(Matrix4x4.TRS(zombie.Position, Quaternion.LookRotation(zombie.Rotation * Vector3.forward, zombie.Up), Vector3.one));
            }
        }

        const int batchSize = 1023;
        for (int i = 0; i < matrices.Count; i += batchSize)
        {
            int count = Mathf.Min(batchSize, matrices.Count - i);
            Graphics.DrawMeshInstanced(m_dummyZombieMesh, 0, m_dummyZombieMaterial, matrices.GetRange(i, count));
        }
    }

    private GameObject GetFromPool()
    {
        return _activeZombiePool.Count > 0 ? _activeZombiePool.Dequeue() : Instantiate(m_activeZombiePrefab, m_zombieTransform);
    }

    public void ReturnToPool(GameObject zombie, DummyZombieData dummyZombieData = null)
    {
        zombie.SetActive(false);
        zombie.transform.position = Vector3.zero;
        zombie.transform.rotation = Quaternion.identity;
        zombie.transform.up = Vector3.up;

        if (dummyZombieData != null)
        {
            _dummyZombies.Remove(dummyZombieData);
        }
        _activeZombiePool.Enqueue(zombie);
    }

    public class DummyZombieData
    {
        public Vector3 Position;
        public Vector3 PrevPosition;
        public Quaternion Rotation;
        public Vector3 Up = Vector3.up;
        public bool IsSwitchedToActive = false;
        public GameObject ActiveZombieInstance;
        public ZombieAI ZombieAI;
        public bool IsPauseMovement = false;
    }
}
