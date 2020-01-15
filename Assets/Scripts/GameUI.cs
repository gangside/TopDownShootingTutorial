using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameUI : MonoBehaviour
{
    public Image fadePlane;
    public GameObject gameOverUI;
    public RectTransform newWaveBanner;
    public Text newWaveTitle;
    public Text newWaveEnemyCount;

    Spawner spawner;

    void Start()
    {
        FindObjectOfType<Player>().OnDeath += OnGameOver;
    }

    private void Awake() {
        spawner = FindObjectOfType<Spawner>();
        spawner.OnNewWave += OnNewWave;
    }

    void OnNewWave(int waveNumber) {
        string[] numbers = { "One", "Two", "Three", "Four", "Five" };
        newWaveTitle.text = "- Wave " + numbers[waveNumber-1] + " -";
        string enemyCountString = (spawner.waves[waveNumber-1].infinite) ? "infinite" : spawner.waves[waveNumber - 1].enemyCount.ToString();
        newWaveEnemyCount.text = "Enemies: " + enemyCountString;
        StopCoroutine("AnimateNewWaveBanner");
        StartCoroutine("AnimateNewWaveBanner");
    }

    IEnumerator AnimateNewWaveBanner() {
        float delayTime = 1.5f;
        float animatePercent = 0;
        float speed = 3f;
        int dir = 1;

        float endDelayTime = Time.time + 1 / speed + delayTime; 

        while (animatePercent >= 0) {
            animatePercent += Time.deltaTime * speed * dir;

            if(animatePercent >= 1) {
                animatePercent = 1;
                if(Time.time >= endDelayTime) {
                    dir = -1;
                }
            }

            newWaveBanner.anchoredPosition = Vector2.up * Mathf.Lerp(-255, 0, animatePercent);
            yield return null;
        }
    }

    private void OnGameOver() {
        StartCoroutine(Fade(Color.clear, Color.black, 1));
        Cursor.visible =  true;
        gameOverUI.SetActive(true);
    }

    IEnumerator Fade(Color from, Color to, float time) {
        float speed = 1 / time;
        float percent = 0;

        while (percent < 1) {
            percent += Time.deltaTime * speed;
            fadePlane.color = Color.Lerp(from, to, percent);
            yield return null;
        }
    }

    //UI Input
    public void StartNewGame() {
        SceneManager.LoadScene("Game");
    }
}
