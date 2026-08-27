using UnityEngine;

public class TamingManagerTutorial : MonoBehaviour
{
    public float maxTamingPoint = 100;
    public float curTamingPoint = 30;
    [SerializeField] private Spawner spawner;
    public float difficulty;
    public GameObject notification;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
        if (curTamingPoint <= 0)
        {
            foreach (var note in spawner.notes)
            {
                Destroy(note.gameObject);
            }
            curTamingPoint = 30;
            this.gameObject.SetActive(false);
        }
        if (curTamingPoint >= maxTamingPoint)
        {
            foreach (var note in spawner.notes)
            {
                Destroy(note.gameObject);
            }
            curTamingPoint = 30;
            notification.SetActive(true);
            this.gameObject.SetActive(false);
        }
    }
}
