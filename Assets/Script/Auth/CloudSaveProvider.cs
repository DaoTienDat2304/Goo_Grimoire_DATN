// ============================================================
// CloudSaveProvider.cs
//
//
//
// Firestore structure:
//   users/{uid}  (document)
//     ├─ json      : string   — SaveEnvelope serialized (payload + HMAC sig)
//     ├─ savedAt   : long     — Unix timestamp milliseconds
//
// Save integrity:
// ============================================================

using System;
using System.Collections;
using UnityEngine;

#if FIREBASE_FIRESTORE
using Firebase.Firestore;
using Firebase.Extensions;
#endif

public class CloudSaveProvider : MonoBehaviour
{
    public static CloudSaveProvider Instance { get; private set; }

    [Tooltip("Enable only when you explicitly need to test Firestore cloud saves in the Unity Editor.")]
    public bool useFirestoreInEditor = false;

    public bool HasCloudSave    { get; private set; }
    public bool CloudCheckDone  { get; private set; }

    string _cachedCloudJson;

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else Destroy(gameObject);
    }

    void Start()
    {
        if (AuthManager.Instance == null) return;

        AuthManager.Instance.OnLoginSuccess += uid => StartCoroutine(InitCloudCheck(uid));
        AuthManager.Instance.OnLoggedOut    += ResetCheckState;

        if (AuthManager.Instance.IsLoggedIn)
            StartCoroutine(InitCloudCheck(AuthManager.Instance.CurrentUserId));
    }

    // ── Cloud check ──────────────────────────────────────────
    public IEnumerator InitCloudCheck(string uid)
    {
        HasCloudSave     = false;
        CloudCheckDone   = false;
        _cachedCloudJson = null;

        yield return new WaitUntil(() =>
            RemoteConfigManager.Instance == null || RemoteConfigManager.Instance.IsReady);

#if FIREBASE_FIRESTORE
        if (IsFirestoreDisabledInEditor())
        {
            Debug.LogWarning("[CloudSave] Skipping Firestore load in Editor to avoid Firebase native crash. Using local save only.");
            yield return null;
        }
        else
        {
        string rawJson = null;
        yield return StartCoroutine(LoadFromCloud(uid, (json, _) => rawJson = json));

        if (!string.IsNullOrEmpty(rawJson))
        {
            string gameJson = UnwrapAndVerify(rawJson, uid);
            _cachedCloudJson = gameJson;
            HasCloudSave     = gameJson != null;
        }
        }
#else
        yield return null;
#endif

        if (!HasCloudSave)
        {
            string localId = AuthManager.Instance != null ? AuthManager.Instance.LocalSaveId : "guest";
            string localJson = LocalSaveStore.Load(localId);
            if (!string.IsNullOrEmpty(localJson))
            {
                _cachedCloudJson = localJson;
                HasCloudSave     = true;
                Debug.Log("[CloudSave] No cloud save. Using local save.");
            }
        }

        CloudCheckDone = true;
        Debug.Log($"[CloudSave] InitCloudCheck xong. HasCloudSave={HasCloudSave}");
    }

    public string GetCachedJson() => _cachedCloudJson;

    public void ResetCheckState()
    {
        HasCloudSave     = false;
        CloudCheckDone   = false;
        _cachedCloudJson = null;
        Debug.Log("[CloudSave] Cloud check reset.");
    }

    // ── Save ─────────────────────────────────────────────────
    public void StartSave(string uid, string json)
    {
        if (!string.IsNullOrEmpty(json))
        {
            _cachedCloudJson = json;
            HasCloudSave = true;
        }
        StartCoroutine(SaveToCloud(uid, json));
    }
    public IEnumerator SaveToCloud(string uid, string json)
    {
#if FIREBASE_FIRESTORE
        if (IsFirestoreDisabledInEditor())
        {
            Debug.Log("[CloudSave] Firestore save skipped in Editor. Local save is already written.");
            yield break;
        }

        if (string.IsNullOrEmpty(uid) || string.IsNullOrEmpty(json)) yield break;

        var envelope = new SaveEnvelope
        {
            payload       = json,
            sig           = SaveIntegrity.Sign(json, uid),
            schemaVersion = 1
        };
        string envelopeJson = JsonUtility.ToJson(envelope);

        bool done = false;
        string error = null;

        var db  = FirebaseFirestore.DefaultInstance;
        var doc = db.Collection("users").Document(uid);

        var saveData = new System.Collections.Generic.Dictionary<string, object>
        {
            { "json",       envelopeJson },
            { "savedAt",    DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() },
            { "appVersion", Application.version }
        };

        doc.SetAsync(saveData).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
                error = task.Exception?.InnerException?.Message ?? "Unknown error";
            done = true;
        });

        yield return new WaitUntil(() => done);

        if (error != null)
            Debug.LogWarning($"[CloudSave] ✗ Cloud save failed: {error}");
        else
        {
            Debug.Log("[CloudSave] ✓ Saved to Firestore (HMAC).");
            _cachedCloudJson = json;
            HasCloudSave = true;
        }
#else
        Debug.Log("[CloudSave] Firestore off. Skip cloud save.");
        yield break;
#endif
    }

    // ── Load ─────────────────────────────────────────────────
    public IEnumerator LoadFromCloud(string uid, Action<string, long> onComplete)
    {
#if FIREBASE_FIRESTORE
        if (IsFirestoreDisabledInEditor())
        {
            onComplete?.Invoke(null, 0);
            yield break;
        }

        if (string.IsNullOrEmpty(uid)) { onComplete?.Invoke(null, 0); yield break; }

        bool   done    = false;
        string rawJson = null;
        long   savedAt = 0;

        var db  = FirebaseFirestore.DefaultInstance;
        var doc = db.Collection("users").Document(uid);

        doc.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogWarning($"[CloudSave] ✗ Cloud load failed: {task.Exception?.InnerException?.Message}");
            }
            else if (task.Result.Exists)
            {
                rawJson = task.Result.GetValue<string>("json");
                if (task.Result.TryGetValue("savedAt", out long ts)) savedAt = ts;
                Debug.Log($"[CloudSave] ✓ Cloud load OK. savedAt={savedAt}");
            }
            else
            {
                Debug.Log("[CloudSave] No cloud save data.");
            }
            done = true;
        });

        yield return new WaitUntil(() => done);
        onComplete?.Invoke(rawJson, savedAt);
#else
        Debug.Log("[CloudSave] Firestore off.");
        onComplete?.Invoke(null, 0);
        yield break;
#endif
    }

    // ── HMAC Integrity ───────────────────────────────────────
    string UnwrapAndVerify(string raw, string uid)
    {
        var envelope = JsonUtility.FromJson<SaveEnvelope>(raw);

        if (envelope != null && !string.IsNullOrEmpty(envelope.payload))
        {
            if (SaveIntegrity.Verify(envelope.payload, uid, envelope.sig))
            {
                Debug.Log("[CloudSave] ✓ HMAC valid.");
                return envelope.payload;
            }

            Debug.LogWarning("[CloudSave] ✗ Invalid HMAC. Load rejected.");
            return null;
        }

        Debug.LogWarning("[CloudSave] Unsigned save. Load rejected.");
        return null;
    }

    bool IsFirestoreDisabledInEditor()
    {
#if UNITY_EDITOR
        return !useFirestoreInEditor;
#else
        return false;
#endif
    }
}
