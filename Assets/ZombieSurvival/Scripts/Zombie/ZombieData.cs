using UnityEngine;

[CreateAssetMenu(menuName = "GameData/ZombieData")]
public class ZombieData : ScriptableObject
{
    public string ZombieName;
    public float Speed;
    public int Damage;
    public float AttackInterval;
    public int MaxHealth;
    public float AttackRange;
}
