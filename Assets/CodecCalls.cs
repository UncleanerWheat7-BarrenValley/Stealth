using System.Collections;
using TMPro;
using UnityEngine;

public class CodecCalls : MonoBehaviour
{
    public TMP_Text dialogueText;
    public float textSpeed = 0.05f; // Speed at which the text is revealed
    public float delayBetweenLines = 1f; // Delay before showing the next line

    private string[] dialogueLines; // Array to store the dialogue lines
    private int currentLineIndex = 0; // Index to track the current line
    private bool isTyping = false; // Flag to indicate if typing is ongoing

    private void Start()
    {
        // Optionally, initialize some dialogue to test
        dialogueLines = new string[] {
            "Hello, Snake!",
            "This is your Codec device.",
            "You must defeat the terrorists.",
            "Be careful, Snake! They're everywhere!"
        };
    }

    public void StartCodecConversation(string[] dialogue)
    {
        // Initialize dialogue lines and reset the index
        dialogueLines = dialogue;
        currentLineIndex = 0;

        // Start the conversation if not typing
        if (!isTyping && dialogueLines.Length > 0)
        {
            Debug.Log("Starting dialogue sequence.");
            ShowNextDialogue();
        }
    }

    private void ShowNextDialogue()
    {
        // Check if typing is ongoing or if we have exhausted all lines
        if (isTyping || currentLineIndex >= dialogueLines.Length)
        {
            if (currentLineIndex >= dialogueLines.Length)
            {
                Debug.Log("All lines displayed. Ending conversation.");
                EndConversation();
            }
            return;
        }

        // Get the next line of dialogue
        string nextLine = dialogueLines[currentLineIndex];
        Debug.Log("Displaying line: " + nextLine);

        // Start typing the line
        if (currentLineIndex < dialogueLines.Length)
        {
            isTyping = true; // Set flag to prevent multiple calls
            StartCoroutine(TypeText(nextLine));
        }
    }

    IEnumerator TypeText(string text)
    {
        dialogueText.text = ""; // Clear previous text

        // Type each character one by one
        foreach (char letter in text.ToCharArray())
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(textSpeed);
        }

        // Wait before showing the next line
        yield return new WaitForSeconds(delayBetweenLines);

        // Move to the next line
        currentLineIndex++;

        // Reset typing flag and continue
        isTyping = false;
        ShowNextDialogue();
    }

    void EndConversation()
    {
        Debug.Log("Conversation ended.");
        // Additional logic for ending the conversation can be placed here
    }
}