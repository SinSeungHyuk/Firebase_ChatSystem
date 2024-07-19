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

    private FirebaseAuth auth; // 인증에 관한 정보 저장할 객체
    private FirebaseUser user; // 파이어베이스 유저의 정보를 담을 객체

    private List<string> receiveKeyList = new List<string>();


    void Start()
    {
        // 구글플레이와 버전이 제대로 호환이 되는지 검사
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

        // auth 객체의 이벤트에 함수 구독
        auth.StateChanged += AuthStateChanged;

        DatabaseReference chatDB = FirebaseDatabase.DefaultInstance.GetReference("ChatMessage");
        chatDB.OrderByChild("timestamp").LimitToLast(1).ValueChanged += ReceiveMessage;

        // Google 로그인에 필요한 토큰 값들
        string googleIdToken = "YOUR_GOOGLE_ID_TOKEN";

        // GoogleAuthProvider를 사용하여 Credential 생성
        Credential credential = GoogleAuthProvider.GetCredential(googleIdToken, null);

        // Credential을 사용하여 Google 계정으로 로그인 시도
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

            // 로그인 성공 시
            FirebaseUser newUser = task.Result;
            Debug.Log("Google 사용자 로그인 성공: " + newUser.DisplayName);
        });
    }

    private void AuthStateChanged(object sender, EventArgs args)
    {
        FirebaseAuth senderAuth = sender as FirebaseAuth;

        if (senderAuth != null)
        {
            // CurrentUser : 현재 접속한 유저의 정보
            user = senderAuth.CurrentUser;
            if (user != null)
            {
                Debug.Log(user.UserId); // 유저가 있다면 id 로그
                UiController.UpdateUserInfo(true, user.UserId);
            }
        }
    }

    public void ReceiveMessage(object sender, ValueChangedEventArgs e) 
    {
        DataSnapshot snapshot = e.Snapshot;

        foreach (var data in snapshot.Children) // ChatMessage 안의 메세지들
        {
            if (receiveKeyList.Contains(data.Key)) continue;

            string username = data.Child("username").Value.ToString();
            string msg = data.Child("message").Value.ToString();
            UiController.AddChatMessage(username,msg);
            receiveKeyList.Add(data.Key);
        }
}

    // 익명 로그인 함수 - 버튼에 등록
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

    // 익명 로그아웃 함수 - 버튼에 등록
    public void SignOut()
    {
        auth.SignOut();
        UiController.UpdateUserInfo(false);
    }

    public void ReadChatMessage()
    {
        // DefaultInstance : 콘솔에 있는 가장 기본적인 최상위 데이터베이스
        DatabaseReference chatDB = FirebaseDatabase.DefaultInstance.GetReference("ChatMessage");
        chatDB.GetValueAsync().ContinueWithOnMainThread(
                task =>
                {
                    if (task.IsFaulted) Debug.LogError("ReadError");
                    else if (task.IsCompleted)
                    {
                        // DataSnapshot : ChatMessage 안에 있는 하위데이터에 접근가능
                        DataSnapshot snapshot = task.Result;

                        // DataSnapshot이 가진 정보 예시
                        Debug.Log("데이터 수:" + snapshot.ChildrenCount);

                        foreach (var data in snapshot.Children) // ChatMessage 안의 메세지들
                        {
                            Debug.Log("메세지번호 :" + data.Key + 
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

        // 새 메세지를 생성하기 위해 키값을 생성하기 (Push().Key로 자동생성)
        string key = chatDB.Push().Key;

        // <메세지 번호, 메세지 딕셔너리(유저이름,메세지내용)>
        Dictionary<string, object> updateMsg = new Dictionary<string, object>();
        // 유저네임,메세지가 들어있는 딕셔너리
        Dictionary<string, object> msgDic = new Dictionary<string, object>();
        msgDic.Add("message", msg);
        msgDic.Add("username", username);

        msgDic.Add("timestamp", ServerValue.Timestamp);

        updateMsg.Add(key, msgDic); // 메세지번호,메세지를 키-밸류로 딕셔너리에 삽입

        // chatDB에 updateMsg를 추가해서 데이터 업데이트
        chatDB.UpdateChildrenAsync(updateMsg)
            .ContinueWithOnMainThread( task =>
            {
                if (task.IsCompleted)
                    Debug.Log(key);
            });
    }
}
