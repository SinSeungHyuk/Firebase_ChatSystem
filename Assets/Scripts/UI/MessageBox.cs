using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MessageBox : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI txtUsername;
    [SerializeField] private TextMeshProUGUI txtMessage;

    
    public void SetMessage(string username, string msg)
    {
        txtUsername.text = username;
        txtMessage.text = msg;
    }
}
