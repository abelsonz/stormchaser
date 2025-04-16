using UnityEngine;
using TMPro;

public class HorizontalScroller : MonoBehaviour
{
    public float speed = 50f; // pixels per second
    private RectTransform rectTransform;
    private float startX;
    private float textWidth;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        startX = rectTransform.anchoredPosition.x;

        // Get the width of the text content
        textWidth = GetComponent<TextMeshProUGUI>().preferredWidth;
    }

    void Update()
    {
        Vector2 pos = rectTransform.anchoredPosition;
        pos.x -= speed * Time.deltaTime;

        // Reset position once it's completely out of view
        if (pos.x <= -textWidth)
            pos.x = startX;

        rectTransform.anchoredPosition = pos;
    }
}
