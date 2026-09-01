using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 대기실 화면: 라운드 목록 편집(호스트 전용) + 진영 배정(호스트 전용) + 드래프트 시작 + 참가자 목록 표시.
/// DraftSessionServer의 Format/State/FirstSideClientId/SecondSideClientId/Nicknames를 그대로 구독해서 그린다.
///
/// 진영 배정은 테스트 편의를 위해 "자동 배정"(랜덤으로 변경 선공/후공) 버튼 하나로 단순화했다.
/// 실제 매치메이킹/초대 시스템이 붙으면 이 부분만 교체하면 됨.
///
/// 편집 흐름: 라운드 행(DraftRoundRowView)이 편집되면 지금 보이는 모든 행의 값을 다시 모아
/// DraftFormatData를 새로 만들고 HostSetFormat으로 통째로 반영한다. HostSetFormat은
/// RPC가 아니라 서버(호스트)에서 로컬로 직접 호출하는 일반 메서드이므로 - 이 컨트롤러 자체가
/// 호스트에서만 편집 가능 상태(SetInteractable)이기 때문에 게스트가 잘못 호출할 일은 없다.
///
/// 참가자 목록: DraftSessionServer.Nicknames(NetworkList)가 바뀔 때마다(접속/해제) 전체를
/// 다시 그린다. 개별 참가자의 역할(관전자/선공/후공) 표시는 LobbyParticipantRowView가
/// FirstSideClientId/SecondSideClientId를 직접 구독해서 스스로 갱신하므로, 진영 배정이
/// 바뀔 때마다 이 목록 전체를 다시 그릴 필요는 없다.
/// </summary>
public class DraftLobbyController : MonoBehaviour
{
    [Header("Round List")]
    [SerializeField] private Transform roundListContainer;
    [SerializeField] private DraftRoundRowView roundRowPrefab;
    [SerializeField] private Button addRoundButton;
    [SerializeField] private Button flipLastRoundButton; // 마지막 라운드를 복제 + 이니셔티브 반전해서 추가
    [SerializeField] private Button applyLolPresetButton; // 전반 ABABAB/ABBAAB, 후반 BABA/BAABBA 한 번에 적용 (테스트용)

    [Header("참가자 목록")]
    [Tooltip("비워두면 참가자 목록을 그리지 않는다(다른 UI 요소들과 동일하게 미할당 시 스킵).")]
    [SerializeField] private Transform participantListContainer;
    [SerializeField] private LobbyParticipantRowView participantRowPrefab;

    [Header("Session Controls (호스트 전용)")]
    [SerializeField] private Button autoAssignSidesButton;
    [SerializeField] private Button startDraftButton;

    [Header("2인 연습 모드 (호스트 전용)")]
    [Tooltip("켜면 호스트 자신도 자동 배정 후보에 포함되어 선공/후공 중 하나로 뽑힐 수 있다. " +
             "관전자 역할의 3번째 인원 없이 2명이서 바로 연습할 때 사용.")]
    [SerializeField] private Toggle hostCanPlayToggle;

    [Header("Status")]
    [SerializeField] private TMP_Text statusText;

    [Header("Room Code")]
    [SerializeField] private TMP_Text roomCodeText;
    [SerializeField] private Button copyRoomCodeButton; // 있으면 클립보드 복사

    private DraftSessionServer session;
    private readonly List<DraftRoundRowView> rows = new();
    private readonly List<LobbyParticipantRowView> participantRows = new();

    private void Awake()
    {
        addRoundButton.onClick.AddListener(HandleAddRound);
        flipLastRoundButton.onClick.AddListener(HandleFlipLastRound);
        applyLolPresetButton.onClick.AddListener(HandleApplyLolPreset);
        autoAssignSidesButton.onClick.AddListener(HandleAutoAssignSides);
        startDraftButton.onClick.AddListener(HandleStartDraft);
        copyRoomCodeButton.onClick.AddListener(HandleCopyRoomCode);
        if (hostCanPlayToggle != null) hostCanPlayToggle.onValueChanged.AddListener(HandleHostCanPlayToggled);
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
        session.HostCanPlay.OnValueChanged += HandleHostCanPlayChanged;
        session.PreDraftLoadBufferSeconds.OnValueChanged += HandleTimerSettingChanged;
        session.TurnTimeLimitSeconds.OnValueChanged += HandleTimerSettingChanged;
        session.PostDraftDisplaySeconds.OnValueChanged += HandleTimerSettingChanged;
        session.Nicknames.OnListChanged += HandleNicknamesChanged;

        RebuildRows();
        RebuildParticipantRows();
        RefreshInteractable();
        RefreshStatus();
        RefreshRoomCode();
        RefreshHostCanPlayToggle();
    }

