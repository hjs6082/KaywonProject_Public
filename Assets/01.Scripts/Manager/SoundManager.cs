using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [SerializeField]
    private AudioMixer mixer;
    public AudioMixer Mixer => mixer;
    [SerializeField]
    private AudioSource _audioSource;
    [SerializeField]
    private AudioSource _talkAudioSource;
    [SerializeField]
    private AudioClip[] _bgClips;
    // Start is called before the first frame update
    void Awake()
    {
        Init();
    }

    void Init()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(this.gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;

        _audioSource = this.gameObject.GetComponent<AudioSource>();
        if (_bgClips != null && _bgClips.Length > 0)
            BgSoundPlay(_bgClips[0]);
    }

    private void OnSceneLoaded(Scene arg0, LoadSceneMode arg1)
    {
        for (int i = 0; i < _bgClips.Length; i++)
        {
            if (arg0.name == _bgClips[i].name)
            {
                BgSoundPlay(_bgClips[i]);
            }
        }
    }

    public void BGSoundVolume(float val)
    {
        mixer.SetFloat("BG", val <= 0.0001f ? -80f : Mathf.Log10(val) * 20);
    }

    public void SFXSoundVolume(float val)
    {
        mixer.SetFloat("SFX", val <= 0.0001f ? -80f : Mathf.Log10(val) * 20);
    }

    public void TALKSoundVolume(float val)
    {
        mixer.SetFloat("TALK", val <= 0.0001f ? -80f : Mathf.Log10(val) * 20);
    }

    public void PlayTalk(AudioClip clip, float volume = 1f)
    {
        if (_talkAudioSource == null || clip == null) return;
        var groups = mixer.FindMatchingGroups("TALK");
        _talkAudioSource.outputAudioMixerGroup = groups.Length > 0 ? groups[0] : null;
        _talkAudioSource.clip = clip;
       // _talkAudioSource.volume = volume;
        _talkAudioSource.Play();
    }

    public void StopTalk()
    {
        if (_talkAudioSource != null && _talkAudioSource.isPlaying)
            _talkAudioSource.Stop();
    }

    public void PlaySFX(string sfxName)
    {
        GameObject soundObject = new GameObject(sfxName + "Sound");
        AudioSource soundObjectAudioSource = soundObject.AddComponent<AudioSource>();
        AudioClip playClip = Resources.Load<AudioClip>("05.Sound/" + sfxName);
        soundObjectAudioSource.outputAudioMixerGroup = mixer.FindMatchingGroups("SFX")[0];
        soundObjectAudioSource.clip = playClip;
        soundObjectAudioSource.Play();

        Destroy(soundObject, playClip.length);
    }

    public void BgSoundPlay(AudioClip clip)
    {
        _audioSource.outputAudioMixerGroup = mixer.FindMatchingGroups("BG")[0];
        _audioSource.clip = clip;
        _audioSource.loop = true;
        _audioSource.volume = 0.6f;
        _audioSource.Play();
    }
}