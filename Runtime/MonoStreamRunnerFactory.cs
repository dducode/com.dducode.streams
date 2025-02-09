using StreamsForUnity.StreamRunners;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StreamsForUnity {

  public class MonoStreamRunnerFactory {

    public TRunner Create<TRunner>(Transform parent = null, string name = "StreamRunner") where TRunner : MonoBehaviour, IStreamRunner {
      var gameObject = new GameObject(name);
      if (parent != null)
        gameObject.transform.SetParent(parent);
      return gameObject.AddComponent<TRunner>();
    }

    public TRunner Create<TRunner>(Scene scene, string name = "StreamRunner") where TRunner : MonoBehaviour, IStreamRunner {
      var gameObject = new GameObject(name);
      SceneManager.MoveGameObjectToScene(gameObject, scene);
      return gameObject.AddComponent<TRunner>();
    }

  }

}