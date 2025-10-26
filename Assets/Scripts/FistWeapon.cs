using UnityEngine;

public class FistWeapon : MonoBehaviour
{
    [Header("References")]
    public Camera cam;
    public Animator animator;
    public AudioSource audiosource;
    public AudioClip PunchClip;

    [Header("Timing")]
    public float cooldown = 0.5f;
    public float AnimSpeed = 1.0f;

    [Header("Input")]
    public bool UseLegacyInput = true;
    public KeyCode PunchKey = KeyCode.Mouse0;

    [Header("Hit Detection")]
    public float range = 1.8f;
    public float radius = 0.25f;
    [Tooltip("Layers that can be hit (e.g., Enemy). Do NOT include Player.")]
    public LayerMask HittableMask;
    [Tooltip("Start the spherecast a bit in front of the camera to avoid self hits.")]
    public float originForwardOffset = 0.15f;

    [Header("Damage and Force")]
    public float damage = 12f;
    public float KnockbackForce = 3f;

    [Header("SFX")]
    public AudioClip HitSound;
    public GameObject HitEffect;

    float NextReadyTime;

    void Awake()
    {
        if (!cam) cam = Camera.main;
        if (!animator) animator = GetComponent<Animator>();
        if (!audiosource) audiosource = GetComponent<AudioSource>();
        if (animator) animator.speed = AnimSpeed;
    }

    void Update()
    {
        if (UseLegacyInput && Input.GetKeyDown(PunchKey))
            TryPunch();
    }

    public void TryPunch()
    {
        if (Time.time < NextReadyTime || !animator) return;

        animator.Play("PunchAnim", 0, 0f); // ensure from start
        if (audiosource && PunchClip) audiosource.PlayOneShot(PunchClip);
        NextReadyTime = Time.time + cooldown;
    }

    // Called by Animation Event at the impact frame
    public void PunchActivate()
    {
        if (!cam) cam = Camera.main;

        Vector3 origin = cam.transform.position + cam.transform.forward * originForwardOffset;
        Vector3 dir = cam.transform.forward;

        var hits = Physics.SphereCastAll(origin, radius, dir, range, HittableMask, QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0) return;

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        foreach (var h in hits)
        {
            // Skip punching yourself if your player has a rigidbody somewhere
            if (h.collider.attachedRigidbody && h.collider.attachedRigidbody.gameObject == gameObject)
                continue;

            // 1) Preferred: hit DummyEnemy directly
            var dummy = h.collider.GetComponentInParent<DummyEnemy>();
            if (dummy != null)
            {
                dummy.TakeDamage(Mathf.CeilToInt(damage), cam.transform.position);

                if (h.rigidbody) h.rigidbody.AddForce(dir * KnockbackForce, ForceMode.VelocityChange);
                if (audiosource && HitSound) audiosource.PlayOneShot(HitSound);
                if (HitEffect) Destroy(Instantiate(HitEffect, h.point, Quaternion.LookRotation(h.normal)), 2f);
                break; // one target per punch
            }

            // 2) If you also use IDamageable elsewhere, keep this as a fallback:
            var dmg = h.collider.GetComponentInParent<IDamageable>();
            if (dmg != null)
            {
                DamageInfo info = new DamageInfo
                {
                    Amount = damage,
                    Point = h.point,
                    Normal = h.normal,
                    Source = cam ? cam.gameObject : gameObject,
                    Type = DamageType.Melee,
                    Crit = false
                };

                dmg.TakeDamage(info);

                if (h.rigidbody) h.rigidbody.AddForce(dir * KnockbackForce, ForceMode.VelocityChange);
                if (audiosource && HitSound) audiosource.PlayOneShot(HitSound);
                if (HitEffect) Destroy(Instantiate(HitEffect, h.point, Quaternion.LookRotation(h.normal)), 2f);
                break;
            }
        }
    }



#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!cam) return;
        Gizmos.matrix = Matrix4x4.identity;

        Vector3 origin = cam.transform.position + cam.transform.forward * originForwardOffset;
        Vector3 end = origin + cam.transform.forward * range;

        // Draw start & end spheres
        UnityEditor.Handles.color = Color.yellow;
        UnityEditor.Handles.DrawWireDisc(origin, cam.transform.up, radius);
        UnityEditor.Handles.DrawWireDisc(end, cam.transform.up, radius);
        // Draw line
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(origin, end);
    }
#endif
}
