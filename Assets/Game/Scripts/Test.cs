using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
public class Test : MonoBehaviour
{
    public TextMeshProUGUI inputStrengthText;
    public TextMeshProUGUI playerYRotationText;
    public TextMeshProUGUI playerStateText;

    Player player;

    private void Start()
    {
        player = FindObjectOfType<Player>();
    }

    private void Update()
    {
        if (player == null)
            player = FindObjectOfType<Player>();
        else
        {
            playerYRotationText.SetText("Y:" + player.transform.eulerAngles.y);
            playerStateText.SetText(player.GetCurrentStat());
        }
           
    }
    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void PlustInputStrength()
    {
      float j= player.AddInputStrengthTest(0.05f);
        inputStrengthText.SetText("" + j);
    }

    public void MinustInputStrength()
    {
       float j= player.AddInputStrengthTest(-0.05f);
        inputStrengthText.SetText("" + j);
    }
}
