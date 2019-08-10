using System.Collections.Generic;

namespace Infrastructure.Session.Abstraction
{
    public interface ISessionStorage
    {
        void SetHeaders(params (string Key, IEnumerable<string> Value)[] headers);
        void SetHeaders(Dictionary<string, string> headers);

        //adds to every log message
        Dictionary<string, object> GetLoggingHeaders();

        //used as context to call api or enquue messages
        Dictionary<string, string> GetTraceHeaders();
        Dictionary<string, string> GetExternalTraceHeaders();
    }
}
