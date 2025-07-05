using UnityEngine.Networking;

namespace Streams.Exceptions {

  public class UnityWebRequestProtocolException : UnityWebRequestException {

    internal UnityWebRequestProtocolException(UnityWebRequest webRequest) : base(webRequest) {
    }

  }

}