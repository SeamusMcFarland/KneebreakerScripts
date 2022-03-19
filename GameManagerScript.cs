using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System;
using System.Linq.Expressions;
using UnityEngine.UIElements;

public class GameManagerScript : MonoBehaviour
{

    int score;
    int lives;
    int lives2;
    int lives3;
    int lives4;
    float timer;

    int defaultScore;
    int defaultLives;

    bool beatLevel;
    bool canBeatLevel;
    int beatLevelScore;

    bool timedLevel;
    float startTime;

    GameObject player;
    bool playerEnabled;

    public AudioSource backgroundMusic;
    public AudioSource soundEffects;
    AudioSource gameOverSFX, beatLevelSFX;
    bool backgroundMusicOver;

    enum GameStates { Playing, Death, GameOver, BeatLevel };
    GameStates gameState;
    bool gameIsOver;
    bool playerIsDead;

    public Canvas menuCanvas, HUDCanvas, endScreenCanvas, footerCanvas;

    public String endMessageWinS, endMessageLoseS, gameOverS, gameTitleS, gameCreditsS, gameCopyrightS, timerTitleS, scoreS, livesS, scoreVS, livesVS, timerVS, lives2VS, lives2S, lives3VS, lives3S, lives4VS, lives4S;
    public Text endMessageT, gameOverT, gameTitleT, gameCreditsT, gameCopyrightT, timerTitleT, scoreT, livesT, timerT, scoreVT, livesVT, timerVT, lives2VT, lives2T, lives3VT, lives3T, lives4VT, lives4T;

    public String firstLevel, nextLevel, levelToLoad, currentLevel;

    float currentTime;

    bool gameStarted;
    bool replay;

    String copyrightTextAquire;

    AudioSource audioSource;

    float hurtTimer;
    const float HURT_COOLDOWN = 0.5f;

    GameObject camera;

    PlayerScript[] allPS;

    public UnityEngine.UI.Toggle[] playerAI;
    public UnityEngine.UI.Slider numPlayersSlider;
    public UnityEngine.UI.Slider numLivesSlider;

    private void Awake()
    {
        DontDestroyOnLoad(this.gameObject);
    }

    public void Reset()
    {
        endMessageT.text = "";
        gameOverT.text = "";
        gameTitleT.text = "";
        gameCreditsT.text = "";
        gameCopyrightT.text = "";
        timerTitleT.text = "";
        scoreT.text = "";
        livesT.text = "";
        timerT.text = "";
        scoreVT.text = "";
        livesVT.text = "";
        timerVT.text = "";
        lives2T.text = "";
        lives2VT.text = "";
        lives3T.text = "";
        lives3VT.text = "";
        lives4T.text = "";
        lives4VT.text = "";
    }

    public void HideMenu()
    {
        menuCanvas.enabled = false;
        footerCanvas.enabled = false;
        endScreenCanvas.enabled = false;
    }

    // Start is called before the first frame update
    void Start()
    {
        camera = GameObject.Find("Main Camera");
        HideMenu();
        levelToLoad = firstLevel;
        MainMenu();
        //PlayGame();
    }

    public void MainMenu()
    {
        HUDCanvas.enabled = false;

        defaultScore = 0;
        defaultLives = 3;

        gameTitleS = "Kneebreaker";
        gameCreditsS = "by Seamus McFarland";
        gameCopyrightS = "music: Matrix - Slow Drift (1996)";

        gameTitleT.text = gameTitleS;
        gameCreditsT.text = gameCreditsS;
        gameCopyrightT.text = gameCopyrightS;

        if (menuCanvas != null)
            menuCanvas.enabled = true;
        if (footerCanvas != null)
            footerCanvas.enabled = true;
    }

    public void Quit()
    {
        Application.Quit();
    }

    public void PlayGame()
    {

        playerEnabled = true;
        HideMenu();
        if (HUDCanvas != null)
            HUDCanvas.enabled = true;

        if (timedLevel)
        {
            currentTime = startTime;

            timerTitleS = "Timer: ";
            timerTitleT.text = timerTitleS;
        }

        if (scoreT != null)
        {
            scoreT.text = scoreS;
        }

        if (livesT != null)
        {
            livesT.text = "Lives:";
        }

        if (lives2T != null)
        {
            lives2T.text = "Lives:";
        }

        if (lives3T != null)
        {
            lives3T.text = "Lives:";
        }

        if (lives4T != null)
        {
            lives4T.text = "Lives:";
        }

        gameStarted = true;
        gameState = GameStates.Playing;
        playerIsDead = false;
        camera.SetActive(false);
        SceneManager.LoadScene(levelToLoad, LoadSceneMode.Additive);

        defaultLives = (int)numLivesSlider.value;
        backgroundMusic.volume = 1f;
        lives = defaultLives;
        lives2 = defaultLives;
        if (numPlayersSlider.value >= 3)
            lives3 = defaultLives;
        else
            lives3 = 0;
        if (numPlayersSlider.value >= 4)
            lives4 = defaultLives;
        else
            lives4 = 0;

        timerT.enabled = false;
        timerVT.enabled = false;

        livesVT.enabled = true;
        livesT.enabled = true;
        lives2VT.enabled = true;
        lives2T.enabled = true;
        lives3VT.enabled = true;
        lives3T.enabled = true;
        lives4VT.enabled = true;
        lives4T.enabled = true;

        backgroundMusic.Play();
        currentLevel = levelToLoad;
        StartCoroutine("PSDelaySetup");
    }

