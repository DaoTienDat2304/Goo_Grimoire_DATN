// FirebaseSessionCleaner.cs

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
        if (state != PlayModeStateChange.EnteredEditMode) return;

        try
        {
            var auth = FirebaseAuth.DefaultInstance;
            if (auth?.CurrentUser != null)
            {
                string uid = auth.CurrentUser.UserId;
                auth.SignOut();
                Debug.Log($"[FirebaseSessionCleaner] Signed out uid={uid} on play exit.");
            }
        }
        catch (System.Exception e)
        {
            Debug.Log($"[FirebaseSessionCleaner] Cannot sign out (Firebase not init): {e.Message}");
        }
    }
}
#endif
