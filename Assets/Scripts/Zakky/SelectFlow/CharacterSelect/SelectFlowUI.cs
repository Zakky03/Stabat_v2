using UnityEngine;
using TMPro;

public class SelectFlowUI : MonoBehaviour
{
    TextMeshProUGUI text_;
    [SerializeField]
    CursorHand cursorHand_;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        text_ = GetComponent<TextMeshProUGUI>();
    }

    // Update is called once per frame
    void Update()
    {
        text_.text = $"{cursorHand_.playerKind}";
    }
}
