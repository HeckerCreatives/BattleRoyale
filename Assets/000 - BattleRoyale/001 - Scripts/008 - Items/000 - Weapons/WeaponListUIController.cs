using Fusion;
using MyBox;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WeaponListUIController : MonoBehaviour
{
    public int Index
    {
        get => transform.GetSiblingIndex();
    }

    //  ======================

    [SerializeField] private Image weaponImg;
    [SerializeField] private TextMeshProUGUI weaponNameTMP;
    [SerializeField] private TextMeshProUGUI qtyTMP;

    [Header("DEBUGGER")]
    [MyBox.ReadOnly][SerializeField] private int index;
    [MyBox.ReadOnly] public NetworkObject noCrateWeaponObj;
    
    public void InitializeData(Sprite weaponSprite, string weaponName, string qty)
    {
        weaponImg.sprite = weaponSprite;
        weaponNameTMP.text = weaponName;
        qtyTMP.text = qty;
    }
}
