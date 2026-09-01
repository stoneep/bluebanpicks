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
/// </summary>
[CreateAssetMenu(fileName = "NicknameProfile", menuName = "Draft/Nickname Profile")]
public class NicknameProfileSO : ScriptableObject
{
    private const string PlayerPrefsKey = "Draft.PlayerNickname";
    private const int MaxLength = 16;

    [SerializeField, Tooltip("PlayerPrefs에 저장된 값이 없을 때(최초 실행 등) 사용할 기본 닉네임")]
    private string defaultNickname = "Player";

    /// <summary>현재 세션에서 사용 중인 닉네임. Load()/Save()를 통해서만 채워진다.</summary>
    public string Current { get; private set; }

    private bool loaded;

    /// <summary>
    /// PlayerPrefs에서 이전에 저장된 닉네임을 불러온다. 없으면 defaultNickname을 사용.
    /// 여러 번 호출해도 안전하다(이미 로드했으면 캐시된 값을 그대로 반환).
    /// </summary>
    public string Load()
    {
        if (!loaded)
        {
            Current = PlayerPrefs.GetString(PlayerPrefsKey, defaultNickname);
            loaded = true;
        }
        return Current;
    }

    /// <summary>
    /// 새 닉네임을 정리(trim/길이 제한)해서 Current에 반영하고 PlayerPrefs에 저장한다.
    /// 다음에 접속 화면을 다시 열었을 때 Load()로 그대로 재사용할 수 있게 하기 위함.
    /// 정리된 최종 문자열을 반환하므로, 호출부는 이 값을 그대로 입력창에 다시 표시하면 된다.
    /// </summary>
    public string Save(string nickname)
    {
        string sanitized = Sanitize(nickname);
        Current = sanitized;
        loaded = true;
        PlayerPrefs.SetString(PlayerPrefsKey, sanitized);
        PlayerPrefs.Save();
        return sanitized;
    }

    private string Sanitize(string raw)
    {
        string trimmed = string.IsNullOrWhiteSpace(raw) ? defaultNickname : raw.Trim();
        return trimmed.Length > MaxLength ? trimmed.Substring(0, MaxLength) : trimmed;
    }
}
