using UnityEngine;

public class PlayerCameraFollow2D : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -10f);
    [SerializeField] private float followSpeed = 8f;
    [SerializeField] private bool snapOnStart = true;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureMainCameraHasFollow()
    {
        Camera camera = Camera.main != null ? Camera.main : FindAnyObjectByType<Camera>();
        if (camera == null || camera.GetComponent<PlayerCameraFollow2D>() != null)
            return;

        camera.gameObject.AddComponent<PlayerCameraFollow2D>();
    }

    private void Awake()
    {
        ResolveTarget();
        if (snapOnStart)
            SnapToTarget();
    }

    private void LateUpdate()
    {
        if (target == null)
            ResolveTarget();
        if (target == null)
            return;

        Vector3 desired = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desired, followSpeed * Time.deltaTime);
    }

    private void ResolveTarget()
    {
        PlayerMovement playerMovement = FindAnyObjectByType<PlayerMovement>(FindObjectsInactive.Exclude);
        if (playerMovement != null)
        {
            target = playerMovement.transform;
            return;
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
            target = playerObject.transform;
    }

    private void SnapToTarget()
    {
        if (target != null)
            transform.position = target.position + offset;
    }
}
