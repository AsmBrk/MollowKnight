using UnityEngine;
using UnityEngine.SceneManagement; 
using UnityEngine.Audio;

public class MainMenu_sc : MonoBehaviour
{
    public AudioMixer audioMixer;

    public void PlayGame()
    {
        SceneManager.LoadScene("Sahnem"); 
    }

    public void QuitGame()
    {
        Debug.Log("Oyundan Çıkıldı!"); 
        Application.Quit();
    }

    public void SetMusicVolume(float value)
    {
        audioMixer.SetFloat("MusicVol", Mathf.Log10(value) * 20);
    }

}
