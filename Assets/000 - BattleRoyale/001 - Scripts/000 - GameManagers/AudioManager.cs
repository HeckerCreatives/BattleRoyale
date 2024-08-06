using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using MyBox;

public class AudioManager : MonoBehaviour
{
    private event EventHandler VolumeChange;
    public event EventHandler OnVolumeChange
    {
        add
        {
            if (VolumeChange == null || !VolumeChange.GetInvocationList().Contains(value))
                VolumeChange += value;
        }
        remove { VolumeChange -= value; }
    }
    public float CurrentVolume
    {
        get => currentVolume;
        set
        {
            currentVolume = value;
            VolumeChange?.Invoke(this, EventArgs.Empty);
        }
    }

    private event EventHandler SFXChange;
    public event EventHandler OnSFXChange
    {
        add
        {
            if (SFXChange == null || !SFXChange.GetInvocationList().Contains(value))
                SFXChange += value;
        }
        remove { SFXChange -= value; }
    }
    public float CurrentSFXVolume
    {
        get => sfxVolume;
        set
        {
            sfxVolume = value;
            SFXChange?.Invoke(this, EventArgs.Empty);
        }
    }
    public bool IsBGPlaying
    {
        get => bgSource.isPlaying;
    }

    private event EventHandler MusicVolumeChange;
    public event EventHandler OnMusicVolumeChange
    {
        add
        {
            if (MusicVolumeChange == null || !MusicVolumeChange.GetInvocationList().Contains(value))
                MusicVolumeChange += value;
        }
        remove { MusicVolumeChange -= value; }
    }
    public float CurrentMusicVolume
    {
        get => musicVolume;
        set
        {
            musicVolume = value;
            MusicVolumeChange?.Invoke(this, EventArgs.Empty);
        }
    }

    private event EventHandler AmbientVolumeChange;
    public event EventHandler OnAmbientVolumeChange
    {
        add
        {
            if (AmbientVolumeChange == null || !AmbientVolumeChange.GetInvocationList().Contains(value))
                AmbientVolumeChange += value;
        }
        remove { AmbientVolumeChange -= value; }
    }
    public float CurrentAmbientVolume
    {
        get => ambientVolume;
        set
        {
            ambientVolume = value;
            AmbientVolumeChange?.Invoke(this, EventArgs.Empty);
        }
    }

    public AudioSource SfxLoopSource
    {
        get => sfxLoopSource;
    }

    [SerializeField] private AudioSource bgSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource ambientSource;
    [SerializeField] private AudioSource sfxLoopSource;

    [Header("DEBUGGER")]
    [ReadOnly][SerializeField] private float currentVolume;
    [ReadOnly][SerializeField] private float musicVolume;
    [ReadOnly][SerializeField] private float sfxVolume;
    [ReadOnly][SerializeField] private float ambientVolume;

    private void Awake()
    {
        CheckVolumeSaveData();
        OnVolumeChange += VolumeCheck;
        OnSFXChange += SFXCheck;
    }

    private void OnDisable()
    {
        OnVolumeChange -= VolumeCheck;
        OnSFXChange -= SFXCheck;
    }

    private void SFXCheck(object sender, EventArgs e)
    {
        sfxSource.volume = CurrentSFXVolume;
        sfxLoopSource.volume = CurrentSFXVolume;
        SaveSFXVolume();
    }

    private void VolumeCheck(object sender, EventArgs e)
    {
        bgSource.volume = CurrentVolume;
        SaveBGVolume();
    }

    public void CheckVolumeSaveData()
    {
        if (!PlayerPrefs.HasKey("BGVolume"))
        {
            CurrentVolume = 1;
            PlayerPrefs.SetFloat("BGVolume", 1);
        }
        else
            CurrentVolume = PlayerPrefs.GetFloat("BGVolume");

        if (!PlayerPrefs.HasKey("MusicVolume"))
        {
            CurrentMusicVolume = 1;
            PlayerPrefs.SetFloat("MusicVolume", 1);
        }
        else
            CurrentMusicVolume = PlayerPrefs.GetFloat("MusicVolume");


        if (!PlayerPrefs.HasKey("SFXVolume"))
        {
            CurrentSFXVolume = 1;
            PlayerPrefs.SetFloat("SFXVolume", 1);
        }
        else
            CurrentSFXVolume = PlayerPrefs.GetFloat("SFXVolume");

        if (!PlayerPrefs.HasKey("AmbientVolume"))
        {
            CurrentAmbientVolume = 1;
            PlayerPrefs.SetFloat("AmbientVolume", 1);
        }
        else
            CurrentAmbientVolume = PlayerPrefs.GetFloat("AmbientVolume");
    }

    public void SaveBGVolume()
    {
        PlayerPrefs.SetFloat("BGVolume", CurrentVolume);
    }

    public void SaveSFXVolume()
    {
        PlayerPrefs.SetFloat("SFXVolume", CurrentSFXVolume);
    }

    public void SaveAmbientVolume()
    {
        PlayerPrefs.SetFloat("AmbientVolume", CurrentAmbientVolume);
    }

    public void SaveMusicVolume()
    {
        PlayerPrefs.SetFloat("MusicVolume", CurrentMusicVolume);
    }

    public void SetBGMusic(AudioClip clip)
    {
        LeanTween.value(gameObject, CurrentVolume, 0f, 0.25f).setEase(LeanTweenType.easeOutCubic).setOnUpdate((float val) =>
        {
            bgSource.volume = val;
        }).setOnComplete(() =>
        {
            bgSource.Stop();
            bgSource.clip = clip;
            bgSource.Play();
            LeanTween.value(gameObject, 0f, CurrentVolume, 0.25f).setEase(LeanTweenType.easeOutCubic).setOnUpdate((float val) =>
            {
                bgSource.volume = val;
            }).setOnComplete(() =>
            {
                bgSource.volume = CurrentVolume;
            });
        });
    }

    public void PauseBGMusic() => LeanTween.value(gameObject, CurrentVolume, 0f, 0.25f).setEase(LeanTweenType.easeOutCubic).setOnUpdate((float val) =>
    {
        bgSource.volume = val;
    });

    public void ResumeBGMusic() => LeanTween.value(gameObject, 0f, CurrentVolume, 0.25f).setEase(LeanTweenType.easeOutCubic).setOnUpdate((float val) =>
    {
        bgSource.volume = val;
    });

    public bool CheckBGVolume(float volume) => bgSource.volume == volume;

    public void PlaySFX(AudioClip clip) => sfxSource.PlayOneShot(clip);
}
