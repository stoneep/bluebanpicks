using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 밴픽 결과창의 한 줄. "몇 번째로 / 어느 진영이 / 밴인지 픽인지 / 어떤 캐릭터를" 선택했는지
/// 한 행으로 보여준다. PickedCharacterView와 같은 방식으로 초상화를 로드하되,
/// 순서/진영/밴픽 구분/이름까지 함께 표시해야 해서 별도 뷰로 분리했다.
/// </summary>
public sealed class DraftResultRowView : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private TMP_Text orderText;   // "1", "2", ...
    [SerializeField] private TMP_Text sideText;    // "나"/"상대" 또는 "선공"/"후공"
    [SerializeField] private TMP_Text typeText;    // "BAN" / "PICK"
    [SerializeField] private TMP_Text nameText;    // 캐릭터 표시명
    [SerializeField] private Image portraitImage;
    [SerializeField] private CharacterCut cut = CharacterCut.Slot;

    [Header("Type Colors")]
    [SerializeField] private Color banColor = new(0.85f, 0.3f, 0.3f);
    [SerializeField] private Color pickColor = new(0.3f, 0.55f, 0.9f);

    private readonly CharacterArtProvider artProvider = new();
    private string currentId;
    private int loadToken;

    /// <summary>
    /// 한 행을 채운다. sideLabel은 "나"/"상대"/"선공"/"후공" 중 호출부(DraftResultPanelController)가
    /// LocalSide 유무에 따라 미리 계산해서 넘겨준다 (이 뷰는 세션/네트워크를 몰라도 되게 하기 위함).
    /// </summary>
    public void Bind(int order, DraftSide side, DraftResultType type, string characterId, string sideLabel)
    {
        if (orderText) orderText.text = order.ToString();
        if (sideText) sideText.text = sideLabel;

        if (typeText)
        {
            typeText.text = type == DraftResultType.Ban ? "BAN" : "PICK";
            typeText.color = type == DraftResultType.Ban ? banColor : pickColor;
        }

        if (nameText) nameText.text = CharDatabaseLoader.GetDisplayName(characterId);

        LoadPortrait(characterId);
    }

    private void LoadPortrait(string characterId)
    {
        if (!portraitImage || string.IsNullOrEmpty(characterId)) return;

        currentId = characterId;
        int token = ++loadToken;

        var handle = artProvider.LoadSprite(characterId, cut);

        if (handle.IsDone) Apply(token, characterId, handle.Result);
        else handle.Completed += h => Apply(token, characterId, h.Result);
    }

    private void Apply(int token, string characterId, Sprite sprite)
    {
        // 늦게 온 콜백이 이미 재활용/파괴된 슬롯에 적용되는 것을 방지
        if (token != loadToken || currentId != characterId || !portraitImage) return;

        portraitImage.sprite = sprite;
        portraitImage.enabled = (sprite != null);
        portraitImage.preserveAspect = true;
    }

    private void OnDestroy() => artProvider.ReleaseAll();
}
