using TMPro;
using Unity.Netcode;
using UnityEngine;












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
            label = "대기중"; 

        roleText.text = label;
    }
}
