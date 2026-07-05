// ============================================================
// CloudSaveProvider.cs
//
// Define symbol cần có: FIREBASE_FIRESTORE
//
// Không có symbol → Load/Save chỉ dùng local file (hành vi dev offline).
//
// Firestore structure:
//   users/{uid}  (document)
//     ├─ json      : string   — SaveEnvelope serialized (payload + HMAC sig)
//     ├─ savedAt   : long     — Unix timestamp milliseconds
//     └─ appVersion: string   — để debug tương thích dữ liệu
//
// Save integrity:
//   Mỗi save được bọc trong SaveEnvelope với chữ ký HMAC-SHA256.
//   Key = SHA256(uid + salt), salt lấy từ Firebase Remote Config.
//   Legacy save (không có envelope) được chấp nhận 1 lần rồi ký lại.
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

    // ── Trạng thái cloud check ──────────────────────────────
    /// <summary>true nếu tài khoản hiện tại đã có save data trên cloud (và hợp lệ).</summary>
    public bool HasCloudSave    { get; private set; }
    /// <summary>true sau khi InitCloudCheck() hoàn tất.</summary>
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

        // Offline dev mode: ApplyOfflineUser() gọi trong Awake nên event đã fire trước Start
        if (AuthManager.Instance.IsLoggedIn)
            StartCoroutine(InitCloudCheck(AuthManager.Instance.CurrentUserId));
    }

    // ── Cloud check ──────────────────────────────────────────
    /// <summary>
    /// Kiểm tra cloud save của uid. Tự chạy khi login.
    /// Chờ RemoteConfig sẵn sàng để dùng salt đúng khi verify HMAC.
    /// Kết quả cache trong HasCloudSave / GetCachedJson().
    /// </summary>
    public IEnumerator InitCloudCheck(string uid)
    {
        HasCloudSave     = false;
        CloudCheckDone   = false;
        _cachedCloudJson = null;

        // Chờ RemoteConfig để có salt trước khi verify
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

        // Fallback: cloud không có save hợp lệ (hoặc đang offline / dev mode) nhưng máy
        // có save cục bộ (PlayerPrefs) → dùng nó. Nhờ vậy guest/offline vẫn "Continue" được,
        // không bị coi là game mới và bắt chơi lại tutorial từ đầu.
        if (!HasCloudSave)
        {
            string localId = AuthManager.Instance != null ? AuthManager.Instance.LocalSaveId : "guest";
            string localJson = LocalSaveStore.Load(localId);
            if (!string.IsNullOrEmpty(localJson))
            {
                _cachedCloudJson = localJson;
                HasCloudSave     = true;
                Debug.Log("[CloudSave] Không có cloud save — dùng save cục bộ (PlayerPrefs).");
            }
        }

        CloudCheckDone = true;
        Debug.Log($"[CloudSave] InitCloudCheck xong. HasCloudSave={HasCloudSave}");
    }

    /// <summary>JSON save data đã tải về và xác minh, dùng được ngay.</summary>
    public string GetCachedJson() => _cachedCloudJson;

    /// <summary>Reset trạng thái khi logout — bắt buộc check lại khi login tiếp.</summary>
    public void ResetCheckState()
    {
        HasCloudSave     = false;
        CloudCheckDone   = false;
        _cachedCloudJson = null;
        Debug.Log("[CloudSave] Đã reset trạng thái cloud check.");
    }

    // ── Save ─────────────────────────────────────────────────
    /// <summary>
    /// Khởi chạy SaveToCloud trên CloudSaveProvider (DontDestroyOnLoad) để coroutine
    /// không bị interrupt khi scene thay đổi.
    /// </summary>
    public void StartSave(string uid, string json)
    {
        StartCoroutine(SaveToCloud(uid, json));
    }

    /// <summary>
    /// Ký JSON rồi lưu lên Firestore dưới dạng SaveEnvelope (fire-and-forget).
    /// </summary>
    public IEnumerator SaveToCloud(string uid, string json)
    {
#if FIREBASE_FIRESTORE
        if (IsFirestoreDisabledInEditor())
        {
            Debug.Log("[CloudSave] Firestore save skipped in Editor. Local save is already written.");
            yield break;
        }

        if (string.IsNullOrEmpty(uid) || string.IsNullOrEmpty(json)) yield break;

        // Bọc game JSON trong envelope có chữ ký HMAC
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
                error = task.Exception?.InnerException?.Message ?? "Lỗi không xác định";
            done = true;
        });

        yield return new WaitUntil(() => done);

        if (error != null)
            Debug.LogWarning($"[CloudSave] ✗ Lưu cloud thất bại: {error}");
        else
        {
            Debug.Log("[CloudSave] ✓ Đã lưu lên Firestore (có chữ ký HMAC).");
            _cachedCloudJson = json;
            HasCloudSave = true;
        }
#else
        Debug.Log("[CloudSave] Firestore chưa bật — bỏ qua cloud save.");
        yield break;
#endif
    }

    // ── Load ─────────────────────────────────────────────────
    /// <summary>
    /// Tải raw string từ Firestore về (chưa verify — verify trong UnwrapAndVerify).
    /// Trả về (rawJson, savedAt). rawJson = null nếu không có dữ liệu hoặc lỗi.
    /// </summary>
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
                Debug.LogWarning($"[CloudSave] ✗ Tải cloud thất bại: {task.Exception?.InnerException?.Message}");
            }
            else if (task.Result.Exists)
            {
                rawJson = task.Result.GetValue<string>("json");
                if (task.Result.TryGetValue("savedAt", out long ts)) savedAt = ts;
                Debug.Log($"[CloudSave] ✓ Tải cloud thành công. savedAt={savedAt}");
            }
            else
            {
                Debug.Log("[CloudSave] Chưa có save data trên cloud.");
            }
            done = true;
        });

        yield return new WaitUntil(() => done);
        onComplete?.Invoke(rawJson, savedAt);
#else
        Debug.Log("[CloudSave] Firestore chưa bật.");
        onComplete?.Invoke(null, 0);
        yield break;
#endif
    }

    // ── HMAC Integrity ───────────────────────────────────────
    /// <summary>
    /// Parse raw Firestore string thành game JSON, đồng thời verify chữ ký.
    /// - Envelope hợp lệ + sig đúng → trả về payload
    /// - Envelope hợp lệ + sig sai  → trả về null (reject)
    /// - Legacy save (không có envelope) → chấp nhận, log warning
    /// </summary>
    string UnwrapAndVerify(string raw, string uid)
    {
        var envelope = JsonUtility.FromJson<SaveEnvelope>(raw);

        if (envelope != null && !string.IsNullOrEmpty(envelope.payload))
        {
            if (SaveIntegrity.Verify(envelope.payload, uid, envelope.sig))
            {
                Debug.Log("[CloudSave] ✓ Chữ ký HMAC hợp lệ.");
                return envelope.payload;
            }

            Debug.LogWarning("[CloudSave] ✗ Chữ ký HMAC KHÔNG hợp lệ — từ chối load.");
            return null;
        }

        // Không có envelope → save cũ hoặc dữ liệu lạ, reject
        Debug.LogWarning("[CloudSave] Save không có chữ ký — từ chối load.");
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
