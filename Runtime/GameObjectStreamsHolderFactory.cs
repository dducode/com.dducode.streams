using StreamsForUnity.StreamHolders;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace StreamsForUnity {

  public class GameObjectStreamsHolderFactory {

    public GameObjectStreamsHolder Create(Transform parent = null, string name = "StreamHolder") {
      var gameObject = new GameObject(name);
      if (parent != null)
        gameObject.transform.SetParent(parent);
      return gameObject.AddComponent<GameObjectStreamsHolder>();
    }

    public GameObjectStreamsHolder Create(Scene scene, string name = "StreamHolder") {
      var gameObject = new GameObject(name);
      SceneManager.MoveGameObjectToScene(gameObject, scene);
      return gameObject.AddComponent<GameObjectStreamsHolder>();
    }

  }

}