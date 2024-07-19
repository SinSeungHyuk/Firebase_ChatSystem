using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Firebase.Extensions;
using Firebase.Auth;
using Firebase.Database;
using System.Threading.Tasks;
using System;

public class FirebaseController : MonoBehaviour
{
    public UIController UiController;

    private FirebaseAuth auth; // ������ ���� ���� ������ ��ü
    private FirebaseUser user; // ���̾�̽� ������ ������ ���� ��ü

    private List<string> receiveKeyList = new List<string>();


    void Start()
    {
        // �����÷��̿� ������ ����� ȣȯ�� �Ǵ��� �˻�
        Firebase.FirebaseApp.CheckAndFixDependenciesAsync()
            .ContinueWithOnMainThread(task =>
            {
                if ( task.Result == Firebase.DependencyStatus.Available) {
                    FirebaseInit();
                }
                else
                {
                    Debug.LogError("CheckAndFixDependenciesAsync Fail");
                }
            });
    }

    private void FirebaseInit()
    {
        auth = FirebaseAuth.DefaultInstance;

        // auth ��ü�� �̺�Ʈ�� �Լ� ����
        auth.StateChanged += AuthStateChanged;

        DatabaseReference chatDB = FirebaseDatabase.DefaultInstance.GetReference("ChatMessage");
        chatDB.OrderByChild("timestamp").LimitToLast(1).ValueChanged += ReceiveMessage;

        // Google �α��ο� �ʿ��� ��ū ����
        string googleIdToken = "YOUR_GOOGLE_ID_TOKEN";

        // GoogleAuthProvider�� ����Ͽ� Credential ����
        Credential credential = GoogleAuthProvider.GetCredential(googleIdToken, null);

        // Credential�� ����Ͽ� Google �������� �α��� �õ�
        auth.SignInWithCredentialAsync(credential).ContinueWithOnMainThread(task =>
        {
            if (task.IsCanceled)
            {
                Debug.LogError("SignInWithCredentialAsync was canceled.");
                return;
            }
            if (task.IsFaulted)
            {
                Debug.LogError("SignInWithCredentialAsync encountered an error: " + task.Exception);
                return;
            }

            // �α��� ���� ��
            FirebaseUser newUser = task.Result;
            Debug.Log("Google ����� �α��� ����: " + newUser.DisplayName);
        });
    }

    private void AuthStateChanged(object sender, EventArgs args)
    {
        FirebaseAuth senderAuth = sender as FirebaseAuth;

        if (senderAuth != null)
        {
            // CurrentUser : ���� ������ ������ ����
            user = senderAuth.CurrentUser;
            if (user != null)
            {
                Debug.Log(user.UserId); // ������ �ִٸ� id �α�
                UiController.UpdateUserInfo(true, user.UserId);
            }
        }
    }

    public void ReceiveMessage(object sender, ValueChangedEventArgs e) 
    {
        DataSnapshot snapshot = e.Snapshot;

        foreach (var data in snapshot.Children) // ChatMessage ���� �޼�����
        {
            if (receiveKeyList.Contains(data.Key)) continue;

            string username = data.Child("username").Value.ToString();
            string msg = data.Child("message").Value.ToString();
            UiController.AddChatMessage(username,msg);
            receiveKeyList.Add(data.Key);
        }
}

    // �͸� �α��� �Լ� - ��ư�� ���
    public void SignIn()
    {
        SignInAnonymous();
    }
    private Task SignInAnonymous()
    {
        return auth.SignInAnonymouslyAsync().
            ContinueWithOnMainThread( task =>
            {
                if (task.IsFaulted) Debug.LogError("SignIn Fail!!!!");
                else if (task.IsCompleted) Debug.Log("SignIn Completed");
            });
    }

    // �͸� �α׾ƿ� �Լ� - ��ư�� ���
    public void SignOut()
    {
        auth.SignOut();
        UiController.UpdateUserInfo(false);
    }

    public void ReadChatMessage()
    {
        // DefaultInstance : �ֿܼ� �ִ� ���� �⺻���� �ֻ��� �����ͺ��̽�
        DatabaseReference chatDB = FirebaseDatabase.DefaultInstance.GetReference("ChatMessage");
        chatDB.GetValueAsync().ContinueWithOnMainThread(
                task =>
                {
                    if (task.IsFaulted) Debug.LogError("ReadError");
                    else if (task.IsCompleted)
                    {
                        // DataSnapshot : ChatMessage �ȿ� �ִ� ���������Ϳ� ���ٰ���
                        DataSnapshot snapshot = task.Result;

                        // DataSnapshot�� ���� ���� ����
                        Debug.Log("������ ��:" + snapshot.ChildrenCount);

                        foreach (var data in snapshot.Children) // ChatMessage ���� �޼�����
                        {
                            Debug.Log("�޼�����ȣ :" + data.Key + 
                                "\n" + data.Child("username").Value.ToString() +
                                " : " + data.Child("message").Value.ToString());
                        }
                    }
                }
            );
    }

    public void SendChatMessage(string username, string msg)
    {
        DatabaseReference chatDB = FirebaseDatabase.DefaultInstance.GetReference("ChatMessage");

        // �� �޼����� �����ϱ� ���� Ű���� �����ϱ� (Push().Key�� �ڵ�����)
        string key = chatDB.Push().Key;

        // <�޼��� ��ȣ, �޼��� ��ųʸ�(�����̸�,�޼�������)>
        Dictionary<string, object> updateMsg = new Dictionary<string, object>();
        // ��������,�޼����� ����ִ� ��ųʸ�
        Dictionary<string, object> msgDic = new Dictionary<string, object>();
        msgDic.Add("message", msg);
        msgDic.Add("username", username);

        msgDic.Add("timestamp", ServerValue.Timestamp);

        updateMsg.Add(key, msgDic); // �޼�����ȣ,�޼����� Ű-����� ��ųʸ��� ����

        // chatDB�� updateMsg�� �߰��ؼ� ������ ������Ʈ
        chatDB.UpdateChildrenAsync(updateMsg)
            .ContinueWithOnMainThread( task =>
            {
                if (task.IsCompleted)
                    Debug.Log(key);
            });
    }
}
