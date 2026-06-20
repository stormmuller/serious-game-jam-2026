using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class PrizeDissolve : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

    private bool isDissolving;

    public void BeginDissolve(float duration)
    {
        if (isDissolving)
        {
            return;
        }

        isDissolving = true;
        StartCoroutine(FadeOut(duration));
    }

    private IEnumerator FadeOut(float duration)
    {
        Material material = GetComponent<Renderer>().material;
        float startAlpha = material.GetColor(BaseColorId).a;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            Color color = material.GetColor(BaseColorId);
            color.a = Mathf.Lerp(startAlpha, 0f, elapsed / duration);
            material.SetColor(BaseColorId, color);
            yield return null;
        }

        Destroy(gameObject);
    }
}
