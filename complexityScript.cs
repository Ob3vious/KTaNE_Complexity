using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;

public class complexityScript : MonoBehaviour {

	//public stuff
	public KMAudio Audio;
	public KMSelectable[] Buttons;
	public GameObject[] OtherDisplays;
	public TextMesh[] Text;
	public List<MeshRenderer> ButtonMesh;
	public KMBombModule Module;

	//functionality
	private bool solved = false;
	private int stage = 0;
	private string stages;
	private int[] input = { 0, 0 };
	private int[] display = { 0, 0, 0, 0 };
	private int[] inbutton = { 0, 0 };
	private int[] answer = { 0, 0 };
	private int[] givenbase = { 0, 0 };

	//logging
	static int _moduleIdCounter = 1;
	int _moduleID = 0;

	private KMSelectable.OnInteractHandler Press(int pos)
	{
		return delegate
		{
			if (!solved)
			{
				Audio.PlaySoundAtTransform("Beep", Buttons[pos].transform);
				Buttons[pos].AddInteractionPunch(1f);
				switch (pos)
				{
					case 0:
						inbutton[0] = (inbutton[0] + 1) % givenbase[0];
						Text[3].text = "ABCDEFGH"[inbutton[0]].ToString() + "abcdefgh"[inbutton[1]].ToString();
						break;
					case 1:
						inbutton[1] = (inbutton[1] + 1) % givenbase[1];
						Text[3].text = "ABCDEFGH"[inbutton[0]].ToString() + "abcdefgh"[inbutton[1]].ToString();
						break;
					case 2:
						if (input[0] == answer[0] && input[1] == answer[1])
						{
							if (stage == 2)
							{
								Module.HandlePass();
								solved = true;
								foreach (var text in Text)
								{
									text.text = "";
								}
								StartCoroutine(Shutdown());
							}
							else
							{
								stage++;
								Generate(stage);
							}
						}
						else
						{
							Debug.LogFormat("[Complexity #{0}] Expected answer was {1}i, but I received {2}i; Strike!", _moduleID, answer.Join("+"), input.Join("+"));
							Module.HandleStrike();
							Generate(stage);
						}
						break;
					case 3:
						if (input[0] < givenbase[0] * givenbase[0] * givenbase[0] * givenbase[0] && input[1] < givenbase[1] * givenbase[1] * givenbase[1] * givenbase[1] && (inbutton[0] + inbutton[1] + input[0] + input[1] != 0))
						{
							for (int i = 0; i < 2; i++)
							{
								input[i] = input[i] * givenbase[i] + inbutton[i];
							}
							if (Text[0].text != "")
							{
								Text[0].text += "\n";
							}
							Text[0].text += "ABCDEFGH"[inbutton[0]].ToString() + "abcdefgh"[inbutton[1]].ToString();
						}
						break;
				}
			}
			return false;
		};
	}

	void Awake()
	{
		_moduleID = _moduleIdCounter++;
		for (int i = 0; i < Buttons.Length; i++)
		{
			Buttons[i].OnInteract += Press(i);
			int x = i;
			Buttons[i].OnHighlight += delegate { if (!solved) { ButtonMesh[x].material.color = new Color(1, .625f, .875f); } };
			Buttons[i].OnHighlightEnded += delegate { if (!solved) { ButtonMesh[x].material.color = new Color(1, .25f, .75f); } };
			ButtonMesh.Add(Buttons[i].GetComponent<MeshRenderer>());
		}
		foreach (var item in OtherDisplays)
		{
			ButtonMesh.Add(item.GetComponent<MeshRenderer>());
		}
	}

	void Start () {
		stages = "+-*/".ToCharArray().Shuffle().Take(3).Join("");
		Debug.Log(stages);
		givenbase = new int[] { Rnd.Range(2, 9), Rnd.Range(2, 9) };
		Debug.LogFormat("[Complexity #{0}] Using base {1}i.", _moduleID, givenbase.Join("+"));
		Generate(stage);
		StartCoroutine(FlickerScreen());
	}

