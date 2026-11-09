using UnityEngine;
using System.Collections.Generic;

public class LiDARSpawner : MonoBehaviour
{
    public static LiDARSpawner Instance;

    public enum GameState { Idle, Scanning, Placed, Playing }
    public GameState currentState = GameState.Idle;

    [Header("Prefabs")]
    public GameObject notePrefabLeft;
    public GameObject notePrefabRight;
    public GameObject bombPrefab;
    [Tooltip("The REAL solid portal that appears when playing")]
    public GameObject portalPrefab;
    [Tooltip("The transparent preview prefab")]
    public GameObject hologramPrefab;

    [Header("Beat Saber Grid Settings")]
    public int columns = 3;
    public int rows = 3;
    public float spacing = 0.4f;

    [Header("Miss Zone Tracking")]
    public Transform missZoneWall;

    [Header("Automatic Audio Settings")]
    [HideInInspector] public AudioClip song;
    public float noteSpeed = 1.0f;

    [Range(0.001f, 1.0f)]
    public float beatThreshold = 0.2f;
    public float minimumBeatInterval = 0.3f;

    [Range(0f, 1f)]
    public float bombProbability = 0.05f;

    [Header("AR Settings")]
    public float minimumSpawnDistance = 1.5f;
    public float roughnessTolerance = 0.2f;
    public float edgePadding = 0.3f;
    public LayerMask meshLayer;

    private Transform[,] gridPoints;
    private GameObject[,] activePortals;
    private bool isPreviewActive = false;
    private bool hasFoundValidSpot = false;

    private float initialTravelTime;
    private Plane gridPlane;
    private Queue<float> scheduledBeats = new Queue<float>();

    private float lastBeatDetectedTime = 0f;
    private int lastSpawnCol = -1;
    private int lastSpawnRow = -1;

    private AudioSource analyzerSource;
    private AudioSource playerSource;
    private float[] spectrumData = new float[1024];
    private double dspStartTime;

    private List<GameObject> activeCubes = new List<GameObject>();
    private GameObject musicEmitter;
    private Vector3 gridCenterPosition;

