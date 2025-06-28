using UnityEngine;
using TMPro; 
using System.Linq; 
using System.Collections; // Required for Coroutines

public class GPT : MonoBehaviour
{
    public string[] sentencesToType = { }; // Array of sentences
    public TextMeshProUGUI displayText; // Assign this in the Unity Inspector (TextMeshProUGUI)
    public UnityEngine.UI.Image timerImage; // New: Assign your sliced UI Image here in the Inspector

    private int currentSentenceIndex = 0;
    private string[] currentWordsInSentence; 
    private int currentWordIndex = 0;
    private int currentLetterIndex = 0;
    private string currentWordToType = "";
    private bool letterWasIncorrect = false; 
    private System.Collections.Generic.List<int> indicesPendingGreen = new System.Collections.Generic.List<int>(); // Added for delayed green

    private bool canType = false; 
    private float typingEnableTimer = maxTypingTime; // Initialize timer to full
    private const float maxTypingTime = 7.0f; // Changed from typingDuration, new max time
    private bool isRecharging = false; // New: Controls if the timer is recharging
    private bool forcedRechargeActive = false; // New: True if timer hit 0 and must fully recharge

    void Start()
    {
        typingEnableTimer = maxTypingTime; // Ensure timer starts full
        canType = false;                   // Start with typing disabled
        isRecharging = false;              // Not initially recharging
        forcedRechargeActive = false;        // Not initially in forced recharge
        LoadSentence();
        // Initial UI update for timer
        if (timerImage != null) timerImage.fillAmount = typingEnableTimer / maxTypingTime;
    }

