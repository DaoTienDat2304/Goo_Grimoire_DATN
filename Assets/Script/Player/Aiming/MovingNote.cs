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
        ResolveTamingManager();
        tamingManagerTutorial = FindAnyObjectByType<TamingManagerTutorial>();
        ResolveBars();
    }

    // Update is called once per frame
    void Update()
    {
        if (TamingManager == null)
            ResolveTamingManager();
        if (TamingManager == null)
            return;

        float difficulty = Mathf.Max(1f, TamingManager.difficulty);
        if (TamingManager.difficulty == 0)
            transform.position += Vector3.right * noteSpeed * 3 * Time.deltaTime;
        else
            transform.position += Vector3.right * noteSpeed * difficulty * Time.deltaTime;

        if (isInCheckBar)
        {
            MobileDirection correctDirection = MobileDirection.None;
            switch (typeID)
            {
                case 1:
                    correctDirection = MobileDirection.Right;
                    break;
                case 2:
                    correctDirection = MobileDirection.Up;
                    break;
                case 3:
                    correctDirection = MobileDirection.Left;
                    break;
                case 4:
                    correctDirection = MobileDirection.Down;
                    break;
            }

            if (MobileInput.TryGetDirectionDown(out var pressedDirection))
            {
                if (pressedDirection == correctDirection)
                {
                    TamingManager.curTamingPoint += tamingScore * 8 / difficulty;
                    if (tamingManagerTutorial != null)
                        tamingManagerTutorial.curTamingPoint += 30;
                }
                else
                {
                    TamingManager.curTamingPoint -= tamingScore;
                }

                Destroy(gameObject);
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
            if (TamingManager != null)
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

    private void ResolveTamingManager()
    {
        TamingManager = GetComponentInParent<tamingManager>(true);
        if (TamingManager == null)
            TamingManager = tamingManager.Active;
    }

    private void ResolveBars()
    {
        if (TamingManager != null)
        {
            foreach (Collider2D candidate in TamingManager.GetComponentsInChildren<Collider2D>(true))
            {
                if (checkBar == null && candidate.CompareTag("CheckBar"))
                    checkBar = candidate;
                else if (failBar == null && candidate.CompareTag("FailBar"))
                    failBar = candidate;
            }
        }

        if (checkBar == null)
        {
            GameObject obj = GameObject.FindGameObjectWithTag("CheckBar");
            if (obj != null)
                checkBar = obj.GetComponent<Collider2D>();
        }

        if (failBar == null)
        {
            GameObject obj = GameObject.FindGameObjectWithTag("FailBar");
            if (obj != null)
                failBar = obj.GetComponent<Collider2D>();
        }
    }
}
