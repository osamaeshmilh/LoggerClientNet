namespace LoggerClient.Models;

public class LogEntry
{
    public DateTime request_timestamp { get; set; }
    public string http_method { get; set; }
    public string request_url { get; set; }
    public string http_status_code { get; set; }
    public string remote_ip_address { get; set; }
    public long duration { get; set; }
    public string request_headers { get; set; }
    public string response_headers { get; set; }
    public string request_url_parameters { get; set; }
    public string request_body { get; set; }
    public string request_cookies { get; set; }
    public string response_body { get; set; }
    public string response_cookies { get; set; }
    public long? application_id { get; set; }
}