using UnityEngine;
using UnityEngine.SceneManagement;

public class ChangeScene : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            SceneManager.LoadScene(0);
        }

        if (Input.GetKeyDown(KeyCode.O))
        {
            SceneManager.LoadScene(1);
        }
    }
}
