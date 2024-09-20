using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;
using Rnd = UnityEngine.Random;
using ExMath = ExMath;

public class RustedMorsematics : MonoBehaviour {

   public KMBombInfo Bomb;
   public KMAudio Audio;

   public KMSelectable PlayButton;
   public KMSelectable[] InputButtons; // dot dash space
   public KMSelectable ClearButton;
   public KMSelectable SubmitButton;

   public TextMesh InputDisplay;
   public MeshRenderer MorseScreen;
   public Material[] MorseFlashColour;

   public MeshFilter PuzzleBackground;
   public Mesh[] MeshOptions;
   public KMStatusLightParent SL;

   private static string[] TheMorse = {"-----", ".----", "..---", "...--", "....-", ".....", "-....", "--...", "---..", "----.", 
      ".-", "-...", "-.-.", "-..", ".", "..-.", "--.", "....", "..", ".---", "-.-", ".-..", "--", "-.", "---", ".--.", "--.-", ".-.", "...", "-", "..-", "...-", ".--", "-..-", "-.--", "--..",
      "-....-"};
   private static string Base36 = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ-";
   private static string[] Operators = {"ADD", "SUB", "MULT"};
   private static string[] OperatorsMorse = {".- -.. -..", "... ..- -...", "-- ..- .-.. -"};

   private int num1;
   private int num2;
   private int op;
   private int theAnswer;
   List<string> MorseSequenceOriginal = new List<string> {};
   //List<string> MorseSequenceModified = new List<string> {};
   private string MorseSequenceModified = "";
   private string inputMorse = "";
   private char[] inputChars = {'\0', '\0', '\0', '\0', '\0'};
   private int inputCharPos = 0;

   private bool playing = false;

   static int ModuleIdCounter = 1;
   int ModuleId;
   private bool ModuleSolved = false;

   void Awake () { //Avoid doing calculations in here regarding edgework. Just use this for setting up buttons for simplicity.
      ModuleId = ModuleIdCounter++;
      GetComponent<KMBombModule>().OnActivate += Activate;

      int meshNum = Rnd.Range(0, MeshOptions.Length);
      PuzzleBackground.mesh = MeshOptions[meshNum];
      if (meshNum == 0) { // BrokenSL, Broken1, Broken2
         SL.transform.Rotate(new Vector3(48.88f, 38.8f, -23.112f)); //72.373f, 0.051f, -23.112f   55f, 314.12f, 0
         //SL.transform.position -= new Vector3(0.011066f, 0.01346f, 0.029057f);
      }
      //SL pos: 0.075167 0.01986 0.076057
      //SL new: 0.0641 0.0064 0.047 rot 55 314.12 0 

      PlayButton.OnInteract += delegate () { PlayPress(); return false; };
      InputButtons[0].OnInteract += delegate () { InputPress('.'); return false; };
      InputButtons[1].OnInteract += delegate () { InputPress('-'); return false; };
      InputButtons[2].OnInteract += delegate () { InputPress(' '); return false; };
      ClearButton.OnInteract += delegate () { ClearPress(); return false; };
      SubmitButton.OnInteract += delegate () { SubmitPress(); return false; };

   }

   

   void Activate () { //Shit that should happen when the bomb arrives (factory)/Lights turn on
      InputDisplay.text = "";
   }

