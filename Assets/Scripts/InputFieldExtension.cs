using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
[RequireComponent(typeof(TMP_InputField))]
public class InputFieldExtension : MonoBehaviour
{
  [SerializeField]
  TMP_InputField inputField;
  [SerializeField]
  List<string> forbiddenCharacters = new List<string>();
  // Start is called before the first frame update
  void Start()
  {
    inputField.onValidateInput += delegate (string input, int charIndex, char addedChar) { return CharactersValidation(addedChar); };
  }

  char CharactersValidation(char charToValidate)
  {
    if (forbiddenCharacters.Contains(charToValidate.ToString()))
    {
      charToValidate = '\0';
    }
    return charToValidate;
  }
}
