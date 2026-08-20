using UnityEngine;
using Spine.Unity;

public class SlimeAnimationController : MonoBehaviour
{
    [Header("Animation Settings")]
    public SkeletonDataAsset animationAsset;
    public string animationName = "animation";
    public bool loop = true;
    public float timeScale = 2f;
    
    private SkeletonAnimation skeletonAnimation;
    private SpriteRenderer spriteRenderer;
    
    public void Initialize(SkeletonDataAsset asset, string animName = "animation")
    {
        animationAsset = asset;
        animationName = animName;
        
        if (animationAsset != null)
        {
            SetupAnimation();
        }
        else
        {
            SetupSpriteRenderer();
        }
    }
    
    private void SetupAnimation()
    {
        if (spriteRenderer != null)
        {
            DestroyImmediate(spriteRenderer);
            spriteRenderer = null;
        }
        
        skeletonAnimation = GetComponent<SkeletonAnimation>();
        if (skeletonAnimation == null)
        {
            skeletonAnimation = gameObject.AddComponent<SkeletonAnimation>();
        }
        
        skeletonAnimation.skeletonDataAsset = animationAsset;
        skeletonAnimation.AnimationName = animationName;
        skeletonAnimation.loop = loop;
        skeletonAnimation.timeScale = timeScale;
        skeletonAnimation.transform.localScale = Vector3.one;
    }
    
    private void SetupSpriteRenderer()
    {
        if (skeletonAnimation != null)
        {
            DestroyImmediate(skeletonAnimation);
            skeletonAnimation = null;
        }
        
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }
    }
    
    public void SetSprite(Sprite sprite)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = sprite;
        }
        if (skeletonAnimation != null && spriteRenderer != null)
        {
            Color c = spriteRenderer.color;
            c.a = 0f;
            spriteRenderer.color = c;
        }
    }
    
    public void PlayAnimation(string animName)
    {
        if (skeletonAnimation != null && skeletonAnimation.AnimationState != null)
        {
            skeletonAnimation.AnimationState.SetAnimation(0, animName, loop);
        }
    }
    
    public void StopAnimation()
    {
        if (skeletonAnimation != null && skeletonAnimation.AnimationState != null)
        {
            skeletonAnimation.AnimationState.ClearTrack(0);
        }
    }
}


