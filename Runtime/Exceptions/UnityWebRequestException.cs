using System;
using UnityEngine.Networking;

namespace Streams.Exceptions {

  public class UnityWebRequestException : Exception {

    private protected UnityWebRequestException(UnityWebRequest webRequest) : base(webRequest.error) {
    }

  }

}