using MyBox;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public enum CustomizationState
{
    HAIRSTYLE,
    HAIRCOLOR,
    CLOTHES,
    SKIN
}

public class CharacterCreationController : MonoBehaviour
{
    private event EventHandler CustomizationStateChange;
    public event EventHandler OnCustomizationStateChange
    {
        add
        {
            if (CustomizationStateChange == null || !CustomizationStateChange.GetInvocationList().Contains(value))
                CustomizationStateChange += value;
        }
        remove { CustomizationStateChange -= value; }
    }
    public CustomizationState CurrentCustomizationState
    {
        get => customizationState;
        set
        {
            customizationState = value;
            CustomizationStateChange?.Invoke(this, EventArgs.Empty);
        }
    }

    //  ==================

    [SerializeField] private UserData userData;
    [SerializeField] private RectTransform customizerTF;
    [SerializeField] private LeanTweenType easeType;
    [SerializeField] private float easeDuration;

    [Space]
    [SerializeField] private GameObject profileContainer;
    [SerializeField] private GameObject colorSchemeContainer;

    [Header("SPRITES")]
    [SerializeField] private List<Sprite> hairStyleList;
    [SerializeField] private List<Sprite> hairColorList;
    [SerializeField] private List<Sprite> clothingColorList;
    [SerializeField] private List<Sprite> skinColorList;

    [Header("HAIR")]
    [SerializeField] private List<GameObject> hairStyles;
    [SerializeField] private List<SkinnedMeshRenderer> hairMR;
    [SerializeField] private List<SkinnedMeshRenderer> hatMR;
    [SerializeField] private List<GameObject> profileHairStyles;
    [SerializeField] private List<SkinnedMeshRenderer> profileHairMR;
    [SerializeField] private List<SkinnedMeshRenderer> profileHatMR;

    //[Header("IMAGES")]
    //[SerializeField] private Image hairStyleImg;
    //[SerializeField] private Image hairColorImg;
    //[SerializeField] private Image clothingColorImg;
    //[SerializeField] private Image skinColorImg;

    [Header("COLOR")]
    [SerializeField] private List<Color> hairColor;
    [SerializeField] private List<ColorSchemes> clothingColors;
    [SerializeField] private List<Color> skinColor;

    [Header("CUSTOMIZER ACTIVATE INDICATOR")]
    [SerializeField] private List<Image> hairStyleIndicator;
    [SerializeField] private List<Image> hairColorIndicator;
    [SerializeField] private List<Image> clotheColorIndicator;
    [SerializeField] private List<Image> skinColorIndicator;

    [Header("PROFILE CUSTOMIZATION")]
    [SerializeField] private GameObject hairstyleContainerObj;
    [SerializeField] private GameObject hairColorContainerObj;
    [SerializeField] private GameObject clotheColorContainerObj;
    [SerializeField] private GameObject skinColorContainerObj;

    [Header("CHARACTER")]
    [SerializeField] private SkinnedMeshRenderer bodyColorMR;
    [SerializeField] private List<SkinnedMeshRenderer> clothingMRList;
    [SerializeField] private SkinnedMeshRenderer profileBodyColorMR;
    [SerializeField] private List<SkinnedMeshRenderer> profileClothingMRList;


    [Header("BUTTONS")]
    [SerializeField] private Button saveBtn;

    [Header("DEBUGGER")]
    [SerializeField] private CustomizationState customizationState;
    [ReadOnly][SerializeField] private bool customIsOn;
    [ReadOnly][SerializeField] private int hairStyleIndex;
    [ReadOnly][SerializeField] private int hairColorIndex;
    [ReadOnly][SerializeField] private int clothingColorIndex;
    [ReadOnly][SerializeField] private int skinColorIndex;
    [SerializeField] private bool canSaveCustomization;

    //  =======================

    int customizerLT;

    //  =======================

    private void OnEnable()
    {
        CheckCustomizationState();
        OnCustomizationStateChange += CustomStateChange;
    }

