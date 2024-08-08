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

    public float InitialAmbientVolume
    {
        get => initialAmbientVolume;
    }

    //  ==============================

    [SerializeField] private AudioSource bgSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource ambientSource;
    [SerializeField] private AudioSource sfxLoopSource;

    [Header("DEBUGGER")]
    [ReadOnly][SerializeField] private float currentVolume;
    [ReadOnly][SerializeField] private float musicVolume;
    [ReadOnly][SerializeField] private float sfxVolume;
    [ReadOnly][SerializeField] private float ambientVolume;
    [ReadOnly][SerializeField] private float initialBGVolume;
    [ReadOnly][SerializeField] private float initialSFXVolume;
    [ReadOnly][SerializeField] private float initialAmbientVolume;

    private void Awake()
    {
        CheckVolumeSaveData();
        OnVolumeChange += VolumeCheck;
        OnMusicVolumeChange += MusicVolumeCheck;
        OnSFXChange += SFXCheck;
        OnAmbientVolumeChange += AmbientCheck;
    }

    private void OnDisable()
    {
        OnVolumeChange -= VolumeCheck;
        OnMusicVolumeChange -= MusicVolumeCheck;
        OnSFXChange -= SFXCheck;
        OnAmbientVolumeChange -= AmbientCheck;
    }

    private void AmbientCheck(object sender, EventArgs e)
    {
        initialAmbientVolume = CurrentAmbientVolume;
        ambientSource.volume = initialAmbientVolume * CurrentVolume;
        SaveAmbientVolume();
    }

    private void MusicVolumeCheck(object sender, EventArgs e)
    {
        initialBGVolume = CurrentMusicVolume;
        bgSource.volume = initialBGVolume * CurrentVolume;
        SaveMusicVolume();
    }

    private void SFXCheck(object sender, EventArgs e)
    {
        initialSFXVolume = CurrentSFXVolume;
        sfxSource.volume = initialSFXVolume * CurrentVolume;
        sfxLoopSource.volume = initialSFXVolume * CurrentVolume;
        SaveSFXVolume();
    }

    private void VolumeCheck(object sender, EventArgs e)
    {
        bgSource.volume = initialBGVolume * CurrentVolume;
        sfxSource.volume = initialSFXVolume * CurrentVolume;
        sfxLoopSource.volume = initialSFXVolume * CurrentVolume;
        ambientSource.volume = initialAmbientVolume * CurrentVolume;

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
            initialBGVolume = 1;
            PlayerPrefs.SetFloat("MusicVolume", 1);
        }
        else
        {
            CurrentMusicVolume = PlayerPrefs.GetFloat("MusicVolume");
            initialBGVolume = CurrentMusicVolume;
        }


        if (!PlayerPrefs.HasKey("SFXVolume"))
        {
            CurrentSFXVolume = 1;
            initialSFXVolume = 1;
            PlayerPrefs.SetFloat("SFXVolume", 1);
        }
        else
        {
            CurrentSFXVolume = PlayerPrefs.GetFloat("SFXVolume");
            initialSFXVolume = CurrentSFXVolume;
        }

        if (!PlayerPrefs.HasKey("AmbientVolume"))
        {
            CurrentAmbientVolume = 1;
            initialAmbientVolume = 1;
            PlayerPrefs.SetFloat("AmbientVolume", 1);
        }
        else
        {
            CurrentAmbientVolume = PlayerPrefs.GetFloat("AmbientVolume");
            initialAmbientVolume = CurrentAmbientVolume;
        }

        bgSource.volume = initialBGVolume * CurrentVolume;
        sfxSource.volume = initialSFXVolume * CurrentVolume;
        sfxLoopSource.volume = initialSFXVolume * CurrentVolume;
        ambientSource.volume = initialAmbientVolume * CurrentVolume;
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
        LeanTween.value(gameObject, bgSource.volume, 0f, 0.25f).setEase(LeanTweenType.easeOutCubic).setOnUpdate((float val) =>
        {
            bgSource.volume = val;
        }).setOnComplete(() =>
        {
            bgSource.Stop();
            bgSource.clip = clip;
            bgSource.Play();
            LeanTween.value(gameObject, 0f, initialBGVolume * CurrentVolume, 0.25f).setEase(LeanTweenType.easeOutCubic).setOnUpdate((float val) =>
            {
                bgSource.volume = val;
            }).setOnComplete(() =>
            {
                bgSource.volume = initialBGVolume * CurrentVolume;
            });
        });
    }

    public void PauseBGMusic() => LeanTween.value(gameObject, bgSource.volume, 0f, 0.25f).setEase(LeanTweenType.easeOutCubic).setOnUpdate((float val) =>
    {
        bgSource.volume = val;
    });

    public void StopBGMusic()
    {
        bgSource.Stop();
    }

    public void ResumeBGMusic() => LeanTween.value(gameObject, 0f, initialBGVolume * CurrentVolume, 0.25f).setEase(LeanTweenType.easeOutCubic).setOnUpdate((float val) =>
    {
        bgSource.volume = val;
    });

    public bool CheckBGVolume(float volume) => bgSource.volume == volume;

    public void PlaySFX(AudioClip clip) => sfxSource.PlayOneShot(clip);
}
