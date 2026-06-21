using UnityEngine;

public class CardSpawner : MonoBehaviour
{
    [SerializeField] private GameObject cardPrefab;
    
    public void SpawnCard()
    {
        Instantiate(cardPrefab, transform.position, Quaternion.identity, transform);
    }
}
