using UnityEngine;

public class viewslime : MonoBehaviour
{

    public GameObject slimeBody;
    public GameObject SlimeArmor;
    public GameObject SlimeWeapon;
    private Slime slime;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public void SetupSlime(Slime newSlime)
    {
        slime = newSlime;
        UIupdate();
    }

    private void UIupdate()
    {
        var bodyRenderer = slimeBody?.GetComponent<SpriteRenderer>();
        var armorRenderer = SlimeArmor?.GetComponent<SpriteRenderer>();
        var weaponRenderer = SlimeWeapon?.GetComponent<SpriteRenderer>();

        var bodyImage = slimeBody?.GetComponent<UnityEngine.UI.Image>();
        var armorImage = SlimeArmor?.GetComponent<UnityEngine.UI.Image>();
        var weaponImage = SlimeWeapon?.GetComponent<UnityEngine.UI.Image>();

        if (bodyRenderer != null)
        {
            bodyRenderer.transform.localScale = Vector3.one * 30;
            bodyRenderer.sprite = (slime != null ? slime.body?.sprite : null);
            bodyRenderer.sortingOrder = 200;
        }
        else if (bodyImage != null)
        {
            bodyImage.sprite = (slime != null ? slime.body?.sprite : null);
        }

        if (armorRenderer != null)
        {
            armorRenderer.transform.localScale = Vector3.one;
            armorRenderer.sprite = (slime != null ? slime.armor?.sprite : null);
            armorRenderer.sortingOrder = 201;
        }
        else if (armorImage != null)
        {
            armorImage.sprite = (slime != null ? slime.armor?.sprite : null);
        }

        if (weaponRenderer != null)
        {
            weaponRenderer.transform.localScale = Vector3.one;
            weaponRenderer.sprite = (slime != null ? slime.weapon?.sprite : null);
            weaponRenderer.sortingOrder = 202;
        }
        else if (weaponImage != null)
        {
            weaponImage.sprite = (slime != null ? slime.weapon?.sprite : null);
        }
    }

    public void deactive()
    {
        gameObject.SetActive(false);
    }

  
}
