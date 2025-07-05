using UnityEngine.Networking;

namespace Streams.Exceptions {

  public class UnityWebRequestConnectionException : UnityWebRequestException {

    internal UnityWebRequestConnectionException(UnityWebRequest webRequest) : base(webRequest) {
    }

  }

}