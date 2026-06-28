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
        bodyRenderer.transform.localScale = Vector3.one * 30;
        armorRenderer.transform.localScale = Vector3.one;
        weaponRenderer.transform.localScale = Vector3.one;
        bodyRenderer.sprite = (slime != null ? slime.body?.sprite : null);
        bodyRenderer.sortingOrder = 2;
        armorRenderer.sprite = (slime != null ? slime.armor?.sprite : null);
        armorRenderer.sortingOrder = 3;
        weaponRenderer.sprite = (slime != null ? slime.weapon?.sprite : null);
        weaponRenderer.sortingOrder = 4;
    }

    public void deactive()
    {
        
    }

  
}
