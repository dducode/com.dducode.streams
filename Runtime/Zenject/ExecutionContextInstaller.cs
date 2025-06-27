#if STREAMS_ZENJECT_INTEGRATION
using Zenject;

namespace Streams.Zenject {

  public class ExecutionContextInstaller : MonoInstaller {

    public override void InstallBindings() {
      Container.BindInterfacesTo<InjectedExecutionContext>().AsSingle().CopyIntoAllSubContainers();
    }

  }

}
#endif