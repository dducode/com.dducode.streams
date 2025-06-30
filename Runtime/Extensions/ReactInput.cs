using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.PlayerLoop;

namespace Streams.Extensions {

  public static class ReactInput {

    public static void WhenPressed(this KeyCode keyCode, Action handler, StreamToken token = default) {
      When(() => Input.GetKeyDown(keyCode), handler, token);
    }

    public static void WhenPressed(this KeyCode keyCode, KeyCode modifier, Action handler, StreamToken token = default) {
      When(() => Input.GetKey(modifier) && Input.GetKeyDown(keyCode), handler, token);
    }

    public static void WhenPressed(this KeyCode keyCode, KeyCode modifier1, KeyCode modifier2, Action handler, StreamToken token = default) {
      When(() => Input.GetKey(modifier1) && Input.GetKey(modifier2) && Input.GetKeyDown(keyCode), handler, token);
    }

    public static void WhenHold(this KeyCode keyCode, Action<float> handler, StreamToken token = default) {
      WhenHold(() => Input.GetKey(keyCode), handler, token);
    }

    public static void WhenReleased(this KeyCode keyCode, Action handler, StreamToken token = default) {
      When(() => Input.GetKeyUp(keyCode), handler, token);
    }

    public static void HandleDoubleClick(this KeyCode keyCode, float threshold, Action handler, StreamToken token = default) {
      HandleDoubleClick(() => Input.GetKey(keyCode), threshold, handler, token);
    }

#if ENABLE_INPUT_SYSTEM
    public static void WhenPressed(this InputAction action, Action handler, StreamToken token = default) {
      When(action.WasPressedThisFrame, handler, token);
    }

    public static void WhenHold(this InputAction action, Action<float> handler, StreamToken token = default) {
      WhenHold(action.IsPressed, handler, token);
    }

    public static void WhenReleased(this InputAction action, Action handler, StreamToken token = default) {
      When(action.WasReleasedThisFrame, handler, token);
    }

    public static void HandleDoubleClick(this InputAction action, float threshold, Action handler, StreamToken token = default) {
      HandleDoubleClick(action.WasPressedThisFrame, threshold, handler, token);
    }
#endif

    private static void When(Func<bool> condition, Action handler, StreamToken token) {
      UnityPlayerLoop.GetStream<Update>().Add(_ => {
        if (condition())
          handler();
      }, token);
    }

    private static void WhenHold(Func<bool> condition, Action<float> handler, StreamToken token) {
      var holdingTime = 0f;
      UnityPlayerLoop.GetStream<Update>().Add(deltaTime => {
        if (condition())
          handler(holdingTime += deltaTime);
        else
          holdingTime = 0;
      }, token);
    }

    private static void HandleDoubleClick(Func<bool> condition, float threshold, Action handler, StreamToken token) {
      var lastClickTime = 0f;
      var released = true;

      UnityPlayerLoop.GetStream<Update>().Add(_ => {
        if (condition()) {
          if (Time.time < lastClickTime + threshold) {
            if (released) {
              handler();
              released = false;
            }
          }

          lastClickTime = Time.time;
        }

        if (Time.time > lastClickTime + threshold * 1.2f)
          released = true;
      }, token);
    }

  }

}