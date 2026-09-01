using TMPro;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 대기실 참가자 목록의 행(row) 하나.
///
/// DraftSessionServer.Nicknames의 항목 하나(clientId + 닉네임)를 nameText에 표시한다.
/// 역할(관전자/선공/후공)은 별도 리스트가 아니라 DraftSessionServer의 두 NetworkVariable
/// (FirstSideClientId/SecondSideClientId)에서 그때그때 파생시킨다 - RoleIndicator와 동일한
/// 설계 원칙. 그래서 이 컴포넌트는 Bind() 이후에도 스스로 그 두 변수를 구독해서 역할 표시만
/// 갱신한다. 닉네임 자체가 바뀌는 경우(참가자 입/퇴장)는 DraftLobbyController가 행을 통째로
/// 다시 만들어 처리하므로 이 클래스가 신경 쓸 필요 없다.
///
/// roleText / localIndicator는 선택 사항이다. 인스펙터에 비워두면 해당 부분만 조용히
/// 생략된다(다른 뷰들과 동일하게 "참조 미할당 시 스킵" 원칙을 따름) - nameText 하나만
/// 연결해도 최소 동작은 된다.
/// </summary>
public class LobbyParticipantRowView : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;

    [Tooltip("선택 사항. 비워두면 역할 표시를 하지 않는다.")]
    [SerializeField] private TMP_Text roleText;

    [Tooltip("선택 사항. 이 행이 '나 자신'일 때만 활성화할 오브젝트(예: \"(나)\" 라벨). 비워두면 사용 안 함.")]
    [SerializeField] private GameObject localIndicator;

    /// <summary>이 행이 표시 중인 참가자의 clientId.</summary>
    public ulong ClientId { get; private set; }

    private DraftSessionServer session;

    /// <summary>
    /// 서버에서 동기화된 닉네임으로 행을 채운다.
    /// DraftLobbyController.RebuildParticipantRows()가 Nicknames를 순회하며 매번 호출한다.
    /// </summary>
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

    /// <summary>
    /// 역할은 닉네임과 별개로 언제든(진영 배정/재배정) 바뀔 수 있으므로, 행이 살아있는 동안
    /// 세션의 두 NetworkVariable을 직접 구독해서 스스로 갱신한다.
    /// </summary>
    private void BindSession()
    {
        if (session != null && session == DraftSessionServer.Instance) return; // 이미 같은 세션 구독 중

        UnbindSession();

        session = DraftSessionServer.Instance;
        if (session == null) return;

        session.FirstSideClientId.OnValueChanged += HandleSideAssignmentChanged;
        session.SecondSideClientId.OnValueChanged += HandleSideAssignmentChanged;
    }

    private void UnbindSession()
    {
        if (session == null) return;

        session.FirstSideClientId.OnValueChanged -= HandleSideAssignmentChanged;
        session.SecondSideClientId.OnValueChanged -= HandleSideAssignmentChanged;
        session = null;
    }

    private void HandleSideAssignmentChanged(ulong previous, ulong current) => RefreshRole();

    private void RefreshRole()
    {
        if (roleText == null) return;

        if (session == null)
        {
            roleText.text = string.Empty;
            return;
        }

        string label;
        if (ClientId == session.FirstSideClientId.Value) label = "선공";
        else if (ClientId == session.SecondSideClientId.Value) label = "후공";
        else label = "관전자";

        roleText.text = label;
    }
}