using System.Collections;
using UnityEngine;

/// <summary>
/// Dummy enemy that sits still, faces a target, plays 8-direction walk loops,
/// shows a directional hurt sprite when damaged, and plays a non-directional
/// death animation that freezes on the last frame. Uses Animator.
/// </summary>
[RequireComponent(typeof(Animator))]
[DisallowMultipleComponent]
public class DummyEnemy : MonoBehaviour
{
    [Header("References")]
    public Animator animator;
    public Transform facingTarget;          // e.g., your Player transform
    public AudioSource audioSource;
    public AudioClip hurtSfx;
    public AudioClip deathSfx;

    [Header("Health")]
    public int maxHealth = 3;
    public float hurtLockDuration = 0.15f;  // time to lock hurt direction
    public float postHurtInvuln = 0.05f;    // tiny grace to prevent double-hits in same frame

    [Header("Facing")]
    public bool faceTargetContinuously = true; // if true, keeps rotating "direction" toward target while alive
    public bool drawDebugSectors = false;

    // Animator parameter names (configure your controller to use these)
    const string ParamDirIndex = "dirIndex"; // int 0..7 (E,NE,N,NW,W,SW,S,SE)
    const string ParamHurtTrig = "Hurt";     // trigger
    const string ParamDeadBool = "Dead";     // bool

    int _health;
    bool _dead;
    bool _inHurtLock;
    int _lockedDirIndex = 2; // default N
    float _invulnUntil;
    int _currentDirIndex = 2;

    void Reset()
    {
        animator = GetComponent<Animator>();
        // Try to find an AudioSource on this object
        if (!audioSource) audioSource = GetComponent<AudioSource>();
    }

    void Awake()
    {
        if (!animator) animator = GetComponent<Animator>();
        _health = Mathf.Max(1, maxHealth);
    }

    void Update()
    {
        if (_dead) return;

        // Compute facing direction
        if (_inHurtLock)
        {
            // Hold the direction chosen at the moment of hurt
            SetDir(_lockedDirIndex);
        }
        else if (faceTargetContinuously && facingTarget)
        {
            int di = WorldDirToIndex(facingTarget.position - transform.position);
            SetDir(di);
        }
        // else: keep whatever dir we had last (still playing its loop)
    }

    /// <summary>
    /// Public damage entry. Pass the world position the hit came FROM (e.g., the attacker).
    /// </summary>
    public void TakeDamage(int amount, Vector3 hitFromWorldPos)
    {
        if (_dead) return;
        if (Time.time < _invulnUntil) return;

        _health -= Mathf.Max(1, amount);
        int dirFromHit = WorldDirToIndex(transform.position - hitFromWorldPos); // incoming vector
        PlayHurt(dirFromHit);

        if (_health <= 0)
        {
            Die();
        }
        else
        {
            _invulnUntil = Time.time + postHurtInvuln;
        }
    }

    /// <summary>
    /// Convenience overload for melee colliders that only know the attacker transform.
    /// </summary>
    public void TakeDamageFrom(Transform attacker, int amount = 1)
    {
        if (!attacker) return;
        TakeDamage(amount, attacker.position);
    }

    void PlayHurt(int dirIndex)
    {
        // Lock facing during the brief hurt display so the correct single-frame shows.
        _lockedDirIndex = Mathf.Clamp(dirIndex, 0, 7);
        _inHurtLock = true;

        SetDir(_lockedDirIndex);

        // Trigger Animator "Hurt"
        animator.ResetTrigger(ParamHurtTrig);
        animator.SetTrigger(ParamHurtTrig);

        // SFX
        if (audioSource && hurtSfx) audioSource.PlayOneShot(hurtSfx);

        // Unlock after a short moment so the walk resumes.
        StopCoroutine(nameof(Co_EndHurtLock));
        StartCoroutine(Co_EndHurtLock(hurtLockDuration));
    }

    IEnumerator Co_EndHurtLock(float t)
    {
        yield return new WaitForSeconds(t);
        _inHurtLock = false;
    }

    void Die()
    {
        if (_dead) return;
        _dead = true;

        const string DeathStateName = "Death";

        // Gate everything else out first
        animator.ResetTrigger("Hurt");
        animator.SetBool(ParamDeadBool, true);

        // Optional: make sure no Walk transition conditions match, even without the gate
        animator.SetInteger("dirIndex", -999);

        // Hard-jump into Death from current pose at t=0 (no blend)
        animator.Play(DeathStateName, 0, 0f);

        if (audioSource && deathSfx) audioSource.PlayOneShot(deathSfx);

        StopAllCoroutines();
        StartCoroutine(Co_FreezeOnDeathLastFrame(DeathStateName));
    }

