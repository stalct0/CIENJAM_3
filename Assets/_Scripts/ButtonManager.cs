using UnityEngine;

public class ButtonManager : MonoBehaviour
{
    public GameObject OptionsMenu;
    public GameObject SpellMenu;
    public void StartGame()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("TGH");
    }

    public void QuitGame()
    {
        UnityEditor.EditorApplication.isPlaying = false;
        Application.Quit();
    }

    public void OpenOptions()
    {
        OptionsMenu.SetActive(true);
    }

    public void CloseOptions()
    {
        OptionsMenu.SetActive(false);
    }

    public void OpenSpellMenu()
    {
        SpellMenu.SetActive(true);
    }

    public void CloseSpellMenu()
    {
        SpellMenu.SetActive(false);
    }

    public void SelectSpell1()
    {
        
    }

    public void SelectSpell2()
    {
        
    }
}
