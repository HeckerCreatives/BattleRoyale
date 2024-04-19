using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ButtonSelector : MonoBehaviour
{
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
        transform.position = new Vector3(selectedPos, transform.position.y, transform.position.z);
    }

    public void UnselectedBtn()
    {
        selectorObj.SetActive(false);
        btnImg.sprite = unselected;
        transform.position = new Vector3(unselectedPos, transform.position.y, transform.position.z);
    }

    public void PressBtn()
    {
        selectorObj.SetActive(true);
        btnImg.sprite = selected;
        transform.position = new Vector3(selectedPos, transform.position.y, transform.position.z);
        SceneManager.LoadScene(nextScene);
    }
}
