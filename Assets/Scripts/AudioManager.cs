using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public enum AudioChannel {Master, Sfx, Music};

    public float masterVolumePercent { get; private set; }
    public float sfxVolumePercent { get; private set; }
    public float musicVolumepercent { get; private set; }

    AudioSource sfx2DSource;
    AudioSource[] musicSources;
    int activeMusicSourceIndex;

    public static AudioManager instance;

    Transform audioListner;
    Transform playerT;

    SoundLibrary soundLibrary;

    private void Awake() {
        if(instance != null) {
            Destroy(this.gameObject);
        }
        else {
            instance = this;

            DontDestroyOnLoad(this.gameObject);

            soundLibrary = GetComponent<SoundLibrary>();
            musicSources = new AudioSource[2];
            for (int i = 0; i < musicSources.Length; i++) {
                GameObject newMusicSource = new GameObject("Music source " + (i + 1));
                musicSources[i] = newMusicSource.AddComponent<AudioSource>();
                musicSources[i].transform.parent = transform;
            }

            GameObject newSfx2DSource = new GameObject("2D sfx source");
            sfx2DSource = newSfx2DSource.AddComponent<AudioSource>();
            sfx2DSource.transform.parent = transform;

            audioListner = FindObjectOfType<AudioListener>().transform;
            if(FindObjectOfType<Player>() != null) {
                playerT = FindObjectOfType<Player>().transform;
            }

            masterVolumePercent = PlayerPrefs.GetFloat("master vol", 1);
            sfxVolumePercent = PlayerPrefs.GetFloat("sfx vol", 1);
            musicVolumepercent = PlayerPrefs.GetFloat("music vol", 1);
        }
    }

    public void SetVolume(float volumePecent, AudioChannel channel) {
        switch (channel) {
            case AudioChannel.Master:
                masterVolumePercent = volumePecent;
                break;
            case AudioChannel.Sfx:
                sfxVolumePercent = volumePecent;
                break;
            case AudioChannel.Music:
                musicVolumepercent = volumePecent;
                break;
        }

        musicSources[0].volume = musicVolumepercent * masterVolumePercent;
        musicSources[1].volume = musicVolumepercent * masterVolumePercent;

        PlayerPrefs.SetFloat("master vol", masterVolumePercent);
        PlayerPrefs.SetFloat("sfx vol", sfxVolumePercent);
        PlayerPrefs.SetFloat("music vol", musicVolumepercent);
        PlayerPrefs.Save();
    }

    private void Update() {
        if(playerT != null) {
            audioListner.position = playerT.position;
        }
    }

    public void PlayMusic(AudioClip clip, float fadeDuration = 1) {
        if(clip!= null) {
            activeMusicSourceIndex = 1 - activeMusicSourceIndex;
            musicSources[activeMusicSourceIndex].clip = clip;
            musicSources[activeMusicSourceIndex].Play();

            StartCoroutine(AnimateMusicCrossFade(fadeDuration));
        }
    }

    public void PlaySound(string soundName, Vector3 pos) {
        PlaySound(soundLibrary.GetClipFromName(soundName), pos);
    }

    public void PlaySound(AudioClip clip, Vector3 pos) {
        if(clip != null) {
            AudioSource.PlayClipAtPoint(clip, pos, sfxVolumePercent * masterVolumePercent);
        }
    }

    public void PlaySound2D(string soundName) {
        sfx2DSource.PlayOneShot(soundLibrary.GetClipFromName("Level Complete"), sfxVolumePercent * masterVolumePercent);
    }

    IEnumerator AnimateMusicCrossFade(float duration) {
        float percent = 0;
        while(percent < 1) {
            percent += Time.deltaTime * 1 / duration;
            musicSources[activeMusicSourceIndex].volume = Mathf.Lerp(0, musicVolumepercent * masterVolumePercent, percent);
            musicSources[1-activeMusicSourceIndex].volume = Mathf.Lerp(musicVolumepercent * masterVolumePercent, 0, percent);
            yield return null;
        }
    }
}
