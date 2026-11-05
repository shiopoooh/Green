using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChange : MonoBehaviour
{
    [SerializeField] private string nextSceneName;

    public void NextScene()
    {
        SceneManager.LoadScene(nextSceneName);
    }
}