using UnityEngine;

public class Bomb : MonoBehaviour
{
    private const string PrizeTag = "Prize";

    [Header("Fuse")]
    [SerializeField] private float fuseTime = 3f;

    [Header("Explosion")]
    [SerializeField] private float explosionRadius = 5f;
    [SerializeField] private float explosionForce = 10f;
    [SerializeField] private float upwardsModifier = 0.5f;
    [SerializeField] private float dissolveDuration = 2f;

    private void Start()
    {
        Invoke(nameof(Explode), fuseTime);
    }

    private void Explode()
    {
        foreach (Collider hit in Physics.OverlapSphere(transform.position, explosionRadius))
        {
            if (hit.gameObject == gameObject || !hit.CompareTag(PrizeTag))
            {
                continue;
            }

            if (hit.attachedRigidbody != null)
            {
                hit.attachedRigidbody.AddExplosionForce(explosionForce, transform.position, explosionRadius, upwardsModifier, ForceMode.Impulse);
            }

            PrizeDissolve dissolve = hit.GetComponent<PrizeDissolve>();
            dissolve ??= hit.gameObject.AddComponent<PrizeDissolve>();
            dissolve.BeginDissolve(dissolveDuration);
        }

        Destroy(gameObject);
    }
}
