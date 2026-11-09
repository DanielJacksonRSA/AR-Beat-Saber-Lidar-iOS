using UnityEngine;
using System.Runtime.InteropServices;

public class SaberBlade : MonoBehaviour
{
    [Header("Game Juice - Notes")]
    [Tooltip("Drag your Spark Particle System prefab here")]
    public GameObject sparkPrefab;
    [Tooltip("Drag a satisfying slice/shatter MP3 or WAV here")]
    public AudioClip sliceSound;

    [Header("Game Juice - Bombs")]
    [Tooltip("Drag your Explosion Particle System prefab here")]
    public GameObject explosionPrefab;
    [Tooltip("Drag a loud boom/error MP3 or WAV here")]
    public AudioClip explosionSound;

    private AudioSource audioSource;

    // iOS: system sound 1520 triggers a short haptic tap.
    #if UNITY_IOS
    [DllImport("__Internal")]
    private static extern void AudioServicesPlaySystemSound(int id);
    #endif

    // Android: call the system vibrator directly.
    #if UNITY_ANDROID
    private AndroidJavaObject vibrator;
    #endif

    void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;

        #if UNITY_ANDROID
        using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        {
            AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
            vibrator = currentActivity.Call<AndroidJavaObject>("getSystemService", "vibrator");
        }
        #endif
    }

    private void OnTriggerEnter(Collider other)
    {
        // --- HIT A NORMAL NOTE ---
        if (other.CompareTag("Note"))
        {
            // 1. SOUND EFFECT
            if (sliceSound != null) audioSource.PlayOneShot(sliceSound);

            // 2. CRISP HAPTIC TAP
            TriggerLightHaptic();

            // 3. SPAWN SPARKS
            if (sparkPrefab != null) Instantiate(sparkPrefab, other.transform.position, Quaternion.identity);

            // 4. SCORE & HEALTH
            if (ScoreManager.Instance != null) ScoreManager.Instance.RegisterHit();
            if (HealthManager.Instance != null) HealthManager.Instance.AddEnergy();

            // 5. DESTROY THE CUBE
            Destroy(other.gameObject);
        }
        // --- HIT A BOMB ---
        else if (other.CompareTag("Bomb"))
        {
            // 1. SOUND EFFECT (Loud Boom/Error)
            if (explosionSound != null) audioSource.PlayOneShot(explosionSound);

            // 2. HEAVY HAPTIC SHAKE
            TriggerHeavyHaptic();

            // 3. SPAWN EXPLOSION
            if (explosionPrefab != null) Instantiate(explosionPrefab, other.transform.position, Quaternion.identity);

            // 4. SCORE, HEALTH & UI WARNING
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.RegisterMissOrBomb();
                ScoreManager.Instance.ShowFeedback("BOMB");
            }
            if (HealthManager.Instance != null) HealthManager.Instance.TakeBombDamage();

            // 5. DESTROY THE BOMB
            Destroy(other.gameObject);
        }
    }

    private void TriggerLightHaptic()
    {
    #if UNITY_EDITOR
    #elif UNITY_IOS
        AudioServicesPlaySystemSound(1520);
    #elif UNITY_ANDROID
        if (vibrator != null) vibrator.Call("vibrate", 25L);
        else Handheld.Vibrate();
    #endif
    }

    private void TriggerHeavyHaptic()
    {
    #if UNITY_EDITOR
    #elif UNITY_IOS
        Handheld.Vibrate();
    #elif UNITY_ANDROID
        if (vibrator != null) vibrator.Call("vibrate", 150L);
        else Handheld.Vibrate();
    #endif
    }
}
