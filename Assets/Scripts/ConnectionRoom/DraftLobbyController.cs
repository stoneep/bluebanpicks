using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 대기실 화면: 라운드 목록 편집(호스트 전용) + 진영 배정(호스트 전용) + 드래프트 시작.
/// DraftSessionServer의 Format/State/FirstSideClientId/SecondSideClientId를 그대로 구독해서 그린다.
///
/// 진영 배정은 테스트 편의를 위해 "자동 배정"(랜덤으로 변경 선공/후공) 버튼 하나로 단순화했다.
/// 실제 매치메이킹/초대 시스템이 붙으면 이 부분만 교체하면 됨.
///
/// 편집 흐름: 라운드 행(DraftRoundRowView)이 편집되면 지금 보이는 모든 행의 값을 다시 모아
/// DraftFormatData를 새로 만들고 HostSetFormat으로 통째로 반영한다. HostSetFormat은
/// RPC가 아니라 서버(호스트)에서 로컬로 직접 호출하는 일반 메서드이므로 - 이 컨트롤러 자체가
/// 호스트에서만 편집 가능 상태(SetInteractable)이기 때문에 게스트가 잘못 호출할 일은 없다.
/// </summary>
public class DraftLobbyController : MonoBehaviour
{
    [Header("Round List")]
    [SerializeField] private Transform roundListContainer;
    [SerializeField] private DraftRoundRowView roundRowPrefab;
    [SerializeField] private Button addRoundButton;
    [SerializeField] private Button flipLastRoundButton; // 마지막 라운드를 복제 + 이니셔티브 반전해서 추가
    [SerializeField] private Button applyLolPresetButton; // 전반 ABABAB/ABBAAB, 후반 BABA/BAABBA 한 번에 적용 (테스트용)

    [Header("Session Controls (호스트 전용)")]
    [SerializeField] private Button autoAssignSidesButton;
    [SerializeField] private Button startDraftButton;

    [Header("Status")]
    [SerializeField] private TMP_Text statusText;

    [Header("Room Code")]
    [SerializeField] private TMP_Text roomCodeText;
    [SerializeField] private Button copyRoomCodeButton; // 있으면 클립보드 복사
    
    private DraftSessionServer session;
    private readonly List<DraftRoundRowView> rows = new();

    private void Awake()
    {
        addRoundButton.onClick.AddListener(HandleAddRound);
        flipLastRoundButton.onClick.AddListener(HandleFlipLastRound);
        applyLolPresetButton.onClick.AddListener(HandleApplyLolPreset);
        autoAssignSidesButton.onClick.AddListener(HandleAutoAssignSides);
        startDraftButton.onClick.AddListener(HandleStartDraft);
        copyRoomCodeButton.onClick.AddListener(HandleCopyRoomCode);
    }

    private void OnEnable()
    {
        if (DraftSessionServer.Instance != null)
            Bind(DraftSessionServer.Instance);
        else
            DraftSessionServer.OnSessionReady += Bind; // 아직 호스트가 안 떴으면, 뜨는 순간 자동 바인딩
    }

    private void OnDisable()
    {
        DraftSessionServer.OnSessionReady -= Bind;
        Unbind();
    }

    // ==================== 바인딩 ====================

    private void Bind(DraftSessionServer newSession)
    {
        if (session != null) Unbind();
        session = newSession;

        session.Format.OnListChanged += HandleFormatChanged;
        session.State.OnValueChanged += HandleStateChanged;
        session.FirstSideClientId.OnValueChanged += HandleSideAssignmentChanged;
        session.SecondSideClientId.OnValueChanged += HandleSideAssignmentChanged;

        RebuildRows();
        RefreshInteractable();
        RefreshStatus();
        RefreshRoomCode();
    }

