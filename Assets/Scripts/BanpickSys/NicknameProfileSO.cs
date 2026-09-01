using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 로컬 플레이어의 닉네임을 씬 전환과 무관하게 들고 다니기 위한 ScriptableObject.
///
/// 왜 static 싱글턴이 아니라 SO인가:
///  - Title(접속) 씬의 NetworkConnectionUI와, 이후 대기실/드래프트 씬의 다른 스크립트가
///    인스펙터에서 "같은 에셋"을 참조하기만 하면 별도의 DontDestroyOnLoad 오브젝트 없이도
///    동일한 값을 공유해서 읽을 수 있다. 이 프로젝트의 다른 Config성 SO
///    (DraftFormatSO, CharacterDefinitionSO 등)와 동일한 패턴.
///
/// 영속성(재실행/재접속 시에도 이전 닉네임 재사용):
///  - SO 자산 자체는 빌드에서 런타임에 값을 바꿔도 디스크에 다시 저장되지 않으므로,
///    실제 저장은 PlayerPrefs가 담당한다. SO는 "현재 세션에서 쓰는 값을 여러 스크립트가
///    공유해서 보는 창구" 역할만 한다.
///
/// 자유 입력 → 고정 목록 선택으로 변경:
///  - 이전에는 TMP_InputField에 아무 문자열이나 입력받아 Save(string)로 그대로 저장했지만,
///    지금은 presetNicknames에 미리 정해둔 목록 중 하나만 고를 수 있다(드롭다운 UI 전제).
///    그래서 저장/조회도 "문자열"이 아니라 "목록 안에서의 인덱스" 기준으로 동작한다
///    (SaveIndex/LoadIndex). PlayerPrefs에는 여전히 문자열(닉네임 자체)을 저장하는데,
///    이는 프로젝트에서 나중에 presetNicknames 순서를 바꿔도 이전에 골랐던 "이름"을
///    최대한 그대로 복원하기 위함이다 - 순서가 바뀌어도 같은 이름이 목록에 남아있으면
///    그 이름을 다시 찾아 선택해준다. 목록에서 아예 사라진 경우에만 첫 번째 항목으로 대체한다.
/// </summary>
[CreateAssetMenu(fileName = "NicknameProfile", menuName = "Draft/Nickname Profile")]
public class NicknameProfileSO : ScriptableObject
{
    private const string PlayerPrefsKey = "Draft.PlayerNickname";

    [SerializeField, Tooltip("드롭다운에 표시할 고정 닉네임 목록. 자유 입력 없이 이 중에서만 고를 수 있다. 최소 1개 이상 채워둘 것.")]
    private string[] presetNicknames = { "Player" };

    /// <summary>드롭다운을 채울 때 사용. 이 목록 순서 그대로 옵션을 만들면 인덱스가 그대로 대응된다.</summary>
    public IReadOnlyList<string> PresetNicknames => presetNicknames;

    /// <summary>현재 세션에서 사용 중인 닉네임. Load()/SaveIndex()를 통해서만 채워진다.</summary>
    public string Current { get; private set; }

    private bool loaded;

    /// <summary>
    /// PlayerPrefs에서 이전에 저장된 닉네임을 불러온다. 목록에 없는 값이면(자산 개편 등)
    /// 첫 번째 프리셋으로 대체한다. 여러 번 호출해도 안전하다(이미 로드했으면 캐시된 값 반환).
    /// </summary>
    public string Load()
    {
        if (!loaded)
        {
            string saved = PlayerPrefs.GetString(PlayerPrefsKey, string.Empty);
            Current = IsKnownPreset(saved) ? saved : FirstPresetOrEmpty();
            loaded = true;
        }
        return Current;
    }

    /// <summary>
    /// Load()와 같은 값을, 드롭다운에 바로 넣을 수 있는 인덱스로 반환한다.
    /// (예: nicknameDropdown.SetValueWithoutNotify(profile.LoadIndex())).
    /// 목록에서 찾을 수 없으면 0을 반환한다.
    /// </summary>
    public int LoadIndex()
    {
        string nickname = Load();
        if (presetNicknames == null || presetNicknames.Length == 0) return 0;

        int index = Array.IndexOf(presetNicknames, nickname);
        return index >= 0 ? index : 0;
    }

    /// <summary>
    /// 드롭다운에서 고른 인덱스를 Current에 반영하고 PlayerPrefs에 저장한다
    /// (다음에 접속 화면을 다시 열었을 때 LoadIndex()로 같은 항목이 다시 선택되도록).
    /// 범위를 벗어난 인덱스는 안전하게 clamp된다. 최종 선택된 닉네임 문자열을 반환한다.
    /// </summary>
    public string SaveIndex(int index)
    {
        if (presetNicknames == null || presetNicknames.Length == 0)
        {
            Current = string.Empty;
            loaded = true;
            return Current;
        }

        int clamped = Mathf.Clamp(index, 0, presetNicknames.Length - 1);
        string nickname = presetNicknames[clamped];

        Current = nickname;
        loaded = true;
        PlayerPrefs.SetString(PlayerPrefsKey, nickname);
        PlayerPrefs.Save();
        return nickname;
    }

    private bool IsKnownPreset(string nickname) =>
        !string.IsNullOrEmpty(nickname) && presetNicknames != null && Array.IndexOf(presetNicknames, nickname) >= 0;

    private string FirstPresetOrEmpty() =>
        (presetNicknames != null && presetNicknames.Length > 0) ? presetNicknames[0] : string.Empty;
}
