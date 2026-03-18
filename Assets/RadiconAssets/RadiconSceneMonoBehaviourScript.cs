using UnityEngine;
using UnityEngine.SceneManagement;

public class RadiconSceneMonoBehaviourScript : MonoBehaviour {
    [Header("Scene")]
    // targetSceneName
    [SerializeField] private string targetSceneName = "RadiconScene";
    // createOnStart
    [SerializeField] private bool createOnStart = true;

    [Header("Direct Object Assignments (Scene object or Prefab)")]
    // floorObject
    [SerializeField] private GameObject floorObject;
    // playerObject
    [SerializeField] private GameObject playerObject;
    // radiconObject
    [SerializeField] private GameObject radiconObject;
    // wallObjects
    [SerializeField] private GameObject[] wallObjects;

    [Header("Additional Attach Objects")]
    // enemyObject
    [SerializeField] private GameObject enemyObject;
    // keyObject
    [SerializeField] private GameObject keyObject;
    // doorObject
    [SerializeField] private GameObject doorObject;
    // closeDoorObject
    [SerializeField] private GameObject closeDoorObject;
    // blockObject
    [SerializeField] private GameObject blockObject;
    // candleObject
    [SerializeField] private GameObject candleObject;
    // extraSceneObjects
    [SerializeField] private GameObject[] extraSceneObjects;

    [Header("Spawn Points")]
    // floorSpawnPoint
    [SerializeField] private Transform floorSpawnPoint;
    // playerSpawnPoint
    [SerializeField] private Transform playerSpawnPoint;
    // radiconSpawnPoint
    [SerializeField] private Transform radiconSpawnPoint;

    [Header("Wall Spawn Points (optional)")]
    // frontWallSpawnPoint
    [SerializeField] private Transform frontWallSpawnPoint;
    // backWallSpawnPoint
    [SerializeField] private Transform backWallSpawnPoint;
    // leftWallSpawnPoint
    [SerializeField] private Transform leftWallSpawnPoint;
    // rightWallSpawnPoint
    [SerializeField] private Transform rightWallSpawnPoint;

    [Header("Fallback Names")]
    // generatedFloorName
    [SerializeField] private string generatedFloorName = "Floor Generated";
    // generatedPlayerName
    [SerializeField] private string generatedPlayerName = "Player Generated";
    // generatedRadiconName
    [SerializeField] private string generatedRadiconName = "Radicon Generated";
    // generatedWallNamePrefix
    [SerializeField] private string generatedWallNamePrefix = "Wall Generated";

    // bootstrapRegistered
    private static bool bootstrapRegistered;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    // 登録bootstrapper
    // RegisterBootstrapper の処理
    private static void RegisterBootstrapper () {
        if (bootstrapRegistered) {
            return;
            }

        bootstrapRegistered = true;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        }

    // handleシーン読み込み済み
    // HandleSceneLoaded の処理
    private static void HandleSceneLoaded (Scene scene, LoadSceneMode mode) {
        if (!scene.IsValid() || !scene.isLoaded) {
            return;
            }

        if (FindAnyObjectByType<RadiconSceneMonoBehaviourScript>() != null) {
            return;
            }

        GameObject bootstrapperObject = new GameObject("RadiconSceneBootstrapper");
        DontDestroyOnLoad(bootstrapperObject);
        RadiconSceneMonoBehaviourScript bootstrapper = bootstrapperObject.AddComponent<RadiconSceneMonoBehaviourScript>();
        bootstrapper.TryCreateRadiconSceneObjects(scene);
        }

    // シーン開始時の初期化
    private void Start () {
        if (!createOnStart) {
            return;
            }

        TryCreateRadiconSceneObjects(SceneManager.GetActiveScene());
        }

    // 試行生成ラジコンシーンobjectsを試行
    // TryCreateRadiconSceneObjects の処理
    private void TryCreateRadiconSceneObjects (Scene scene) {
        if (!ShouldCreateInScene(scene)) {
            return;
            }

        TryCreateFloor();
        TryCreateWalls();
        TryCreatePlayer();
        TryCreateRadicon();
        TryEnsureAttachObjects();
        }

    // should生成inシーン
    // ShouldCreateInScene の処理
    private bool ShouldCreateInScene (Scene scene) {
        if (!scene.IsValid() || !scene.isLoaded) {
            return false;
            }

        return string.IsNullOrWhiteSpace(targetSceneName) || scene.name == targetSceneName;
        }

