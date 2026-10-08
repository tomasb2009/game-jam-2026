using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    private AudioSource audioSource;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        audioSource = GetComponent<AudioSource>();
    }

    public void ReproducirSonido(AudioClip sonido)
    {
        if (sonido != null)
        {
            audioSource.PlayOneShot(sonido);
        }
    }
}