    private void Unbind()
    {
        if (session == null) return;

        session.Format.OnListChanged -= HandleFormatChanged;
        session.State.OnValueChanged -= HandleStateChanged;
        session.FirstSideClientId.OnValueChanged -= HandleSideAssignmentChanged;
        session.SecondSideClientId.OnValueChanged -= HandleSideAssignmentChanged;
        session.HostCanPlay.OnValueChanged -= HandleHostCanPlayChanged;
        session.PreDraftLoadBufferSeconds.OnValueChanged -= HandleTimerSettingChanged;
        session.TurnTimeLimitSeconds.OnValueChanged -= HandleTimerSettingChanged;
        session.PostDraftDisplaySeconds.OnValueChanged -= HandleTimerSettingChanged;
        session.Nicknames.OnListChanged -= HandleNicknamesChanged;

        ClearRows();
        ClearParticipantRows();
        session = null;
    }

    // ==================== 라운드 목록 ====================

    private void HandleFormatChanged(NetworkListEvent<NetworkDraftRoundConfig> _) => RebuildRows();

    private void RebuildRows()
    {
        ClearRows();

        bool editable = IsHostInLobby();
        float preDraftBuffer = session.PreDraftLoadBufferSeconds.Value;
        float turnTimeLimit = session.TurnTimeLimitSeconds.Value;
        float postDraftDisplay = session.PostDraftDisplaySeconds.Value;

        foreach (var netRound in session.Format)
        {
            var row = Instantiate(roundRowPrefab, roundListContainer);
            row.Bind(netRound.ToRoundConfig());
            row.BindTimers(preDraftBuffer, turnTimeLimit, postDraftDisplay); // 세션 공통값이라 모든 행에 동일하게 채움
            row.SetInteractable(editable);

            if (editable)
            {
                row.OnEdited += ApplyRowsToServer;
                row.OnRemoveRequested += () => HandleRemoveRow(row);
                row.OnTimerEdited += () => HandleTimerEdited(row);
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

    // ==================== 참가자 목록 ====================

    private void HandleNicknamesChanged(NetworkListEvent<ClientNicknameEntry> _) => RebuildParticipantRows();

    private void RebuildParticipantRows()
    {
        ClearParticipantRows();
        if (participantListContainer == null || participantRowPrefab == null) return; // 인스펙터 미할당 시 스킵

        foreach (var entry in session.Nicknames)
        {
            var row = Instantiate(participantRowPrefab, participantListContainer);
            row.Bind(entry.ClientId, entry.Nickname.ToString());
            participantRows.Add(row);
        }
    }

    private void ClearParticipantRows()
    {
        foreach (var row in participantRows)
        {
            if (row) Destroy(row.gameObject);
        }
        participantRows.Clear();
    }

    // ==================== 타이머 (세션 공통값) ====================

    /// <summary>어느 행에서 타이머 값을 고쳤든, 그 행의 현재 입력값을 세션 공통값으로 반영한다.</summary>
    private void HandleTimerEdited(DraftRoundRowView row)
    {
        var (preDraftBuffer, turnTimeLimit, postDraftDisplay) = row.ReadTimerValues();
        session.HostSetTimerSettings(preDraftBuffer, turnTimeLimit, postDraftDisplay);
    }

    /// <summary>서버 값이 바뀌면(내가 방금 고친 것 포함) 모든 행의 표시값을 다시 맞춘다.</summary>
    private void HandleTimerSettingChanged(float previous, float current) => RefreshTimerFields();

    private void RefreshTimerFields()
    {
        if (session == null) return;

        float preDraftBuffer = session.PreDraftLoadBufferSeconds.Value;
        float turnTimeLimit = session.TurnTimeLimitSeconds.Value;
        float postDraftDisplay = session.PostDraftDisplaySeconds.Value;

        foreach (var row in rows)
        {
            if (!row) continue;
            row.BindTimers(preDraftBuffer, turnTimeLimit, postDraftDisplay);
        }
    }

    // ==================== 진영 배정 / 시작 ====================

    /// <summary>
    /// 역할 규칙: 기본적으로 호스트는 관전자이고, 드래프트에 실제로 참가하는(선공/후공)
    /// 클라이언트는 호스트를 제외한 나머지 접속자들 중에서만 뽑는다.
    /// 단, "2인 연습 모드"(HostCanPlay)가 켜져 있으면 호스트도 후보에 포함시킨다 -
    /// 관전자 역할의 3번째 인원 없이, 대결할 두 사람 중 한 명이 방을 만들고
    /// 자기 자신을 선공/후공 중 하나로 배정해 바로 시작할 수 있게 하기 위함.
    /// (서버 쪽 DraftSessionServer.HostAssignSides에도 같은 규칙이 최종 방어선으로 들어가 있음)
    /// </summary>
    private void HandleAutoAssignSides()
    {
        bool hostCanPlay = session.HostCanPlay.Value;

        var players = new List<ulong>();
        foreach (var id in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (id == NetworkManager.ServerClientId && !hostCanPlay) continue;
            players.Add(id);
        }

        if (players.Count < 2)
        {
            string hint = hostCanPlay
                ? $"(현재 접속자 {players.Count}명, 2인 연습 모드 켜짐)"
                : $"(현재 참가자 {players.Count}명, 호스트는 관전자)";
            SetStatus($"진영을 배정하려면 참가 가능한 인원이 2명 이상이어야 합니다. {hint}");
            return;
        }

        // 접속자가 3명 이상인데 2인 연습 모드가 켜져 있는 경우, 호스트를 포함해 무작위로
        // 두 명을 뽑는다(랜덤 셔플 후 앞 2명 사용). 접속자가 정확히 2명이면 항상 그 둘이다.
        for (int i = players.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (players[i], players[j]) = (players[j], players[i]);
        }

        session.HostAssignSides(players[0], players[1]);
    }

    private void HandleStartDraft() => session.HostStartDraft();

    private void HandleHostCanPlayToggled(bool isOn)
    {
        if (!IsHostInLobby())
        {
            RefreshHostCanPlayToggle(); // 게스트가 실수로 못 건드리게 즉시 원복 표시
            return;
        }

        session.HostSetHostCanPlay(isOn);
    }

    private void HandleHostCanPlayChanged(bool previous, bool current)
    {
        RefreshHostCanPlayToggle();
        RefreshStatus();
    }

    private void RefreshHostCanPlayToggle()
    {
        if (hostCanPlayToggle == null) return;

        bool value = session != null && session.HostCanPlay.Value;
        hostCanPlayToggle.SetIsOnWithoutNotify(value);
        hostCanPlayToggle.interactable = IsHostInLobby();
    }

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

        if (hostCanPlayToggle != null) hostCanPlayToggle.interactable = editable;
    }

    private void RefreshStatus()
    {
        if (session == null) { SetStatus("세션 없음"); return; }

        string state = session.State.Value switch
        {
            DraftSessionState.Lobby => "대기실",
            DraftSessionState.Loading => "잠시 후 시작",
            DraftSessionState.InProgress => "드래프트 진행 중",
            DraftSessionState.Completed => "종료",
            _ => "?"
        };

        string sides = session.FirstSideClientId.Value == ulong.MaxValue
            ? "진영 미배정"
            : $"선공=클라{session.FirstSideClientId.Value} / 후공=클라{session.SecondSideClientId.Value}";

        ulong hostId = NetworkManager.ServerClientId;
        bool hostIsPlaying = hostId == session.FirstSideClientId.Value || hostId == session.SecondSideClientId.Value;
        string hostRole = hostIsPlaying
            ? $"호스트(클라{hostId})=참가자 (2인 연습 모드)"
            : session.HostCanPlay.Value
                ? $"호스트(클라{hostId})=관전 (2인 연습 모드 켜짐, 아직 미배정)"
                : $"호스트(클라{hostId})=관전";

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