    private void OnDisable()
    {
        OnCustomizationStateChange -= CustomStateChange;
    }

    private void CustomStateChange(object sender, EventArgs e)
    {
        CheckCustomizationState();
    }

    private void CheckCustomizationState()
    {
        hairstyleContainerObj.SetActive(customizationState == CustomizationState.HAIRSTYLE);
        hairColorContainerObj.SetActive(customizationState == CustomizationState.HAIRCOLOR);
        clotheColorContainerObj.SetActive(customizationState == CustomizationState.CLOTHES);
        skinColorContainerObj.SetActive(customizationState == CustomizationState.SKIN);
    }

    public void InitializeCharacterSettings(int hairStyleIndex, int hairColorIndex, int clothingColorIndex, int skinColorIndex)
    {
        this.hairStyleIndex = hairStyleIndex;
        this.hairColorIndex = hairColorIndex;
        this.clothingColorIndex = clothingColorIndex;
        this.skinColorIndex = skinColorIndex;

        //hairStyleImg.sprite = hairStyleList[hairStyleIndex];
        //hairColorImg.sprite = hairColorList[hairColorIndex];
        //clothingColorImg.sprite = clothingColorList[clothingColorIndex];
        //skinColorImg.sprite = skinColorList[skinColorIndex];

        hairStyles[hairStyleIndex].SetActive(true);
        profileHairStyles[hairStyleIndex].SetActive(true);

        hairStyleIndicator[hairStyleIndex].enabled = true;
        hairColorIndicator[hairColorIndex].enabled = true;
        clotheColorIndicator[clothingColorIndex].enabled = true;
        skinColorIndicator[skinColorIndex].enabled = true;

        hairMR[hairStyleIndex].material.SetColor("_BaseColor", hairColor[hairColorIndex]);

        for (int a = 0; a < clothingColors[clothingColorIndex].Colors.Count; a++)
            clothingMRList[a].material.SetColor("_BaseColor", clothingColors[clothingColorIndex].Colors[a]);

        for (int a = 0; a < hatMR.Count; a++)
            hatMR[a].material.SetColor("_BaseColor", clothingColors[clothingColorIndex].Colors[1]);

        bodyColorMR.material.SetColor("_BaseColor", skinColor[skinColorIndex]);

        profileHairMR[hairStyleIndex].material.SetColor("_BaseColor", hairColor[hairColorIndex]);

        for (int a = 0; a < clothingColors[clothingColorIndex].Colors.Count; a++)
            profileClothingMRList[a].material.SetColor("_BaseColor", clothingColors[clothingColorIndex].Colors[a]);

        for (int a = 0; a < profileHatMR.Count; a++)
            profileHatMR[a].material.SetColor("_BaseColor", clothingColors[clothingColorIndex].Colors[1]);

        profileBodyColorMR.material.SetColor("_BaseColor", skinColor[skinColorIndex]);

        CheckSettingsForSaveButton();
    }

    private void CheckSettingsForSaveButton()
    {
        if (hairStyleIndex != userData.CharacterSetting.hairstyle || hairColorIndex != userData.CharacterSetting.haircolor || clothingColorIndex != userData.CharacterSetting.clothingcolor || skinColorIndex != userData.CharacterSetting.skincolor)
            canSaveCustomization = true;
        else
            canSaveCustomization = false;

        saveBtn.interactable = canSaveCustomization;
    }

    public void ResetCharacterCreationForOpen()
    {
        canSaveCustomization = false;
        CheckSettingsForSaveButton();

        profileContainer.SetActive(false);
        colorSchemeContainer.SetActive(true);
    }

    public void CloseCustomization()
    {
        CheckSettingsForSaveButton();

        if (canSaveCustomization)
            ResetCustomization();

        profileContainer.SetActive(true);
        colorSchemeContainer.SetActive(false);
    }

