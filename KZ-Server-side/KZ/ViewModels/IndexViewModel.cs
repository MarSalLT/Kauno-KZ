using System.Web.Configuration;

namespace KZ.ViewModels
{
    public class IndexViewModel
    {
        public string Description { get; }
        public string Title { get; }
        public string Version { get; }
        public IndexViewModel()
        {
            Title = "Kauno miesto techninių eismo reguliavimo priemonių IS";
            Version = WebConfigurationManager.AppSettings["Version"];
        }
    }
}