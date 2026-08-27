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
            }
        }
        catch (System.Exception e)
        {
        }
    }
}
#endif
