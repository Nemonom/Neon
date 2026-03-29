using UnityEngine;

public class Manager : MonoBehaviour
{
    [Header("찍어낼 껍데기 (Prefab)")]
    public GameObject enemyPrefab;

    [Header("적군 데이터베이스")]
    public EnemyDatas[] enemyTypes;

    void Update()
    {
        // 예시: 스페이스바 누르면 적 생성
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SpawnEnemy();
        }
    }

    private void SpawnEnemy()
    {
        // 1. 어떤 종류의 적을 뽑을지 데이터 배열에서 랜덤으로 하나 선택
        EnemyDataSO selectedData = enemyTypes[Random.Range(0, enemyTypes.Length)];

        // 2. 껍데기(프리팹) 생성
        Vector3 spawnPos = new Vector3(Random.Range(-5f, 5f), 5f, 0f);
        GameObject newEnemyObj = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);

        // 3. 생성된 껍데기에게 "너는 이제 이 데이터대로 살아라" 하고 주입!
        EnemyController controller = newEnemyObj.GetComponent<EnemyController>();
        controller.Initialize(selectedData);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }
}
