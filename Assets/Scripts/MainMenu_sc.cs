using UnityEngine;
using UnityEngine.SceneManagement; 
using UnityEngine.Audio;
using UnityEngine.UI; 

public class MainMenu_sc : MonoBehaviour
{
    public AudioMixer audioMixer;
    public Toggle aiToggle; 

    // --- BU FONKSİYON EKSİKTİ, BUNU EKLEDİK ---
    void Start()
    {
        // 1. Menü açılır açılmaz önceki oyundan kalan hafızayı sil
        Enemy_sc.LoadAIFromDisk = false;
        Enemy_sc.TrainMode = false;

        // 2. Kutucuğu da görsel olarak boşalt (Tiki kaldır)
        if (aiToggle != null)
        {
            aiToggle.isOn = false;
        }

        Debug.Log("Menü Başladı: Tüm ayarlar sıfırlandı.");
    }
    // -------------------------------------------

    public void PlayGame()
    {
        // Kutucuk işaretliyse AI dosyasını yükle
        if (aiToggle != null)
        {
            Enemy_sc.LoadAIFromDisk = aiToggle.isOn;
            Debug.Log("Yapay Zeka Modu: " + (aiToggle.isOn ? "YÜKLE" : "RASTGELE"));
        }
        else
        {
            // Eğer kutucuğu sürüklemeyi unuttuysan varsayılan olarak KAPALI olsun
            Enemy_sc.LoadAIFromDisk = false; 
        }

        Enemy_sc.TrainMode = false; // Normal modda olduğumuz kesinleşsin
        SceneManager.LoadScene("Sahnem"); 
    }

    public void TrainAI()
    {
        Enemy_sc.TrainMode = true;
        Enemy_sc.LoadAIFromDisk = false; 
        
        Debug.Log("★ EĞİTİM MODUNA GEÇİLDİ");
        Debug.Log("Düşman rastgele hareket edecek. ~1-2 dakika sonra P tuşuna basarak kaydedin!");
        
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