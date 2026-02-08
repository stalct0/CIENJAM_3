using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion; // 퓨전 참조 추가

public class BattleUIManager : MonoBehaviour
{
    public GameObject Canvas;
    
    [Header("Players")]
    public GameObject BluePlayer; // GameManager/Player 스크립트에서 할당됨
    public GameObject RedPlayer;  // GameManager/Player 스크립트에서 할당됨

    private HealthEX _blueHealth;
    private HealthEX _redHealth;
    private UltGauge _blueUlt;

    [Header("HP Gauges")]
    public Image BlueHPGauge;
    public Image RedHPGauge;

    [Header("Skills")]
    public SkillDefinition[] SkillDefs; 
    public Image[] CDImgs; 
    public TextMeshProUGUI[] CDTexts;

    [Header("Ultimate")]
    public Image UltMask;
    public Image UltGauge;
    public TextMeshProUGUI UltText;

    [Header("Summoner Spells")]
    public GameObject SpellDIcon;
    public GameObject SpellFIcon;

    [Header("Runners")]
    public SkillRunner playerRunner;     // Player.cs에서 할당됨
    public SummonerSpellRunner spellRunner; // Player.cs에서 할당됨

    private bool _isInitialized = false;
    private float elapsedTime = 0f;

    // OnEnable 대신, Player가 할당되었는지 체크하는 로직으로 변경
    private bool CheckAndInit()
    {
        if (_isInitialized) return true;
        if (BluePlayer == null || RedPlayer == null || playerRunner == null || spellRunner == null) return false;

        // 플레이어가 할당된 직후 한 번만 컴포넌트 가져오기
        _blueHealth = BluePlayer.GetComponentInChildren<HealthEX>();
        _redHealth = RedPlayer.GetComponentInChildren<HealthEX>();
        
        _blueUlt = BluePlayer.GetComponentInParent<UltGauge>();
        if (!_blueUlt) _blueUlt = BluePlayer.GetComponentInChildren<UltGauge>();

        // 초기화 완료
        _isInitialized = true;
        Debug.Log("[UI] 모든 플레이어 컴포넌트 연결 완료");
        return true;
    }

    void Update()
    {
        // 1. 초기 대기 시간 (게임 시작 연출 등)
        if (elapsedTime < 10f)
        {
            elapsedTime += Time.deltaTime;
            return;
        }

        // 2. 초기화 확인 (플레이어 할당 대기)
        if (!CheckAndInit()) return;

        // 3. 캔버스 활성화
        if (!Canvas.activeSelf) Canvas.SetActive(true);
        // 4. 스킬 쿨다운 업데이트 (Runner에서 직접 가져옴)
        UpdateCdUI(0, SkillSlot.Q);
        UpdateCdUI(1, SkillSlot.W);
        UpdateCdUI(2, SkillSlot.E);

        // 5. HP바 업데이트 (HealthEX의 hp가 [Networked]여야 실시간 동기화됨)
        if (_blueHealth != null) BlueHPGauge.fillAmount = _blueHealth.hp / 100f;
        if (_redHealth != null) RedHPGauge.fillAmount = _redHealth.hp / 100f;

        // 6. 궁극기 및 소환사 주문 업데이트
        UltGaugeUpdate();
        UpdateSpCdUI();
    }

    void UpdateCdUI(int idx, SkillSlot slot)
    {
        if (playerRunner == null) return;

        float remain = playerRunner.GetCooldownRemaining(slot);
        float dur = playerRunner.GetCooldownDuration(slot);

        if (remain > 0f)
        {
            CDImgs[idx].fillAmount = remain / dur;
            CDTexts[idx].text = Mathf.CeilToInt(remain).ToString();
        }
        else
        {
            CDImgs[idx].fillAmount = 0f;
            CDTexts[idx].text = "";
        }
    }

    void UpdateSpCdUI()
    {
        if (spellRunner == null || spellRunner.spellD == null) return;

        // 소환사 주문 쿨다운 (Time.time 대신 퓨전의 Runner.SimulationTime 권장이나 현재 구조 유지)
        float dRemain = spellRunner.cdEnd[SummonerSlot.D] - Time.time;
        float fRemain = spellRunner.cdEnd[SummonerSlot.F] - Time.time;
        
        UpdateSingleSpellUI(SpellDIcon, dRemain, spellRunner.spellD.cooldownSeconds);
        UpdateSingleSpellUI(SpellFIcon, fRemain, spellRunner.spellF.cooldownSeconds);
    }

    private void UpdateSingleSpellUI(GameObject iconObj, float remain, float total)
    {
        var img = iconObj.GetComponentInChildren<Image>();
        var txt = iconObj.GetComponentInChildren<TextMeshProUGUI>();

        if (remain > 0f)
        {
            img.fillAmount = remain / total;
            txt.text = Mathf.CeilToInt(remain).ToString();
        }
        else
        {
            img.fillAmount = 0f;
            txt.text = "";
        }
    }

    void UltGaugeUpdate()
    {
        if (_blueUlt != null)
        {
            float p = _blueUlt.GaugePercent; 
            UltText.text = (p >= 100f) ? "" : Mathf.FloorToInt(p).ToString() + "%";
            UltGauge.fillAmount = (100f - p) / 100f;

            UltMask.enabled = (p < 100f);
        }
    }
}