using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UI;
public class FormationManager : MonoBehaviour
{
    public Team teamSlimes;
    public List<Member> teamMembers;
    public List<Transform> dropZones;
    public List<GameObject> slimeFormation;
    public int rows = 3;
    public int cols = 3;
    public Transform gridParent;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public Sprite CreateDefaultSlimeSprite()
    {
        int size = 64;
        Texture2D texture = new Texture2D(size, size);

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(size / 2, size / 2));
                if (distance < size / 2)
                {
                    float alpha = 1f - (distance / (size / 2));
                    texture.SetPixel(x, y, new Color(0.2f, 0.8f, 0.3f, alpha));
                }
                else
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    public Transform GetSlot(int row, int col)
    {
        int index = row * cols + col;
        if (index < 0 || index >= gridParent.childCount) return null;
        Debug.Log(gridParent.GetChild(index).name);
        return gridParent.GetChild(index);
    }

    public GameObject GetRandomRowLastAlive()
    {
        List<int> rowList = new List<int>();
        for (int i = 0; i < rows; i++)
            rowList.Add(i);

        for (int i = 0; i < rowList.Count; i++)
        {
            int rnd = Random.Range(i, rowList.Count);
            (rowList[i], rowList[rnd]) = (rowList[rnd], rowList[i]);
        }

        foreach (int row in rowList)
        {
            for (int col = cols - 1; col >= 0; col--)
            {
                Transform slot = GetSlot(row, col);
                if (slot == null) continue;

                var battleStats = slot.GetComponentInChildren<SlimeBattleStats>();
                if (battleStats != null && battleStats.CurrentHP > 0)
                    return battleStats.gameObject;
            }
        }

        return null;
    }

    void Start()
    {
        int maxSlots = rows * cols;
        if (gridParent != null)
        {
            for (int i = 0; i < gridParent.childCount; i++)
            {
                gridParent.GetChild(i).gameObject.SetActive(i < maxSlots);
            }
        }

        Invoke(nameof(RefreshTeamDisplay), 0.1f);
    }
    
    public void RefreshTeamDisplay()
    {
        if (teamSlimes == null || teamSlimes.team == null)
        {
            return;
        }
        
        if (teamMembers == null || teamMembers.Count == 0)
        {
            return;
        }
        
        
        // Clear all slots first
        for (int j = 0; j < teamMembers.Count; j++)
        {
            if (teamMembers[j] != null)
            {
                var bodyRenderer = teamMembers[j].body?.GetComponent<Image>();
                var armorRenderer = teamMembers[j].armor?.GetComponent<Image>();
                var weaponRenderer = teamMembers[j].weapon?.GetComponent<Image>();
                
                if (bodyRenderer != null) bodyRenderer.sprite = null;
                if (armorRenderer != null) armorRenderer.sprite = null;
                if (weaponRenderer != null) weaponRenderer.sprite = null;
            }
        }
        
        // Display team slimes
        int i = 0;
        foreach (Slime slime in teamSlimes.team)
        {
            if (i >= teamMembers.Count)
            {
                break;
            }
            
            if (teamMembers[i] == null)
            {
                i++;
                continue;
            }
            
            if (slime != null)
            {
                teamMembers[i].id = slime.id;
                var bodyRenderer = teamMembers[i].body?.GetComponent<Image>();
                var armorRenderer = teamMembers[i].armor?.GetComponent<Image>();
                var weaponRenderer = teamMembers[i].weapon?.GetComponent<Image>();
                
                if (bodyRenderer != null)
                {
                    bodyRenderer.sprite = slime.body?.sprite ?? CreateDefaultSlimeSprite();
                }
                
                if (armorRenderer != null)
                {
                    armorRenderer.sprite = slime.armor?.sprite ?? CreateDefaultSlimeSprite();
                }
                
                if (weaponRenderer != null)
                {
                    weaponRenderer.sprite = slime.weapon?.sprite ?? CreateDefaultSlimeSprite();
                }
            }
            else
            {
                Debug.LogWarning($"Slime at index {i} is null!");
            }
            
            i++;
        }
        
        Debug.Log($"Team display refresh completed. Displayed {i} slimes.");
    }

    
    public void ForceRefreshTeam()
    {
        RefreshTeamDisplay();
    }
    
    public bool IsTeamValid()
    {
        return teamSlimes != null && teamSlimes.team != null && teamMembers != null && teamMembers.Count > 0;
    }

    // --- Spatial Query Methods ---