    IEnumerator PSDelaySetup() // waits a frame in order for PlayerScripts to be properly set up to reference
    {
        yield return new WaitForSeconds(0.01f);
        allPS = FindObjectsOfType(typeof(PlayerScript)) as PlayerScript[];
        for (int i = 0; i < playerAI.Length; i++)
        {
            allPS[i].SetAI(playerAI[i].isOn);
        }

        if (numPlayersSlider.value <= 2)
        {
            for (int i = 0; i < allPS.Length; i++) // finds and disables player3 
            {
                if (allPS[i].CompareTag("player3"))
                {
                    allPS[i].gameObject.SetActive(false);
                    break;
                }
            }
            lives3VT.enabled = false;
            lives3T.enabled = false;
        }
        if (numPlayersSlider.value <= 3)
        {
            for (int i = 0; i < allPS.Length; i++) // finds and disables player4
            {
                if (allPS[i].CompareTag("player4"))
                {
                    allPS[i].gameObject.SetActive(false);
                    break;
                }
            }
            lives4VT.enabled = false;
            lives4T.enabled = false;
        }
    }

    // Update is called once per frame
    void Update()
    {
        hurtTimer -= Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.Escape))
            Quit();

        //TestCommands();

        UpdateScoreAndLives();

        switch (gameState)
        {
            case GameStates.Playing:
                if (playerIsDead)
                {
                        gameState = GameStates.Death;
                    
                }
                if (canBeatLevel && score >= beatLevelScore)
                {
                    gameState = GameStates.BeatLevel;
                }
                if (timedLevel)
                {
                    if (currentTime < 0)
                        gameState = GameStates.GameOver;
                    else if (timerVT != null)
                    {
                        timer -= Time.deltaTime;
                        timerVS = "" + timer;
                        timerVT.text = timerVS;
                    }
                }
                break;

            case GameStates.Death:
                if (backgroundMusic != null)
                {
                    if (backgroundMusic.volume <= 0)
                        backgroundMusicOver = true;
                    else
                        backgroundMusic.volume -= 0.002f;

                    if (backgroundMusicOver || backgroundMusic.clip == null)
                    {
                        if (gameOverSFX != null)
                            audioSource = gameOverSFX;
                        if (lives2 <= 0 && lives3 <= 0 && lives4 <= 0)
                            endMessageLoseS = "Player 1 Wins!";
                        else if (lives <= 0 && lives3 <= 0 && lives4 <= 0)
                            endMessageLoseS = "Player 2 Wins!";
                        else if (lives <= 0 && lives2 <= 0 && lives4 <= 0)
                            endMessageLoseS = "Player 3 Wins!";
                        else if (lives <= 0 && lives2 <= 0 && lives3 <= 0)
                            endMessageLoseS = "Player 4 Wins!";
                        else
                            print("INVALID PLAYER LIVES FOR ENDMESSAGELOSE");
                        endMessageT.text = endMessageLoseS;
                        gameState = GameStates.GameOver;
                    }
                }
                break;

            case GameStates.BeatLevel:
                if (backgroundMusic != null)
                {
                    if (backgroundMusic.volume <= 0)
                        backgroundMusicOver = true;
                    else
                        backgroundMusic.volume -= 0.01f;

                    if (backgroundMusicOver || backgroundMusic.clip == null)
                    {
                        if (beatLevelSFX != null)
                            audioSource = beatLevelSFX;

                        if (lives2 <= 0 && lives3 <= 0 && lives4 <= 0)
                            endMessageT.text = "Player 1 wins!" + endMessageWinS;
                        else if (lives <= 0 && lives3 <= 0 && lives4 <= 0)
                            endMessageT.text = "Player 2 wins!" + endMessageWinS;
                        else if (lives <= 0 && lives2 <= 0 && lives4 <= 0)
                            endMessageT.text = "Player 3 wins!" + endMessageWinS;
                        else if (lives <= 0 && lives2 <= 0 && lives3 <= 0)
                            endMessageT.text = "Player 4 wins!" + endMessageWinS;

                            gameState = GameStates.GameOver;
                        
                    }
                }
                break;

            case GameStates.GameOver:
                if (playerEnabled)
                {
                    playerEnabled = false;
                }
                HideMenu();
                if (endScreenCanvas != null)
                {
                    endScreenCanvas.enabled = true;
                    gameOverT.text = gameOverS;
                }
                break;

            default:
                print("ERROR! INVALID GAMESTATE!");
                break;
        }

    }

    private void TestCommands()
    {
        if (Input.GetKeyDown(KeyCode.U))
            print("playerIsDead: " + playerIsDead);
        if (Input.GetKeyDown(KeyCode.I))
            print("gameIsOver: " + gameIsOver);
        if (Input.GetKeyDown(KeyCode.O))
            print("playerIsDead: " + canBeatLevel);
    }

    private void UpdateScoreAndLives()
    {
        if (scoreVT != null)
        {
            scoreVS = "" + score;
            scoreVT.text = scoreVS;
        }
        if (livesVT != null)
        {
            livesVS = "" + lives;
            livesVT.text = livesVS;
        }
        if (lives2VT != null)
        {
            lives2VS = "" + lives2;
            lives2VT.text = lives2VS;
        }
        if (lives3VT != null)
        {
            lives3VS = "" + lives3;
            lives3VT.text = lives3VS;
        }
        if (lives4VT != null)
        {
            lives4VS = "" + lives4;
            lives4VT.text = lives4VS;
        }
    }

    public void ResetLevel()
    {
        print("resetlevel called");
        playerIsDead = false;
        SceneManager.UnloadSceneAsync(currentLevel);
        PlayGame();
    }

    public void StartNextLevel()
    {
        print("startnextlevel called");
        backgroundMusicOver = false;
        lives = defaultLives;
        lives2 = defaultLives;
        if (numPlayersSlider.value >= 3)
            lives3 = defaultLives;
        else
            lives3 = 0;
        if (numPlayersSlider.value >= 4)
            lives4 = defaultLives;
        else
            lives4 = 0;

        SceneManager.UnloadSceneAsync(currentLevel);
        levelToLoad = nextLevel;
        PlayGame();
    }

    public void RestartGame()
    {
        print("restart called");
        score = defaultScore;
        lives = defaultLives;
        lives2 = defaultLives;
        if (numPlayersSlider.value >= 3)
            lives3 = defaultLives;
        else
            lives3 = 0;
        if (numPlayersSlider.value >= 4)
            lives4 = defaultLives;
        else
            lives4 = 0;

        SceneManager.UnloadSceneAsync(currentLevel);
        levelToLoad = firstLevel;
        PlayGame();
    }

    public void Win()
    {
        gameState = GameStates.BeatLevel;
    }


    public void Lose()
    {
        lives = 0;
        lives2 = 0;
        lives3 = 0;
        lives4 = 0;
        gameState = GameStates.GameOver;
    }

    public void DecreaseLife(int p)
    {
        if (p == 1)
        {
            lives--;
            CheckWin();
        }
        else if (p == 2)
        {
            lives2--;
            CheckWin();
        }
        else if (p == 3)
        {
            lives3--;
            CheckWin();
        }
        else if (p == 4)
        {
            lives4--;
            CheckWin();
        }
        else
            print("INVALID PLAYER NUM IN DECREASED LIFE");
    }

    private void CheckWin() // also disables dead player's health in UI
    {
        int winInt = 0;
        if (lives <= 0)
        {
            winInt++;
            livesVT.enabled = false;
            livesT.enabled = false;
        }
        if (lives2 <= 0)
        {
            winInt++;
            lives2VT.enabled = false;
            lives2T.enabled = false;
        }
        if (lives3 <= 0)
        {
            winInt++;
            lives3VT.enabled = false;
            lives3T.enabled = false;
        }
        if (lives4 <= 0)
        {
            winInt++;
            lives4VT.enabled = false;
            lives4T.enabled = false;
        }

        if (winInt >= 3)
        {
            PlaySFX("coin");
            gameState = GameStates.BeatLevel;
        }
    }

    public void ScorePoint()
    {
        score++;
    }

    public AudioClip stickBreakA, slapA, swishA, impactA, coinA; 

    public void PlaySFX(String n)
    {
        if (String.Equals("stickbreak", n))
            soundEffects.PlayOneShot(stickBreakA);
        else if (String.Equals("slap", n))
            soundEffects.PlayOneShot(slapA);
        else if (String.Equals("swish", n))
            soundEffects.PlayOneShot(swishA);
        else if (String.Equals("impact", n))
            soundEffects.PlayOneShot(impactA);
        else if (String.Equals("coin", n))
            soundEffects.PlayOneShot(coinA);
        else
            print("INVALID SOUND STRING!");
    }

    public int GetLife(int p)
    {
        switch(p)
        {
            case 1:
                return lives;
            case 2:
                return lives2;
            case 3:
                return lives3;
            case 4:
                return lives4;

            default:
                print("ERROR! INVALID PLAYER FOR GETLIFE");
                break;
        }
        return 1; // if invalid input
    }

}
