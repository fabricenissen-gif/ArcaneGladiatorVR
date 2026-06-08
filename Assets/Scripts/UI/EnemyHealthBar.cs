using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour
{
    [SerializeField] private Image fillImage;

    public void SetNormalized(float value)
    {
        if (fillImage == null)
            return;

        fillImage.fillAmount = Mathf.Clamp01(value);
    }
}