using TMPro;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 로컬 클라이언트가 "관전자"인지 "선공"인지 "후공"인지를 표시하는 얇은 뷰.
///
/// DraftSessionServer.FirstSideClientId / SecondSideClientId(NetworkVariable)를 구독해서
/// 내 NetworkManager.LocalClientId와 비교하는 방식이라, 대기실(Lobby) 화면과 드래프트
/// 진행(MainLobby) 화면 어디에 붙여놔도 동일하게 동작한다. 진영 배정이 대기실 단계에서
/// 바뀔 수 있으므로(자동 배정 버튼) OnValueChanged 구독으로 실시간 반영한다.
///
/// DraftLobbyController와 마찬가지로 DraftSessionServer.OnSessionReady를 통해
/// "세션이 아직 스폰 전이어도 늦게 자동 바인딩"되는 패턴을 따른다.
/// </summary>
public sealed class RoleIndicator : MonoBehaviour
{
    [SerializeField] private TMP_Text roleText;

    private DraftSessionServer session;

    private void OnEnable()
    {
        if (DraftSessionServer.Instance != null)
            Bind(DraftSessionServer.Instance);
        else
            DraftSessionServer.OnSessionReady += Bind;
    }

    private void OnDisable()
    {
        DraftSessionServer.OnSessionReady -= Bind;
        Unbind();
    }

    private void Bind(DraftSessionServer newSession)
    {
        if (session != null) Unbind();
        session = newSession;

        session.FirstSideClientId.OnValueChanged += HandleSideAssignmentChanged;
        session.SecondSideClientId.OnValueChanged += HandleSideAssignmentChanged;

        Refresh();
    }

    private void Unbind()
    {
        if (session == null) return;

        session.FirstSideClientId.OnValueChanged -= HandleSideAssignmentChanged;
        session.SecondSideClientId.OnValueChanged -= HandleSideAssignmentChanged;

        session = null;
    }

    private void HandleSideAssignmentChanged(ulong previous, ulong current) => Refresh();

    private void Refresh()
    {
        if (!roleText || session == null) return;

        if (NetworkManager.Singleton == null)
        {
            roleText.text = string.Empty;
            return;
        }

        ulong myClientId = NetworkManager.Singleton.LocalClientId;

        string label;
        if (myClientId == session.FirstSideClientId.Value)
            label = "선공";
        else if (myClientId == session.SecondSideClientId.Value)
            label = "후공";
        else
            label = "관전"; // 호스트를 포함해, 선공/후공 어디에도 배정되지 않은 클라이언트는 전부 관전자

        roleText.text = label;
    }
}
