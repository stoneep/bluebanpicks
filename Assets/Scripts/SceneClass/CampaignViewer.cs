using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System; // Array.ForEach 사용을 위해 추가

public class CampaignViewer : MonoBehaviour
{
    [Header("Navigation")]
    [SerializeField] private Button backBtn;

    [Header("Content Buttons")]
    [SerializeField] private Button missionBtn;
    [SerializeField] private Button storyBtn;
    [SerializeField] private Button bountyBtn;
    [SerializeField] private Button commissionsBtn;
    [SerializeField] private Button scrimmageBtn;
    [SerializeField] private Button totalAssaultBtn;
    [SerializeField] private Button jointFiringDrillBtn;
    [SerializeField] private Button grandAssaultBtn;
    [SerializeField] private Button tacticalBtn;
    [SerializeField] private Button finalRestrictionBtn;
    
    [Header("Settings")]
    [SerializeField] private Image[] hitImg;
    [SerializeField] [Range(0f, 1f)] private float alphaThreshold = 0.1f;
    
    public const string VIEWER_SCENE_MAIN = "MainLobby";
    public const string VIEWER_CAMPAIGN_MISSION = "Campaign_Mission";
    
    private void Awake()
    {
        // 뒤로가기 버튼 (씬 로드)
        backBtn.onClick.AddListener(() => SceneManager.LoadScene(VIEWER_SCENE_MAIN));
        missionBtn.onClick.AddListener(() => SceneManager.LoadScene(VIEWER_CAMPAIGN_MISSION));

        // 각 콘텐츠 버튼 이벤트 연결 (필요한 로직을 중괄호 안에 작성)
        storyBtn.onClick.AddListener(() => { Debug.Log("Story Logic"); });
        bountyBtn.onClick.AddListener(() => { Debug.Log("Bounty Logic"); });
        commissionsBtn.onClick.AddListener(() => { Debug.Log("Commissions Logic"); });
        scrimmageBtn.onClick.AddListener(() => { Debug.Log("Scrimmage Logic"); });
        totalAssaultBtn.onClick.AddListener(() => { Debug.Log("TotalAssault Logic"); });
        jointFiringDrillBtn.onClick.AddListener(() => { Debug.Log("JointFiringDrill Logic"); });
        grandAssaultBtn.onClick.AddListener(() => { Debug.Log("GrandAssault Logic"); });
        tacticalBtn.onClick.AddListener(() => { Debug.Log("Tactical Logic"); });
        finalRestrictionBtn.onClick.AddListener(() => { Debug.Log("FinalRestriction Logic"); });
    }

    private void Start()
    {
        // 이미지 알파 히트 테스트 설정 (Array.ForEach + Lambda)
        if (hitImg != null)
        {
            Array.ForEach(hitImg, img => 
            {
                if (img != null) img.alphaHitTestMinimumThreshold = alphaThreshold;
            });
        }
    }
}