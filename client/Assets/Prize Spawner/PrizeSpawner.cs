using UnityEngine;

[CreateAssetMenu(fileName = "Prize Spawner", menuName = "Prizes/Prize Spawner")]
public class PrizeSpawner : ScriptableObject
{
  public float minXSpawnPoint;
  public float maxXSpawnPoint;

  void OnValidate()
  {
    if (minXSpawnPoint > maxXSpawnPoint)
    {
      (maxXSpawnPoint, minXSpawnPoint) = (minXSpawnPoint, maxXSpawnPoint);
    }
  }

  public void SpawnPrize(Prize prize)
  {
    float randomX = Random.Range(minXSpawnPoint, maxXSpawnPoint);
    Vector3 spawnPosition = new(randomX, 0, 0);

    Instantiate(prize.prizePrefab, spawnPosition, Quaternion.identity);
  }
}