    public int totalNotesSpawned = 0;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        analyzerSource = gameObject.AddComponent<AudioSource>();
        analyzerSource.volume = 0.001f;
        analyzerSource.playOnAwake = false;
        analyzerSource.spatialBlend = 0f;
    }

    public void BeginScanning(AudioClip selectedSong)
    {
        song = selectedSong;
        analyzerSource.clip = song;

        hasFoundValidSpot = false;

        InitializeGridObjects();
        currentState = GameState.Scanning;
    }

    void Update()
    {
        if (currentState == GameState.Scanning)
        {
            HandleARScanning();
        }
        else if (currentState == GameState.Playing)
        {
            if (!analyzerSource.isPlaying && AudioSettings.dspTime > dspStartTime + 1.0f)
            {
                currentState = GameState.Placed;
                if (UIManager.Instance != null) UIManager.Instance.TriggerGameOver(true);
                return;
            }

            AnalyzeAudio();
            ProcessNoteQueue();
            UpdateAudioPosition();

            if (missZoneWall != null)
            {
                float playerDepth = gridPlane.GetDistanceToPoint(Camera.main.transform.position);
                missZoneWall.position = gridCenterPosition + (gridPlane.normal * (playerDepth + 1.5f));
                missZoneWall.rotation = Quaternion.LookRotation(gridPlane.normal);
            }
        }
    }

    void HandleARScanning()
    {
        Transform player = Camera.main.transform;
        Vector3 levelForward = new Vector3(player.forward.x, 0f, player.forward.z).normalized;

        if (Physics.Raycast(player.position, levelForward, out RaycastHit hit, 10.0f, meshLayer))
        {
            if (hit.distance > minimumSpawnDistance && Mathf.Abs(hit.normal.y) < 0.3f)
            {
                if (TryFindFlatSurface(hit.point, hit.normal, out Vector3 trueCenter, out Vector3 trueNormal))
                {
                    hasFoundValidSpot = true;

                    TogglePreviewVisibility(true);
                    UpdateGridPosition(trueCenter, trueNormal);
                    return;
                }
            }
        }

        if (!hasFoundValidSpot)
        {
            TogglePreviewVisibility(false);
        }
    }

    public void ConfirmGridPlacement()
    {
        if (currentState == GameState.Scanning && hasFoundValidSpot)
        {
            LockGridPlacement(Camera.main.transform.position);
        }
    }

    void LockGridPlacement(Vector3 playerPos)
    {
        currentState = GameState.Placed;
        initialTravelTime = gridPlane.GetDistanceToPoint(playerPos) / noteSpeed;

        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                Destroy(activePortals[x, y]);
                activePortals[x, y] = Instantiate(portalPrefab, gridPoints[x, y].position, gridPoints[x, y].rotation);
            }
        }

        if (UIManager.Instance != null) UIManager.Instance.StartPlaying();

        StartMusic();
    }

    void InitializeGridObjects()
    {
        gridPoints = new Transform[columns, rows];
        activePortals = new GameObject[columns, rows];

        musicEmitter = new GameObject("MusicEmitter_3D");
        playerSource = musicEmitter.AddComponent<AudioSource>();
        playerSource.playOnAwake = false;
        playerSource.spatialBlend = 1.0f;
        playerSource.dopplerLevel = 0f;
        playerSource.volume = 1.0f;
        playerSource.rolloffMode = AudioRolloffMode.Linear;
        playerSource.minDistance = 10.0f;
        playerSource.maxDistance = 50.0f;

        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                GameObject portal = Instantiate(hologramPrefab, Vector3.zero, Quaternion.identity);
                activePortals[x, y] = portal;

                GameObject pointRef = new GameObject($"SpawnPoint_Col{x}_Row{y}");
                gridPoints[x, y] = pointRef.transform;

                portal.SetActive(false);
            }
        }
    }

    void UpdateGridPosition(Vector3 centerPoint, Vector3 normal)
    {
        gridCenterPosition = centerPoint;
        gridPlane = new Plane(normal, centerPoint);

        Quaternion rotation = Quaternion.LookRotation(normal);
        Vector3 right = rotation * Vector3.right;
        Vector3 up = rotation * Vector3.up;

        Vector3 startPos = centerPoint
                           - (right * (columns - 1) * spacing * 0.5f)
                           - (up * (rows - 1) * spacing * 0.5f);

        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                Vector3 spawnPos = startPos + (right * x * spacing) + (up * y * spacing);

                activePortals[x, y].transform.position = spawnPos;
                activePortals[x, y].transform.rotation = rotation;

                gridPoints[x, y].position = spawnPos;
                gridPoints[x, y].rotation = rotation;
            }
        }

        musicEmitter.transform.position = centerPoint;
    }

    void TogglePreviewVisibility(bool isVisible)
    {
        if (isPreviewActive == isVisible) return;

        isPreviewActive = isVisible;
        for (int x = 0; x < columns; x++)
        {
            for (int y = 0; y < rows; y++)
            {
                if (activePortals[x, y] != null) activePortals[x, y].SetActive(isVisible);
            }
        }
    }

    bool TryFindFlatSurface(Vector3 initialCenter, Vector3 bumpyNormal, out Vector3 trueCenter, out Vector3 trueNormal)
    {
        trueCenter = initialCenter;
        trueNormal = bumpyNormal;

        Quaternion roughRot = Quaternion.LookRotation(bumpyNormal);
        Vector3 right = roughRot * Vector3.right;
        Vector3 up = roughRot * Vector3.up;

        float checkWidth = (((columns - 1) * spacing) / 2f) + edgePadding;
        float checkHeight = (((rows - 1) * spacing) / 2f) + edgePadding;

        Vector3[] probes = {
            initialCenter + (right * checkWidth) + (up * checkHeight),
            initialCenter - (right * checkWidth) + (up * checkHeight),
            initialCenter + (right * checkWidth) - (up * checkHeight),
            initialCenter - (right * checkWidth) - (up * checkHeight)
        };

        Vector3[] validHits = new Vector3[4];
        for (int i = 0; i < 4; i++)
        {
            Vector3 rayStart = probes[i] + (bumpyNormal * 0.5f);
            if (Physics.Raycast(rayStart, -bumpyNormal, out RaycastHit probeHit, 1.0f, meshLayer))
                validHits[i] = probeHit.point;
            else return false;
        }

        Vector3 diagonal1 = validHits[0] - validHits[3];
        Vector3 diagonal2 = validHits[1] - validHits[2];
        Vector3 calculatedNormal = Vector3.Cross(diagonal1, diagonal2).normalized;

        if (Vector3.Dot(calculatedNormal, bumpyNormal) < 0) calculatedNormal = -calculatedNormal;

        Vector3 calculatedCenter = (validHits[0] + validHits[1] + validHits[2] + validHits[3]) / 4f;

        Plane smoothPlane = new Plane(calculatedNormal, calculatedCenter);
        foreach (Vector3 hitPoint in validHits)
        {
            if (Mathf.Abs(smoothPlane.GetDistanceToPoint(hitPoint)) > roughnessTolerance) return false;
        }

        trueCenter = calculatedCenter;
        trueNormal = calculatedNormal;
        return true;
    }

    public void StartMusic()
    {
        lastBeatDetectedTime = Time.time;
        dspStartTime = AudioSettings.dspTime;

        playerSource.clip = song;

        analyzerSource.PlayScheduled(dspStartTime);
        playerSource.PlayScheduled(dspStartTime + initialTravelTime);

        currentState = GameState.Playing;
    }

    void AnalyzeAudio()
    {
        if (AudioSettings.dspTime < dspStartTime + 0.5f) return;

        analyzerSource.GetSpectrumData(spectrumData, 0, FFTWindow.BlackmanHarris);

        float currentBassLevel = 0f;
        for (int i = 1; i <= 5; i++)
        {
            currentBassLevel += (spectrumData[i] * 500f);
        }

        if (currentBassLevel > beatThreshold && Time.time > lastBeatDetectedTime + minimumBeatInterval)
        {
            ScheduleNote();
        }
    }

    void ScheduleNote()
    {
        scheduledBeats.Enqueue(Time.time + initialTravelTime);
        lastBeatDetectedTime = Time.time;
    }

    void ProcessNoteQueue()
    {
        if (scheduledBeats.Count == 0) return;

        Transform player = Camera.main.transform;
        float currentDistance = gridPlane.GetDistanceToPoint(player.position);
        if (currentDistance < 0.1f) currentDistance = 0.1f;

        float currentTravelTime = currentDistance / noteSpeed;

        while (scheduledBeats.Count > 0)
        {
            float targetHitTime = scheduledBeats.Peek();

            if (Time.time + currentTravelTime >= targetHitTime)
            {
                scheduledBeats.Dequeue();
                SpawnFlowNote();
            }
            else break;
        }
    }

    void UpdateAudioPosition()
    {
        if (musicEmitter == null) return;

        activeCubes.RemoveAll(cube => cube == null);
        Vector3 targetPosition = gridCenterPosition;
        Transform player = Camera.main.transform;

        float playerDistanceFromWall = gridPlane.GetDistanceToPoint(player.position);
        GameObject leadingCube = null;
        float maxCubeDistance = -Mathf.Infinity;

        foreach (GameObject cube in activeCubes)
        {
            float cubeDistanceFromWall = gridPlane.GetDistanceToPoint(cube.transform.position);
            if (cubeDistanceFromWall < playerDistanceFromWall)
            {
                if (cubeDistanceFromWall > maxCubeDistance)
                {
                    maxCubeDistance = cubeDistanceFromWall;
                    leadingCube = cube;
                }
            }
        }

        if (leadingCube != null) targetPosition = leadingCube.transform.position;
        musicEmitter.transform.position = Vector3.Lerp(musicEmitter.transform.position, targetPosition, Time.deltaTime * 4f);
    }

    void SpawnFlowNote()
    {
        int col = 0; int row = 0;
        bool isBomb = Random.value < bombProbability && bombPrefab != null;

        if (isBomb && lastSpawnCol != -1)
        {
            int tries = 0;
            while (tries < 4)
            {
                col = Random.Range(0, columns);
                row = Random.Range(0, rows);
                if (col != lastSpawnCol || row != lastSpawnRow) break;
                tries++;
            }
        }
        else
        {
            if (lastSpawnCol == -1)
            {
                col = Random.Range(0, columns);
                row = Random.Range(0, rows);
            }
            else
            {
                do
                {
                    col = Random.Range(0, columns);
                    row = Random.Range(0, rows);
                } while (col == lastSpawnCol && row == lastSpawnRow);
            }
        }

        lastSpawnCol = col;
        lastSpawnRow = row;

        Transform spawnPoint = gridPoints[col, row];
        GameObject toSpawn = isBomb ? bombPrefab : ((Random.value < 0.5f) ? notePrefabLeft : notePrefabRight);

        if (toSpawn == null) return;

        GameObject instance = Instantiate(toSpawn, spawnPoint.position, spawnPoint.rotation);
        activeCubes.Add(instance);

        NoteBehavior nb = instance.GetComponent<NoteBehavior>();
        if (nb != null) nb.speed = noteSpeed;

        totalNotesSpawned++;
    }

    void OnDestroy()
    {
        if (musicEmitter != null) Destroy(musicEmitter);
    }
}
