// FirebaseSessionCleaner.cs
// Sign out Firebase khi thoát play mode trong Editor.
// Ngăn session của dev bị kế thừa khi chạy build trên cùng máy.
// File này chỉ tồn tại trong Editor, không ảnh hưởng build thật.

#if UNITY_EDITOR && FIREBASE_AUTH
using Firebase.Auth;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class FirebaseSessionCleaner
{
    static FirebaseSessionCleaner()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    static void OnPlayModeChanged(PlayModeStateChange state)
    {
        // Chạy khi vừa thoát play mode (sau khi nhấn Stop)
        if (state != PlayModeStateChange.EnteredEditMode) return;

        try
        {
            var auth = FirebaseAuth.DefaultInstance;
            if (auth?.CurrentUser != null)
            {
                string uid = auth.CurrentUser.UserId;
                auth.SignOut();
                Debug.Log($"[FirebaseSessionCleaner] Đã sign out uid={uid} khi thoát play mode.");
            }
        }
        catch (System.Exception e)
        {
            // Firebase có thể chưa init nếu play mode bị stop sớm — bỏ qua
            Debug.Log($"[FirebaseSessionCleaner] Không thể sign out (Firebase chưa init): {e.Message}");
        }
    }
}
#endif
