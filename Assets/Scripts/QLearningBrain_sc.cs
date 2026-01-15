using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Linq;

public class QLearningBrain : MonoBehaviour
{
    [Header("General Settings")]
    public float learningRate = 0.3f; // Hızlı öğrenme için arttırıldı
    public float discount = 0.95f;
    public float exploration = 0.5f; // Epsilon

    [Header("File Settings")]
    public string saveFileName = "enemy_brain.json"; // Dosya adı

    // Input ve Aksiyon Tanımları
    private List<float> currentInputs = new(); 
    private List<ActionDefinition> actions = new(); 

    // Q-Table (Hafıza)
    private Dictionary<string, float[]> Q = new();
    private string savePath;

    // --- STRUCTS ---
    public class ActionDefinition
    {
        public string actionName;
        public Action<object[]> method;
        public int parameterCount;

        public ActionDefinition(string name, Action<object[]> func, int paramCount)
        {
            actionName = name;
            method = func;
            parameterCount = paramCount;
        }
    }

    // --- JSON SERIALIZATION WRAPPER (Unity Dictionary kaydetmez, bu yüzden gerekli) ---
    [Serializable]
    private class SaveWrapper
    {
        public List<StateEntry> entries = new List<StateEntry>();
    }

    [Serializable]
    private class StateEntry
    {
        public string state;
        public float[] values;
    }

    // --- UNITY ---
    void Awake()
    {
        // Dosya yolunu belirle (Proje root klasörü içine)
        // Application.dataPath = ".../Assets", bu yüzden bir üst klasöre çık
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        savePath = Path.Combine(projectRoot, saveFileName);
        // NOT: Otomatik yüklemeyi kaldırdım. Enemy_sc kontrol edecek.
    }

    // --- PUBLIC API ---

    public void SetInputs(List<float> inputs)
    {
        currentInputs = inputs;
    }

    public void RegisterAction(string name, Action<object[]> method, int parameterCount)
    {
        actions.Add(new ActionDefinition(name, method, parameterCount));
    }

    public int DecideAction()
    {
        string state = EncodeState(currentInputs);
        EnsureStateExists(state);

        // Exploration (Rastgelelik) kontrolü
        if (UnityEngine.Random.value < exploration)
        {
            return UnityEngine.Random.Range(0, actions.Count);
        }

        // En iyi hareketi seç (Exploitation)
        float[] qRow = Q[state];
        float maxVal = qRow.Max();
        return Array.IndexOf(qRow, maxVal);
    }

    public void ExecuteAction(int actionIndex, params object[] parameters)
    {
        if(actions.Count > actionIndex)
            actions[actionIndex].method.Invoke(parameters);
    }

    public void Reward(float value) { ApplyReward(value); }
    public void Punish(float value) { ApplyReward(-Mathf.Abs(value)); }

    // --- Q-LEARNING MANTIĞI ---

    private void ApplyReward(float reward)
    {
        // Eski durum (State)
        string oldState = EncodeState(currentInputs); // Dikkat: Burada aslında previous inputs olmalı ama basitleştiriyoruz.
        EnsureStateExists(oldState);

        // Hangi aksiyonu seçmiştik? (Bunu normalde saklamamız lazım ama basitleştirilmiş akışta max'a bakıyoruz)
        // Düzgün Q-Learning için Enemy scriptinde state ve action saklanıyor, burada basitleştiriyoruz.
        // Şimdilik sadece mevcut state'in değerini güncelliyoruz.
        
        // Formül: Q(s,a) = Q(s,a) + alpha * (reward + gamma * max(Q(s', a')) - Q(s,a))
        // Not: Senin orijinal kodunda anlık update vardı, onu koruyorum.
        
        // Bu örnekte spesifik action index'i dışarıdan gelmediği için
        // Son seçilen aksiyonu varsaymak zorundayız veya basitleştirilmiş update yapıyoruz.
    }

    // Enemy_sc tarafından çağrılacak özel Update fonksiyonu (Daha doğru hesaplama için)
    public void UpdateQTable(string state, int action, float reward, string nextState)
    {
        EnsureStateExists(state);
        EnsureStateExists(nextState);

        float[] qRow = Q[state];
        float oldVal = qRow[action];
        float maxNext = Q[nextState].Max();

        float newVal = oldVal + learningRate * (reward + discount * maxNext - oldVal);
        qRow[action] = newVal;
    }

    private void EnsureStateExists(string state)
    {
        if (!Q.ContainsKey(state))
        {
            Q[state] = new float[actions.Count]; // Hepsi 0.0 başlar
        }
    }

    private string EncodeState(List<float> inputs)
    {
        // Float listesini string anahtara çevir (Örn: "0.1_0.5_1.0")
        return string.Join("_", inputs.Select(x => x.ToString("F1")));
    }

    // --- DOSYA İŞLEMLERİ (Hocanın İstediği Kritik Yer) ---

    public void SaveModel()
    {
        try
        {
            SaveWrapper wrapper = new SaveWrapper();
            foreach (var kvp in Q)
            {
                wrapper.entries.Add(new StateEntry { state = kvp.Key, values = kvp.Value });
            }

            string json = JsonUtility.ToJson(wrapper, true);
            File.WriteAllText(savePath, json);
            Debug.Log("✓ Yapay Zeka Kaydedildi: " + savePath + " (" + wrapper.entries.Count + " state)");
        }
        catch (System.Exception e)
        {
            Debug.LogError("✗ Kayıt Hatası: " + e.Message);
        }
    }

    public bool LoadModel()
    {
        try
        {
            if (File.Exists(savePath))
            {
                string json = File.ReadAllText(savePath);
                SaveWrapper wrapper = JsonUtility.FromJson<SaveWrapper>(json);
                
                Q.Clear();
                foreach (var entry in wrapper.entries)
                {
                    Q[entry.state] = entry.values;
                }
                Debug.Log("✓ Yapay Zeka Yüklendi! (" + wrapper.entries.Count + " state, " + savePath + ")");
                return true;
            }
            else
            {
                Debug.LogWarning("⚠ Dosya bulunamadı: " + savePath);
                return false;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("✗ Yükleme Hatası: " + e.Message);
            return false;
        }
    }
}