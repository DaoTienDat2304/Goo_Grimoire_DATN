using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class tamingManager : MonoBehaviour
{
    public float maxTamingPoint = 100;
    public float curTamingPoint = 30;
    [SerializeField] private Spawner spawner;
    [SerializeField] private PlayerMovement playerMovement;
    public int curID;
    public WildSlimes wildSlimes;
    public SlimeSpawner slimeSpawner;
    public float difficulty;

    public Image emote;
    public Sprite succeedcatch;
    public Sprite failcatch;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        var curSlime = new WildSlimes.WildSlimeTraits();
        foreach (var s in wildSlimes.slimes)
        {
            if (s.slimeID == curID)
            {
                curSlime = s;
                difficulty = curSlime.wildSlimeTraits[0].GenerateInstance().GetRarityMultiplier(curSlime.wildSlimeTraits[0].rarity)
                    + curSlime.wildSlimeTraits[1].GenerateInstance().GetRarityMultiplier(curSlime.wildSlimeTraits[1].rarity)
                    + curSlime.wildSlimeTraits[2].GenerateInstance().GetRarityMultiplier(curSlime.wildSlimeTraits[2].rarity);
                break;
            }
        }
        if (curTamingPoint <= 0)
        {
            Debug.Log("Fail");
            foreach (var note in spawner.notes)
            {
                Destroy(note.gameObject);
            }
            foreach(var t in slimeSpawner.activeSlimes)
            {
                if (t.GetComponent<WildSlimeTraits>().wildSlimeID == curID)
                {
                    Destroy(t);
                    Vector3 spawnPosition = slimeSpawner.GetRandomSpawnPosition();
                    slimeSpawner.SpawnSingleSlime(spawnPosition);
                    break;
                }
            }
            wildSlimes.slimes.Remove(curSlime);
            curTamingPoint = 30;
            spawner.gameObject.SetActive(false);
            emote.gameObject.SetActive(true);
            emote.sprite = failcatch;
            StartCoroutine(deactive());
        }
        if (curTamingPoint >= maxTamingPoint)
        {
            Debug.Log("Success");
            foreach (var note in spawner.notes)
            {
                Destroy(note.gameObject);
            }
            wildSlimes.tamedSlimes.Add(curSlime);
            for (int i = slimeSpawner.activeSlimes.Count - 1; i >= 0; i--)
            {
                var t = slimeSpawner.activeSlimes[i];
                if (t.GetComponent<WildSlimeTraits>().wildSlimeID == curID)
                {
                    if (t != null)Destroy(t);
                    slimeSpawner.activeSlimes.RemoveAt(i);

                    Vector3 spawnPosition = slimeSpawner.GetRandomSpawnPosition();
                    slimeSpawner.SpawnSingleSlime(spawnPosition);
                    break;
                }
            }
            wildSlimes.slimes.Remove(curSlime);
            curTamingPoint = 30;
            spawner.gameObject.SetActive(false);
            emote.gameObject.SetActive(true);
            emote.sprite = succeedcatch;
            StartCoroutine(deactive());
            
        }
    }

    public void hit ()
    {
        emote.gameObject.SetActive(true);
    }

    IEnumerator hitdeactive()
    {
        yield return new WaitForSeconds(0.5f);
        emote.gameObject.SetActive(false);
    }



    IEnumerator deactive()
    {
        yield return new WaitForSeconds(3);
        spawner.gameObject.SetActive(true);
        emote.gameObject.SetActive(false);
        this.gameObject.SetActive(false);
        playerMovement.enabled = true;
    }
}