    private void ResetCustomization()
    {
        hairStyles[hairStyleIndex].SetActive(false);
        profileHairStyles[hairStyleIndex].SetActive(false);

        hairStyleIndicator[hairStyleIndex].enabled = false;
        hairColorIndicator[hairColorIndex].enabled = false;
        clotheColorIndicator[clothingColorIndex].enabled = false;
        skinColorIndicator[skinColorIndex].enabled = false;

        hairStyleIndex = userData.CharacterSetting.hairstyle;
        hairColorIndex = userData.CharacterSetting.haircolor;
        clothingColorIndex = userData.CharacterSetting.clothingcolor;
        skinColorIndex = userData.CharacterSetting.skincolor;

        hairStyles[hairStyleIndex].SetActive(true);
        profileHairStyles[hairStyleIndex].SetActive(true);

        hairStyleIndicator[hairStyleIndex].enabled = true;
        hairColorIndicator[hairColorIndex].enabled = true;
        clotheColorIndicator[clothingColorIndex].enabled = true;
        skinColorIndicator[skinColorIndex].enabled = true;

        hairMR[hairStyleIndex].material.SetColor("_BaseColor", hairColor[hairColorIndex]);

        for (int a = 0; a < clothingColors[clothingColorIndex].Colors.Count; a++)
            clothingMRList[a].material.SetColor("_BaseColor", clothingColors[clothingColorIndex].Colors[a]);

        for (int a = 0; a < hatMR.Count; a++)
            hatMR[a].material.SetColor("_BaseColor", clothingColors[clothingColorIndex].Colors[1]);

        bodyColorMR.material.SetColor("_BaseColor", skinColor[skinColorIndex]);

        profileHairMR[hairStyleIndex].material.SetColor("_BaseColor", hairColor[hairColorIndex]);

        for (int a = 0; a < clothingColors[clothingColorIndex].Colors.Count; a++)
            profileClothingMRList[a].material.SetColor("_BaseColor", clothingColors[clothingColorIndex].Colors[a]);

        for (int a = 0; a < profileHatMR.Count; a++)
            profileHatMR[a].material.SetColor("_BaseColor", clothingColors[clothingColorIndex].Colors[1]);

        profileBodyColorMR.material.SetColor("_BaseColor", skinColor[skinColorIndex]);
    }

    public void ChangeHairStyle(int index)
    {
        hairStyles[hairStyleIndex].SetActive(false);
        profileHairStyles[hairStyleIndex].SetActive(false);
        hairStyleIndicator[hairStyleIndex].enabled = false;

        //if (isNext)
        //{
        //    if (hairStyleIndex >= hairStyleList.Count - 1)
        //        hairStyleIndex = 0;
        //    else
        //        hairStyleIndex++;
        //}
        //else
        //{
        //    if (hairStyleIndex <= 0)
        //        hairStyleIndex = hairStyleList.Count - 1;
        //    else
        //        hairStyleIndex--;
        //}

        hairStyleIndex = index;

        hairStyleIndicator[hairStyleIndex].enabled = true;
        CheckSettingsForSaveButton();

        hairStyles[hairStyleIndex].SetActive(true);
        hairMR[hairStyleIndex].material.SetColor("_BaseColor", hairColor[hairColorIndex]);
        //hairStyleImg.sprite = hairStyleList[hairStyleIndex];

        profileHairStyles[hairStyleIndex].SetActive(true);
        profileHairMR[hairStyleIndex].material.SetColor("_BaseColor", hairColor[hairColorIndex]);
    }


    public void ChangeHairColor(int index)
    {
        hairColorIndicator[hairColorIndex].enabled = false;

        //if (isNext)
        //{
        //    if (hairColorIndex >= hairColorList.Count - 1)
        //        hairColorIndex = 0;
        //    else
        //        hairColorIndex++;
        //}
        //else
        //{
        //    if (hairColorIndex <= 0)
        //        hairColorIndex = hairColorList.Count - 1;
        //    else
        //        hairColorIndex--;
        //}

        hairColorIndex = index;
        hairColorIndicator[hairColorIndex].enabled = true;

        CheckSettingsForSaveButton();
        hairMR[hairStyleIndex].material.SetColor("_BaseColor", hairColor[hairColorIndex]);
        //hairColorImg.sprite = hairColorList[hairColorIndex];
        profileHairMR[hairStyleIndex].material.SetColor("_BaseColor", hairColor[hairColorIndex]);
    }

