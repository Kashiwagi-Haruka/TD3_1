using UnityEngine;

public class BlockBehaviourScript : MonoBehaviour {
    public static bool TryGetTouchingBlock (
        Transform actor,
        float interactionHeightOffset,
        float blockCheckRange,
        out Collider blockCollider,
        out Vector3 hitPoint
    ) {
        Vector3 center = actor.position + Vector3.up * interactionHeightOffset;
        Collider[] nearbyColliders = Physics.OverlapSphere(center, blockCheckRange, ~0, QueryTriggerInteraction.Ignore);

        foreach (Collider nearbyCollider in nearbyColliders) {
            if (!nearbyCollider.gameObject.name.Contains("Block")) {
                continue;
                }

            blockCollider = nearbyCollider;
            hitPoint = nearbyCollider.ClosestPoint(center);
            return true;
            }

        blockCollider = null;
        hitPoint = center + actor.forward * 0.2f;
        return false;
        }

    public static void ConsumeBlock (Collider blockCollider, Vector3 position, float particleLifetime, float particleSpeed, int particleBurstCount) {
        if (blockCollider == null) {
            return;
            }

        Destroy(blockCollider.gameObject);
        SpawnRedParticles(position, particleLifetime, particleSpeed, particleBurstCount);
        }

    private static void SpawnRedParticles (Vector3 position, float particleLifetime, float particleSpeed, int particleBurstCount) {
        GameObject particleObject = new GameObject("RedInteractionParticles");
        particleObject.transform.position = position;

        ParticleSystem particleSystem = particleObject.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particleSystem.main;
        main.startColor = Color.red;
        main.startLifetime = particleLifetime;
        main.startSpeed = particleSpeed;
        main.startSize = 0.12f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = particleBurstCount;

        ParticleSystem.EmissionModule emission = particleSystem.emission;
        emission.rateOverTime = 0f;

        ParticleSystem.ShapeModule shape = particleSystem.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.08f;

        particleSystem.Emit(particleBurstCount);
        Destroy(particleObject, particleLifetime + 0.4f);
        }
    }