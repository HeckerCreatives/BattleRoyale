using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CharacterCreationController : MonoBehaviour
{
    public void Proceed()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
