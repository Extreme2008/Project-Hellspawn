// IDamageable.cs
using UnityEngine;

public interface IDamageable
{
    void TakeDamage(DamageInfo info);
    bool IsAlive { get; }
}

public struct DamageInfo
{
    public float Amount;
    public Vector3 Point;
    public Vector3 Normal;
    public GameObject Source;
    public DamageType Type;
    public bool Crit;
}

public enum DamageType {Melee, Bullet, Shell, Explosive, Energy}