    IEnumerator Co_FreezeOnDeathLastFrame(string deathStateName)
    {
        // Wait until we are actually in Death
        for (int i = 0; i < 120; i++)
        {
            var st = animator.GetCurrentAnimatorStateInfo(0);
            if (st.IsName(deathStateName)) break;
            yield return null;
        }

        // Wait until the non-looping clip finishes once
        for (int i = 0; i < 600; i++)
        {
            var st = animator.GetCurrentAnimatorStateInfo(0);
            if (st.IsName(deathStateName) && st.normalizedTime >= 1f) break;
            yield return null;
        }

        // Hold last frame forever
        animator.speed = 0f;

        // IMPORTANT: leave Dead=true; do NOT clear it here.
        // (Clear it only when you respawn/reset the enemy.)

        foreach (var c in GetComponentsInChildren<Collider>())
            c.enabled = false;

    }



    IEnumerator Co_FreezeOnDeathLastFrame()
    {
        // Wait until the Animator is actually in the Death state
        // (We assume it's in base layer and the state name contains "Death")
        int safety = 0;
        while (safety++ < 120)
        {
            var st = animator.GetCurrentAnimatorStateInfo(0);
            if (st.IsName("Death") || st.shortNameHash == Animator.StringToHash("Death")) break;
            yield return null;
        }

        // Wait for the state to finish playing once
        safety = 0;
        while (safety++ < 480)
        {
            var st = animator.GetCurrentAnimatorStateInfo(0);
            if (st.normalizedTime >= 1f) break;
            yield return null;
        }

        // Freeze animator so it holds the last frame forever
        animator.speed = 0f;
    }

    void SetDir(int dirIndex)
    {
        if (dirIndex == _currentDirIndex) return;
        _currentDirIndex = dirIndex;
        animator.SetInteger(ParamDirIndex, _currentDirIndex);
    }

    /// <summary>
    /// Maps a world-space direction vector to an 8-way index:
    /// 0=E, 1=NE, 2=N, 3=NW, 4=W, 5=SW, 6=S, 7=SE
    /// </summary>
    public static int WorldDirToIndex(Vector3 worldDir)
    {
        Vector2 d = new Vector2(worldDir.x, worldDir.z); // assuming Z-forward world
        if (d.sqrMagnitude < 1e-6f) return 2; // default N

        float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg; // 0 = east, 90 = north
        if (angle < 0) angle += 360f;

        // Each direction is centered at:
        // E=0°, NE=45°, N=90°, NW=135°, W=180°, SW=225°, S=270°, SE=315°
        // Each covers ±22.5° from center
        if (angle >= 337.5f || angle < 22.5f) return 0; // E
        if (angle < 67.5f) return 1; // NE
        if (angle < 112.5f) return 2; // N
        if (angle < 157.5f) return 3; // NW
        if (angle < 202.5f) return 4; // W
        if (angle < 247.5f) return 5; // SW
        if (angle < 292.5f) return 6; // S
        return 7; // SE
    }


    // --- Optional: quick melee demo hooks (comment out if you don't want trigger handling) ---
    // Expect a trigger collider on the enemy, and melee hitboxes with tag "PlayerFist".
    // For 2D, swap to OnTriggerEnter2D and Collider2D.

    void OnTriggerEnter(Collider other)
    {
        if (_dead) return;
        if (other.CompareTag("PlayerFist"))
        {
            TakeDamageFrom(other.transform, 1);
        }
    }

#if UNITY_2D
    void OnTriggerEnter2D(Collider2D other)
    {
        if (_dead) return;
        if (other.CompareTag("PlayerFist"))
        {
            TakeDamageFrom(other.transform, 1);
        }
    }
#endif

    // --- Debug drawing (optional) ---
    void OnDrawGizmosSelected()
    {
        if (!drawDebugSectors) return;
        Gizmos.color = Color.yellow;
        Vector3 p = transform.position;
        for (int i = 0; i < 8; i++)
        {
            float deg = (i * 45f) * Mathf.Deg2Rad;
            Vector3 dir = new Vector3(Mathf.Cos(deg), 0, Mathf.Sin(deg));
            Gizmos.DrawLine(p, p + dir * 1.0f);
        }
    }
}
