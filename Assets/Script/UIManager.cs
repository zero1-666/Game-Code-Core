using UnityEngine;
using UnityEngine.UI;
using TMPro; // 1. 必须有这一行才能支持 TextMeshPro
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("UI 面板引用")]
    public GameObject gameOverPanel;
    public GameObject hintTextObject;

    [Header("文字组件引用")]
    public TMP_Text hintText; // 2. 修改为 TMP_Text
    public TMP_Text hpText;   // 3. 修改为 TMP_Text

    [Header("设置")]
    public float hintDuration = 2.0f;
    private float hintTimer = 0f;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (hintTextObject != null) hintTextObject.SetActive(false);
    }

    void Update()
    {
        if (hintTimer > 0)
        {
            hintTimer -= Time.deltaTime;
            if (hintTimer <= 0 && hintTextObject != null) hintTextObject.SetActive(false);
        }
    }

    public void ShowHint(string message)
    {
        if (hintText != null && hintTextObject != null)
        {
            hintText.text = message;
            hintTextObject.SetActive(true);
            hintTimer = hintDuration;
        }
    }

    public void ShowGameOver()
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void UpdateHPDisplay(int currentHP)
    {
        if (hpText != null) hpText.text = "Remaining life: " + currentHP;
    }

    public void RetryGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}