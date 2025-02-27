using StreamsForUnity.StreamHolders;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StreamsForUnity {

  public class MonoStreamHolderFactory {

    public THolder Create<THolder>(Transform parent = null, string name = "StreamHolder") where THolder : MonoBehaviour, IStreamHolder {
      var gameObject = new GameObject(name);
      if (parent != null)
        gameObject.transform.SetParent(parent);
      return gameObject.AddComponent<THolder>();
    }

    public THolder Create<THolder>(Scene scene, string name = "StreamHolder") where THolder : MonoBehaviour, IStreamHolder {
      var gameObject = new GameObject(name);
      SceneManager.MoveGameObjectToScene(gameObject, scene);
      return gameObject.AddComponent<THolder>();
    }

  }

}