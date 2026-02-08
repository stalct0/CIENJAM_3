using UnityEngine;
using UnityEngine.UI;

public class ButtonManager : MonoBehaviour
{
    public GameObject OptionsMenu;
    public GameObject DSpellMenu;
    public GameObject FSpellMenu;
    public GameObject SpellDButton;
    public GameObject SpellFButton;
    public Image[] SpellImages; // 0: Flash, 1: Ghost, 2: Barrier, 3: Exhaust

    void Start()
    {
        OptionsMenu.SetActive(false);
        DSpellMenu.SetActive(false);
        FSpellMenu.SetActive(false);
        SpellHolder.spellD = SummonerSpellType.Flash;
        SpellHolder.spellF = SummonerSpellType.Ghost;
        UpdateSpellImages();
    }
    public void StartGame()
    {
        //UnityEngine.SceneManagement.SceneManager.LoadScene("TGH");
    }

    public void QuitGame()
    {
        //UnityEditor.EditorApplication.isPlaying = false;
        //Application.Quit();
    }

    public void OpenOptions()
    {
        OptionsMenu.SetActive(true);
    }

    public void CloseOptions()
    {
        OptionsMenu.SetActive(false);
    }

    public void OpenDSpellMenu()
    {
        DSpellMenu.SetActive(true);
    }

    public void CloseDSpellMenu()
    {
        DSpellMenu.SetActive(false);
    }

    public void OpenFSpellMenu()
    {
        FSpellMenu.SetActive(true);
    }
    public void CloseFSpellMenu()
    {
        FSpellMenu.SetActive(false);
    }

    public void SelectSpellD(int id)
    {
        SummonerSpellType spellType = SummonerSpellType.None;
        switch (id)
        {
            case 0:
                spellType = SummonerSpellType.Flash;
                break;
            case 1:
                spellType = SummonerSpellType.Ghost;
                break;
            case 2:
                spellType = SummonerSpellType.Barrier;
                break;
            case 3:
                spellType = SummonerSpellType.Exhaust;
                break;
            default:
                break;
        }

        if (spellType == SpellHolder.spellF)
        {
            // swap
            SummonerSpellType temp = SpellHolder.spellD;
            SpellHolder.spellD = SpellHolder.spellF;
            SpellHolder.spellF = temp;
        }
        else
        {
            SpellHolder.spellD = spellType;
        }    

        UpdateSpellImages();
    }

    public void SelectSpellF(int id)
    {
                SummonerSpellType spellType = SummonerSpellType.None;
        switch (id)
        {
            case 0:
                spellType = SummonerSpellType.Flash;
                break;
            case 1:
                spellType = SummonerSpellType.Ghost;
                break;
            case 2:
                spellType = SummonerSpellType.Barrier;
                break;
            case 3:
                spellType = SummonerSpellType.Exhaust;
                break;
            default:
                break;
        }

        if (spellType == SpellHolder.spellD)
        {
            // swap
            SummonerSpellType temp = SpellHolder.spellF;
            SpellHolder.spellF = SpellHolder.spellD;
            SpellHolder.spellD = temp;
        }
        else
        {
            SpellHolder.spellF = spellType;
        }

        UpdateSpellImages();
    }

    void UpdateSpellImages()
    {
        // Update Spell D Button Image
        switch (SpellHolder.spellD)
        {
            case SummonerSpellType.Flash:
                SpellDButton.GetComponent<Image>().sprite = SpellImages[0].sprite;
                SpellHolder.spellDImage = SpellImages[0];
                break;
            case SummonerSpellType.Ghost:
                SpellDButton.GetComponent<Image>().sprite = SpellImages[1].sprite;
                SpellHolder.spellDImage = SpellImages[1];
                break;
            case SummonerSpellType.Barrier:
                SpellDButton.GetComponent<Image>().sprite = SpellImages[2].sprite;
                SpellHolder.spellDImage = SpellImages[2];
                break;
            case SummonerSpellType.Exhaust:
                SpellDButton.GetComponent<Image>().sprite = SpellImages[3].sprite;
                SpellHolder.spellDImage = SpellImages[3];
                break;
            default:
                break;
        }

        // Update Spell F Button Image
        switch (SpellHolder.spellF)
        {
            case SummonerSpellType.Flash:
                SpellFButton.GetComponent<Image>().sprite = SpellImages[0].sprite;
                SpellHolder.spellFImage = SpellImages[0];
                break;
            case SummonerSpellType.Ghost:
                SpellFButton.GetComponent<Image>().sprite = SpellImages[1].sprite;
                SpellHolder.spellFImage = SpellImages[1];
                break;
            case SummonerSpellType.Barrier:
                SpellFButton.GetComponent<Image>().sprite = SpellImages[2].sprite;
                SpellHolder.spellFImage = SpellImages[2];
                break;
            case SummonerSpellType.Exhaust:
                SpellFButton.GetComponent<Image>().sprite = SpellImages[3].sprite;
                SpellHolder.spellFImage = SpellImages[3];
                break;
            default:
                break;
        }
    }
}