    public bool TryGetSlimePosition(GameObject slime, out int row, out int col)
    {
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                Transform slot = GetSlot(r, c);
                if (slot == null) continue;
                if (slime.transform.IsChildOf(slot))
                {
                    row = r; col = c;
                    return true;
                }
            }
        }
        row = -1; col = -1;
        return false;
    }

    public List<GameObject> GetAllAliveAllies()
    {
        var result = new List<GameObject>();
        foreach (var go in slimeFormation)
        {
            if (go == null) continue;
            var stats = go.GetComponent<SlimeBattleStats>();
            if (stats != null && stats.CurrentHP > 0 && go.activeInHierarchy)
                result.Add(go);
        }
        return result;
    }

    public List<GameObject> GetAllAliveEnemies(GameObject currentTarget)
    {
        var result = new List<GameObject>();
        var allStats = Object.FindObjectsByType<SlimeBattleStats>(FindObjectsSortMode.None);
        foreach (var stats in allStats)
        {
            if (stats != null && stats.CurrentHP > 0 && stats.gameObject.activeInHierarchy)
            {
                var baseStats = stats.GetComponent<SlimeStats>();
                if (baseStats != null && baseStats.isEnemy)
                {
                    result.Add(stats.gameObject);
                }
            }
        }
        return result;
    }

    public List<GameObject> GetAliveSlimesInRow(int row)
    {
        var result = new List<GameObject>();
        for (int c = 0; c < cols; c++)
        {
            Transform slot = GetSlot(row, c);
            if (slot == null) continue;
            var stats = slot.GetComponentInChildren<SlimeBattleStats>();
            if (stats != null && stats.CurrentHP > 0)
                result.Add(stats.gameObject);
        }
        return result;
    }

    public List<GameObject> GetAliveSlimesInColumn(int col)
    {
        var result = new List<GameObject>();
        for (int r = 0; r < rows; r++)
        {
            Transform slot = GetSlot(r, col);
            if (slot == null) continue;
            var stats = slot.GetComponentInChildren<SlimeBattleStats>();
            if (stats != null && stats.CurrentHP > 0)
                result.Add(stats.gameObject);
        }
        return result;
    }

    public GameObject GetFirstAliveInRow(int row)
    {
        for (int c = 0; c < cols; c++)
        {
            Transform slot = GetSlot(row, c);
            if (slot == null) continue;
            var stats = slot.GetComponentInChildren<SlimeBattleStats>();
            if (stats != null && stats.CurrentHP > 0)
                return stats.gameObject;
        }
        return null;
    }

    public GameObject GetFirstAliveInColumn(int col)
    {
        for (int r = 0; r < rows; r++)
        {
            Transform slot = GetSlot(r, col);
            if (slot == null) continue;
            var stats = slot.GetComponentInChildren<SlimeBattleStats>();
            if (stats != null && stats.CurrentHP > 0)
                return stats.gameObject;
        }
        return null;
    }

    public GameObject GetRandomAliveAlly()
    {
        var alive = GetAllAliveAllies();
        if (alive.Count == 0) return null;
        return alive[Random.Range(0, alive.Count)];
    }

    public GameObject GetLowestHPAlly()
    {
        var alive = GetAllAliveAllies();
        if (alive.Count == 0) return null;
        alive.Sort((a, b) =>
            a.GetComponent<SlimeBattleStats>().CurrentHP
            .CompareTo(b.GetComponent<SlimeBattleStats>().CurrentHP));
        return alive[0];
    }

    public GameObject GetHighestHPAlly()
    {
        var alive = GetAllAliveAllies();
        if (alive.Count == 0) return null;
        alive.Sort((a, b) =>
            b.GetComponent<SlimeBattleStats>().CurrentHP
            .CompareTo(a.GetComponent<SlimeBattleStats>().CurrentHP));
        return alive[0];
    }

    public List<GameObject> ResolveTargets(EffectEntry entry, GameObject caster, GameObject directTarget, GameObject boss)
    {
        var fx = entry.effect;

        GameObject anchor = fx.anchorType switch
        {
            AnchorType.Self           => caster,
            AnchorType.AttackTarget   => directTarget,
            _                         => directTarget
        };

        if (anchor == null) return new();

        List<GameObject> candidates;
        if (fx.aoeShape == AoEShape.FullSide)
        {
            bool casterIsEnemy = (caster == boss || caster.GetComponent<SlimeStats>()?.isEnemy == true);
            if (fx.targetSide == TargetSide.Allies)
            {
                candidates = casterIsEnemy ? GetAllAliveEnemies(boss) : GetAllAliveAllies();
            }
            else if (fx.targetSide == TargetSide.Enemies)
            {
                candidates = casterIsEnemy ? GetAllAliveAllies() : GetAllAliveEnemies(boss);
            }
            else
            {
                candidates = new(GetAllAliveAllies());
                candidates.AddRange(GetAllAliveEnemies(boss));
            }
        }
        else if (fx.aoeShape == AoEShape.Blast)
        {
            candidates = new() { anchor };
            
            bool anchorIsBoss = (anchor == boss || anchor.GetComponent<SlimeStats>()?.isEnemy == true);
            if (!anchorIsBoss)
            {
                if (TryGetSlimePosition(anchor, out int row, out int col))
                {
                    if (col > 0)
                    {
                        Transform leftSlot = GetSlot(row, col - 1);
                        var leftStats = leftSlot != null ? leftSlot.GetComponentInChildren<SlimeBattleStats>() : null;
                        if (leftStats != null && leftStats.CurrentHP > 0) candidates.Add(leftStats.gameObject);
                    }
                    if (col < cols - 1)
                    {
                        Transform rightSlot = GetSlot(row, col + 1);
                        var rightStats = rightSlot != null ? rightSlot.GetComponentInChildren<SlimeBattleStats>() : null;
                        if (rightStats != null && rightStats.CurrentHP > 0) candidates.Add(rightStats.gameObject);
                    }
                }
            }
        }
        else // Single target
        {
            candidates = new() { anchor };
        }

        bool targetIsEnemySide = (fx.targetSide == TargetSide.Enemies);
        bool casterIsEnemySide = (caster == boss || caster.GetComponent<SlimeStats>()?.isEnemy == true);
        
        bool wantEnemyOfCaster = targetIsEnemySide;

        candidates.RemoveAll(go => {
            if (go == null) return true;
            bool goIsEnemy = (go == boss || go.GetComponent<SlimeStats>()?.isEnemy == true);
            bool isAllyOfCaster = (goIsEnemy == casterIsEnemySide);
            
            if (wantEnemyOfCaster)
            {
                return isAllyOfCaster;
            }
            else
            {
                return !isAllyOfCaster;
            }
        });

        candidates.RemoveAll(go => go == null
            || go.GetComponent<SlimeBattleStats>()?.CurrentHP <= 0);

        if (entry.applyChance < 100f)
            candidates.RemoveAll(_ => Random.Range(0f, 100f) >= entry.applyChance);

        return candidates;
    }
}
