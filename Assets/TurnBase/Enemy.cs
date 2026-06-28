using UnityEngine;

public class Enemy : MonoBehaviour
{
    public Slime enemy;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        var stats = this.GetComponent<SlimeStats>();
        stats.HP = enemy.totalHP;
        stats.Attack = enemy.totalAttack;
        stats.MagicAttack = enemy.totalMagicAttack;
        stats.Defense = enemy.totalDefense;
        stats.Speed = enemy.totalSpeed;
        stats.CritRate = enemy.totalCritRate;
        stats.CritDMG = enemy.totalCritDMG;
        stats.isEnemy = true;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
