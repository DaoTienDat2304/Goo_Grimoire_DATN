using UnityEngine;

/// <summary>
/// Legacy marker retained so existing scene and prefab references remain valid.
/// UI layout is authored in the Unity editor; this component must not rewrite it.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Canvas))]
public sealed class CanvasSafeArea : MonoBehaviour
{
}