    private void Unbind()
    {
        if (session == null) return;

        session.Format.OnListChanged -= HandleFormatChanged;
        session.State.OnValueChanged -= HandleStateChanged;
        session.FirstSideClientId.OnValueChanged -= HandleSideAssignmentChanged;
        session.SecondSideClientId.OnValueChanged -= HandleSideAssignmentChanged;

        ClearRows();
        session = null;
    }

    // ==================== 라운드 목록 ====================

    private void HandleFormatChanged(NetworkListEvent<NetworkDraftRoundConfig> _) => RebuildRows();

    private void RebuildRows()
    {
        ClearRows();

        bool editable = IsHostInLobby();

        foreach (var netRound in session.Format)
        {
            var row = Instantiate(roundRowPrefab, roundListContainer);
            row.Bind(netRound.ToRoundConfig());
            row.SetInteractable(editable);

            if (editable)
            {
                row.OnEdited += ApplyRowsToServer;
                row.OnRemoveRequested += () => HandleRemoveRow(row);
            }

            rows.Add(row);
        }
    }

    private void ClearRows()
    {
        foreach (var row in rows)
        {
            if (!row) continue;
            row.OnEdited -= ApplyRowsToServer;
            Destroy(row.gameObject);
        }
        rows.Clear();
    }

    private void HandleAddRound()
    {
        var data = CollectCurrentRows();
        data.AddRound(new DraftRoundConfig(3, 3, 3, 3, DraftSide.First, $"라운드 {rows.Count + 1}"));
        session.HostSetFormat(data);
    }

    /// <summary>마지막 라운드를 복제하되 시작 진영만 반전해서 새 라운드로 추가.
    /// "전반은 선공부터, 후반은 후공부터" 같은 규칙을 버튼 한 번으로 만들 때 사용.</summary>
    private void HandleFlipLastRound()
    {
        if (rows.Count == 0)
        {
            SetStatus("먼저 라운드를 1개 이상 추가하세요.");
            return;
        }

        var last = rows[^1].ReadValue();
        var data = CollectCurrentRows();
        data.AddRound(last.WithFlippedInitiative($"{last.RoundName} (반전)"));
        session.HostSetFormat(data);
    }

    private void HandleRemoveRow(DraftRoundRowView row)
    {
        var data = new DraftFormatData();
        foreach (var r in rows)
        {
            if (r == row) continue;
            data.AddRound(r.ReadValue());
        }
        session.HostSetFormat(data);
    }

    /// <summary>
    /// 논의했던 정확한 규칙(전반 밴 ABABAB / 전반 픽 ABBAAB / 후반 밴 BABA / 후반 픽 BAABBA)을
    /// 기존 라운드를 전부 지우고 한 번에 적용한다. 빠른 테스트/데모용.
    /// </summary>
    private void HandleApplyLolPreset()
    {
        var data = new DraftFormatData();

        data.AddRound(new DraftRoundConfig(
            firstBanSlots: 3, secondBanSlots: 3,
            firstPickSlots: 3, secondPickSlots: 3,
            startingSide: DraftSide.First,
            roundName: "전반",
            banOrderPattern: "ABABAB",
            pickOrderPattern: "ABBAAB"));

        data.AddRound(new DraftRoundConfig(
            firstBanSlots: 2, secondBanSlots: 2,
            firstPickSlots: 3, secondPickSlots: 3,
            startingSide: DraftSide.Second,
            roundName: "후반",
            banOrderPattern: "BABA",
            pickOrderPattern: "BAABBA"));

        session.HostSetFormat(data);
    }

    private DraftFormatData CollectCurrentRows()
    {
        var data = new DraftFormatData();
        foreach (var row in rows) data.AddRound(row.ReadValue());
        return data;
    }

    private void ApplyRowsToServer() => session.HostSetFormat(CollectCurrentRows());

    // ==================== 진영 배정 / 시작 ====================

