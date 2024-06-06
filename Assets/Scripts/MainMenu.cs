using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;


public class MainMenu : MonoBehaviour {

    public Settings settings;

    public GameObject titleMenuObject;
    public GameObject settingsMenuObject;
    public GameObject startMenuObject;

    [Header("Settings Menu")]
    public Slider viewDistanceSlider;
    public TextMeshProUGUI viewDistanceText;
    public Slider mouseSensitivitySlider;
    public TextMeshProUGUI mouseSensitivityText;
    public Toggle threadingToggle;

    [Header("Start Menu")]
    public TextMeshProUGUI seedField;


    public void Awake() {

        if (!File.Exists(Application.dataPath + "/settings.cfg")) {

            settings = new Settings();
            string settingsJSON = JsonUtility.ToJson(settings);
            File.WriteAllText(Application.dataPath + "/settings.cfg", settingsJSON);

        } else {

            string settingsJSON = File.ReadAllText(Application.dataPath + "/settings.cfg");
            settings = JsonUtility.FromJson<Settings>(settingsJSON);

        }
    }

    public void QuitGame() { Application.Quit(); }

    // ========== Title Menu ========== //

    public void EnterTitle() {
        titleMenuObject.SetActive(true);
    }

    public void LeaveTitle() {
        titleMenuObject.SetActive(false);
    }

    // ========== Settings Menu ========== //

    public void EnterSettings() {

        viewDistanceSlider.value = settings.viewDistance;
        viewDistanceText.text = "View Distance: " + viewDistanceSlider.value;
        mouseSensitivitySlider.value = settings.mouseSensitivity;
        mouseSensitivityText.text = "Mouse Sensitivity: " + mouseSensitivitySlider.value;
        threadingToggle.isOn = settings.enableThreading;

        settingsMenuObject.SetActive(true);
    }

    public void UpdateViewDistanceSlider() {
        viewDistanceText.text = "View Distance: " + viewDistanceSlider.value;
    }

    public void UpdateMouseSensitivitySlider() {
        mouseSensitivityText.text = "Mouse Sensitivity: " + mouseSensitivitySlider.value.ToString("F1");
    }

    public void LeaveSettings() {

        settings.viewDistance = (int)viewDistanceSlider.value;
        settings.mouseSensitivity = mouseSensitivitySlider.value;
        settings.enableThreading = threadingToggle.isOn;

        string settingsJSON = JsonUtility.ToJson(settings);
        File.WriteAllText(Application.dataPath + "/settings.cfg", settingsJSON);

        settingsMenuObject.SetActive(false);
    }

    // ========== World Menu ========== //

    public void EnterStart() {
        startMenuObject.SetActive(true);
    }

    public void LeaveStart() {
        startMenuObject.SetActive(false);
    }

    public void StartGame() {
        VoxelData.seed = Mathf.Abs(seedField.text.GetHashCode()) / VoxelData.worldSizeInChunks;
        SceneManager.LoadScene("World", LoadSceneMode.Single);
    }

}
