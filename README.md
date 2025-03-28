# About Streams

Streams is the unity package for simplify managed the low level player loop API. In the most common case you can
subscribe to any stream of the player loop subsystem

```csharp
UnityPlayerLoop.GetStream<Update>().Add(deltaTime => Debug.Log(deltaTime));
```

## Installing
Install the package in unity package manager from the git url `https://github.com/dducode/com.dducode.streams.git`

## Execution types

Many different types of actions can be connected to a stream. In the previous example showed one way to subscribe
to a stream - it's persistent action. Also, you can connect parallel action, which will be executed in parallel with the
others

```csharp
UnityPlayerLoop.GetStream<Update>().AddParallel(deltaTime => /* some actions */);
```

To configure the parallel work strategy, set the stream's `WorkStrategy` property to `Economy`, `Optimal` or `Performance`

Ordinary actions will be executed sequentially by priority or by the time of their creation with the same priority.
Also, one of the common ways to connect to the stream - once action

```csharp
UnityPlayerLoop.GetStream<Update>().AddOnce(() => /* some actions */);
```

And async once action

```csharp
UnityPlayerLoop.GetStream<Update>().AddOnce(async () => {
  await StreamTask.Delay(100);
  Debug.Log("Complete action");
});
```

You can use `StreamTask` inside the stream execution only. Also, you can convert `Task`, `UniTask` or `Awaitable` to `StreamTask` using extension methods.
`StreamTask` allows code execution to continue in the same stream

If you want to separate your persistent action, you can pass coroutine to the stream

```csharp
UnityPlayerLoop.GetStream<Update>().Add(Coroutine);

private IEnumerator Coroutine() {
  Debug.Log(1);
  yield return null;
  Debug.Log(2);
  yield return null;
  Debug.Log(3);
}
```

## Execution configuration

If you want, you can configure the action by setting the priority and subscription cancellation token

```csharp
UnityPlayerLoop.GetStream<Update>().Add(deltaTime => Debug.Log(deltaTime), subscriptionToken, 0);
```

Zero priority is the highest, the lowest priority is the max value of the `uint` type. If a token cancellation
is requested, the subscription will be destroyed. Also, you can configure the delta of the execution and its tick rate

```csharp
UnityPlayerLoop.GetStream<Update>().Add(deltaTime => Debug.Log(deltaTime)).SetDelta(0.5f); // will be executed twice per second
```

You can adjust either delta or tick rate at the same time

## Streams lifetime

Streams from the unity player loop live all the time. But you can subscribe to the scene streams or the game object
streams

```csharp
SceneManager.GetActiveScene().GetStream<Update>();
GameObject.Find("MyGO").GetStream<FixedUpdate>();
```

Scene streams live as long as their scene is loaded. Same goes for game object streams - they live as long as their game
object is alive.
For scene streams, the underlying streams are the player loop streams, and for game object streams, the underlying
streams are the scene streams.

## Managed streams

In a more advanced scenario you can create and configure managed execution stream

```csharp
var customStream = new ManagedExecutionStream(UnityPlayerLoop.GetStream<Update>()) { // a managed stream requires a base stream in which to execute
  Priority = 0,
  Delta = 0.02f
};
customStream.Add(deltaTime => Debug.Log(deltaTime));
```

You can also lock a stream with a token, join it with another stream, reconnect to another base stream, and terminate it