    /// <summary>
    /// 역할 규칙: 호스트는 항상 관전자다. 드래프트에 실제로 참가하는(선공/후공)
    /// 클라이언트는 호스트를 제외한 나머지 접속자들 중에서만 뽑는다.
    /// (서버 쪽 DraftSessionServer.HostAssignSides에도 같은 규칙이 최종 방어선으로 들어가 있음)
    /// </summary>
    private void HandleAutoAssignSides()
    {
        var players = new List<ulong>();
        foreach (var id in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (id == NetworkManager.ServerClientId) continue;
            players.Add(id);
        }

        if (players.Count < 2)
        {
            SetStatus($"진영을 배정하려면 호스트를 제외한 참가자가 2명 이상 접속해야 합니다. " +
                      $"(현재 참가자 {players.Count}명, 호스트는 관전자)");
            return;
        }

        // 50% 확률로 두 참가자의 선공/후공을 뒤바꾼다.
        // 호스트 로컬에서만 실행되는 코드이므로(버튼 interactable 조건 + HostAssignSides의 IsServer 방어)
        // 클라이언트가 결과에 개입할 여지가 없다.
        if (Random.value < 0.5f)
        {
            (players[0], players[1]) = (players[1], players[0]);
        }

        session.HostAssignSides(players[0], players[1]);
    }

    private void HandleStartDraft() => session.HostStartDraft();

    // ==================== 상태 표시 ====================

    private void HandleStateChanged(DraftSessionState previous, DraftSessionState current)
    {
        RebuildRows(); // 상태가 바뀌면 편집 가능 여부(SetInteractable)도 같이 바뀌어야 하므로 재구성
        RefreshInteractable();
        RefreshStatus();
    }

    private void HandleSideAssignmentChanged(ulong previous, ulong current) => RefreshStatus();

    private bool IsHostInLobby() =>
        NetworkManager.Singleton != null &&
        NetworkManager.Singleton.IsServer &&
        session != null &&
        session.State.Value == DraftSessionState.Lobby;

    private void RefreshInteractable()
    {
        bool editable = IsHostInLobby();

        addRoundButton.interactable = editable;
        flipLastRoundButton.interactable = editable;
        applyLolPresetButton.interactable = editable;
        autoAssignSidesButton.interactable = editable;
        startDraftButton.interactable = editable;
    }

    private void RefreshStatus()
    {
        if (session == null) { SetStatus("세션 없음"); return; }

        string state = session.State.Value switch
        {
            DraftSessionState.Lobby => "대기실",
            DraftSessionState.InProgress => "드래프트 진행 중",
            DraftSessionState.Completed => "종료",
            _ => "?"
        };

        string sides = session.FirstSideClientId.Value == ulong.MaxValue
            ? "진영 미배정"
            : $"선공=클라{session.FirstSideClientId.Value} / 후공=클라{session.SecondSideClientId.Value}";

        string hostRole = $"호스트(클라{NetworkManager.ServerClientId})=관전";

        SetStatus($"[{state}] {hostRole} / {sides}");
    }
    
    private void RefreshRoomCode()
    {
        if (roomCodeText == null) return;

        var relay = NetworkManager.Singleton != null
            ? NetworkManager.Singleton.GetComponent<RelayRoomService>()
            : null;

        string code = relay != null ? relay.CurrentJoinCode : null;
        roomCodeText.text = string.IsNullOrEmpty(code) ? string.Empty : $"방 코드: {code}";

        if (copyRoomCodeButton != null)
            copyRoomCodeButton.gameObject.SetActive(!string.IsNullOrEmpty(code));
    }

    private void HandleCopyRoomCode()
    {
        var relay = NetworkManager.Singleton?.GetComponent<RelayRoomService>();
        if (relay != null && !string.IsNullOrEmpty(relay.CurrentJoinCode))
            GUIUtility.systemCopyBuffer = relay.CurrentJoinCode;
    }
    
    private void SetStatus(string message)
    {
        if (statusText) statusText.text = message;
    }
}
