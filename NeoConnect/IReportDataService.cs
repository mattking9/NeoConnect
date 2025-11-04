
namespace NeoConnect
{
    public interface IReportDataService
    {
        void Add(IEnumerable<string> data);
        void Add(string data);
        void Clear();
        string? ToHtmlReportString();
    }
}