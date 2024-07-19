using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIController : MonoBehaviour
{
    public FirebaseController firebaseController;

    [SerializeField] private Button btnSignIn;
    [SerializeField] private Button btnSignOut;
    [SerializeField] private TextMeshProUGUI txtUserIDLabel;

    [SerializeField] private TMP_InputField inpUsername;
    [SerializeField] private TMP_InputField inpMessage;

    [SerializeField] private MessageBox msgBoxPrefab;
    [SerializeField] private Transform msgBoxContents;

    void Start()
    {
        UpdateUserInfo(false);
    }

    public void UpdateUserInfo(bool isSigned, string userId = "") 
    {
        if (isSigned)
        {
            btnSignIn.interactable = false;
            btnSignOut.interactable = true;
            txtUserIDLabel.text = "UID : " + userId;
        } else
        {
            btnSignOut.interactable = false;
            btnSignIn.interactable = true;
            txtUserIDLabel.text = "Sign Out";
        }
    }

    public void SendChatMessage()
    {
        btnSignIn.gameObject.SetActive(false);

        string username = inpUsername.text;
        string message = inpMessage.text;
        firebaseController.SendChatMessage(username, message);
    }

    public void AddChatMessage(string username, string msg)
    {
        // msgBoxContents의 자식오브젝트로 프리팹 생성
        MessageBox msgBox = Instantiate(msgBoxPrefab, msgBoxContents);

        msgBox.SetMessage(username, msg);
    }
}
