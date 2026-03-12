namespace Acadify.Models.AdminPages
{
    public class ManageRequestsVM
    {
        public List<RequestRow> PendingRequests { get; set; } = new();

        public class RequestRow
        {
            public int RequestId { get; set; }

            public string StudentName { get; set; } = "";
            public string UniversityId { get; set; } = "";

            public string RequestedAdvisorName { get; set; } = "";
            public string RequestedAdvisorEmail { get; set; } = "";

            public string Status { get; set; } = "Pending";
            public DateTime CreatedAt { get; set; }
        }
    }
}