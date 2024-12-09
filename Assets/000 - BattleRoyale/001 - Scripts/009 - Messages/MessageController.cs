using MyBox;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MessageController : MonoBehaviour
{
    private event EventHandler SelectedChange;
    public event EventHandler OnSelectedChange
    {
        add
        {
            if (SelectedChange == null || !SelectedChange.GetInvocationList().Contains(value))
                SelectedChange += value;
        }
        remove { SelectedChange -= value; }
    }
    public MessageItem SelectedItem
    {
        get => selectedItem;
        set
        {
            selectedItem = value;
            SelectedChange?.Invoke(this, EventArgs.Empty);
        }
    }

    public bool CanAction { get => canAction; set => canAction = value; }

    //  ==========================

    [SerializeField] private GameObject messageObj;
    [SerializeField] private Transform messageParentTF;
    [SerializeField] private GameObject itemsPrefab;
    [SerializeField] private GameObject itemListContentObj;
    [SerializeField] private GameObject contentViewportObj;
    [SerializeField] private GameObject noItemsYetObj;

    [Header("LOADER")]
    [SerializeField] private GameObject itemListLoader;
    [SerializeField] private GameObject itemContentLoader;

    [Header("TMP")]
    [SerializeField] private TextMeshProUGUI messageTitleTMP;
    [SerializeField] private TextMeshProUGUI messageFromTMP;
    [SerializeField] private TextMeshProUGUI messageDayTMP;
    [SerializeField] private TextMeshProUGUI messageContentTMP;

    [Header("DEBUGGER")]
    [ReadOnly][SerializeField] private MessageItem selectedItem;
    [ReadOnly][SerializeField] private bool canAction;

    //  =====================

    Coroutine getMessages;

    //  =====================

    public IEnumerator SetMessageInboxItemSet()
    {
        itemListLoader.SetActive(true);
        itemContentLoader.SetActive(true);

        itemListContentObj.SetActive(false);
        contentViewportObj.SetActive(false);

        noItemsYetObj.SetActive(false);
        messageTitleTMP.text = "";
        messageFromTMP.text = "";
        messageDayTMP.text = "";
        messageContentTMP.text = "";

        while(messageParentTF.childCount > 0)
        {
            for(int a = 0; a < messageParentTF.childCount; a++)
            {
                Destroy(messageParentTF.GetChild(a).gameObject);
                yield return null;
            }

            yield return null;
        }

        yield return StartCoroutine(GameManager.Instance.GetRequest("/inbox/getinboxlist", "", false, (response) =>
        {
            if (response != null)
            {
                Dictionary<string, object> tempresponsedata = JsonConvert.DeserializeObject<Dictionary<string, object>>(response.ToString());

                Debug.Log(tempresponsedata["data"].ToString());

                Dictionary<string, object> tempdata = JsonConvert.DeserializeObject<Dictionary<string, object>>(tempresponsedata["data"].ToString());

                if (tempdata.ContainsKey("inbox"))
                {
                    List<MessageItem> itemList = JsonConvert.DeserializeObject<List<MessageItem>>(tempdata["inbox"].ToString());

                    if (itemList.Count > 0)
                    {
                        StartCoroutine(InstantiateItems(itemList));
                    }
                    else
                    {
                        noItemsYetObj.SetActive(true);

                        itemListLoader.SetActive(false);
                        itemContentLoader.SetActive(false);

                        CanAction = true;
                        getMessages = null;
                    }
                }
                else
                {
                    noItemsYetObj.SetActive(true);

                    itemListLoader.SetActive(false);
                    itemContentLoader.SetActive(false);

                    CanAction = true;
                    getMessages = null;
                }
            }
        }, () => { CanAction = true; }));
    }

    IEnumerator InstantiateItems(List<MessageItem> items)
    {
        for (int a = 0; a < items.Count; a++)
        {
            GameObject item = Instantiate(itemsPrefab, messageParentTF);

            item.GetComponent<InboxItem>().ItemData = items[a];
            item.GetComponent<InboxItem>().Controller = this;

            item.GetComponent<InboxItem>().SetData();

            yield return null;
        }

        SelectedItem = items[0];

        SetMessageContent();

        itemListContentObj.SetActive(true);
        itemListLoader.SetActive(false);
    }

    public void OpenMessage()
    {
        messageTitleTMP.text = "";
        messageFromTMP.text = "";
        messageDayTMP.text = "";
        messageContentTMP.text = "";

        itemContentLoader.SetActive(true);
        contentViewportObj.SetActive(false);
    }

    public void SetMessageContent()
    {
        messageTitleTMP.text = SelectedItem.title;
        messageFromTMP.text = "Master Admin";
        messageDayTMP.text = SelectedItem.datetime;
        messageContentTMP.text = SelectedItem.description;

        contentViewportObj.SetActive(true);
        itemContentLoader.SetActive(false);

        CanAction = true;
    }

    #region BUTTON

    public void ShowMessages()
    {
        //CanAction = false;

        getMessages = StartCoroutine(SetMessageInboxItemSet());
        messageObj.SetActive(true);
    }

    public void CloseMessages()
    {
        if (!CanAction) return;

        if (getMessages != null) StopCoroutine(getMessages);

        messageObj.SetActive(false);
    }

    #endregion
}

[System.Serializable]
public class MessageItem
{
    public string id;
    public string type;
    public string title;
    public string daysago;
    public string datetime;
    public string description;
    public string status;
}