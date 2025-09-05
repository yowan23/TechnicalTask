using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    //Start Button
    public void PlayGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }

    //Quit Button
    public void QuitGame()
    {
        Debug.Log("Quit Game!"); // Shows in Unity editor
        Application.Quit();      // Quits when built (won't work in editor)
    }
}
