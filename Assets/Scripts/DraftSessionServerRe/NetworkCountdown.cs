using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// 서버 권위 카운트다운 하나를 코루틴으로 돌려 NetworkVariable&lt;float&gt;에 반영하는 범용 헬퍼.
///
/// DraftSessionServer의 PreDraft(로딩 유예) / Turn(턴 제한시간) / PostDraft(종료 후 안내) 세
/// 카운트다운이 각각 거의 동일한 코루틴을 따로 들고 있던 걸 하나로 모은 것이다:
///  - 초 단위로 올림(Ceil)한 값이 실제로 바뀔 때만 NetworkVariable을 갱신해 불필요한 동기화를 줄인다.
///  - 이미 돌고 있는 카운트다운이 있으면 새로 시작하기 전에 먼저 멈춘다.
///  - 일시정지 체크가 필요한 카운트다운(턴 타이머)만 isPaused 콜백을 넘기면 되고, 필요 없는
///    카운트다운(종료 후 안내)은 생략하면 된다.
///
/// 주의: NetworkVariable 자체는 여전히 NetworkBehaviour(DraftSessionServer)의 필드로 선언되어 있어야
/// Netcode가 동기화 대상으로 인식한다. 이 클래스는 NetworkBehaviour가 아니라 그 필드의 참조만 받아
/// 값을 읽고 쓸 뿐이므로, 필드 선언 위치를 옮기는 것과는 무관하다.
/// </summary>
public class NetworkCountdown
{
    private readonly MonoBehaviour host;
    private readonly NetworkVariable<float> remainingSeconds;
    private readonly Func<bool> isPaused;
    private Coroutine routine;

    /// <param name="host">코루틴을 실제로 실행할 MonoBehaviour (보통 DraftSessionServer 자신, this).</param>
    /// <param name="remainingSeconds">카운트다운 값을 반영할 NetworkVariable.</param>
    /// <param name="isPaused">
    /// true를 반환하는 동안은 그 프레임의 경과 시간을 버리고 그 자리에서 멈춘다(선택).
    /// 넘기지 않으면 일시정지 없이 항상 흐른다.
    /// </param>
    public NetworkCountdown(MonoBehaviour host, NetworkVariable<float> remainingSeconds, Func<bool> isPaused = null)
    {
        this.host = host;
        this.remainingSeconds = remainingSeconds;
        this.isPaused = isPaused;
    }

    /// <summary>
    /// 실행 중인 코루틴만 멈추고 NetworkVariable 값은 건드리지 않는다.
    /// OnDestroy처럼 이미 스폰 해제 중이라 NetworkVariable 쓰기가 안전하지 않을 수 있는
    /// 시점의 정리용으로 쓴다.
    /// </summary>
    public void Cancel()
    {
        if (routine != null) host.StopCoroutine(routine);
        routine = null;
    }

    /// <summary>카운트다운을 멈추고 남은 시간을 0으로 되돌린다(동기화 O). 런타임 중 "타이머 끔" 용도.</summary>
    public void Stop()
    {
        Cancel();
        remainingSeconds.Value = 0f;
    }

    /// <summary>
    /// durationSeconds부터 0까지 카운트다운을 시작한다. 이미 돌고 있던 카운트다운은 먼저 멈춘다.
    ///
    /// durationSeconds가 0 이하여도 그대로 코루틴을 시작한다 - while 루프를 건너뛰고 곧바로
    /// onComplete가 호출되므로, "0초 = 즉시 완료"가 필요한 호출부(PreDraft)는 별도 분기 없이
    /// 이 메서드만 호출하면 된다. 반대로 "0 이하 = 기능 자체를 끔(완료 콜백 호출 안 함)"이 필요한
    /// 호출부(Turn/PostDraft)는 호출 전에 duration을 직접 확인해서 0 이하면 Stop()만 호출하고
    /// Begin()은 호출하지 않아야 한다 (DraftSessionServer의 RestartTurnTimer / BeginPostDraftCountdown 참고).
    /// </summary>
    public void Begin(float durationSeconds, Action onComplete)
    {
        if (routine != null) host.StopCoroutine(routine);
        routine = host.StartCoroutine(Run(durationSeconds, onComplete));
    }

    private IEnumerator Run(float durationSeconds, Action onComplete)
    {
        float remaining = Mathf.Max(0f, durationSeconds);
        remainingSeconds.Value = Mathf.Ceil(remaining);

        while (remaining > 0f)
        {
            yield return null;

            // 일시정지 중에는 이번 프레임의 경과 시간을 그냥 버린다 - remaining을 건드리지 않으므로
            // 코루틴을 취소/재시작하지 않고도 정확히 멈췄던 지점에서 다시 흐르게 된다.
            if (isPaused != null && isPaused()) continue;

            remaining -= Time.deltaTime;

            // NetworkVariable은 값이 실제로 바뀔 때만 트래픽을 보내므로, 프레임마다가 아니라
            // 초 단위(올림)로만 갱신해서 불필요한 동기화를 줄인다.
            float rounded = Mathf.Max(0f, Mathf.Ceil(remaining));
            if (!Mathf.Approximately(rounded, remainingSeconds.Value))
                remainingSeconds.Value = rounded;
        }

        remainingSeconds.Value = 0f;
        routine = null;
        onComplete?.Invoke();
    }
}
