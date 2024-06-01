using MyBox;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CharacterCreationController : MonoBehaviour
{
    [SerializeField] private UserData userData;

    [Header("SPRITES")]
    [SerializeField] private List<Sprite> hairStyleList;
    [SerializeField] private List<Sprite> hairColorList;
    [SerializeField] private List<Sprite> clothingColorList;
    [SerializeField] private List<Sprite> skinColorList;

    [Header("HAIR")]
    [SerializeField] private List<GameObject> hairStyles;
    [SerializeField] private List<MeshRenderer> hairMR;

    [Header("IMAGES")]
    [SerializeField] private Image hairStyleImg;
    [SerializeField] private Image hairColorImg;
    [SerializeField] private Image clothingColorImg;
    [SerializeField] private Image skinColorImg;

    [Header("COLOR")]
    [SerializeField] private List<Color> hairColor;
    [SerializeField] private List<Color> clothingColor;
    [SerializeField] private List<Color> skinColor;

    [Header("CHARACTER")]
    [SerializeField] private SkinnedMeshRenderer bodyColorMR;
    [SerializeField] private SkinnedMeshRenderer upperClothingMR;
    [SerializeField] private SkinnedMeshRenderer lowerClothingMR;

    [Header("BUTTONS")]
    [SerializeField] private Button saveBtn;

    [Header("DEBUGGER")]
    [ReadOnly][SerializeField] private int hairStyleIndex;
    [ReadOnly][SerializeField] private int hairColorIndex;
    [ReadOnly][SerializeField] private int clothingColorIndex;
    [ReadOnly][SerializeField] private int skinColorIndex;

    public void InitializeCharacterSettings(int hairStyleIndex, int hairColorIndex, int clothingColorIndex, int skinColorIndex)
    {
        this.hairStyleIndex = hairStyleIndex;
        this.hairColorIndex = hairColorIndex;
        this.clothingColorIndex = clothingColorIndex;
        this.skinColorIndex = skinColorIndex;

        hairStyleImg.sprite = hairStyleList[hairStyleIndex];
        hairColorImg.sprite = hairColorList[hairColorIndex];
        clothingColorImg.sprite = clothingColorList[clothingColorIndex];
        skinColorImg.sprite = skinColorList[skinColorIndex];

        hairStyles[hairStyleIndex].SetActive(true);

        hairMR[hairStyleIndex].material.SetColor("_BaseColor", hairColor[hairColorIndex]);
        upperClothingMR.material.SetColor("_BaseColor", clothingColor[clothingColorIndex]);
        lowerClothingMR.materials[0].SetColor("_BaseColor", clothingColor[clothingColorIndex]);
        lowerClothingMR.materials[1].SetColor("_BaseColor", clothingColor[clothingColorIndex]);
        bodyColorMR.material.SetColor("_BaseColor", skinColor[skinColorIndex]);

        CheckSettingsForSaveButton();
    }

    private void CheckSettingsForSaveButton()
    {
        if (hairStyleIndex != userData.CharacterSetting.hairstyle || hairColorIndex != userData.CharacterSetting.haircolor || clothingColorIndex != userData.CharacterSetting.clothingcolor || skinColorIndex != userData.CharacterSetting.skincolor)
            saveBtn.gameObject.SetActive(true);
        else
            saveBtn.gameObject.SetActive(false);
    }

    public void ChangeHairStyle(bool isNext)
    {
        hairStyles[hairStyleIndex].SetActive(false);

        if (isNext)
        {
            if (hairStyleIndex >= hairStyleList.Count - 1)
                hairStyleIndex = 0;
            else
                hairStyleIndex++;
        }
        else
        {
            if (hairStyleIndex <= 0)
                hairStyleIndex = hairStyleList.Count - 1;
            else
                hairStyleIndex--;
        }
        CheckSettingsForSaveButton();
        hairStyles[hairStyleIndex].SetActive(true);
        hairMR[hairStyleIndex].material.SetColor("_BaseColor", hairColor[hairColorIndex]);
        hairStyleImg.sprite = hairStyleList[hairStyleIndex];
    }


    public void ChangeHairColor(bool isNext)
    {
        if (isNext)
        {
            if (hairColorIndex >= hairColorList.Count - 1)
                hairColorIndex = 0;
            else
                hairColorIndex++;
        }
        else
        {
            if (hairColorIndex <= 0)
                hairColorIndex = hairColorList.Count - 1;
            else
                hairColorIndex--;
        }
        CheckSettingsForSaveButton();
        hairMR[hairStyleIndex].material.SetColor("_BaseColor", hairColor[hairColorIndex]);
        hairColorImg.sprite = hairColorList[hairColorIndex];
    }

    public void ChangeClothingColor(bool isNext)
    {
        if (isNext)
        {
            if (clothingColorIndex >= clothingColorList.Count - 1)
                clothingColorIndex = 0;
            else
                clothingColorIndex++;
        }
        else
        {
            if (clothingColorIndex <= 0)
                clothingColorIndex = clothingColorList.Count - 1;
            else
                clothingColorIndex--;
        }
        CheckSettingsForSaveButton();
        upperClothingMR.material.SetColor("_BaseColor", clothingColor[clothingColorIndex]);
        lowerClothingMR.materials[0].SetColor("_BaseColor", clothingColor[clothingColorIndex]);
        lowerClothingMR.materials[1].SetColor("_BaseColor", clothingColor[clothingColorIndex]);
        clothingColorImg.sprite = clothingColorList[clothingColorIndex];
    }

    public void ChangeSkinColor(bool isNext)
    {
        if (isNext)
        {
            if (skinColorIndex >= skinColorList.Count - 1)
                skinColorIndex = 0;
            else
                skinColorIndex++;
        }
        else
        {
            if (skinColorIndex <= 0)
                skinColorIndex = skinColorList.Count - 1;
            else
                skinColorIndex--;
        }
        CheckSettingsForSaveButton();
        bodyColorMR.material.SetColor("_BaseColor", skinColor[skinColorIndex]);
        skinColorImg.sprite = skinColorList[skinColorIndex];
    }

    public void SaveCharacterSettings()
    {
        GameManager.Instance.NotificationController.ShowConfirmation("Are you sure you want to save this character settings?", () =>
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
            }, () =>
            {
                GameManager.Instance.NoBGLoading.SetActive(false);
                saveBtn.gameObject.SetActive(false);
            }));
        }, null);
    }
}