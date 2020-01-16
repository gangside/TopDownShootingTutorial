﻿using System;
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
    public Text scoreUI;
    public Text gameOverScoreUI;
    public RectTransform healthBar;

    Spawner spawner;
    Player player;

    void Start()
    {
        player = FindObjectOfType<Player>();
        player.OnDeath += OnGameOver;
    }

    private void Awake() {
        spawner = FindObjectOfType<Spawner>();
        spawner.OnNewWave += OnNewWave;
    }

    private void Update() {
        scoreUI.text = ScoreKeeper.score.ToString("D6");

        //플레이어가 죽어있든지 살아있던지 상관없이 체력체크
        float healthPercent = 0;
        if(player != null) {
            healthPercent = player.health / player.startingHealth;
        }

        healthBar.localScale = new Vector3(healthPercent, 1, 1);
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
        StartCoroutine(Fade(Color.clear, new Color(0,0,0,0.95f), 1));
        Cursor.visible =  true;

        gameOverScoreUI.text = scoreUI.text;
        scoreUI.gameObject.SetActive(false);
        healthBar.transform.parent.gameObject.SetActive(false);
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

    public void ReturnToMainMenu() {
        SceneManager.LoadScene("Menu");
    }
}
