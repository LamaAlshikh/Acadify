namespace Acadify.Models.AdminPages
{
    public class ManageRequestsVM
    {
        public List<RequestRow> PendingRequests { get; set; } = new();

        public class RequestRow
        {
            public int RequestId { get; set; }
<<<<<<< HEAD

            public string StudentName { get; set; } = "";
            public string UniversityId { get; set; } = "";

            public string RequestedAdvisorName { get; set; } = "";
            public string RequestedAdvisorEmail { get; set; } = "";

            public string Status { get; set; } = "Pending";
=======
            public int StudentId { get; set; }
            public int? RequestedAdvisorId { get; set; }

            public string StudentName { get; set; } = string.Empty;
            public string UniversityId { get; set; } = string.Empty;

            public string RequestedAdvisorName { get; set; } = "Not registered yet";
            public string RequestedAdvisorEmail { get; set; } = string.Empty;

            public string Status { get; set; } = string.Empty;
>>>>>>> origin_second/لما2
            public DateTime CreatedAt { get; set; }
        }
    }
}