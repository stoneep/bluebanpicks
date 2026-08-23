using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LobbyViewer : MonoBehaviour
{
    [SerializeField] private Button workBtn;
    public SceneAsset workScene;
    public const string VIEWER_SCENE_CAMPAIGN = "CampaignLobby";
    
    private void Awake()
    {
        if (workBtn != null) 
            workBtn.onClick.AddListener(OnWorkButtonClicked);
    }

    private void OnWorkButtonClicked() => SceneManager.LoadScene(VIEWER_SCENE_CAMPAIGN);

    private void OnDestroy()
    {
        if (workBtn != null)
        {
            workBtn.onClick.RemoveListener(OnWorkButtonClicked);
        }
    }
}