    // 試行確保attachobjectsを試行
    // TryEnsureAttachObjects の処理
    private void TryEnsureAttachObjects () {
        enemyObject = EnsureAttachedObject(enemyObject, "EnemyPrefab");
        keyObject = EnsureAttachedObject(keyObject, "KagiPrefab");
        doorObject = EnsureAttachedObject(doorObject, "DoorPrefab");
        closeDoorObject = EnsureAttachedObject(closeDoorObject, "CloseDoorPrefab");
        blockObject = EnsureAttachedObject(blockObject, "BlockPrefab");
        candleObject = EnsureAttachedObject(candleObject, "rousokuPrefab");

        if (extraSceneObjects == null) {
            return;
            }

        for (int i = 0; i < extraSceneObjects.Length; i++) {
            extraSceneObjects[i] = EnsureAttachedObject(extraSceneObjects[i], string.Empty);
            }
        }

    // EnsureAttachedObject の処理
    private GameObject EnsureAttachedObject (GameObject target, string sceneNameFallback) {
        if (target != null && target.scene.IsValid()) {
            target.SetActive(true);
            return target;
            }

        if (target != null) {
            GameObject instance = Instantiate(target);
            instance.name = target.name;
            return instance;
            }

        if (string.IsNullOrWhiteSpace(sceneNameFallback)) {
            return null;
            }

        GameObject existing = GameObject.Find(sceneNameFallback);
        if (existing != null) {
            existing.SetActive(true);
            return existing;
            }

        return null;
        }

    // 試行生成床を試行
    // TryCreateFloor の処理
    private void TryCreateFloor () {
        if (( floorObject != null && floorObject.scene.IsValid() ) || GameObject.Find("floor") != null || GameObject.Find(generatedFloorName) != null) {
            return;
            }

        floorObject = ResolveOrInstantiate(floorObject, generatedFloorName, floorSpawnPoint, new Vector3(12f, 1f, 12f), PrimitiveType.Cube);
        Rigidbody floorBody = floorObject.GetComponent<Rigidbody>();
        if (floorBody == null) {
            floorBody = floorObject.AddComponent<Rigidbody>();
            }

        floorBody.useGravity = false;
        floorBody.isKinematic = true;
        }

    // 試行生成wallsを試行
    // TryCreateWalls の処理
    private void TryCreateWalls () {
        if (FindAnyObjectByType<WallMarker>() != null || GameObject.Find("WallPrefab") != null) {
            return;
            }

        EnsureWallArray();

        Transform[] spawnPoints = {
            frontWallSpawnPoint,
            backWallSpawnPoint,
            leftWallSpawnPoint,
            rightWallSpawnPoint,
        };

        Vector3[] fallbackPositions = {
            new Vector3(0f, 1f, 6f),
            new Vector3(0f, 1f, -6f),
            new Vector3(-6f, 1f, 0f),
            new Vector3(6f, 1f, 0f),
        };

        Vector3[] fallbackScales = {
            new Vector3(12f, 2f, 1f),
            new Vector3(12f, 2f, 1f),
            new Vector3(1f, 2f, 12f),
            new Vector3(1f, 2f, 12f),
        };

        for (int i = 0; i < wallObjects.Length; i++) {
            string wallName = $"{generatedWallNamePrefix} {i + 1}";
            wallObjects[i] = ResolveOrInstantiate(wallObjects[i], wallName, spawnPoints[i], fallbackScales[i], PrimitiveType.Cube);

            if (spawnPoints[i] == null) {
                wallObjects[i].transform.position = fallbackPositions[i];
                }

            if (wallObjects[i].GetComponent<WallMarker>() == null) {
                wallObjects[i].AddComponent<WallMarker>();
                }

            Rigidbody wallBody = wallObjects[i].GetComponent<Rigidbody>();
            if (wallBody != null) {
                wallBody.isKinematic = true;
                wallBody.useGravity = false;
                }
            }
        }

    // EnsureWallArray の処理
    private void EnsureWallArray () {
        if (wallObjects == null || wallObjects.Length != 4) {
            wallObjects = new GameObject[4];
            }
        }