    public void ChangeClothingColor(int index)
    {
        clotheColorIndicator[clothingColorIndex].enabled = false;

        //if (isNext)
        //{
        //    if (clothingColorIndex >= clothingColorList.Count - 1)
        //        clothingColorIndex = 0;
        //    else
        //        clothingColorIndex++;
        //}
        //else
        //{
        //    if (clothingColorIndex <= 0)
        //        clothingColorIndex = clothingColorList.Count - 1;
        //    else
        //        clothingColorIndex--;
        //}

        clothingColorIndex = index;

        clotheColorIndicator[clothingColorIndex].enabled = true;
        CheckSettingsForSaveButton();

        for (int a = 0; a < clothingColors[clothingColorIndex].Colors.Count; a++)
            clothingMRList[a].material.SetColor("_BaseColor", clothingColors[clothingColorIndex].Colors[a]);

        for (int a = 0; a < hatMR.Count; a++)
            hatMR[a].material.SetColor("_BaseColor", clothingColors[clothingColorIndex].Colors[1]);

        //clothingColorImg.sprite = clothingColorList[clothingColorIndex];

        for (int a = 0; a < clothingColors[clothingColorIndex].Colors.Count; a++)
            profileClothingMRList[a].material.SetColor("_BaseColor", clothingColors[clothingColorIndex].Colors[a]);

        for (int a = 0; a < profileHatMR.Count; a++)
            profileHatMR[a].material.SetColor("_BaseColor", clothingColors[clothingColorIndex].Colors[1]);
    }

    public void ChangeSkinColor(int index)
    {
        skinColorIndicator[skinColorIndex].enabled = false;

        //if (isNext)
        //{
        //    if (skinColorIndex >= skinColorList.Count - 1)
        //        skinColorIndex = 0;
        //    else
        //        skinColorIndex++;
        //}
        //else
        //{
        //    if (skinColorIndex <= 0)
        //        skinColorIndex = skinColorList.Count - 1;
        //    else
        //        skinColorIndex--;
        //}

        skinColorIndex = index;
        skinColorIndicator[skinColorIndex].enabled = true;

        CheckSettingsForSaveButton();
        bodyColorMR.material.SetColor("_BaseColor", skinColor[skinColorIndex]);
        //skinColorImg.sprite = skinColorList[skinColorIndex];

        profileBodyColorMR.material.SetColor("_BaseColor", skinColor[skinColorIndex]);
    }

    public void SaveCharacterSettings()
    {
        GameManager.Instance.NotificationController.ShowConfirmation("Would you like to save your character settings?", () =>
        {
            GameManager.Instance.NoBGLoading.SetActive(true);

            StartCoroutine(GameManager.Instance.PostRequest("/characters/savecharactersetting", "", new Dictionary<string, object>
            {
                { "hairstyle", hairStyleIndex },
                { "haircolor", hairColorIndex },
                { "clothingcolor", clothingColorIndex },
                { "skincolor", skinColorIndex }
            }, false, (response) =>
            {
                userData.CharacterSetting.hairstyle = hairStyleIndex;
                userData.CharacterSetting.haircolor = hairColorIndex;
                userData.CharacterSetting.clothingcolor = clothingColorIndex;
                userData.CharacterSetting.skincolor = skinColorIndex;

                canSaveCustomization = false;
                CheckSettingsForSaveButton();
            }, () =>
            {
                GameManager.Instance.NoBGLoading.SetActive(false);
            }));
        }, null);
    }
}