	private void Generate(int stagenum)
	{
		switch (stages[stagenum])
		{
			case '+':
				for (int i = 0; i < 4; i++)
				{
					display[i] = Rnd.Range(0, givenbase[i % 2] * givenbase[i % 2]);
				}
				for (int i = 0; i < 2; i++)
				{
					answer[i] = display[i] + display[i + 2];
				}
				break;
			case '-':
				for (int i = 0; i < 4; i++)
				{
					display[i] = Rnd.Range(0, givenbase[i % 2] * givenbase[i % 2]);
				}
				for (int i = 0; i < 2; i++)
				{
					answer[i] = Math.Abs(display[i] - display[i + 2]);
				}
				break;
			case '*':
				answer[0] = 0;
				answer[1] = 0;
				while (answer[0] > givenbase[0] * givenbase[0] * givenbase[0] || answer[1] > givenbase[1] * givenbase[1] * givenbase[1] || (answer[0] == 0 && answer[1] == 0))
				{
					for (int i = 0; i < 4; i++)
					{
						display[i] = Rnd.Range(0, givenbase[i % 2] * givenbase[i % 2]);
					}
					answer[0] = Math.Abs(display[0] * display[2] - display[1] * display[3]);
					answer[1] = display[0] * display[3] + display[1] * display[2];
				}
				break;
			case '/':
				display[0] = 0;
				while (display[0] > givenbase[0] * givenbase[0] * givenbase[0] || display[1] > givenbase[1] * givenbase[1] * givenbase[1] || display[0] <= 0 || display[1] <= 1)
				{
					for (int i = 0; i < 2; i++)
					{
						display[i + 2] = Rnd.Range(0, givenbase[i]);
						answer[i] = Rnd.Range(0, givenbase[i] * givenbase[i]);
					}
					display[0] = display[2] * answer[0] - display[3] * answer[1];
					display[1] = display[2] * answer[1] + display[3] * answer[0];
				}
				break;
		}
		Debug.LogFormat("[Complexity #{0}] Generated ({1}i){2}({3}i). Expected answer is {4}i.", _moduleID, display.Take(2).Join("+"), stages[stagenum], display.Skip(2).Join("+"), answer.Join("+"));
		Text[0].text = "";
		Text[1].text = ConvertTo(display[0], display[1]) + "\n\n" + ConvertTo(display[2], display[3]);
		Text[2].text = stages[stagenum].ToString();
		inbutton = new int[] { 0, 0 };
		input = new int[] { 0, 0 };
		Text[3].text = "Aa";
	}

	private string ConvertTo(int Re, int Im)
	{
		string output = "ABCDEFGH"[Re % givenbase[0]].ToString() + "abcdefgh"[Im % givenbase[1]].ToString(); ;
		while (Re + Im > 0)
		{
			Re /= givenbase[0];
			Im /= givenbase[1];
			if (Re + Im > 0)
				output = "ABCDEFGH"[Re % givenbase[0]].ToString() + "abcdefgh"[Im % givenbase[1]].ToString() + "\n" + output;
		}
		return output;
	}

	private IEnumerator FlickerScreen()
	{
		while (true)
		{
			float x = Rnd.Range(0f, 1f);
			ButtonMesh[5].material.color = Color.Lerp(new Color(1, .25f, .75f), new Color(.0625f, .0625f, .0625f), x); 
			yield return new WaitForSeconds(0.05f);
		}
	}

	private IEnumerator Shutdown()
	{
		for (int i = 0; i < 20; i++)
		{
			float x = Rnd.Range(0f, 1f);
			for (int j = 0; j < ButtonMesh.Count(); j++)
			{
				if (j != 5)
				{
					ButtonMesh[j].material.color = Color.Lerp(new Color(1, .25f, .75f), new Color(.0625f, .0625f, .0625f), x);
				}
			}
			yield return new WaitForSeconds(0.05f);
		}
		for (int i = 0; i < ButtonMesh.Count(); i++)
		{
			if (i != 5)
			{
				ButtonMesh[i].material.color = new Color(.0625f, .0625f, .0625f);
			}
		}
	}

#pragma warning disable 414
	private string TwitchHelpMessage = "'!{0} rias' to press 'real modifier', 'imaginary modifier', 'append to number' and 'submit' buttons respectively.";
#pragma warning restore 414
	IEnumerator ProcessTwitchCommand(string command)
	{
		yield return null;
		for (int i = 0; i < command.Length; i++)
		{
			if (!"risa".Contains(command[i]))
			{
				yield return "sendtochaterror Invalid command.";
				yield break;
			}
		}
		for (int i = 0; i < command.Length; i++)
		{
			for (int j = 0; j < 4; j++)
			{
				if ("risa"[j] == command[i])
				{
					Buttons[j].OnInteract();
					yield return null;
				}
			}
		}
	}

	IEnumerator TwitchHandleForcedSolve()
	{
		yield return true;
		while (!solved)
		{
			string ans = ConvertTo(answer[0], answer[1]).Replace("\n", "");
			for (int i = 0; i < ans.Length / 2; i++)
			{
				while (ans[2 * i] != Text[3].text[0])
				{
					Buttons[0].OnInteract();
					yield return true;
				}
				while (ans[2 * i + 1] != Text[3].text[1])
				{
					Buttons[1].OnInteract();
					yield return true;
				}
				Buttons[3].OnInteract();
				yield return true;
			}
			Buttons[2].OnInteract();
			yield return true;
		}
	}
}
