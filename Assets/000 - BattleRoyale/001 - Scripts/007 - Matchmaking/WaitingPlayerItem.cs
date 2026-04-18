using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WaitingPlayerItem : MonoBehaviour
{
    [SerializeField] private UserData userData;

    [Space]
    [SerializeField] private TextMeshProUGUI playernameTMP;
    [SerializeField] private GameObject havePlayerIndicator;

    [Space]
    [SerializeField] private GameObject avatarOne;
    [SerializeField] private GameObject avatarTwo;
    [SerializeField] private GameObject avatarThree;
    [SerializeField] private GameObject avatarFour;
    [SerializeField] private GameObject avatarFive;
    [SerializeField] private GameObject avatarSix;


    public void SetData(string charactername, bool havePlayer, string avatarid)
    {
        playernameTMP.text = charactername;

        playernameTMP.gameObject.SetActive(havePlayer);

        CharacterIconEnabler(havePlayer, avatarid, userData.Username == charactername);
    }


    private void CharacterIconEnabler(bool enable, string avatarid, bool isOwnPlayer)
    {
        avatarOne.SetActive(avatarid == "AVATAR1");
        avatarTwo.SetActive(avatarid == "AVATAR2");
        avatarThree.SetActive(avatarid == "AVATAR3");
        avatarFour.SetActive(avatarid == "AVATAR4");
        avatarFive.SetActive(avatarid == "AVATAR5");
        avatarSix.SetActive(avatarid == "AVATAR6");

        havePlayerIndicator.SetActive(enable);
        havePlayerIndicator.GetComponent<Image>().enabled = isOwnPlayer;
    }
}
