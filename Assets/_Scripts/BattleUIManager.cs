using UnityEngine;
using UnityEngine.UI;

public class BattleUIManager : MonoBehaviour
{
    [SerializeField]
    Image[] CDImgs; // 0~2: Skill CDs, 3~4: Spell CDs
    Image UltGauge;
    float[] coolDowns;
    bool[] isOnCD;


    void Start()
    {
        coolDowns = new float[CDImgs.Length];
        isOnCD = new bool[CDImgs.Length];
        UltGauge.fillAmount = 1f;

        for (int i = 0; i < CDImgs.Length; i++)
        {
            CDImgs[i].fillAmount = 0f;
            isOnCD[i] = false;
        }
    }

    public void StartCooldown(char skill, float cooldown)
    {
        switch (skill)
        {
            case 'Q':
            case 'q':
                if (!isOnCD[0])
                {
                    coolDowns[0] = cooldown;
                    CDImgs[0].fillAmount = 1f;
                    isOnCD[0] = true;
                }
                break;
            case 'W':
            case 'w':
                if (!isOnCD[1])
                {
                    coolDowns[1] = cooldown;
                    CDImgs[1].fillAmount = 1f;
                    isOnCD[1] = true;
                }
                break;

            case 'E':
            case 'e':
                if (!isOnCD[2])
                {
                    coolDowns[2] = cooldown;
                    CDImgs[2].fillAmount = 1f;
                    isOnCD[2] = true;
                }
                break;
            /*
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
            */
        }
    }

    public void ChargeUlt(float amount)
    {
        UltGauge.fillAmount -= amount / 100f;

    }

    // Update is called once per frame
    void Update()
    {
        for (int i = 0; i < CDImgs.Length; i++)
        {
            if (isOnCD[i])
            {
                CDImgs[i].fillAmount -= Time.deltaTime / coolDowns[i];

                if (CDImgs[i].fillAmount <= 0f)
                {
                    isOnCD[i] = false;
                }
            }
        }

        StartCooldown('Q', 6f);
    }
}
