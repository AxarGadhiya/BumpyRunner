using UnityEngine;

public class GameUIManager : MonoBehaviour
{
    [SerializeField] GameObject menuPanel;
    [SerializeField] GameObject gameWinPanel;
    [SerializeField] GameObject gameOverPanel;


    private void Start()
    {
        GameManager.Instance.OnGameWinEvent += OpenWinPanel;
        GameManager.Instance.OnGameFailEvent += OpenGameOverPanel;

        OpenMenuPanel();
    }

    public void OpenMenuPanel()
    {
        OpenPanel(menuPanel.name);
    }

    public void OpenWinPanel()
    {
        OpenPanel(gameWinPanel.name);
    }
    public void OpenGameOverPanel()
    {
        OpenPanel(gameOverPanel.name);
    }

    private void OpenPanel(string panelName)
    {
        menuPanel.SetActive(panelName == menuPanel.name);
        gameWinPanel.SetActive(panelName == gameWinPanel.name);
        gameOverPanel.SetActive(panelName == gameOverPanel.name);
    }

}
