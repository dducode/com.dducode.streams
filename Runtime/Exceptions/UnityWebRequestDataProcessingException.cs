using UnityEngine.Networking;

namespace Streams.Exceptions {

  public class UnityWebRequestDataProcessingException : UnityWebRequestException {

    internal UnityWebRequestDataProcessingException(UnityWebRequest webRequest) : base(webRequest) {
    }

  }

}