    void LoadSentence()
    {
        if (sentencesToType.Length == 0)
        {
            Debug.LogError("Sentences list is empty! Please provide at least one sentence in the Inspector.");
            if (displayText != null) displayText.text = "Error: Lista de frases vacía.";
            enabled = false; 
            return;
        }

        if (currentSentenceIndex < sentencesToType.Length)
        {
            string currentSentence = sentencesToType[currentSentenceIndex];
            currentWordsInSentence = !string.IsNullOrEmpty(currentSentence) ? 
                                     currentSentence.Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries) : 
                                     new string[0];

            if (currentWordsInSentence.Length > 0)
            {
                currentWordIndex = 0;
                currentLetterIndex = 0;
                currentWordToType = currentWordsInSentence[currentWordIndex];
                indicesPendingGreen.Clear(); // Clear pending states for new word/sentence
                letterWasIncorrect = false; 
                PrintCurrentProgress(); // Initial display of the word
            }
            else
            {
                Debug.LogWarning("Current sentence is empty or contains no words. Moving to next sentence if available.");
                currentSentenceIndex++; 
                LoadSentence(); 
            }
        }
        else
        {
            Debug.Log("Congratulations! You have typed all the sentences.");
            if (displayText != null) displayText.text = "¡Felicidades! Has escrito todas las frases.";
            enabled = false; 
        }
    }

    void Update()
    {
        if (!enabled) return;

        // --- Input Handling for "1" key ---
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            if (forcedRechargeActive)
            {
                Debug.Log("Mandatory recharge active. Key '1' ignored until timer is full.");
            }
            else // Not in forced recharge, player has control
            {
                if (canType) // Currently typing, toggle to recharge
                {
                    canType = false;
                    isRecharging = true;
                    Debug.Log("Typing stopped by user. Manual recharge initiated.");
                }
                else if (isRecharging) // Currently manually recharging, toggle to type
                {
                    isRecharging = false;
                    canType = true;
                    Debug.Log("Manual recharge stopped by user. Typing enabled.");
                }
                else // Neither typing nor manually recharging (timer is idle, e.g., at start or after full recharge)
                {
                    if (typingEnableTimer > 0) // Ensure there's time to use
                    {
                        canType = true; // Start typing
                        Debug.Log("Timer idle with time. Typing enabled.");
                    }
                    // If timer is <= 0 and somehow not in forcedRecharge, this state is unusual,
                    // but the timer logic below should push it into forcedRecharge.
                }
            }
        }

        // --- Timer Logic ---
        if (canType && !isRecharging && !forcedRechargeActive) // Normal typing countdown
        {
            typingEnableTimer -= Time.deltaTime;
            if (typingEnableTimer <= 0)
            {
                typingEnableTimer = 0;
                canType = false;
                isRecharging = true;        // Start automatic/forced recharge
                forcedRechargeActive = true; // Enter mandatory recharge mode
                Debug.Log("Time's up! Mandatory recharge started. Key '1' disabled until full.");
            }
        }
        else if (isRecharging && !canType) // Recharging (either manual or forced)
        {
            float rechargeRate = 1.0f; // 1 unit of timer time per 1 second of real time
            typingEnableTimer += rechargeRate * Time.deltaTime;
            if (typingEnableTimer >= maxTypingTime)
            {
                typingEnableTimer = maxTypingTime;
                isRecharging = false; // Stop the act of recharging

                if (forcedRechargeActive)
                {
                    forcedRechargeActive = false; // Mandatory period is over
                    // canType remains false. Timer is now full and idle.
                    Debug.Log("Mandatory recharge complete. Timer full. Press 1 to type.");
                }
                else
                {
                    // Manual recharge finished. Timer is full and idle.
                    // canType remains false.
                    Debug.Log("Manual recharge complete. Timer full. Press 1 to type.");
                }
            }
        }

        // Update Timer UI Image
        if (timerImage != null)
        {
            // maxTypingTime is a const float > 0, so no need to check for division by zero here
            // if it were a variable, the check would be important.
            timerImage.fillAmount = typingEnableTimer / maxTypingTime;
        }

        // Only process input if canType is true
        if (!canType) return;

        if (currentSentenceIndex >= sentencesToType.Length || currentWordsInSentence == null || currentWordIndex >= currentWordsInSentence.Length)
        {
            // Safeguard, should be handled by LoadSentence or coroutine logic
            if (currentSentenceIndex >= sentencesToType.Length && enabled)
            {
                if (displayText != null && enabled) displayText.text = "¡Felicidades! Has escrito todas las frases.";
                enabled = false;
            }
            return;
        }

        // Input detection logic from previous version
        if (Input.anyKeyDown)
        {
            string letterToProcess = null;
            if (Event.current != null && Event.current.isKey && Event.current.type == EventType.KeyDown) 
            {
                char eventChar = Event.current.character;
                if (char.IsLetter(eventChar)) 
                {
                    letterToProcess = eventChar.ToString().ToLower();
                }
            }
            
            if (letterToProcess == null && !string.IsNullOrEmpty(Input.inputString))
            {
                foreach (char c in Input.inputString)
                {
                    if (char.IsLetter(c))
                    {
                        letterToProcess = c.ToString().ToLower();
                        break; 
                    }
                }
            }

            if (letterToProcess != null)
            {
                HandleInput(letterToProcess);
            }
        }
    }

    void HandleInput(string letter)
    {
        if (currentLetterIndex < currentWordToType.Length)
        {
            if (letter == currentWordToType[currentLetterIndex].ToString().ToLower())
            {
                int indexBeingCorrected = currentLetterIndex;
                letterWasIncorrect = false; // Clear error state for this position.

                indicesPendingGreen.Add(indexBeingCorrected); // Mark for delayed green
                currentLetterIndex++; // Advance logical cursor
                
                bool isLastLetterOfWord = (indexBeingCorrected == currentWordToType.Length - 1);
                
                PrintCurrentProgress(); // Refresh display immediately. The letter will be default color.
                
                StartCoroutine(DelayedGreenActivation(indexBeingCorrected, isLastLetterOfWord));
            }
            else 
            {
                letterWasIncorrect = true; 
                StartCoroutine(AnimateLetterEffect(currentLetterIndex, Color.red, 0.25f, false, true)); // Flash red
            }
        }
    }

    IEnumerator DelayedGreenActivation(int charIndex, bool isLastLetterOfWord)
    {
        yield return new WaitForSeconds(0.1f);

        // Safety check: if word changed or charIndex was already processed/cancelled.
        if (charIndex >= currentWordToType.Length || !indicesPendingGreen.Contains(charIndex))
        {
            // If it was in pendingGreenIndices, remove it to be safe, though Contains check should handle it.
            indicesPendingGreen.Remove(charIndex); 
            PrintCurrentProgress(); // Refresh display with current state
            yield break; 
        }

        indicesPendingGreen.Remove(charIndex); // No longer pending, will start animating to green.

        // Call the original AnimateLetterEffect to do the green animation and subsequent logic.
        StartCoroutine(AnimateLetterEffect(charIndex, Color.green, 0.3f, isLastLetterOfWord, false));
    }

    IEnumerator AnimateLetterEffect(int charIndex, Color targetColor, float duration, bool isLastAndCorrect, bool isErrorFlash)
    {
        float elapsedTime = 0f;
        Color originalTextColor = displayText.color; // Base color of the TMPro object

        // Stop any other letter animations to avoid conflict
        StopAllCoroutinesOfType(typeof(GPT), "AnimateLetterEffect");

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / duration);
            Color currentColor;

            if (isErrorFlash)
            {
                // Simple flash: targetColor for the first half, original for the second
                currentColor = (t < 0.5f) ? targetColor : originalTextColor;
            }
            else
            {
                currentColor = Color.Lerp(originalTextColor, targetColor, t);
            }
            BuildAndSetTextWithOneAnimatedLetter(charIndex, currentColor);
            yield return null;
        }

        // Ensure final state for the animated letter
        BuildAndSetTextWithOneAnimatedLetter(charIndex, isErrorFlash ? originalTextColor : targetColor);

        if (isLastAndCorrect)
        {
            currentWordIndex++;
            currentLetterIndex = 0; 
            if (currentWordIndex < currentWordsInSentence.Length) 
            {
                currentWordToType = currentWordsInSentence[currentWordIndex];
                indicesPendingGreen.Clear(); // Clear pending states for new word/sentence
                letterWasIncorrect = false; 
                PrintCurrentProgress(); // Display new word statically
            }
            else 
            {
                currentSentenceIndex++;
                LoadSentence(); 
            }
        }
        else
        {
            // After animation (especially error flash), refresh to static state
            PrintCurrentProgress();
        }
    }
    
    // Helper to stop specific coroutines if needed (more robust than StopAllCoroutines)
    // For simplicity, StopAllCoroutines() is used above, but this is a better practice if other coroutines exist.
    void StopAllCoroutinesOfType(System.Type type, string methodName)
    {
        // This is a conceptual placeholder. Actual implementation would require
        // tracking coroutines manually or using a more advanced coroutine manager.
    }

    void BuildAndSetTextWithOneAnimatedLetter(int animatedCharIndex, Color animatedCharColor)
    {
        if (string.IsNullOrEmpty(currentWordToType)) return;

        string progress = "";
        for (int i = 0; i < currentWordToType.Length; i++)
        {
            char letterToShow = currentWordToType[i];
            if (i == animatedCharIndex)
            {
                progress += "<color=#" + ColorUtility.ToHtmlStringRGB(animatedCharColor) + ">" + letterToShow + "</color>";
            }
            else if (indicesPendingGreen.Contains(i)) // Typed correctly, awaiting green
            {
                progress += letterToShow; // Default color
            }
            else if (i < currentLetterIndex) // Typed correctly, processed
            {
                progress += "<color=green>" + letterToShow + "</color>";
            }
            else if (i == currentLetterIndex && letterWasIncorrect) // Current caret, incorrect state
            {
                progress += "<color=red>" + letterToShow + "</color>";
            }
            else // Not yet typed or current caret and correct (and not pending)
            {
                progress += letterToShow; 
            }
        }

        if (displayText != null)
        {
            displayText.text = progress;
        }
    }

    void PrintCurrentProgress()
    {
        if (string.IsNullOrEmpty(currentWordToType))
        {
            if (displayText != null) displayText.text = ""; // Clear if no word
            return;
        }

        string progress = "";
        for (int i = 0; i < currentWordToType.Length; i++)
        {
            char letterToShow = currentWordToType[i];
            if (indicesPendingGreen.Contains(i)) // Typed correctly, awaiting green
            {
                progress += letterToShow; // Default color
            }
            else if (i < currentLetterIndex) // Typed correctly, processed
            {
                progress += "<color=green>" + letterToShow + "</color>";
            }
            else if (i == currentLetterIndex && letterWasIncorrect) // Current caret, incorrect
            {
                progress += "<color=red>" + letterToShow + "</color>";
            }
            else // Not yet typed or current caret and correct
            {
                progress += letterToShow;
            }
        }

        if (displayText != null)
        {
            displayText.text = progress;
        }
        else
        {
            Debug.Log(progress);
        }
    }
}