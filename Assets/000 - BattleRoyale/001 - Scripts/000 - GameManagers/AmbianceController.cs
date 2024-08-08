using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AmbianceController : MonoBehaviour
{
    [SerializeField] private List<AudioSource> ambienceSources;

    //  ==========================

    Coroutine ambienceCoroutine;

    // Start is called before the first frame update
    void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AudioController.OnAmbientVolumeChange += AmbianceChange;
            GameManager.Instance.AudioController.StopBGMusic();
        }
    }

    private void AmbianceChange(object sender, EventArgs e)
    {
        if (ambienceCoroutine != null) StopCoroutine(ambienceCoroutine);

        ambienceCoroutine = StartCoroutine(SetAmbienceVolume());
    }

    IEnumerator SetAmbienceVolume()
    {
        foreach(var ambience in ambienceSources)
        {
            ambience.volume = GameManager.Instance.AudioController.InitialAmbientVolume * GameManager.Instance.AudioController.CurrentVolume;

            yield return null;
        }
    }
}
