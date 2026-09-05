using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    public void MainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    public void GameScene()
    {
        SceneManager.LoadScene("GameScene");
    }

    public void LoreScene()
    {
        SceneManager.LoadScene("LoreScene");
    }

    public void Quit()
    {
        RankingStore.Clear();
        Application.Quit();
    }
}
