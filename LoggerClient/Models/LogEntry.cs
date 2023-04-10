using System;

namespace LoggerClient.Models
{
    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public string HttpMethod { get; set; }
        public string RequestUrl { get; set; }
        public int HttpStatusCode { get; set; }
        public string RemoteIpAddress { get; set; }
        public long Duration { get; set; }
        public string RequestHeaders { get; set; }
        public string ResponseHeaders { get; set; }
        public string RequestUrlParameters { get; set; }
        public string RequestBody { get; set; }
        public string RequestCookies { get; set; }
        public string ResponseBody { get; set; }
        public string ResponseCookies { get; set; }
    }
}