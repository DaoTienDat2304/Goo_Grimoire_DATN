using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>
/// </summary>
public static class MobileTilemapOptimizer
{
    private const int CachedChunkCount = 32;
    private const int CachedFrameAge = 120;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        ApplyToLoadedTilemaps();
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyToLoadedTilemaps();
    }

    private static void ApplyToLoadedTilemaps()
    {
        TilemapRenderer[] renderers = Object.FindObjectsByType<TilemapRenderer>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < renderers.Length; i++)
        {
            TilemapRenderer tilemapRenderer = renderers[i];
            if (tilemapRenderer == null)
                continue;

            tilemapRenderer.maxChunkCount = Mathf.Max(tilemapRenderer.maxChunkCount, CachedChunkCount);
            tilemapRenderer.maxFrameAge = Mathf.Max(tilemapRenderer.maxFrameAge, CachedFrameAge);
        }

        OptimizeObstacleColliders();
        OptimizePlayerPhysics();
    }

    /// <summary>
    /// </summary>
    private static void OptimizeObstacleColliders()
    {
        TilemapCollider2D[] tilemapColliders = Object.FindObjectsByType<TilemapCollider2D>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < tilemapColliders.Length; i++)
        {
            TilemapCollider2D tilemapCollider = tilemapColliders[i];
            if (tilemapCollider == null || tilemapCollider.gameObject.layer != LayerMask.NameToLayer("obstacle"))
                continue;

            GameObject obstacleObject = tilemapCollider.gameObject;
            Rigidbody2D body = obstacleObject.GetComponent<Rigidbody2D>();
            if (body == null)
                body = obstacleObject.AddComponent<Rigidbody2D>();

            body.bodyType = RigidbodyType2D.Static;
            body.simulated = true;
            body.useFullKinematicContacts = false;
            body.interpolation = RigidbodyInterpolation2D.None;
            body.collisionDetectionMode = CollisionDetectionMode2D.Discrete;

            CompositeCollider2D composite = obstacleObject.GetComponent<CompositeCollider2D>();
            if (composite == null)
                composite = obstacleObject.AddComponent<CompositeCollider2D>();

            composite.geometryType = CompositeCollider2D.GeometryType.Outlines;
            composite.vertexDistance = Mathf.Max(composite.vertexDistance, 0.02f);
            composite.edgeRadius = Mathf.Max(composite.edgeRadius, 0.01f);
            composite.callbackLayers = 0;
            composite.contactCaptureLayers = 0;

            tilemapCollider.compositeOperation = Collider2D.CompositeOperation.Merge;
            tilemapCollider.callbackLayers = 0;
            tilemapCollider.contactCaptureLayers = 0;

            composite.GenerateGeometry();
        }
    }

    private static void OptimizePlayerPhysics()
    {
        PlayerMovement[] players = Object.FindObjectsByType<PlayerMovement>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);

        for (int i = 0; i < players.Length; i++)
        {
            PlayerMovement player = players[i];
            if (player == null)
                continue;

            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
            if (body != null)
            {
                body.interpolation = RigidbodyInterpolation2D.Interpolate;
                body.collisionDetectionMode = CollisionDetectionMode2D.Discrete;
                body.sleepMode = RigidbodySleepMode2D.StartAwake;
            }

            Collider2D[] colliders = player.GetComponents<Collider2D>();
            for (int c = 0; c < colliders.Length; c++)
            {
                colliders[c].callbackLayers = 0;
                colliders[c].contactCaptureLayers = 0;
            }
        }
    }
}
