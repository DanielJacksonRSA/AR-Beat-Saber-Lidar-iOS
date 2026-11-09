using UnityEngine;

public class MissZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Note"))
        {
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.RegisterMissOrBomb();
                ScoreManager.Instance.ShowFeedback("MISS");
            }

            if (HealthManager.Instance != null) HealthManager.Instance.TakeMissDamage();

            Destroy(other.gameObject);
        }
        else if (other.CompareTag("Bomb"))
        {
            Destroy(other.gameObject);
        }
    }
}
