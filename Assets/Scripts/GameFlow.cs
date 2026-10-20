using UnityEngine;

public class GameFlow : MonoBehaviour
{
    public GameObject startScreen;
    public GameObject gameWorld;

    void Start()
    {
        startScreen.SetActive(true);
        gameWorld.SetActive(false);
    }

    public void StartGame()
    {
        startScreen.SetActive(false);
        gameWorld.SetActive(true);
    }

    public void BackToStart()
    {
        gameWorld.SetActive(false);
        startScreen.SetActive(true);
    }
}