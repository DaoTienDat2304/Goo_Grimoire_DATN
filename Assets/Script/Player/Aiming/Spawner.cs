using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using NUnit.Framework;

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
        if (TamingManager.difficulty == 0) spd = 1f;
        else spd = 3f / TamingManager.difficulty;
    }
    private void OnEnable()
    {
        foreach (var note in notes)
        {
            Destroy(note.gameObject);
        }
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
            int rand = Random.Range(1, 5); // 1 -> 4
            switch (rand)
            {
                case 1:
                    note.GetComponent<MovingNote>().typeID = 1;
                    break;

                case 2:
                    note.GetComponent<MovingNote>().typeID = 2;
                    note.transform.rotation = Quaternion.Euler(0, 0, 90);
                    break;

                case 3:
                    note.GetComponent<MovingNote>().typeID = 3;
                    note.transform.rotation = Quaternion.Euler(0, 0, 180);
                    break;

                case 4:
                    note.GetComponent<MovingNote>().typeID = 4;
                    note.transform.rotation = Quaternion.Euler(0, 0, 270);
                    break;
            }

            yield return new WaitForSeconds(spd); 
        }
    }
}
