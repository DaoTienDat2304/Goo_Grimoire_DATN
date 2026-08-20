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
    public int rows = 3;   // 3 hàng
    public int cols = 3;   // 3 cột 
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
        int index = row * cols + col;  // tính index trong grid
        if (index < 0 || index >= gridParent.childCount) return null;
        Debug.Log(gridParent.GetChild(index).name);
        return gridParent.GetChild(index);
    }

    public GameObject GetRandomRowLastAlive()
    {
        // Chọn ngẫu nhiên 1 hàng
        List<int> rowList = new List<int>();
        for (int i = 0; i < rows; i++)
            rowList.Add(i);

        // Trộn ngẫu nhiên danh sách hàng
        for (int i = 0; i < rowList.Count; i++)
        {
            int rnd = Random.Range(i, rowList.Count);
            (rowList[i], rowList[rnd]) = (rowList[rnd], rowList[i]);
        }

        // Duyệt theo thứ tự ngẫu nhiên
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

        // Không tìm thấy slime còn sống
        return null;
    }

    void Start()
    {
        // Tự động ẩn các ô vượt quá giới hạn 3x3 (max = 9)
        int maxSlots = rows * cols;
        if (gridParent != null)
        {
            for (int i = 0; i < gridParent.childCount; i++)
            {
                gridParent.GetChild(i).gameObject.SetActive(i < maxSlots);
            }
        }

        // Delay để đảm bảo team đã được load
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
        
        
        int activeSlimesCount = teamSlimes.team != null ? teamSlimes.team.Count : 0;

        for (int idx = 0; idx < teamMembers.Count; idx++)
        {
            if (teamMembers[idx] == null) continue;

            // Luôn giữ ô Member hiển thị (không ẩn ô)
            teamMembers[idx].gameObject.SetActive(true);

            var bodyRenderer = teamMembers[idx].body?.GetComponent<Image>();
            var armorRenderer = teamMembers[idx].armor?.GetComponent<Image>();
            var weaponRenderer = teamMembers[idx].weapon?.GetComponent<Image>();

            if (idx < activeSlimesCount && teamSlimes.team[idx] != null)
            {
                // Slot hợp lệ có slime -> Gán id và sprite
                Slime slime = teamSlimes.team[idx];
                teamMembers[idx].id = slime.id;

                if (bodyRenderer != null)
                {
                    Sprite s = slime.body?.sprite;
                    bodyRenderer.sprite = s;
                    bodyRenderer.enabled = (s != null);
                }

                if (armorRenderer != null)
                {
                    Sprite s = slime.armor?.sprite;
                    armorRenderer.sprite = s;
                    armorRenderer.enabled = (s != null);
                }

                if (weaponRenderer != null)
                {
                    Sprite s = slime.weapon?.sprite;
                    weaponRenderer.sprite = s;
                    weaponRenderer.enabled = (s != null);
                }
            }
            else
            {
                // Không có slime cho slot này (ví dụ chỉ chọn 1-2 slime) -> Xóa sprite và tắt renderer để không bị hiện khối trắng
                if (bodyRenderer != null) { bodyRenderer.sprite = null; bodyRenderer.enabled = false; }
                if (armorRenderer != null) { armorRenderer.sprite = null; armorRenderer.enabled = false; }
                if (weaponRenderer != null) { weaponRenderer.sprite = null; weaponRenderer.enabled = false; }
            }
        }
    }

    // Update() đã được xóa — không có logic nào cần chạy mỗi frame.
    
    // Method để refresh team từ bên ngoài
    public void ForceRefreshTeam()
    {
        RefreshTeamDisplay();
    }
    
    // Method để kiểm tra team có hợp lệ không
    public bool IsTeamValid()
    {
        return teamSlimes != null && teamSlimes.team != null && teamMembers != null && teamMembers.Count > 0;
    }

    // --- Spatial Query Methods ---

    // Tìm vị trí (row, col) của một slime trong grid ally
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

    // Tất cả ally còn sống
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

    // Ally còn sống trong một hàng
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

    // Ally còn sống trong một cột
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

    // Slime còn sống đầu tiên trong hàng (scan trái → phải)
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

    // Slime còn sống đầu tiên trong cột (scan trên → dưới)
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

    // Resolve danh sách target cho một effect — dùng chung giữa TurnSystem và SkillTestHelper
    public List<GameObject> ResolveTargets(EffectEntry entry, GameObject caster, GameObject directTarget, GameObject boss)
    {
        var fx = entry.effect;

        // Bước 1: Resolve anchor
        GameObject anchor = fx.anchorType switch
        {
            AnchorType.Self           => caster,
            AnchorType.AttackTarget   => directTarget,
            _                         => directTarget
        };

        if (anchor == null) return new();

        // Bước 2: Expand theo AoEShape
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
            
            // Tìm các ô lân cận trong grid (chỉ áp dụng nếu anchor là player slime trên grid 3x3)
            bool anchorIsBoss = (anchor == boss || anchor.GetComponent<SlimeStats>()?.isEnemy == true);
            if (!anchorIsBoss)
            {
                if (TryGetSlimePosition(anchor, out int row, out int col))
                {
                    // Lân cận trên cùng một hàng (cột col-1 và col+1)
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

        // Bước 3: Filter theo TargetSide
        bool targetIsEnemySide = (fx.targetSide == TargetSide.Enemies);
        bool casterIsEnemySide = (caster == boss || caster.GetComponent<SlimeStats>()?.isEnemy == true);
        
        // Xác định xem mục tiêu có thuộc phe đối địch với caster hay không
        bool wantEnemyOfCaster = targetIsEnemySide;

        candidates.RemoveAll(go => {
            if (go == null) return true;
            bool goIsEnemy = (go == boss || go.GetComponent<SlimeStats>()?.isEnemy == true);
            bool isAllyOfCaster = (goIsEnemy == casterIsEnemySide);
            
            if (wantEnemyOfCaster)
            {
                return isAllyOfCaster; // Loại bỏ nếu là đồng đội của caster
            }
            else
            {
                return !isAllyOfCaster; // Loại bỏ nếu là kẻ địch của caster
            }
        });

        // Bước 4: Filter dead / null
        candidates.RemoveAll(go => go == null
            || go.GetComponent<SlimeBattleStats>()?.CurrentHP <= 0);

        // Bước 5: Roll applyChance mỗi target
        if (entry.applyChance < 100f)
            candidates.RemoveAll(_ => Random.Range(0f, 100f) >= entry.applyChance);

        return candidates;
    }
}
