using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NumpadController : MonoBehaviour
{
    [Tooltip("The input field that this numpad types in.")]
    public TMP_InputField inputField;

    [Tooltip("Must be in order from 0-9")] public Button[] numbers;
    public Button backspaceButton;

    // Start is called before the first frame update
    private void Start()
    {
        // add listeners for each number 
        for (int i = 0; i < numbers.Length; i++)
        {
            string indexNumber = i.ToString();
            numbers[i].onClick.AddListener(() => { inputField.text += indexNumber; });
        }

        // backspace
        backspaceButton.onClick.AddListener(() =>
        {
            if (inputField.text.Length > 0)
            {
                inputField.text = inputField.text.Substring(0, inputField.text.Length - 1);
            }
        });
    }
}