using UnityEngine;

public class Crystal : MonoBehaviour
{
    public GameObject Field;
    Transform fieldTransform;
    [SerializeField] float maxAbsZ;
    [SerializeField] GameObject Timer; 
    [SerializeField] GameObject BlueCrystal;
    [SerializeField] GameObject RedCrystal;

    GameObject blue;
    GameObject red;
    
    BattleTimer battleTimer;
    float crystalTimer;
    bool isFirst = true;
    bool isSpawned = false;
    bool isOneDestroyed = false;
    float timeLimit = 0f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        battleTimer = Timer.GetComponent<BattleTimer>();
        fieldTransform = Field.GetComponent<Transform>();
    }

    // Update is called once per frame
    void Update()
    { 
        if (!isSpawned && isFirst)
        {
            crystalTimer = battleTimer.battleTime;
            if (crystalTimer >= 15f)
            {
                float randomZ = Random.Range(-maxAbsZ, maxAbsZ);
                float randomX = Random.Range(2f, maxAbsZ);
                SpawnCrystal(randomX, randomZ);
                isFirst = false;
            }
        }

        if (!isSpawned && !isFirst)
        {
            crystalTimer += Time.deltaTime;
            if (crystalTimer >= 12f)
            {
                float randomZ = Random.Range(-maxAbsZ, maxAbsZ);
                float randomX = Random.Range(2f, maxAbsZ);
                SpawnCrystal(randomX, randomZ);
            }
        }

        if (isSpawned)
        {
            bool blueDead = blue == null;
            bool redDead = red == null;
            if (blueDead && redDead) 
            {
                SpawnEnd();
                return;
            }
            
            isOneDestroyed = (blueDead || redDead) && !(blueDead && redDead);
            
            if (isOneDestroyed)
            {
                timeLimit += Time.deltaTime;
                if (timeLimit >= 2f)
                {
                    SpawnEnd();
                }
            }
        }
    }

    void SpawnCrystal(float randomX, float randomZ)
    {
        if (!isSpawned)
        {
            blue = Instantiate(BlueCrystal, new Vector3(-randomX, 0.8f, randomZ) + fieldTransform.position, Quaternion.identity);
            SetupCrystal(blue, TeamId.A);

            red = Instantiate(RedCrystal, new Vector3(randomX, 0.8f, randomZ) + fieldTransform.position, Quaternion.identity);
            SetupCrystal(red, TeamId.B);

            isSpawned = true;
            timeLimit = 0f;
        }
    }

    void SpawnEnd()
    {
        if (blue != null) Destroy(blue);
        if (red != null) Destroy(red);
        isOneDestroyed = false;
        crystalTimer = 0f;
        timeLimit = 0f;
        isSpawned = false;
    }
    
    void SetupCrystal(GameObject crystal, TeamId ownerTeam)
    {
        // 1. 접근 규칙
        var rule = crystal.GetComponent<CrystalAccessRule>();
        rule.ownerTeam = ownerTeam;
        rule.access = CrystalAccessType.EnemyOnly;
    }
}
