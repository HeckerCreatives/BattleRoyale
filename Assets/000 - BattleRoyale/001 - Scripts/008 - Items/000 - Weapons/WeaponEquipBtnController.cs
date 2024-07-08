using MyBox;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WeaponEquipBtnController : MonoBehaviour
{
    [SerializeField] private WeaponSpawnData weaponSpawnData;
    [SerializeField] private Sprite onIndicatorSprite;
    [SerializeField] private Sprite offIndicatorSprite;
    [SerializeField] private Image btnIndicator;
    [SerializeField] private Image weaponImg;
    
    public void SetIndicator(bool isActivated)
    {
        btnIndicator.sprite = isActivated ? onIndicatorSprite : offIndicatorSprite;
    }

    public void ChangeSpriteButton(string itemID)
    {
        weaponImg.sprite = weaponSpawnData.GetItemButtonSprite(itemID);

        if (weaponImg.sprite != null) weaponImg.gameObject.SetActive(true);
        else weaponImg.gameObject.SetActive(false);
    }

    public void ResetUI()
    {
        btnIndicator.sprite = offIndicatorSprite;
        weaponImg.sprite = null;
        weaponImg.gameObject.SetActive(false);
    }
}
