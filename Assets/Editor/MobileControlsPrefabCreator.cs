using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class MobileControlsPrefabCreator
{
    private const string PrefabPath = "Assets/UI/prefab/MobileControlsCanvas.prefab";
    private const string ResourcesPrefabPath = "Assets/Resources/UI/MobileControlsCanvas.prefab";
    private const string SpriteFolder = "Assets/UI/MobileControls";
    private const string CircleSpritePath = SpriteFolder + "/MobileControlCircle.png";

    [MenuItem("Tools/Mobile Controls/Create Joystick Prefab")]
    public static void CreateJoystickPrefab()
    {
        EnsureFolder("Assets/UI");
        EnsureFolder("Assets/UI/prefab");
        EnsureFolder("Assets/Resources");
        EnsureFolder("Assets/Resources/UI");
        EnsureFolder(SpriteFolder);

        Sprite circleSprite = LoadOrCreateCircleSprite();

        var canvasObject = new GameObject("MobileControlsCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        var leftZone = CreateRect("LeftMovementZone", canvasObject.transform, Vector2.zero, new Vector2(0.45f, 0.72f), Vector2.zero, Vector2.zero);
        var leftZoneImage = leftZone.AddComponent<Image>();
        leftZoneImage.color = new Color(0f, 0f, 0f, 0.035f);
        leftZoneImage.raycastTarget = true;
        var joystick = leftZone.AddComponent<VirtualJoystickUI>();

        var joystickBase = CreateCircle("JoystickBase", leftZone.transform, circleSprite, 240f, new Color(1f, 1f, 1f, 0.22f));
        var joystickBaseRect = joystickBase.GetComponent<RectTransform>();
        joystickBaseRect.anchorMin = new Vector2(0.5f, 0.5f);
        joystickBaseRect.anchorMax = new Vector2(0.5f, 0.5f);
        joystickBaseRect.anchoredPosition = new Vector2(-224.64f, -186.62f);

        var joystickRing = CreateCircle("JoystickRing", joystickBase.transform, circleSprite, 172.8f, new Color(1f, 1f, 1f, 0.34f));
        joystickRing.GetComponent<Image>().raycastTarget = false;

        var joystickKnob = CreateCircle("JoystickKnob", joystickBase.transform, circleSprite, 92f, new Color(1f, 1f, 1f, 0.58f));
        joystickKnob.GetComponent<Image>().raycastTarget = false;

        SetObject(joystick, "movementZoneImage", leftZoneImage);
        SetObject(joystick, "joystickBaseImage", joystickBase.GetComponent<Image>());
        SetObject(joystick, "joystickRingImage", joystickRing.GetComponent<Image>());
        SetObject(joystick, "joystickKnobImage", joystickKnob.GetComponent<Image>());

        var rightZone = CreateRect("RightThrowZone", canvasObject.transform, new Vector2(0.55f, 0f), new Vector2(1f, 0.72f), Vector2.zero, Vector2.zero);
        var rightZoneImage = rightZone.AddComponent<Image>();
        rightZoneImage.color = Color.clear;
        rightZoneImage.raycastTarget = true;
        var throwButtonScript = rightZone.AddComponent<MobileThrowButtonUI>();

        var throwButton = CreateCircle("ThrowBallButton", rightZone.transform, circleSprite, 152f, new Color(1f, 1f, 1f, 0.26f));
        throwButton.GetComponent<Image>().raycastTarget = false;
        var throwButtonRect = throwButton.GetComponent<RectTransform>();
        throwButtonRect.anchorMin = new Vector2(0.5f, 0.5f);
        throwButtonRect.anchorMax = new Vector2(0.5f, 0.5f);
        throwButtonRect.anchoredPosition = new Vector2(224.64f, -186.62f);

        var throwIcon = CreateCircle("ThrowBallIcon", throwButton.transform, circleSprite, 96f, new Color(0.75f, 0.95f, 1f, 0.78f));
        throwIcon.GetComponent<Image>().raycastTarget = false;

        SetObject(throwButtonScript, "throwButtonImage", throwButton.GetComponent<Image>());
        SetObject(throwButtonScript, "throwButtonIconImage", throwIcon.GetComponent<Image>());

        PrefabUtility.SaveAsPrefabAsset(canvasObject, PrefabPath);
        PrefabUtility.SaveAsPrefabAsset(canvasObject, ResourcesPrefabPath);
        Object.DestroyImmediate(canvasObject);

        AssetDatabase.Refresh();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
    }

    private static GameObject CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        var obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);

        var rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        rect.localScale = Vector3.one;
        return obj;
    }

    private static GameObject CreateCircle(string name, Transform parent, Sprite sprite, float size, Color color)
    {
        var obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(parent, false);

        var rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(size, size);
        rect.anchoredPosition = Vector2.zero;
        rect.localScale = Vector3.one;

        var image = obj.GetComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.preserveAspect = true;
        image.raycastTarget = true;
        return obj;
    }

    private static Sprite LoadOrCreateCircleSprite()
    {
        var existing = AssetDatabase.LoadAssetAtPath<Sprite>(CircleSpritePath);
        if (existing != null)
            return existing;

        const int size = 128;
        var texture = new Texture2D(size, size, TextureFormat.ARGB32, false);
        var center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.48f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float alpha = Mathf.Clamp01(radius - distance + 1f);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        File.WriteAllBytes(CircleSpritePath, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);

        AssetDatabase.ImportAsset(CircleSpritePath);
        var importer = (TextureImporter)AssetImporter.GetAtPath(CircleSpritePath);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Sprite>(CircleSpritePath);
    }

    private static void SetObject(Object target, string propertyName, Object value)
    {
        var serializedObject = new SerializedObject(target);
        serializedObject.FindProperty(propertyName).objectReferenceValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder))
            return;

        string parent = Path.GetDirectoryName(folder).Replace("\\", "/");
        string name = Path.GetFileName(folder);
        AssetDatabase.CreateFolder(parent, name);
    }
}
