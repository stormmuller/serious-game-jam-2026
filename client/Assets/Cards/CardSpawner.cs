using UnityEngine;

public class CardSpawner : MonoBehaviour
{
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private int numberOfCardsToSpawn;
    
    public void SpawnCards()
    {
        for (int i = 0; i < numberOfCardsToSpawn; i++)
        {
            Instantiate(cardPrefab, transform.position, Quaternion.identity);
        }
    }
}