    // 試行生成プレイヤーを試行
    // TryCreatePlayer の処理
    private void TryCreatePlayer () {
        if (FindAnyObjectByType<PlsyerRadiconMonoBehaviourScript>() != null) {
            return;
            }

        playerObject = ResolveOrInstantiate(playerObject, generatedPlayerName, playerSpawnPoint, Vector3.one, PrimitiveType.Capsule);

        CharacterController characterController = playerObject.GetComponent<CharacterController>();
        if (characterController == null) {
            characterController = playerObject.AddComponent<CharacterController>();
            characterController.radius = 0.35f;
            characterController.height = 1.8f;
            }

        PlsyerRadiconMonoBehaviourScript playerController = playerObject.GetComponent<PlsyerRadiconMonoBehaviourScript>();
        if (playerController == null) {
            playerController = playerObject.AddComponent<PlsyerRadiconMonoBehaviourScript>();
            }

        EnsurePlayerCamera(playerObject.transform, playerController);
        }

    // 試行生成ラジコンを試行
    // TryCreateRadicon の処理
    private void TryCreateRadicon () {
        if (FindAnyObjectByType<RadiconMonoBehaviourScript>() != null) {
            return;
            }

        radiconObject = ResolveOrInstantiate(radiconObject, generatedRadiconName, radiconSpawnPoint, new Vector3(1f, 0.5f, 1f), PrimitiveType.Cube);

        Rigidbody rigidbody = radiconObject.GetComponent<Rigidbody>();
        if (rigidbody == null) {
            rigidbody = radiconObject.AddComponent<Rigidbody>();
            }

        rigidbody.useGravity = true;

        CapsuleCollider capsuleCollider = radiconObject.GetComponent<CapsuleCollider>();
        if (capsuleCollider == null) {
            BoxCollider boxCollider = radiconObject.GetComponent<BoxCollider>();
            if (boxCollider != null) {
                Destroy(boxCollider);
                }

            capsuleCollider = radiconObject.AddComponent<CapsuleCollider>();
            }

        if (radiconObject.GetComponent<RadiconMonoBehaviourScript>() == null) {
            radiconObject.AddComponent<RadiconMonoBehaviourScript>();
            }

        if (radiconObject.transform.Find("GroundCheck") == null) {
            CreateGroundCheck(radiconObject.transform);
            }
        }

    // ResolveOrInstantiate の処理
    private GameObject ResolveOrInstantiate (GameObject source, string fallbackName, Transform spawnPoint, Vector3 fallbackScale, PrimitiveType primitiveType) {
        if (source == null) {
            GameObject generated = GameObject.CreatePrimitive(primitiveType);
            generated.name = fallbackName;
            ApplySpawn(generated.transform, spawnPoint, fallbackScale);
            return generated;
            }

        if (source.scene.IsValid()) {
            source.SetActive(true);
            ApplySpawn(source.transform, spawnPoint, source.transform.localScale == Vector3.zero ? fallbackScale : source.transform.localScale);
            return source;
            }

        GameObject instance = Instantiate(source);
        instance.name = source.name;
        ApplySpawn(instance.transform, spawnPoint, instance.transform.localScale == Vector3.zero ? fallbackScale : instance.transform.localScale);
        return instance;
        }

    // ApplySpawn の処理
    private void ApplySpawn (Transform target, Transform spawnPoint, Vector3 fallbackScale) {
        if (spawnPoint != null) {
            target.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
            }

        if (target.localScale == Vector3.zero) {
            target.localScale = fallbackScale;
            }
        }

    // EnsurePlayerCamera の処理
    private void EnsurePlayerCamera (Transform playerTransform, PlsyerRadiconMonoBehaviourScript playerController) {
        Transform playerCameraTransform = playerTransform.Find("PlayerCamera");
        GameObject cameraObject = playerCameraTransform == null ? new GameObject("PlayerCamera") : playerCameraTransform.gameObject;

        cameraObject.transform.SetParent(playerTransform, false);
        cameraObject.transform.localPosition = new Vector3(0f, 0.7f, 0f);
        cameraObject.tag = "MainCamera";

        if (cameraObject.GetComponent<Camera>() == null) {
            cameraObject.AddComponent<Camera>();
            }

        if (FindAnyObjectByType<AudioListener>() == null && cameraObject.GetComponent<AudioListener>() == null) {
            cameraObject.AddComponent<AudioListener>();
            }

        playerController.viewPivot = cameraObject.transform;
        }

    // CreateGroundCheck の処理
    private void CreateGroundCheck (Transform parent) {
        GameObject groundCheck = new GameObject("GroundCheck");
        groundCheck.transform.SetParent(parent, false);
        groundCheck.transform.localPosition = Vector3.zero;
        }
    }

public class WallMarker : MonoBehaviour {
    }