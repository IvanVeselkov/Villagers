using UnityEngine;
using UnityEngine.SceneManagement;
public class MenuController : MonoBehaviour
{
  public void LoadScene(string sceneName)
  {
    SceneManager.LoadScene(sceneName);
  }

  public void ExitGame()
  {
    Application.Quit();
  }

  public void ActiveObject(GameObject gameObject)
  {
    if(gameObject.activeSelf==true)
    {
      gameObject.SetActive(false);
    }
    else
    {
      gameObject.SetActive(true);
    }
  }
}
