using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BattleUIManager : MonoBehaviour
{
    public GameObject BluePlayer;
    HealthEX BluePlayerHealth;
    private UltGauge BlueUlt;
    
    public GameObject RedPlayer;
    HealthEX RedPlayerHealth;
    public Image BlueHPGauge;
    public Image RedHPGauge;
    public SkillDefinition[] SkillDefs; // 0~2: Skills
    public Image[] CDImgs; // 0~2: Skill CDs
    public TextMeshProUGUI[] CDTexts;
    public Image UltMask;
    public Image UltGauge;
    public TextMeshProUGUI UltText;
    public GameObject SpellDIcon;
    public GameObject SpellFIcon;
    public SkillRunner playerRunner;
    float[] coolDowns;
    bool[] isOnCD;

    void Awake()
    {
        SpellDIcon.GetComponent<Image>().sprite = SpellHolder.spellDImage.sprite;
        SpellFIcon.GetComponent<Image>().sprite = SpellHolder.spellFImage.sprite;
    }

    void Start()
    {
        BluePlayerHealth = BluePlayer.GetComponentInChildren<HealthEX>();
        RedPlayerHealth = RedPlayer.GetComponentInChildren<HealthEX>();
        
        BlueUlt = BluePlayer.GetComponentInParent<UltGauge>();
        if (!BlueUlt) BlueUlt = BluePlayer.GetComponentInChildren<UltGauge>();
        
        coolDowns = new float[CDImgs.Length];
        isOnCD = new bool[CDImgs.Length];
        UltGauge.fillAmount = 1f;

        for (int i = 0; i < CDImgs.Length; i++)
        {
            CDImgs[i].fillAmount = 0f;
            CDTexts[i].text = "";
            isOnCD[i] = false;
        }
    }
    
    /*
    public void StartCooldown(char skill)
    {
        switch (skill)
        {
            case 'Q':
            case 'q':
                if (!isOnCD[0])
                {
                    coolDowns[0] = SkillDefs[0].cooldown;
                    CDImgs[0].fillAmount = 1f;
                    isOnCD[0] = true;
                }
                break;
            case 'W':
            case 'w':
                if (!isOnCD[1])
                {
                    coolDowns[1] = SkillDefs[1].cooldown;
                    CDImgs[1].fillAmount = 1f;
                    isOnCD[1] = true;
                }
                break;

            case 'E':
            case 'e':
                if (!isOnCD[2])
                {
                    coolDowns[2] = SkillDefs[2].cooldown;
                    CDImgs[2].fillAmount = 1f;
                    isOnCD[2] = true;
                }
                break;

            case 'D':
                if (!isOnCD[3])
                {
                    coolDowns[3] = cooldown;
                    CDImgs[3].fillAmount = 1f;
                    isOnCD[3] = true;
                }
                break;
            case 'F':
                if (!isOnCD[4])
                {
                    coolDowns[4] = cooldown;
                    CDImgs[4].fillAmount = 1f;
                    isOnCD[4] = true;
                }
                break;

        }
    }
*/
    void UltGaugeUpdate()
    {
        if (BlueUlt != null)
        {
            float p = BlueUlt.GaugePercent; // 0~100
            UltText.text = (p >= 100f) ? "" : Mathf.FloorToInt(p).ToString() + "%";

            UltGauge.fillAmount = (100 - p)/ 100f;

            if (UltGauge.fillAmount <= 0)
            {
                UltMask.enabled = false;
                UltText.text = "";
            }
        }
    }
    
    void UpdateCdUI(int idx, SkillSlot slot)
    {
        float remain = playerRunner.GetCooldownRemaining(slot);
        float dur = playerRunner.GetCooldownDuration(slot);

        if (remain > 0f)
        {
            CDImgs[idx].fillAmount = remain / dur; // 1 -> 0
            CDTexts[idx].text = Mathf.CeilToInt(remain).ToString();
        }
        else
        {
            CDImgs[idx].fillAmount = 0f;
            CDTexts[idx].text = "";
        }
    }

    // Update is called once per frame
    void Update()
    {
        
        UpdateCdUI(0, SkillSlot.Q);
        UpdateCdUI(1, SkillSlot.W);
        UpdateCdUI(2, SkillSlot.E);
        
        BlueHPGauge.fillAmount = BluePlayerHealth.hp / 100f;
        RedHPGauge.fillAmount = RedPlayerHealth.hp / 100f;
        
        UltGaugeUpdate();
        
        for (int i = 0; i < CDImgs.Length; i++)
        {
            if (isOnCD[i])
            {
                CDImgs[i].fillAmount -= Time.deltaTime / SkillDefs[i].cooldown;
                coolDowns[i] -= Time.deltaTime;
                CDTexts[i].text = Mathf.CeilToInt(coolDowns[i]).ToString();

                if (coolDowns[i] <= 0f)
                {
                    CDImgs[i].fillAmount = 0f;
                    isOnCD[i] = false;
                    CDTexts[i].text = "";
                }
            }
        }
        
    }
}
