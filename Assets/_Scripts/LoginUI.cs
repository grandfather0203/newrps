using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Firebase.Auth;

public class LoginUI : MonoBehaviour {
    // 로그인 UI
    public GameObject loginPanel;
    public TMP_InputField loginEmailField;
    public TMP_InputField loginPasswordField;
    public Button loginButton;
    public Button goToSignupButton;

    // 회원가입 UI
    public GameObject signupPanel;
    public TMP_InputField signupEmailField;
    public TMP_InputField signupPasswordField;
    public TMP_InputField signupConfirmPasswordField;
    public Button signupButton;
    public Button goToLoginButton;

    // 코드 UI
    public GameObject codePanel;
    public TMP_Text codeDisplay;
    public TMP_InputField codeInputField;
    public Button submitCodeButton;
    public Button reissueCodeButton;

    void Start() {
        loginPanel.SetActive(true);
        signupPanel.SetActive(false);
        codePanel.SetActive(false);

        loginButton.onClick.AddListener(OnLoginClick);
        goToSignupButton.onClick.AddListener(OnGoToSignupClick);
        signupButton.onClick.AddListener(OnSignupClick);
        goToLoginButton.onClick.AddListener(OnGoToLoginClick);
        submitCodeButton.onClick.AddListener(OnSubmitCodeClick);
        reissueCodeButton.onClick.AddListener(OnReissueCodeClick);

        StartCoroutine(CheckConnectionPeriodically());
    }

    void OnLoginClick() {
        FindObjectOfType<AuthManager>().Login();
    }

    void OnGoToSignupClick() {
        loginPanel.SetActive(false);
        signupPanel.SetActive(true);
        codePanel.SetActive(false);
    }

    void OnSignupClick() {
        FindObjectOfType<AuthManager>().SignUp();
    }

    void OnGoToLoginClick() {
        loginPanel.SetActive(true);
        signupPanel.SetActive(false);
        codePanel.SetActive(false);
    }

    void OnSubmitCodeClick() {
        FindObjectOfType<AuthManager>().ConnectCouple();
    }

    void OnReissueCodeClick() {
        FindObjectOfType<AuthManager>().ReissueCoupleCode();
    }

    public void ShowCodePanel() {
        loginPanel.SetActive(false);
        signupPanel.SetActive(false);
        codePanel.SetActive(true);
    }

    System.Collections.IEnumerator CheckConnectionPeriodically() {
        while (true) {
            if (codePanel.activeSelf && FirebaseAuth.DefaultInstance.CurrentUser != null) {
                FindObjectOfType<AuthManager>().CheckCoupleStatus(FirebaseAuth.DefaultInstance.CurrentUser.UserId);
            }
            yield return new WaitForSeconds(5f);
        }
    }
}