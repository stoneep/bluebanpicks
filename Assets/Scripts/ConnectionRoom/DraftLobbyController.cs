using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class DraftLobbyController : MonoBehaviour
{
    [Header("Round List")]
    [SerializeField] private Transform roundListContainer;
    [SerializeField] private DraftRoundRowView roundRowPrefab;
    [SerializeField] private Button addRoundButton;
    [SerializeField] private Button flipLastRoundButton;
    [SerializeField] private Button applyLolPresetButton;

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
    [SerializeField] private Button copyRoomCodeButton;
    
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
        if (hostCanPlayToggle != null) hostCanPlayToggle.onValueChanged.AddListener(HandleHostCanPlayToggled);
    }

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

        session.Format.OnListChanged += HandleFormatChanged;
        session.State.OnValueChanged += HandleStateChanged;
        session.FirstSideClientId.OnValueChanged += HandleSideAssignmentChanged;
        session.SecondSideClientId.OnValueChanged += HandleSideAssignmentChanged;
        session.HostCanPlay.OnValueChanged += HandleHostCanPlayChanged;
        session.PreDraftLoadBufferSeconds.OnValueChanged += HandleTimerSettingChanged;
        session.TurnTimeLimitSeconds.OnValueChanged += HandleTimerSettingChanged;
        session.PostDraftDisplaySeconds.OnValueChanged += HandleTimerSettingChanged;

        RebuildRows();
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

        ClearRows();
        session = null;
    }
    

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
            row.BindTimers(preDraftBuffer, turnTimeLimit, postDraftDisplay);
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
    
    
    private void HandleTimerEdited(DraftRoundRowView row)
    {
        var (preDraftBuffer, turnTimeLimit, postDraftDisplay) = row.ReadTimerValues();
        session.HostSetTimerSettings(preDraftBuffer, turnTimeLimit, postDraftDisplay);
    }
    
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
            RefreshHostCanPlayToggle();
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
    

    private void HandleStateChanged(DraftSessionState previous, DraftSessionState current)
    {
        RebuildRows();
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
