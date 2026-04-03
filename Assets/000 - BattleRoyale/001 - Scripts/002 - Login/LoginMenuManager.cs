using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class LoginMenuManager : MonoBehaviour
{
    private event EventHandler LoginMenuStateChange;
    public event EventHandler OnLoginMenuStateChange
    {
        add
        {
            if (LoginMenuStateChange == null || LoginMenuStateChange.GetInvocationList().Contains(value))
                LoginMenuStateChange += value;
        }
        remove { LoginMenuStateChange -= value; }
    }

    public enum LoginMenuState
    {
        MAIN,
        LOGINREGISTER,
        MORE,
        SOCMED,
        CONTACTUS,
        REPAIR,
        SERVERSELECT,
        SERVERSELECTION
    }

    public LoginMenuState MenuState
    {
        get => loginMenuState;
        set
        {
            lastLoginMenuState.Add(loginMenuState);
            loginMenuState = value;
            LoginMenuStateChange?.Invoke(this, EventArgs.Empty);
        }
    }

    public LoginMenuState GoingBackMenuState
    {
        get => loginMenuState;
        set
        {
            loginMenuState = value;
            LoginMenuStateChange?.Invoke(this, EventArgs.Empty);
        }
    }


    private event EventHandler LoginRegisterStateChange;
    public event EventHandler OnLoginRegisterStateChange
    {
        add
        {
            if (LoginRegisterStateChange == null || LoginRegisterStateChange.GetInvocationList().Contains(value))
                LoginRegisterStateChange += value;
        }
        remove { LoginRegisterStateChange -= value; }
    }

    public enum LoginRegisterState
    {
        LOGIN,
        REGISTER
    }

    public LoginRegisterState CurrentLoginRegisterState
    {
        get => loginRegisterState;
        set
        {
            loginRegisterState = value;
            LoginRegisterStateChange?.Invoke(this, EventArgs.Empty);
        }
    }

    //  =========================

    [SerializeField] private LoginManager loginManager;

    [Space]
    [SerializeField] private GameObject mainObj;
    [SerializeField] private GameObject loginRegisterObj;
    [SerializeField] private GameObject moreObj;
    [SerializeField] private GameObject socMedObj;
    [SerializeField] private GameObject contactUsObj;

    [Space]
    [SerializeField] private GameObject loginObj;
    [SerializeField] private GameObject registerObj;
    [SerializeField] private Image loginBtnImg;
    [SerializeField] private Image registerBtnImg;

    [Space]
    [SerializeField] private Image repairBtnImg;
    [SerializeField] private Image socmedBtnImg;
    [SerializeField] private Image contactUsImg;

    [Space]
    [SerializeField] private GameObject serverSelectObj;
    [SerializeField] private GameObject serverSelectionObj;

    [Space]
    [SerializeField] private Color selected;
    [SerializeField] private Color unselected;

    [Header("DEBUGGER")]
    [SerializeField] private LoginMenuState loginMenuState;
    [SerializeField] private List<LoginMenuState> lastLoginMenuState;
    [SerializeField] private LoginRegisterState loginRegisterState;

    private void OnEnable()
    {
        //PanelEnabler();
        LoginRegisterPanelChecker();

        LoginMenuStateChange += StateChange;
        LoginRegisterStateChange += LoginRegisterStateChanged;
    }

    private void OnDisable()
    {
        LoginMenuStateChange -= StateChange;
        LoginRegisterStateChange -= LoginRegisterStateChanged;
    }

    #region MENU PANELS

    private void StateChange(object sender, EventArgs e)
    {
        PanelEnabler();
    }

    private void PanelEnabler()
    {
        mainObj.SetActive(false);
        loginRegisterObj.SetActive(false);
        moreObj.SetActive(false);
        socMedObj.SetActive(false);
        contactUsObj.SetActive(false);
        serverSelectObj.SetActive(false);
        serverSelectionObj.SetActive(false);

        repairBtnImg.color = selected;
        socmedBtnImg.color = selected;
        contactUsImg.color = selected;

        switch (MenuState)
        {
            case LoginMenuState.MAIN:
                mainObj.SetActive(true);
                break;
            case LoginMenuState.LOGINREGISTER:
                CurrentLoginRegisterState = LoginRegisterState.LOGIN;
                loginRegisterObj.SetActive(true);
                break;
            case LoginMenuState.MORE:
                moreObj.SetActive(true);
                break;
            case LoginMenuState.SOCMED:
                socMedObj.SetActive(true);

                repairBtnImg.color = unselected;
                contactUsImg.color = unselected;
                break;
            case LoginMenuState.CONTACTUS:
                contactUsObj.SetActive(true);

                repairBtnImg.color = unselected;
                socmedBtnImg.color = unselected;
                break;
            case LoginMenuState.SERVERSELECT:
                serverSelectObj.SetActive(true);
                break;
            case LoginMenuState.SERVERSELECTION:
                serverSelectionObj.SetActive(true);
                break;
        }
    }

    public void ReturnToPreviousState()
    {
        GoingBackMenuState = lastLoginMenuState[lastLoginMenuState.Count - 1];
        lastLoginMenuState.RemoveAt(lastLoginMenuState.Count - 1);
    }

    public void ChangeMenuState(int index)
    {
        if (index == (int)MenuState)
            return;

        MenuState = (LoginMenuState)index;
    }

    #endregion

    #region LOGIN REGISTER PANELS

    private void LoginRegisterStateChanged(object sender, EventArgs e)
    {
        LoginRegisterPanelChecker();
    }

    private void LoginRegisterPanelChecker()
    {
        loginBtnImg.color = unselected;
        registerBtnImg.color = unselected;

        loginObj.SetActive(false);
        registerObj.SetActive(false);

        switch (CurrentLoginRegisterState)
        {
            case LoginRegisterState.LOGIN:
                loginBtnImg.color = selected;
                loginObj.SetActive(true);
                break;
            case LoginRegisterState.REGISTER:
                registerBtnImg.color = selected;
                registerObj.SetActive(true);
                break;
        }
    }

    public void LoginRegisterChangeState(int index) => CurrentLoginRegisterState = (LoginRegisterState)index;

    #endregion
}
