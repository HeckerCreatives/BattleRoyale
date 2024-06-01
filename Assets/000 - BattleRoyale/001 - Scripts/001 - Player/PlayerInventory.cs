using Fusion;
using MyBox;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public int WeaponIndex
    {
        get => weaponIndex;
        set => weaponIndex = value;
    }

    [Networked][field: MyBox.ReadOnly][field: SerializeField] public GameplayController Controller { get; set; }

    //  ==========================

    [SerializeField] private UserData userData;
    [SerializeField] private Animator animator;

    [Header("SKINS")]
    [SerializeField] private List<GameObject> hairStyles;
    [SerializeField] private List<MeshRenderer> hairMR;
    [SerializeField] private SkinnedMeshRenderer bodyColorMR;
    [SerializeField] private SkinnedMeshRenderer upperClothingMR;
    [SerializeField] private SkinnedMeshRenderer lowerClothingMR;

    [Header("SKIN COLOR")]
    [SerializeField] private List<Color> hairColor;
    [SerializeField] private List<Color> clothingColor;
    [SerializeField] private List<Color> skinColor;

    [Space]
    [SerializeField] private Transform primaryWeaponSlot;
    [SerializeField] private Transform secondaryWeaponSlot;
    [SerializeField] private Transform trapWeaponSlot;
    [SerializeField] private List<GameObject> primaryWeapons;
    [SerializeField] private List<GameObject> secondaryWeapons;
    [SerializeField] private List<GameObject> trapWeapons;

    [Header("DEBUGGER")]
    [MyBox.ReadOnly][SerializeField] private int weaponIndex;
    [MyBox.ReadOnly][SerializeField] private int tempLastIndex;
    [MyBox.ReadOnly][SerializeField] private int hairStyleIndex;
    [MyBox.ReadOnly][SerializeField] private int hairColorIndex;
    [MyBox.ReadOnly][SerializeField] private int clothingColorIndex;
    [MyBox.ReadOnly][SerializeField] private int skinColorIndex;

    private void Awake()
    {
        weaponIndex = 1; 
        InitializeCharacterSettings(userData.CharacterSetting.hairstyle, userData.CharacterSetting.haircolor, userData.CharacterSetting.clothingcolor, userData.CharacterSetting.skincolor);
    }


    private void Update()
    {
        SetHandsItem();
        SetPrimaryItem();
        AnimateSwitch();
    }

    public void InitializeCharacterSettings(int hairStyleIndex, int hairColorIndex, int clothingColorIndex, int skinColorIndex)
    {
        this.hairStyleIndex = hairStyleIndex;
        this.hairColorIndex = hairColorIndex;
        this.clothingColorIndex = clothingColorIndex;
        this.skinColorIndex = skinColorIndex;

        hairStyles[hairStyleIndex].SetActive(true);

        hairMR[hairStyleIndex].material.SetColor("_BaseColor", hairColor[hairColorIndex]);
        upperClothingMR.material.SetColor("_BaseColor", clothingColor[clothingColorIndex]);
        lowerClothingMR.materials[0].SetColor("_BaseColor", clothingColor[clothingColorIndex]);
        lowerClothingMR.materials[1].SetColor("_BaseColor", clothingColor[clothingColorIndex]);
        bodyColorMR.material.SetColor("_BaseColor", skinColor[skinColorIndex]);
    }

    private void AnimateSwitch()
    {
        animator.SetLayerWeight(tempLastIndex, Mathf.Lerp(animator.GetLayerWeight(tempLastIndex), 0f, Time.deltaTime * 13f));
        animator.SetLayerWeight(weaponIndex, Mathf.Lerp(animator.GetLayerWeight(weaponIndex), 1f, Time.deltaTime * 13f));
    }

    private void SetHandsItem()
    {
        if (!Controller.SwitchHands) return;

        if (weaponIndex == 1) return;

        tempLastIndex = weaponIndex;
        weaponIndex = 1;

        animator.SetTrigger("switchweapon");

        Controller.SwitchHandsStop();
    }

    private void SetPrimaryItem()
    {
        if (!Controller.SwitchPrimary) return;

        if (weaponIndex == 2) return;

        tempLastIndex = weaponIndex;
        weaponIndex = 2;

        animator.SetTrigger("switchweapon");

        Controller.SwitchPrimaryStop();
    }

    public void ResetTriggerSwitch() => animator.ResetTrigger("switchweapon");
}
