using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class WaitingPlayerItem : MonoBehaviour
{
    [SerializeField] private List<GameObject> characters;
    [SerializeField] private TextMeshProUGUI playernameTMP;
    [SerializeField] private GameObject havePlayerIndicator;


    public void SetData(string charactername, bool havePlayer)
    {
        playernameTMP.text = charactername;

        playernameTMP.gameObject.SetActive(havePlayer);

        CharacterIconEnabler(havePlayer);
    }


    private void CharacterIconEnabler(bool enable)
    {
        for (int a = 0; a < characters.Count; a++)
            characters[a].SetActive(false);

        if (enable)
        {
            int rand = Random.Range(0, characters.Count);
            characters[rand].SetActive(true);
        }

        havePlayerIndicator.SetActive(enable);
    }
}
