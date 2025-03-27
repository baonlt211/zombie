using UnityEngine;

[CreateAssetMenu(menuName = "GameData/WeaponData")]
public class WeaponData : ScriptableObject
{
    public enum FireType { Single, Cone }
    public string WeaponName;
    public int Damage;
    public FireType fireType = FireType.Cone;
    public float Range; // Full distance of cone
    public float DamageRange; // thickness of cone
    public float DamageConeAngle; // e.g., 60 degrees
    public float FireRate;
    public float BulletSpeed;
}
