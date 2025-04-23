using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField] private GameObject pauseMenuUI; 
    private bool isPaused = false;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P)) 
        {
            if (isPaused) ResumeGame();
            else PauseGame();
        }
    }

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f; 
        Cursor.lockState = CursorLockMode.None; 
        Cursor.visible = true; 
        pauseMenuUI.SetActive(true); 
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f; 
        Cursor.lockState = CursorLockMode.Locked; 
        Cursor.visible = false; 
        pauseMenuUI.SetActive(false); 
    }

    public void SaveGame()
    {
        GameDataManager.Instance.SaveGame();
    }

    public void LoadGame()
    {
        GameDataManager.Instance.LoadGame();
    }
}
