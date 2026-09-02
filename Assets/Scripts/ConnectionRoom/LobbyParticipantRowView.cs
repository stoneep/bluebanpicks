using TMPro;
using Unity.Netcode;
using UnityEngine;

public class LobbyParticipantRowView : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;

    [Tooltip("선택 사항. 비워두면 역할 표시를 하지 않는다.")]
    [SerializeField] private TMP_Text roleText;

    [Tooltip("선택 사항. 호스트가 대기실에서 이 참가자의 역할(선공/후공/관전자)을 직접 바꾸는 드롭다운. " +
             "비워두면 사용 안 함. 옵션 순서는 반드시 '선공, 후공, 관전자' 3개(index 0/1/2)여야 한다. " +
             "호스트가 Lobby 상태일 때만 상호작용 가능하도록 자동으로 interactable이 조정된다.")]
    [SerializeField] private TMP_Dropdown roleDropdown;

    [Tooltip("선택 사항. 이 행이 '나 자신'일 때만 활성화할 오브젝트(예: \"(나)\" 라벨). 비워두면 사용 안 함.")]
    [SerializeField] private GameObject localIndicator;

    public ulong ClientId { get; private set; }

    private DraftSessionServer session;

    private void Awake()
    {
        // Bind()가 입/퇴장마다 매번 호출돼도 리스너가 중복 등록되지 않도록 여기서 한 번만 연결
        if (roleDropdown != null)
            roleDropdown.onValueChanged.AddListener(HandleRoleDropdownChanged);
    }

    public void Bind(ulong clientId, string nickname)
    {
        ClientId = clientId;
        if (nameText != null) nameText.text = nickname;

        bool isLocal = NetworkManager.Singleton != null && NetworkManager.Singleton.LocalClientId == clientId;
        if (localIndicator != null) localIndicator.SetActive(isLocal);

        BindSession();
        RefreshRole();
    }

    private void OnDisable() => UnbindSession();

    private void BindSession()
    {
        if (session != null && session == DraftSessionServer.Instance) return;
        UnbindSession();

        session = DraftSessionServer.Instance;
        if (session == null) return;

        session.FirstSideClientId.OnValueChanged += HandleSideAssignmentChanged;
        session.SecondSideClientId.OnValueChanged += HandleSideAssignmentChanged;
        session.State.OnValueChanged += HandleStateChanged; // Lobby 종료 시 드롭다운 잠그기 위해 추가
    }

    private void UnbindSession()
    {
        if (session == null) return;
        session.FirstSideClientId.OnValueChanged -= HandleSideAssignmentChanged;
        session.SecondSideClientId.OnValueChanged -= HandleSideAssignmentChanged;
        session.State.OnValueChanged -= HandleStateChanged;
        session = null;
    }

    private void HandleSideAssignmentChanged(ulong previous, ulong current) => RefreshRole();
    private void HandleStateChanged(DraftSessionState previous, DraftSessionState current) => RefreshRole();

    private void RefreshRole()
    {
        DraftSide? role = null; // null = 관전자
        if (session != null)
        {
            if (ClientId == session.FirstSideClientId.Value) role = DraftSide.First;
            else if (ClientId == session.SecondSideClientId.Value) role = DraftSide.Second;
        }

        if (roleText != null)
            roleText.text = session == null ? string.Empty : RoleLabel(role);

        if (roleDropdown != null)
        {
            // SetValueWithoutNotify로 onValueChanged 재발화(=서버로 되쏘는 무한 루프) 방지
            roleDropdown.SetValueWithoutNotify(RoleToDropdownIndex(role));
            roleDropdown.interactable = IsHostEditable();
        }
    }

    private void HandleRoleDropdownChanged(int index)
    {
        if (session == null || !IsHostEditable())
        {
            RefreshRole(); // 잘못 눌린 경우 서버 값으로 즉시 원복
            return;
        }

        session.HostSetParticipantRole(ClientId, DropdownIndexToRole(index));
    }

    private bool IsHostEditable() =>
        NetworkManager.Singleton != null &&
        NetworkManager.Singleton.IsServer &&
        session != null &&
        session.State.Value == DraftSessionState.Lobby;

    // 드롭다운 옵션 순서: 0=선공, 1=후공, 2=관전자
    private static int RoleToDropdownIndex(DraftSide? role) => role switch
    {
        DraftSide.First => 0,
        DraftSide.Second => 1,
        _ => 2,
    };

    private static DraftSide? DropdownIndexToRole(int index) => index switch
    {
        0 => DraftSide.First,
        1 => DraftSide.Second,
        _ => null,
    };

    private static string RoleLabel(DraftSide? role) => role switch
    {
        DraftSide.First => "선공",
        DraftSide.Second => "후공",
        _ => "관전자",
    };
}