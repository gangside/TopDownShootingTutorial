using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    float masterVolumePercent = 0.2f;
    float sfxVolumePercent = 1f;
    float musicVolumepercent = 1f;

    AudioSource[] musicSources;
    int activeMusicSourceIndex;

    public static AudioManager instance;

    Transform audioListner;
    Transform playerT;

    private void Awake() {

        instance = this;

        musicSources = new AudioSource[2];
        for (int i = 0; i < musicSources.Length; i++) {
            GameObject newMusicSource = new GameObject("Music source " + (i + 1));
            musicSources[i] = newMusicSource.AddComponent<AudioSource>();
            musicSources[i].transform.parent = transform;
        }

        audioListner = FindObjectOfType<AudioListener>().transform;
        playerT = FindObjectOfType<Player>().transform;
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

    public void PlaySound(AudioClip clip, Vector3 pos) {
        if(clip != null) {
            AudioSource.PlayClipAtPoint(clip, pos, sfxVolumePercent * masterVolumePercent);
        }
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
