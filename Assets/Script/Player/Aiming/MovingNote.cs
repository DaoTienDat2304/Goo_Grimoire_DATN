using UnityEngine;

public class MovingNote : MonoBehaviour
{
    [SerializeField] public float noteSpeed;
    [SerializeField] public Collider2D checkBar;
    [SerializeField] public Collider2D failBar;
    [SerializeField] private float tamingScore = 10;
    [SerializeField] private tamingManager TamingManager;
    [SerializeField] private TamingManagerTutorial tamingManagerTutorial;
    public int typeID;
    private bool isInCheckBar = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        TamingManager = FindAnyObjectByType<tamingManager>();
        tamingManagerTutorial = FindAnyObjectByType<TamingManagerTutorial>();
        GameObject obj = GameObject.FindGameObjectWithTag("CheckBar");
        if (obj != null)
            checkBar = obj.GetComponent<Collider2D>();

        obj = GameObject.FindGameObjectWithTag("FailBar");
        if (obj != null)
            failBar = obj.GetComponent<Collider2D>();
    }

    // Update is called once per frame
    void Update()
{
    if (TamingManager.difficulty == 0) this.transform.position += Vector3.right * noteSpeed * 3 * Time.deltaTime;
    else this.transform.position += Vector3.right * noteSpeed * TamingManager.difficulty * Time.deltaTime;
    if (isInCheckBar)
    {
        KeyCode correctKey = KeyCode.None;
        switch (typeID)
        {
            case 1:
                correctKey = KeyCode.RightArrow;
                break;
            case 2:
                correctKey = KeyCode.UpArrow;
                break;
            case 3:
                correctKey = KeyCode.LeftArrow;
                break;
            case 4:
                correctKey = KeyCode.DownArrow;
                break;
        }
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            if (Input.GetKeyDown(correctKey))
            {
                TamingManager.curTamingPoint += tamingScore * 8/TamingManager.difficulty;
                if (tamingManagerTutorial != null) tamingManagerTutorial.curTamingPoint += 30;
                Destroy(gameObject);
            }
            else
            {
                TamingManager.curTamingPoint -= tamingScore;
                Destroy(gameObject); 
            }
        }
    }
}
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision == checkBar)
        {
            isInCheckBar = true;
        }
        else if (collision == failBar)
        {
            TamingManager.curTamingPoint -= tamingScore;
            Destroy(gameObject); 
        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision == checkBar)
        {
            isInCheckBar = false; 
        }
    }
}
