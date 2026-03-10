using Acadify.Models;
using Microsoft.AspNetCore.Mvc;

namespace Acadify.Controllers
{
    public class StudentController : Controller
    {
        // Student Home Page
        public IActionResult StudentHome()
        {
            // مؤقتًا: القيمة جاية من الإيجنت
            // لاحقًا: تستبدل بقيمة من Database
            int progressFromAgent = 80;

            var model = new StudentHomeViewModel
            {
                StudentName = "lama alshikh",
                StudentEmail = "lalshikh@stu.kau.edu.sa",
                ProgressPercentage = progressFromAgent,
                CurrentStatus = GetStatus(progressFromAgent)
            };

            return View(model);
        }

        // تحديد حالة الطالبة بناءً على نسبة التقدم
        private string GetStatus(int progress)
        {
            if (progress <= 30)
                return "Beginning";

            if (progress <= 70)
                return "Has Remaining Courses";

            return "Near Graduation";
        }



        /* =========================================================
                          CommunityStudent
          ========================================================= */

        // Community Student Page
        [HttpGet]
        public IActionResult CommunityStudent()
        {
            var model = new CommunityStudentVM
            {
                Messages = new List<CommunityMessageVM>
                {
                    new CommunityMessageVM
                    {
                        SenderName = "Lina Alrwaily",
                        SenderInitials = "LA",
                        MessageText = "السلام عليكم دكتورة أمينة",
                        IsCurrentUserMessage = false,
                        BubbleColorClass = "msg-blue"
                    },
                    new CommunityMessageVM
                    {
                        SenderName = "Lina Alrwaily",
                        SenderInitials = "LA",
                        MessageText = "هل اقدر أنزل مادة تطوير برمجيات الترم الجاي؟",
                        IsCurrentUserMessage = false,
                        BubbleColorClass = "msg-blue"
                    },
                    new CommunityMessageVM
                    {
                        SenderName = "Rahaf Alghamdi",
                        SenderInitials = "RA",
                        MessageText = "ايوا دكتورة حتى انا",
                        IsCurrentUserMessage = false,
                        BubbleColorClass = "msg-pink"
                    },
                    new CommunityMessageVM
                    {
                        SenderName = "Amina Gamlo",
                        SenderInitials = "AG",
                        MessageText = "و عليكم السلام و رحمة الله و بركاته\nليش ما تبغو تنزلوها هذا الترم؟",
                        IsCurrentUserMessage = false,
                        BubbleColorClass = "msg-purple"
                    },
                    new CommunityMessageVM
                    {
                        SenderName = "Lama Alshaikh (me)",
                        SenderInitials = "LA",
                        MessageText = "عندي استفسار بخصوص التدريب",
                        IsCurrentUserMessage = true,
                        BubbleColorClass = "msg-indigo"
                    }
                },

                Members = new List<CommunityMemberVM>
                {
                    new CommunityMemberVM
                    {
                        Name = "DR.Amina Gamlo",
                        ImagePath = "~/images/user.png"
                    },
                    new CommunityMemberVM
                    {
                        Name = "Lina Alrwaily",
                        ImagePath = "~/images/user.png"
                    },
                    new CommunityMemberVM
                    {
                        Name = "Rahaf Alghamdi",
                        ImagePath = "~/images/user.png"
                    },
                    new CommunityMemberVM
                    {
                        Name = "Rahaf Alzahrani",
                        ImagePath = "~/images/user.png"
                    }
                }
            };

            return View(model);
        }

        // Send message from student page
        [HttpPost]
        public IActionResult SendStudentMessage([FromBody] SendStudentMessageRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest(new { success = false, message = "Message is empty." });
            }

            // لاحقًا هنا نحفظ الرسالة في الداتابيس
            // مثال:
            // var chatMessage = new ChatMessage { ... };
            // _context.ChatMessages.Add(chatMessage);
            // _context.SaveChanges();

            return Json(new
            {
                success = true,
                text = request.Message.Trim()
            });
        }

        // Request body for AJAX
        public class SendStudentMessageRequest
        {
            public string Message { get; set; } = string.Empty;
        }
    }
}
