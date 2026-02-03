using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{

    public static GameManager Instance;

    public Action OnGameWinEvent,OnGameFailEvent;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        PlayerSpawnner.Instance.SpawnCharacters();
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }


    public void GameLoose()
    {
        OnGameFailEvent?.Invoke();
    }

    public void GameWin()
    {
        OnGameWinEvent?.Invoke();
    }

}
