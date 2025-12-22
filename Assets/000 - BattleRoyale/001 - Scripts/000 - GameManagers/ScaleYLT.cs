using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScaleYLT : MonoBehaviour
{
    [SerializeField] private LeanTweenType easeType;
    [SerializeField] private float easeAmount;
    [SerializeField] private RectTransform objTF;
    [SerializeField] private float to;

    //  =================

    int ltDesc;

    //  =================

    private void OnEnable()
    {
        objTF.sizeDelta = new Vector3(1f, 0f);
        PlayTween();
    }

    private void OnDisable()
    {
        objTF.sizeDelta = new Vector3(1f, 0f);
    }

    private void PlayTween()
    {
        if (ltDesc != 0) LeanTween.cancel(ltDesc);

        ltDesc = LeanTween.value(objTF.gameObject, 0f, to, easeAmount).setOnUpdate((float val) =>
        {
            objTF.sizeDelta = new Vector3(1f, val);
        }).setEase(easeType).id;
    }
}
