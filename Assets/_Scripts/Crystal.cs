using UnityEngine;

public class Crystal : MonoBehaviour
{
    public GameObject Field;
    Transform fieldTransform;
    [SerializeField] float maxAbsZ;
    [SerializeField] GameObject Timer; 
    [SerializeField] GameObject BlueCrystal;
    [SerializeField] GameObject RedCrystal;
    
    BattleTimer battleTimer;
    float crystalTimer;
    bool isFirst = true;
    bool isSpawned = false;
    bool isOneDestroyed = false;
    float timeLimit = 0;

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
            if (isOneDestroyed)
            {
                timeLimit += Time.deltaTime;
                if (timeLimit >= 2f)
                    SpawnEnd(); 
            }
        }
    }

    void SpawnCrystal(float randomX, float randomZ)
    {
        if (!isSpawned)
        {
            GameObject blue = Instantiate(BlueCrystal, new Vector3(-randomX, 0.5f, randomZ) + fieldTransform.position, Quaternion.identity);
            SetupCrystal(blue, TeamId.A);

            GameObject red = Instantiate(RedCrystal, new Vector3(randomX, 0.5f, randomZ) + fieldTransform.position, Quaternion.identity);
            SetupCrystal(red, TeamId.B);

            isSpawned = true;
            
        }
    }

    void SpawnEnd()
    {
        try
        {
            Destroy(BlueCrystal);
        }
        catch { }

        try
        {
            Destroy(RedCrystal);
        }
        catch { }
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
