using System.Collections;
using TMPro;
using UnityEngine;

public class PlayerDialogue : MonoBehaviour
{
	[SerializeField] private PlayerDialogue _otherPlayerDialogue = default;
	[SerializeField] private TextMeshProUGUI _dialogueText = default;
	private DialogueSO _dialogue;
	private CharacterTypeEnum _opponentCharacter;
	public bool FinishedDialogue { get; private set; }

	public void Initialize(DialogueSO dialogue, CharacterTypeEnum opponentCharacter)
	{
		_dialogue = dialogue;
		_opponentCharacter = opponentCharacter;
	}

	public void PlayDialogue()
	{
		transform.GetChild(0).gameObject.SetActive(true);
		StartCoroutine(PlayDialogueCoroutine(GetSentence(_dialogue, _opponentCharacter)));
	}

	IEnumerator PlayDialogueCoroutine(string sentence)
	{
		for (int i = 0; i < sentence.Length; i++)
		{
			yield return new WaitForSecondsRealtime(0.05f);
			_dialogueText.text += sentence[i];
		}
		yield return new WaitForSecondsRealtime(1.5f);
		transform.GetChild(0).gameObject.SetActive(false);
		yield return new WaitForSecondsRealtime(0.35f);
		FinishedDialogue = true;
		if (FinishedDialogue && _otherPlayerDialogue.FinishedDialogue)
		{
			GameManager.Instance.StartRound();
		}
	}

	private string GetSentence(DialogueSO dialogue, CharacterTypeEnum opponentCharacter)
	{
		for (int i = 0; i < dialogue.dialogues.Length; i++)
		{
			if (dialogue.dialogues[i].character == opponentCharacter)
			{
				return dialogue.dialogues[i].sentence;
			}
		}
		return "Let's fight.";
	}
}
