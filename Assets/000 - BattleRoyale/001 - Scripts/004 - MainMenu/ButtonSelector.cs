using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ButtonSelector : MonoBehaviour
{
    [SerializeField] private RectTransform buttonRT;
    [SerializeField] private Sprite unselected;
    [SerializeField] private Sprite selected;
    [SerializeField] private Image btnImg;
    [SerializeField] private GameObject selectorObj;
    [SerializeField] private float selectedPos;
    [SerializeField] private float unselectedPos;
    [SerializeField] private string nextScene;

    private void OnEnable()
    {
        UnselectedBtn();
    }

    public void SelectedBtn()
    {
        selectorObj.SetActive(true);
        btnImg.sprite = selected;
        buttonRT.anchoredPosition = new Vector3(selectedPos, buttonRT.anchoredPosition.y);
    }

    public void UnselectedBtn()
    {
        selectorObj.SetActive(false);
        btnImg.sprite = unselected;
        buttonRT.anchoredPosition = new Vector3(unselectedPos, buttonRT.anchoredPosition.y);
    }

    public void PressBtn()
    {
        selectorObj.SetActive(true);
        btnImg.sprite = selected;
        buttonRT.anchoredPosition = new Vector3(selectedPos, buttonRT.anchoredPosition.y);
        SceneManager.LoadScene(nextScene);
    }
}
