using UnityEngine;
using UnityEngine.UI;

public class AlphaBlink : MonoBehaviour
{
    [SerializeField] private float speed = 3f; // 클수록 빠르게 깜빡임
    private Image image; // SpriteRenderer 쓴다면 이걸로 교체

    private void Awake()
    {
        image = GetComponent<Image>();
    }

    private void Update()
    {
        float alpha = Mathf.PingPong(Time.time * speed, 1f);
        var c = image.color;
        c.a = alpha;
        image.color = c;
    }
}