using Firebase.Auth;
using Firebase.Firestore;
using UnityEngine;
using UnityEngine.SceneManagement;
using Firebase.Extensions;
using System.Text.RegularExpressions;
using System.Linq;

public class AuthManager : MonoBehaviour {
    public LoginUI loginUI;

    public void SignUp() {
        string email = loginUI.signupEmailField.text;
        string password = loginUI.signupPasswordField.text;
        string confirmPassword = loginUI.signupConfirmPasswordField.text;

        // 입력 검증
        if (string.IsNullOrWhiteSpace(email)) {
            loginUI.codeDisplay.text = "이메일을 입력하세요.";
            Debug.LogError("이메일이 비어 있습니다.");
            return;
        }
        if (!IsValidEmail(email)) {
            loginUI.codeDisplay.text = "올바른 이메일 형식을 입력하세요.";
            Debug.LogError("잘못된 이메일 형식: " + email);
            return;
        }
        if (string.IsNullOrWhiteSpace(password)) {
            loginUI.codeDisplay.text = "비밀번호를 입력하세요.";
            Debug.LogError("비밀번호가 비어 있습니다.");
            return;
        }
        if (password != confirmPassword) {
            loginUI.codeDisplay.text = "비밀번호가 일치하지 않습니다.";
            Debug.LogError("비밀번호 확인 실패: 비밀번호가 일치하지 않음");
            return;
        }

        FirebaseAuth auth = FirebaseAuth.DefaultInstance;
        auth.CreateUserWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task => {
            if (task.IsCompletedSuccessfully) {
                Debug.Log("회원가입 성공: " + task.Result.User.UserId);
                GenerateCoupleCode(task.Result.User.UserId);
                loginUI.ShowCodePanel();
            } else {
                Debug.LogError("회원가입 실패: " + task.Exception);
                loginUI.codeDisplay.text = "회원가입 실패: " + (task.Exception?.Message ?? "알 수 없는 오류");
            }
        });
    }

    public void Login() {
        string email = loginUI.loginEmailField.text;
        string password = loginUI.loginPasswordField.text;

        // 입력 검증
        if (string.IsNullOrWhiteSpace(email)) {
            loginUI.codeDisplay.text = "이메일을 입력하세요.";
            Debug.LogError("이메일이 비어 있습니다.");
            return;
        }
        if (string.IsNullOrWhiteSpace(password)) {
            loginUI.codeDisplay.text = "비밀번호를 입력하세요.";
            Debug.LogError("비밀번호가 비어 있습니다.");
            return;
        }

        FirebaseAuth auth = FirebaseAuth.DefaultInstance;
        auth.SignInWithEmailAndPasswordAsync(email, password).ContinueWithOnMainThread(task => {
            if (task.IsCompletedSuccessfully) {
                Debug.Log("로그인 성공: " + task.Result.User.UserId);
                CheckCoupleStatus(task.Result.User.UserId);
            } else {
                Debug.LogError("로그인 실패: " + task.Exception);
                loginUI.codeDisplay.text = "로그인 실패: " + (task.Exception?.Message ?? "알 수 없는 오류");
            }
        });
    }

    public void CheckCoupleStatus(string userId) {
        if (string.IsNullOrEmpty(userId)) {
            loginUI.codeDisplay.text = "로그인이 필요합니다.";
            Debug.LogError("커플 상태 확인 실패: userId가 null입니다.");
            return;
        }

        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
        db.Collection("couples").WhereEqualTo("User1", userId).GetSnapshotAsync().ContinueWithOnMainThread(task => {
            if (task.IsCompletedSuccessfully) {
                var snapshot = task.Result;
                if (snapshot.Count > 0) {
                    Debug.Log("커플 연결됨, HomeScene으로 이동");
                    SceneManager.LoadScene("HomeScene");
                } else {
                    db.Collection("couples").WhereEqualTo("User2", userId).GetSnapshotAsync().ContinueWithOnMainThread(task2 => {
                        if (task2.IsCompletedSuccessfully) {
                            var snapshot2 = task2.Result;
                            if (snapshot2.Count > 0) {
                                Debug.Log("커플 연결됨, HomeScene으로 이동");
                                SceneManager.LoadScene("HomeScene");
                            } else {
                                Debug.Log("커플 미연결, 코드 입력 필요");
                                loginUI.codeDisplay.text = "코드를 입력하세요.";
                                loginUI.ShowCodePanel();
                            }
                        } else {
                            Debug.LogError("커플 상태 확인 실패: " + task2.Exception);
                            loginUI.codeDisplay.text = "오류 발생: " + (task2.Exception?.Message ?? "알 수 없는 오류");
                        }
                    });
                }
            } else {
                Debug.LogError("커플 상태 확인 실패: " + task.Exception);
                loginUI.codeDisplay.text = "오류 발생: " + (task.Exception?.Message ?? "알 수 없는 오류");
            }
        });
    }

    void GenerateCoupleCode(string userId) {
        string code = System.Guid.NewGuid().ToString().Substring(0, 8);
        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
        db.Collection("users").Document(userId).SetAsync(new { CoupleCode = code, PartnerCode = "" }).ContinueWithOnMainThread(task => {
            if (task.IsCompletedSuccessfully) {
                loginUI.codeDisplay.text = "새 코드: " + code + "\n상대방에게 이 코드를 공유하세요.";
                Debug.Log("코드 생성: " + code);
            } else {
                Debug.LogError("코드 생성 실패: " + task.Exception);
                loginUI.codeDisplay.text = "코드 생성 실패: " + (task.Exception?.Message ?? "알 수 없는 오류");
            }
        });
    }

    public void ConnectCouple() {
        string inputCode = loginUI.codeInputField.text;
        if (string.IsNullOrWhiteSpace(inputCode)) {
            loginUI.codeDisplay.text = "코드를 입력하세요.";
            Debug.LogError("코드가 비어 있습니다.");
            return;
        }

        FirebaseAuth auth = FirebaseAuth.DefaultInstance;
        if (auth.CurrentUser == null) {
            loginUI.codeDisplay.text = "로그인이 필요합니다.";
            Debug.LogError("코드 입력 실패: 로그인이 필요함");
            return;
        }

        string userId = auth.CurrentUser.UserId;
        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;

        // 입력한 코드를 PartnerCode로 저장
        db.Collection("users").Document(userId).UpdateAsync("PartnerCode", inputCode).ContinueWithOnMainThread(task => {
            if (!task.IsCompletedSuccessfully) {
                Debug.LogError("PartnerCode 저장 실패: " + task.Exception);
                loginUI.codeDisplay.text = "오류 발생: " + (task.Exception?.Message ?? "알 수 없는 오류");
                return;
            }

            // 입력한 코드가 유효한지 확인
            db.Collection("users").WhereEqualTo("CoupleCode", inputCode).GetSnapshotAsync().ContinueWithOnMainThread(task2 => {
                if (!task2.IsCompletedSuccessfully) {
                    Debug.LogError("코드 조회 실패: " + task2.Exception);
                    loginUI.codeDisplay.text = "오류 발생: " + (task2.Exception?.Message ?? "알 수 없는 오류");
                    return;
                }

                var snapshot = task2.Result;
                var partnerDoc = snapshot.Documents.FirstOrDefault();
                if (partnerDoc == null) {
                    Debug.LogError("잘못된 코드");
                    loginUI.codeDisplay.text = "잘못된 코드입니다.";
                    return;
                }

                string partnerId = partnerDoc.Id;
                // 현재 사용자의 CoupleCode 가져오기
                db.Collection("users").Document(userId).GetSnapshotAsync().ContinueWithOnMainThread(task3 => {
                    if (!task3.IsCompletedSuccessfully) {
                        Debug.LogError("내 코드 조회 실패: " + task3.Exception);
                        loginUI.codeDisplay.text = "오류 발생: " + (task3.Exception?.Message ?? "알 수 없는 오류");
                        return;
                    }

                    var myDoc = task3.Result;
                    string myCoupleCode = myDoc.Exists && myDoc.ContainsField("CoupleCode") ? myDoc.GetValue<string>("CoupleCode") : "";

                    // 상대방의 PartnerCode 가져오기
                    db.Collection("users").Document(partnerId).GetSnapshotAsync().ContinueWithOnMainThread(task4 => {
                        if (!task4.IsCompletedSuccessfully) {
                            Debug.LogError("파트너 데이터 조회 실패: " + task4.Exception);
                            loginUI.codeDisplay.text = "오류 발생: " + (task4.Exception?.Message ?? "알 수 없는 오류");
                            return;
                        }

                        var partnerData = task4.Result;
                        string partnerPartnerCode = partnerData.Exists && partnerData.ContainsField("PartnerCode") ? partnerData.GetValue<string>("PartnerCode") : "";

                        if (partnerPartnerCode == myCoupleCode) {
                            // 양방향 코드 입력 확인, 커플 연결
                            string coupleId = System.Guid.NewGuid().ToString();
                            db.Collection("couples").Document(coupleId).SetAsync(new {
                                User1 = userId,
                                User2 = partnerId,
                                SharedSceneJson = ""
                            }).ContinueWithOnMainThread(coupleTask => {
                                if (coupleTask.IsCompletedSuccessfully) {
                                    Debug.Log("커플 연결 성공, HomeScene으로 이동");
                                    SceneManager.LoadScene("HomeScene");
                                } else {
                                    Debug.LogError("커플 연결 실패: " + coupleTask.Exception);
                                    loginUI.codeDisplay.text = "커플 연결 실패: " + (coupleTask.Exception?.Message ?? "알 수 없는 오류");
                                }
                            });
                        } else {
                            loginUI.codeDisplay.text = "연결 대기 중: 상대방이 새 코드(" + myCoupleCode + ")를 입력해야 합니다.";
                            Debug.Log("연결 대기 중: 상대방이 코드를 입력하지 않음");
                        }
                    });
                });
            });
        });
    }

    public void ReissueCoupleCode() {
        FirebaseAuth auth = FirebaseAuth.DefaultInstance;
        if (auth.CurrentUser == null) {
            loginUI.codeDisplay.text = "로그인이 필요합니다.";
            Debug.LogError("코드 재발급 실패: 로그인이 필요함");
            return;
        }

        string userId = auth.CurrentUser.UserId;
        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;

        // 기존 커플 연결 확인
        var query1 = db.Collection("couples").WhereEqualTo("User1", userId).GetSnapshotAsync();
        var query2 = db.Collection("couples").WhereEqualTo("User2", userId).GetSnapshotAsync();

        System.Threading.Tasks.Task.WhenAll(query1, query2).ContinueWithOnMainThread(task => {
            if (task.IsCompletedSuccessfully) {
                bool isCoupled = (query1.Result.Count > 0 || query2.Result.Count > 0);
                if (isCoupled) {
                    loginUI.codeDisplay.text = "이미 커플이 연결되어 있습니다.";
                    Debug.Log("코드 재발급 불가: 이미 커플 연결됨");
                } else {
                    GenerateCoupleCode(userId);
                }
            } else {
                Debug.LogError("커플 상태 확인 실패: " + task.Exception);
                loginUI.codeDisplay.text = "오류 발생: " + (task.Exception?.Message ?? "알 수 없는 오류");
            }
        });
    }

    // 이메일 형식 검증
    private bool IsValidEmail(string email) {
        if (string.IsNullOrWhiteSpace(email)) return false;
        string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
        return Regex.IsMatch(email, pattern);
    }
}