using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class Spawner : MonoBehaviour
{
    public GameObject notePrefab;  
    public Transform spawnPoint;
    public Transform spawnpanel;
    public List<GameObject> notes;
    [SerializeField] private tamingManager TamingManager;
    private float spd = 1;

    void Update()
    {
        ResolveManager();
        if (TamingManager == null)
            return;

        if (TamingManager.difficulty == 0) spd = 1f;
        else spd = 3f / TamingManager.difficulty;
    }
    private void OnEnable()
    {
        ResolveManager();
        if (notePrefab == null || spawnPoint == null || spawnpanel == null)
            return;

        if (notes == null)
            notes = new List<GameObject>();

        foreach (var note in notes)
        {
            if (note != null)
                Destroy(note.gameObject);
        }
        notes.Clear();
        StartCoroutine(SpawnNotes());
    }
    IEnumerator SpawnNotes()
    {
        while (true)
        {
            var note = Instantiate(notePrefab, spawnPoint.position, Quaternion.identity);
            note.transform.SetParent(spawnpanel);
            notes.Add(note);
            note.transform.localScale = Vector3.one * 1.8f;
            MovingNote movingNote = note.GetComponent<MovingNote>();
            if (movingNote == null)
            {
                Destroy(note);
                yield break;
            }

            movingNote.Configure(
                TamingManager,
                TamingManager != null ? TamingManager.CheckBar : null,
                TamingManager != null ? TamingManager.FailBar : null);

            int rand = Random.Range(1, 5); // 1 -> 4
            switch (rand)
            {
                case 1:
                    movingNote.typeID = 1;
                    break;

                case 2:
                    movingNote.typeID = 2;
                    note.transform.rotation = Quaternion.Euler(0, 0, 90);
                    break;

                case 3:
                    movingNote.typeID = 3;
                    note.transform.rotation = Quaternion.Euler(0, 0, 180);
                    break;

                case 4:
                    movingNote.typeID = 4;
                    note.transform.rotation = Quaternion.Euler(0, 0, 270);
                    break;
            }

            yield return new WaitForSeconds(spd); 
        }
    }

    private void ResolveManager()
    {
        if (TamingManager == null)
            TamingManager = GetComponentInParent<tamingManager>(true);
        if (TamingManager == null)
            TamingManager = tamingManager.Active;
    }
}
