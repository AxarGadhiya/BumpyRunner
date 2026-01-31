using UnityEngine;
using UnityEngine.SceneManagement;

public class Test : MonoBehaviour
{
    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
