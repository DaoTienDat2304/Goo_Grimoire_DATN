using Spine.Unity;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class WildSlimeTraits : MonoBehaviour
{
    private static Sprite cachedDefaultSlimeSprite;
    public WildSlimeAllTraits allWildTraits;
    public WildSlimes wildSlimes;
    public WildSlimes.WildSlimeTraits newSlime;
    public Image bodyRender;
    public Image armorRender;
    public Image weaponRender;
    public int wildSlimeID;
    public tamingManager taming;
    public float tamingDifficulty;
    public TraitSO RollWildTrait(TraitType type)
    {
        if (allWildTraits == null)
        {

            return null;
        }

        var pool = allWildTraits.traits.Where(t => t != null && t.type == type && t.dropRate > 0f).ToList();
        if (pool.Count == 0)
        {

            return null;
        }

        float totalRate = pool.Sum(t => t.dropRate);
        if (totalRate <= 0f)
        {

            return null;
        }

        float roll = Random.Range(0f, totalRate);
        float cumulative = 0f;

        foreach (var t in pool)
        {
            cumulative += t.dropRate;
            if (roll <= cumulative) return t;
        }

        return pool[0];
    }
    public Slime GenerateWildSlime(string name)
    {
        
        var bodySo = RollWildTrait(TraitType.Body);
        var armorSo = RollWildTrait(TraitType.Armor);
        var weaponSo = RollWildTrait(TraitType.Weapon);
        newSlime.wildSlimeTraits[0] = bodySo;
        newSlime.wildSlimeTraits[1] = armorSo;
        newSlime.wildSlimeTraits[2] = weaponSo;
        tamingDifficulty = bodySo.GenerateInstance().GetRarityMultiplier(bodySo.rarity) + armorSo.GenerateInstance().GetRarityMultiplier(armorSo.rarity) + weaponSo.GenerateInstance().GetRarityMultiplier(weaponSo.rarity);

        if (bodySo == null || armorSo == null || weaponSo == null)
        {

            return null;
        }

        Slime s = new Slime();
        s.slimeName = name;
        s.body = bodySo.GenerateInstance();
        s.armor = armorSo.GenerateInstance();
        s.weapon = weaponSo.GenerateInstance();
        s.CalculateStats();
        return s;
    }
    
    public Slime CreateSlimeFromWildSlimeTraits(string name, WildSlimes.WildSlimeTraits wildSlimeTraits)
    {
        if (wildSlimeTraits == null || wildSlimeTraits.wildSlimeTraits == null)
        {
            return null;
        }

        TraitSO bodySo = null;
        TraitSO armorSo = null;
        TraitSO weaponSo = null;

        foreach (var trait in wildSlimeTraits.wildSlimeTraits)
        {
            if (trait == null) continue;
            
            if (trait.type == TraitType.Body)
                bodySo = trait;
            else if (trait.type == TraitType.Armor)
                armorSo = trait;
            else if (trait.type == TraitType.Weapon)
                weaponSo = trait;
        }

        if (bodySo == null || armorSo == null || weaponSo == null)
        {
            return null;
        }

        Slime s = new Slime();
        s.slimeName = name;
        s.body = bodySo.GenerateInstance();
        s.armor = armorSo.GenerateInstance();
        s.weapon = weaponSo.GenerateInstance();
        AdventureStatRoll.Apply(s);
        return s;
    }
    public Sprite CreateDefaultSlimeSprite()
    {
        if (cachedDefaultSlimeSprite != null)
            return cachedDefaultSlimeSprite;

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
        cachedDefaultSlimeSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        cachedDefaultSlimeSprite.name = "DefaultWildSlimeSprite_Cached";
        return cachedDefaultSlimeSprite;
    }

    IEnumerator MenuSetup()
    {
        bodyRender = GameObject.Find("BodySprite").GetComponent<Image>();
        armorRender = GameObject.Find("ArmorSprite").GetComponent<Image>();
        weaponRender = GameObject.Find("WeaponSprite").GetComponent<Image>();
        taming = GameObject.Find("TamingPanel").GetComponent<tamingManager>();
        taming.curID = wildSlimeID;
        yield return new WaitForSeconds(0.03f);
        foreach (var t in newSlime.wildSlimeTraits)
        {
            
            if (t.type == TraitType.Body) bodyRender.sprite = t.sprite;
            if (t.type == TraitType.Armor) armorRender.sprite = t.sprite;
            if (t.type == TraitType.Weapon) weaponRender.sprite = t.sprite;
        }
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Slime slime = GenerateWildSlime("Wild Slime");
        var body = new GameObject("Body");
        body.transform.SetParent(transform, false);
        var bodyAnimationController = body.AddComponent<SlimeAnimationController>();
        if (slime?.body?.hasAnimation == true)
        {
            bodyAnimationController.Initialize(slime.body.animationAsset, slime.body.animationName);

            bodyAnimationController.PlayAnimation("animation");
        }
        else
        {
            bodyAnimationController.Initialize(null);
            bodyAnimationController.SetSprite((slime != null ? slime.body?.sprite : null) ?? CreateDefaultSlimeSprite());
        }
        var animator = body.GetComponent<SkeletonAnimation>();
        animator.GetComponent<MeshRenderer>().sortingLayerName = "Player";
        animator.GetComponent<MeshRenderer>().sortingOrder = 0;
        var armorGO = new GameObject("Armor");
        armorGO.transform.SetParent(transform, false);
        armorGO.transform.localScale = Vector3.one;
        var armorRenderer = armorGO.AddComponent<SpriteRenderer>();
        armorGO.transform.localPosition = Vector3.up * 4.1f;
        var weaponGO = new GameObject("Weapon");
        weaponGO.transform.SetParent(transform, false);
        weaponGO.transform.localScale = Vector3.one;
        var weaponRenderer = weaponGO.AddComponent<SpriteRenderer>();
        weaponGO.transform.localPosition = Vector3.up * 4.1f;
        armorRenderer.sprite = (slime != null ? slime.armor?.sprite : null) ?? CreateDefaultSlimeSprite();
        armorRenderer.sortingLayerName = "Player";
        armorRenderer.sortingOrder = 1;
        weaponRenderer.sprite = (slime != null ? slime.weapon?.sprite : null) ?? CreateDefaultSlimeSprite();
        weaponRenderer.sortingLayerName = "Player";
        weaponRenderer.sortingOrder = 2;
        newSlime.slimeID = wildSlimeID;
        newSlime.slimeType = Random.value < 0.5f ? WildSlimeType.Friendly : WildSlimeType.Aggressive;
        wildSlimes.slimes.Add(newSlime);
        wildSlimes.slimes[wildSlimes.slimes.Count - 1].slimeID = wildSlimeID;
    }

    public WildSlimeType GetSlimeType()
    {
        if (wildSlimes == null) return WildSlimeType.Friendly;
        
        foreach (var s in wildSlimes.slimes)
        {
            if (s.slimeID == wildSlimeID)
            {
                return s.slimeType;
            }
        }
        return WildSlimeType.Friendly; // Default
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Catcher"))
        {
            WildSlimes.WildSlimeTraits currentSlime = null;
            foreach (var s in wildSlimes.slimes)
            {
                if (s.slimeID == wildSlimeID)
                {
                    currentSlime = s;
                    break;
                }
            }
            
            if (currentSlime != null)
            {
                if (currentSlime.slimeType == WildSlimeType.Friendly)
                {
                    StartCoroutine(MenuSetup());
                }
                else if (currentSlime.slimeType == WildSlimeType.Aggressive)
                {
                    StartCoroutine(StartBattleWithSlime(currentSlime));
                }
            }
        }
    }
    
    IEnumerator StartBattleWithSlime(WildSlimes.WildSlimeTraits wildSlimeTraits)
    {
        if (wildSlimeID <= 0 && wildSlimeTraits != null)
        {
            wildSlimeID = wildSlimeTraits.slimeID;
        }

        Slime bossSlime = CreateSlimeFromWildSlimeTraits("Wild Boss", wildSlimeTraits);
        
        if (bossSlime == null)
        {
            Debug.LogError("Failed to create boss slime from wild slime traits!");
            yield break;
        }
        
        if (BattleDataManager.Instance == null)
        {
            GameObject battleDataManagerGO = new GameObject("BattleDataManager");
            battleDataManagerGO.AddComponent<BattleDataManager>();
        }
        
        BattleDataManager.Instance.SetBossData(bossSlime, BattleMode.Adventure);
        BattleDataManager.Instance.SetWildSlimeID(wildSlimeID);
        
        yield return new WaitForSeconds(0.1f);
        yield return SceneLoader.LoadSceneWithLoadingCoroutine("TurnBaseGame");
    }
        // Update is called once per frame
    void Update()
    {
        
    }
}
