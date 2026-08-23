using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 밴픽 슬롯 등에서, 그리드에서 클릭된 캐릭터의 초상화를 표시하는 뷰.
/// CharacterGridViewAdapter.OnCharacterPicked를 구독해서 Show()를 호출하는 방식으로 사용.
/// </summary>
public sealed class PickedCharacterView : MonoBehaviour
{
    [SerializeField] private Image portraitImage;
    [SerializeField] private CharacterCut cut = CharacterCut.Large;

    private readonly CharacterArtProvider artProvider = new();
    private string currentId;
    private int loadToken;

    private void OnDestroy() => artProvider.ReleaseAll();

    public void Show(CharacterViewData data) => Show(data.Id);

    public void Show(string characterId)
    {
        if (!portraitImage || string.IsNullOrEmpty(characterId)) return;

        currentId = characterId;
        int token = ++loadToken;

        var handle = artProvider.LoadSprite(characterId, cut);

        if (handle.IsDone) Apply(token, characterId, handle.Result);
        else handle.Completed += h => Apply(token, characterId, h.Result);
    }

    public void Clear()
    {
        currentId = null;
        loadToken++; // 늦게 오는 콜백 무효화
        if (portraitImage)
        {
            portraitImage.sprite = null;
            portraitImage.enabled = false;
        }
    }

    private void Apply(int token, string characterId, Sprite sprite)
    {
        // 요청 이후 다른 캐릭터가 선택됐거나 오브젝트가 사라졌으면 무시
        if (token != loadToken || currentId != characterId || !portraitImage) return;

        portraitImage.sprite = sprite;
        portraitImage.enabled = (sprite != null);
        portraitImage.preserveAspect = true;
    }
}