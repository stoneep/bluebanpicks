using UnityEngine;
using UnityEngine.UI;

public class AlphaBlink : MonoBehaviour
{
    [SerializeField] private float speed = 3f;
    private Image image;

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