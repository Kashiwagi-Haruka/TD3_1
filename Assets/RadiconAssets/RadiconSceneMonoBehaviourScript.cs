using UnityEngine;
using UnityEngine.SceneManagement;

public class RadiconSceneMonoBehaviourScript : MonoBehaviour {
    [Header("Generated Radicon")]
    [SerializeField] private string generatedObjectName = "Radicon Generated";
    [SerializeField] private Vector3 spawnPosition = new Vector3(1.72f, 0.23f, -2.2f);
    [SerializeField] private Vector3 spawnEulerAngles = new Vector3(90f, 0f, 0f);
    [SerializeField] private Vector3 spawnScale = new Vector3(1f, 0.5f, 1f);
    [SerializeField] private Material radiconMaterial;

    [Header("Collider")]
    [SerializeField] private float colliderRadius = 0.5f;
    [SerializeField] private float colliderHeight = 2f;

    [Header("Behavior")]
    [SerializeField] private bool createOnStart = true;

    private static bool bootstrapRegistered;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RegisterBootstrapper () {
        if (bootstrapRegistered) {
            return;
            }

        bootstrapRegistered = true;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        }

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
        bootstrapper.TryCreateRadicon();
        }

    private void Start () {
        if (!createOnStart) {
            return;
            }

        TryCreateRadicon();
        }

    private void TryCreateRadicon () {
        if (FindAnyObjectByType<RadiconMonoBehaviourScript>() != null) {
            return;
            }

        GameObject radicon = GameObject.CreatePrimitive(PrimitiveType.Cube);
        radicon.name = generatedObjectName;
        radicon.transform.SetPositionAndRotation(spawnPosition, Quaternion.Euler(spawnEulerAngles));
        radicon.transform.localScale = spawnScale;

        MeshRenderer meshRenderer = radicon.GetComponent<MeshRenderer>();
        if (meshRenderer != null && radiconMaterial != null) {
            meshRenderer.sharedMaterial = radiconMaterial;
            }

        Rigidbody rigidbody = radicon.AddComponent<Rigidbody>();
        rigidbody.useGravity = true;

        BoxCollider existingBoxCollider = radicon.GetComponent<BoxCollider>();
        if (existingBoxCollider != null) {
            Destroy(existingBoxCollider);
            }

        CapsuleCollider capsuleCollider = radicon.AddComponent<CapsuleCollider>();
        capsuleCollider.radius = colliderRadius;
        capsuleCollider.height = colliderHeight;

        radicon.AddComponent<RadiconMonoBehaviourScript>();
        CreateGroundCheck(radicon.transform);
        }

    private void CreateGroundCheck (Transform parent) {
        GameObject groundCheck = new GameObject("GroundCheck");
        groundCheck.transform.SetParent(parent, false);
        groundCheck.transform.localPosition = Vector3.zero;
        }
    }