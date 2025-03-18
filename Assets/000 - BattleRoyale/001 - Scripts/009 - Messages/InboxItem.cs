using MyBox;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InboxItem : MonoBehaviour
{
    public MessageController Controller { get => messageController; set => messageController = value; }

    public MessageItem ItemData { get => messageItem; set => messageItem = value; }

    //  ==============================

    [SerializeField] private UserData userData;

    [Header("IMAGES")]
    [SerializeField] private Image newMessageIconImg;
    [SerializeField] private Image inboxIconImg;
    [SerializeField] private Sprite announcementIcon;
    [SerializeField] private Sprite messageIcon;
    [SerializeField] private Image inboxItemSelect;
    [SerializeField] private Sprite itemSelectedOnSprite;
    [SerializeField] private Sprite itemSelectedOffSprite;

    [Header("TMP")]
    [SerializeField] private TextMeshProUGUI itemTitleTMP;
    [SerializeField] private TextMeshProUGUI fromTMP;
    [SerializeField] private TextMeshProUGUI daysCountTMP;

    [Header("DEBUGGER")]
    [ReadOnly][SerializeField] private MessageController messageController;
    [ReadOnly][SerializeField] private MessageItem messageItem;

    private void OnDisable()
    {
        messageController.OnSelectedChange -= SelectedChange;
    }

    public void SetData()
    {
        messageController.OnSelectedChange += SelectedChange;

        if (ItemData.status  == "unopen")
            newMessageIconImg.gameObject.SetActive(true);
        else
            newMessageIconImg.gameObject.SetActive(false);

        itemTitleTMP.text = ItemData.title;
        fromTMP.text = "Master Admin";
        daysCountTMP.text = ItemData.daysago;
    }

    private void SelectedChange(object sender, EventArgs e)
    {
        ChangeItemSelected();
    }

    private void ChangeItemSelected()
    {
        if (ItemData == Controller.SelectedItem)
            inboxItemSelect.sprite = itemSelectedOnSprite;
        else
            inboxItemSelect.sprite = itemSelectedOffSprite;
    }

    public void SelectMessage()
    {
        if (!Controller.CanAction) return;

        Controller.CanAction = false;

        if (ItemData.status != "unopen")
        {
            Controller.SelectedItem = ItemData;
            Controller.CanAction = false;
            Controller.SetMessageContent();
        }
        else
        {
            Controller.SelectedItem = ItemData;
            Controller.CanAction = false;

            ItemData.status = "opened";
            SetData();

            Controller.OpenMessage();

            StartCoroutine(GameManager.Instance.PostRequest("/inbox/openmessage", "", new Dictionary<string, object>
            {
                { "itemid", ItemData.id }
            }, false, (response) =>
            {
                userData.Messages.Find(x => x.id == ItemData.id).status = "opened";
                Controller.SetMessageContent();
            }, () => { }));
        }
    }
}
