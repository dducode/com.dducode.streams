using System;
using Streams.StreamContexts;
using UnityEngine;
using UnityEngine.PlayerLoop;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Streams {

  public static class UnityPlayerLoop {

    private static IStreamExecutionContext _runtimeContext;
    private static StreamTokenSource _runtimeCancellation = new();

#if UNITY_EDITOR
    private static IStreamExecutionContext _editorContext;
    private static StreamTokenSource _editorCancellation = new();

    static UnityPlayerLoop() {
      EditorApplication.playModeStateChanged += state => {
        if (state == PlayModeStateChange.ExitingPlayMode) 
          EditorInitialize();
      };
    }
#endif

    /// <summary>
    /// Gets the base stream in the specified player loop system
    /// </summary>
    /// <typeparam name="TSystem"> The system that contains the stream </typeparam>
    /// <returns> Existing stream or new </returns>
    /// <remarks> Three streams are always created at system startup (in the <see cref="Update"/>, <see cref="FixedUpdate"/> and <see cref="PreLateUpdate"/> systems) </remarks>
    public static ExecutionStream GetStream<TSystem>() {
#if UNITY_EDITOR
      if (!EditorApplication.isPlaying)
        return _editorContext.GetStream<TSystem>();
#endif

      return _runtimeContext.GetStream<TSystem>();
    }

    internal static ExecutionStream GetStream(Type systemType) {
      return _runtimeContext.GetStream(systemType);
    }

#if UNITY_EDITOR
    [InitializeOnLoadMethod]
    private static void EditorInitialize() {
      _runtimeCancellation?.Release();
      _runtimeCancellation = null;

      _editorCancellation = new StreamTokenSource();
      _editorContext = new GlobalExecutionContext(_editorCancellation.Token);
    }
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void RuntimeInitialize() {
#if UNITY_EDITOR
      _editorCancellation.Release();
      _editorCancellation = null;
#endif
      _runtimeCancellation = new StreamTokenSource();
      _runtimeContext = new GlobalExecutionContext(_runtimeCancellation.Token);
    }

  }

}