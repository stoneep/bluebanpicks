using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// 대기실 참가자 목록의 한 행. 클라이언트 ID/표시 이름과 현재 역할(관전자/선공/후공)을 보여주고,
/// 호스트가 드롭다운으로 역할을 바꾸면 OnRoleChangeRequested를 발행한다.
///
/// 이 뷰는 상태를 스스로 들고 있지 않는다 - DraftRoundRowView와 같은 패턴으로,
/// DraftLobbyController가 참가자 목록이 바뀔 때마다 Bind()로 다시 그려주는 "그리기 전용" 컴포넌트다.
/// 실제 반영(NetworkVariable 갱신)은 컨트롤러가 이벤트를 받아 DraftSessionServer.HostSetParticipantRole로 요청한다.
/// </summary>
public class LobbyParticipantRowView : MonoBehaviour
{
    // 드롭다운 인덱스 0/1/2 = 관전자/선공/후공. IndexToRole/RoleToIndex와 순서를 반드시 맞출 것.
    private static readonly List<string> RoleOptions = new() { "관전자", "선공", "후공" };

    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Dropdown roleDropdown;

    /// <summary>(clientId, 새로 선택된 역할) - role이 null이면 관전자.</summary>
    public event Action<ulong, DraftSide?> OnRoleChangeRequested;

    private ulong clientId;
    private bool suppressCallback; // Bind()로 값을 코드에서 맞출 때 onValueChanged가 재귀 호출되는 것을 막기 위함

    private void Awake()
    {
        if (roleDropdown == null) return;

        roleDropdown.ClearOptions();
        roleDropdown.AddOptions(RoleOptions);
        roleDropdown.onValueChanged.AddListener(HandleDropdownChanged);
    }

    /// <summary>참가자 정보로 이 행을 채운다. currentRole이 null이면 관전자.</summary>
    public void Bind(ulong id, string displayName, DraftSide? currentRole)
    {
        clientId = id;
        if (nameText) nameText.text = displayName;
        SetDropdownValueWithoutNotify(RoleToIndex(currentRole));
    }

    /// <summary>호스트가 아니거나 대기실 상태가 아닐 때는 드롭다운을 잠근다.</summary>
    public void SetInteractable(bool interactable)
    {
        if (roleDropdown) roleDropdown.interactable = interactable;
    }

    private void HandleDropdownChanged(int index)
    {
        if (suppressCallback) return;
        OnRoleChangeRequested?.Invoke(clientId, IndexToRole(index));
    }

    private void SetDropdownValueWithoutNotify(int index)
    {
        if (roleDropdown == null) return;

        suppressCallback = true;
        roleDropdown.value = index;
        roleDropdown.RefreshShownValue();
        suppressCallback = false;
    }

    private static int RoleToIndex(DraftSide? role) => role switch
    {
        null => 0,
        DraftSide.First => 1,
        DraftSide.Second => 2,
        _ => 0
    };

    private static DraftSide? IndexToRole(int index) => index switch
    {
        1 => DraftSide.First,
        2 => DraftSide.Second,
        _ => null
    };

    private void OnDestroy()
    {
        if (roleDropdown != null) roleDropdown.onValueChanged.RemoveListener(HandleDropdownChanged);
    }
}
