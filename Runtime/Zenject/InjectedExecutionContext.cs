#if STREAMS_ZENJECT_INTEGRATION
using System;
using System.Collections.Generic;
using Streams.Internal;
using Streams.StreamContexts;
using Zenject;

namespace Streams.Zenject {

  internal sealed class InjectedExecutionContext : IStreamExecutionContext, IDisposable {

    private readonly Dictionary<Type, ManagedExecutionStream> _streams = new();
    private readonly IStreamExecutionContext _parentContext;
    private readonly StreamTokenSource _disposeHandle = new();

    public InjectedExecutionContext([Inject(Source = InjectSources.Parent, Optional = true)] IStreamExecutionContext parentContext) {
      _parentContext = parentContext;
    }

    public ExecutionStream GetStream<TSystem>() {
      return GetStream(typeof(TSystem));
    }

    public ExecutionStream GetStream(Type systemType) {
      return _streams.TryGetValue(systemType, out ManagedExecutionStream stream) ? stream : CreateStream(systemType);
    }

    public void Dispose() {
      _disposeHandle.Dispose();
    }

    private ExecutionStream CreateStream(Type systemType) {
      ExecutionStream baseStream = _parentContext != null ? _parentContext.GetStream(systemType) : UnityPlayerLoop.GetStream(systemType);
      var name = $"LocalStream_{NamesUtility.CreateProfilerSampleName(systemType)}";
      var stream = new ManagedExecutionStream(baseStream, name);
      _streams.Add(systemType, stream);
      _disposeHandle.Token.Register(stream.Terminate);
      ExecutionContexts.All.Add(stream, this);
      stream.OnTerminate(() => {
        _streams.Remove(systemType);
        ExecutionContexts.All.Remove(stream);
      });
      return stream;
    }

  }

}
#endif