   void Start () { //Shit that you calculate, usually a majority if not all of the module
      // Choose operator
      op = Rnd.Range(0, 3);

      // Set initial numbers
      switch (Operators[op]) {
         case "MULT":
            num1 = Rnd.Range(2, 10);
            num2 = Rnd.Range(2, 10);
            theAnswer = num1 * num2;
            break;
         case "ADD":
            num1 = Rnd.Range(0, 100);
            num2 = Rnd.Range(0, 100);
            theAnswer = num1 + num2;
            break;
         case "SUB":
            num1 = Rnd.Range(0, 100);
            num2 = Rnd.Range(0, 100);
            theAnswer = num1 - num2;
            break;
      }

      // Get serial stuff
      int serDig1 = Base36.IndexOf(Bomb.GetSerialNumber().ToArray()[0]);
      int serDig2 = Base36.IndexOf(Bomb.GetSerialNumber().ToArray()[1]);
      if (serDig1 > 31) {
         serDig1 = 31;
      }
      if (serDig2 > 31) {
         serDig2 = 31;
      }
      string binVal1 = Convert.ToString(serDig1, 2).PadLeft(5, '0');
      string binVal2 = Convert.ToString(serDig2, 2).PadLeft(5, '0');

      //Debug.LogFormat("{0} {1} {2} = {5}, {3}, {4}", num1, Operators[op], num2, binVal1, binVal2, theAnswer);

      // Add morse sequence
      MorseSequenceOriginal.Add(NumberToMorse(num1));
      MorseSequenceOriginal.Add(OperatorsMorse[op]);
      MorseSequenceOriginal.Add(NumberToMorse(num2));
      //Debug.LogFormat("{0}", string.Join(" ", MorseSequenceOriginal.ToArray()));

      // Modify morse sequence
      for (int i = 0; i < 3; i++) { // Loop each element of sequence
         char[] curMorseSeq = MorseSequenceOriginal[i].ToCharArray();
         if (i == 0 || i == 2) {
            for (int j = 0; j < 5; j++) {
               for (int w = 0; w < Convert.ToInt32(Math.Floor(new decimal(MorseSequenceOriginal[i].Length / 5))); w++) {
                  if (i == 0) {
                     if (binVal1[j] == '1') {
                        switch (MorseSequenceOriginal[i][j + w*6]) {
                           case '-':
                              curMorseSeq[j + w*6] = '.';
                              break;
                           case '.':
                              curMorseSeq[j + w*6] = '-';
                              break;
                        }
                     }
                  } else {
                     if (binVal2[j] == '1') {
                        switch (MorseSequenceOriginal[i][j + w*6]) {
                           case '-':
                              curMorseSeq[j + w*6] = '.';
                              break;
                           case '.':
                              curMorseSeq[j + w*6] = '-';
                              break;
                        }
                     }
                  }
               }
            }
            MorseSequenceModified += new string(curMorseSeq);
            //MorseSequenceModified.Add(new string(curMorseSeq));
         } else {
            string[] yes = MorseSequenceOriginal[1].Split(' ');
            yes = yes.Reverse().ToArray();
            MorseSequenceModified += string.Join(" ", yes) + " ";
            //MorseSequenceModified.Add(string.Join(" ", yes) + " ");
         }
      }

      Debug.LogFormat("[Rusted Morsematics #{0}] The flashing morse sequence is: {1}", ModuleId, MorseSequenceModified);
      Debug.LogFormat("[Rusted Morsematics #{0}] The binary values of the first two serial characters are {1} and {2}", ModuleId, binVal1, binVal2);
      Debug.LogFormat("[Rusted Morsematics #{0}] The corrected morse sequence is: a = {1}| op = {2} | b = {3}", ModuleId, MorseSequenceOriginal[0], MorseSequenceOriginal[1], MorseSequenceOriginal[2]); // writing this like 3 weeks after doing the initial code; why the fuck did I do the spacing like this in this variable
      Debug.LogFormat("[Rusted Morsematics #{0}] The morse reads the equation: {1} {2} {3}", ModuleId, num1, Operators[op], num2);
      Debug.LogFormat("[Rusted Morsematics #{0}] The correct value to submit is: {1}", ModuleId, theAnswer);

   }

   // Func for converting multiple chars to morse
   string NumberToMorse(int num) {
      string numstring = num.ToString();
      string nummorse = "";

      foreach (char c in numstring) {
         nummorse += TheMorse[c - '0'] + " ";
      }

      return nummorse;
   }

   IEnumerator MorseFlashing() {

      // the loop of all time
      // ok I take it back this code is kinda fresh
      foreach (char c in MorseSequenceModified) {
         switch (c) {

            case '-':
               MorseScreen.material = MorseFlashColour[1];
               yield return new WaitForSecondsRealtime(0.75f);
               MorseScreen.material = MorseFlashColour[0];
               break;
            case '.':
               MorseScreen.material = MorseFlashColour[1];
               yield return new WaitForSecondsRealtime(0.25f);
               MorseScreen.material = MorseFlashColour[0];
               break;
            case ' ':
               yield return new WaitForSecondsRealtime(0.75f);
               break;
         }
         yield return new WaitForSecondsRealtime(0.25f);
      }
      
      playing = false;
   }
   
