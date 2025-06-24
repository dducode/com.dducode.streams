using Streams.StreamContexts;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Streams {

  public class GameObjectStreamsContextFactory {

    public GameObjectExecutionContext Create(Transform parent = null, string name = "StreamContext") {
      var gameObject = new GameObject(name);
      if (parent != null)
        gameObject.transform.SetParent(parent);
      return gameObject.AddComponent<GameObjectExecutionContext>();
    }

    public GameObjectExecutionContext Create(Scene scene, string name = "StreamContext") {
      var gameObject = new GameObject(name);
      SceneManager.MoveGameObjectToScene(gameObject, scene);
      return gameObject.AddComponent<GameObjectExecutionContext>();
    }

  }

}