   void PlayPress() {
      PlayButton.AddInteractionPunch();
      Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, PlayButton.transform);
      if (!ModuleSolved) {
         if (playing == false) {
            playing = true;
            //Debug.Log("it's morse time");
            StartCoroutine(MorseFlashing());
         } else {
            StopAllCoroutines();
            MorseScreen.material = MorseFlashColour[0];
            playing = false;
         }
      }
   }

   void InputPress(char button) {
      InputButtons[1].AddInteractionPunch();
      Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, InputButtons[1].transform);
      if (!ModuleSolved) {
         if (button == ' ') {
            if (inputCharPos < 5 && inputMorse != "") { // space button just wont work after 5 chars
               inputCharPos++;
               inputMorse = "";
               InputDisplay.text += " ";
            }
         } else {
            inputMorse += button;
            try {
               inputChars[inputCharPos] = Base36[Array.IndexOf(TheMorse, inputMorse)];
            } catch (IndexOutOfRangeException) {
               inputChars[inputCharPos] = '?';
            }
         }
         
         InputDisplay.text = new string(inputChars);
      }
   }

   void ClearPress() {
      ClearButton.AddInteractionPunch();
      Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, ClearButton.transform);
      if (!ModuleSolved) {
         ClearScreens();
      }
   }

   void SubmitPress() {
      SubmitButton.AddInteractionPunch();
      Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, SubmitButton.transform);
      if (!ModuleSolved) {
         Debug.LogFormat("[Rusted Morsematics #{0}] Number submitted: {1}", ModuleId, InputDisplay.text);
         int submission;
         try { //yucky!!!
            submission = Convert.ToInt32(InputDisplay.text);
            if (submission == theAnswer) {
               Solve();
            } else {
               Strike();
            }
         } catch (FormatException) {
            Strike(); // can't convert to int so it aint correct
         }
         
      }
   }

   void ClearScreens() {
      inputMorse = "";
      inputChars[0] = '\0';
      inputChars[1] = '\0';
      inputChars[2] = '\0';
      inputChars[3] = '\0';
      inputChars[4] = '\0';
      inputCharPos = 0;
      InputDisplay.text = "";
   }

   void Solve () {
      ModuleSolved = true;
      StopAllCoroutines();
      MorseScreen.material = MorseFlashColour[0];
      playing = false;
      Debug.LogFormat("[Rusted Morsematics #{0}] That was correct. Module Solved!", ModuleId);
      GetComponent<KMBombModule>().HandlePass();
   }

   void Strike () {
      ClearScreens();
      Debug.LogFormat("[Rusted Morsematics #{0}] That was incorrect. Strike!", ModuleId);
      GetComponent<KMBombModule>().HandleStrike();
   }

#pragma warning disable 414
   private readonly string TwitchHelpMessage = @"Use !{0} play to play the morse sequence. To submit into the module, use !{0} input followed by each morse number seperated by spaces. Use !{0} submit to submit and !{0} clear to clear input.";
#pragma warning restore 414

   IEnumerator ProcessTwitchCommand (string Command) {
      Command = Command.Trim().ToUpper();
      yield return null;
      string[] parameters = Command.Split(' ');

      if (parameters[0] == "PLAY") {
         PlayButton.OnInteract();
      } else if (parameters[0] == "INPUT") {
         // Check if parameters are valid morse
         var regexItem = new Regex("^[.-]+$");
         for (int i = 1; i < parameters.Length; i++) {
            if (!regexItem.IsMatch(parameters[i])) {
               yield return "sendtochaterror Invalid Morse Code Submission.";
               yield break;
            }
         }
         // Press input keys
         for (int i = 1; i < parameters.Length; i++) {
            foreach (char c in parameters[i]) {
               switch (c) {
                  case '.': InputButtons[0].OnInteract(); break;
                  case '-': InputButtons[1].OnInteract(); break;
               }
               yield return new WaitForSeconds(.1f);
            }
           InputButtons[2].OnInteract();
         }
      } else if (parameters[0] == "CLEAR") {
         ClearButton.OnInteract();
      } else if (parameters[0] == "SUBMIT") {
         SubmitButton.OnInteract();
      } else {
         yield return "sendtochaterror Invalid Command.";
         yield break;
      }
   }

   IEnumerator TwitchHandleForcedSolve () {
      // Clear anything first
      ClearButton.OnInteract();
      string correctMorseInput = NumberToMorse(theAnswer);
      foreach (char c in correctMorseInput) {
         switch (c) {
            case '.': InputButtons[0].OnInteract(); break;
            case '-': InputButtons[1].OnInteract(); break;
            case ' ': InputButtons[2].OnInteract(); break;
         }
         yield return new WaitForSeconds(.1f);
      }
      SubmitButton.OnInteract();
   }
}
